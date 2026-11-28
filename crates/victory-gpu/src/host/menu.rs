//! The F1 settings menu. F1 shows and hides it while our window has the
//! focus; Up/Down pick a setting, Left/Right change it, Enter runs the line
//! picked (save, save and restart, preload shaders, reset, close), Esc
//! closes. Preloading saves, ends this run and has the launcher make every
//! shader before the title starts again (`VICTORY_PRELOAD=1`, `victory
//! preload`); it asks for a second Enter first. While it is
//! shown the title sees no keyboard (`crate::SETTINGS_MENU_OPEN`, read by the
//! input module); a pad still works.
//!
//! The settings are read once at start, so a change is saved at once (as well as by the save
//! lines) to the file
//! `VICTORY_MENU_FILE` names (`scripts\play-foxhound.ps1` sets it to
//! `victory-settings.txt` in the repository's folder and applies it at every
//! start) and takes effect at the next start; "SAVE AND RESTART" starts the
//! launcher again (`VICTORY_MENU_PLAY`, with `VICTORY_MENU_ARGS`) and ends
//! this run. The `VICTORY_MENU_...` settings are the launcher's own choices
//! (DLSS mode, frame generation, frame rate, console, picture size), which it
//! turns into the layer's switches; the others are the switches themselves.
//!
//! In every build, as the stats panel is. Drawn with the panel's letters, in
//! colours of its own ([`Tone`]): a frame, a title bar, the settings in
//! sections, the line picked on a bar, and under them what that line does;
//! copied onto the swap chain's buffer, in the middle of the picture.
//!
//! Started by Julia's Launcher (`victory run`, [`by_launcher`]): its own
//! choices (DLSS, frame generation, the frame rate cap, MFG Unlocker) are
//! shown and not changed here, the console lines are left out (it always
//! plays the Series S profile), and the launcher applies the rest of the
//! saved file at its next PLAY.

use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::{Mutex, OnceLock};
use std::time::Duration;

use windows::Win32::Graphics::Direct3D12::*;
use windows::Win32::Graphics::Dxgi::Common::*;
use windows_sys::Win32::UI::Input::KeyboardAndMouse::{
    GetAsyncKeyState, VK_DOWN, VK_ESCAPE, VK_F1, VK_LEFT, VK_RETURN, VK_RIGHT, VK_UP,
};
use windows_sys::Win32::UI::WindowsAndMessaging::{GetForegroundWindow, GetWindowThreadProcessId};

use super::font::glyph;
use super::{Host, HostList, transition};

const GLYPH: (u32, u32) = (5, 7);
const CELL: (u32, u32) = (6, 9);
const COLUMNS: u32 = 64;
/// Lines of the box: a title, a help line, a blank, the settings shown, a
/// blank, what the line picked does (`ABOUT`, else blank), a note and the
/// repository's link.
const ROWS: usize = 22;
const LINES: u32 = ROWS as u32 + 9;
/// The action that preloads shaders, and what is said of it while it is picked.
const PRELOAD: usize = 2;
const ABOUT_PRELOAD: [&str; 3] = [
    "MAKES EVERY SHADER AHEAD, THEN STARTS THE GAME AGAIN.",
    "TAKES A WHILE: MAYBE A LONG TIME, DEPENDING ON HARDWARE.",
    "A LATER UPDATE OF THIS BUILD MAY NEED A NEW PRELOAD.",
];
/// The actions that export and import the game's save (`crate::save_share`), and what is said
/// of them while they are picked.
const EXPORT: usize = 4;
const IMPORT: usize = 5;
const ABOUT_EXPORT: [&str; 3] = [
    "MAKES A ZIP OF YOUR SAVE IN SAVES, IN THE FOLDER OF",
    "GAME_SCARLETT_MASTER.EXE. SEND THAT ZIP TO SHARE IT.",
    "YOUR SAVE STAYS AS IT IS. GAME SETTINGS ARE LEFT OUT.",
];
const ABOUT_IMPORT: [&str; 3] = [
    "PUT A SAVE ZIP INTO SAVES, IN THE FOLDER OF",
    "GAME_SCARLETT_MASTER.EXE, THEN PRESS ENTER HERE AND",
    "PICK IT FROM THE LIST. YOUR GAME SETTINGS ARE KEPT.",
];
/// The list IMPORT SAVE opens: the saves in `Saves`, then a line back to the settings.
const PICK_TITLE: &str = "IMPORT SAVE                       ESC GOES BACK";
const PICK_HELP: &str = "UP/DOWN PICK  ENTER TWICE IMPORTS IT";
const PICK_NONE: &str = "  NONE YET: PUT A SAVE ZIP INTO SAVES IN THE GAME FOLDER";
const PICK_BACK: &str = "BACK TO THE SETTINGS";
const ABOUT_PICK: [&str; 3] = [
    "THE SAVE ZIPS IN SAVES, IN THE GAME FOLDER, NEWEST FIRST.",
    "ENTER TWICE: THE GAME RESTARTS WITH THE SAVE PICKED.",
    "YOUR OLD SAVE IS ZIPPED INTO SAVES FIRST, SO IT IS KEPT.",
];
/// The note after the first Enter on a save in the list: the second one imports it.
const CONFIRM_IMPORT: &str = "ENTER AGAIN: IMPORT IT AND RESTART THE GAME";
/// The longest save name shown in the list.
const IMPORT_NAME: usize = 56;

/// A save's file name as the menu's letters can show it.
fn shown_name(name: &str) -> String {
    name.chars()
        .map(|c| c.to_ascii_uppercase())
        .map(|c| if c.is_ascii() && glyph(c as u8) != glyph(b'#') { c } else { '-' })
        .take(IMPORT_NAME)
        .collect()
}

/// The setting that turns MFG Unlocker on, and what is said of it while it is picked.
const MFG: &str = "VICTORY_MFG_UNLOCKER";
const ABOUT_MFG: [&str; 3] = [
    "A MOD, NOT OURS: DLSS FRAME GEN ON RTX 20 AND 30 CARDS.",
    "ON: DOWNLOADED FROM ITS GITHUB AT THE NEXT START, THEN RUN.",
    "NVIDIA DOES NOT SUPPORT IT. IF THE GAME FAILS, TURN IT OFF.",
];
/// The DLSS setting, and what is said while it is picked on in a start without NVIDIA's files.
const DLSS: &str = "VICTORY_MENU_DLSS";
const ABOUT_NO_DLSS: [&str; 3] = [
    "THE NVIDIA DLSS FILES ARE NOT ON THIS PC YET: THEY ARE",
    "DOWNLOADED FROM NVIDIA ON GITHUB AT THE NEXT START",
    "(ABOUT 70 MB, ONCE). SAVE AND RESTART NOW DOES IT.",
];
/// The note after the first Enter on it: the second one does it.
const CONFIRM_PRELOAD: &str = "ENTER AGAIN: SAVE, CLOSE THE GAME AND PRELOAD";
/// The last line of the box: where this build comes from.
const REPO: &str = "GITHUB.COM/FELICIENRADIO/COLD-PROJECT-VI";
const RIM: u32 = 4;
const UPLOADS: usize = 4;

/// A setting: the variable, its name in the menu, the values it takes
/// (`""`: not set, the build's own default), whether a change applies at once,
/// and what it does (shown under the settings while it is picked).
struct Item {
    var: &'static str,
    label: &'static str,
    values: &'static [(&'static str, &'static str)],
    live: bool,
    about: &'static str,
}

const ON_UNSET: &[(&str, &str)] = &[("1", "ON"), ("", "OFF")];
const UNSET_ON: &[(&str, &str)] = &[("", "OFF"), ("1", "ON")];
const STAGES: &[(&str, &str)] = &[("2", "2"), ("1", "1"), ("0", "0 OFF")];

/// The first value of each is what the launcher starts with. In the order shown, by section
/// ([`SECTIONS`]).
#[rustfmt::skip]
const ITEMS: &[Item] = &[
    // Upscaling and frame generation: the launcher's own choices, and the DLSS model.
    Item { var: "VICTORY_MENU_DLSS", label: "DLSS", values: &[("quality", "QUALITY"), ("balanced", "BALANCED"), ("performance", "PERFORMANCE"), ("dlaa", "DLAA (NATIVE)"), ("off", "OFF")], live: false,
        about: "NVIDIA DLSS DRAWS THE WORLD SMALLER AND REBUILDS IT SHARP. QUALITY LOOKS BEST, PERFORMANCE IS THE FASTEST. RTX CARDS ONLY." },
    Item { var: "VICTORY_DLSS_PRESET", label: "DLSS MODEL", values: &[("", "DEFAULT"), ("k", "K"), ("j", "J"), ("m", "M"), ("l", "L")], live: false,
        about: "THE DLSS MODEL. DEFAULT IS USUALLY BEST, TRY K OR M IF MOVING THINGS SMEAR." },
    Item { var: "VICTORY_MENU_FRAMEGEN", label: "FRAME GENERATION", values: &[("0", "OFF"), ("2", "2X"), ("3", "3X"), ("4", "4X"), ("5", "5X"), ("6", "6X (MOST)")], live: false,
        about: "MADE-UP FRAMES BETWEEN THE GAME FRAMES: SMOOTHER MOTION, A LITTLE MORE INPUT LAG. THE GAME ITSELF DOES NOT RUN FASTER." },
    // DLSS Frame Generation where it runs (RTX 40 and later), else AMD's FSR 3 (2X only: it reads
    // the upscaling, DLSS's or AMD's own with VICTORY_FSR=1).
    Item { var: "VICTORY_MENU_FG_TYPE", label: "FRAME GEN TYPE", values: &[("auto", "AUTO"), ("dlss", "DLSS"), ("fsr", "FSR 3")], live: false,
        about: "DLSS FRAME GEN NEEDS AN RTX 40 OR 50 (RTX 20/30: MFG UNLOCKER). FSR 3 GIVES 2X ON DLSS UPSCALING, OR ON THE RADEON FSR UPSCALER." },
    // Someone else's mod (apps\victory\src\play.rs): downloaded at the next start, then loaded.
    Item { var: MFG, label: "MFG UNLOCKER (MOD, RTX 20/30)", values: UNSET_ON, live: false,
        about: "A MOD, NOT OURS: DLSS FRAME GEN ON RTX 20 AND 30 CARDS." },
    // Frame rate and screen.
    Item { var: "VICTORY_MENU_FPS", label: "FRAME RATE CAP", values: &[("1000", "NONE"), ("120", "120"), ("60", "60"), ("30", "30 (THE GAME)")], live: false,
        about: "THE MOST FRAMES A SECOND THE GAME DRAWS. 60 KEEPS IT STEADY, NONE LETS IT GO AS HIGH AS IT CAN." },
    Item { var: "VICTORY_FPS_LINE", label: "FPS AT THE TOP", values: &[("1", "ON"), ("0", "OFF")], live: true,
        about: "A SMALL FRAMES-A-SECOND COUNTER IN THE TOP LEFT CORNER. CHANGES AT ONCE." },
    Item { var: "VICTORY_PANEL", label: "STATS PANEL (F3)", values: &[("0", "OFF"), ("1", "ON")], live: true,
        about: "THE F3 PANEL: FRAME TIME, MEMORY, VIDEO MEMORY AND WHAT THE EMULATOR IS DOING. CHANGES AT ONCE." },
    Item { var: "VICTORY_FULLSCREEN", label: "FULLSCREEN (NO BORDER)", values: UNSET_ON, live: false,
        about: "THE WINDOW COVERS THE WHOLE SCREEN, WITHOUT A BORDER." },
    Item { var: "VICTORY_MOUSE_CAPTURE", label: "MOUSE CAPTURE", values: UNSET_ON, live: false,
        about: "KEEPS THE MOUSE INSIDE THE GAME WINDOW WHILE YOU PLAY." },
    Item { var: "VICTORY_IME_ENGLISH", label: "ENGLISH KEYBOARD", values: UNSET_ON, live: false,
        about: "READS KEYS AS AN ENGLISH (US) KEYBOARD, FOR LAYOUTS WHERE KEYS LAND IN THE WRONG PLACE." },
    // Console.
    Item { var: "VICTORY_MENU_CONSOLE", label: "CONSOLE", values: &[("series-x", "SERIES X"), ("series-s", "SERIES S (FASTER)")], live: false,
        about: "SERIES S DRAWS LESS AND RUNS FASTER, SERIES X LOOKS SHARPER." },
    Item { var: "VICTORY_MENU_RES", label: "SERIES X PICTURE", values: &[("1440", "2560X1440"), ("1080to1440", "1080P TO 1440P"), ("4k", "3840X2160")], live: false,
        about: "THE PICTURE SIZE WITH THE SERIES X PROFILE." },
    // Picture.
    Item { var: "VICTORY_AMBIENT", label: "LIGHT ESTIMATE", values: &[("0.8", "0.8"), ("1", "1"), ("0.5", "0.5"), ("", "OFF")], live: false,
        about: "EXTRA LIGHT IN PLACE OF THE RAY TRACED BOUNCE LIGHT. HIGHER IS BRIGHTER, OFF LEAVES DARK ROOMS DARK." },
    Item { var: "VICTORY_AMBIENT_SPECULAR", label: "REFLECTION ESTIMATE", values: ON_UNSET, live: false,
        about: "A STAND-IN FOR RAY TRACED REFLECTIONS ON SHINY SURFACES." },
    Item { var: "VICTORY_AMBIENT_AIR", label: "FOG CAP", values: &[("1.5", "1.5"), ("1", "1"), ("2", "2"), ("", "NONE")], live: false,
        about: "HOW THICK THE FOG AND THE LIGHT IN THE AIR MAY GET. LOWER IS CLEARER." },
    Item { var: "VICTORY_BVH_BUILDER", label: "RAY TRACING (SLOW, BROKEN)", values: UNSET_ON, live: false,
        about: "THE GAME RAY TRACING. VERY SLOW AND STILL BROKEN: FOR TESTS ONLY." },
    // On unless "0": a file saved by an earlier version holds an empty value (its OFF) or "1",
    // and both start it on, so nobody keeps it off from before by accident (2026-11-27).
    Item { var: "VICTORY_WATER_LIGHT", label: "WATER LIGHT", values: &[("", "ON"), ("0", "OFF")], live: false,
        about: "THE LIGHT ON OPEN WATER. TURN IT OFF ONLY IF THE WATER SHOWS DARK SQUARES." },
    Item { var: "VICTORY_LEAVE_OUT", label: "HEAT HAZE AND DECAL DEFRAG", values: &[("02384a60fda6f2d0,731a39171b568298", "LEFT OUT"), ("", "RUN (SLOWER, SMEARS)")], live: false,
        about: "THE HEAT HAZE AND DECAL DEFRAG PASSES. LEFT OUT IS FASTER, RUN THEM ONLY TO COMPARE." },
    // Advanced: the layer's switches, for tests.
    Item { var: "VICTORY_EARLY_DEPTH", label: "EARLY DEPTH TEST", values: ON_UNSET, live: false,
        about: "TESTS DEPTH BEFORE SHADING. A CHANGE REBUILDS EVERY SHADER AT THE NEXT START: A SLOW FIRST RUN." },
    Item { var: "VICTORY_WC_NO_BACK", label: "SKIP BIG COPY-BACKS", values: ON_UNSET, live: false,
        about: "BIG BUFFERS THE CARD WRITES ARE NOT COPIED BACK. FASTER, OFF IF SOMETHING LOOKS WRONG." },
    Item { var: "VICTORY_SOFT_VIEWS", label: "SOFT VIEWS (NO CUT FREEZES)", values: &[("copies", "ON"), ("", "OFF")], live: false,
        about: "MEMORY VIEWS KEPT AS COPIES, AGAINST SHORT FREEZES IN CUTSCENES." },
    Item { var: "VICTORY_TEXTURE_NO_WAIT", label: "TEXTURE NO WAIT", values: ON_UNSET, live: false,
        about: "TEXTURES ARE READ WITHOUT WAITING FOR THE GAME TO FINISH THEM. OFF FIXES RAINBOW FACES, COSTS FRAMES." },
    Item { var: "VICTORY_RELAY", label: "RELAY (SPEED, TRIANGLES)", values: STAGES, live: false,
        about: "HOW BUFFER COPIES REACH THE CARD. 2 IS FASTEST, 1 OR 0 MAY FIX STRETCHED BODIES, SLOWER." },
    Item { var: "VICTORY_CARD_STEPS", label: "CARD STEPS", values: STAGES, live: false,
        about: "WORK DONE BY THE CARD INSTEAD OF THE PROCESSOR. 0 MAY FIX STRETCHED BODIES, SLOWER." },
    Item { var: "VICTORY_INDIRECT_BATCH", label: "INDIRECT BATCH", values: STAGES, live: false,
        about: "HOW MANY INDIRECT DRAWS ARE PREPARED TOGETHER. FOR TESTS." },
    Item { var: "VICTORY_CARD_BUFFERS_MIN_KB", label: "CARD COMPARES FROM (KB)", values: &[("64", "64"), ("1024", "1024")], live: false,
        about: "THE SMALLEST BUFFER THE CARD CHECKS FOR CHANGES. FOR TESTS." },
    Item { var: "VICTORY_PORT_CONSTANTS", label: "CONSTANTS HEAP IN PLACE", values: UNSET_ON, live: false,
        about: "THE CARD READS THE GAME CONSTANTS WHERE THE GAME WRITES THEM. EXPERIMENTAL." },
];
/// The section headers: the setting each comes before, and its title.
const SECTIONS: &[(&str, &str)] = &[
    ("VICTORY_MENU_DLSS", "UPSCALING AND FRAME GENERATION"),
    ("VICTORY_MENU_FPS", "FRAME RATE AND SCREEN"),
    ("VICTORY_MENU_CONSOLE", "CONSOLE"),
    ("VICTORY_AMBIENT", "PICTURE"),
    ("VICTORY_EARLY_DEPTH", "ADVANCED: FOR TESTS"),
];
/// The header before the actions.
const ACTIONS_SECTION: &str = "SAVE, SHARE AND MORE";
/// The lines after the settings.
const ACTIONS: [&str; 7] = [
    "SAVE AND RESTART NOW",
    "SAVE FOR NEXT START",
    "PRELOAD SHADERS (RESTARTS THE GAME)",
    "RESET ALL TO PLAY DEFAULTS",
    "EXPORT SAVE (TO SAVES IN THE GAME FOLDER)",
    "IMPORT SAVE (PICK FROM SAVES)",
    "CLOSE",
];
/// What the actions without lines of their own (`ABOUT_PRELOAD` and the save ones) do.
const ABOUT_ACTIONS: [&str; 7] = [
    "SAVES THE CHANGES AND STARTS THE GAME AGAIN WITH THEM.",
    "SAVES THE CHANGES FOR THE NEXT TIME THE GAME STARTS.",
    "",
    "FORGETS EVERY CHANGE MADE HERE: THE DEFAULTS COME BACK AT THE NEXT START.",
    "",
    "",
    "CLOSES THIS MENU. F1 OPENS IT AGAIN.",
];
/// The line under the settings while nothing was said.
const FOOTER: &str = "* = CHANGED: APPLIES AT THE NEXT START UNLESS LIVE";
/// Said of a launcher's choice ([`locked`]) when it is picked or changed.
const LOCKED: &str = "SET IN THE LAUNCHER: CHANGE IT ON ITS PLAY PAGE";

/// Started by Julia's Launcher (`victory run`): no `VICTORY_MENU_PLAY` to start again with, and
/// the code to end with for the launcher to start the game again (`VICTORY_RESTART_EXIT`).
fn by_launcher() -> bool {
    std::env::var_os("VICTORY_MENU_PLAY").is_none() && get("VICTORY_RESTART_EXIT").parse::<u32>().is_ok_and(|c| c > 1)
}

/// A choice the launcher owns on its Play page (the frame generation engine, the frame rate
/// cap, MFG Unlocker): shown as it set it, not changed here. DLSS is changed here and the
/// launcher applies it at the next PLAY; the frame generation multiplier here while frame
/// generation runs (live, and the launcher keeps it), else the launcher's engine decides.
/// Locked all together, changes made in F1 were refused while the start notice said "F1 MENU:
/// 2X-6X", and players thought nothing saved (28/11/2026).
fn locked(item: &Item) -> bool {
    by_launcher()
        && (matches!(item.var, "VICTORY_MENU_FG_TYPE" | "VICTORY_MENU_FPS")
            || item.var == MFG
            || item.var == "VICTORY_MENU_FRAMEGEN" && !crate::streamline::framegen_live())
}

/// A line that does not apply to this start: the launcher always plays the Series S profile.
fn hidden(item: &Item) -> bool {
    by_launcher() && matches!(item.var, "VICTORY_MENU_CONSOLE" | "VICTORY_MENU_RES")
}

/// A line of the box: its letters, the colour of each, and the colour behind them.
#[derive(Clone, Debug)]
struct Line {
    text: String,
    fg: Vec<Tone>,
    bg: Tone,
}

impl Line {
    fn new(bg: Tone) -> Line {
        Line { text: String::new(), fg: Vec::new(), bg }
    }

    fn plain(text: &str, fg: Tone, bg: Tone) -> Line {
        let mut line = Line::new(bg);
        line.push(text, fg);
        line
    }

    /// Adds `text` in `tone`, as much of it as fits the box.
    fn push(&mut self, text: &str, tone: Tone) {
        for c in text.chars() {
            if self.text.len() >= COLUMNS as usize {
                break;
            }
            self.text.push(c);
            self.fg.push(tone);
        }
    }
}

/// The box's colours.
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
#[repr(u8)]
enum Tone {
    Back,
    Black,
    White,
    Border,
    TitleBar,
    Title,
    Muted,
    Section,
    Rule,
    Label,
    Value,
    Bar,
    Picked,
    Cursor,
    Live,
    Changed,
    Locked,
    Note,
    Link,
}

/// Every tone, in the order of its number.
const TONES: [Tone; 19] = [
    Tone::Back,
    Tone::Black,
    Tone::White,
    Tone::Border,
    Tone::TitleBar,
    Tone::Title,
    Tone::Muted,
    Tone::Section,
    Tone::Rule,
    Tone::Label,
    Tone::Value,
    Tone::Bar,
    Tone::Picked,
    Tone::Cursor,
    Tone::Live,
    Tone::Changed,
    Tone::Locked,
    Tone::Note,
    Tone::Link,
];

impl Tone {
    /// sRGB.
    const fn rgb(self) -> [u8; 3] {
        match self {
            Tone::Back => [16, 17, 26],
            Tone::Black => [0, 0, 0],
            Tone::White | Tone::Title | Tone::Picked => [255, 255, 255],
            Tone::Border => [150, 80, 190],
            Tone::TitleBar => [60, 30, 78],
            Tone::Muted => [138, 142, 164],
            Tone::Section | Tone::Cursor => [214, 150, 240],
            Tone::Rule => [58, 60, 82],
            Tone::Label => [226, 229, 240],
            Tone::Value => [125, 205, 255],
            Tone::Bar => [44, 48, 72],
            Tone::Live => [110, 220, 150],
            Tone::Changed => [255, 196, 90],
            Tone::Locked => [112, 116, 138],
            Tone::Note => [255, 214, 120],
            Tone::Link => [100, 104, 130],
        }
    }
}

/// A colour's bytes in the swap chain's format (`None`: a format the box is not drawn in).
fn encode(format: DXGI_FORMAT, [r, g, b]: [u8; 3]) -> Option<Vec<u8>> {
    Some(match format {
        DXGI_FORMAT_R8G8B8A8_UNORM => vec![r, g, b, 0xFF],
        DXGI_FORMAT_B8G8R8A8_UNORM => vec![b, g, r, 0xFF],
        DXGI_FORMAT_R10G10B10A2_UNORM => {
            let ten = |v: u8| u32::from(v) * 1023 / 255;
            (ten(r) | ten(g) << 10 | ten(b) << 20 | 3 << 30).to_le_bytes().to_vec()
        }
        // Linear light in halves.
        DXGI_FORMAT_R16G16B16A16_FLOAT => [linear(r), linear(g), linear(b), 1.0].into_iter().flat_map(|v| half(v).to_le_bytes()).collect(),
        _ => return None,
    })
}

/// An sRGB byte as linear light.
fn linear(v: u8) -> f32 {
    let c = f32::from(v) / 255.0;
    if c <= 0.04045 { c / 12.92 } else { ((c + 0.055) / 1.055).powf(2.4) }
}

/// A value from 0 to 1 as a half (smaller than a half's least normal: 0).
fn half(v: f32) -> u16 {
    if v <= 0.0 {
        return 0;
    }
    let bits = v.to_bits();
    let exponent = ((bits >> 23) & 0xFF) as i32 - 127 + 15;
    if exponent <= 0 {
        return 0;
    }
    ((exponent as u32) << 10 | (bits >> 13) & 0x3FF) as u16
}

/// What a line of the list is: a section's header, or a setting or action (`Menu::at`'s numbers).
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
enum Entry {
    /// A blank before a section's header.
    Gap,
    Section(&'static str),
    Line(usize),
}

/// The list as shown: the settings that apply, each section's header before its first, then
/// the actions under their own header.
fn entries() -> Vec<Entry> {
    let mut out = Vec::new();
    for (i, item) in ITEMS.iter().enumerate() {
        if hidden(item) {
            continue;
        }
        if let Some((_, title)) = SECTIONS.iter().find(|(var, _)| *var == item.var) {
            if !out.is_empty() {
                out.push(Entry::Gap);
            }
            out.push(Entry::Section(title));
        }
        out.push(Entry::Line(i));
    }
    out.push(Entry::Gap);
    out.push(Entry::Section(ACTIONS_SECTION));
    out.extend((0..ACTIONS.len()).map(|a| Entry::Line(ITEMS.len() + a)));
    out
}

/// `text` in up to three lines of the box, broken between words.
fn wrapped(text: &str) -> [String; 3] {
    let mut out: [String; 3] = Default::default();
    let mut n = 0;
    for word in text.split_whitespace() {
        if !out[n].is_empty() && out[n].len() + 1 + word.len() > COLUMNS as usize {
            n += 1;
            if n == out.len() {
                break;
            }
        }
        if !out[n].is_empty() {
            out[n].push(' ');
        }
        out[n].push_str(word);
    }
    out
}

/// A title bar: `left` at its start, `right` at its end.
fn title_line(left: &str, right: &str) -> Line {
    let gap = (COLUMNS as usize).saturating_sub(left.len() + right.len() + 2);
    Line::plain(&format!(" {left}{}{right} ", " ".repeat(gap)), Tone::Title, Tone::TitleBar)
}

/// A section's header: its title, then a rule to the edge.
fn section_line(title: &str) -> Line {
    let mut line = Line::new(Tone::Back);
    line.push(&format!("  {title} "), Tone::Section);
    let rest = (COLUMNS as usize).saturating_sub(line.text.len() + 1);
    line.push(&"-".repeat(rest), Tone::Rule);
    line
}

fn get(var: &str) -> String {
    std::env::var(var).unwrap_or_default()
}

/// Where the menu saves: the file `VICTORY_MENU_FILE` names (`victory play` sets it); else,
/// however the game was started (a script, another launcher), the one `victory play` reads at
/// every start, `victory-settings.txt` in `VICTORY_HOME` or `%LOCALAPPDATA%\victory`.
fn settings_file() -> Option<std::path::PathBuf> {
    let set = |name: &str| std::env::var_os(name).filter(|v| !v.is_empty()).map(std::path::PathBuf::from);
    set("VICTORY_MENU_FILE")
        .or_else(|| set("VICTORY_HOME").map(|home| home.join("victory-settings.txt")))
        .or_else(|| set("LOCALAPPDATA").map(|base| base.join("victory").join("victory-settings.txt")))
}

/// The first start of a new victory.exe: the seconds it plays before it starts
/// again by itself (`VICTORY_WARMUP_RESTART`, set by the launcher while the exe
/// has no mark under `%LOCALAPPDATA%\victory\warmed`). On the RTX 5070, every
/// new build's first run stayed at a flat 14 fps (Present ~40 ms) for as long as
/// it ran, and every later run was normal (27-29 fps), even after a first run of
/// 45 seconds; a copy of an exe already run was fast at once (2026-11-27). Likely
/// the driver's cache of the program; not ours to fix, so it is run through once.
/// Draws in a frame from which the warm-up's time counts: the game's scenes
/// (loading into the world: about 300, in the world: thousands), not its
/// title screen and menus (about 10).
const SCENE_DRAWS: u64 = 100;

pub(crate) fn warmup_seconds() -> Option<u64> {
    get("VICTORY_WARMUP_RESTART").parse().ok().filter(|s| *s > 0)
}

/// The start's notice lines for the warm-up start ([`warmup_seconds`]).
pub(crate) fn warmup_notice(seconds: u64) -> Vec<String> {
    vec![
        "FIRST START OF THIS VERSION: A ONE-TIME WARM-UP".to_owned(),
        format!("THE GAME RESTARTS BY ITSELF {seconds} SECONDS AFTER IT"),
        "LOADS INTO THE GAME (PRESS A AT THE TITLE SCREEN)".to_owned(),
        "THIS HAPPENS ONCE PER NEW VERSION, AND IS NEEDED:".to_owned(),
        "WITHOUT IT THE FIRST RUN STAYS AT ABOUT 14 FPS".to_owned(),
        String::new(),
    ]
}

/// Starts `command_line` in a console window of its own, with that console's input and output.
/// (`std::process::Command` always hands its child this process's standard handles: the restarted
/// launcher then wrote into the old launcher's pipe and log, not its own window, and kept the old
/// launcher waiting on that pipe.)
fn new_console(command_line: &str) -> std::io::Result<()> {
    use windows_sys::Win32::Foundation::CloseHandle;
    use windows_sys::Win32::System::Threading::{CREATE_NEW_CONSOLE, CreateProcessW, PROCESS_INFORMATION, STARTUPINFOW};
    let mut line: Vec<u16> = command_line.encode_utf16().chain(Some(0)).collect();
    // SAFETY: plain structs, zero is their empty value.
    let mut startup: STARTUPINFOW = unsafe { std::mem::zeroed() };
    startup.cb = std::mem::size_of::<STARTUPINFOW>() as u32;
    let mut process: PROCESS_INFORMATION = unsafe { std::mem::zeroed() };
    // SAFETY: `line` is a writable, terminated UTF-16 string; no handles inherited; this
    // process's environment and folder.
    let made = unsafe {
        CreateProcessW(
            std::ptr::null(),
            line.as_mut_ptr(),
            std::ptr::null(),
            std::ptr::null(),
            0,
            CREATE_NEW_CONSOLE,
            std::ptr::null(),
            std::ptr::null(),
            &startup,
            &mut process,
        )
    };
    if made == 0 {
        return Err(std::io::Error::last_os_error());
    }
    // SAFETY: the two handles CreateProcessW gave, closed once.
    unsafe {
        CloseHandle(process.hThread);
        CloseHandle(process.hProcess);
    }
    Ok(())
}

/// Whether a change of `item` shows at once. Frame generation does when this run started with
/// it ready (the launcher asks for that whenever Streamline is there).
fn live(item: &Item) -> bool {
    item.live || (item.var == "VICTORY_MENU_FRAMEGEN" && crate::streamline::framegen_live())
}

/// Whether the frame-rate line is shown at the top while the menu is closed
/// (`VICTORY_FPS_LINE=0`: not; the menu changes it).
fn fps_line_on() -> &'static AtomicBool {
    static ON: OnceLock<AtomicBool> = OnceLock::new();
    ON.get_or_init(|| AtomicBool::new(std::env::var_os("VICTORY_FPS_LINE").is_none_or(|v| v != "0")))
}

/// That line: the frames rendered a second and, while frame generation shows more, those shown.
fn fps_line() -> Option<String> {
    if !fps_line_on().load(Ordering::Relaxed) {
        return None;
    }
    // The stats panel has that corner (F3), and says the same.
    if super::panel::shown().load(Ordering::Relaxed) {
        return None;
    }
    let fps = f64::from(super::METER_FPS_TENTHS.load(Ordering::Relaxed)) / 10.0;
    if fps <= 0.0 {
        return None;
    }
    Some(match crate::streamline::shown_per_frame() {
        1 => format!("FPS: {fps:.1}"),
        per => format!("FPS: {fps:.1}  FG: {:.1}", fps * f64::from(per)),
    })
}

/// The index of `value` among the item's values (an unknown value: shown as
/// itself, at the end).
fn index_of(item: &Item, value: &str) -> Option<usize> {
    item.values.iter().position(|(v, _)| *v == value)
}

/// What the menu shows for `value`: its label, an unknown value as itself,
/// and a value the launcher did not set as "DEFAULT" (the build's own) rather than a blank.
fn shown_value(item: &Item, value: &str) -> String {
    match index_of(item, value) {
        Some(i) => item.values[i].1.to_owned(),
        None if value.is_empty() => "DEFAULT".to_owned(),
        // The launcher's frame generation outside the emulator, or its own (shown, not picked here).
        None if item.var == "VICTORY_MENU_FG_TYPE" => match value {
            "smooth" => "NVIDIA SMOOTH MOTION (DRIVER)".to_owned(),
            "nvsmooth30" => "NVSMOOTH30 (DRIVER)".to_owned(),
            "vi" => "VI 2X".to_owned(),
            "off" => "OFF".to_owned(),
            other => other.to_ascii_uppercase(),
        },
        None => value.to_ascii_uppercase(),
    }
}

/// Keys pressed, gathered on a thread of their own every 10 ms: a frame
/// lasts 50 ms and more, and a short press would fall between two frames.
fn keys() -> &'static Mutex<Vec<u16>> {
    static KEYS: OnceLock<Mutex<Vec<u16>>> = OnceLock::new();
    KEYS.get_or_init(|| {
        let _ = std::thread::Builder::new().name("settings menu keys".into()).spawn(|| {
            const WATCHED: [u16; 7] = [VK_F1, VK_UP, VK_DOWN, VK_LEFT, VK_RIGHT, VK_RETURN, VK_ESCAPE];
            let mut was = [false; WATCHED.len()];
            loop {
                std::thread::sleep(Duration::from_millis(10));
                let mut process = 0;
                // SAFETY: plain queries; room for the answer.
                unsafe { GetWindowThreadProcessId(GetForegroundWindow(), &mut process) };
                let ours = process == std::process::id();
                for (i, &vk) in WATCHED.iter().enumerate() {
                    // SAFETY: a plain query.
                    let down = ours && unsafe { GetAsyncKeyState(i32::from(vk)) } < 0;
                    if down && !was[i] {
                        keys().lock().unwrap_or_else(|e| e.into_inner()).push(vk);
                    }
                    was[i] = down;
                }
            }
        });
        Mutex::new(Vec::new())
    })
}

/// The box's size in pixels.
const fn size(scale: u32) -> (u32, u32) {
    ((COLUMNS * CELL.0 + 2 * RIM) * scale, (LINES * CELL.1 + 2 * RIM) * scale)
}

/// The tone of the box's pixel (`x`, `y`) of `w` by `h` (unscaled). The menu has a frame and its
/// title bar reaches the top; a single line (the frame-rate line) has neither.
fn tone_at(lines: &[Line], x: u32, y: u32, (w, h): (u32, u32)) -> Tone {
    let boxed = lines.len() > 1;
    if boxed && (x == 0 || y == 0 || x + 1 == w || y + 1 == h) {
        return Tone::Border;
    }
    let row = y.checked_sub(RIM).map(|v| (v / CELL.1) as usize);
    let Some(line) = row.map_or(lines.first(), |r| lines.get(r)) else {
        return if boxed { Tone::Back } else { Tone::Black };
    };
    let (Some(cx), Some(cy)) = (x.checked_sub(RIM), y.checked_sub(RIM)) else {
        return line.bg;
    };
    if cx % CELL.0 >= GLYPH.0 || cy % CELL.1 >= GLYPH.1 || cx / CELL.0 >= COLUMNS {
        return line.bg;
    }
    let column = (cx / CELL.0) as usize;
    let Some(&c) = line.text.as_bytes().get(column) else {
        return line.bg;
    };
    if (glyph(c.to_ascii_uppercase())[(cy % CELL.1) as usize] >> (GLYPH.0 - 1 - cx % CELL.0)) & 1 != 0 {
        line.fg.get(column).copied().unwrap_or(Tone::White)
    } else {
        line.bg
    }
}

/// The box's picture in `format` (`None`: a format it is not drawn in).
fn painted(lines: &[Line], scale: u32, format: DXGI_FORMAT, pitch: usize) -> Option<Vec<u8>> {
    let palette = TONES.iter().map(|t| encode(format, t.rgb())).collect::<Option<Vec<_>>>()?;
    let (width, height) = size(scale);
    let (w, h) = (width / scale, height / scale);
    let bytes = palette[0].len();
    let mut out = vec![0; pitch * height as usize];
    for y in 0..h {
        let top = pitch * (y * scale) as usize;
        for x in 0..w {
            let colour = &palette[tone_at(lines, x, y, (w, h)) as usize];
            for i in 0..scale {
                let at = top + (x * scale + i) as usize * bytes;
                out[at..at + bytes].copy_from_slice(colour);
            }
        }
        for i in 1..scale as usize {
            out.copy_within(top..top + pitch, top + i * pitch);
        }
    }
    Some(out)
}

struct Buffers {
    format: DXGI_FORMAT,
    scale: u32,
    pitch: u32,
    uploads: Vec<(ID3D12Resource, *mut u8)>,
    card: ID3D12Resource,
    turn: usize,
    fresh: bool,
    /// The card's buffer was copied into before (it is in the copy-source state;
    /// at first it is in the common state, promoted by the first copy).
    used: bool,
}

// SAFETY: free-threaded resources, and memory only written under the presenter's lock.
unsafe impl Send for Buffers {}

impl Buffers {
    fn new(host: &Host, format: DXGI_FORMAT, scale: u32) -> Option<Buffers> {
        let (width, height) = size(scale);
        let pitch = (width * encode(format, [0; 3])?.len() as u32).next_multiple_of(256);
        let bytes = u64::from(pitch) * u64::from(height);
        let uploads = (0..UPLOADS).map(|_| host.upload_buffer(bytes)).collect::<Option<Vec<_>>>()?;
        let heap = D3D12_HEAP_PROPERTIES { Type: D3D12_HEAP_TYPE_DEFAULT, ..Default::default() };
        let desc = D3D12_RESOURCE_DESC {
            Dimension: D3D12_RESOURCE_DIMENSION_BUFFER,
            Width: bytes,
            Height: 1,
            DepthOrArraySize: 1,
            MipLevels: 1,
            SampleDesc: DXGI_SAMPLE_DESC { Count: 1, Quality: 0 },
            Layout: D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
            ..Default::default()
        };
        let mut card: Option<ID3D12Resource> = None;
        // SAFETY: plain creation of a buffer.
        unsafe {
            host.device
                .CreateCommittedResource(&heap, D3D12_HEAP_FLAG_NONE, &desc, D3D12_RESOURCE_STATE_COMMON, None, &mut card)
                .ok()?;
        }
        Some(Buffers { format, scale, pitch, uploads, card: card?, turn: 0, fresh: false, used: false })
    }

    fn write(&mut self, lines: &[Line]) {
        let Some(picture) = painted(lines, self.scale, self.format, self.pitch as usize) else { return };
        self.turn = (self.turn + 1) % UPLOADS;
        // SAFETY: a mapped upload buffer of the picture's size, which the card read frames ago.
        unsafe { std::ptr::copy_nonoverlapping(picture.as_ptr(), self.uploads[self.turn].1, picture.len()) };
        self.fresh = true;
    }
}

/// The menu of a presenter.
pub(super) struct Menu {
    /// The values chosen, by item (as at start until changed).
    values: Vec<String>,
    /// The values at start.
    started: Vec<String>,
    /// The line picked: an item, or an action after them.
    at: usize,
    note: String,
    changed: bool,
    /// What the buffer holds: the frame-rate line, or (none) the menu's lines.
    painted: Option<String>,
    buffers: Option<Buffers>,
    failed: bool,
    /// The saves in `Saves` that can be imported (read when the menu opens), and the line
    /// picked in their list (`imports.len()`: the line back), shown while `picking`.
    imports: Vec<String>,
    import_at: usize,
    picking: bool,
}

impl Menu {
    pub(super) fn new() -> Menu {
        // Start the key thread now: F1 must be seen while the menu is closed.
        let _ = keys();
        // `VICTORY_MENU_OPEN=1`: shown from the start (to look at it in a run nobody presses keys in).
        if std::env::var_os("VICTORY_MENU_OPEN").is_some_and(|v| v == "1") {
            crate::SETTINGS_MENU_OPEN.store(true, Ordering::Relaxed);
        }
        let values: Vec<String> = ITEMS.iter().map(|i| get(i.var)).collect();
        Menu { started: values.clone(), values, at: 0, note: String::new(), changed: true, painted: None, buffers: None, failed: false, imports: Vec::new(), import_at: 0, picking: false }
    }

    fn open() -> bool {
        crate::SETTINGS_MENU_OPEN.load(Ordering::Relaxed)
    }

    /// The box's lines as text (what the tests read).
    #[cfg(test)]
    fn lines(&self) -> Vec<String> {
        self.rows().into_iter().map(|l| l.text).collect()
    }

    /// The box's lines: a title bar, a help line, a blank, [`ROWS`] of the list (sections,
    /// settings, actions) around the line picked, a blank, what the line picked does (three
    /// lines), a note and the repository's link.
    fn rows(&self) -> Vec<Line> {
        if self.picking {
            return self.pick_rows();
        }
        let mut out = vec![
            title_line("SETTINGS", "F1 OR ESC CLOSES"),
            Line::plain(" UP/DOWN PICK    LEFT/RIGHT CHANGE    ENTER DOES IT", Tone::Muted, Tone::Back),
            Line::new(Tone::Back),
        ];
        let list = entries();
        let at = list.iter().position(|e| *e == Entry::Line(self.at)).unwrap_or(0);
        let first = at.saturating_sub(ROWS / 2).min(list.len().saturating_sub(ROWS));
        for entry in &list[first..(first + ROWS).min(list.len())] {
            out.push(match *entry {
                Entry::Gap => Line::new(Tone::Back),
                Entry::Section(title) => section_line(title),
                Entry::Line(row) => self.row(row),
            });
        }
        while out.len() < 3 + ROWS {
            out.push(Line::new(Tone::Back));
        }
        out.push(Line::new(Tone::Back));
        out.extend(self.about().iter().map(|l| Line::plain(l, Tone::Muted, Tone::Back)));
        out.push(if self.note.is_empty() { Line::plain(FOOTER, Tone::Muted, Tone::Back) } else { Line::plain(&self.note, Tone::Note, Tone::Back) });
        out.push(Line::plain(REPO, Tone::Link, Tone::Back));
        out
    }

    /// A setting's or an action's line (`row`: `Menu::at`'s numbers).
    fn row(&self, row: usize) -> Line {
        let picked = row == self.at;
        let mut line = Line::new(if picked { Tone::Bar } else { Tone::Back });
        line.push(if picked { "> " } else { "  " }, Tone::Cursor);
        let name = if picked { Tone::Picked } else { Tone::Label };
        let Some(item) = ITEMS.get(row) else {
            line.push(ACTIONS[row - ITEMS.len()], name);
            return line;
        };
        line.push(&format!("{:<30}", item.label), name);
        let value = &self.values[row];
        let shown = shown_value(item, value);
        if locked(item) {
            line.push(&format!("  {shown}  LAUNCHER"), Tone::Locked);
            return line;
        }
        line.push("< ", Tone::Muted);
        line.push(&shown, Tone::Value);
        line.push(" >", Tone::Muted);
        if *value != self.started[row] {
            line.push(" *", Tone::Changed);
        }
        if live(item) {
            line.push(" LIVE", Tone::Live);
        }
        line
    }

    /// What the line picked does, in three lines.
    fn about(&self) -> [String; 3] {
        let fixed = |lines: [&str; 3]| lines.map(str::to_owned);
        if let Some(action) = self.at.checked_sub(ITEMS.len()) {
            return match action {
                PRELOAD => fixed(ABOUT_PRELOAD),
                EXPORT => fixed(ABOUT_EXPORT),
                IMPORT => fixed(ABOUT_IMPORT),
                other => wrapped(ABOUT_ACTIONS.get(other).copied().unwrap_or("")),
            };
        }
        let item = &ITEMS[self.at];
        if locked(item) {
            let mut out = wrapped(item.about);
            out[2] = LOCKED.to_owned();
            return out;
        }
        if item.var == MFG {
            return fixed(ABOUT_MFG);
        }
        if item.var == DLSS
            && !matches!(self.values[self.at].as_str(), "" | "off")
            && std::env::var_os("VICTORY_STREAMLINE_DIR").is_none()
        {
            return fixed(ABOUT_NO_DLSS);
        }
        wrapped(item.about)
    }

    /// The list IMPORT SAVE opens, in the box's lines.
    fn pick_rows(&self) -> Vec<Line> {
        let mut out = vec![
            Line::plain(&format!(" {PICK_TITLE}"), Tone::Title, Tone::TitleBar),
            Line::plain(PICK_HELP, Tone::Muted, Tone::Back),
            Line::new(Tone::Back),
        ];
        if self.imports.is_empty() {
            out.push(Line::plain(PICK_NONE, Tone::Note, Tone::Back));
        }
        let rows = ROWS - (out.len() - 3);
        let total = self.imports.len() + 1;
        let first = self.import_at.saturating_sub(rows / 2).min(total.saturating_sub(rows));
        for row in first..(first + rows).min(total) {
            let picked = row == self.import_at;
            let name = self.imports.get(row).map_or_else(|| PICK_BACK.to_owned(), |n| shown_name(n));
            let mut line = Line::new(if picked { Tone::Bar } else { Tone::Back });
            line.push(if picked { "> " } else { "  " }, Tone::Cursor);
            line.push(&name, if picked { Tone::Picked } else { Tone::Label });
            out.push(line);
        }
        while out.len() < 3 + ROWS {
            out.push(Line::new(Tone::Back));
        }
        out.push(Line::new(Tone::Back));
        out.extend(ABOUT_PICK.iter().map(|l| Line::plain(l, Tone::Muted, Tone::Back)));
        let note = if self.note.is_empty() { format!("{} IN SAVES", self.imports.len()) } else { self.note.clone() };
        out.push(Line::plain(&note, Tone::Note, Tone::Back));
        out.push(Line::plain(REPO, Tone::Link, Tone::Back));
        out
    }

    /// Moves the line picked by `by` (+1 / -1), over the lines left out ([`hidden`]).
    fn step(&mut self, by: isize) {
        let total = (ITEMS.len() + ACTIONS.len()) as isize;
        for _ in 0..total {
            self.at = (self.at as isize + by).rem_euclid(total) as usize;
            if !ITEMS.get(self.at).is_some_and(hidden) {
                break;
            }
        }
    }

    /// Changes item `i` by `step` (+1 / -1) through its values.
    fn change(&mut self, i: usize, step: isize) {
        let item = &ITEMS[i];
        let n = item.values.len() as isize;
        let now = index_of(item, &self.values[i]).map_or(-1, |x| x as isize);
        let next = if now < 0 { if step > 0 { 0 } else { n - 1 } } else { (now + step).rem_euclid(n) };
        self.values[i] = item.values[next as usize].0.to_owned();
        if live(item) {
            self.apply_live(i);
        }
        self.note.clear();
    }

    /// A setting the build reads at each use: changed in this run's environment at once.
    fn apply_live(&self, i: usize) {
        let (var, value) = (ITEMS[i].var, &self.values[i]);
        // SAFETY: the variables changed here are read by this build with
        // std::env::var at each use; Windows' environment block is locked by
        // the process heap's routines, and std holds its own lock.
        unsafe {
            if value.is_empty() {
                std::env::remove_var(var);
            } else {
                std::env::set_var(var, value);
            }
        }
        match var {
            // Off (0) or the frames shown for each one rendered, from the next present on.
            "VICTORY_MENU_FRAMEGEN" => crate::streamline::set_framegen(value.parse().unwrap_or(0)),
            "VICTORY_FPS_LINE" => fps_line_on().store(value != "0", Ordering::Relaxed),
            "VICTORY_PANEL" => super::panel::shown().store(value == "1", Ordering::Relaxed),
            _ => {}
        }
    }

    /// Writes the changed values over those in the settings file.
    fn save(&mut self) -> bool {
        let Some(path) = settings_file() else {
            self.note = "NO FOLDER FOR THE SETTINGS FILE".to_owned();
            return false;
        };
        if let Some(dir) = path.parent() {
            let _ = std::fs::create_dir_all(dir);
        }
        let mut kept: Vec<(String, String)> = std::fs::read_to_string(&path)
            .unwrap_or_default()
            .lines()
            .filter_map(|l| l.split_once('=').map(|(k, v)| (k.trim().to_owned(), v.trim().to_owned())))
            .filter(|(k, _)| k.starts_with("VICTORY_"))
            .collect();
        for (i, item) in ITEMS.iter().enumerate() {
            if self.values[i] == self.started[i] {
                continue;
            }
            kept.retain(|(k, _)| k != item.var);
            kept.push((item.var.to_owned(), self.values[i].clone()));
        }
        let mut text = String::from("# Saved by the F1 menu; applied over the play settings at every start (an empty value: unset).\r\n");
        for (k, v) in &kept {
            text.push_str(&format!("{k}={v}\r\n"));
        }
        match std::fs::write(&path, text) {
            Ok(()) => {
                tracing::info!(path = %path.display(), "settings menu: saved");
                self.started = self.values.clone();
                self.note = if std::env::var_os("VICTORY_MENU_FILE").is_some() {
                    "SAVED. APPLIES AT THE NEXT START"
                } else if by_launcher() {
                    // Julia's Launcher applies the file's switches at its next PLAY.
                    "SAVED. APPLIES AT THE NEXT PLAY"
                } else {
                    // Started some other way (a script, another launcher): the file `victory play`
                    // reads at every start, which this start did not.
                    "SAVED. APPLIES WHEN VICTORY.EXE STARTS THE GAME"
                }
                .to_owned();
                true
            }
            Err(e) => {
                tracing::warn!(error = %e, "settings menu: not saved");
                self.note = "COULD NOT SAVE THE SETTINGS FILE".to_owned();
                false
            }
        }
    }

    /// Starts the launcher again in a new console, a few seconds on, and ends this run.
    /// `preload`: the launcher makes every shader first (`VICTORY_PRELOAD=1`, for that start alone).
    fn restart(&mut self, preload: bool) {
        let Some(play) = std::env::var_os("VICTORY_MENU_PLAY") else {
            // Julia's Launcher (`victory run`) starts the game again itself: this process ends
            // with the code it named (`VICTORY_RESTART_EXIT`), and the launcher, which watches
            // it, starts it once more with its own settings and log.
            if let Some(code) = get("VICTORY_RESTART_EXIT").parse::<u32>().ok().filter(|c| *c > 1) {
                tracing::info!(code, preload, "settings menu: ending for the launcher to start the game again");
                // As below: ended at once, without the DLLs' unloading.
                // SAFETY: this process, with nothing of its own left to save.
                unsafe {
                    use windows_sys::Win32::System::Threading::{GetCurrentProcess, TerminateProcess};
                    TerminateProcess(GetCurrentProcess(), code);
                }
                std::process::exit(code as i32);
            }
            self.note = "SAVED. START THE LAUNCHER AGAIN YOURSELF".to_owned();
            return;
        };
        let play = play.to_string_lossy().replace('\'', "''");
        let args = get("VICTORY_MENU_ARGS");
        // The preload is asked by VICTORY_PRELOAD=1, which both launchers read (victory.exe's is
        // `--preload`, the script's `-Preload`: the script's flag given to victory.exe was refused,
        // and the window closed before it could be read, 2026-11-27). A launcher that fails keeps
        // its window until Enter.
        let command = format!(
            "Start-Sleep 3; {}& '{play}' {args}; if ($LASTEXITCODE) {{ Read-Host 'The start failed (see above). Press Enter to close this window' }}",
            if preload { "$env:VICTORY_PRELOAD='1'; " } else { "" }
        );
        match new_console(&format!("powershell.exe -NoProfile -ExecutionPolicy Bypass -Command \"{}\"", command.replace('"', "\\\""))) {
            Ok(()) => {
                tracing::info!(command, preload, "settings menu: restarting");
                // Ended at once: std::process::exit runs the DLLs' unloading, which with
                // Streamline loaded did not come back (a windowless leftover process).
                // SAFETY: this process, with nothing of its own left to save.
                unsafe {
                    use windows_sys::Win32::System::Threading::{GetCurrentProcess, TerminateProcess};
                    TerminateProcess(GetCurrentProcess(), 0);
                }
                std::process::exit(0);
            }
            Err(e) => {
                tracing::warn!(error = %e, "settings menu: restart failed");
                self.note = "SAVED, BUT THE RESTART FAILED".to_owned();
            }
        }
    }

    /// The warm-up start ([`warmup_seconds`]): once the game has drawn whole
    /// scenes for that long, this exe is marked as warmed up
    /// (`VICTORY_WARMUP_MARK`) and the game starts again. The time counts
    /// from the first frame of [`SCENE_DRAWS`] draws or more, not from the
    /// start: a player who waited on the title screen ("press A", about ten
    /// draws a frame) was restarted after 20 seconds and still had the 14 fps
    /// first run after it (2026-11-27).
    fn warmup(&mut self) {
        static BEGAN: OnceLock<std::time::Instant> = OnceLock::new();
        static DONE: AtomicBool = AtomicBool::new(false);
        static LAST: std::sync::atomic::AtomicU64 = std::sync::atomic::AtomicU64::new(0);
        let Some(seconds) = warmup_seconds() else { return };
        if DONE.load(Ordering::Relaxed) {
            return;
        }
        let ever = crate::list::draws_ever();
        let this_frame = ever.saturating_sub(LAST.swap(ever, Ordering::Relaxed));
        if BEGAN.get().is_none() && this_frame < SCENE_DRAWS {
            return;
        }
        if BEGAN.get_or_init(std::time::Instant::now).elapsed() < Duration::from_secs(seconds) {
            return;
        }
        DONE.store(true, Ordering::Relaxed);
        let mark = std::path::PathBuf::from(get("VICTORY_WARMUP_MARK"));
        if !mark.as_os_str().is_empty() {
            if let Some(dir) = mark.parent() {
                let _ = std::fs::create_dir_all(dir);
            }
            let _ = std::fs::write(&mark, "this victory.exe was warmed up (its first start, then a restart)\r\n");
        }
        tracing::info!(seconds, mark = %mark.display(), "first start of this victory.exe: warmed up; the game starts again");
        self.restart(false);
    }

    /// Reads which saves `Saves` holds, keeping the one picked if it is still there.
    fn read_imports(&mut self) {
        let picked = self.imports.get(self.import_at).cloned();
        self.imports = crate::save_share::share().map(|s| crate::save_share::exports(&s)).unwrap_or_default();
        self.import_at = picked.and_then(|p| self.imports.iter().position(|n| *n == p)).unwrap_or(0);
    }

    /// Writes the game's save into a new zip in `Saves`.
    fn export_save(&mut self) {
        let (Some(data), Some(share)) = (crate::save_share::data(), crate::save_share::share()) else {
            self.note = "SAVE FOLDERS NOT KNOWN IN THIS START".to_owned();
            return;
        };
        let name = format!("save {}", crate::save_share::stamp());
        match crate::save_share::export(&data, &share, &name) {
            Ok(to) => {
                tracing::info!(to = %to.display(), "settings menu: save exported");
                self.note = format!("EXPORTED TO SAVES: {}.ZIP", shown_name(&name));
                self.read_imports();
            }
            Err(e) => {
                tracing::warn!(error = %e, share = %share.display(), "settings menu: save not exported");
                self.note = if e.kind() == std::io::ErrorKind::NotFound {
                    "NOTHING TO EXPORT: THE GAME HAS NOT SAVED YET"
                } else {
                    "EXPORT FAILED: THE GAME FOLDER CANNOT BE WRITTEN"
                }
                .to_owned();
            }
        }
    }

    /// Stages the save picked to be put in place at the next start, and starts the game again.
    fn import_save(&mut self) {
        let (Some(data), Some(share)) = (crate::save_share::data(), crate::save_share::share()) else {
            self.note = "SAVE FOLDERS NOT KNOWN IN THIS START".to_owned();
            return;
        };
        let Some(name) = self.imports.get(self.import_at).cloned() else {
            self.note = "NO SAVE TO IMPORT: PUT ONE IN SAVES FIRST".to_owned();
            return;
        };
        if let Err(e) = crate::save_share::stage(&data, &share, &name) {
            tracing::warn!(error = %e, name, "settings menu: save import not staged");
            self.note = if e.kind() == std::io::ErrorKind::NotFound {
                "IMPORT FAILED: NO SAVE IN THAT ZIP"
            } else {
                "IMPORT FAILED: THAT ZIP IS DAMAGED OR NOT A SAVE"
            }
            .to_owned();
            return;
        }
        tracing::info!(name, "settings menu: save import staged; put in place at the next start");
        if std::env::var_os("VICTORY_MENU_PLAY").is_some() {
            self.restart(false);
        } else {
            self.note = "IMPORTED: CLOSE THE GAME AND START IT AGAIN".to_owned();
        }
    }

    /// Removes the settings file: the launcher's own settings at the next start.
    fn reset(&mut self) {
        if let Some(path) = settings_file() {
            let _ = std::fs::remove_file(path);
        }
        self.note = "RESET. PLAY DEFAULTS AT THE NEXT START".to_owned();
    }

    /// Changes a setting and saves it at once: many players changed settings and closed the
    /// menu without the save lines, and lost them. The save lines stay (and restart, preload).
    fn change_and_save(&mut self, i: usize, step: isize) {
        if locked(&ITEMS[i]) {
            self.note = LOCKED.to_owned();
            return;
        }
        self.change(i, step);
        if self.save() && live(&ITEMS[i]) {
            self.note = "SAVED. ON NOW".to_owned();
        }
    }

    /// A frame was presented: the keys pressed since the last one.
    pub(super) fn frame(&mut self) {
        self.warmup();
        let pressed = std::mem::take(&mut *keys().lock().unwrap_or_else(|e| e.into_inner()));
        if pressed.is_empty() {
            return;
        }
        for vk in pressed {
            let open = Self::open();
            // Esc in the list of saves goes back to the settings.
            if open && self.picking && vk == VK_ESCAPE {
                self.picking = false;
                self.note.clear();
                self.changed = true;
                continue;
            }
            if vk == VK_F1 || (open && vk == VK_ESCAPE) {
                crate::SETTINGS_MENU_OPEN.store(!open && vk == VK_F1, Ordering::Relaxed);
                self.note.clear();
                self.picking = false;
                if !open {
                    self.read_imports();
                }
                self.changed = true;
                continue;
            }
            if !open {
                continue;
            }
            // Only an Enter right after the first one starts a preload or an import.
            let confirmed = self.note == CONFIRM_PRELOAD || self.note == CONFIRM_IMPORT;
            if confirmed {
                self.note.clear();
            }
            if self.picking {
                let (n, back) = (self.imports.len() + 1, self.import_at >= self.imports.len());
                match vk {
                    VK_UP => self.import_at = (self.import_at + n - 1) % n,
                    VK_DOWN => self.import_at = (self.import_at + 1) % n,
                    VK_RETURN if back => self.picking = false,
                    VK_RETURN if !confirmed => self.note = CONFIRM_IMPORT.to_owned(),
                    VK_RETURN => self.import_save(),
                    _ => {}
                }
                self.changed = true;
                continue;
            }
            match vk {
                VK_UP => self.step(-1),
                VK_DOWN => self.step(1),
                VK_LEFT if self.at < ITEMS.len() => self.change_and_save(self.at, -1),
                VK_RIGHT if self.at < ITEMS.len() => self.change_and_save(self.at, 1),
                VK_RETURN if self.at < ITEMS.len() => self.change_and_save(self.at, 1),
                VK_RETURN => match self.at - ITEMS.len() {
                    0 => {
                        if self.save() {
                            self.restart(false);
                        }
                    }
                    1 => {
                        self.save();
                    }
                    PRELOAD if !confirmed => self.note = CONFIRM_PRELOAD.to_owned(),
                    PRELOAD => {
                        if self.save() {
                            self.restart(true);
                        }
                    }
                    3 => self.reset(),
                    EXPORT => self.export_save(),
                    IMPORT => {
                        self.read_imports();
                        self.import_at = 0;
                        self.picking = true;
                    }
                    _ => crate::SETTINGS_MENU_OPEN.store(false, Ordering::Relaxed),
                },
                _ => {}
            }
            self.changed = true;
        }
    }

    /// Records the copy of the box onto `back` (in the copy-destination state).
    pub(super) fn draw(&mut self, host: &Host, list: &HostList, back: &ID3D12Resource, (width, height): (u32, u32), format: DXGI_FORMAT) {
        if self.failed {
            return;
        }
        // Closed: the frame-rate line in the top left corner, if it is wanted and there is a rate.
        let open = Self::open();
        let line = if open { None } else { fps_line() };
        if !open && line.is_none() {
            return;
        }
        // Our own copies are not part of a recorded frame (the recorder is in diagnostics builds).
        #[cfg(feature = "diagnostics")]
        let _ours = crate::replay::record::unrecorded();
        let scale = (height / 540).max(1);
        if self.buffers.as_ref().is_none_or(|b| (b.format, b.scale) != (format, scale)) {
            std::mem::forget(self.buffers.take());
            self.buffers = Buffers::new(host, format, scale);
            self.failed = self.buffers.is_none();
            self.changed = true;
        }
        if self.painted != line {
            self.painted = line.clone();
            self.changed = true;
        }
        let lines = if self.changed { Some(line.as_ref().map_or_else(|| self.rows(), |l| vec![Line::plain(l, Tone::White, Tone::Black)])) } else { None };
        let Some(buffers) = &mut self.buffers else { return };
        if let Some(lines) = lines {
            self.changed = false;
            buffers.write(&lines);
        }
        // The whole box in the middle, or of it the first line's part in the corner.
        let (w, h) = match &line {
            Some(l) => ((l.len() as u32 * CELL.0 + 2 * RIM) * scale, (CELL.1 + 2 * RIM) * scale),
            None => size(scale),
        };
        let (w, h) = (w.min(width), h.min(height));
        let (x, y) = if line.is_some() { (0, 0) } else { ((width - w) / 2, (height - h) / 2) };
        let list = &list.list;
        // SAFETY: an open list and live resources; the box lies within both.
        unsafe {
            if std::mem::take(&mut buffers.fresh) {
                if std::mem::replace(&mut buffers.used, true) {
                    list.ResourceBarrier(&[transition(&buffers.card, D3D12_RESOURCE_STATE_COPY_SOURCE, D3D12_RESOURCE_STATE_COPY_DEST)]);
                }
                list.CopyResource(&buffers.card, &buffers.uploads[buffers.turn].0);
                list.ResourceBarrier(&[transition(&buffers.card, D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_COPY_SOURCE)]);
            }
            let from = D3D12_TEXTURE_COPY_LOCATION {
                pResource: std::mem::transmute_copy(&buffers.card),
                Type: D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
                Anonymous: D3D12_TEXTURE_COPY_LOCATION_0 {
                    PlacedFootprint: D3D12_PLACED_SUBRESOURCE_FOOTPRINT {
                        Offset: 0,
                        Footprint: D3D12_SUBRESOURCE_FOOTPRINT { Format: format, Width: size(scale).0, Height: size(scale).1, Depth: 1, RowPitch: buffers.pitch },
                    },
                },
            };
            let to = D3D12_TEXTURE_COPY_LOCATION {
                pResource: std::mem::transmute_copy(back),
                Type: D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
                Anonymous: D3D12_TEXTURE_COPY_LOCATION_0 { SubresourceIndex: 0 },
            };
            let part = D3D12_BOX { left: 0, top: 0, front: 0, right: w, bottom: h, back: 1 };
            list.CopyTextureRegion(&to, x, y, 0, &from, Some(&part));
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn a_change_is_saved_at_once_however_the_game_was_started() {
        let home = std::env::temp_dir().join(format!("victory-menu-test-{}", std::process::id()));
        let _ = std::fs::remove_dir_all(&home);
        // SAFETY: no other test in this crate reads these two.
        unsafe {
            std::env::remove_var("VICTORY_MENU_FILE");
            std::env::set_var("VICTORY_HOME", &home);
        }
        let mut menu = Menu { values: ITEMS.iter().map(|_| String::new()).collect(), started: Vec::new(), at: 0, note: String::new(), changed: false, painted: None, buffers: None, failed: false, imports: Vec::new(), import_at: 0, picking: false };
        menu.started = menu.values.clone();
        let dlss = ITEMS.iter().position(|i| i.var == "VICTORY_MENU_DLSS").expect("in the menu");
        menu.change_and_save(dlss, 1);
        let text = std::fs::read_to_string(home.join("victory-settings.txt")).expect("saved without a save line");
        assert!(text.contains("VICTORY_MENU_DLSS=quality"), "{text}");
        assert_eq!(menu.note, "SAVED. APPLIES WHEN VICTORY.EXE STARTS THE GAME");
        // Every note fits the box and has its letters.
        for note in [menu.note.as_str(), "SAVED. ON NOW", "SAVED. APPLIES AT THE NEXT START", "NO FOLDER FOR THE SETTINGS FILE"] {
            assert!(note.len() <= COLUMNS as usize, "{note}");
            assert!(note.bytes().all(|c| glyph(c) != glyph(b'#')), "{note}");
        }
        let _ = std::fs::remove_dir_all(&home);
    }

    #[test]
    fn every_line_fits_and_every_character_has_a_picture() {
        let mut menu = Menu { values: ITEMS.iter().map(|_| String::new()).collect(), started: Vec::new(), at: 0, note: String::new(), changed: false, painted: None, buffers: None, failed: false, imports: Vec::new(), import_at: 0, picking: false };
        menu.started = menu.values.clone();
        for at in 0..ITEMS.len() + ACTIONS.len() {
            menu.at = at;
            for line in menu.lines() {
                assert!(line.len() <= COLUMNS as usize, "{line}");
                for c in line.bytes() {
                    assert_ne!(glyph(c.to_ascii_uppercase()), glyph(b'#'), "{line}: {}", c as char);
                }
            }
        }
    }

    #[test]
    fn the_link_is_always_right_under_the_note() {
        let mut menu = Menu { values: ITEMS.iter().map(|_| String::new()).collect(), started: Vec::new(), at: 0, note: String::new(), changed: false, painted: None, buffers: None, failed: false, imports: Vec::new(), import_at: 0, picking: false };
        menu.started = menu.values.clone();
        for note in ["", "SAVED"] {
            menu.note = note.to_owned();
            for at in 0..ITEMS.len() + ACTIONS.len() {
                menu.at = at;
                let lines = menu.lines();
                assert_eq!(lines.len(), LINES as usize);
                let n = lines.len();
                assert!(lines[n - 2].starts_with("* = CHANGED") || lines[n - 2] == note, "{}", lines[n - 2]);
                assert_eq!(lines[n - 1], REPO);
                // What a preload costs, and what MFG Unlocker is, is said while it is picked, and only then.
                let about = &lines[n - 5..n - 2];
                if at == ITEMS.len() + PRELOAD {
                    assert_eq!(about, ABOUT_PRELOAD);
                } else if at == ITEMS.len() + EXPORT {
                    assert_eq!(about, ABOUT_EXPORT);
                } else if at == ITEMS.len() + IMPORT {
                    assert_eq!(about, ABOUT_IMPORT);
                } else if at < ITEMS.len() && ITEMS[at].var == MFG {
                    assert_eq!(about, ABOUT_MFG);
                } else if at < ITEMS.len() {
                    // Every setting says what it does.
                    assert_eq!(about, wrapped(ITEMS[at].about), "{}", ITEMS[at].label);
                    assert!(!about[0].is_empty(), "{}", ITEMS[at].label);
                } else {
                    assert_eq!(about, wrapped(ABOUT_ACTIONS[at - ITEMS.len()]));
                }
            }
        }
        assert_eq!(ACTIONS[PRELOAD], "PRELOAD SHADERS (RESTARTS THE GAME)");
        assert!(CONFIRM_PRELOAD.len() <= COLUMNS as usize);
        assert_eq!(ACTIONS[EXPORT], "EXPORT SAVE (TO SAVES IN THE GAME FOLDER)");
        assert_eq!(ACTIONS[IMPORT], "IMPORT SAVE (PICK FROM SAVES)");
        let pick = [PICK_TITLE, PICK_HELP, PICK_NONE, PICK_BACK, CONFIRM_IMPORT, "IMPORT FAILED: THAT ZIP IS DAMAGED OR NOT A SAVE"];
        for line in ABOUT_EXPORT.iter().chain(&ABOUT_IMPORT).chain(&ABOUT_PICK).chain(&pick) {
            assert!(line.len() <= COLUMNS as usize, "{line}");
            assert!(line.bytes().all(|c| glyph(c) != glyph(b'#')), "{line}");
        }
        assert!(ABOUT_MFG.iter().all(|l| l.len() <= COLUMNS as usize));
        for line in ABOUT_NO_DLSS {
            assert!(line.len() <= COLUMNS as usize && line.bytes().all(|c| glyph(c) != glyph(b'#')), "{line}");
        }
        // DLSS picked on where NVIDIA's files are missing (no VICTORY_STREAMLINE_DIR in tests): said so.
        let dlss = ITEMS.iter().position(|i| i.var == DLSS).expect("in the menu");
        menu.at = dlss;
        menu.values[dlss] = "quality".to_owned();
        let lines = menu.lines();
        assert_eq!(&lines[lines.len() - 5..lines.len() - 2], ABOUT_NO_DLSS);
        menu.values[dlss] = "off".to_owned();
        let lines = menu.lines();
        assert_eq!(&lines[lines.len() - 5..lines.len() - 2], wrapped(ITEMS[dlss].about));
        assert_eq!(ITEMS.iter().filter(|i| i.var == MFG).count(), 1);
    }

    #[test]
    fn values_cycle_both_ways() {
        let mut menu = Menu { values: ITEMS.iter().map(|i| i.values[0].0.to_owned()).collect(), started: Vec::new(), at: 0, note: String::new(), changed: false, painted: None, buffers: None, failed: false, imports: Vec::new(), import_at: 0, picking: false };
        menu.started = menu.values.clone();
        let n = ITEMS[0].values.len();
        for _ in 0..n {
            menu.change(0, 1);
        }
        assert_eq!(menu.values[0], ITEMS[0].values[0].0);
        menu.change(0, -1);
        assert_eq!(menu.values[0], ITEMS[0].values[n - 1].0);
    }

    #[test]
    fn a_shared_save_name_is_shown_in_letters_the_menu_has() {
        let names = vec!["save 2026-11-27 23-45-10.zip".to_owned(), "Jason's boat ~ 100% [\u{e9}] and a very long name that goes on and on.zip".to_owned()];
        let mut menu = Menu { values: ITEMS.iter().map(|_| String::new()).collect(), started: Vec::new(), at: ITEMS.len() + IMPORT, note: String::new(), changed: false, painted: None, buffers: None, failed: false, imports: names, import_at: 0, picking: true };
        menu.started = menu.values.clone();
        let check = |menu: &Menu| {
            let lines = menu.lines();
            assert_eq!(lines.len(), LINES as usize);
            assert_eq!(lines[lines.len() - 1], REPO);
            assert_eq!(&lines[lines.len() - 5..lines.len() - 2], ABOUT_PICK);
            for line in &lines {
                assert!(line.len() <= COLUMNS as usize, "{line}");
                assert!(line.bytes().all(|c| glyph(c.to_ascii_uppercase()) != glyph(b'#')), "{line}");
            }
            lines
        };
        let lines = check(&menu);
        assert!(lines.iter().any(|l| l == "> SAVE 2026-11-27 23-45-10.ZIP"), "{lines:?}");
        assert!(lines.iter().any(|l| l == &format!("  {PICK_BACK}")), "{lines:?}");
        menu.import_at = 1;
        check(&menu);
        menu.import_at = 2;
        assert!(check(&menu).iter().any(|l| l == &format!("> {PICK_BACK}")));
        // Many saves: the list scrolls and the box keeps its size.
        menu.imports = (0..50).map(|i| format!("save {i}.zip")).collect();
        for at in [0, 25, 49, 50] {
            menu.import_at = at;
            let lines = check(&menu);
            let picked = if at == 50 { format!("> {PICK_BACK}") } else { format!("> SAVE {at}.ZIP") };
            assert!(lines.contains(&picked), "{at}: {lines:?}");
        }
        menu.imports.clear();
        menu.import_at = 0;
        let lines = check(&menu);
        assert!(lines.iter().any(|l| l == PICK_NONE));
        assert!(lines.iter().any(|l| l == &format!("> {PICK_BACK}")));
    }
    #[test]
    fn every_setting_says_what_it_does_in_three_lines_or_less() {
        for item in ITEMS {
            let words: Vec<&str> = item.about.split_whitespace().collect();
            let shown: Vec<String> = wrapped(item.about).iter().flat_map(|l| l.split_whitespace().map(str::to_owned).collect::<Vec<_>>()).collect();
            assert_eq!(words.len(), shown.len(), "{}: too long for three lines", item.label);
            assert!(item.about.bytes().all(|c| glyph(c) != glyph(b'#')), "{}", item.label);
        }
        for about in ABOUT_ACTIONS.iter().chain([&LOCKED, &FOOTER]) {
            assert!(about.bytes().all(|c| glyph(c) != glyph(b'#')), "{about}");
        }
    }

    #[test]
    fn sections_come_before_their_settings_and_every_setting_is_listed_once() {
        let list = entries();
        // Not started by the launcher in tests: every setting and every action is there.
        for at in 0..ITEMS.len() + ACTIONS.len() {
            assert_eq!(list.iter().filter(|e| **e == Entry::Line(at)).count(), 1, "{at}");
        }
        for (var, title) in SECTIONS {
            let i = ITEMS.iter().position(|item| item.var == *var).expect("a section starts at a setting");
            let at = list.iter().position(|e| *e == Entry::Line(i)).expect("listed");
            assert_eq!(list[at - 1], Entry::Section(title));
        }
        assert_eq!(list[0], Entry::Section(SECTIONS[0].1));
    }

    #[test]
    fn the_box_is_painted_in_its_colours_in_every_format() {
        let menu = Menu { values: ITEMS.iter().map(|_| String::new()).collect(), started: ITEMS.iter().map(|_| String::new()).collect(), at: 0, note: String::new(), changed: false, painted: None, buffers: None, failed: false, imports: Vec::new(), import_at: 0, picking: false };
        let rows = menu.rows();
        assert_eq!(rows.len(), LINES as usize);
        assert!(rows.iter().all(|r| r.text.len() == r.fg.len()));
        for format in [DXGI_FORMAT_R8G8B8A8_UNORM, DXGI_FORMAT_B8G8R8A8_UNORM, DXGI_FORMAT_R10G10B10A2_UNORM, DXGI_FORMAT_R16G16B16A16_FLOAT] {
            let bytes = encode(format, [0; 3]).expect("drawn in this format").len();
            let pitch = (size(1).0 as usize * bytes).next_multiple_of(256);
            let picture = painted(&rows, 1, format, pitch).expect("painted");
            assert_eq!(picture.len(), pitch * size(1).1 as usize);
            // The frame's corner, the title bar under it, and the box's back are the colours set.
            assert_eq!(&picture[..bytes], encode(format, Tone::Border.rgb()).unwrap().as_slice());
            let title = pitch * (RIM + 1) as usize + bytes * (RIM as usize + CELL.0 as usize * (COLUMNS as usize - 1));
            assert_eq!(&picture[title..title + bytes], encode(format, Tone::TitleBar.rgb()).unwrap().as_slice());
        }
        // Halves: 1 is 0x3C00, 0 is 0.
        assert_eq!((half(1.0), half(0.0)), (0x3C00, 0));
        assert_eq!(encode(DXGI_FORMAT_B8G8R8A8_UNORM, [1, 2, 3]), Some(vec![3, 2, 1, 0xFF]));
        assert_eq!(encode(DXGI_FORMAT_R32G32B32A32_FLOAT, [0; 3]), None);
    }
    /// `VICTORY_MENU_DUMP=<folder> cargo test -p victory-gpu --lib dump_the_menu -- --ignored`: the
    /// box as the game shows it, in `menu.ppm` and `menu-picked-action.ppm` there (to look at a change).
    #[test]
    #[ignore = "writes pictures; run by hand"]
    fn dump_the_menu() {
        let Some(folder) = std::env::var_os("VICTORY_MENU_DUMP") else { return };
        let folder = std::path::PathBuf::from(folder);
        let mut menu = Menu { values: ITEMS.iter().map(|i| i.values[0].0.to_owned()).collect(), started: ITEMS.iter().map(|i| i.values[0].0.to_owned()).collect(), at: 2, note: String::new(), changed: false, painted: None, buffers: None, failed: false, imports: Vec::new(), import_at: 0, picking: false };
        menu.values[1] = "k".to_owned();
        for (name, at) in [("menu.ppm", 2), ("menu-picked-action.ppm", ITEMS.len() + PRELOAD)] {
            menu.at = at;
            let (w, h) = size(2);
            let pitch = (w * 4) as usize;
            let picture = painted(&menu.rows(), 2, DXGI_FORMAT_R8G8B8A8_UNORM, pitch).expect("painted");
            let mut ppm = format!("P6 {w} {h} 255
").into_bytes();
            for px in picture.chunks(4) {
                ppm.extend_from_slice(&px[..3]);
            }
            std::fs::write(folder.join(name), ppm).expect("written");
        }
    }
}
