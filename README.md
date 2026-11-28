<div align="center">

<img src="https://capsule-render.vercel.app/api?type=waving&height=180&color=0:ff5fa2,50:ffc371,100:5f6dff&text=Cold%20Project%20VI&fontColor=ffffff&fontSize=54&fontAlignY=36&desc=edupode%20remake%20%E2%80%94%20GTA%20VI%20(Xbox%20Series)%20on%20an%20RTX%20laptop&descAlignY=58&animation=fadeIn" alt="Cold Project VI - edupode remake" width="100%"/>

<a href="#where-it-stands"><img src="https://readme-typing-svg.demolab.com?font=Fira+Code&weight=600&size=20&pause=1200&color=FF5FA2&center=true&vCenter=true&width=680&lines=Open+world+22-26+fps%2C+cutscenes+35%2B%2C+menus+60;NVIDIA+Smooth+Motion+set+up+for+you;Pipelines+loaded+before+the+game+opens;VI+Launcher%3A+one+window+to+build+and+play;Missions+playable%2C+long+sessions+without+crashes" alt="animation"/></a>

<p>
  <a href="#quick-start-julias-launcher"><img src="https://img.shields.io/badge/%F0%9F%8E%AE%20Julia%27s%20Launcher-start%20here-ff5fa2?style=for-the-badge" alt="Julia's Launcher"/></a>
  <a href="docs/GUIDE-30FPS.md"><img src="https://img.shields.io/badge/Guide-English-C8102E?style=for-the-badge" alt="Guide"/></a>
  <a href="README.pt.md"><img src="https://img.shields.io/badge/Leia%20em-Portugu%C3%AAs-006600?style=for-the-badge" alt="Portugues"/></a>
  <a href="README.fr.md"><img src="https://img.shields.io/badge/Lire%20en-Fran%C3%A7ais-0055A4?style=for-the-badge" alt="Francais"/></a>
  <a href="https://github.com/edupode"><img src="https://img.shields.io/badge/by-%40edupode-222?style=for-the-badge&logo=github" alt="by edupode"/></a>
</p>

<p>
  <img src="https://img.shields.io/badge/fps-22--26%20open%20world%20%7C%2060%20menus-ff5f6d?style=flat-square" alt="fps"/>
  <img src="https://img.shields.io/badge/VRAM-6--8%20GB%20of%2012-ffc371?style=flat-square" alt="VRAM"/>
  <img src="https://img.shields.io/badge/RAM-~20%20GB-ffc371?style=flat-square" alt="RAM"/>
  <img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?style=flat-square&logo=windows" alt="Windows"/>
  <img src="https://img.shields.io/badge/GPU-NVIDIA%20%7C%20AMD%20%7C%20Intel-76B900?style=flat-square" alt="NVIDIA, AMD, Intel"/>
  <img src="https://img.shields.io/badge/Rust-pinned-B7410E?style=flat-square&logo=rust" alt="Rust"/>
  <img src="https://img.shields.io/badge/license-GPL--3.0-845ec2?style=flat-square" alt="GPL-3.0"/>
</p>

<p>
  <a href="#quick-start-julias-launcher"><img src="https://img.shields.io/badge/%E2%96%B6%20Run-222?style=for-the-badge" alt="Run"/></a>
  <a href="#modes"><img src="https://img.shields.io/badge/%E2%9A%A1%20Modes-222?style=for-the-badge" alt="Modes"/></a>
  <a href="#controls"><img src="https://img.shields.io/badge/%F0%9F%95%B9%20Controls-222?style=for-the-badge" alt="Controls"/></a>
  <a href="#where-it-stands"><img src="https://img.shields.io/badge/%F0%9F%93%8B%20Status-222?style=for-the-badge" alt="Status"/></a>
</p>

</div>

> [!NOTE]
> **This is a remake by [@edupode](https://github.com/edupode)** of the Cold Project VI idea, internally
> called **"luz"** (Portuguese for *light*, after the lighting work it started with). It combines
> [VIctory](https://github.com/Marvisak/VIctory), the cgbj build (cputroll/cgbj-reupload), Leonidas's and
> AnyXboxSeries's work, the VIctory-NVIDIA fork (trustno1-coder) and through it foxhound (readmet3xt-lab), cool_fox (codec-d) and DexNom's VIctory, fxceless's rendering fixes and adaptations from an AMD fork
> (avatarmanz/VIctory-7900XTX), tuned and extended for an NVIDIA laptop. It is not the upstream Cold Project
> VI (felicienradio/cold-project-vi), whose page inspired this one.

> [!IMPORTANT]
> **This repository contains no game files**: no executable, data or extracted shaders. You need your own copy
> of the game. Not affiliated with or endorsed by the game's developer or publisher.

### From the author

> It runs 24-26 fps, all controls have been mapped,currently using -locallights  
> On the Open world i get about 22-24fps  
> Everything seems to be loading ok  
> I need help to fix volumetric shaders _Tiled (i already know how to fix them but it will take some time)  
> It's running on 12gb vram and 32gb ddr5  
> Currently it uses about 6-8gb on vram and 20-21gb of ddr5  
> it used to use 10-12gb on vram and 27-29gb of ram of ddr5  
> I got a laptop with a rtx 5070 ti capped at 145w and a Intel i9 ultra 275hx  
> Hope this helps anyone, missions are already possible and doesn't crash even after long sessions  
> on menus it goes from 30-32/33fps now its 60 fixed  

GTA VI from **your own Xbox Series dump**, on Windows, through [VIctory](https://github.com/Marvisak/VIctory)
(this branch: the cgbj build with the VIctory-NVIDIA lighting and many changes of our own). **No game files are
included.**

### What's new on this branch (28/11/2026)

- **The VIctory-NVIDIA fork merged in** ([trustno1-coder/VIctory-NVIDIA](https://github.com/trustno1-coder/VIctory-NVIDIA) and its
  contributors; their commits keep their authors): DLSS Frame Generation and AMD FSR 3 frame generation through NVIDIA
  Streamline, the game's own 30 fps limit lifted (the 60 FPS box), open water without the dark blocks, PS4 and PS5 pads, the Windows 10 runtime.
- **Frame generation on the Play page:** Automatic, NVIDIA Smooth Motion, DLSS Frame Generation (RTX 40/50; RTX 20/30 with
  [MFG Unlocker](https://github.com/dashdogy/MFG-Unlocker), someone else's MIT-licensed mod, fetched only if you pick it),
  FSR 3 (RTX cards, it needs DLSS upscaling) or VIctory 2X. NVIDIA's and AMD's files are fetched from their own GitHub at PLAY.
- **F1 in the game:** a redesigned settings menu (sections, what each setting does); what you change there applies at the next
  PLAY. DLSS and the frame generation multiplier can now be changed in F1 even when the launcher started the game (only
  the frame generation engine and the frame cap stay the launcher's). **F3:** the stats panel, in every build.
- **8 GB cards (0.3.18):** no new textures while the card is past its memory budget; the overflow filled the PC's memory
  and crashed 8 GB / 16 GB RAM PCs a few minutes in.
- **Launcher (0.3.19):** updates swap all files as one piece and roll back if anything fails; Setup shows "Could not
  check" instead of "Not needed" when a check fails; each game's log lists the settings it really used and which F1 changed.
- **Missing game files (0.3.20):** the launcher names a missing DLL of the game (e.g. amd_fidelityfx_loader_xbox.dll)
  before PLAY instead of a bare "code 1", and such a start no longer moves AMD cards to another engine.
- **Launcher (0.3.21):** automatic settings for each PC every time it opens (preset, tested options, the frame
  generation tested on that card); untested options marked EXPERIMENTAL; Players' PCs shows the median FPS per card.
- **Launcher (0.3.22):** a first-start warm-up that crashed is not asked for again (it looped on every start), and
  automatic settings keep each PC's preset instead of picking it again at every opening.
- **Engine (0.3.23):** the title's stop at +0x1dac734 (a component under its FidelityFX setup that did not start)
  is taken as going on without it: the game no longer closes a second in on some PCs; others never reach it.
- **0.3.24:** a launcher outside a source folder always plays the victory.exe its updates install; the title's
  component-start stops are stepped over at three places; logs and reports name the victory.exe that started.
- **0.3.25:** the launcher puts back the victory.exe its own update brought when another one is in place; reports
  list the game's folder (names and sizes) and each game's summary has a line about it.
- **Less churn:** a list's upload buffers are reused instead of one new buffer per copy (about 700 a second before).
- **Launcher:** themes (Plum, Midnight, Vice, Graphite, Daylight) and a cleaner Play page; the first start of a new
  `victory.exe` warms up and starts again once, by itself.
- The FPS figures below are from before this merge. If Windows Defender removes a freshly built launcher (a `!ml` detection),
  Windows Security, Protection history, **Allow on device** brings it back.

### The edupode engine

The engine every card plays with by default. Measured on the author's RTX 5070 Ti laptop (12 GB, 32 GB RAM):

| | cgbj before | edupode engine |
|---|---|---|
| Video memory | ~12 GB, over the system's budget | **6-8 GB** |
| Textures that fit on the card | ~10,000 | **~40,000** |
| RAM | 27-29 GB | **20-21 GB** |
| Open world | 10-15 FPS | **22-26 FPS** |
| Crowds and fights | 10-15 FPS | **14-17 FPS** |
| Cutscenes / menus | ~26 / 30-33 FPS | **35-38 / 60 FPS** |
| Long sessions | | **missions without crashes** |

## Progress on PortWatch VI's scale

**About 79 %** (27/11/2026: the author's RTX 5070 Ti laptop, and players reporting a steady 30 fps). PortWatch's scale: up to 50 %,
each step needs the one before; beyond that, story, picture, speed and stability share the last 50 points.
Points past 50 are our estimate (PortWatch counts them only with proof, a video).

| Milestone | % | State |
|---|---|---|
| Groundwork, executable, first picture, main menu, in the world | 0-40 | ✅ |
| Playable (you control a character) | 50 | ✅ missions playable |
| Story (17.5 points) | about 3 | ✅ the opening and its missions play through; the rest of the story not played yet |
| Picture (12.5 points) | about 8 | ✅ NVIDIA lighting, water right, night scenes lit; ⏳ ray tracing and the `_Tiled` volumetrics still off |
| Speed (10 points: fps ÷ 30) | about 10 | ✅ players at a steady 30 fps; 22-26 fps in the open world on the author's laptop, 35-38 in cutscenes, 60 in menus |
| Stability (10 points) | about 8 | ✅ long sessions and missions without crashes |

## Download

> Julia's Launcher is made by felicienradio and edupode together; releases come from this repository.

**[Releases](https://github.com/felicienradio/cold-project-vi/releases/latest)**: get `VI-Setup-<version>.exe` and run it.

[Is it safe?](docs/SAFETY.md) · [Code signing policy](docs/CODE-SIGNING-POLICY.md) · [Privacy](docs/CODE-SIGNING-POLICY.md#privacy)

- **One administrator prompt at the start.** After that the installer does the rest in the background:
  - power mode;
  - virtual memory;
  - Defender's ransomware rule for its folder;
  - the dedicated graphics card for `victory.exe`;
  - Smooth Motion.
- **The launcher opens when it finishes.** It finds your game and prepares the shaders.
- **Nothing to compile:** you don't need Rust or Build Tools.
- **Updates install themselves.** See [What the launcher does](#what-the-launcher-does).

**Bugs:** open an [issue](https://github.com/edupode/cold-project-vi/issues) or post in the **"Send bugs here please"** channel on [Discord](https://discord.gg/648WEXCAsZ); the launcher makes the package and fills the issue in for you (see [Logs and errors](#logs-and-errors)).

### Checking a release

Every program in a release is built by GitHub Actions from this repository's code ([`release.yml`](.github/workflows/release.yml)), never on a PC. Anyone can check that:

1. **The download came from this code:**
   ```powershell
   gh attestation verify "Julias-Launcher.exe" -R felicienradio/cold-project-vi
   ```
2. **The SHA-256 of each file** matches `SHA256SUMS.txt` in the release.
3. **The launcher's source is included:** `launcher-source.zip` sits beside every install. `launcher\build-launcher.cmd` builds it with the C# compiler that ships with Windows.
4. **Each update is signed with edupode's release key.** The launcher installs a file only when the signature and its SHA-256 match ([`Updater.cs`](launcher/Updater.cs)).

## Quick start: Julia's Launcher

1. Get this branch (`git clone`, or **Code → Download ZIP** on GitHub) and build the launcher; it needs nothing
   but Windows:
   ```powershell
   git clone -b edupode https://github.com/edupode/cold-project-vi.git
   cd cold-project-vi
   launcher\build-launcher.cmd
   ```
2. Open **`Julia's Launcher.exe`** and press **⚡ First-time auto-setup** (or the **Setup** page). It installs Rust and the
   C++ Build Tools with winget, builds VIctory with a progress bar, adds the Defender exception (Windows asks
   first), finds your game folder on its own, prepares the shaders, sets Windows to best performance and
   system-managed virtual memory, and turns Smooth Motion on. By hand, the same steps are:
   - **Build VIctory** (once, several minutes);
   - choose your **game folder** (the one with `game_scarlett_master.exe`);
   - **Prepare shaders** (about 5 minutes; again after each rebuild);
   - press **★ Recommended setup** and **PLAY**.

If Windows says "Access denied", Defender's ransomware rule blocked the new exe. In an administrator
PowerShell: `Add-MpPreference -AttackSurfaceReductionOnlyExclusions "<repo folder>\target"`.

### `git` is not recognized

If PowerShell answers the `git clone` with:

```text
PS C:\Users\Will> git clone -b edupode https://github.com/edupode/cold-project-vi.git
git : The term 'git' is not recognized as the name of a cmdlet, function, script file, or operable program.
```

Git is not installed. Install it, **close and reopen PowerShell**, and run the `git clone` again:

```powershell
winget install --id Git.Git -e --source winget
```

No winget? Download Git from <https://git-scm.com/download/win>, or skip Git: on this page press
**Code → Download ZIP**, extract it and run `launcher\build-launcher.cmd` inside the extracted folder.

<p align="center"><img src="launcher/screenshot.png" alt="Julia's Launcher" width="760"/></p>

The launcher checks your GPU, video memory, RAM, Resizable BAR and Smooth Motion support and sets everything
else. Point at any option to see what it does; **★** marks what was tested on the author's PC. On cards with
less than 10 GB or PCs with less than 24 GB of RAM it picks the **Modest PC** mode.
English by default, **Português** and **Français** in the side bar; this page also in [Português](README.pt.md) and [Français](README.fr.md). `Jogar-Luz.cmd` does the same from a command line
(`Jogar-Luz.cmd tudo`).

### What the launcher does

| | |
|---|---|
| **Play** | Picks a preset from your hardware the first time.<br>**Presets:** Recommended (720p), Fastest and Modest PC (540p), **Ultra Low** (4-6 GB cards, 8-16 GB of RAM), Sharp and Classic cgbj (1080p), **Custom** (your values).<br>**Output:** 1080p, 1440p (2K) or 2160p (4K); the preset's scale applies on top (Recommended at 4K draws 2560x1440 and upscales).<br>A strip shows whether the dump, the emulator, the shaders and Windows are ready. A **yellow notice** lists what the game would run without (virtual memory, power, Resizable BAR, the dedicated graphics card, shaders) and opens the fix. |
| **Stuck game** | A VIctory that stopped answering (a crash, an error window) never blocks PLAY: it turns into **CLOSE AND PLAY**, and **Close game** ends it. |
| **Several game executables** | When the dump has more than one `game_scarlett*.exe`, you choose which one beside the game folder.<br>A broken file (a download that stopped, an encrypted package) is named before PLAY, instead of VIctory's "malformed image". |
| **Loading bar before the game** | Every pipeline earlier sessions drew with is made before the game opens, so there is no stutter the first time you see a place. |
| **Setup** | Every first-run step, checked (✓/✗), with a button that does it, plus **Do everything for me**:<br>Rust, Build Tools, build, AMD engine, Defender, game folder, shaders, power, virtual memory, dedicated graphics card, Smooth Motion, Resizable BAR.<br>**Build** installs what it needs first. If the launcher is not inside the source folder, it downloads the source; this fixes `could not find Cargo.toml in ...\Downloads`. |
| **Resizable BAR** | Checked the way the game sees it (D3D12's GPU upload heaps), not only in the BIOS.<br>The guide covers your motherboard maker's menus, NVIDIA's firmware update for early RTX 30 cards, and Windows 11 24H2.<br>When it cannot be turned on, it says what to do instead, and **I can't turn it on** stops the warning. |
| **Shaders** | Live: shaders translated, pipelines made, the ones that failed, the driver, disk room, the last errors.<br>Two progress bars: **Shaders** (how much of the game's files has been translated) and **Pipelines** (how many are made, also during the loading before each game).<br>**Repair** retries what failed; **Rebuild everything** starts again. |
| **Performance** | The apps holding video memory and RAM; tick them and they close.<br>The video memory reserve and frame generation are set here. |
| **Frame generation** | **RTX 40/50:** NVIDIA Smooth Motion, written into the driver's profile (no NVIDIA App).<br>**RTX 30:** [NVSmooth30](https://github.com/ItsAdeline/NVSmooth30), experimental.<br>**Every other card (GTX 10, RTX 20, AMD with the edupode engine, Intel):** VI frame generation (experimental, off by default) makes a frame between each two of the game's from the pictures alone, at the cost of half a frame of delay. |
| **AMD cards** | They play with the **edupode engine** too, like every card. As an alternative tuned for Radeon there is **[VIctory-Belobug-AMD](https://github.com/Belobug/VIctory-Belobug-AMD) by Belobug** (GPL-3.0; all credit for it is his): pick it in Hardware & ID, and Setup downloads and builds it once. If the edupode engine ends at start twice in a row on a Radeon, the launcher switches to Belobug's on its own and says how to go back. |
| **Advanced Tweaks (Custom)** | Resolution, texture size, buffers, memory guards and FPS clock.<br>**Copy my profile** gives a code (`VI1-...`) to share. Paste a friend's code in the box and **Apply**: it shows what changes first, and a code can only hold these values. |
| **Game saves** | Hardware & ID > **Game saves**: export your save to a zip on the Desktop, or import a friend's (yours are kept in a backup first, in `%LOCALAPPDATA%\VIctory\save-backups`). |
| **Integrated graphics** | Every adapter is read through DXGI. A PC with only integrated graphics (Intel Iris Xe / UHD) gets Ultra Low with the heavy options off and a warning before PLAY: a 4 GB+ graphics card is needed to run well. |
| **Hardware & ID** | Your permanent ID (`VI-XXXX-XXXX`, from PC parts; it stays after reinstalling Windows) with a copy button, your hardware, and the engine choice. |
| **Hardware sharing** | **Off by default.** When on, it sends **only hardware** (graphics card, processor, RAM, motherboard, Windows, disk type), at most once a day, to a write-only database only edupode can read.<br>It never sends logs, options, paths, names or your IP. The page shows exactly what is sent.<br>While it is off, the launcher asks before each game, explaining why it helps; **Don't ask again** ends that. Only the averages are public: [docs/HARDWARE-STATS.md](docs/HARDWARE-STATS.md), updated every 6 hours. |
| **Updates** | Checks GitHub when the launcher opens and **every 5 minutes**: the newest release of the repository that carries a signed manifest, whatever branch it was cut from (`main` or `edupode`). A new version installs itself while the game is not running, and only when its signature and SHA-256 match.<br>This and the opt-in hardware report are the launcher's only network traffic. |
| **FPS overlay (F3)** | FPS now, the last minute's average (AVG 1M), the whole run's (ALL), the worst half second (LOW), frame time, draws, video memory and RAM. |

### Logs and errors

- **Game logs:** `%LOCALAPPDATA%\VIctory\luz\logs`. `session-*.log` is one file per game; `prepare.log`, `build.log` and `build-amd.log` cover shaders and builds.
- **Launcher errors:** `%TEMP%\vi-launcher-error.txt`.
- **Send errors (GitHub / Discord)** (side bar or Help):
  1. Choose **launcher errors** or **game errors**.
  2. Tick what happened (shaders not rendering, virtual memory not turning on, ...) and write a line.
  3. The launcher puts the right files in a zip on your Desktop, with your user name taken out of every path. It opens the folder and copies a short summary.
  4. Send it:
     - **Send to edupode:** one click. A compact copy (summary, settings, the end of each log, your user name taken out) goes to a private report box on Cloudflare that only edupode can read; you get the report's number.
     - **Open a GitHub issue:** a new [issue](https://github.com/edupode/cold-project-vi/issues) opens in your browser already filled in (hardware, what you ticked, your words). Drag the zip into it and press **Submit new issue** (a free GitHub account is needed).
     - **Discord:** send the zip in the **"Send bugs here please"** channel on [Discord](https://discord.gg/648WEXCAsZ).

  Nothing is sent by the launcher itself.

## Which PCs can play

From the smallest PC to the biggest, with what we have actually seen on each one. The **processor** and the
**RAM** limit the game more than the graphics card does. A **dedicated graphics card** is needed: integrated
graphics do not run it.

| PC (smallest → biggest) | Preset | What we saw | State |
|---|---|---|---|
| Integrated graphics only: Intel Iris Xe, Core i5-1334U, 16 GB RAM (laptop) | Ultra Low, heavy options off | The game closes after about a minute: out of video memory, then the GPU stops ("device removed"); the Intel driver refuses many of the game's shaders. Tested 27/11/2026 | ❌ does not run |
| 4-6 GB card, 8-16 GB RAM | Ultra Low (540p, 30 FPS clock) | No report yet | ❓ not tested |
| 6-8 GB card, 16 GB RAM | Modest PC (540p, light) | 8 GB cards ran at about 3 FPS while "Crowds and fights" overfilled video memory; fixed in 0.3.2, not measured since | ⚠️ to measure |
| RTX 3060, 16 GB RAM | Modest PC | 4.6-4.7 FPS in the prison cell (26/11/2026, before the edupode engine); 16 GB of RAM is the limit | ⚠️ very slow |
| RTX 3070 Ti, Core i5-14400F, 16 GB RAM | Modest PC | 4.8 FPS on its first test, 3 in a heavier scene (26/11/2026, before the edupode engine); the card was only busy 60 ms a frame | ⚠️ very slow |
| Radeon RX 6800 (16 GB) | Recommended | The edupode engine stopped at start ("device removed"); the launcher switches to Belobug's AMD engine on its own | ⚠️ with Belobug's engine |
| RTX 5070 Ti laptop (12 GB, 145 W), Core Ultra 9 275HX, 32 GB DDR5 | ★ Recommended (720p → 1080p) | Open world 22-26 FPS, crowds and fights 14-17, cutscenes 35-38, menus 60; 6-8 GB of video memory and ~20 GB of RAM used. Long sessions without a crash | ✅ plays well |
| 12 GB+ card, strong CPU, 32 GB RAM | Sharp (1080p) | No report yet | ❓ not tested |

Some players report a steady 30 FPS, on PCs not named yet. Your PC is not in the table? Turn on **Hardware
sharing** (Hardware & ID) and tell us your FPS on Discord: each report fills a line.

## Modes

| Mode | What it does | RTX 5070 Ti laptop |
|---|---|---|
| **★ Recommended - 720p** (+ ★ crowds and fights: the author's daily setup) | World at 1280x720 + the game's own upscaler to 1080p, texture levels held, 60 FPS clock, borderless | open world 22-26 FPS, crowds 14-17, cutscenes 35+, menus 60 |
| Fastest - 540p | Softer picture, most frames; for slower CPUs | a few FPS more |
| Modest PC - 540p, light | For 6-8 GB cards and 16 GB of RAM: less video memory and RAM | |
| Sharp - 1080p | Full resolution | slower in the open world |
| Classic cgbj | The original cgbj settings | for comparison |

Options: ★ FPS overlay (F3), ★ borderless full screen (test A: 22-24 FPS on the author's PC; needed by Smooth
Motion), ★ the 60 FPS clock, pipelines loaded before the game (new), NVIDIA Smooth Motion, the video memory
reserve, crowds and fights (2 GB of the game's buffers on the card; 12 GB cards and up), anti-stretch (fewer
stretched bodies, slower), rainbow-face fix (slower), fast loading (experimental), ★ more CPU cores (half the cores
copy the game's memory: +1 FPS on the author's PC).

## Controls

Keyboard and mouse are mapped to the Xbox pad (a connected controller replaces them). Click the game window
to capture the mouse (Alt/F1 releases it). **F** enters vehicles and switches W/S to accelerate/brake (high
beep); in aircraft hold **W** (throttle) and **K** (nose up) to take off, **I** for nose down. Full table:
the launcher's Controls tab or [docs/GUIDE-30FPS.md](docs/GUIDE-30FPS.md).

## Before and after

| | Before (cgbj on this laptop) | Now (Julia's Launcher, Recommended) |
|---|---|---|
| Menus and map | 30-33 FPS | **60 FPS** |
| Open world | 10-15 FPS | **22-26 FPS** |
| Cutscenes | ~26 FPS | **35-38 FPS** |
| Video memory | ~12 GB (over the system's budget) | **6-8 GB** |
| RAM | 27-29 GB | **20-21 GB** |

## FAQ

**The first cutscene lags a lot.** On the first run the game draws everything for the first time: thousands of
pipelines are made and the textures of the whole scene stream in. It runs at 7-15 FPS there, dropping to 2 now
and then, and then settles at 21-25. Press *Prepare shaders* in the launcher first; each session also prepares
the pipelines it used for the next one, so later runs start much faster.

**Is a controller supported?** Yes, an Xbox controller connected to the PC replaces the keyboard and mouse.

**Can it generate more frames, like DLSS Frame Generation?** DLSS FG needs the game's motion vectors, depth
and a picture without the HUD, and VIctory cannot yet tell those apart among the game's buffers. NVIDIA
**Smooth Motion** is the same idea done by the driver from the finished picture (2x), and the launcher turns
it on for you. Tools such as Lossless Scaling can make 3x-4x from any window, but from a base of ~22 FPS that
adds visible lag and artefacts: the real gains still come from the CPU work per frame.

**Why black or flat objects even on a strong PC?** Two causes: textures that have not reached the card yet
(the game's memory is copied to the card by the CPU, and video memory runs out: the Performance page and the
video memory reserve help), and the `_Tiled` volumetrics shader, still being fixed (see below).

## Where it stands

- Works: intro, prison, missions, open world, boats; long sessions without crashes.
- Video memory under the system's budget (6-8 GB used on a 12 GB card), ~20 GB of RAM.
- The CPU is the limit: crowds and fights now run at 14-17 FPS (were 10-15), with 6,000-8,000 draws a frame,
  50-90 MB of the game's memory copied and ~10 ms copied back each frame.
- New: copies of textures whose memory the game has already freed are let go at once (about 1 GB of video
  memory sat in them), and VIctory reserves its video memory with Windows.
- `_Tiled` (water volumetrics): off for now (black squares when on). F9 in the recording mode captures a
  frame, which can be replayed on WARP (`VICTORY_REPLAY_WARP=1`) as one wave of 64 and as two of 32 to find
  the instruction where the two disagree. (Fixed: on recent Windows builds D3D12Core swaps a command list's
  method table when it is closed, which made the recorder crash at start.)
- Known problems: stretched bodies for a frame or two ("spaghetti"), rainbow faces now and then, black
  flickers on characters, no ray tracing.

More: [docs/GUIDE-30FPS.md](docs/GUIDE-30FPS.md) (English guide and what each setting does),
[docs/FSR.md](docs/FSR.md) (AMD's own upscaler and frame generation, and the files you fetch from AMD),
[docs/LUZ-24FPS.md](docs/LUZ-24FPS.md) (Portuguese notes), [docs/replay.md](docs/replay.md) (frame recorder).
Earlier upstream notes: [docs/archive/README-upstream.md](docs/archive/README-upstream.md).

---

Maintained by **@edupode** on GitHub: <https://github.com/edupode>
