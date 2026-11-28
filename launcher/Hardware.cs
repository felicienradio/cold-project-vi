// Julia's Launcher: the PC's hardware, its permanent ID, the hardware-only report (opt-in, write-only),
// the engine (edupode for every card; Belobug's AMD fork as the Radeon alternative), and frame generation for each card.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VIctoryLauncher
{
    partial class LauncherForm
    {
        // ---------------------------------------------------------------- cards and engines
        bool hasNvidia, hasRadeon, rtx30;
        // A GeForce RTX (20 to 50: DLSS runs), and one of the 20 or 30 series (DLSS Frame Generation only with MFG Unlocker).
        bool anyRtx, olderRtx;
        // Every adapter DXGI reports, and whether the PC has only integrated graphics (Intel Iris Xe / UHD, an
        // AMD APU's Radeon Graphics): no memory of its own, far below what the game needs (Gpus.cs).
        List<GpuAdapter> adapters = new List<GpuAdapter>();
        bool integratedOnly;
        string driverVersion = "";
        string radeonName = "";

        // Belobug's AMD fork of VIctory (GPL-3.0): github.com/Belobug/VIctory-Belobug-AMD.
        public const string AmdRepo = "https://github.com/Belobug/VIctory-Belobug-AMD";
        public const string AmdBranch = "a-publier";
        // The commit the launcher was checked with; Setup builds this one.
        public const string AmdCommit = "4eb6cfdf9e61cb112f1f811344ec0dd309eddd6c";

        // Beside the other data, so it never depends on where the launcher sits (Downloads, Program Files).
        string AmdEngineDir { get { return Path.Combine(StorageRoot ?? Path.GetDirectoryName(dataRoot), "engines", "amd"); } }

        // launcher.ini's engine=: edupode (the default, every card: the least video memory and the most
        // stable), or amd (Belobug's AMD engine, the alternative for Radeon cards, chosen by the player).
        // The AMD engine is for Radeon cards only: picked on an NVIDIA card it crashed at start (an RTX 4080 SUPER, 27/11/2026).
        string EngineChoice { get { return ReadSetting("engine") == "amd" && !(hasNvidia && !hasRadeon) ? "amd" : "edupode"; } }

        bool UseAmdEngine { get { return EngineChoice == "amd"; } }

        string EngineName { get { return UseAmdEngine ? "AMD · Belobug" : "edupode"; } }

        // The graphics cards Windows lists (names only).
        void DetectCards()
        {
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Name, AdapterCompatibility FROM Win32_VideoController"))
                    foreach (ManagementObject o in s.Get())
                    {
                        var name = (o["Name"] as string ?? "").Trim();
                        if (name.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) >= 0) hasNvidia = true;
                        // A Radeon card of its own (RX, Pro), not the graphics inside a Ryzen processor.
                        if (Regex.IsMatch(name, @"Radeon\s*(RX|Pro|VII)", RegexOptions.IgnoreCase)) { hasRadeon = true; radeonName = name; }
                    }
            }
            catch (Exception) { }
            adapters = Gpus.List();
            integratedOnly = adapters.Count > 0 && adapters.TrueForAll(a => a.Integrated);
            GpuAdapter best = null;
            foreach (var a in adapters)
                if (best == null || (best.Integrated && !a.Integrated) || (best.Integrated == a.Integrated && a.DedicatedMb > best.DedicatedMb)) best = a;
            if (best != null && vramGb <= 0)
            {
                // Without nvidia-smi (AMD, Intel): the adapter's own memory, never "unknown" read as large.
                vramGb = best.DedicatedMb / 1024.0;
                if (!hasNvidia && !hasRadeon) gpuName = best.Name;
            }
            rtx30 = Regex.IsMatch(gpuName, @"RTX\s*30\d\d");
            // (RTX Ada, Blackwell and RTX PRO workstation cards too, as VIctory-NVIDIA counts them.)
            anyRtx = Regex.IsMatch(gpuName, @"RTX\s*[2-5]0\d\d|RTX\s*A\d{4}|RTX\s*PRO|\bAda\b|Blackwell", RegexOptions.IgnoreCase);
            olderRtx = Regex.IsMatch(gpuName, @"RTX\s*(20|30)\d\d");
            if (!hasNvidia && hasRadeon) gpuName = radeonName.Replace("AMD ", "");
        }

        // `victory gpu` (this repository's build): the card a run gets and whether D3D12 offers it
        // Resizable BAR (GPU upload heaps). Without the build, the BIOS side (nvidia-smi) stands.
        void ProbeGpu()
        {
            var exe = OwnVictoryExe;
            if (exe == null) return;
            new Thread(() => {
                var text = RunCapture(exe, "gpu");
                var kv = new Dictionary<string, string>();
                foreach (var line in text.Split('\n'))
                {
                    var i = line.IndexOf('=');
                    if (i > 0) kv[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
                }
                string v;
                if (!kv.TryGetValue("gpu_upload_heap", out v)) return;
                try
                {
                    BeginInvoke(new Action(() => {
                        rebarHeap = v == "true" ? 1 : 0;
                        string known;
                        probeOptions16 = kv.TryGetValue("options16_known", out known) && known == "true";
                        kv.TryGetValue("adapter", out probeAdapter);
                        kv.TryGetValue("vendor", out probeVendor);
                        probeAdapter = probeAdapter ?? ""; probeVendor = probeVendor ?? "";
                        string mb;
                        double d;
                        if (vramGb <= 0 && kv.TryGetValue("dedicated_mb", out mb) && double.TryParse(mb, out d)) vramGb = d / 1024.0;
                        if (gpuName == "unknown GPU" && probeAdapter.Length > 0) gpuName = probeAdapter;
                        rebar = rebarHeap == 1;
                        UpdateBanner();
                        Refresh2();
                        Log(string.Format(L("Graphics card for VIctory: {0}; Resizable BAR for the game: {1}.", "Placa grafica do VIctory: {0}; Resizable BAR para o jogo: {1}."),
                            probeAdapter, rebarHeap == 1 ? L("yes", "sim") : rebarBios ? L("on in the BIOS, not offered by Windows (see Setup)", "ligado na BIOS, o Windows nao o oferece (ve o Preparar)") : L("no", "nao")));
                    }));
                }
                catch (Exception) { }
            }) { IsBackground = true }.Start();
        }

        // Windows' graphics preference for victory.exe: on a laptop with two cards the run must get
        // the dedicated one (`victory gpu` names the one it gets).
        bool GpuPreferenceOk()
        {
            if (probeAdapter.Length == 0) return true;
            if (hasNvidia && probeAdapter.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) < 0) return false;
            if (!hasNvidia && hasRadeon && !Regex.IsMatch(probeAdapter, @"Radeon\s*(RX|Pro|VII)", RegexOptions.IgnoreCase)) return false;
            return true;
        }

        // "GpuPreference=2;" (high performance) for every runtime executable the launcher may start.
        bool SetGpuPreference()
        {
            try
            {
                using (var k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences"))
                    foreach (var exe in OwnExeCandidates.Concat(new[] {
                                                Path.Combine(AmdEngineDir, "target", "diag", "victory.exe"), Path.Combine(BuildTarget(AmdEngineDir), "diag", "victory.exe"), Path.Combine(AmdEngineDir, "victory.exe") }))
                        k.SetValue(exe, "GpuPreference=2;");
                Log(L("Windows now gives victory.exe the high-performance graphics card (restart the game to apply).", "O Windows da agora ao victory.exe a placa grafica de alto desempenho (reinicia o jogo para aplicar)."));
                return true;
            }
            catch (Exception e) { Log("GPU preference: " + e.Message); return false; }
        }

        bool GpuPreferenceSet()
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences"))
                {
                    var exe = VictoryExe;
                    if (k == null || exe == null) return false;
                    var v = k.GetValue(exe) as string;
                    return v != null && v.Contains("GpuPreference=2");
                }
            }
            catch (Exception) { return false; }
        }

        // The AMD fork's switches (its README): the renderer patches its measured run had on, the
        // frame interval that lets the title pass 30 FPS, keyboard and mouse for GTA, 1080p output.
        Dictionary<string, string> AmdEnvironment()
        {
            var e = new Dictionary<string, string>();
            Action<string, string> set = (k, v) => e[k] = v;
            set("VICTORY_GAME_DIR", gameBox.Text.Trim());
            set("VICTORY_CACHE_DIR", Path.Combine(dataRoot, "cache-amd"));
            set("VICTORY_DATA_DIR", dataDir);
            set("VICTORY_LOG", "warn");
            // The pipelines each game uses are written down, so "Load pipelines before the game" (warm --used)
            // has something to make: without it the AMD engine's warm-up made 0 and every pipeline was
            // compiled during play (hundreds of draws dropped a frame for the first minutes).
            set("VICTORY_PIPELINE_BOOK", "1");
            set("VICTORY_PORT_CONSTANTS", "1");
            set("VICTORY_PORT_DESCRIPTORS", "1");
            set("VICTORY_PORT_CBV", "1");
            set("VICTORY_PORT_GUARD", "1");
            set("VICTORY_PORT_JOURNAL", "1");
            // At most 1080p: Belobug's engine stops at start with a 1440p or 4K output (the game's fatal error at
            // title+0x56a2afd, 7 sessions on RX 6800 and RTX 4080 SUPER, 27/11/2026).
            int h = Math.Min(DisplayHeight, 1080), w = h * 16 / 9;
            set("VICTORY_OUTPUT", w + "x" + h);
            set("VICTORY_WINDOW_MAX", w + "x" + h);
            set("VICTORY_KBM_GTA", "1");
            // AMD cards run 64-lane waves natively: the wave-wide form is the console's own.
            set("VICTORY_WAVE_WIDE", "1");
            if (fps60Box.Checked && !Selected.UltraLow) set("VICTORY_FRAME_INTERVAL_MS", "16.667");
            // The switches both builds share.
            if (panelBox.Checked) set("VICTORY_PANEL", "1");
            if (borderlessBox.Checked) { set("VICTORY_BORDERLESS", "1"); set("VICTORY_BORDERLESS_DPI", "0"); }
            var m = Selected;
            var scale = EffectiveScale(m.Custom ? custom.Scale : m.Scale);
            if (scale != null && scale != "1.0") { set("VICTORY_RENDER_SCALE", scale); set("VICTORY_OWN_UPSCALER", "1"); }
            return e;
        }

        // ---------------------------------------------------------------- frame generation, per card
        string NvSmoothDll { get { return Path.Combine(AppDir, "tools", "nvsmooth30", "version.dll"); } }

        // Which frame generation this PC gets: NVIDIA Smooth Motion (RTX 40/50), NVSmooth30 (RTX 30,
        // when the release brought it), else VIctory's own (VICTORY_VI_FRAMEGEN, any card; this
        // repository's engine only, the AMD engine does not have it yet).
        bool NvSmoothHere { get { return rtx30 && File.Exists(NvSmoothDll) && (FgChoice == "auto" || FgChoice == "nvsmooth30"); } }
        bool OwnFrameGen { get { return !rtxFrameGen && !NvSmoothHere && !UseAmdEngine && (FgChoice == "auto" || FgChoice == "vi"); } }
        // The NVIDIA fork's frame generation, in this repository's engine (`victory run` with the switches
        // `victory play` sets: VendorFrameGen): DLSS Frame Generation on an RTX (RTX 20/30 through MFG Unlocker),
        // AMD FSR 3 on any card of its own memory.
        bool DlssFgHere { get { return FgChoice == "dlss" && anyRtx && !UseAmdEngine; } }
        // FSR 3 only with DLSS upscaling (an RTX): without it the game's own FidelityFX check stopped the game
        // (VIctory-NVIDIA #331, #459; their c1cbdc9 makes it ready only with DLSS on).
        // FSR frame generation needs an upscaling that is not the title's own. DLSS is one; AMD's own
        // upscaler is the other now (VICTORY_FSR, this engine only), so a dedicated Radeon has it too.
        // AMD's frame generation needs an upscaling of ours: DLSS on an RTX, or AMD's own upscaler on a Radeon
        // (which picking this turns on, see PickFg). Were it offered only with the picker already on, a Radeon's
        // owner would look for it and not find it.
        bool FsrFgHere { get { return FgChoice == "fsr" && !UseAmdEngine && !integratedOnly && (anyRtx || hasRadeon || FsrMode != "off"); } }

        // The frame generation the player picked (launcher.ini fg_engine): auto (the best this card has), smooth
        // (NVIDIA Smooth Motion, RTX 40/50), nvsmooth30 (RTX 30, experimental) or vi (VIctory's own, any card,
        // experimental), dlss (DLSS Frame Generation, RTX) or fsr (AMD FSR 3), the last two the NVIDIA fork's. A choice the
        // card cannot have falls to none (the box only lists what it can).
        string FgChoice { get { var v = ReadSetting("fg_engine"); return v == "smooth" || v == "nvsmooth30" || v == "vi" || v == "dlss" || v == "fsr" ? v : "auto"; } }

        ComboBox fgBox;
        readonly List<string> fgKeys = new List<string>();

        // ---------------------------------------------------------------- upscaling, AMD's own
        // Which upscaler does the title's own (VICTORY_FSR): AMD's FSR, at the render scale of the
        // mode picked, in place of the title's own FidelityFX. Off unless asked for: nothing changes
        // for a PC that does not want it.
        string FsrMode
        {
            get
            {
                var v = ReadSetting("fsr_mode");
                return v == "nativeaa" || v == "quality" || v == "balanced" || v == "performance" || v == "ultraperformance" ? v : "off";
            }
        }

        ComboBox fsrBox;
        readonly List<string> fsrKeys = new List<string>();

        void FillFsrBox()
        {
            if (fsrBox == null) return;
            fsrKeys.Clear(); fsrBox.Items.Clear();
            Action<string, string> add = (key, text) => { fsrKeys.Add(key); fsrBox.Items.Add(text); };
            add("off", L("The title's own upscaler (as today)", "O upscaler do proprio jogo (como hoje)", "L'upscaler du jeu (comme aujourd'hui)"));
            // AMD's upscaler is AMD's: on a machine without a Radeon the picker offers nothing it cannot do.
            if (!hasRadeon)
            {
                add("off", L("AMD FSR needs an AMD Radeon (this PC has none)", "O FSR da AMD precisa de uma Radeon (este PC nao tem)", "Le FSR d'AMD demande une Radeon (ce PC n'en a pas)"));
                fsrKeys.RemoveAt(fsrKeys.Count - 1);
                fsrKeys.Add("none");
                fsrBox.Enabled = false;
                fsrBox.SelectedIndex = 0;
                return;
            }
            fsrBox.Enabled = true;
            add("nativeaa", L("AMD FSR, native AA (same size, sharpened)", "AMD FSR, AA nativo (mesmo tamanho, mais nitido)", "AMD FSR, AA natif (meme taille, plus net)"));
            add("quality", L("AMD FSR, quality (2/3 of the screen)", "AMD FSR, qualidade (2/3 do ecra)", "AMD FSR, qualite (2/3 de l'ecran)"));
            add("balanced", L("AMD FSR, balanced (58 %)", "AMD FSR, equilibrado (58 %)", "AMD FSR, equilibre (58 %)"));
            add("performance", L("AMD FSR, performance (1/2)", "AMD FSR, desempenho (1/2)", "AMD FSR, performance (1/2)"));
            add("ultraperformance", L("AMD FSR, ultra performance (1/3)", "AMD FSR, ultra desempenho (1/3)", "AMD FSR, ultra performance (1/3)"));
            var i = fsrKeys.IndexOf(FsrMode);
            fsrBox.SelectedIndex = i < 0 ? 0 : i;
        }

        void PickFsr()
        {
            if (fsrBox == null || fsrBox.SelectedIndex < 0 || fsrBox.SelectedIndex >= fsrKeys.Count) return;
            var key = fsrKeys[fsrBox.SelectedIndex];
            if (key == FsrMode) return;
            ManualChoice();
            WriteSetting("fsr_mode", key);
            Log(L("Upscaling: ", "Upscaling: ", "Upscaling : ") + fsrBox.SelectedItem);
            if (key != "off" && !HasFsrUpscaler)
                Log(L("AMD's FSR files are not in " + FidelityFxDir + ": run scripts\\get-amd-fsr.ps1, or the title's own upscaler runs.",
                      "Os ficheiros do FSR da AMD nao estao em " + FidelityFxDir + ": corre scripts\\get-amd-fsr.ps1, ou corre o upscaler do proprio jogo.",
                      "Les fichiers FSR d'AMD ne sont pas dans " + FidelityFxDir + " : lance scripts\\get-amd-fsr.ps1, ou l'upscaler du jeu s'en charge."));
            UpdateBanner(); Refresh2();
        }

        /// AMD's upscaler in the folder (`VICTORY_FSR`): the upscaling effect and the loader.
        static bool HasFsrUpscaler
        {
            get { return File.Exists(Path.Combine(FidelityFxDir, "amd_fidelityfx_loader_dx12.dll")) && File.Exists(Path.Combine(FidelityFxDir, "amd_fidelityfx_upscaler_dx12.dll")); }
        }

        // What `victory play` sets for AMD's upscaler (docs/FSR.md): the mode's render scale, and the
        // folder AMD's files are in. No render scale of the preset's own: the app patches the title's
        // renderer to the mode's, as it does for DLSS.
        void FsrUpscaling(Dictionary<string, string> e)
        {
            if (FsrMode == "off" || UseAmdEngine || integratedOnly) return;
            // (The files are fetched at PLAY, before the game starts: this runs before that, so it does not
            // wait to see them. Without them the app says so and the title's own upscaler runs.)
            e.Remove("VICTORY_RENDER_SCALE");
            e.Remove("VICTORY_OWN_UPSCALER");
            e["VICTORY_FSR"] = "1";
            e["VICTORY_FSR_MODE"] = FsrMode;
            e["VICTORY_FIDELITYFX_DIR"] = FidelityFxDir;
        }

        // The choices this card has, once the card is known (DetectSystem).
        void FillFgBox()
        {
            if (fgBox == null) return;
            fgKeys.Clear(); fgBox.Items.Clear();
            Action<string, string> add = (key, text) => { fgKeys.Add(key); fgBox.Items.Add(text); };
            add("auto", L("Automatic (the best this card has)", "Automatico (o melhor desta placa)", "Automatique (le meilleur de cette carte)"));
            if (canSmooth) add("smooth", L("NVIDIA Smooth Motion (RTX 40/50)", "NVIDIA Smooth Motion (RTX 40/50)", "NVIDIA Smooth Motion (RTX 40/50)"));
            if (!UseAmdEngine && anyRtx)
                add("dlss", olderRtx ? L("DLSS Frame Generation (RTX 20/30, with MFG Unlocker)", "DLSS Frame Generation (RTX 20/30, com o MFG Unlocker)", "DLSS Frame Generation (RTX 20/30, avec MFG Unlocker)")
                                     : L("DLSS Frame Generation (RTX 40/50, experimental)", "DLSS Frame Generation (RTX 40/50, experimental)", "DLSS Frame Generation (RTX 40/50, expérimental)"));
            if (!UseAmdEngine && !integratedOnly && (anyRtx || hasRadeon || FsrMode != "off"))
                add("fsr", anyRtx ? L("AMD frame generation (with AMD's upscaler, experimental)", "Geracao de frames AMD (com o upscaler AMD, experimental)", "Generation d'images AMD (avec l'upscaler AMD, experimental)")
                                 : L("AMD frame generation 2X (experimental)", "Geracao de frames AMD 2X (experimental)", "Generation d'images AMD 2X (experimental)"));
            if (rtx30 && File.Exists(NvSmoothDll)) add("nvsmooth30", L("NVSmooth30 (RTX 30, experimental)", "NVSmooth30 (RTX 30, experimental)", "NVSmooth30 (RTX 30, expérimental)"));
            if (!UseAmdEngine && !integratedOnly) add("vi", L("VIctory 2X (any card, experimental)", "VIctory 2X (qualquer placa, experimental)", "VIctory 2X (toute carte, expérimental)"));
            var i = fgKeys.IndexOf(FgChoice);
            fgBox.SelectedIndex = i < 0 ? 0 : i;
        }

        void PickFg()
        {
            if (fgBox == null || fgBox.SelectedIndex < 0 || fgBox.SelectedIndex >= fgKeys.Count) return;
            var key = fgKeys[fgBox.SelectedIndex];
            if (key == FgChoice) return;
            if (!AcceptVendorFrameGen(key)) { var back = fgKeys.IndexOf(FgChoice); fgBox.SelectedIndex = back < 0 ? 0 : back; return; }
            ManualChoice();
            WriteSetting("fg_engine", key);
            // FSR frame generation reads an upscaling of ours: on a Radeon that is AMD's own upscaler, so picking
            // it turns the upscaler on rather than asking the person to find the picker first.
            if (key == "fsr" && hasRadeon && FsrMode == "off")
            {
                WriteSetting("fsr_mode", "quality");
                FillFsrBox();
                Log(L("Upscaling: AMD FSR, quality (set by the frame generation choice)", "Upscaling: AMD FSR, qualidade (posto pela escolha da geracao de frames)", "Upscaling : AMD FSR, qualite (choisi par la generation d'images)"));
            }
            rtxFrameGen = canSmooth && (key == "auto" || key == "smooth");
            UpdateFgCard();
            UpdateBanner(); Refresh2();
            Log(L("Frame generation: ", "Geracao de frames: ", "Génération d'images : ") + fgBox.SelectedItem);
        }
        // (VIctory's own frame generation needs memory an integrated GPU does not have.)
        // "dedicated, 12.0 GB of its own" / "integrated, 0.1 GB of its own (shares RAM)", for the hardware page and reports.
        string GpuKind()
        {
            GpuAdapter best = null;
            foreach (var a in adapters) if (best == null || (best.Integrated && !a.Integrated) || (best.Integrated == a.Integrated && a.DedicatedMb > best.DedicatedMb)) best = a;
            double own = best != null ? Math.Max(vramGb, best.DedicatedMb / 1024.0) : vramGb;
            return integratedOnly
                ? string.Format(L("integrated, {0:0.0} GB of its own (shares RAM)", "integrada, {0:0.0} GB proprios (partilha a RAM)", "intégrée, {0:0.0} Go à elle (partage la RAM)"), own)
                : string.Format(L("dedicated, {0:0.0} GB of its own", "dedicada, {0:0.0} GB proprios", "dédiée, {0:0.0} Go à elle"), own);
        }

        // The frame generation card's title: tested (NVIDIA Smooth Motion on an RTX 40/50) or experimental (the rest).
        string FgTitle()
        {
            return (rtxFrameGen ? "★ " : "◇ ") + L("Frame generation", "Geracao de frames", "Génération d'images");
        }

        void UpdateFgCard()
        {
            if (smoothBox == null) return;
            smoothBox.Text = FgTitle();
            if (smoothBox is OptionCard) ((OptionCard)smoothBox).Line = FrameGenLine();
            smoothBox.Invalidate();
        }

        bool FrameGenAvailable { get { return rtxFrameGen || NvSmoothHere || DlssFgHere || FsrFgHere || (OwnFrameGen && !integratedOnly); } }

        string FrameGenLine()
        {
            if (rtxFrameGen) return L("NVIDIA Smooth Motion (RTX 40/50, set up for you)", "NVIDIA Smooth Motion (RTX 40/50, configurado por ti)");
            if (NvSmoothHere) return L("NVSmooth30 for RTX 30 (experimental)", "NVSmooth30 para RTX 30 (experimental)");
            if (DlssFgHere) return olderRtx ? L("DLSS Frame Generation through MFG Unlocker (RTX 20/30, experimental)", "DLSS Frame Generation pelo MFG Unlocker (RTX 20/30, experimental)", "DLSS Frame Generation par MFG Unlocker (RTX 20/30, expérimental)")
                                            : L("DLSS Frame Generation 2X, with DLSS upscaling", "DLSS Frame Generation 2X, com o upscaling DLSS", "DLSS Frame Generation 2X, avec l'upscaling DLSS");
            // (Which model runs is AMD's API's to say and the log's to print: ffxQuery answers 4.0.2 here,
            // the ML frame generation of FidelityFX SDK 2.4 on RDNA 4, not the 3.x one the fork's name suggests.)
            if (FsrFgHere) return L("AMD frame generation 2X (FidelityFX SDK 2.4: ML on RDNA 4)", "Geracao de frames AMD 2X (FidelityFX SDK 2.4: ML nas RDNA 4)", "Génération d'images AMD 2X (FidelityFX SDK 2.4 : ML sur RDNA 4)");
            if (OwnFrameGen) return L("VI frame generation: any card, a frame between each two (experimental)", "Geracao de frames VI: qualquer placa, um frame entre cada dois (experimental)");
            return L("Not in the AMD engine yet (pick the edupode engine on Hardware & ID to try it)", "Ainda nao no motor AMD (escolhe o motor edupode em Hardware e ID para experimentar)");
        }

        string FrameGenChip()
        {
            if (rtxFrameGen) return "Smooth Motion " + (smoothState == "on" ? L("On", "ligado") : L("ready", "pronto"));
            if (NvSmoothHere) return "NVSmooth30 " + (smoothBox.Checked ? L("On", "ligado") : L("off", "desligado"));
            if (DlssFgHere) return "DLSS FG " + (smoothBox.Checked ? L("On", "ligada") : L("off", "desligada"));
            if (FsrFgHere) return "AMD FG " + (smoothBox.Checked ? L("On", "ligada") : L("off", "desligada"));
            if (OwnFrameGen) return L("VI frame gen ", "Geracao VI ") + (smoothBox.Checked ? L("On", "ligada") : L("off", "desligada"));
            return L("Frame gen n/a", "Geracao de frames n/d");
        }

        // Before PLAY: the driver profile (Smooth Motion), and on RTX 30 NVSmooth30's version.dll
        // beside victory.exe (taken away again when the game closes: RemoveNvSmooth).
        void ApplyFrameGen()
        {
            // Another frame generation picked: the driver's Smooth Motion goes off, or the frames would be doubled twice.
            if (canSmooth && !rtxFrameGen && smoothState == "on") ApplySmooth(false, false);
            if (rtxFrameGen)
            {
                if (smoothBox.Checked && smoothState != "on") ApplySmooth(true, false);
                else if (!smoothBox.Checked && smoothState == "on") ApplySmooth(false, false);
                return;
            }
            if (!rtx30 || !File.Exists(NvSmoothDll) || VictoryExe == null) return;
            var beside = Path.Combine(Path.GetDirectoryName(VictoryExe), "version.dll");
            try
            {
                if (smoothBox.Checked && NvSmoothHere)
                {
                    File.Copy(NvSmoothDll, beside, true);
                    if (smoothState != "on") ApplySmooth(true, false);
                    Log(L("NVSmooth30 (experimental) is on for this session.", "NVSmooth30 (experimental) ligado nesta sessao."));
                }
                else RemoveNvSmooth();
            }
            catch (Exception e) { Log("NVSmooth30: " + e.Message); }
        }

        void RemoveNvSmooth()
        {
            try
            {
                if (VictoryExe == null) return;
                var beside = Path.Combine(Path.GetDirectoryName(VictoryExe), "version.dll");
                if (File.Exists(beside)) File.Delete(beside);
            }
            catch (Exception) { }
        }

        // ---------------------------------------------------------------- DLSS Frame Generation and FSR 3 (the NVIDIA fork's)
        // Where victory.exe's get-dlss, get-fsr and get-mfg put NVIDIA's, AMD's and MFG Unlocker's files
        // (apps\victory\src\play.rs: %LOCALAPPDATA%\victory, or the folder VICTORY_HOME names).
        static string VictoryHome
        {
            get
            {
                var named = Environment.GetEnvironmentVariable("VICTORY_HOME");
                return !string.IsNullOrEmpty(named) ? named : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "victory");
            }
        }
        static string StreamlineDir { get { return Path.Combine(VictoryHome, "nvidia-sdk", "Streamline", "bin", "x64"); } }
        static string FidelityFxDir { get { return Path.Combine(VictoryHome, "amd-fidelityfx"); } }
        static string MfgDll { get { return Path.Combine(VictoryHome, "mfg-unlocker", "MFGUnlocker.dll"); } }
        static bool HasStreamline { get { return File.Exists(Path.Combine(StreamlineDir, "sl.interposer.dll")); } }
        static bool HasFsr
        {
            get { return File.Exists(Path.Combine(FidelityFxDir, "amd_fidelityfx_loader_dx12.dll")) && File.Exists(Path.Combine(FidelityFxDir, "amd_fidelityfx_framegeneration_dx12.dll")); }
        }

        // DLSS upscales for both: DLSS Frame Generation reads its inputs, and FSR 3 needs it (FsrFgHere).
        bool VendorUpscaling { get { return anyRtx && (DlssFgHere || FsrFgHere); } }
        bool FsrBlocked { get { return false; } }

        // Asked once when DLSS Frame Generation or FSR 3 is picked: what is downloaded, from where, and (RTX 20/30)
        // MFG Unlocker, someone else's mod. false: the player said no, the choice goes back.
        bool AcceptVendorFrameGen(string key)
        {
            if (key != "dlss" && key != "fsr") return true;
            bool mfg = key == "dlss" && olderRtx;
            var seen = "fg_ok_" + key + (mfg ? "_mfg" : "");
            if (ReadSetting(seen) == "1") { smoothBox.Checked = true; return true; }
            var lines = new List<string>();
            if (key == "dlss")
            {
                lines.Add(L("DLSS Frame Generation (from the VIctory-NVIDIA fork): the card makes frames between the game's, from its depth and motion. DLSS also does the upscaling.",
                            "DLSS Frame Generation (do fork VIctory-NVIDIA): a placa cria frames entre os do jogo, a partir da profundidade e do movimento. O DLSS tambem faz o upscaling.",
                            "DLSS Frame Generation (du fork VIctory-NVIDIA) : la carte crée des images entre celles du jeu, à partir de la profondeur et du mouvement. DLSS fait aussi l'upscaling."));
                if (mfg)
                {
                    lines.Add("");
                    lines.Add(L("This card (" + gpuName + ") is an RTX 20/30: NVIDIA's frame generation runs only on RTX 40 and later. MFG Unlocker, someone else's mod (github.com/dashdogy/MFG-Unlocker, MIT licence, about 5 MB), unlocks it. NVIDIA does not support that, and it was not tested with this game.",
                                "Esta placa (" + gpuName + ") e uma RTX 20/30: a geracao de frames da NVIDIA so corre nas RTX 40 e mais recentes. O MFG Unlocker, um mod de outra pessoa (github.com/dashdogy/MFG-Unlocker, licenca MIT, cerca de 5 MB), desbloqueia-a. A NVIDIA nao suporta isto, e nao foi testado com este jogo.",
                                "Cette carte (" + gpuName + ") est une RTX 20/30 : la génération d'images de NVIDIA ne tourne que sur les RTX 40 et plus récentes. MFG Unlocker, le mod de quelqu'un d'autre (github.com/dashdogy/MFG-Unlocker, licence MIT, environ 5 Mo), la débloque. NVIDIA ne le prend pas en charge, et il n'a pas été testé avec ce jeu."));
                }
            }
            else
            {
                lines.Add(L("AMD frame generation, FidelityFX SDK 2.4's: a frame made between each two of the game's (2X). AMD's own API reports which model it runs; on an RDNA 4 card such as this one it is the ML one (4.0.2), not the 3.x generation.",
                            "Geracao de frames AMD, do FidelityFX SDK 2.4: um frame criado entre cada dois do jogo (2X). A API da AMD diz que modelo corre; numa placa RDNA 4 como esta e o de ML (4.0.2), nao o da geracao 3.x.",
                            "Génération d'images AMD, celle du FidelityFX SDK 2.4 : une image créée entre chaque deux du jeu (2X). C'est l'API d'AMD qui dit quel modèle tourne ; sur une carte RDNA 4 comme celle-ci, c'est celui par ML (4.0.2), pas la génération 3.x."));
                lines.Add(L("DLSS does the upscaling it reads (an RTX is needed: without DLSS the game stops).", "O DLSS faz o upscaling que ela le (e preciso uma RTX: sem DLSS o jogo para).", "DLSS fait l'upscaling qu'elle lit (une RTX est nécessaire : sans DLSS le jeu s'arrête)."));
            }
            lines.Add("");
            lines.Add(L("At PLAY, what is missing is downloaded once into " + VictoryHome + ":", "No JOGAR, o que faltar e descarregado uma vez para " + VictoryHome + ":", "Au JOUER, ce qui manque est téléchargé une fois dans " + VictoryHome + " :"));
            if (anyRtx) lines.Add(L("   •  NVIDIA's DLSS files, NVIDIA's own licence, github.com/NVIDIA-RTX/Streamline (about 70 MB)", "   •  os ficheiros DLSS da NVIDIA, licenca da NVIDIA, github.com/NVIDIA-RTX/Streamline (cerca de 70 MB)", "   •  les fichiers DLSS de NVIDIA, licence de NVIDIA, github.com/NVIDIA-RTX/Streamline (environ 70 Mo)"));
            if (key == "fsr") lines.Add(L("   •  AMD's FidelityFX SDK 2.4 files and upscaler, AMD's own licence, github.com/GPUOpen-LibrariesAndSDKs/FidelityFX-SDK (about 88 MB)", "   •  os ficheiros e o upscaler do FidelityFX SDK 2.4 da AMD, licenca da AMD, github.com/GPUOpen-LibrariesAndSDKs/FidelityFX-SDK (cerca de 88 MB)", "   •  les fichiers et l'upscaler du FidelityFX SDK 2.4 d'AMD, licence d'AMD, github.com/GPUOpen-LibrariesAndSDKs/FidelityFX-SDK (environ 88 Mo)"));
            if (mfg) lines.Add(L("   •  MFG Unlocker, from its author's GitHub release", "   •  o MFG Unlocker, da release do autor no GitHub", "   •  MFG Unlocker, depuis la release GitHub de son auteur"));
            lines.Add("");
            lines.Add(L("Experimental here. It replaces the preset's own upscaler (its world resolution becomes the DLSS mode nearest to it). Changes made in the game's F1 menu are not kept when the launcher starts the game.",
                        "Experimental aqui. Substitui o upscaler do preset (a resolucao do mundo passa ao modo DLSS mais proximo). As mudancas feitas no menu F1 do jogo nao ficam guardadas quando o launcher arranca o jogo.",
                        "Expérimental ici. Remplace l'upscaler du preset (sa résolution du monde devient le mode DLSS le plus proche). Les changements faits dans le menu F1 du jeu ne sont pas gardés quand le launcher lance le jeu."));
            var answer = Notice(key == "dlss" ? "DLSS Frame Generation" : "AMD frame generation (FidelityFX SDK 2.4)", string.Join("\r\n", lines.ToArray()),
                mfg ? L("Use it, with MFG Unlocker", "Usar, com o MFG Unlocker", "L'utiliser, avec MFG Unlocker") : L("Use it", "Usar", "L'utiliser"),
                L("Cancel", "Cancelar", "Annuler"));
            if (answer != 1) return false;
            WriteSetting(seen, "1");
            smoothBox.Checked = true;
            return true;
        }

        // The DLSS mode whose scene size is nearest the preset's scale. The game's own FidelityFX (FSR 3 without
        // an RTX) takes only 720p or 1080p into 1080p: quality or dlaa there.
        static string DlssModeFor(string scale, bool dlss)
        {
            double s = 1.0;
            if (scale != null) double.TryParse(scale, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out s);
            if (s <= 0) s = 1.0;
            if (!dlss) return s >= 0.95 ? "dlaa" : "quality";
            return s >= 0.95 ? "dlaa" : s >= 0.62 ? "quality" : s >= 0.55 ? "balanced" : s >= 0.45 ? "performance" : "ultraperformance";
        }

        // What `victory play` sets for DLSS Frame Generation or FSR 3 (play.rs, settings), for the launcher's
        // `victory run`. Only with the files in place (FetchFrameGenFiles, at PLAY): without them, nothing.
        void VendorFrameGen(Dictionary<string, string> e)
        {
            if (!smoothBox.Checked || (!DlssFgHere && !FsrFgHere) || FsrBlocked) return;
            bool dlss = VendorUpscaling;
            if (dlss && !HasStreamline) return;
            if (FsrFgHere && !HasFsr) return;
            string scale;
            e.TryGetValue("VICTORY_RENDER_SCALE", out scale);
            var mode = DlssModeFor(scale, dlss);
            // The game's FidelityFX path, which DLSS and FSR 3 read, in place of its own upscaler; the scene's
            // size is the DLSS mode's.
            e.Remove("VICTORY_RENDER_SCALE");
            e.Remove("VICTORY_OWN_UPSCALER");
            e["VICTORY_DLSS_MODE"] = mode;
            if (dlss) { e["VICTORY_DLSS"] = "1"; e["VICTORY_STREAMLINE_DIR"] = StreamlineDir; }
            if (DlssFgHere) e["VICTORY_FRAMEGEN"] = "1";
            else
            {
                e["VICTORY_FSR_FRAMEGEN"] = "1";
                e["VICTORY_FIDELITYFX_DIR"] = FidelityFxDir;
                // Without DLSS, AMD's upscaler is what frame generation reads from (the title's own
                // stops the game without one).
                if (!dlss) { e["VICTORY_FSR"] = "1"; e["VICTORY_FSR_MODE"] = mode; }
            }
            e["VICTORY_FRAMEGEN_X"] = "2";
            // Frame generation takes 1.5 to 2 GB of the card's memory, which the layer otherwise fills with textures.
            e["VICTORY_TEXTURE_ROOM_PERCENT"] = "80";
            if (DlssFgHere && olderRtx && File.Exists(MfgDll)) { e["VICTORY_MFG_UNLOCKER"] = "1"; e["VICTORY_MFG_UNLOCKER_DLL"] = MfgDll; }
            // For the F1 menu: what it shows as the current choices.
            e["VICTORY_MENU_CONSOLE"] = "series-s";
            e["VICTORY_MENU_DLSS"] = dlss ? mode : "off";
            e["VICTORY_MENU_FRAMEGEN"] = "2";
            e["VICTORY_MENU_FG_TYPE"] = DlssFgHere ? "dlss" : "fsr";
        }

        // At PLAY (a worker thread), before the game: what DLSS Frame Generation or FSR 3 needs and is missing,
        // fetched by victory.exe (get-dlss, get-fsr, get-mfg). One that does not arrive leaves the game without it.
        void FetchFrameGenFiles(string log, bool on)
        {
            if (!on || (!DlssFgHere && !FsrFgHere && FsrMode == "off")) return;
            if (FsrBlocked)
            {
                Log(L("AMD frame generation needs the 1080p screen size here: this game runs without frame generation.", "A geracao de frames da AMD precisa aqui do ecra a 1080p: este jogo corre sem geracao de frames.", "La génération d'images AMD demande ici l'écran en 1080p : ce jeu tourne sans génération d'images."));
                return;
            }
            // (`VICTORY_FSR` needs the upscaler too; frame generation needs its own two files.)
            bool fsrUp = FsrMode != "off";
            Func<string, string, Func<bool>, bool> get = (command, what, have) => {
                if (have()) return true;
                if (ClosingNow()) return false;
                Log(L("Downloading ", "A descarregar ", "Téléchargement de ") + what + "...");
                try { var p = Start(command, log); p.WaitForExit(); } catch (Exception ex) { Log(what + ": " + ex.Message); }
                var ok = have();
                Log(ok ? what + L(": ready.", ": pronto.", " : prêt.")
                       : what + L(" did not arrive (see the session log): the game runs without it.", " nao chegou (ve o log da sessao): o jogo corre sem isso.", " n'est pas arrivé (voir le log de la session) : le jeu tourne sans."));
                return ok;
            };
            if (VendorUpscaling && !get("get-dlss", L("NVIDIA's DLSS files", "os ficheiros DLSS da NVIDIA", "les fichiers DLSS de NVIDIA"), () => HasStreamline)) return;
            if (FsrFgHere || fsrUp)
                get("get-fsr", L("AMD's FSR files", "os ficheiros FSR da AMD", "les fichiers FSR d'AMD"),
                    () => HasFsr && (!fsrUp || HasFsrUpscaler));
            if (DlssFgHere && olderRtx) get("get-mfg", "MFG Unlocker", () => File.Exists(MfgDll));
        }

        // ---------------------------------------------------------------- hardware, ID and the report
        class HardwareInfo
        {
            public string Id = "", Gpu = "", GpuVendor = "", Driver = "", Cpu = "", Board = "", BoardMaker = "", Os = "", Disk = "";
            public int VramMb, RamGb, Cores, Threads;
            public bool Laptop, RebarBios;
            public int RebarHeap = -1;
        }

        HardwareInfo hw;
        Label idLabel;
        InfoCard hwText;
        TextBox hwJson;
        CheckBox shareBox;
        Label shareStatus;
        ComboBox engineBox;

        // Where the hardware report goes (server\hardware-worker). Empty: the database is not online
        // yet, and nothing is sent.
        public const string HardwareEndpoint = "https://vi-hardware.edupodept.workers.dev/v1/hw";

        static string Wmi(string query, string field)
        {
            try
            {
                using (var s = new ManagementObjectSearcher(query))
                    foreach (ManagementObject o in s.Get())
                    {
                        var v = o[field];
                        if (v != null && v.ToString().Trim().Length > 0) return v.ToString().Trim();
                    }
            }
            catch (Exception) { }
            return "";
        }

        static string WmiIn(string scope, string query, string field)
        {
            try
            {
                using (var s = new ManagementObjectSearcher(new ManagementScope(scope), new ObjectQuery(query)))
                    foreach (ManagementObject o in s.Get())
                    {
                        var v = o[field];
                        if (v != null) return v.ToString().Trim();
                    }
            }
            catch (Exception) { }
            return "";
        }

        HardwareInfo ReadHardware()
        {
            var h = new HardwareInfo();
            // The ID: a hash of parts that stay when Windows is installed again (the board's UUID and
            // serial, the CPU's ID); none of them leaves the PC.
            var uuid = Wmi("SELECT UUID FROM Win32_ComputerSystemProduct", "UUID");
            var serial = Wmi("SELECT SerialNumber FROM Win32_BaseBoard", "SerialNumber");
            var cpuId = Wmi("SELECT ProcessorId FROM Win32_Processor", "ProcessorId");
            h.Id = MakeId(uuid + "|" + serial + "|" + cpuId);
            h.Gpu = hasNvidia || !hasRadeon ? gpuName : radeonName;
            h.GpuVendor = hasNvidia ? "NVIDIA" : hasRadeon ? "AMD" : probeVendor;
            h.Driver = driverVersion;
            h.VramMb = (int)(vramGb * 1024);
            h.RebarBios = rebarBios;
            h.RebarHeap = rebarHeap;
            h.Cpu = Wmi("SELECT Name FROM Win32_Processor", "Name");
            int n;
            if (int.TryParse(Wmi("SELECT NumberOfCores FROM Win32_Processor", "NumberOfCores"), out n)) h.Cores = n;
            if (int.TryParse(Wmi("SELECT NumberOfLogicalProcessors FROM Win32_Processor", "NumberOfLogicalProcessors"), out n)) h.Threads = n;
            h.RamGb = (int)Math.Round(ramGb);
            h.BoardMaker = Wmi("SELECT Manufacturer FROM Win32_BaseBoard", "Manufacturer");
            h.Board = Wmi("SELECT Product FROM Win32_BaseBoard", "Product");
            h.Os = Wmi("SELECT Caption FROM Win32_OperatingSystem", "Caption") + " " + Wmi("SELECT BuildNumber FROM Win32_OperatingSystem", "BuildNumber");
            // The system drive's kind: 3 HDD, 4 SSD (Storage's MSFT_PhysicalDisk).
            var media = WmiIn(@"\\.\root\Microsoft\Windows\Storage", "SELECT MediaType FROM MSFT_PhysicalDisk WHERE DeviceId='0'", "MediaType");
            h.Disk = media == "4" ? "SSD" : media == "3" ? "HDD" : "unknown";
            var chassis = Wmi("SELECT ChassisTypes FROM Win32_SystemEnclosure", "ChassisTypes");
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT ChassisTypes FROM Win32_SystemEnclosure"))
                    foreach (ManagementObject o in s.Get())
                    {
                        var types = o["ChassisTypes"] as ushort[];
                        if (types != null && types.Any(t => t == 8 || t == 9 || t == 10 || t == 14 || t == 31 || t == 32)) h.Laptop = true;
                    }
            }
            catch (Exception) { }
            return h;
        }

        // VI-XXXX-XXXX: the first 40 bits of a SHA-256 in base 32 (no 0/O, 1/I).
        static string MakeId(string parts)
        {
            const string digits = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            byte[] hash;
            using (var sha = SHA256.Create()) hash = sha.ComputeHash(Encoding.UTF8.GetBytes(parts + "|cold-project-vi"));
            ulong v = 0;
            for (int i = 0; i < 5; i++) v = v << 8 | hash[i];
            var sb = new StringBuilder("VI-");
            for (int i = 7; i >= 0; i--)
            {
                sb.Append(digits[(int)(v >> (i * 5) & 31)]);
                if (i == 4) sb.Append('-');
            }
            return sb.ToString();
        }

        static string Json(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (var c in s ?? "")
            {
                if (c == '"' || c == '\\') sb.Append('\\').Append(c);
                else if (c < 32) sb.AppendFormat("\\u{0:x4}", (int)c);
                else sb.Append(c);
            }
            return sb.Append('"').ToString();
        }

        // The presets by their English names, the same whatever the launcher's language (for the averages).
        static readonly string[] ModeKeys = { "Recommended", "Fastest", "Modest PC", "Sharp", "Classic cgbj", "Ultra Low", "Custom" };

        // A game's FPS from its log: the engine's "victory_gpu::host: N fps, ..." lines every 5 seconds, the first
        // 90 seconds left out (loading). Kept for the next hardware report only after 3 minutes or more of play:
        // the median and the slowest 5 %, the preset, the engine and this launcher's version.
        void KeepFps(string log, DateTime started, DateTime ended)
        {
            try
            {
                if ((ended - started).TotalMinutes < 3 || log == null || !File.Exists(log)) return;
                var fps = new List<double>();
                var ansi = new Regex("\x1b\\[[0-9;]*m");
                var line = new Regex(@"victory_gpu::host:\s+([0-9]+(?:\.[0-9]+)?) fps,");
                var stamp = new Regex(@"^(\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d)");
                DateTime first = DateTime.MinValue;
                using (var f = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var r = new StreamReader(f))
                {
                    string raw;
                    while ((raw = r.ReadLine()) != null)
                    {
                        if (raw.IndexOf(" fps,", StringComparison.Ordinal) < 0) continue;
                        var l = ansi.Replace(raw, "");
                        var m = line.Match(l);
                        if (!m.Success) continue;
                        var t = stamp.Match(l);
                        DateTime at;
                        if (t.Success && DateTime.TryParse(t.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out at))
                        {
                            if (first == DateTime.MinValue) first = at;
                            if ((at - first).TotalSeconds < 90) continue;
                        }
                        double v;
                        if (double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out v) && v > 0 && v < 1000) fps.Add(v);
                    }
                }
                if (fps.Count < 12) return; // under a minute of measured play
                fps.Sort();
                double median = fps[fps.Count / 2], low = fps[(int)(fps.Count * 0.05)];
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                WriteSetting("fps_median", median.ToString("0.0", inv));
                WriteSetting("fps_low", low.ToString("0.0", inv));
                WriteSetting("fps_minutes", Math.Round((ended - started).TotalMinutes).ToString(inv));
                WriteSetting("fps_preset", modeIndex >= 0 && modeIndex < ModeKeys.Length ? ModeKeys[modeIndex] : "Recommended");
                WriteSetting("fps_engine", UseAmdEngine ? "amd" : (ReadSetting("engine") ?? "edupode"));
                WriteSetting("fps_version", Updater.Version);
                // Sent with the next hardware report, even on a day one was already sent.
                WriteSetting("share_sent", "");
                Log(string.Format(inv, L("This game: {0:0.0} FPS median, {1:0.0} in the slowest 5 %.", "Este jogo: {0:0.0} FPS de mediana, {1:0.0} nos 5 % mais lentos.", "Cette partie : {0:0.0} FPS médians, {1:0.0} dans les 5 % les plus lents."), median, low));
            }
            catch (Exception) { }
        }

        // The FPS part of the report: the last game kept by KeepFps, or nothing.
        string FpsJson()
        {
            var median = ReadSetting("fps_median");
            if (string.IsNullOrEmpty(median)) return "";
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            Func<string, string> num = k => { double v; return double.TryParse(ReadSetting(k), System.Globalization.NumberStyles.Float, inv, out v) && v >= 0 && v <= 1000 ? v.ToString("0.0", inv) : "0"; };
            Func<string, string> str = k => { var v = ReadSetting(k) ?? ""; return Json(v.Length > 40 ? v.Substring(0, 40) : v); };
            return ",\n  \"fps_median\": " + num("fps_median") + ",\n  \"fps_low\": " + num("fps_low") + ",\n  \"fps_minutes\": " + num("fps_minutes")
                + ",\n  \"fps_preset\": " + str("fps_preset") + ",\n  \"fps_engine\": " + str("fps_engine") + ",\n  \"fps_version\": " + str("fps_version");
        }

        // Exactly what the report holds: hardware and the ID, nothing else (and the last game's FPS, FpsJson).
        static string HardwareJson(HardwareInfo h)
        {
            return HardwareJson(h, "");
        }

        static string HardwareJson(HardwareInfo h, string fps)
        {
            return "{\n"
                + "  \"schema\": 1,\n"
                + "  \"id\": " + Json(h.Id) + ",\n"
                + "  \"gpu\": " + Json(h.Gpu) + ",\n"
                + "  \"gpu_vendor\": " + Json(h.GpuVendor) + ",\n"
                + "  \"gpu_driver\": " + Json(h.Driver) + ",\n"
                + "  \"vram_mb\": " + h.VramMb + ",\n"
                + "  \"rebar_bios\": " + (h.RebarBios ? "true" : "false") + ",\n"
                + "  \"rebar_d3d12\": " + (h.RebarHeap == 1 ? "true" : h.RebarHeap == 0 ? "false" : "null") + ",\n"
                + "  \"cpu\": " + Json(h.Cpu) + ",\n"
                + "  \"cpu_cores\": " + h.Cores + ",\n"
                + "  \"cpu_threads\": " + h.Threads + ",\n"
                + "  \"ram_gb\": " + h.RamGb + ",\n"
                + "  \"board_maker\": " + Json(h.BoardMaker) + ",\n"
                + "  \"board\": " + Json(h.Board) + ",\n"
                + "  \"os\": " + Json(h.Os) + ",\n"
                + "  \"disk\": " + Json(h.Disk) + ",\n"
                + "  \"laptop\": " + (h.Laptop ? "true" : "false") + fps + "\n"
                + "}";
        }

        // A random secret kept only on this PC: the database takes a newer report for this ID only
        // with it, so knowing someone's ID is not enough to change their entry.
        string ShareSecret()
        {
            var file = Path.Combine(Path.GetDirectoryName(dataRoot), "hardware-secret.txt");
            try
            {
                if (File.Exists(file)) { var s = File.ReadAllText(file).Trim(); if (s.Length == 64) return s; }
                var bytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
                var hex = string.Concat(bytes.Select(b => b.ToString("x2")).ToArray());
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllText(file, hex);
                return hex;
            }
            catch (Exception) { return null; }
        }

        // Sends the report (POST only; the answer is read for its status and nothing else), at most
        // once a day, when the player turned sharing on.
        void StartHardwareShare()
        {
            if (ReadSetting("share") != "1" || HardwareEndpoint.Length == 0) return;
            var last = ReadSetting("share_sent");
            if (last == DateTime.UtcNow.ToString("yyyy-MM-dd")) return;
            new Thread(() => {
                try
                {
                    Thread.Sleep(15000);
                    var h = ReadHardware();
                    var secret = ShareSecret();
                    if (secret == null) return;
                    ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
                    var req = (HttpWebRequest)WebRequest.Create(HardwareEndpoint);
                    req.Method = "POST";
                    req.ContentType = "application/json";
                    req.UserAgent = "Julias-Launcher";
                    req.Timeout = 15000;
                    req.Headers["X-VI-Secret"] = secret;
                    var body = Encoding.UTF8.GetBytes(HardwareJson(h, FpsJson()));
                    req.ContentLength = body.Length;
                    using (var s = req.GetRequestStream()) s.Write(body, 0, body.Length);
                    using (var r = (HttpWebResponse)req.GetResponse())
                        if ((int)r.StatusCode / 100 == 2) WriteSetting("share_sent", DateTime.UtcNow.ToString("yyyy-MM-dd"));
                }
                catch (Exception) { }
            }) { IsBackground = true }.Start();
        }

        // A label gets the height its text needs at its width (never less than a few lines) and its
        // bottom edge comes back, so the buttons under it never overlap long Portuguese or French text.
        static int FitText(Label l, int top)
        {
            int need = TextRenderer.MeasureText(l.Text ?? "", l.Font, new Size(l.Width, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + 8;
            l.Height = Math.Max(60, need);
            return top + l.Height;
        }

        // The question before each game while sharing is off: why it helps, what the list is, what
        // goes and what never does. "Don't ask again" ends it; Hardware & ID changes it any time.
        void AskToShare()
        {
            // Never asked while the database is not online (HardwareEndpoint empty): nothing would be sent.
            if (HardwareEndpoint.Length == 0 || ReadSetting("share") == "1" || ReadSetting("share_ask") == "never") return;
            var f = new Form { Text = L("Help the game run on more PCs", "Ajuda o jogo a correr em mais PCs"), ClientSize = new Size(660, 520), BackColor = Theme.Back, ForeColor = Theme.Text,
                Font = Theme.Ui(9.75f), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Icon = Icon };
            f.Controls.Add(new Heading(L("Share your hardware?", "Partilhar o teu hardware?")) { Location = new Point(20, 16), Width = 620 });
            var text = new Label { Location = new Point(20, 50), Size = new Size(620, 390), Font = f.Font, ForeColor = Theme.Text, BackColor = Theme.Back };
            text.Text = string.Join("\r\n", Pt ? new[] {
                "Podes deixar o Julia's Launcher enviar o hardware do teu PC ao edupode?",
                "",
                "PORQUE AJUDA",
                "   Vemos em que placas graficas, processadores e quantidades de memoria as pessoas jogam de verdade.",
                "   Isso decide que predefinicoes vem primeiro, que correcoes se fazem a seguir (por exemplo o Ultra baixo",
                "   para PCs de 16 GB, o suporte AMD) e em que placas se testa a geracao de frames.",
                "",
                "A LISTA",
                "   Cada PC que partilha e uma linha, sem nome. So o edupode ve a lista.",
                "   Uma pagina publica mostra so as medias (quantos PCs, RAM e VRAM medias, as placas mais comuns),",
                "   atualizada de 6 em 6 horas: docs/HARDWARE-STATS.md no GitHub.",
                "",
                "O QUE E ENVIADO",
                "   Placa grafica e driver, VRAM, Resizable BAR, processador, RAM, modelo da placa-mae,",
                "   versao do Windows, tipo de disco, se e portatil, e o teu ID VI. No maximo uma vez por dia.",
                "",
                "NUNCA E ENVIADO",
                "   Logs, definicoes, caminhos de ficheiros, o teu nome, o teu IP.",
                "",
                "Podes desligar quando quiseres em Hardware e ID.",
            } : Fr ? new[] {
                "Julia's Launcher peut-il envoyer le matériel de ton PC à edupode ?",
                "",
                "POURQUOI ÇA AIDE",
                "   On voit sur quelles cartes graphiques, quels processeurs et quelles quantités de mémoire les gens jouent vraiment.",
                "   Ça décide quels préréglages passent en premier, quels correctifs viennent ensuite (par exemple Ultra bas pour",
                "   les PC de 16 Go, le support AMD) et sur quelles cartes la génération d'images est testée.",
                "",
                "LA LISTE",
                "   Chaque PC qui partage est une ligne, sans nom. Seul edupode voit la liste.",
                "   Une page publique ne montre que les moyennes (combien de PC, RAM et VRAM moyennes, les cartes les plus",
                "   courantes), mise à jour toutes les 6 heures : docs/HARDWARE-STATS.md sur GitHub.",
                "",
                "CE QUI EST ENVOYÉ",
                "   Carte graphique et pilote, VRAM, Resizable BAR, processeur, RAM, modèle de carte mère,",
                "   version de Windows, type de disque, portable ou non, et ton ID VI. Au plus une fois par jour.",
                "",
                "JAMAIS ENVOYÉ",
                "   Logs, réglages, chemins de fichiers, ton nom, ton adresse IP.",
                "",
                "Tu peux le désactiver à tout moment dans Matériel et ID.",
            } : new[] {
                "Can Julia's Launcher send your PC's hardware to edupode?",
                "",
                "WHY IT HELPS",
                "   We see which graphics cards, processors and amounts of memory people really play on. That decides",
                "   which presets come first, which fixes come next (for example Ultra Low for 16 GB PCs, AMD support)",
                "   and which cards frame generation is tested on.",
                "",
                "THE LIST",
                "   Each PC that shares is one line, with no name. Only edupode sees the list.",
                "   A public page shows only the averages (how many PCs, average RAM and video memory, the most",
                "   common cards), updated every 6 hours: docs/HARDWARE-STATS.md on GitHub.",
                "",
                "WHAT IS SENT",
                "   Graphics card and driver, video memory, Resizable BAR, processor, RAM, motherboard model,",
                "   Windows version, disk type, laptop or not, and your VI ID. At most once a day.",
                "",
                "NEVER SENT",
                "   Logs, settings, file paths, your name, your IP address.",
                "",
                "You can turn it off any time on Hardware & ID.",
            });
            // The text takes the height it needs (Portuguese and French wrap more); the buttons follow it.
            int yb = FitText(text, 50) + 16;
            f.ClientSize = new Size(660, yb + 62);
            f.Controls.Add(text);
            var yes = new Pill { Main = true, Text = L("Yes, share my hardware", "Sim, partilhar o meu hardware"), Location = new Point(20, yb), Size = new Size(250, 42) };
            var later = new Pill { Text = L("Not now", "Agora nao"), Location = new Point(280, yb + 4), Size = new Size(140, 34) };
            var never = new Pill { Text = L("Don't ask again", "Nao perguntar mais"), Location = new Point(430, yb + 4), Size = new Size(190, 34) };
            yes.Click += (s, e) => {
                // The switch on Hardware & ID does the rest (its handler writes the setting and sends).
                if (shareBox != null && !shareBox.Checked) shareBox.Checked = true;
                else { WriteSetting("share", "1"); WriteSetting("share_sent", ""); StartHardwareShare(); }
                Log(L("Hardware sharing is on. Thank you!", "Partilha de hardware ligada. Obrigado!"));
                f.Close();
            };
            later.Click += (s, e) => f.Close();
            never.Click += (s, e) => { WriteSetting("share_ask", "never"); f.Close(); };
            f.Controls.Add(yes); f.Controls.Add(later); f.Controls.Add(never);
            f.CancelButton = later;
            f.ShowDialog(this);
            f.Dispose();
        }

        // ---------------------------------------------------------------- notices, once each
        // New things players should try and tell us about on Discord: VIctory's own frame
        // generation (cards without Smooth Motion), and the edupode engine on AMD cards.
        void ShowNotices()
        {
            if (OwnFrameGen && ReadSetting("notice_fg") != "1")
            {
                WriteSetting("notice_fg", "1");
                var body = string.Join("\r\n", Pt ? new[] {
                    "A tua placa (" + gpuName + ") nao tem o NVIDIA Smooth Motion. Agora o Julia's Launcher tem a sua",
                    "propria geracao de frames: cria um frame entre cada dois do jogo, so a partir das imagens.",
                    "",
                    "O QUE MUDA",
                    "   O movimento fica mais fluido. Os FPS reais do jogo nao sobem, e ha meio frame a mais de atraso.",
                    "",
                    "AINDA NINGUEM A TESTOU NO JOGO: es dos primeiros.",
                    "   1.  Liga 'Geracao de frames' nas Opcoes de arranque (ou carrega em 'Ligar agora').",
                    "   2.  Joga uns minutos com o F3 ligado.",
                    "   3.  Conta no Discord, no canal \"" + DiscordChannel + "\": a tua placa, se ficou mais fluido,",
                    "        se ha tremeliques ou rastos, e se o jogo fechou. O 'Enviar erros no Discord' tem a opcao",
                    "        'Teste da geracao de frames' e junta os logs.",
                    "",
                    "Se correr mal, desliga a opcao e fica tudo como antes.",
                } : Fr ? new[] {
                    "Ta carte (" + gpuName + ") n'a pas NVIDIA Smooth Motion. Julia's Launcher a maintenant sa propre",
                    "génération d'images : il crée une image entre chaque paire d'images du jeu, à partir des images seules.",
                    "",
                    "CE QUI CHANGE",
                    "   Le mouvement paraît plus fluide. Les vrais FPS du jeu n'augmentent pas, et il y a une demi-image de retard en plus.",
                    "",
                    "PERSONNE NE L'A ENCORE TESTÉE EN JEU : tu es parmi les premiers.",
                    "   1.  Active 'Génération d'images' dans Options de lancement (ou appuie sur 'Activer maintenant').",
                    "   2.  Joue quelques minutes avec F3 activé.",
                    "   3.  Raconte-nous sur Discord, dans le salon \"" + DiscordChannel + "\" : ta carte, si c'est plus fluide,",
                    "        s'il y a du scintillement ou des traînées, et si le jeu s'est fermé. 'Envoyer les erreurs sur Discord'",
                    "        a une case 'Test de la génération d'images' et ajoute les logs.",
                    "",
                    "Si ça se passe mal, désactive l'option et tout redevient comme avant.",
                } : new[] {
                    "Your card (" + gpuName + ") has no NVIDIA Smooth Motion. Julia's Launcher now has its own",
                    "frame generation: it makes a frame between each two of the game's, from the pictures alone.",
                    "",
                    "WHAT CHANGES",
                    "   Motion looks smoother. The game's real FPS do not go up, and there is half a frame more delay.",
                    "",
                    "NOBODY HAS TESTED IT IN THE GAME YET: you are among the first.",
                    "   1.  Turn on 'Frame generation' in Launch options (or press 'Turn it on now').",
                    "   2.  Play a few minutes with F3 on.",
                    "   3.  Tell us on Discord, in the \"" + DiscordChannel + "\" channel: your card, whether it looks",
                    "        smoother, any flicker or ghosting, and whether the game closed. 'Send errors to Discord'",
                    "        has a 'Frame generation test' tick and adds the logs.",
                    "",
                    "If it goes wrong, turn the option off and everything is as before.",
                });
                var answer = Notice(L("New: frame generation for your card (experimental)", "Novo: geracao de frames para a tua placa (experimental)"), body,
                    L("Turn it on now", "Ligar agora"), L("Open Discord", "Abrir o Discord"));
                if (answer == 1) { smoothBox.Checked = true; SaveSettings(); Log(L("Frame generation is on (experimental). Tell us how it runs on Discord!", "Geracao de frames ligada (experimental). Conta-nos como corre no Discord!")); }
                if (answer == 2) OpenUrl(DiscordInvite);
            }
            if (hasRadeon && !hasNvidia && ReadSetting("notice_amd") != "1")
            {
                WriteSetting("notice_amd", "1");
                var body = string.Join("\r\n", Pt ? new[] {
                    "Placa AMD detetada (" + gpuName + "). Jogas com o motor edupode, o motor deste projeto e o predefinido",
                    "para todas as placas:",
                    "   •  VRAM de ~12 GB para 6-8 GB na placa do autor, e ~4 vezes mais texturas na mesma memoria;",
                    "   •  sessoes longas e missoes sem crashes.",
                    "",
                    "Foi afinado numa placa NVIDIA: conta-nos no Discord (\"" + DiscordChannel + "\") os FPS e a VRAM na tua Radeon.",
                    "",
                    "Alternativa para Radeon: o VIctory-Belobug-AMD, o fork do Belobug afinado para AMD (o credito e dele).",
                    "Podes trocar quando quiseres em Hardware e ID > Motor.",
                } : Fr ? new[] {
                    "Carte AMD détectée (" + gpuName + "). Tu joues avec le moteur edupode, le moteur de ce projet et celui par défaut",
                    "pour toutes les cartes :",
                    "   •  VRAM de ~12 Go à 6-8 Go sur la carte de l'auteur, et ~4 fois plus de textures dans la même mémoire ;",
                    "   •  longues sessions et missions sans plantage.",
                    "",
                    "Il a été réglé sur une carte NVIDIA : dis-nous sur Discord (\"" + DiscordChannel + "\") les FPS et la VRAM sur ta Radeon.",
                    "",
                    "Alternative pour Radeon : VIctory-Belobug-AMD, le fork de Belobug réglé pour AMD (le mérite lui revient).",
                    "Tu peux changer à tout moment dans Matériel et ID > Moteur.",
                } : new[] {
                    "AMD card found (" + gpuName + "). You play with the edupode engine, this project's engine and the default",
                    "for every card:",
                    "   •  video memory from ~12 GB down to 6-8 GB on the author's card, ~4 times more textures in the same memory;",
                    "   •  long sessions and missions without crashes.",
                    "",
                    "It was tuned on an NVIDIA card: tell us on Discord (\"" + DiscordChannel + "\") the FPS and video memory on your Radeon.",
                    "",
                    "Alternative for Radeon: VIctory-Belobug-AMD, Belobug's fork tuned for AMD (the credit is his).",
                    "You can switch any time in Hardware & ID > Engine.",
                });
                var answer = Notice(L("AMD card: you play with the edupode engine", "Placa AMD: jogas com o motor edupode", "Carte AMD : tu joues avec le moteur edupode"), body,
                    L("Keep edupode", "Ficar com o edupode", "Garder edupode"), L("Try Belobug's instead", "Experimentar o do Belobug", "Essayer celui de Belobug"));
                if (answer == 2)
                {
                    WriteSetting("engine", "amd");
                    if (engineBox != null) engineBox.SelectedIndex = 1;
                    UpdateBanner(); Refresh2();
                    if (VictoryExe == null) { Show(PageSetup); Build(); }
                }
            }
        }

        // A notice in the launcher's style: 1 for the first button, 2 for the second, 0 for "Later".
        int Notice(string title, string body, string first, string second)
        {
            int answer = 0;
            var f = new Form { Text = title, ClientSize = new Size(700, 470), BackColor = Theme.Back, ForeColor = Theme.Text, Font = Theme.Ui(9.75f),
                StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Icon = Icon };
            f.Controls.Add(new Heading(title) { Location = new Point(20, 16), Width = 660 });
            var bodyLabel = new Label { Font = f.Font, Location = new Point(20, 50), Size = new Size(660, 340), Text = body, ForeColor = Theme.Text, BackColor = Theme.Back };
            int yb = FitText(bodyLabel, 50) + 16;
            f.ClientSize = new Size(700, yb + 62);
            f.Controls.Add(bodyLabel);
            var a = new Pill { Main = true, Text = first, Location = new Point(20, yb), Size = new Size(250, 42) };
            var b = new Pill { Text = second, Location = new Point(280, yb + 4), Size = new Size(220, 34) };
            var c = new Pill { Text = L("Later", "Mais tarde"), Location = new Point(510, yb + 4), Size = new Size(170, 34) };
            a.Click += (s, e) => { answer = 1; f.Close(); };
            b.Click += (s, e) => { answer = 2; f.Close(); };
            c.Click += (s, e) => f.Close();
            f.Controls.Add(a); f.Controls.Add(b); f.Controls.Add(c);
            f.CancelButton = c;
            f.ShowDialog(this);
            f.Dispose();
            return answer;
        }

        // ---------------------------------------------------------------- Hardware & ID page
        Panel HardwarePage()
        {
            var page = Page();
            const int X = 40, W = 900;
            int y = PageTop(page, L("Hardware & ID", "Hardware e ID", "Matériel et ID"), L("Hardware", "Hardware", "Matériel"), L("and your ID.", "e o teu ID.", "et ton ID."), L("Your ID, what this PC is made of, and which engine runs the game.", "O teu ID, de que e feito este PC, e que motor corre o jogo.", "Ton ID, de quoi ce PC est fait, et quel moteur lance le jeu."));
            idLabel = new Label { Location = new Point(X, y), Size = new Size(300, 40), Font = Theme.Mono(18f), ForeColor = Theme.Cyan, BackColor = Theme.Back, Text = "VI-····-····" };
            page.Controls.Add(idLabel);
            var copy = new Pill { Text = L("Copy ID", "Copiar ID"), Location = new Point(X + 310, y + 3), Size = new Size(120, 34) };
            copy.Click += (s, e) => { if (hw != null) { Clipboard.SetText(hw.Id); Log(L("ID copied: ", "ID copiado: ") + hw.Id); } };
            page.Controls.Add(copy);
            y += 44;
            page.Controls.Add(new Label { Location = new Point(X, y), Size = new Size(W, 38), ForeColor = Theme.Dim, BackColor = Theme.Back, Font = Theme.Ui(9f),
                Text = L("Made from parts of your PC (the motherboard and the processor): it stays the same after reinstalling Windows. Put it in your bug reports.",
                         "Feito a partir de pecas do teu PC (placa-mae e processador): fica igual depois de reinstalar o Windows. Poe-no nos teus relatorios de erros.") });
            y += 44;

            page.Controls.Add(new Heading(L("Hardware", "Hardware")) { Location = new Point(X, y), Width = W });
            y += 32;
            hwText = new InfoCard { Location = new Point(X, y), Size = new Size(W, 56), Empty = L("Reading your hardware...", "A ler o teu hardware...", "Lecture de ton matériel...") };
            page.Controls.Add(hwText);
            y += hwText.Height + 12;

            page.Controls.Add(new Heading(L("Engine", "Motor")) { Location = new Point(X, y), Width = W });
            y += 32;
            engineBox = new Dropdown { Location = new Point(X, y), Width = 420, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Theme.Card, ForeColor = Theme.Text };
            engineBox.Items.AddRange(new object[] {
                L("★ edupode (recommended, every card: least video memory, most stable)", "★ edupode (recomendado, todas as placas: menos VRAM, mais estavel)"),
                L("AMD alternative · VIctory-Belobug-AMD by Belobug", "Alternativa AMD · VIctory-Belobug-AMD do Belobug"),
            });
            engineBox.SelectedIndex = EngineChoice == "amd" ? 1 : 0;
            engineBox.SelectedIndexChanged += (s, e) => {
                WriteSetting("engine", engineBox.SelectedIndex == 1 ? "amd" : "edupode");
                Log(L("Engine: ", "Motor: ") + EngineName);
                UpdateBanner(); Refresh2();
            };
            // The choice shows as two pills (as on Play); the list stays, hidden, for the code that sets it.
            engineBox.Visible = false;
            page.Controls.Add(engineBox);
            var hwStd = new Pill { Text = L("edupode · recommended", "edupode · recomendado", "edupode · recommandé"), Location = new Point(X, y), Size = new Size(230, 36) };
            var hwAmd = new Pill { Text = L("Belobug AMD · for Radeon", "Belobug AMD · para Radeon", "Belobug AMD · pour Radeon"), Location = new Point(X + 240, y), Size = new Size(230, 36) };
            hwStd.Click += (s, e) => { if (UseAmdEngine) engineBox.SelectedIndex = 0; };
            hwAmd.Click += (s, e) => { if (!UseAmdEngine) engineBox.SelectedIndex = 1; };
            showEnginePills += () => { hwStd.Main = !UseAmdEngine; hwAmd.Main = UseAmdEngine; hwStd.Invalidate(); hwAmd.Invalidate(); };
            hwStd.Main = !UseAmdEngine; hwAmd.Main = UseAmdEngine;
            page.Controls.Add(hwStd); page.Controls.Add(hwAmd);
            y += 46;
            page.Controls.Add(new Label { Location = new Point(X, y), Size = new Size(W, 52), ForeColor = Theme.Dim, BackColor = Theme.Back, Font = Theme.Ui(9f),
                Text = L("Every card plays with the edupode engine: video memory from ~12 GB down to 6-8 GB, ~4x more textures in it, the most stable. For Radeon cards there is also VIctory-Belobug-AMD, Belobug's fork tuned for Radeon (github.com/Belobug/VIctory-Belobug-AMD, GPL-3.0; all credit for it is Belobug's), which Setup builds once.",
                         "Todas as placas jogam com o motor edupode: VRAM de ~12 GB para 6-8 GB, ~4x mais texturas, o mais estavel. Para placas Radeon ha tambem o VIctory-Belobug-AMD, o fork do Belobug afinado para Radeon (github.com/Belobug/VIctory-Belobug-AMD, GPL-3.0; todo o credito e do Belobug), que o Preparar compila uma vez.") });
            y += 60;

            y = SavesSection(page, X, y, W);
            y = ReferencePcSection(page, X, y, W);
            y = PlayersPcsSection(page, X, y, W);
            page.Controls.Add(new Heading(L("Hardware sharing (only hardware)", "Partilha de hardware (so hardware)")) { Location = new Point(X, y), Width = W });
            y += 32;
            shareBox = new OptionCard { Text = L("Share my hardware with edupode", "Partilhar o meu hardware com o edupode"), Line = L("Off by default. Only the list below; never logs, options, paths, names or your IP.", "Desligado por defeito. So a lista em baixo; nunca logs, opcoes, caminhos, nomes nem o teu IP."),
                Location = new Point(X, y), Size = new Size(W, 58), Checked = ReadSetting("share") == "1" };
            shareBox.CheckedChanged += (s, e) => {
                WriteSetting("share", shareBox.Checked ? "1" : "0");
                if (shareBox.Checked) { WriteSetting("share_sent", ""); StartHardwareShare(); }
                RefreshHardware();
            };
            page.Controls.Add(shareBox);
            y += 66;
            y = SessionLogsSection(page, X, y, W);
            shareStatus = new Label { Location = new Point(X, y), Size = new Size(W, 40), ForeColor = Theme.Dim, BackColor = Theme.Back, Font = Theme.Ui(9f) };
            page.Controls.Add(shareStatus);
            y += 44;
            page.Controls.Add(Small(L("Exactly what is sent:", "Exatamente o que e enviado:"), X, y, W));
            y += 22;
            // The text sits in a rounded card of the theme instead of a stock framed box.
            var jsonCard = new CardPanel { Location = new Point(X, y), Size = new Size(W, 300) };
            hwJson = new TextBox { Location = new Point(14, 12), Size = new Size(W - 24, 276), Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Theme.Card, ForeColor = Theme.Soft, Font = Theme.Mono(8.5f), ScrollBars = ScrollBars.Vertical, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            jsonCard.Controls.Add(hwJson);
            page.Controls.Add(jsonCard);
            page.Controls.Add(new Label { Location = new Point(X, y + 310), Size = new Size(10, 10), BackColor = Theme.Back });
            return page;
        }

        void RefreshHardware()
        {
            new Thread(() => {
                HardwareInfo h = null;
                try { h = ReadHardware(); } catch (Exception) { }
                try
                {
                    BeginInvoke(new Action(() => {
                        if (h == null)
                        {
                            // Nothing could be read: say so instead of leaving the card blank.
                            hwText.Empty = L("The hardware could not be read.", "Nao foi possivel ler o hardware.", "Le matériel n'a pas pu être lu.");
                            hwText.Invalidate();
                            return;
                        }
                        hw = h;
                        idLabel.Text = h.Id;
                        hwText.Text = string.Join("\r\n", new[] {
                            L("Graphics card ", "Placa grafica ") + ": " + h.Gpu + (h.Driver.Length > 0 ? "  (driver " + h.Driver + ")" : "") + "  ·  " + GpuKind(),
                            "Resizable BAR : " + RebarText() + (probeAdapter.Length > 0 ? "  ·  " + L("VIctory runs on ", "o VIctory corre na ") + probeAdapter : ""),
                            L("Processor     ", "Processador   ") + ": " + h.Cpu + "  ·  " + h.Cores + L(" cores, ", " nucleos, ") + h.Threads + " threads",
                            "RAM           : " + h.RamGb + " GB",
                            L("Motherboard   ", "Placa-mae     ") + ": " + h.BoardMaker + " " + h.Board,
                            "Windows       : " + h.Os,
                            L("System disk   ", "Disco         ") + ": " + h.Disk + (h.Laptop ? L("  ·  laptop", "  ·  portatil") : ""),
                            L("Engine        ", "Motor         ") + ": " + EngineName,
                        });
                        GrowTo(hwText, hwText.Fit());
                        hwText.Invalidate();
                        hwJson.Text = HardwareJson(h, FpsJson()).Replace("\n", "\r\n");
                        shareStatus.Text = HardwareEndpoint.Length == 0
                            ? L("The database is not online yet: nothing is sent, even with sharing on.", "A base de dados ainda nao esta online: nada e enviado, mesmo com a partilha ligada.")
                            : !shareBox.Checked ? L("Off: nothing is sent.", "Desligado: nada e enviado.")
                            : L("On: sent at most once a day, to a database only edupode can read. Turn it off any time.", "Ligado: enviado no maximo uma vez por dia, para uma base de dados que so o edupode le. Desliga quando quiseres.");
                    }));
                }
                catch (Exception) { }
            }) { IsBackground = true }.Start();
        }
    }
}
