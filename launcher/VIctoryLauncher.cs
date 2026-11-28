// Julia's Launcher (edupode): one window to set up and play the edupode build of VIctory.
// Built with the C# compiler that ships with Windows (.NET Framework 4.x):
//   launcher\build-launcher.cmd   ->   "Julia's Launcher.exe" in the repository's root.
// No game files are included or downloaded; the player points it at their own dump.
// English by default; the buttons in the side bar switch to Portuguese or French (saved in launcher.ini).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace VIctoryLauncher
{
    static class Program
    {
        public static readonly string ErrorFile = Path.Combine(Path.GetTempPath(), "vi-launcher-error.txt");

        [STAThread]
        static int Main(string[] args)
        {
            // What ends the launcher is written down (%TEMP%\vi-launcher-error.txt) for a bug report.
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                try { File.AppendAllText(ErrorFile, DateTime.Now + "  " + e.ExceptionObject + Environment.NewLine); } catch (Exception) { }
            };
            Application.ThreadException += (s, e) => {
                try { File.AppendAllText(ErrorFile, DateTime.Now + "  " + e.Exception + Environment.NewLine); } catch (Exception) { }
                MessageBox.Show("Julia's Launcher: " + e.Exception.Message + "\r\n\r\n(Help > Send errors to Discord)", "Julia's Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            // `--stale-check <source folder> <engine exe> <result file>`: for scripts\test-launcher-stale.ps1,
            // which proves the freshness rule without the game (a windowless program has no console to print
            // to). The same walk the Setup step and PLAY use.
            int stale = Array.IndexOf(args, "--stale-check");
            if (stale >= 0 && stale + 2 < args.Length)
            {
                var src = args[stale + 1];
                var engineExe = args[stale + 2];
                var result = stale + 3 < args.Length ? args[stale + 3] : null;
                // The verdict is the exit code (0 fresh, 1 stale, 2 error): a windowless program has no
                // console, and a file in a temporary folder proved unreliable under the test's own profile.
                // The times go beside it when a file was named.
                try
                {
                    var newest = LauncherForm.NewestInputIn(src);
                    var engine = File.Exists(engineExe) ? File.GetLastWriteTimeUtc(engineExe) : DateTime.MinValue;
                    var newer = newest > engine;
                    if (!string.IsNullOrEmpty(result))
                        try { File.WriteAllText(result, (newer ? "stale " : "fresh ") + newest.ToString("o") + " " + engine.ToString("o")); }
                        catch (Exception) { }
                    return newer ? 1 : 0;
                }
                catch (Exception) { return 2; }
            }
            // The installer's one administrator step (installer\VI.iss): Windows settings, then out.
            if (args.Contains("--admin-setup")) return AdminTasks.Run(args);
            // The Setup page's virtual memory step, run as administrator (no PowerShell).
            if (args.Contains("--pagefile")) return LauncherForm.SetManagedPagefile() ? 0 : 1;
            // The Setup page's "Resizable BAR in the NVIDIA driver", run as administrator (the driver asks for it).
            if (args.Contains("--rebar-profile")) return SmoothMotion.SetRebar(true) == null ? 0 : 1;
            // After an update: the launcher it replaced ends first (it still holds the one-launcher lock).
            int wait = Array.IndexOf(args, "--wait-pid");
            int pid;
            if (wait >= 0 && wait + 1 < args.Length && int.TryParse(args[wait + 1], out pid))
                try { Process.GetProcessById(pid).WaitForExit(15000); } catch (Exception) { }
            // One launcher at a time: a second start brings the first to the front.
            bool first;
            using (var one = new Mutex(true, "VI-Launcher-edupode", out first))
            {
                if (!first)
                {
                    foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Application.ExecutablePath)))
                        try { if (p.Id != Process.GetCurrentProcess().Id && p.MainWindowHandle != IntPtr.Zero) SetForegroundWindow(p.MainWindowHandle); } catch (Exception) { }
                    return 0;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                // The theme (launcher.ini's theme=) before any window or brush is made.
                Theme.Use(SavedTheme());
                Application.Run(new LauncherForm());
            }
            return 0;
        }

        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);

        // launcher.ini's theme= (%LOCALAPPDATA%\VIctory\launcher.ini), read before the form exists.
        static string SavedTheme()
        {
            try
            {
                var ini = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VIctory", "launcher.ini");
                if (File.Exists(ini))
                    foreach (var line in File.ReadAllLines(ini))
                        if (line.StartsWith("theme=")) return line.Substring(6).Trim();
            }
            catch (Exception) { }
            return "plum";
        }
    }

    // A preset: render scale and the switches it turns on.
    class Mode
    {
        public string Name, Short, Tag, Description;
        public string Scale;      // null: native 1080p
        public bool LightTextures;
        public bool Modest;       // smaller textures and buffers for 6-8 GB cards and 16 GB of RAM
        public bool UltraLow;     // the least memory: 4-6 GB cards, 8-16 GB of RAM
        public bool Custom;       // the player's own values (Advanced Tweaks)
        public Mode(string name, string tag, string shortLine, string description, string scale, bool light, bool modest)
        {
            Name = name; Tag = tag; Short = shortLine; Description = description; Scale = scale; LightTextures = light; Modest = modest;
        }
        public override string ToString() { return Name + " (" + Tag + ")"; }
    }

    // An app that holds video memory or RAM, all its processes together.
    class MemoryUser
    {
        public string Name;
        public long Vram;
        public long Ram;
        public bool Protected;
        public string Note = "";
    }

    partial class LauncherForm : Form
    {
        static bool Pt, Fr;
        // French comes from French.cs (looked up by the English text), or from fr when a call passes it.
        static string L(string en, string pt, string fr = null) { return Fr ? (fr ?? Fr_(en)) : Pt ? pt : en; }

        // Opens the launcher again (a new language). Application.Restart started the new one while this one
        // still held the one-launcher lock, so the new one saw "already running" and quit: nothing reopened.
        // --wait-pid makes it wait until this one has ended.
        static void RestartLauncher()
        {
            try { Process.Start(new ProcessStartInfo(Application.ExecutablePath, "--wait-pid " + Process.GetCurrentProcess().Id) { UseShellExecute = false }); }
            catch (Exception) { }
            Application.Exit();
        }

        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        readonly string root = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
        readonly string dataRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VIctory", "luz");
        readonly string settingsFile;
        // Where the shader cache and the game's saves live; launcher.ini's cache= and data= move them
        // (to share them with another way of starting VIctory).
        string cacheDir, dataDir;

        Hero banner;
        FootMeta footMeta;
        StatusStrip statusStrip;
        TextBox gameBox;
        Label gameStatus, buildStatus, modeInfo, vramSummary, smoothStatus, warnText, updateBadge;
        Panel warnPanel;
        Pill warnFix;
        Heading presetHeading, gameHeading;
        readonly List<ModeCard> cards = new List<ModeCard>();
        int modeIndex = -1;
        CheckBox panelBox, borderlessBox, fps60Box, reserveBox, smoothBox, qualityBox, facesBox, loadingBox, fightsBox, warmBox, cpuBox;
        LoadBar loadBar;
        TextBox logBox;
        Pill playButton, prepareButton, buildButton, closeButton;
        ListView vramList;
        readonly List<Panel> pages = new List<Panel>();
        readonly List<NavButton> nav = new List<NavButton>();

        bool rebar, rtxFrameGen;
        // The card can use NVIDIA Smooth Motion (RTX 40/50); rtxFrameGen: it is the frame generation picked.
        bool canSmooth;
        // Resizable BAR as the card reports it (nvidia-smi's BAR1 of 1 GB and more) and as D3D12
        // offers it to a run (`victory gpu`: GPU upload heaps; -1 not asked yet).
        bool rebarBios;
        // nvidia-smi's BAR1 was read at all; D3D12 knows GPU upload heaps (Windows 11 24H2 and newer).
        bool barRead, probeOptions16;
        int rebarHeap = -1;
        string probeAdapter = "", probeVendor = "";
        double vramGb, ramGb;
        string gpuName = "unknown GPU";
        string smoothState = "?";

        Mode[] modes;
        // Pages by their place in `pages`; the side bar shows the first eight.
        const int PagePlay = 0, PagePerf = 1, PageShaders = 2, PageControls = 3, PageSetup = 4, PageHardware = 5, PageUpdates = 6, PageHelp = 7, PageCustom = 8;
        // The order the preset cards show the modes in (modes keep their numbers in launcher.ini).
        static readonly int[] CardOrder = { 0, 1, 2, 5, 3, 4, 6 };
        const int ModeUltraLow = 5, ModeCustom = 6;

        public const string DiscordInvite = "https://discord.gg/648WEXCAsZ";
        public const string DiscordChannel = "Send bugs here please";
        public const string RepoUrl = "https://github.com/felicienradio/cold-project-vi/tree/main";

        public LauncherForm()
        {
            settingsFile = Path.Combine(Path.GetDirectoryName(dataRoot), "launcher.ini");
            // Worker threads stop starting tools once the window is closing (OnFormClosing kills the running ones).
            FormClosing += (s, e) => {
                if (e.Cancel) return;
                bool asks;
                lock (tools) asks = tools.Any(t => { try { return !t.HasExited; } catch (Exception) { return false; } });
                // With a question to come (Session.cs asks before stopping), the answer is not known here yet.
                if (e.CloseReason == CloseReason.UserClosing && asks) closePrompt = true; else closing = true;
            };
            FormClosed += (s, e) => { closing = true; };
            Activated += (s, e) => { if (closePrompt && !closing) { try { BeginInvoke(new Action(() => { if (!closing) closePrompt = false; })); } catch (Exception) { } } };
            Anim.Attach(this, ReadSetting("animations") != "0");
            Pt = ReadSetting("lang") == "pt";
            Fr = ReadSetting("lang") == "fr";
            cacheDir = ReadSetting("cache");
            if (cacheDir == null || (!cacheDir.All(c => c < 128) && cacheDir == Path.Combine(dataRoot, "cache"))) cacheDir = DefaultCacheDir();
            dataDir = ReadSetting("data") ?? Path.Combine(dataRoot, "data");
            modes = new Mode[] {
                new Mode(L("Recommended", "Recomendado"), "720p → 1080p", L("Balanced · edupode", "Equilibrado · edupode"),
                    L("The world drawn at 1280x720 and rebuilt to 1080p by the game's own upscaler, texture levels held (video memory under budget), 60 FPS clock. About 22-26 FPS in the open world and 35+ in cutscenes on an RTX 5070 Ti laptop (6-8 GB of video memory, ~20 GB of RAM).",
                      "O mundo desenhado a 1280x720 e reconstruido para 1080p pelo upscaler do proprio jogo, niveis de texturas guardados (VRAM dentro do limite), relogio de 60 FPS. Cerca de 22-26 FPS no mundo aberto e 35+ nas cutscenes num portatil RTX 5070 Ti (6-8 GB de VRAM, ~20 GB de RAM)."),
                    "0.6666667", true, false),
                new Mode(L("Fastest", "Mais rapido"), "540p", L("Peak FPS", "Mais FPS"),
                    L("The world drawn at 960x540: the most frames, a softer picture. For slower CPUs.",
                      "O mundo desenhado a 960x540: mais frames, imagem mais suave. Para processadores mais lentos."),
                    "0.5", true, false),
                new Mode(L("Modest PC", "PC modesto"), "540p", L("6-8 GB GPUs", "Placas 6-8 GB"),
                    L("For 6-8 GB cards and 16 GB of RAM: 540p, textures over 1024 texels halved, fewer of the game's buffers on the card, smaller texture batches, the memory guards on. Less video memory and RAM, a softer picture.",
                      "Para placas de 6-8 GB e 16 GB de RAM: 540p, texturas acima de 1024 cortadas a metade, menos buffers do jogo na placa, lotes de texturas mais pequenos, protecoes de memoria ligadas. Menos VRAM e RAM, imagem mais suave."),
                    "0.5", true, true),
                new Mode(L("Sharp", "Nitido"), "1080p", L("12 GB+ GPUs", "Placas 12 GB+"),
                    L("The world at full 1080p with held texture levels. Needs a strong GPU with 12 GB or more; slower in the open world.",
                      "O mundo a 1080p completos com niveis de texturas guardados. Precisa de uma placa forte com 12 GB ou mais; mais lento no mundo aberto."),
                    null, true, false),
                new Mode(L("Classic cgbj", "cgbj classico"), "1080p", L("Reference", "Referencia"),
                    L("The original cgbj settings at 1080p, no texture changes. For comparison.",
                      "As definicoes originais do cgbj a 1080p, sem mudancas nas texturas. Para comparar."),
                    null, false, false),
                new Mode(L("Ultra Low", "Ultra baixo"), "540p", L("4-6 GB, 8-16 GB RAM", "4-6 GB, 8-16 GB RAM"),
                    L("For the weakest PCs (4-6 GB cards, 8-16 GB of RAM): 540p, textures capped at 512 texels, the fewest buffers on the card, small texture batches, the memory guards on and a 30 FPS clock. The least memory of all; a soft picture. Give Windows a large page file (Setup).",
                      "Para os PCs mais fracos (placas de 4-6 GB, 8-16 GB de RAM): 540p, texturas ate 512 texels, o minimo de buffers na placa, lotes de texturas pequenos, protecoes de memoria ligadas e relogio de 30 FPS. A menor memoria de todas; imagem suave. Da ao Windows um ficheiro de paginacao grande (Preparar)."),
                    "0.5", true, true) { UltraLow = true },
                new Mode(L("Custom", "Personalizado"), L("your values", "os teus valores"), L("Advanced Tweaks", "Ajustes avancados"),
                    L("Your own values from Advanced Tweaks: resolution, texture size, buffers, memory guards, FPS clock. Copy them as a code to share, or paste a friend's.",
                      "Os teus proprios valores dos Ajustes avancados: resolucao, tamanho das texturas, buffers, protecoes de memoria, relogio de FPS. Copia-os como um codigo para partilhar, ou cola o de um amigo."),
                    null, true, false) { Custom = true },
            };

            Text = "Julia's Launcher";
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception) { }
            ClientSize = new Size(1200, 900);
            MinimumSize = new Size(1160, 720);
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = Theme.Ui(9.75f);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            var main = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Back };
            var content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Back };
            main.Controls.Add(content);
            main.Controls.Add(Footer());
            pages.Add(PlayPage());
            pages.Add(MemoryPage());
            pages.Add(ShadersPage());
            pages.Add(ControlsPage());
            pages.Add(SetupPage());
            pages.Add(HardwarePage());
            pages.Add(UpdatesPage());
            pages.Add(HelpPage());
            pages.Add(CustomPage());
            foreach (var p in pages) { p.Dock = DockStyle.Fill; p.Visible = false; content.Controls.Add(p); }
            Controls.Add(main);
            Controls.Add(SideBar());
            Controls.Add(TopBar());

            LoadSettings();
            DetectSystem();
            Show(PagePlay);
            Refresh2();
            // A first run (nothing built or no game yet): the Setup page, checked.
            if (VictoryExe == null || !File.Exists(GameExe)) Show(PageSetup);
            // `--page N` opens on that page (0 Play, 1 Performance, 2 Shaders, 3 Controls, 4 Setup,
            // 5 Hardware, 6 Updates, 7 Help, 8 Advanced Tweaks).
            var argv = Environment.GetCommandLineArgs();
            int page;
            for (int a = 1; a + 1 < argv.Length; a++)
                if (argv[a] == "--page" && int.TryParse(argv[a + 1], out page) && page >= 0 && page < pages.Count) Show(page);
        }

        // Work on other threads that reports back to the window starts once the window exists.
        ToolTip tips;

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // The first focus on PLAY: a list lower on the page would take it, and the page would open scrolled down to it.
            if (playButton != null && playButton.CanFocus) ActiveControl = playButton;
            // PLAY follows the game and the tasks on its own (a game closed, a build done).
            var timer = new System.Windows.Forms.Timer { Interval = 2000 };
            timer.Tick += (s, a) => { if (!setupBusy) Refresh2(); };
            timer.Start();
            // No game folder yet: look for the game on its own.
            if (!File.Exists(GameExe)) RunSteps(steps.Where(st => st.Fix == (Func<bool>)FindGame).ToArray());
            else CheckAll();
            new Thread(() => {
                var s = SmoothMotion.Status();
                try { BeginInvoke(new Action(() => { smoothState = s; UpdateBanner(); UpdateSmooth(); })); } catch (Exception) { }
            }) { IsBackground = true }.Start();
            ProbeGpu();
            // Windows 10 needs the D3D12 Agility SDK beside victory.exe (Agility.cs).
            EnsureAgility();
            StartChecks();
            // Once the window is up: the notices of new things to try (Hardware.cs).
            var later = new System.Windows.Forms.Timer { Interval = 1500 };
            // First what changed in this version (once), then older copies of the launcher to remove, then the notices.
            later.Tick += (s, a) => { later.Stop(); later.Dispose(); ShowWhatsNewOnce(); CleanupOldLaunchers(); if (!setupBusy) ShowNotices(); };
            later.Start();
            StartUpdates();
            StartHardwareShare();
            RepairEngine();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // The title bar follows the theme: dark (or light), tinted with its side colour where Windows 11 allows it.
            int on = Theme.Dark ? 1 : 0;
            try { if (DwmSetWindowAttribute(Handle, 20, ref on, 4) != 0) DwmSetWindowAttribute(Handle, 19, ref on, 4); } catch (Exception) { }
            int caption = Theme.Side.R | (Theme.Side.G << 8) | (Theme.Side.B << 16);
            try { DwmSetWindowAttribute(Handle, 35, ref caption, 4); } catch (Exception) { }
        }

        // ---------------------------------------------------------------- top bar and side bar
        Control TopBar()
        {
            var bar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.Side };
            // The links at the right as tiny two-line labels (name over what it is).
            string[][] linkText = {
                new[] { "Discord", L("Chat and help", "Conversa e ajuda", "Discussion et aide") },
                new[] { "Site", L("Source on GitHub", "Codigo no GitHub", "Code sur GitHub") },
            };
            string[] linkUrl = { DiscordInvite, RepoUrl };
            var linkBox = new Rectangle[2];
            int hot = -1;
            updateBadge = new Label { Text = "", AutoSize = true, ForeColor = Theme.Good, BackColor = Theme.Side, Font = Theme.Mono(8.5f), Cursor = Cursors.Hand };
            updateBadge.Click += (s, e) => Show(PageUpdates);
            bar.Controls.Add(updateBadge);
            Action place = () => {
                int x = bar.Width - 28;
                using (var f = Theme.Ui(8.5f))
                    for (int i = 1; i >= 0; i--)
                    {
                        int w = Math.Max(Kit.TextW(linkText[i][0], f), Kit.TextW(linkText[i][1], f)) + 20;
                        x -= w;
                        linkBox[i] = new Rectangle(x, 9, w, 34);
                        x -= 30;
                    }
                updateBadge.Location = new Point(linkBox[0].Left - updateBadge.Width - 12, (bar.Height - updateBadge.Height) / 2);
            };
            bar.Paint += (s, e) => {
                var g = e.Graphics;
                Theme.Smooth(g);
                // The mark: "VI" on a small butter tile, then the launcher's name over its version.
                var tile = new RectangleF(16, 13, 26, 26);
                using (var path = Theme.Round(tile, 7))
                using (var b = new SolidBrush(Theme.Cyan)) g.FillPath(b, path);
                using (var f = new Font("Segoe UI Black", 8.5f))
                using (var b = new SolidBrush(Theme.OnAccent))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString("VI", f, b, tile, sf);
                Theme.Meta(g, "Julia's Launcher", "v" + Updater.Version, Theme.Muted, new RectangleF(54, 9, 240, 34), StringAlignment.Near);
                for (int i = 0; i < 2; i++)
                    Theme.Meta(g, linkText[i][0] + " ↗", linkText[i][1], i == hot ? Theme.Soft : Theme.Muted, new RectangleF(linkBox[i].X, linkBox[i].Y, linkBox[i].Width + 14, 34), StringAlignment.Near);
                using (var p = new Pen(Theme.Rule)) g.DrawLine(p, 0, bar.Height - 1, bar.Width, bar.Height - 1);
            };
            bar.MouseMove += (s, e) => {
                int now = -1;
                for (int i = 0; i < 2; i++) if (linkBox[i].Contains(e.Location)) now = i;
                if (now != hot) { hot = now; bar.Cursor = now >= 0 ? Cursors.Hand : Cursors.Default; bar.Invalidate(); }
            };
            bar.MouseLeave += (s, e) => { if (hot != -1) { hot = -1; bar.Cursor = Cursors.Default; bar.Invalidate(); } };
            bar.MouseClick += (s, e) => { for (int i = 0; i < 2; i++) if (linkBox[i].Contains(e.Location)) OpenUrl(linkUrl[i]); };
            bar.Resize += (s, e) => { place(); bar.Invalidate(); };
            place();
            return bar;
        }

        Control SideBar()
        {
            var side = new Panel { Dock = DockStyle.Left, Width = 216, BackColor = Theme.Side };
            side.Paint += (s, e) => { using (var p = new Pen(Theme.Rule)) e.Graphics.DrawLine(p, side.Width - 1, 0, side.Width - 1, side.Height); };
            string[][] items = {
                new[] { "", L("Play", "Jogar") },
                new[] { "", L("Performance", "Desempenho") },
                new[] { "", L("Shaders", "Shaders") },
                new[] { "", L("Controls", "Controlos") },
                new[] { "", L("Setup", "Preparar") },
                new[] { "", L("Hardware & ID", "Hardware e ID") },
                new[] { "", L("Updates", "Atualizacoes") },
                new[] { "", L("Help", "Ajuda") },
            };
            for (int i = 0; i < items.Length; i++)
            {
                int k = i;
                var b = new NavButton { Glyph = (i + 1).ToString("00"), Text = items[i][1], Size = new Size(200, 38), Location = new Point(8, 24 + i * 40) };
                b.Click += (s, e) => Show(k);
                nav.Add(b);
                side.Controls.Add(b);
            }
            var report = new Pill { Text = L("Send errors (GitHub / Discord)", "Enviar erros (GitHub / Discord)"), Size = new Size(184, 34), Location = new Point(16, 0), BackColor = Theme.Side, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            report.Click += (s, e) => ReportErrors();
            side.Controls.Add(report);
            var en = new Pill { Link = true, Text = "EN", Size = new Size(58, 30), BackColor = Theme.Side, Anchor = AnchorStyles.Bottom | AnchorStyles.Left, Main = !Pt && !Fr };
            var pt = new Pill { Link = true, Text = "PT", Size = new Size(58, 30), BackColor = Theme.Side, Anchor = AnchorStyles.Bottom | AnchorStyles.Left, Main = Pt };
            var fr = new Pill { Link = true, Text = "FR", Size = new Size(58, 30), BackColor = Theme.Side, Anchor = AnchorStyles.Bottom | AnchorStyles.Left, Main = Fr };
            en.Click += (s, e) => { if (Pt || Fr) { Pt = false; Fr = false; SaveSettings(); RestartLauncher(); } };
            pt.Click += (s, e) => { if (!Pt) { Pt = true; Fr = false; SaveSettings(); RestartLauncher(); } };
            fr.Click += (s, e) => { if (!Fr) { Fr = true; Pt = false; SaveSettings(); RestartLauncher(); } };
            side.Controls.Add(en); side.Controls.Add(pt); side.Controls.Add(fr);
            var tech = new Label { Text = "D3D12 · SM 6.6", Font = Theme.Mono(8f), ForeColor = Theme.Dim, AutoSize = true, BackColor = Theme.Side, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            side.Controls.Add(tech);
            // The theme picker: one circle per theme; a click saves it (launcher.ini theme=) and opens the launcher again in it.
            int lang = Fr ? 2 : Pt ? 1 : 0;
            var themeLabel = new Label { Text = L("Theme", "Tema", "Thème") + " · " + Theme.Label(Theme.Current, lang), Font = Theme.Ui(8.5f), ForeColor = Theme.Muted, AutoSize = true, BackColor = Theme.Side, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            side.Controls.Add(themeLabel);
            var dots = new List<ThemeDot>();
            var dotTips = new ToolTip();
            foreach (var name in Theme.Names)
            {
                var n = name;
                var d = new ThemeDot { ThemeName = n, BackColor = Theme.Side, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
                dotTips.SetToolTip(d, Theme.Label(n, lang));
                d.MouseEnter += (s, e) => themeLabel.Text = L("Theme", "Tema", "Thème") + " · " + Theme.Label(n, lang);
                d.MouseLeave += (s, e) => themeLabel.Text = L("Theme", "Tema", "Thème") + " · " + Theme.Label(Theme.Current, lang);
                d.Click += (s, e) => { if (n == Theme.Current) return; WriteSetting("theme", n); SaveSettings(); RestartLauncher(); };
                dots.Add(d);
                side.Controls.Add(d);
            }
            side.Resize += (s, e) => {
                themeLabel.Location = new Point(18, side.Height - 152);
                for (int i = 0; i < dots.Count; i++) dots[i].Location = new Point(14 + i * 37, side.Height - 128);
            };
            side.Resize += (s, e) => {
                report.Top = side.Height - 200;
                en.Location = new Point(16, side.Height - 80); pt.Location = new Point(79, side.Height - 80); fr.Location = new Point(142, side.Height - 80);
                tech.Location = new Point((side.Width - tech.Width) / 2, side.Height - 36);
            };
            return side;
        }

        int shownPage = -1;

        void Show(int page)
        {
            bool changed = page != shownPage;
            shownPage = page;
            for (int i = 0; i < pages.Count; i++) pages[i].Visible = i == page;
            if (changed) foreach (Control c in pages[page].Controls) { var poster = c as Poster; if (poster != null) { poster.PlayIntro(); break; } }
            Anim.Wake();
            for (int i = 0; i < nav.Count; i++) nav[i].Active = i == page || (page == PageCustom && i == PagePlay);
            PlacePlayButton(page);
            if (page == PageSetup) CheckAll();
            if (page == PagePerf) ScanMemory();
            if (page == PageShaders) RefreshShaders();
            if (page == PageHardware) RefreshHardware();
            if (page == PageUpdates) RefreshUpdates();
            if (page == PageCustom) RefreshCustom();
        }

        Panel Page()
        {
            var page = new Panel { BackColor = Theme.Back, AutoScroll = true, Padding = new Padding(24, 20, 24, 20) };
            // A poster (the head of a page) always spans the page's whole width, glow to the edges.
            Action stretch = () => {
                int w = page.ClientSize.Width, off = page.AutoScrollPosition.X;
                if (w <= 0) return;
                foreach (Control c in page.Controls)
                    if (c is Poster && (c.Width != w || c.Left != off)) c.SetBounds(off, 0, w, 0, BoundsSpecified.X | BoundsSpecified.Width);
            };
            page.Resize += (s, e) => stretch();
            page.ClientSizeChanged += (s, e) => stretch();
            page.ControlAdded += (s, e) => stretch();
            page.VisibleChanged += (s, e) => { if (page.Visible) stretch(); };
            return page;
        }

        // The head of a page: tiny labels on the rule, the headline (a heavy word, an italic serif run in butter) and a quiet line.
        // The page's content starts at PageHeadH(sub) below it.
        PageHead Head(string eyeValue, string heavy, string accent, string sub)
        {
            var h = new PageHead { Location = new Point(0, 0), Eye = "Julia's Launcher", EyeValue = eyeValue, Title = heavy, Accent = accent, Sub = sub };
            using (var f = Theme.Ui(10f)) h.Height = 150 + (string.IsNullOrEmpty(sub) ? 0 : Kit.TextH(sub, f, h.SubWidth) + 8);
            return h;
        }

        // Adds a page's head and returns the y where its content starts.
        int PageTop(Panel page, string eyeValue, string heavy, string accent, string sub)
        {
            var h = Head(eyeValue, heavy, accent, sub);
            page.Controls.Add(h);
            return h.Height + 16;
        }

        Label Small(string text, int x, int y, int width)
        {
            return new Label { Text = text, Location = new Point(x, y), Size = new Size(width, 20), ForeColor = Theme.Dim, BackColor = Theme.Back };
        }

        // ---------------------------------------------------------------- footer: a thin rule and tiny labels (state, preset, engine, hardware); PLAY on the pages other than Play
        Control Footer()
        {
            var foot = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Theme.Back };
            footPanel = foot;
            footMeta = new FootMeta { Location = new Point(0, 1), Size = new Size(400, 62),
                Labels = new[] { L("State", "Estado", "État"), L("Preset", "Predefinicao", "Préréglage"), L("Engine", "Motor", "Moteur"), L("Hardware", "Hardware", "Matériel"), L("Next step", "Proximo passo", "Prochaine étape") } };
            foot.Controls.Add(footMeta);
            closeButton = new Pill { Text = L("Close game", "Fechar o jogo"), Size = new Size(160, 44), Visible = false };
            closeButton.Click += (s, e) => {
                if (State() == GameState.Running && MessageBox.Show(this, L("Close the game now? What was not saved is lost.", "Fechar o jogo agora? O que nao foi guardado perde-se."),
                    "Julia's Launcher", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                CloseGames();
                Refresh2();
            };
            foot.Controls.Add(closeButton);
            playButton = new Pill { Main = true, Shine = true, Text = L("Play  →", "Jogar  →", "Jouer  →"), Size = new Size(190, 44), Font = new Font("Segoe UI Semibold", 12f) };
            playButton.Click += (s, e) => Play();
            foot.Controls.Add(playButton);
            foot.Paint += (s, e) => { using (var p = new Pen(Theme.Rule)) e.Graphics.DrawLine(p, 0, 0, foot.Width, 0); };
            layoutFoot = () => {
                int right = foot.Width - 40;
                // On the Play page PLAY is on the poster; here it is at the right end.
                if (playButton.Parent == foot) { playButton.Location = new Point(right - playButton.Width, 10); right = playButton.Left - 12; }
                if (closeButton.Visible) { closeButton.Location = new Point(right - closeButton.Width, 10); right = closeButton.Left - 12; }
                footMeta.Width = Math.Max(100, right - 8);
            };
            foot.Resize += (s, e) => layoutFoot();
            closeButton.VisibleChanged += (s, e) => layoutFoot();
            return foot;
        }

        // PLAY lives on the poster of the Play page and in the footer on the others: the one button moves.
        void PlacePlayButton(int page)
        {
            if (banner == null || footPanel == null) return;
            if (page == PagePlay)
            {
                if (playButton.Parent != banner)
                {
                    playButton.Size = new Size(200, 48);
                    playButton.Font = new Font("Segoe UI Semibold", 12.5f);
                    banner.Controls.Add(playButton);
                }
                playButton.Location = new Point(40, HeroButtonsY);
            }
            else if (playButton.Parent != footPanel)
            {
                playButton.Size = new Size(190, 44);
                playButton.Font = new Font("Segoe UI Semibold", 12f);
                footPanel.Controls.Add(playButton);
            }
            if (layoutFoot != null) layoutFoot();
        }

        // A panel of the page's cards gets rounded corners (a region, so its own children stay inside).
        static void Rounded(Control c, float radius)
        {
            Action set = () => {
                if (c.Width < 4 || c.Height < 4) return;
                using (var path = Theme.Round(new RectangleF(0, 0, c.Width, c.Height), radius)) c.Region = new Region(path);
            };
            c.Resize += (s, e) => set();
            set();
        }

        // A text box in a rounded field of the theme (the stock one has a hard system border).
        class Field : Panel
        {
            public Field() { DoubleBuffered = true; BackColor = Theme.Back; ResizeRedraw = true; }
        }

        Field FieldFor(TextBox tb, int x, int y, int w, int h) { return FieldFor(tb, x, y, w, h, false); }

        // flat: no box, just one underline (a quiet row of a page); otherwise a rounded inset.
        Field FieldFor(TextBox tb, int x, int y, int w, int h, bool flat)
        {
            var f = new Field { Location = new Point(x, y), Size = new Size(w, h) };
            tb.BorderStyle = BorderStyle.None;
            tb.BackColor = flat ? Theme.Back : Theme.Card;
            f.Controls.Add(tb);
            Action lay = () => {
                if (tb.Multiline) tb.SetBounds(flat ? 2 : 12, 10, Math.Max(10, f.Width - (flat ? 4 : 24)), Math.Max(10, f.Height - 20));
                else if (flat) tb.SetBounds(0, Math.Max(0, (f.Height - tb.Height) / 2 - 1), Math.Max(10, f.Width - 4), tb.Height);
                else tb.SetBounds(14, Math.Max(0, (f.Height - tb.Height) / 2), Math.Max(10, f.Width - 28), tb.Height);
            };
            f.Paint += (s, e) => {
                Theme.Smooth(e.Graphics);
                if (flat && tb.Multiline)
                {
                    // A text area as type: a hairline above and below it (butter and thicker below while it has focus).
                    using (var b = new SolidBrush(Theme.Rule)) e.Graphics.FillRectangle(b, 0, 0, f.Width, 1);
                    using (var b = new SolidBrush(tb.Focused ? Theme.Cyan : Theme.Rule)) e.Graphics.FillRectangle(b, 0, f.Height - (tb.Focused ? 2 : 1), f.Width, tb.Focused ? 2 : 1);
                    return;
                }
                if (flat)
                {
                    using (var b = new SolidBrush(tb.Focused ? Theme.Cyan : Theme.LineStrong)) e.Graphics.FillRectangle(b, 0, f.Height - (tb.Focused ? 2 : 1), f.Width, tb.Focused ? 2 : 1);
                    return;
                }
                Theme.Panel(e.Graphics, new RectangleF(0.5f, 0.5f, f.Width - 1.5f, f.Height - 1.5f), Theme.Card, tb.Focused ? Theme.Cyan : Theme.Line, tb.Multiline ? 10 : f.Height / 2f);
            };
            f.Resize += (s, e) => lay();
            tb.GotFocus += (s, e) => f.Invalidate();
            tb.LostFocus += (s, e) => f.Invalidate();
            lay();
            return f;
        }

        // A drop-down list in the theme's colours (the list, the picked row, the highlight).
        static void ThemeCombo(ComboBox c)
        {
            c.DrawMode = DrawMode.OwnerDrawFixed;
            c.ItemHeight = 22;
            c.DrawItem += (s, e) => {
                if (e.Index < 0) return;
                bool hot = (e.State & DrawItemState.Selected) != 0 && (e.State & DrawItemState.ComboBoxEdit) == 0;
                using (var bg = new SolidBrush(hot ? Theme.Selected : Theme.Card)) e.Graphics.FillRectangle(bg, e.Bounds);
                TextRenderer.DrawText(e.Graphics, c.GetItemText(c.Items[e.Index]), c.Font, new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height),
                    Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            };
        }

        // Makes a page follow the window's width: every control keeps its place in the 'baseW' grid and is
        // stretched with it (a hidden control keeps its left and width too; its top is set when it is shown).
        // 'after' runs with the new width for what has to be measured again (wrapped text).
        void Fluid(Panel page, int x, int baseW, Action<int> after)
        {
            var snap = new List<Tuple<Control, int, int>>();
            foreach (Control c in page.Controls) if (!(c is Poster)) snap.Add(Tuple.Create(c, c.Left, c.Width));
            bool busy = false;
            Action apply = () => {
                if (busy || page.ClientSize.Width <= 0) return;
                busy = true;
                try
                {
                    int avail = page.ClientSize.Width - 2 * x;
                    float k = Math.Max(0.8f, Math.Min(1.35f, avail / (float)baseW));
                    int off = page.AutoScrollPosition.X;
                    page.SuspendLayout();
                    foreach (var t in snap)
                        t.Item1.SetBounds(off + x + (int)Math.Round((t.Item2 - x) * k), 0, (int)Math.Round(t.Item3 * k), 0, BoundsSpecified.X | BoundsSpecified.Width);
                    if (after != null) after((int)Math.Round(baseW * k));
                    page.ResumeLayout(true);
                }
                finally { busy = false; }
            };
            page.Resize += (s, e) => apply();
            page.VisibleChanged += (s, e) => { if (page.Visible) apply(); };
        }

        // ---------------------------------------------------------------- Play page
        // A poster, then quiet rows (game, engine), the presets set as type, the launch options as flat rows.
        Panel PlayPage()
        {
            var page = Page();
            const int X = 40, W = 900;
            Theme.TestedText = L("Tested", "Testado", "Testé");
            Theme.ExperimentalText = L("Experimental", "Experimental", "Expérimental");

            // The poster: Julia's Launcher and the build along the top rule, the headline, one sentence, PLAY and
            // Setup, the readiness steps as milestone chips on the rings, the hardware along the bottom rule
            // (Refresh2 and UpdateBanner fill them in). The strip of checks stays as the steps' data, not drawn.
            statusStrip = new StatusStrip();
            banner = new Hero { Location = new Point(0, 0), Size = new Size(2 * X + W, Hero.Rows),
                // (The name and the version are in the top bar already: the poster names the page.)
                Brand = L("Play", "Jogar", "Jouer"), BrandValue = L("Everything the game needs, checked", "Tudo o que o jogo precisa, verificado", "Tout ce qu'il faut au jeu, vérifié"),
                StepsTitle = L("Ready to play", "Pronto para jogar", "Prêt à jouer"),
                RightLabel = L("Build", "Versao", "Version"),
                RightValue = L("Xbox Series, run through VIctory", "Xbox Series, corrida pelo VIctory", "Xbox Series, lancée par VIctory"),
                Title = L("Ready", "Pronto", "Prêt"), Accent = L("when you are.", "quando quiseres.", "quand tu veux."),
                StepNames = new[] { L("Game", "Jogo", "Jeu"), L("Engine", "Motor", "Moteur"), L("Shaders", "Shaders", "Shaders"), "Windows" } };
            playButton.MouseDown += (s, e) => { if (playButton.Parent == banner && playButton.Enabled) banner.Ripple(); };
            heroGhost = new Pill { Text = L("Setup", "Preparar", "Préparer"), Font = Theme.Ui(10.5f), Size = new Size(Kit.PillW(L("Fix it", "Corrigir", "Corriger"), 10.5f, 128), 48), Location = new Point(X + 212, HeroButtonsY) };
            heroGhost.Click += (s, e) => Show(PageSetup);
            heroAuto = new Pill { Link = true, Text = "Auto-Setup", Font = Theme.Ui(10f), Size = new Size(104, 32), Location = new Point(X + 212 + heroGhost.Width + 10, HeroButtonsY + 8) };
            heroAuto.Click += (s, e) => { Show(PageSetup); DoEverything(); };
            banner.Controls.Add(heroGhost); banner.Controls.Add(heroAuto);
            page.Controls.Add(banner);
            // What is missing (Windows settings, Resizable BAR, shaders): the poster's sentence says it; these
            // stay as the data Refresh2 fills, off the page.
            warnPanel = new Panel(); warnText = new Label(); warnFix = new Pill();
            warnFix.Click += (s, e) => Show(PageSetup);

            int y = Hero.Rows + 28;

            // Game: the folder as one line with an underline, Auto-detect and Browse, what was found under it.
            gameHeading = new Heading(L("Game", "Jogo", "Jeu")) { Hair = false };
            page.Controls.Add(new Label { Text = L("Game", "Jogo", "Jeu"), Font = Theme.UiBold(10.5f), ForeColor = Theme.Text, BackColor = Theme.Back, UseMnemonic = false, Location = new Point(X, y + 7), Size = new Size(140, 22) });
            gameBox = new TextBox { ForeColor = Theme.Text, Font = Theme.Mono(9.5f) };
            gameBox.TextChanged += (s, e) => Refresh2();
            page.Controls.Add(FieldFor(gameBox, X + 150, y, W - 150 - 254, 34, true));
            var detect = new Pill { Text = L("Auto-detect", "Detetar", "Détecter"), Location = new Point(X + W - 238, y + 1), Size = new Size(120, 32) };
            detect.Click += (s, e) => RunSteps(steps.Where(st => st.Fix == (Func<bool>)FindGame).ToArray());
            page.Controls.Add(detect);
            var browse = new Pill { Text = L("Browse...", "Procurar...", "Parcourir..."), Location = new Point(X + W - 110, y + 1), Size = new Size(110, 32) };
            browse.Click += (s, e) => {
                using (var d = new FolderBrowserDialog { Description = L("Choose the folder that holds the game's game_scarlett*.exe", "Escolhe a pasta que tem o game_scarlett*.exe do jogo") })
                    if (d.ShowDialog(this) == DialogResult.OK) gameBox.Text = d.SelectedPath;
            };
            page.Controls.Add(browse);
            gameStatus = new Label { Location = new Point(X + 150, y + 42), Size = new Size(470, 20), ForeColor = Theme.Dim, BackColor = Theme.Back, Font = Theme.Ui(9f) };
            page.Controls.Add(gameStatus);
            page.Controls.Add(ExeBox(X + W - 240, y + 40, 240));
            y += 74;
            page.Controls.Add(new Rule { Location = new Point(X, y), Width = W });
            y += 14;

            // The engine: the standard one, or Belobug's for Radeon cards (the same choice as on Hardware & ID), set as two words.
            page.Controls.Add(new Label { Text = L("Engine", "Motor", "Moteur"), Font = Theme.UiBold(10.5f), ForeColor = Theme.Text, BackColor = Theme.Back, UseMnemonic = false, Location = new Point(X, y + 5), Size = new Size(140, 22) });
            var stdText = L("Standard · recommended", "Padrao · recomendado", "Standard · recommandé");
            var amdText = L("Belobug AMD · for Radeon", "Belobug AMD · para Radeon", "Belobug AMD · pour Radeon");
            int w1, w2;
            using (var lf = Theme.UiBold(10.5f)) { w1 = Kit.TextW(stdText, lf) + 28; w2 = Kit.TextW(amdText, lf) + 28; }
            var stdEngine = new Pill { Link = true, Text = stdText, Font = Theme.Ui(10.5f), Location = new Point(X + 150, y), Size = new Size(w1, 32) };
            var amdEngine = new Pill { Link = true, Text = amdText, Font = Theme.Ui(10.5f), Location = new Point(X + 150 + w1 + 12, y), Size = new Size(w2, 32) };
            var engineNote = new Label { Location = new Point(X + 150, y + 36), Size = new Size(W - 150, 20), ForeColor = Theme.Muted, BackColor = Theme.Back, Font = Theme.Ui(8.75f), AutoEllipsis = true };
            showEnginePills = () => {
                stdEngine.Main = !UseAmdEngine; amdEngine.Main = UseAmdEngine;
                stdEngine.Invalidate(); amdEngine.Invalidate();
                engineNote.Text = UseAmdEngine
                    ? L("Belobug's fork for Radeon cards, built from its source.", "O fork do Belobug para placas Radeon, compilado do codigo.", "Le fork de Belobug pour les cartes Radeon, compilé depuis son code.")
                    : L("On a Radeon card: if the game closes with \"device removed\", try Belobug AMD.", "Numa placa Radeon: se o jogo fechar com \"device removed\", experimenta o Belobug AMD.", "Sur une carte Radeon : si le jeu se ferme avec « device removed », essaie Belobug AMD.");
            };
            Action<bool> pickEngine = amd => {
                if (amd == UseAmdEngine) return;
                // The Hardware page's choice follows; its handler saves, logs and refreshes.
                if (engineBox != null) engineBox.SelectedIndex = amd ? 1 : 0;
                else { WriteSetting("engine", amd ? "amd" : "edupode"); Log(L("Engine: ", "Motor: ") + EngineName); UpdateBanner(); Refresh2(); }
                showEnginePills();
            };
            stdEngine.Click += (s, e) => pickEngine(false);
            amdEngine.Click += (s, e) => pickEngine(true);
            page.Controls.Add(stdEngine); page.Controls.Add(amdEngine); page.Controls.Add(engineNote);
            showEnginePills();
            y += 66;
            page.Controls.Add(new Rule { Location = new Point(X, y), Width = W });
            y += 30;

            // The presets, set as type: the name large, the resolution under it, the picked one in butter with a rule.
            presetHeading = new Heading(L("Quality preset", "Predefinicao de imagem", "Préréglage d'image")) { Location = new Point(X, y), Width = W - 240, Hair = false };
            page.Controls.Add(presetHeading);
            // The resolution the console reports to the game (VICTORY_DISPLAY_MODE): the preset's scale applies on top.
            outputBox = new Dropdown { Location = new Point(X + W - 220, y - 3), Width = 220 };
            outputBox.Items.AddRange(new object[] {
                L("Output 1080p (Full HD)", "Saida 1080p (Full HD)"),
                L("Output 1440p (2K)", "Saida 1440p (2K)"),
                L("Output 2160p (4K)", "Saida 2160p (4K)"),
                L("World at 720p (lighter)", "Mundo a 720p (mais leve)"),
            });
            outputBox.SelectedIndex = 0;
            outputBox.SelectedIndexChanged += (s, e) => { SaveSettings(); Refresh2(); };
            new ToolTip { AutoPopDelay = 20000 }.SetToolTip(outputBox, L(
                "The resolution the game draws its picture for; the preset's scale applies on top (Recommended at 4K draws 2560x1440 and upscales). Higher output: a sharper picture, more video memory for the screen's images (about +0.5 GB at 2K, +1.5 GB at 4K) and more work for the card; the CPU stays the limit on strong cards. 2K and 4K are new: tell us on Discord how they run.",
                "A resolucao para a qual o jogo desenha a imagem; a escala do preset aplica-se por cima (o Recomendado em 4K desenha 2560x1440 e amplia). Saida maior: imagem mais nitida, mais VRAM para as imagens do ecra (cerca de +0,5 GB em 2K, +1,5 GB em 4K) e mais trabalho para a placa; em placas fortes o limite continua a ser o processador. 2K e 4K sao novos: conta-nos no Discord como correm."));
            page.Controls.Add(outputBox);
            y += 42;
            for (int i = 0; i < CardOrder.Length; i++)
            {
                int k = CardOrder[i];
                // Seven presets and six 12 px gaps fill exactly W (the last one reaches the right edge).
                int cl = (int)Math.Round(i * (W + 12) / 7.0), cr = (int)Math.Round((i + 1) * (W + 12) / 7.0) - 12;
                var c = new ModeCard { Title = modes[k].Name, Tag2 = modes[k].Tag, Line = modes[k].Short, Location = new Point(X + cl, y), Size = new Size(cr - cl, 78), Tag = k };
                c.Click += (s, e) => { ManualChoice(); SelectMode(k); if (k == ModeCustom) Show(PageCustom); };
                cards.Add(c);
                page.Controls.Add(c);
            }
            y += 90;
            modeInfo = new Label { Location = new Point(X, y), Size = new Size(W - 120, 52), ForeColor = Theme.Soft, BackColor = Theme.Back, Font = Theme.Ui(9f) };
            page.Controls.Add(modeInfo);
            y += 66;

            // Launch options: three columns of flat rows (title, one muted line, the switch), a hairline between rows.
            page.Controls.Add(new Heading(L("Launch options", "Opcoes de arranque", "Options de lancement")) { Location = new Point(X, y), Width = W - 370, Hair = false });
            var best = new Pill { Text = L("Automatic for this PC", "Automatico para este PC", "Automatique pour ce PC"), Location = new Point(X + W - 354, y - 3), Size = new Size(200, 32) };
            best.Click += (s, e) => { WriteSetting("auto", "1"); AutoBest(true); };
            page.Controls.Add(best);
            var tweaks = new Pill { Text = L("Advanced Tweaks", "Ajustes avancados", "Réglages avancés"), Location = new Point(X + W - 146, y - 3), Size = new Size(146, 32) };
            tweaks.Click += (s, e) => Show(PageCustom);
            page.Controls.Add(tweaks);
            y += 44;
            // Three groups side by side: what you see, how fast it runs, picture fixes that cost speed.
            Func<int, int> colLeft = c0 => (int)Math.Round(c0 * (W + 28) / 3.0);
            string[] groups = {
                L("Display", "Imagem", "Affichage"),
                L("Performance", "Desempenho", "Performances"),
                L("Picture fixes", "Correcoes de imagem", "Corrections d'image"),
            };
            for (int c = 0; c < groups.Length; c++)
                page.Controls.Add(new Eyebrow(groups[c]) { Location = new Point(X + colLeft(c), y), Width = colLeft(c + 1) - colLeft(c) - 28 });
            y += 26;
            Func<string, string, int, int, bool, CheckBox> T = (title, line, col, row, on) => {
                var t = new OptionCard { Text = title, Line = line, Checked = on, Location = new Point(X + colLeft(col), y + row * 70), Size = new Size(colLeft(col + 1) - colLeft(col) - 28, 70) };
                t.CheckedChanged += (s, e) => Refresh2();
                // A click (mouse or keyboard) is the player's own choice; the launcher setting it is not.
                t.Click += (s, e) => ManualChoice();
                page.Controls.Add(t);
                return t;
            };
            borderlessBox = T(L("★ Borderless Fullscreen", "★ Ecra inteiro sem bordas"), L("Native desktop presentation", "Apresentacao nativa no ambiente de trabalho"), 0, 0, true);
            fps60Box = T(L("★ Unlock 60 FPS Clock", "★ Desbloquear relogio de 60 FPS"), L("Decoupled simulation tick (no 30 FPS cap)", "Tick de simulacao separado (sem limite de 30 FPS)"), 0, 1, true);
            panelBox = T(L("★ Diagnostics Overlay (F3)", "★ Overlay de diagnostico (F3)"), L("FPS, frame time, VRAM & RAM", "FPS, tempo de frame, VRAM e RAM"), 0, 2, true);
            smoothBox = T(FgTitle(), FrameGenLine(), 0, 3, true);
            reserveBox = T(L("◇ Keep the most video memory", "◇ Guardar o maximo de VRAM"), L("Windows reserves video memory for the game", "O Windows reserva VRAM para o jogo"), 1, 0, true);
            cpuBox = T(L("★ More CPU cores", "★ Mais nucleos do processador"), L("Half the cores copy the game's memory (+1 FPS)", "Metade dos nucleos copia a memoria do jogo (+1 FPS)"), 1, 1, true);
            warmBox = T(L("◇ Load pipelines before the game", "◇ Carregar pipelines antes do jogo"), L("No stutter the first time you see a place", "Sem engasgos a primeira vez que ves um sitio"), 1, 2, true);
            fightsBox = T(L("★ Crowds and fights", "★ Multidoes e lutas"), L("2 GB of the game's buffers on the card", "2 GB dos buffers do jogo na placa"), 1, 3, true);
            // Anti-stretch is off for everyone and hidden since 0.3.14 (it turned the picture cyan and dark): the
            // box stays for the code that reads it, out of sight, and the picture fixes move up into its place.
            qualityBox = T(L("Anti-stretch", "Anti-esticados"), L("Fewer stretched bodies; several FPS slower", "Menos corpos esticados; varios FPS mais lento"), 2, 3, false);
            qualityBox.Visible = false;
            facesBox = T(L("◇ Rainbow-face fix", "◇ Correcao das caras arco-iris"), L("Textures finish uploading first; costs FPS", "Texturas acabam de subir primeiro; custa FPS"), 2, 0, false);
            loadingBox = T(L("◇ Fast loading", "◇ Carregamento rapido"), L("Shorter loading pauses; untested", "Pausas de carregamento mais curtas; nao testado"), 2, 1, false);
            y += 4 * 70 + 14;
            // Which frame generation, when the switch above is on.
            page.Controls.Add(new Label { Text = L("Frame generation engine", "Motor de geracao de frames", "Moteur de génération d'images"), Location = new Point(X, y + 6), AutoSize = true, ForeColor = Theme.Soft, BackColor = Theme.Back, Font = Theme.Ui(9.5f) });
            fgBox = new Dropdown { Location = new Point(X + 220, y + 2), Width = 340, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Theme.Card, ForeColor = Theme.Text };
            fgBox.SelectedIndexChanged += (s, e) => PickFg();
            page.Controls.Add(fgBox);
            y += 38;
            // Which upscaler does the title's own: AMD's FSR at a render scale, or the title's as today.
            page.Controls.Add(new Label { Text = L("Upscaling", "Upscaling", "Upscaling"), Location = new Point(X, y + 6), AutoSize = true, ForeColor = Theme.Soft, BackColor = Theme.Back, Font = Theme.Ui(9.5f) });
            fsrBox = new Dropdown { Location = new Point(X + 220, y + 2), Width = 340, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Theme.Card, ForeColor = Theme.Text };
            fsrBox.SelectedIndexChanged += (s, e) => PickFsr();
            page.Controls.Add(fsrBox);
            y += 40;
            explain = new Label { Location = new Point(X, y), Size = new Size(W, 40), ForeColor = Theme.Muted, BackColor = Theme.Back, Font = Theme.Ui(9f),
                Text = L("Point at an option for the details. Tested: tried on edupode's PC (RTX 5070 Ti laptop).", "Passa o rato por uma opcao para ver os detalhes. Testado: experimentado no PC do edupode (portatil RTX 5070 Ti).", "Survole une option pour les détails. Testé : essayé sur le PC d'edupode (portable RTX 5070 Ti).") };
            page.Controls.Add(explain);
            var tipsBox = new ToolTip { AutoPopDelay = 20000, InitialDelay = 300 };
            foreach (var kv in Explanations())
            {
                var t = kv.Key; var text = kv.Value;
                tipsBox.SetToolTip(t, text);
                t.MouseEnter += (s, e) => { explain.Text = text; };
            }
            y += 56;

            // Activity: the loading bar shows while a task runs; the log stays folded until asked for.
            page.Controls.Add(new Rule { Location = new Point(X, y), Width = W });
            y += 18;
            page.Controls.Add(new Heading(L("Activity", "Atividade", "Activité")) { Location = new Point(X, y + 3), Width = 300, Hair = false });
            var showLog = new Pill { Text = L("Show activity", "Mostrar atividade", "Afficher l'activité"), Location = new Point(X + W - 190, y), Size = new Size(190, 32) };
            page.Controls.Add(showLog);
            y += 42;
            loadBar = new LoadBar { Location = new Point(X, y), Size = new Size(W, 26), Visible = false };
            page.Controls.Add(loadBar);
            y += 34;
            logBox = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, ForeColor = Theme.Dim, Font = Theme.Mono(8.5f) };
            logPanel = FieldFor(logBox, X, y, W, 160, true);
            Kit.Dark(logBox, "DarkMode_Explorer");
            logPanel.Visible = false;
            page.Controls.Add(logPanel);
            loadBarAnchor = showLog;
            showLog.Click += (s, e) => {
                // Placed again under the button first: a hidden control is not moved when the page scrolls.
                if (!logPanel.Visible) logPanel.Top = showLog.Top + 76;
                logPanel.Visible = !logPanel.Visible;
                showLog.Text = logPanel.Visible ? L("Hide activity", "Esconder atividade", "Masquer l'activité") : L("Show activity", "Mostrar atividade", "Afficher l'activité");
                showLog.Invalidate();
                if (logPanel.Visible) page.ScrollControlIntoView(logPanel);
            };
            page.Controls.Add(new Label { Location = new Point(X, y + 8), Size = new Size(10, 10), BackColor = Theme.Back });
            Fluid(page, X, W, null);
            return page;
        }

        // Where the poster's buttons sit (PLAY at its left margin, Setup beside it).
        const int HeroButtonsY = 256;
        Pill heroGhost, heroAuto;
        Panel footPanel;
        Action layoutFoot;

        Field logPanel;

        Label explain;

        // What each option does, and whether it is part of the setup tested on edupode's PC.
        Dictionary<CheckBox, string> Explanations()
        {
            var d = new Dictionary<CheckBox, string>();
            d[panelBox] = L("★ Shows FPS (now, last-minute average, whole-run average, worst), frame time, draws, video memory and RAM in the upper left corner. F3 hides it.",
                            "★ Mostra os FPS (agora, media do ultimo minuto, media total, pior), tempo por frame, draws, VRAM e RAM no canto superior esquerdo. F3 esconde.");
            d[borderlessBox] = L("★ A window without borders over the whole screen (test A: 22-24 FPS on edupode's PC). Needed for Smooth Motion and quick Alt+Tab.",
                                 "★ Janela sem bordas no ecra todo (teste A: 22-24 FPS no PC do edupode). Precisa-se para o Smooth Motion e para Alt+Tab rapido.");
            d[fps60Box] = L("★ The game asks for 30 FPS; this lets its clock run up to 60, so cutscenes and menus go past 30 when the PC can.",
                            "★ O jogo pede 30 FPS; isto deixa o relogio ir ate 60, por isso cutscenes e menus passam dos 30 quando o PC consegue.");
            d[smoothBox] = L("New: NVIDIA's driver frame generation for RTX 40/50. The launcher writes it into the driver's profile for victory.exe, no NVIDIA App needed. Doubles the frames shown; adds a little input lag.",
                             "Novo: a geracao de frames do driver NVIDIA para RTX 40/50. O launcher escreve-a no perfil do driver para o victory.exe, sem NVIDIA App. Duplica os frames mostrados; junta um pouco de atraso.");
            d[reserveBox] = L("New: asks Windows to reserve video memory for the game, so other apps are moved out first when memory runs short. Fewer black and flat textures.",
                              "Novo: pede ao Windows uma reserva de VRAM para o jogo, por isso as outras apps saem primeiro quando falta memoria. Menos texturas pretas e lisas.");
            d[fightsBox] = L("Keeps 2 GB of the game's buffers on the card instead of 1.5: for crowds and fights, where the FPS drops. Needs a 12 GB card. Tested on edupode's PC: crowds 14-17 FPS instead of 10-15.",
                             "Mantem 2 GB de buffers do jogo na placa em vez de 1,5: para multidoes e lutas, onde os FPS caem. Precisa de placa de 12 GB. Testado no PC do edupode: multidoes a 14-17 FPS em vez de 10-15.");
            d[qualityBox] = L("Waits for every copy of the game's buffers before drawing: fewer stretched bodies ('spaghetti'), but several FPS slower.",
                              "Espera por todas as copias dos buffers do jogo antes de desenhar: menos corpos esticados ('esparguete'), mas varios FPS mais lento.");
            d[facesBox] = L("Waits for textures to finish uploading before use: fixes rainbow faces, costs FPS.",
                            "Espera que as texturas acabem de subir antes de as usar: corrige as caras arco-iris, custa FPS.");
            d[cpuBox] = L("★ Tested on edupode's PC: +1 FPS in the open world (21.6 vs 20.5) and 2.3 ms less preparing each frame. The CPU is the limit (the card waits ~0 ms a frame): 6 helper threads used to refresh the game's buffers and textures (~18 ms of a 44 ms frame); this gives them half your cores (up to 12) and asks Windows which pages changed on a thread of its own.",
                          "★ Testado no PC do edupode: +1 FPS no mundo aberto (21,6 contra 20,5) e menos 2,3 ms a preparar cada frame. O limite e o processador (a placa espera ~0 ms por frame): 6 ajudantes refrescavam os buffers e as texturas do jogo (~18 ms de um frame de 44 ms); isto da-lhes metade dos teus nucleos (ate 12) e pergunta ao Windows que paginas mudaram numa thread a parte.");
            d[warmBox] = L("New: before the game starts, a loading bar makes every pipeline earlier sessions drew with, in separate processes, so the driver has them ready: no stutter the first time you see a place. Takes seconds once the cache is ready; after a VIctory or driver update it takes longer, once.",
                           "Novo: antes de o jogo arrancar, uma barra de carregamento prepara todas as pipelines usadas nas sessoes anteriores, em processos a parte, para o driver ja as ter: sem engasgos a primeira vez que ves um sitio. Demora segundos com a cache pronta; depois de atualizar o VIctory ou o driver demora mais, uma vez.");
            d[loadingBox] = L("Experimental: textures the game builds on the CPU no longer wait for the game's memory to settle. Shorter loading pauses; untested.",
                              "Experimental: as texturas que o jogo prepara no processador deixam de esperar que a memoria assente. Pausas de carregamento mais curtas; nao testado.");
            return d;
        }

        // The setup that worked on edupode's PC: 720p with light textures, borderless (test A), 60 FPS clock, overlay;
        // plus the new video-memory reserve and Smooth Motion.
        void Recommended(bool say = true)
        {
            SelectMode(0);
            panelBox.Checked = true; borderlessBox.Checked = true; fps60Box.Checked = true; reserveBox.Checked = true;
            smoothBox.Checked = rtxFrameGen;
            fightsBox.Checked = !integratedOnly && vramGb >= 11; cpuBox.Checked = true; qualityBox.Checked = false; facesBox.Checked = false; loadingBox.Checked = false; warmBox.Checked = true;
            if (say) Log(L("Recommended setup (= the TUDO + lutas shortcut): Recommended 720p, overlay, borderless, 60 FPS clock, 2 GB of buffers for crowds, video memory reserve" + (rtxFrameGen ? ", Smooth Motion." : "."),
                  "Configuracao recomendada (= atalho TUDO + lutas): Recomendado 720p, overlay, sem bordas, relogio de 60 FPS, 2 GB de buffers para multidoes, reserva de VRAM" + (rtxFrameGen ? ", Smooth Motion." : "."),
                  "Réglages recommandés (= raccourci TUDO + lutas) : Recommandé 720p, overlay, sans bordure, horloge à 60 FPS, 2 Go de buffers pour les foules, réserve de VRAM" + (rtxFrameGen ? ", Smooth Motion." : ".")));
            // Integrated graphics: the lightest setup, nothing that costs memory it does not have.
            if (integratedOnly) { SelectMode(ModeUltraLow); fps60Box.Checked = false; smoothBox.Checked = false; fightsBox.Checked = false; }
        }

        // Automatic settings (launcher.ini auto, on unless the player chose something): every time the launcher
        // opens, the preset this PC's card and memory hold best, the tested options, and the frame generation that is
        // tested on this card (NVIDIA Smooth Motion on an RTX 40/50; the experimental ones are never turned on by
        // themselves). A preset, an option or a frame generation the player picks turns it off; "Automatic for this
        // PC" turns it on again.
        bool AutoOn { get { return ReadSetting("auto") != "0"; } }

        void ManualChoice()
        {
            if (!AutoOn) return;
            WriteSetting("auto", "0");
            Log(L("Your own settings from now on (\"Automatic for this PC\" brings the automatic ones back).",
                  "As tuas definicoes a partir de agora (\"Automatico para este PC\" repoe as automaticas).",
                  "Tes propres réglages désormais (« Automatique pour ce PC » rétablit les automatiques)."));
        }

        // The preset for this PC: the lightest for integrated graphics, 16 GB of RAM or 6 GB of video memory or less;
        // Modest PC under 10 GB of video memory or 24 GB of RAM; else Recommended.
        int BestMode()
        {
            if (integratedOnly || (ramGb > 0 && ramGb <= 16.5) || (vramGb > 0 && vramGb <= 6.5)) return ModeUltraLow;
            if ((vramGb > 0 && vramGb < 10) || (ramGb > 0 && ramGb < 24)) return 2;
            return 0;
        }

        void AutoBest(bool loud)
        {
            // The preset is picked for this PC only when asked ("Automatic for this PC"), on a PC whose preset was
            // never picked, or after its card or memory changed; otherwise the one it plays with stays. 0.3.21 picked
            // it at every opening and moved an RX 7600 with 16 GB of RAM from Fastest (17 minutes of play) to Ultra
            // Low, which ended in its first frame.
            var hw = (gpuName ?? "") + "|" + Math.Round(vramGb) + "|" + Math.Round(ramGb);
            var known = ReadSetting("auto_hw");
            bool pick = loud || modeIndex < 0 || (!string.IsNullOrEmpty(known) && known != hw);
            int keep = modeIndex;
            Recommended(false);
            SelectMode(pick ? BestMode() : keep);
            WriteSetting("auto_hw", hw);
            if (FgChoice != "auto") { WriteSetting("fg_engine", "auto"); rtxFrameGen = canSmooth; FillFgBox(); }
            // Only the tested frame generation goes on by itself.
            smoothBox.Checked = rtxFrameGen && !integratedOnly;
            UpdateFgCard();
            SaveSettings();
            UpdateBanner(); Refresh2();
            if (loud || ReadSetting("auto_said") != Selected.Name)
            {
                WriteSetting("auto_said", Selected.Name);
                Log(string.Format(L("Automatic settings for this PC: {0}{1}, tested options only.", "Definicoes automaticas para este PC: {0}{1}, so opcoes testadas.", "Réglages automatiques pour ce PC : {0}{1}, options testées seulement."),
                    Selected.Name, rtxFrameGen ? ", NVIDIA Smooth Motion" : ""));
            }
        }

        void SelectMode(int k)
        {
            if (k < 0 || k >= modes.Length) return;
            modeIndex = k;
            foreach (var c in cards) c.Selected = (int)c.Tag == k;
            Refresh2();
        }

        Mode Selected { get { return modeIndex >= 0 ? modes[modeIndex] : modes[0]; } }

        // ---------------------------------------------------------------- Performance page
        Panel MemoryPage()
        {
            var page = Page();
            const int X = 40, W = 900;
            int y = PageTop(page, L("Performance", "Desempenho", "Performances"), L("Memory", "Memoria", "Mémoire"), L("measured.", "medida.", "mesurée."), L("Video memory and RAM, and which apps are holding them.", "Memoria de video e RAM, e que apps as estao a ocupar.", "Mémoire vidéo et RAM, et quelles applis les occupent."));
            vramSummary = new Label { Location = new Point(X, y), Size = new Size(W, 46), ForeColor = Theme.Text, BackColor = Theme.Back, Font = new Font("Segoe UI Semibold", 10.5f) };
            page.Controls.Add(vramSummary);
            y += 54;
            vramList = new ListView { Location = new Point(X, y), Size = new Size(W, 230), View = View.Details, CheckBoxes = true, FullRowSelect = true, BackColor = Theme.Card, ForeColor = Theme.Text, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10f), HeaderStyle = ColumnHeaderStyle.Nonclickable, OwnerDraw = true };
            vramList.Columns.Add(L("App", "App"), 230);
            vramList.Columns.Add(L("Video memory", "VRAM"), 130, HorizontalAlignment.Right);
            vramList.Columns.Add("RAM", 110, HorizontalAlignment.Right);
            vramList.Columns.Add(L("Note", "Nota"), 400);
            DarkHeader(vramList);
            // The note column takes what the other three leave.
            vramList.Resize += (s, e) => {
                int rest = vramList.ClientSize.Width - vramList.Columns[0].Width - vramList.Columns[1].Width - vramList.Columns[2].Width;
                if (rest > 120) vramList.Columns[3].Width = rest;
            };
            // Nothing big found: the empty list says so.
            vramEmpty = new Label { Text = L("No app is holding much video memory or RAM.", "Nenhuma app ocupa muita VRAM ou RAM.", "Aucune appli n'occupe beaucoup de VRAM ou de RAM."),
                TextAlign = ContentAlignment.MiddleCenter, ForeColor = Theme.Dim, BackColor = Theme.Card, Location = new Point(0, 40), Size = new Size(W, 60), Visible = false };
            vramList.Controls.Add(vramEmpty);
            vramList.Resize += (s, e) => vramEmpty.Width = vramList.ClientSize.Width;
            vramList.ItemCheck += (s, e) => {
                var u = vramList.Items[e.Index].Tag as MemoryUser;
                if (u != null && u.Protected) e.NewValue = CheckState.Unchecked;
            };
            page.Controls.Add(vramList);
            y += 244;
            var refresh = new Pill { Text = L("Refresh", "Atualizar", "Actualiser"), Location = new Point(X, y), Size = new Size(140, 34) };
            refresh.Click += (s, e) => ScanMemory();
            page.Controls.Add(refresh);
            var close = new Pill { Text = L("Close the checked apps", "Fechar as apps marcadas"), Location = new Point(X + 150, y), Size = new Size(230, 34) };
            close.Click += (s, e) => CloseChecked();
            page.Controls.Add(close);
            y += 58;

            page.Controls.Add(new Heading("NVIDIA Smooth Motion") { Location = new Point(X, y), Width = W });
            y += 34;
            smoothStatus = new Label { Location = new Point(X, y), Size = new Size(W, 58), ForeColor = Theme.Text, BackColor = Theme.Back };
            page.Controls.Add(smoothStatus);
            y += 62;
            var smoothOn = new Pill { Text = L("Turn it on now", "Ligar agora"), Location = new Point(X, y), Size = new Size(170, 34) };
            smoothOn.Click += (s, e) => ApplySmooth(true, true);
            page.Controls.Add(smoothOn);
            var smoothOff = new Pill { Text = L("Turn it off", "Desligar"), Location = new Point(X + 180, y), Size = new Size(150, 34) };
            smoothOff.Click += (s, e) => ApplySmooth(false, true);
            page.Controls.Add(smoothOff);
            y += 58;

            page.Controls.Add(new Heading(L("Getting the most video memory", "Ter o maximo de VRAM")) { Location = new Point(X, y), Width = W });
            y += 34;
            var tips = new Label { Location = new Point(X, y), Size = new Size(W, 250), ForeColor = Theme.Dim, BackColor = Theme.Back };
            tips.Text = string.Join("\r\n", Pt ? new[] {
                "Mais VRAM livre = mais texturas na placa = menos coisas pretas, texturas lisas e recarregamentos.",
                "•  NVIDIA App > Definicoes > Funcionalidades: desliga o Instant Replay e o overlay (o 'NVIDIA Overlay' pode ter 1-2 GB).",
                "•  Discord: Definicoes > Avancado > Aceleracao de hardware: desligada. Wallpaper Engine: pausa ou fecha.",
                "•  Browsers, emuladores e outros jogos: fecha-os enquanto jogas.",
                "•  Portateis com MUX (PredatorSense, Armoury Crate, Legion Vantage): no modo Hibrido/Optimus o ambiente de trabalho",
                "   corre na grafica integrada e liberta normalmente 0,5-1,5 GB na NVIDIA.",
                "•  'Guardar o maximo de VRAM' pede ao Windows uma reserva para o VIctory: quando outra app pede memoria, a dela sai primeiro.",
                "•  O Smooth Motion e escrito no perfil do driver para victory.exe (como o NVIDIA Profile Inspector): nao precisa da NVIDIA App.",
            } : Fr ? new[] {
                "Plus de VRAM libre = plus de textures sur la carte = moins d'objets noirs, de textures plates et de rechargements.",
                "•  NVIDIA App > Paramètres > Fonctionnalités : désactive Instant Replay et l'overlay ('NVIDIA Overlay' peut prendre 1-2 Go).",
                "•  Discord : Paramètres > Avancés > Accélération matérielle : désactivée. Wallpaper Engine : mets-le en pause ou quitte-le.",
                "•  Navigateurs, émulateurs et autres jeux : ferme-les pendant que tu joues.",
                "•  Portables avec MUX (PredatorSense, Armoury Crate, Legion Vantage) : en mode Hybride/Optimus le bureau",
                "   tourne sur la carte intégrée, ce qui libère en général 0,5-1,5 Go sur la carte NVIDIA.",
                "•  'Garder le max de VRAM' demande à Windows une réserve pour VIctory : les autres applis sortent en premier.",
                "•  Smooth Motion est écrit dans le profil du pilote pour victory.exe (comme NVIDIA Profile Inspector) : pas besoin de NVIDIA App.",
            } : new[] {
                "More free video memory = more textures on the card = fewer black objects, flat textures and reloads.",
                "•  NVIDIA App > Settings > Features: turn off Instant Replay and the overlay ('NVIDIA Overlay' can hold 1-2 GB).",
                "•  Discord: Settings > Advanced > Hardware acceleration: off. Wallpaper Engine: pause or quit it.",
                "•  Browsers, emulators and other games: close them while you play.",
                "•  Laptops with a MUX switch (PredatorSense, Armoury Crate, Legion Vantage): in Hybrid/Optimus mode the desktop",
                "   runs on the integrated GPU, which usually frees 0.5-1.5 GB on the NVIDIA card.",
                "•  'Keep the most video memory' asks Windows to reserve memory for VIctory: other apps are moved out first.",
                "•  Smooth Motion is written into the driver's profile for victory.exe (as NVIDIA Profile Inspector does): no NVIDIA App needed.",
            });
            page.Controls.Add(tips);
            var tipsEnd = new Label { Location = new Point(X, y + 260), Size = new Size(10, 10), BackColor = Theme.Back };
            page.Controls.Add(tipsEnd);
            Fluid(page, X, W, w => {
                // The tips wrap with the window: the label is as tall as its text.
                var h = TextRenderer.MeasureText(tips.Text, tips.Font, new Size(w, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
                tips.Height = h + 6;
                tipsEnd.Top = tips.Bottom + 4;
            });
            return page;
        }

        Label vramEmpty;

        static void DarkHeader(ListView list)
        {
            list.DrawColumnHeader += (s, e) => {
                using (var b = new SolidBrush(Theme.Side)) e.Graphics.FillRectangle(b, e.Bounds);
                using (var b = new SolidBrush(Theme.Dim))
                using (var f = new Font("Segoe UI Semibold", 9.5f))
                {
                    var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = e.Header.TextAlign == HorizontalAlignment.Right ? StringAlignment.Far : StringAlignment.Near };
                    var r = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height);
                    e.Graphics.DrawString(e.Header.Text, f, b, r, sf);
                }
            };
            list.DrawItem += (s, e) => { e.DrawDefault = true; };
            list.DrawSubItem += (s, e) => { e.DrawDefault = true; };
        }

        static readonly HashSet<string> ProtectedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "dwm", "csrss", "explorer", "System", "Idle", "svchost", "winlogon", "services", "lsass", "smss", "wininit",
            "fontdrvhost", "sihost", "ShellExperienceHost", "StartMenuExperienceHost", "SearchHost", "TextInputHost",
            "ctfmon", "RuntimeBroker", "audiodg", "Memory Compression", "Registry", "victory", "victory-ferramenta",
            "edu-launcher", "Edu's Remake", "Julia's Launcher", "MsMpEng", "NVDisplay.Container", "nvcontainer", "claude", "msedgewebview2", "conhost",
            "powershell", "pwsh", "cmd", "WindowsTerminal", "OpenConsole", "ApplicationFrameHost", "LockApp",
            "SecurityHealthSystray", "Taskmgr", "PredatorSense", "Secure System", "LsaIso", "spoolsv", "dllhost",
        };

        // Per-app video memory on the busiest card (the NVIDIA one), from the "GPU Process Memory" counters
        // Task Manager uses; and the card's total in use. Empty if the counters are not there.
        static List<MemoryUser> GpuUsers(out long cardUsed)
        {
            cardUsed = 0;
            var byLuid = new Dictionary<string, Dictionary<int, long>>();
            try
            {
                var data = new PerformanceCounterCategory("GPU Process Memory").ReadCategory()["Dedicated Usage"];
                foreach (InstanceData d in data.Values)
                {
                    // pid_1234_luid_0x00000000_0x0001ca5c_phys_0
                    var parts = d.InstanceName.Split('_');
                    if (parts.Length < 5 || parts[0] != "pid") continue;
                    int pid;
                    if (!int.TryParse(parts[1], out pid)) continue;
                    var luid = parts[3] + "_" + parts[4];
                    Dictionary<int, long> per;
                    if (!byLuid.TryGetValue(luid, out per)) byLuid[luid] = per = new Dictionary<int, long>();
                    long was;
                    per.TryGetValue(pid, out was);
                    per[pid] = was + d.RawValue;
                }
                var adapters = new PerformanceCounterCategory("GPU Adapter Memory").ReadCategory()["Dedicated Usage"];
                string best = null;
                foreach (InstanceData d in adapters.Values)
                    if (d.RawValue > cardUsed) { cardUsed = d.RawValue; best = d.InstanceName; }
                if (best == null) return new List<MemoryUser>();
                var bp = best.Split('_');
                var key = bp.Length >= 3 ? bp[1] + "_" + bp[2] : "";
                Dictionary<int, long> card;
                if (!byLuid.TryGetValue(key, out card)) return new List<MemoryUser>();
                var users = new Dictionary<string, MemoryUser>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in card)
                {
                    string name;
                    try { name = Process.GetProcessById(kv.Key).ProcessName; } catch (Exception) { continue; }
                    MemoryUser u;
                    if (!users.TryGetValue(name, out u)) users[name] = u = new MemoryUser { Name = name };
                    u.Vram += kv.Value;
                }
                return users.Values.ToList();
            }
            catch (Exception) { return new List<MemoryUser>(); }
        }

        void ScanMemory()
        {
            vramSummary.Text = L("Looking...", "A ver...");
            new Thread(() => {
                long cardUsed;
                var users = GpuUsers(out cardUsed);
                var byName = users.ToDictionary(u => u.Name, StringComparer.OrdinalIgnoreCase);
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        MemoryUser u;
                        if (!byName.TryGetValue(p.ProcessName, out u)) byName[p.ProcessName] = u = new MemoryUser { Name = p.ProcessName };
                        u.Ram += p.PrivateMemorySize64;
                    }
                    catch (Exception) { }
                }
                long victory = 0;
                MemoryUser self;
                if (byName.TryGetValue("victory", out self)) victory = self.Vram;
                var shown = byName.Values.Where(u => u.Vram >= (40L << 20) || u.Ram >= (400L << 20)).OrderByDescending(u => u.Vram * 4 + u.Ram).ToList();
                foreach (var u in shown)
                {
                    u.Protected = ProtectedNames.Contains(u.Name);
                    if (u.Name.Equals("dwm", StringComparison.OrdinalIgnoreCase)) u.Note = L("Windows desktop: see the MUX tip below", "Ambiente de trabalho: ve a dica do MUX em baixo");
                    else if (u.Name.Equals("victory", StringComparison.OrdinalIgnoreCase)) u.Note = L("the game", "o jogo");
                    else if (u.Name.StartsWith("NVIDIA Overlay", StringComparison.OrdinalIgnoreCase)) u.Note = L("turn off Instant Replay / overlay in NVIDIA App", "desliga Instant Replay / overlay na NVIDIA App");
                    else if (u.Name.Equals("Discord", StringComparison.OrdinalIgnoreCase)) u.Note = L("or turn off its hardware acceleration", "ou desliga a aceleracao de hardware");
                    else if (u.Name.StartsWith("wallpaper", StringComparison.OrdinalIgnoreCase)) u.Note = "Wallpaper Engine";
                    else if (u.Name.Equals("msedgewebview2", StringComparison.OrdinalIgnoreCase)) u.Note = L("part of other apps (protected)", "parte de outras apps (protegido)");
                    else if (u.Protected) u.Note = L("system or needed (protected)", "sistema ou necessario (protegido)");
                }
                var ram = new Microsoft.VisualBasic.Devices.ComputerInfo();
                double ramTotal = ram.TotalPhysicalMemory / 1073741824.0, ramFree = ram.AvailablePhysicalMemory / 1073741824.0;
                BeginInvoke(new Action(() => {
                    double gb = 1073741824.0;
                    var others = Math.Max(0, cardUsed - victory);
                    vramSummary.Text = cardUsed == 0
                        ? L("Could not read the video memory counters (they need Windows 10 1709 or later).", "Nao deu para ler os contadores de VRAM (precisam do Windows 10 1709 ou mais recente).")
                        : string.Format(L("Card: {0:0.0} of {1:0.0} GB in use  —  VIctory {2:0.0} GB, everything else {3:0.0} GB\r\nRAM: {4:0.0} of {5:0.0} GB free",
                                          "Placa: {0:0.0} de {1:0.0} GB em uso  —  VIctory {2:0.0} GB, o resto {3:0.0} GB\r\nRAM: {4:0.0} de {5:0.0} GB livres"),
                                        cardUsed / gb, vramGb, victory / gb, others / gb, ramFree, ramTotal);
                    vramSummary.ForeColor = others > (2L << 30) ? Theme.Warn : Theme.Text;
                    vramList.BeginUpdate();
                    vramList.Items.Clear();
                    foreach (var u in shown)
                    {
                        var item = new ListViewItem(new[] { u.Name, u.Vram > 0 ? (u.Vram >> 20) + " MB" : "-", (u.Ram >> 20) + " MB", u.Note }) { Tag = u };
                        if (u.Protected) item.ForeColor = Theme.Dim;
                        vramList.Items.Add(item);
                    }
                    vramList.EndUpdate();
                    vramEmpty.Visible = vramList.Items.Count == 0;
                }));
            }) { IsBackground = true }.Start();
        }

        void CloseChecked()
        {
            var names = vramList.CheckedItems.Cast<ListViewItem>().Select(i => i.Tag as MemoryUser).Where(u => u != null && !u.Protected).Select(u => u.Name).ToList();
            if (names.Count == 0) { MessageBox.Show(this, L("Check the apps to close first.", "Marca primeiro as apps a fechar.")); return; }
            var ask = L("Close these apps? Unsaved work in them is lost.\r\n\r\n", "Fechar estas apps? O que nao estiver guardado nelas perde-se.\r\n\r\n") + string.Join("\r\n", names);
            if (MessageBox.Show(this, ask, "Julia's Launcher", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
            foreach (var n in names)
                foreach (var p in Process.GetProcessesByName(n))
                {
                    try { if (!p.CloseMainWindow() || !p.WaitForExit(1500)) p.Kill(); }
                    catch (Exception) { }
                }
            Thread.Sleep(800);
            ScanMemory();
        }

        // ---------------------------------------------------------------- Smooth Motion
        void UpdateSmooth()
        {
            if (smoothStatus == null) return;
            string state = smoothState == "on" ? L("ON for victory.exe", "LIGADO para o victory.exe")
                : smoothState == "off" ? L("off for victory.exe", "desligado para o victory.exe")
                : smoothState == "not set" ? L("not set yet (PLAY sets it when the option is on)", "ainda nao configurado (o JOGAR configura com a opcao ligada)")
                : smoothState;
            smoothStatus.Text = L("Driver frame generation: the card shows a made-up frame between two of the game's. Now: ", "Geracao de frames do driver: a placa mostra um frame inventado entre dois do jogo. Agora: ") + state
                + (rtxFrameGen ? "" : L("\r\nNeeds an RTX 40 or 50 series card and driver 571.86 or newer.", "\r\nPrecisa de uma placa RTX 40 ou 50 e do driver 571.86 ou mais recente."));
            smoothStatus.ForeColor = smoothState == "on" ? Theme.Good : Theme.Text;
        }

        void ApplySmooth(bool on, bool loud)
        {
            new Thread(() => {
                var err = SmoothMotion.Set(on);
                var s = SmoothMotion.Status();
                BeginInvoke(new Action(() => {
                    smoothState = s;
                    UpdateSmooth();
                    UpdateBanner();
                    if (err != null) Log("Smooth Motion: " + err);
                    else Log(on ? L("Smooth Motion is on for victory.exe (driver profile).", "Smooth Motion ligado para o victory.exe (perfil do driver).")
                                : L("Smooth Motion is off for victory.exe.", "Smooth Motion desligado para o victory.exe."));
                    if (loud && err != null) MessageBox.Show(this, "Smooth Motion: " + err, "Julia's Launcher");
                }));
            }) { IsBackground = true }.Start();
        }

        // ---------------------------------------------------------------- Controls and Help
        // The controls grouped by what the player is doing (on foot, talking, fighting, driving, flying, menus),
        // each line: what it does, the key, the pad. Keys are drawn as key caps.
        Panel ControlsPage()
        {
            var page = Page();
            const int X = 40, W = 900;
            int y = PageTop(page, L("Controls", "Controlos", "Commandes"), L("Controls", "Controlos", "Commandes"), L("by what you do.", "pelo que fazes.", "selon l'action."), null);
            page.Controls.Add(Small(L("Keyboard and mouse, and the Xbox controller button for each. A controller connected to the PC replaces all of this.",
                                      "Teclado e rato, e o botao do comando Xbox de cada um. Um comando ligado ao PC substitui tudo isto.",
                                      "Clavier et souris, et le bouton de la manette Xbox pour chacun. Une manette branchée au PC remplace tout cela."), X, y, W) );
            page.Controls[page.Controls.Count - 1].Height = 38;
            y += 40;
            Func<string, string, string, string[]> row = (what, key, pad) => new[] { what, key, pad };
            var groups = new List<Tuple<string, string[][]>> {
                Tuple.Create(L("On foot", "A pe", "À pied"), new[] {
                    row(L("Move", "Andar", "Se déplacer"), "W A S D", L("Left stick", "Analogico esq.", "Stick gauche")),
                    row(L("Look around (click the game to capture the mouse)", "Olhar (clica no jogo para capturar o rato)", "Regarder (clique dans le jeu pour capturer la souris)"), L("Mouse", "Rato", "Souris"), L("Right stick", "Analogico dir.", "Stick droit")),
                    row(L("Run", "Correr", "Courir"), L("Left Shift", "Shift esq.", "Maj gauche"), "L3"),
                    row(L("Jump, climb", "Saltar, escalar", "Sauter, grimper"), L("Space / Enter", "Espaco / Enter", "Espace / Entrée"), "A"),
                    row(L("Crouch, rest", "Agachar, descansar", "S'accroupir, se reposer"), L("Left Ctrl / C / Backspace", "Ctrl esq. / C / Backspace", "Ctrl gauche / C / Retour"), "B"),
                    row(L("Take cover", "Proteger-se", "Se mettre à couvert"), "Q", "RB"),
                    row(L("Look back", "Olhar para tras", "Regarder derrière"), "V", "R3"),
                }),
                Tuple.Create(L("Talking and using things", "Falar e usar coisas", "Parler et utiliser"), new[] {
                    row(L("Interact, talk", "Interagir, falar", "Interagir, parler"), "E / Y", "Y"),
                    row(L("Aim, interact", "Mirar, interagir", "Viser, interagir"), L("Right mouse button", "Botao direito", "Clic droit"), "LT"),
                }),
                Tuple.Create(L("Fighting", "Lutar", "Combat"), new[] {
                    row(L("Shoot", "Disparar", "Tirer"), L("Left mouse button", "Botao esquerdo", "Clic gauche"), "RT / RB"),
                    row(L("Punch, kick", "Soco, pontape", "Coup de poing, de pied"), "R / X", "X"),
                    row(L("Block", "Bloquear", "Bloquer"), L("Space", "Espaco", "Espace"), "A"),
                    row(L("Inventory, weapon wheel (hold)", "Inventario, roda de armas (manter)", "Inventaire, roue des armes (maintenir)"), "Tab", "LB"),
                }),
                Tuple.Create(L("Driving (press F: a high beep means vehicle mode)", "Conduzir (carrega no F: um apito agudo e o modo veiculo)", "Conduire (appuie sur F : un bip aigu = mode véhicule)"), new[] {
                    row(L("Get in, get out", "Entrar, sair", "Monter, descendre"), "F", L("Y + vehicle mode", "Y + modo veiculo", "Y + mode véhicule")),
                    row(L("Accelerate", "Acelerar", "Accélérer"), "W", "RT"),
                    row(L("Brake, reverse", "Travar, marcha-atras", "Freiner, reculer"), "S", "LT"),
                    row(L("Steer", "Virar", "Tourner"), "A / D", L("Left stick", "Analogico esq.", "Stick gauche")),
                    row(L("Handbrake", "Travao de mao", "Frein à main"), L("Space", "Espaco", "Espace"), "A"),
                    row(L("Horn, nitro", "Buzina, nitro", "Klaxon, nitro"), L("Left Shift", "Shift esq.", "Maj gauche"), "L3"),
                    row(L("Headlights", "Farois", "Phares"), L("Left Ctrl / C", "Ctrl esq. / C", "Ctrl gauche / C"), "B"),
                    row(L("Draw a weapon", "Sacar arma", "Sortir une arme"), "Tab", "LB"),
                    row(L("Vehicle mode out of sync? Set it right", "Modo veiculo trocado? Acerta-o", "Mode véhicule décalé ? Le recaler"), "Caps Lock", "-"),
                }),
                Tuple.Create(L("Flying", "Voar", "Voler"), new[] {
                    row(L("Take off", "Levantar voo", "Décoller"), "W + K", "-"),
                    row(L("Nose down, nose up", "Nariz baixo, nariz cima", "Piquer, cabrer"), "I / K", L("Left stick up/down", "Analogico esq. cima/baixo", "Stick gauche haut/bas")),
                    row(L("Turn left", "Virar a esquerda", "Tourner à gauche"), "R / X", "X"),
                    row(L("Turn right", "Virar a direita", "Tourner à droite"), L("Left Ctrl / C", "Ctrl esq. / C", "Ctrl gauche / C"), "B"),
                }),
                Tuple.Create(L("Map, phone and menus", "Mapa, telemovel e menus", "Carte, téléphone et menus"), new[] {
                    row(L("Map", "Mapa", "Carte"), "M", "View"),
                    row(L("Phone, radio, partner", "Telemovel, radio, parceiro", "Téléphone, radio, partenaire"), L("Arrows / 1-4", "Setas / 1-4", "Flèches / 1-4"), L("D-pad", "D-pad", "Croix")),
                    row(L("Pause", "Pausa", "Pause"), "Esc", "Menu"),
                }),
                Tuple.Create(L("VIctory's own keys", "Teclas do VIctory", "Touches de VIctory"), new[] {
                    row(L("Release the mouse", "Soltar o rato", "Libérer la souris"), "Alt / F1", "-"),
                    row(L("Show or hide the overlay (FPS, memory)", "Mostrar ou esconder o overlay (FPS, memoria)", "Afficher ou masquer l'overlay (FPS, mémoire)"), "F3", "-"),
                    row(L("Record a frame (recording mode)", "Gravar um frame (modo de gravacao)", "Enregistrer une image (mode enregistrement)"), "F9", "-"),
                }),
            };
            foreach (var g in groups)
            {
                page.Controls.Add(new Heading(g.Item1) { Location = new Point(X, y), Width = W });
                y += 32;
                foreach (var r in g.Item2)
                {
                    page.Controls.Add(new Label { Text = r[0], Location = new Point(X + 8, y + 4), Size = new Size(500, 22), ForeColor = Theme.Text, BackColor = Theme.Back, Font = Theme.Ui(9.75f), AutoEllipsis = true });
                    page.Controls.Add(new KeyCap { Text = r[1], Location = new Point(X + 520, y), Size = new Size(220, 28) });
                    page.Controls.Add(new KeyCap { Text = r[2], Pad = true, Location = new Point(X + 752, y), Size = new Size(148, 28) });
                    y += 34;
                }
                y += 12;
            }
            page.Controls.Add(new Label { Location = new Point(X, y), Size = new Size(10, 10), BackColor = Theme.Back });
            Fluid(page, X, W, null);
            return page;
        }
        Panel HelpPage()
        {
            var page = Page();
            const int X = 40, W = 900;
            int y = PageTop(page, L("Help", "Ajuda", "Aide"), L("Help", "Ajuda", "Aide"), L("when you need it.", "quando precisares.", "quand il faut."), L("Everything the launcher does, and how to reach us when something breaks.", "Tudo o que o launcher faz, e como falar connosco quando algo avaria.", "Tout ce que fait le launcher, et comment nous joindre quand quelque chose casse."));
            var discord = new Pill { Main = true, Text = L("Send errors (GitHub / Discord)", "Enviar erros (GitHub / Discord)"), Location = new Point(X, y), Size = new Size(240, 38) };
            discord.Click += (s, e) => ReportErrors();
            page.Controls.Add(discord);
            var join = new Pill { Text = L("Join the Discord", "Entrar no Discord"), Location = new Point(X + 250, y), Size = new Size(170, 38) };
            join.Click += (s, e) => OpenUrl(DiscordInvite);
            page.Controls.Add(join);
            var logs = new Pill { Text = L("Open the logs folder", "Abrir a pasta dos logs"), Location = new Point(X + 430, y), Size = new Size(200, 38) };
            logs.Click += (s, e) => { Directory.CreateDirectory(Path.Combine(dataRoot, "logs")); OpenUrl(Path.Combine(dataRoot, "logs")); };
            page.Controls.Add(logs);
            var news = new Pill { Text = L("What's new", "Novidades", "Nouveautés"), Location = new Point(X + 640, y), Size = new Size(120, 38) };
            news.Click += (s, e) => ShowWhatsNew();
            page.Controls.Add(news);
            var safe = new Pill { Text = L("Is it safe?", "E seguro?", "Est-ce sûr ?"), Location = new Point(X + 770, y), Size = new Size(130, 38) };
            safe.Click += (s, e) => OpenUrl(SafetyUrl);
            page.Controls.Add(safe);
            y += 54;
            var text = new TextBox { Location = new Point(X, y), Size = new Size(W, 1180), Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, ForeColor = Theme.Text, BackColor = Theme.Back, Font = Theme.Ui(10f), ScrollBars = ScrollBars.None, TabStop = false };
            text.Text = string.Join("\r\n", FunctionList().Concat(new[] { "", L("ABOUT", "SOBRE"), L("Julia's Launcher is edupode's remake, built on the work of:", "O Julia's Launcher e o remake do edupode, feito com o trabalho de:") }).Concat(Credits()).ToArray());
            page.Controls.Add(text);
            var helpEnd = new Label { Location = new Point(X, y + 1190), Size = new Size(10, 10), BackColor = Theme.Back };
            page.Controls.Add(helpEnd);
            Fluid(page, X, W, w => {
                // Long lines wrap with the window: the text is as tall as it needs.
                var h = TextRenderer.MeasureText(text.Text, text.Font, new Size(w - 8, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
                text.Height = h + 16;
                helpEnd.Top = text.Bottom + 4;
            });
            return page;
        }

        // Every function, as the Help page and the README's download section list it.
        IEnumerable<string> FunctionList()
        {
            return Pt ? new[] {
                "JOGAR",
                "   •  Predefinicoes: Recomendado (720p), Mais rapido e PC modesto (540p), Ultra baixo (PCs com muito pouca memoria), Nitido e",
                "      cgbj classico (1080p), Personalizado (os teus valores). O launcher escolhe uma pelo teu hardware na primeira vez.",
                "   •  Opcoes de arranque: passa o rato por cada uma para ver o que faz. A ★ marca o que foi testado.",
                "   •  JOGAR prepara as pipelines, abre o jogo e, quando fecha, guarda as pipelines desta sessao para a proxima.",
                "   •  Um jogo preso (um erro, uma janela que deixou de responder) nunca bloqueia o JOGAR: aparece 'FECHAR E JOGAR'.",
                "   •  Se faltar alguma coisa (memoria virtual, energia, Resizable BAR, shaders), um aviso amarelo diz o que e e leva-te a corrigir.",
                "",
                "PREPARAR",
                "   •  'Fazer tudo por mim' corre os passos que faltam: motor, pasta do jogo, shaders, energia,",
                "      memoria virtual, placa grafica certa para o VIctory e geracao de frames. O Windows pede administrador quando e preciso.",
                "   •  Resizable BAR: o launcher ve o que o D3D12 da ao jogo (nao so a BIOS) e mostra o guia da tua placa-mae.",
                "",
                "SHADERS",
                "   •  Mostra em tempo real os shaders traduzidos, as pipelines prontas, as que falharam, o espaco em disco e os erros.",
                "   •  'Reparar' refaz o que falta; 'Refazer tudo' apaga a cache e prepara de novo.",
                "",
                "DESEMPENHO",
                "   •  Quem ocupa a VRAM e a RAM; marca apps e fecha-as antes de jogar. Liga ou desliga a geracao de frames.",
                "",
                "HARDWARE E ID",
                "   •  O teu ID permanente (VI-XXXX-XXXX), com botao de copiar. Vem de pecas do PC e nao muda ao reinstalar o Windows.",
                "   •  Partilha de hardware (desligada por defeito): envia SO o hardware (placa, processador, RAM, placa-mae, Windows,",
                "      tipo de disco). Nunca envia logs (essas tem uma escolha propria), opcoes, caminhos, nomes nem o IP. A pagina mostra exatamente o que e enviado.",
                "",
                "ATUALIZACOES",
                "   •  O launcher verifica o GitHub ao abrir e de 5 em 5 minutos. Um update so e instalado se a assinatura e o SHA-256",
                "      baterem certo; instala-se sozinho quando o jogo nao esta a correr. E a unica coisa que o launcher recebe da internet.",
                "",
                "AJUSTES AVANCADOS (PERSONALIZADO)",
                "   •  Resolucao, texturas, buffers, protecoes de memoria e relogio de FPS. 'Copiar o meu perfil' da um codigo (VI1-...)",
                "      para partilhar; cola o de um amigo na caixa e carrega em 'Aplicar' (mostra o que muda antes).",
                "",
                "LOGS E ERROS",
                "   •  Logs do jogo: " + Path.Combine(dataRoot, "logs") + " (session-*.log, prepare.log).",
                "   •  Erros do launcher: " + Program.ErrorFile,
                "   •  'Enviar erros no Discord': escolhes 'erros do launcher' ou 'erros do jogo' e escreves o que aconteceu. O launcher junta os",
                "      ficheiros certos num zip no Ambiente de trabalho, abre a pasta e diz-te para o enviares no canal \"" + DiscordChannel + "\".",
                "      Nada e enviado sozinho: es tu que envias.",
                "",
                "PLACAS",
                "   •  NVIDIA RTX 40/50: Smooth Motion. RTX 30: NVSmooth30 (experimental). Outras placas (GTX 10, RTX 20, AMD, Intel): geracao de frames VI",
                "      (experimental, desligada por defeito): um frame entre cada dois do jogo, feito so com as imagens, com meio frame de atraso.",
                "   •  Todas as placas (NVIDIA, AMD, Intel) jogam com o motor edupode; para Radeon ha a alternativa do Belobug.",
                "",
                "TECLAS NO JOGO",
                "   •  F3 mostra/esconde o overlay: FPS, media do ultimo minuto (AVG 1M), media total (ALL), pior meio segundo (LOW).",
                "   •  Alt ou F1 solta o rato. A pagina Controlos tem o resto.",
            } : Fr ? new[] {
                "JOUER",
                "   •  Préréglages : Recommandé (720p), Le plus rapide et PC modeste (540p), Ultra bas (PC avec très peu de mémoire), Net et",
                "      cgbj classique (1080p), Personnalisé (tes valeurs). Le launcher en choisit un selon ton matériel la première fois.",
                "   •  Options de lancement : survole chacune pour voir ce qu'elle fait. ★ marque ce qui a été testé.",
                "   •  JOUER charge les pipelines, lance le jeu et, à sa fermeture, garde les pipelines de cette session pour la prochaine.",
                "   •  Un jeu bloqué (une erreur, une fenêtre qui ne répond plus) ne bloque jamais JOUER : il devient 'FERMER ET JOUER'.",
                "   •  Quand il manque quelque chose (mémoire virtuelle, alimentation, Resizable BAR, shaders), un avis jaune dit quoi et t'emmène corriger.",
                "",
                "INSTALLATION",
                "   •  'Tout faire pour moi' lance les étapes manquantes : moteur, dossier du jeu, shaders, alimentation,",
                "      mémoire virtuelle, bonne carte graphique pour VIctory et génération d'images. Windows demande les droits administrateur si besoin.",
                "   •  Resizable BAR : le launcher vérifie ce que D3D12 donne au jeu (pas seulement le BIOS) et montre le guide de ta carte mère.",
                "",
                "SHADERS",
                "   •  Affiche en direct les shaders traduits, les pipelines prêtes, celles en échec, l'espace disque et les erreurs.",
                "   •  'Réparer' fait ce qui manque ; 'Tout refaire' vide le cache et prépare à nouveau.",
                "",
                "PERFORMANCES",
                "   •  Ce qui occupe la VRAM et la RAM ; coche des applis et ferme-les avant de jouer. Génération d'images activée ou non.",
                "",
                "MATÉRIEL ET ID",
                "   •  Ton ID permanent (VI-XXXX-XXXX), avec un bouton copier. Il vient des composants du PC et reste après une réinstallation de Windows.",
                "   •  Partage du matériel (désactivé par défaut) : envoie UNIQUEMENT le matériel (carte graphique, processeur, RAM, carte mère, Windows,",
                "      type de disque). Jamais de logs (ceux-ci ont leur propre choix), d'options, de chemins, de noms ni ton IP. La page montre exactement ce qui est envoyé.",
                "",
                "MISES À JOUR",
                "   •  Le launcher vérifie GitHub à l'ouverture et toutes les 5 minutes. Une mise à jour n'est installée que si sa signature et",
                "      son SHA-256 correspondent ; elle s'installe seule quand le jeu ne tourne pas. C'est la seule chose que le launcher reçoit d'internet.",
                "",
                "RÉGLAGES AVANCÉS (PERSONNALISÉ)",
                "   •  Résolution, textures, buffers, protections mémoire et horloge FPS. 'Copier mon profil' donne un code (VI1-...) à partager ;",
                "      colle celui d'un ami dans la case et appuie sur 'Appliquer' (il montre d'abord ce qui change).",
                "",
                "LOGS ET ERREURS",
                "   •  Logs du jeu : " + Path.Combine(dataRoot, "logs") + " (session-*.log, prepare.log).",
                "   •  Erreurs du launcher : " + Program.ErrorFile,
                "   •  'Envoyer les erreurs sur Discord' : choisis 'erreurs du launcher' ou 'erreurs du jeu' et écris ce qui s'est passé. Le launcher met",
                "      les bons fichiers dans un zip sur ton Bureau, ouvre le dossier et te demande de l'envoyer dans le salon \"" + DiscordChannel + "\".",
                "      Rien n'est envoyé tout seul : c'est toi qui l'envoies.",
                "",
                "CARTES GRAPHIQUES",
                "   •  NVIDIA RTX 40/50 : Smooth Motion. RTX 30 : NVSmooth30 (expérimental). Autres cartes (GTX 10, RTX 20, AMD, Intel) : génération d'images VI",
                "      (expérimental, désactivée par défaut) : une image entre chaque paire, faite à partir des images seules, avec une demi-image de retard.",
                "   •  AMD : le launcher détecte la carte et utilise le moteur AMD de Belobug (VIctory-Belobug-AMD).",
                "",
                "TOUCHES EN JEU",
                "   •  F3 affiche/masque l'overlay : FPS, moyenne de la dernière minute (AVG 1M), moyenne depuis le début (ALL), pire demi-seconde (LOW).",
                "   •  Alt ou F1 libère la souris. La page Commandes a le reste.",
            } : new[] {
                "PLAY",
                "   •  Presets: Recommended (720p), Fastest and Modest PC (540p), Ultra Low (PCs with very little memory), Sharp and",
                "      Classic cgbj (1080p), Custom (your values). The launcher picks one from your hardware the first time.",
                "   •  Launch options: point at each to see what it does. ★ marks what was tested.",
                "   •  PLAY loads the pipelines, starts the game and, when it closes, keeps this session's pipelines for the next one.",
                "   •  A stuck game (an error, a window that stopped answering) never blocks PLAY: it turns into 'CLOSE AND PLAY'.",
                "   •  When something is missing (virtual memory, power, Resizable BAR, shaders), a yellow notice says what and takes you to fix it.",
                "",
                "SETUP",
                "   •  'Do everything for me' runs the missing steps: engine, game folder, shaders, power, virtual",
                "      memory, the right graphics card for VIctory and frame generation. Windows asks for administrator rights when needed.",
                "   •  Resizable BAR: the launcher checks what D3D12 gives the game (not only the BIOS) and shows your motherboard's guide.",
                "",
                "SHADERS",
                "   •  Shows, live, the translated shaders, the pipelines ready, the ones that failed, disk space and errors.",
                "   •  'Repair' makes what is missing; 'Rebuild everything' clears the cache and prepares again.",
                "",
                "PERFORMANCE",
                "   •  What holds video memory and RAM; check apps and close them before playing. Frame generation on or off.",
                "",
                "HARDWARE & ID",
                "   •  Your permanent ID (VI-XXXX-XXXX), with a copy button. It comes from PC parts and stays after a Windows reinstall.",
                "   •  Hardware sharing (off by default): sends ONLY hardware (graphics card, CPU, RAM, motherboard, Windows, disk type).",
                "      It never sends logs (those have a choice of their own), options, paths, names or your IP. The page shows exactly what is sent.",
                "",
                "UPDATES",
                "   •  The launcher checks GitHub when it opens and every 5 minutes. An update is installed only when its signature and",
                "      SHA-256 match; it installs itself while the game is not running. It is the only thing the launcher takes from the internet.",
                "",
                "ADVANCED TWEAKS (CUSTOM)",
                "   •  Resolution, textures, buffers, memory guards and FPS clock. 'Copy my profile' gives a code (VI1-...) to share;",
                "      paste a friend's in the box and press 'Apply' (it shows what changes first).",
                "",
                "LOGS AND ERRORS",
                "   •  Game logs: " + Path.Combine(dataRoot, "logs") + " (session-*.log, prepare.log).",
                "   •  Launcher errors: " + Program.ErrorFile,
                "   •  'Send errors to Discord': choose 'launcher errors' or 'game errors' and write what happened. The launcher puts the",
                "      right files in a zip on your Desktop, opens the folder and asks you to send it in the \"" + DiscordChannel + "\" channel.",
                "      Nothing is sent on its own: you send it.",
                "",
                "GRAPHICS CARDS",
                "   •  NVIDIA RTX 40/50: Smooth Motion. RTX 30: NVSmooth30 (experimental). Other cards (GTX 10, RTX 20, AMD, Intel): VI frame generation",
                "      (experimental, off by default): a frame between each two of the game's, made from the pictures alone, half a frame later.",
                "   •  Every card (NVIDIA, AMD, Intel) plays with the edupode engine; Radeon cards can pick Belobug's alternative.",
                "",
                "KEYS IN THE GAME",
                "   •  F3 shows/hides the overlay: FPS, last minute's average (AVG 1M), average since the start (ALL), worst half second (LOW).",
                "   •  Alt or F1 releases the mouse. The Controls page has the rest.",
            };
        }

        // The projects inside this build, for the Help page.
        static IEnumerable<string> Credits()
        {
            return new[] {
                "   •  VIctory (Marvisak) — the Xbox Series emulator: github.com/Marvisak/VIctory",
                "   •  cgbj (cputroll/cgbj-reupload) — the performance build this one starts from",
                "   •  AnyXboxSeries — Xbox Series runtime work",
                "   •  Leonidas — fixes and research",
                "   •  VIctory-NVIDIA (trustno1-coder) and its contributors — DLSS/FSR 3 frame generation, the F1 and F3 menus, save sharing, pads, the water and the lighting for NVIDIA cards: github.com/trustno1-coder/VIctory-NVIDIA",
                "   •  foxhound (readmet3xt-lab) — the base of the VIctory-NVIDIA code merged here: github.com/readmet3xt-lab/foxhound",
                "   •  cool_fox (codec-d), DexNom's VIctory, victory-memory-stack (skornuta) — fixes and memory work carried in with it",
                "   •  DueTonight and mikan123pro — tested save sharing (VIctory-NVIDIA discussion #520)",
                "   •  fxceless — rendering and performance fixes",
                "   •  VIctory-7900XTX (avatarmanz) — adaptations from the AMD fork",
                "   •  VIctory-Belobug-AMD (Belobug) — the alternative engine for Radeon cards: github.com/Belobug/VIctory-Belobug-AMD",
                "   •  NVSmooth30 (ItsAdeline) — Smooth Motion on RTX 30 cards (experimental): github.com/ItsAdeline/NVSmooth30",
                "   •  MFG Unlocker (dashdogy, MIT) — DLSS Frame Generation on RTX 20/30, fetched only when picked: github.com/dashdogy/MFG-Unlocker",
                "   •  Cold Project VI (felicienradio) — the project page this remake follows",
            };
        }

        // ---------------------------------------------------------------- state
        // The emulator this PC plays with: on an AMD card the AMD fork's (Hardware.cs, Engine), this
        // repository's otherwise; a build from source first, then the one an update put in place.
        string VictoryExe
        {
            get
            {
                var places = UseAmdEngine
                    ? new[] { Path.Combine(BuildTarget(AmdEngineDir), "diag", "victory.exe"), Path.Combine(AmdEngineDir, "target", "diag", "victory.exe"), Path.Combine(AmdEngineDir, "victory.exe") }
                    : OwnExeCandidates;
                foreach (var p in places)
                    if (File.Exists(p)) return p;
                return null;
            }
        }

        // Where this repository's emulator may be: built in the source folder (beside the launcher,
        // above it, or the one the launcher downloaded), or ready-made beside the launcher (a release).
        IEnumerable<string> OwnExeCandidates
        {
            get
            {
                var list = new List<string>();
                var src = SourceDir;
                if (src != null) list.Add(Path.Combine(src, "target", "diag", "victory.exe"));
                if (src != null && StorageRoot != null) list.Add(Path.Combine(BuildTarget(src), "diag", "victory.exe"));
                list.Add(Path.Combine(root, "target", "diag", "victory.exe"));
                list.Add(Path.Combine(root, "victory.exe"));
                list.Add(Path.Combine(AppDir, "victory.exe"));
                // The newest first: a fresh build or a fresh update, whichever came last.
                var ordered = list.Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(p => File.Exists(p) ? File.GetLastWriteTimeUtc(p) : DateTime.MinValue).ToList();
                // A launcher that does not run from inside a source folder (installed, or on the Desktop) plays the
                // emulator its updates bring, whatever older builds lie in a source folder it found: on 0.3.23 an
                // RTX 4060 kept stopping where that release's engine goes on, with its launcher updated (a source
                // folder under %LOCALAPPDATA% and another victory.exe beside the game, 28/11/2026).
                var updated = Path.Combine(AppDir, "victory.exe");
                bool fromSource = src != null && (root + "\\").StartsWith(src.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase);
                if (!fromSource && File.Exists(updated))
                {
                    ordered.RemoveAll(p => string.Equals(p, updated, StringComparison.OrdinalIgnoreCase));
                    ordered.Insert(0, updated);
                }
                return ordered;
            }
        }

        // Where updates put the emulator and tools: beside the launcher when it is installed (the
        // installer's folder holds victory.exe), else %LOCALAPPDATA%\VIctory\app, so a launcher on the
        // Desktop or in Downloads never fills that folder.
        string AppDir
        {
            get
            {
                if (File.Exists(Path.Combine(root, "victory.exe")) || File.Exists(Path.Combine(root, "unins000.exe"))) return root;
                return Path.Combine(Path.GetDirectoryName(dataRoot), "app");
            }
        }

        // Which emulator the game starts with, for logs and reports: its path (user name taken out), size and date.
        string ExeLine()
        {
            try
            {
                var exe = VictoryExe;
                if (exe == null) return "victory.exe: none";
                var f = new FileInfo(exe);
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var shown = exe.StartsWith(home, StringComparison.OrdinalIgnoreCase) ? "%USERPROFILE%" + exe.Substring(home.Length) : exe;
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, "victory.exe: {0} ({1:N0} bytes, {2:yyyy-MM-dd HH:mm} UTC)", shown, f.Length, f.LastWriteTimeUtc);
            }
            catch (Exception e) { return "victory.exe: " + e.Message; }
        }

        // This repository's own build (the `victory gpu` probe and the tools), whatever the engine.
        string OwnVictoryExe
        {
            get
            {
                foreach (var p in OwnExeCandidates)
                    if (File.Exists(p)) return p;
                return null;
            }
        }

        void Refresh2()
        {
            // The list of executables starts hidden: placed beside the status line again (a hidden control is not moved when the page scrolls).
            if (exeBox != null && gameStatus != null && !exeBox.Visible) exeBox.Top = gameStatus.Top - 2;
            RefreshExes();
            bool game = gameBox.Text.Trim().Length > 0 && File.Exists(GameExe);
            int count = exeBox != null && exeBox.Visible ? exeBox.Items.Count : 0;
            gameStatus.Text = !game ? L("Choose the folder of your dump.", "Escolhe a pasta do teu dump.")
                : exeProblem != null ? "✗  " + exeProblem
                : count > 1 ? string.Format(L("✓  Game found: {0} executables, choose one on the right.", "✓  Jogo encontrado: {0} executaveis, escolhe um a direita."), count)
                : L("✓  Game found", "✓  Jogo encontrado", "✓  Jeu trouvé") + "  ·  " + Path.GetFileName(GameExe);
            gameStatus.ForeColor = !game ? Theme.Dim : exeProblem != null ? Theme.Warn : Theme.Good;
            gameHeading.Note = game ? Path.GetFileName(GameExe) : "";
            gameHeading.Invalidate();
            var exe = VictoryExe;
            if (buildStatus != null)
            {
                if (exe != null && EngineOutdated)
            {
                buildStatus.Text = "\u26a0  " + L("Rebuild: ", "Recompilar: ", "Recompiler : ") + OutdatedEngineLine();
                buildStatus.ForeColor = Theme.Warn;
                var stale = steps != null ? steps.FirstOrDefault(st => st.Fix == (Func<bool>)BuildBlocking) : null;
                if (stale != null)
                {
                    var title = L("Rebuild VIctory (the source is newer than the engine)", "Recompilar o VIctory (o codigo e mais recente que o motor)", "Recompiler VIctory (le code est plus recent que le moteur)");
                    if (stale.Title != title) { stale.Title = title; stale.FixText = L("Rebuild", "Recompilar", "Recompiler"); }
                }
            }
            else buildStatus.Text = exe != null ? "✓  " + exe : UseAmdEngine
                    ? L("The AMD engine is not built yet: Setup > 'AMD engine (Belobug)'.", "O motor AMD ainda nao foi compilado: Preparar > 'Motor AMD (Belobug)'.")
                    : L("Not built yet: press 'Build VIctory' (needs Rust and Visual Studio Build Tools).", "Ainda nao compilado: carrega em 'Compilar VIctory' (precisa do Rust e do Visual Studio Build Tools).");
                buildStatus.ForeColor = exe != null ? Theme.Good : Theme.Warn;
            }
            modeInfo.Text = modeIndex < 0 ? "" : Selected.Description;
            presetHeading.Note = PresetSummary();
            presetHeading.Invalidate();
            var state = State();
            // A game running (or one just started): the animation clock stops altogether.
            Anim.GameRunning = state != GameState.None || gameProc != null;
            Anim.Wake();
            // A stuck session never keeps PLAY off: PLAY closes it first.
            bool ready = game && exe != null && state != GameState.Running && !busyNow && !setupBusy;
            playButton.Enabled = ready;
            if (prepareButton != null) { prepareButton.Enabled = ready && state == GameState.None; prepareButton.Invalidate(); }
            // When PLAY is off, the button says why (it said only "Play", grey, and players thought it broken).
            playButton.Text = state == GameState.Running ? L("Running", "A correr", "En cours") : busyNow ? L("Wait...", "Espera...", "Attends...")
                : state == GameState.Stuck ? L("Close and play  →", "Fechar e jogar  →", "Fermer et jouer  →")
                : setupBusy ? L("Setup is working...", "O Preparar esta a trabalhar...", "La préparation travaille...")
                : !game ? L("Choose the game first", "Escolhe o jogo primeiro", "Choisis le jeu d'abord")
                : exe == null ? L("Build VIctory first", "Compila o VIctory primeiro", "Compile VIctory d'abord")
                : L("Play  →", "Jogar  →", "Jouer  →");
            closeButton.Visible = state != GameState.None;
            closeButton.Text = state == GameState.Stuck ? L("Close stuck game", "Fechar jogo preso") : L("Close game", "Fechar o jogo");
            if (tips == null) tips = new ToolTip();
            string why = ready ? "" : !game ? L("Choose the game folder first (Auto-detect).", "Escolhe primeiro a pasta do jogo (Detetar).")
                : exe == null ? L("Build VIctory first (Setup).", "Compila o VIctory primeiro (Preparar).")
                : state == GameState.Running ? L("The game is already running ('Close game' ends it).", "O jogo ja esta a correr ('Fechar o jogo' termina-o).")
                : L("Wait for the current task to finish.", "Espera que a tarefa atual acabe.");
            tips.SetToolTip(playButton, state == GameState.Stuck ? L("A VIctory session stopped answering: PLAY closes it and starts again.", "Uma sessao do VIctory deixou de responder: JOGAR fecha-a e arranca de novo.") : why);
            playButton.Invalidate();
            closeButton.Invalidate();

            // The strip of checks, the warnings and the footer.
            var missing = Missing();
            statusStrip.Items = new[] {
                game && exeProblem == null ? L("Dump verified", "Dump verificado") : L("No game dump", "Sem dump do jogo"),
                exe != null ? L("Binary ready", "Binario pronto") : L("Binary missing", "Falta o binario"),
                shaderFiles > 0 ? string.Format(L("{0:N0} shaders cached", "{0:N0} shaders em cache"), shaderFiles) : L("Shaders not prepared", "Shaders por preparar"),
                missing.Count == 0 ? L("Windows ready", "Windows pronto") : string.Format(L("{0} to fix", "{0} por corrigir"), missing.Count),
            };
            statusStrip.States = new[] { game && exeProblem == null ? 1 : 0, exe != null ? 1 : 0, shaderFiles >= 1000 ? 1 : 0, missing.Count == 0 ? 1 : 0 };
            statusStrip.Invalidate();
            if (missing.Count > 0)
            {
                warnText.Text = "⚠  " + L("Running without: ", "A jogar sem: ") + string.Join(" · ", missing.ToArray());
                warnText.ForeColor = Theme.Warn;
                warnFix.Visible = true;
            }
            else
            {
                warnText.Text = "✓  " + L("Windows settings, Resizable BAR and shaders are all set.", "Definicoes do Windows, Resizable BAR e shaders: tudo pronto.");
                warnText.ForeColor = Theme.Dim;
                warnFix.Visible = false;
            }
            warnPanel.Invalidate();
            // The foot: state, preset, engine and hardware as tiny labels; a PLAY that cannot be pressed says why.
            Color stateColor = state == GameState.Running ? Theme.Cyan : state == GameState.Stuck ? Theme.Warn : busyNow ? Theme.Cyan : ready ? Theme.Good : Theme.Warn;
            string stateText = state == GameState.Running ? L("Running", "A correr", "En cours") : state == GameState.Stuck ? L("Stuck", "Preso", "Bloqué") : busyNow ? L("Working", "A trabalhar", "En cours de travail") : ready ? L("Ready", "Pronto", "Prêt") : L("Not ready", "Nao esta pronto", "Pas prêt");
            footMeta.Set(stateText, stateColor, string.Format("{0} ({1})", Selected.Name, Selected.Custom ? CustomTag() : Selected.Tag), EngineName,
                string.Format("{0:0} GB VRAM  ·  {1:0} GB RAM", vramGb, ramGb), !ready && why.Length > 0 && state != GameState.Running ? why : "");
            // The poster: the headline, the sentence, the steps on the rings, the second button.
            bool stepsOk = statusStrip.States.All(v => v == 1);
            banner.Steps = statusStrip.Items; banner.States = statusStrip.States;
            banner.Title = state == GameState.Running ? L("Playing", "A jogar", "En jeu") : stepsOk ? L("Ready", "Pronto", "Prêt") : L("Almost", "Quase", "Presque");
            banner.Accent = state == GameState.Running ? L("right now.", "agora mesmo.", "en ce moment.") : stepsOk ? L("when you are.", "quando quiseres.", "quand tu veux.") : L("there.", "la.", "là.");
            banner.Status = state == GameState.Running ? L("The game is running. Close it from the bar below when you are done.", "O jogo esta a correr. Fecha-o na barra em baixo quando acabares.", "Le jeu tourne. Ferme-le depuis la barre en bas quand tu as fini.")
                : busyNow || setupBusy ? L("A task is running. Play unlocks when it finishes.", "Uma tarefa esta a correr. O Jogar liberta-se quando acabar.", "Une tâche est en cours. Jouer se débloque quand elle finit.")
                : ready && missing.Count == 0 ? L("Game, engine, shaders and Windows are all set. Pick a preset below, or just press play.", "Jogo, motor, shaders e Windows estao prontos. Escolhe uma predefinicao em baixo, ou carrega em jogar.", "Jeu, moteur, shaders et Windows sont prêts. Choisis un préréglage plus bas, ou lance simplement.")
                : ready ? L("Ready to launch, but running without: ", "Pronto a arrancar, mas a jogar sem: ", "Prêt à lancer, mais sans : ") + string.Join(" · ", missing.ToArray())
                : why;
            heroGhost.Text = !stepsOk || missing.Count > 0 ? L("Fix it", "Corrigir", "Corriger") : L("Setup", "Preparar", "Préparer");
            heroGhost.Invalidate();
            banner.Invalidate();
        }

        ComboBox outputBox;

        // The output size picked: 1080p, 1440p, 2160p or 720p (launcher.ini keeps the box's index: 3 is 720p).
        int OutputHeight { get { return outputBox == null ? 1080 : outputBox.SelectedIndex == 3 ? 720 : outputBox.SelectedIndex == 2 ? 2160 : outputBox.SelectedIndex == 1 ? 1440 : 1080; } }
        int OutputWidth { get { return OutputHeight * 16 / 9; } }

        // The size the console reports to the game. Never 720p: the game stops on a 1280x720 display mode with its
        // own fatal error (title+0x2219650; every one of 11 sessions at 720p in 0.3.6, players' logs 27/11/2026),
        // and Belobug's engine crashed with a 1280x720 output. "720p" keeps a 1080p screen and draws the world at
        // 720p at most (EffectiveScale).
        int DisplayHeight { get { return OutputHeight == 720 ? 1080 : OutputHeight; } }
        int DisplayWidth { get { return DisplayHeight * 16 / 9; } }

        // At "720p" the world is drawn at the preset's size or 720p, whichever is smaller (the preset's scale and
        // 2/3 of the 1080p screen): Recommended, Sharp and Classic at 1280x720, the 540p presets as they are. null: native.
        string EffectiveScale(string scale)
        {
            if (OutputHeight != 720) return scale;
            double s = 1.0;
            if (scale != null) double.TryParse(scale, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out s);
            if (s <= 0) s = 1.0;
            return Math.Min(s, 2.0 / 3.0).ToString("0.#######", System.Globalization.CultureInfo.InvariantCulture);
        }

        // The edupode engine is the default on every card. On a Radeon, when it ends with an error in the first
        // three minutes twice in a row, the launcher moves to Belobug's AMD engine on its own (built once by
        // Setup if it is not there yet) and says how to go back. One early failure is not enough: a game can
        // end at start once and then run (an RX 6800 player's first session, 27/11/2026).
        void EngineFallback(bool failedEarly)
        {
            if (!failedEarly) { if (ReadSetting("edupode_fails") != "0") WriteSetting("edupode_fails", "0"); return; }
            if (UseAmdEngine || !hasRadeon || hasNvidia) return;
            // Belobug's repository is gone from GitHub (removed 27/11/2026, its address answers 404): the AMD engine can
            // no longer be downloaded, so the launcher moves to it only where an earlier Setup left its source.
            if (!File.Exists(Path.Combine(AmdEngineDir, "Cargo.toml")))
            {
                Log(L("The edupode engine ended at start on this Radeon. Belobug's AMD engine can no longer be downloaded (its GitHub page was removed): staying with edupode. A report ('Send errors') helps us make it start on your card.",
                      "O motor edupode fechou ao arrancar nesta Radeon. O motor AMD do Belobug ja nao pode ser descarregado (a pagina do GitHub foi removida): fica o edupode. Um relatorio ('Enviar erros') ajuda-nos a po-lo a arrancar na tua placa.",
                      "Le moteur edupode s'est arrêté au lancement sur cette Radeon. Le moteur AMD de Belobug ne peut plus être téléchargé (sa page GitHub a été retirée) : edupode reste. Un rapport ('Envoyer les erreurs') nous aide à le faire démarrer sur ta carte."));
                return;
            }
            int fails;
            int.TryParse(ReadSetting("edupode_fails") ?? "0", out fails);
            fails++;
            WriteSetting("edupode_fails", fails.ToString());
            if (fails < 2)
            {
                Log(L("The edupode engine ended at start on this Radeon (1 of 2): one more and the launcher moves to Belobug's AMD engine.",
                      "O motor edupode fechou ao arrancar nesta Radeon (1 de 2): mais uma e o launcher passa para o motor AMD do Belobug."));
                return;
            }
            WriteSetting("edupode_fails", "0");
            WriteSetting("engine", "amd");
            WriteSetting("engine_auto", "1");
            try
            {
                BeginInvoke(new Action(() => {
                    if (engineBox != null) engineBox.SelectedIndex = 1;
                    UpdateBanner(); Refresh2();
                    Log(L("The edupode engine ended at start twice on this Radeon: switched to Belobug's AMD engine.",
                          "O motor edupode fechou ao arrancar duas vezes nesta Radeon: mudei para o motor AMD do Belobug."));
                    MessageBox.Show(this, L("The edupode engine did not start twice in a row on your Radeon, so the launcher switched to Belobug's AMD engine (VIctory-Belobug-AMD, all credit to Belobug). If it is not built yet, Setup builds it now; then press PLAY.\r\n\r\nTo try the edupode engine again: Hardware & ID > Engine. A report ('Send errors') helps us make it start on your card.",
                                            "O motor edupode nao arrancou duas vezes seguidas na tua Radeon, por isso o launcher mudou para o motor AMD do Belobug (VIctory-Belobug-AMD, todo o credito para o Belobug). Se ainda nao estiver compilado, o Preparar compila-o agora; depois carrega em JOGAR.\r\n\r\nPara experimentar o motor edupode outra vez: Hardware e ID > Motor. Um relatorio ('Enviar erros') ajuda-nos a pô-lo a arrancar na tua placa."),
                                    "Julia's Launcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    if (VictoryExe == null) { Show(PageSetup); RunSteps(steps.Where(st => st.Fix == (Func<bool>)BuildAmdEngine).ToArray()); }
                }));
            }
            catch (Exception) { }
        }

        // "1280×720 internal upscaled to 1080p · 60 FPS clock" for the preset's heading.
        string PresetSummary()
        {
            if (modeIndex < 0) return "";
            var m = Selected;
            string scale = EffectiveScale(m.Custom ? custom.Scale : m.Scale);
            string pic;
            double sc;
            if (scale != null && double.TryParse(scale, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out sc) && sc < 0.999)
                pic = string.Format(L("{0}×{1} internal upscaled to {2}p", "{0}×{1} interno ampliado para {2}p"), (int)Math.Round(DisplayWidth * sc), (int)Math.Round(DisplayHeight * sc), DisplayHeight);
            else pic = string.Format(L("native {0}p", "{0}p nativo"), DisplayHeight);
            bool sixty = m.Custom ? custom.Fps == 60 : fps60Box.Checked && !m.UltraLow;
            return pic + " · " + (sixty ? L("60 FPS clock", "relogio de 60 FPS") : L("30 FPS clock", "relogio de 30 FPS"));
        }

        // The Play page's engine switch, kept in step with the Hardware page's (and the automatic switch).
        Action showEnginePills;

        void UpdateBanner()
        {
            if (showEnginePills != null) showEnginePills();
            if (banner == null) return;
            // The hardware along the poster's bottom rule: a label over its value (a warning keeps its colour).
            var labels = new List<string>();
            var values = new List<string>();
            var colors = new List<Color>();
            Action<string, string, Color> fact = (l, v, c) => { labels.Add(l); values.Add(v); colors.Add(c); };
            fact(L("Graphics card", "Placa grafica", "Carte graphique"), gpuName, Theme.Muted);
            fact(L("Video memory", "Memoria de video", "Mémoire vidéo"), string.Format("{0:0} GB", vramGb), vramGb >= 10 ? Theme.Muted : Theme.Warn);
            fact("RAM", string.Format("{0:0} GB", ramGb), ramGb >= 24 ? Theme.Muted : Theme.Warn);
            var rb = RebarText();
            fact("Resizable BAR", rb.StartsWith("ReBAR ") ? rb.Substring(6) : rb, rebar ? Theme.Muted : Theme.Warn);
            var fgText = FrameGenChip();
            foreach (var pre in new[] { "Frame gen ", "Geracao de frames " }) if (fgText.StartsWith(pre)) fgText = fgText.Substring(pre.Length);
            fact(L("Frame generation", "Geracao de frames", "Génération d'images"), fgText, Theme.Muted);
            fact(L("Engine", "Motor", "Moteur"), UseAmdEngine ? "AMD · Belobug" : L("Standard", "Padrao", "Standard"), UseAmdEngine ? Theme.Cyan : Theme.Muted);
            var fg = smoothBox as OptionCard;
            if (fg != null) { fg.Text = FgTitle(); fg.Line = FrameGenLine(); fg.Enabled = FrameGenAvailable; fg.Invalidate(); }
            banner.FactLabels = labels.ToArray();
            banner.FactValues = values.ToArray();
            banner.FactColors = colors.ToArray();
            banner.Invalidate();
        }

        // Resizable BAR in one chip: what a run gets (D3D12) and, when it differs, what the card says.
        string RebarText()
        {
            if (rebarHeap == 1) return L("ReBAR On", "ReBAR ligado");
            if (rebarHeap == 0 && rebarBios) return L("ReBAR On (not used by Windows)", "ReBAR ligado (o Windows nao o usa)");
            if (rebarBios) return L("ReBAR On", "ReBAR ligado");
            if (!integratedOnly && RebarCardSupport() == 0) return L("ReBAR n/a (card)", "ReBAR n/d (placa)");
            if (RebarMissing()) return L("ReBAR Off", "ReBAR desligado");
            return rebarHeap == 0 ? L("ReBAR not offered by Windows", "ReBAR nao oferecido pelo Windows") : L("ReBAR ?", "ReBAR ?");
        }

        static string NvidiaSmi
        {
            get
            {
                var mine = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "nvidia-smi.exe");
                return File.Exists(mine) ? mine : "nvidia-smi";
            }
        }

        void DetectSystem()
        {
            try
            {
                var smi = RunCapture(NvidiaSmi, "--query-gpu=name,memory.total,driver_version --format=csv,noheader,nounits");
                if (!string.IsNullOrEmpty(smi) && smi.Contains(","))
                {
                    // The card with the most memory (a laptop may list more than one).
                    string best = null; double bestMb = -1;
                    foreach (var line in smi.Split('\n'))
                    {
                        var parts = line.Split(',');
                        double mb;
                        if (parts.Length >= 2 && double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out mb) && mb > bestMb)
                        { bestMb = mb; best = line; }
                    }
                    if (best != null)
                    {
                        var parts = best.Split(',');
                        gpuName = parts[0].Trim().Replace("NVIDIA GeForce ", "");
                        vramGb = bestMb / 1024.0;
                        if (parts.Length > 2) driverVersion = parts[2].Trim();
                    }
                    hasNvidia = true;
                    var q = RunCapture(NvidiaSmi, "-q -d MEMORY");
                    // BAR1 of 1 GB or more: the card and the PC's firmware have Resizable BAR on.
                    foreach (Match m in Regex.Matches(q, @"BAR1 Memory Usage\s+Total\s*:\s*(\d+)\s*MiB", RegexOptions.IgnoreCase))
                    {
                        double bar;
                        if (!double.TryParse(m.Groups[1].Value, out bar)) continue;
                        barRead = true;
                        if (bar >= 1024) rebarBios = true;
                    }
                    canSmooth = Regex.IsMatch(gpuName, @"RTX\s*(40|50)\d\d");
            rtxFrameGen = canSmooth && (FgChoice == "auto" || FgChoice == "smooth");
                }
                var mem = new Microsoft.VisualBasic.Devices.ComputerInfo();
                ramGb = mem.TotalPhysicalMemory / 1073741824.0;
            }
            catch (Exception) { }
            DetectCards();
            FillFgBox();
            FillFsrBox();
            rebar = rebarBios;
            if (!FrameGenAvailable) smoothBox.Checked = false;
            // Integrated graphics, once: the lightest options whatever an earlier launcher saved (it had read the
            // memory as unknown and turned on Crowds and fights and frame generation).
            if (integratedOnly && ReadSetting("igpu_defaults") != "1")
            {
                fightsBox.Checked = false; smoothBox.Checked = false; fps60Box.Checked = false;
                SelectMode(ModeUltraLow);
                WriteSetting("igpu_defaults", "1");
            }
            // VIctory's own frame generation is experimental: off until the player turns it on.
            else if (OwnFrameGen && ReadSetting("smooth") == null) smoothBox.Checked = false;
            if (modeIndex < 0)
                SelectMode(integratedOnly || (ramGb > 0 && ramGb <= 16.5) || (vramGb > 0 && vramGb <= 6.5) ? ModeUltraLow
                    : (vramGb > 0 && vramGb < 10) || (ramGb > 0 && ramGb < 24) ? 2 : 0);
            // Automatic settings: picked again for this PC every time the launcher opens.
            if (AutoOn) AutoBest(false);
            UpdateBanner();
            UpdateSmooth();
        }

        // The program's output; "" if it cannot start or has not ended in 5 seconds (then it is killed).
        static string RunCapture(string file, string args)
        {
            try
            {
                using (var p = Process.Start(new ProcessStartInfo(file, args) { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true }))
                {
                    var sb = new StringBuilder();
                    p.OutputDataReceived += (s, e) => { if (e.Data != null) lock (sb) sb.Append(e.Data).Append("\r\n"); };
                    p.BeginOutputReadLine();
                    if (!p.WaitForExit(5000))
                    {
                        try { p.Kill(); } catch (Exception) { }
                        return "";
                    }
                    p.WaitForExit(2000); // lets the last output lines arrive
                    lock (sb) return sb.ToString();
                }
            }
            catch (Exception) { return ""; }
        }

        bool busyNow;

        // Set when the window is closing (the form's FormClosing / FormClosed): worker threads read them.
        volatile bool closing, closePrompt;

        // True once the window is closing or gone: no tool is started then. Called from worker threads.
        bool ClosingNow()
        {
            if (closePrompt && !closing && InvokeRequired)
                for (int i = 0; i < 60 && closePrompt && !closing && !IsDisposed; i++) Thread.Sleep(50); // the player is being asked: wait for the answer
            return closing || IsDisposed || Disposing;
        }

        // Posts to the window from a worker thread; nothing happens (and nothing throws) when the window is gone.
        void Post(Action a)
        {
            try { if (!IsDisposed && !Disposing && IsHandleCreated) BeginInvoke(a); } catch (Exception) { }
        }

        void Log(string line)
        {
            try
            {
                if (IsDisposed) return;
                if (InvokeRequired) { BeginInvoke(new Action<string>(Log), line); return; }
                logBox.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + line + "\r\n");
            }
            catch (Exception) { }
        }

        // launcher.ini is read and rewritten from the window and from worker threads: one at a time.
        static readonly object settingsLock = new object();

        // Writes through a temporary file, so a crash in the middle cannot leave launcher.ini empty.
        static void WriteLinesAtomic(string path, string[] lines)
        {
            var tmp = path + ".tmp";
            try
            {
                File.WriteAllLines(tmp, lines);
                if (File.Exists(path)) File.Replace(tmp, path, null); else File.Move(tmp, path);
            }
            catch (Exception) { try { File.Delete(tmp); } catch (Exception) { } throw; }
        }

        // ---------------------------------------------------------------- environment
        Dictionary<string, string> Environment2()
        {
            // The AMD fork reads its own switches (Hardware.cs).
            if (UseAmdEngine) return AmdEnvironment();
            var e = new Dictionary<string, string>();
            Action<string, string> set = (k, v) => e[k] = v;
            set("VICTORY_GAME_DIR", gameBox.Text.Trim());
            set("VICTORY_CACHE_DIR", cacheDir);
            set("VICTORY_DATA_DIR", dataDir);
            set("VICTORY_LOG", "warn,victory_gpu::host=info,victory_gpu::copystat=info,victory_gpu::framegen=info");
            // The cgbj base (Play-VIctory.cmd pareto-residency) for a 12 GB card.
            set("VICTORY_DISPLAY_MODE", DisplayWidth + "x" + DisplayHeight);
            set("VICTORY_CARD_BUFFERS_MB", "1024");
            set("VICTORY_PIPELINE_BOOK", "1");
            set("VICTORY_RIPE_BLOCKS", "4");
            set("VICTORY_PRESSURE_FREE_MB", "1024");
            set("VICTORY_PRESSURE_CLEAR_MB", "2048");
            set("VICTORY_TEXTURE_RAM_GUARD", "0");
            set("VICTORY_CARD_ROOM_FOR_TARGETS_MB", "1792");
            set("VICTORY_AMBIENT", "1.6");
            set("VICTORY_TEXTURE_NO_WAIT", "1");
            set("VICTORY_RELAY", "2");
            set("VICTORY_WIDE_BACK", "1");
            set("VICTORY_CARD_REHASH_RUNS", "1");
            set("VICTORY_JOIN_COPIES", "1");
            set("VICTORY_INDIRECT_WINDOW", "1");
            set("VICTORY_RELAY_COPY", "1");
            set("VICTORY_CARD_STEPS", "2");
            set("VICTORY_INDIRECT_BATCH", "2");
            set("VICTORY_MAP_THREADS", "1");
            set("VICTORY_CARD_AWAKE", "1");
            set("VICTORY_SHOW_AFTER", "1");
            set("VICTORY_THIN", "1");
            set("VICTORY_JOIN_PARTS", "8");
            set("VICTORY_UPLOAD_AHEAD", "1");
            set("VICTORY_LEVELS_ON_DEMAND", "1");
            set("VICTORY_CARD_BUFFERS_MIN_KB", "64");
            // The NVIDIA fork's lighting, and the one wave64 water program that translates right.
            set("VICTORY_BVH_MISS", "1");
            set("VICTORY_AMBIENT_AIR", "1.5");
            set("VICTORY_AMBIENT_MOST", "2");
            set("VICTORY_NAN_SCRUB", "1");
            // The sun: an infinite pixel colour held at the largest half instead of 0 (it showed as a black disc).
            // Only pixel colours: held everywhere (0.3.12) the picture went cyan. White sun, picture normal: edupode, 27/11/2026.
            set("VICTORY_INF_AS", "65504");
            set("VICTORY_DYNAMIC_SLOTS", "524288");
            set("VICTORY_WAVE_WIDE_DIGESTS", "7099a57fded142613b44f410463d6be6");
            var skip = Path.Combine(SourceDir ?? root, "scripts", "rt_off_hashes.txt");
            if (File.Exists(skip)) set("VICTORY_SKIP_PIPELINES", "@" + skip);

            var m = Selected;
            if (m.Scale != null)
            {
                set("VICTORY_RENDER_SCALE", m.Scale);
                set("VICTORY_OWN_UPSCALER", "1");
            }
            if (m.LightTextures)
            {
                set("VICTORY_TEXTURE_MAX", "2048");
                set("VICTORY_LEVELS_ON_DEMAND", "0");
                set("VICTORY_CARD_BUFFERS_MB", "1536");
                set("VICTORY_POOL_FULLEST", "1");
                set("VICTORY_TEXTURE_STAGE_MB", "384");
                if (rebar) set("VICTORY_WINDOWS_BAR_MB", "1024");
            }
            if (m.Modest || integratedOnly || (vramGb > 0 && vramGb < 10))
            {
                // 6-8 GB cards: less of the game's buffers on the card, smaller textures and batches.
                set("VICTORY_CARD_BUFFERS_MB", "768");
                set("VICTORY_TEXTURE_MAX", "1024");
                set("VICTORY_TEXTURE_STAGE_MB", "128");
                set("VICTORY_RIPE_BLOCKS", "2");
                set("VICTORY_CARD_ROOM_FOR_TARGETS_MB", "1024");
                e.Remove("VICTORY_WINDOWS_BAR_MB");
            }
            if (m.Modest || (ramGb > 0 && ramGb < 24))
            {
                // 16 GB of RAM: the memory guards as cgbj has them (texture prefetch held back early).
                e.Remove("VICTORY_PRESSURE_FREE_MB");
                e.Remove("VICTORY_PRESSURE_CLEAR_MB");
                e.Remove("VICTORY_TEXTURE_RAM_GUARD");
            }
            if (m.UltraLow)
            {
                // The least memory: 4-6 GB cards and 8-16 GB of RAM.
                set("VICTORY_CARD_BUFFERS_MB", "512");
                set("VICTORY_TEXTURE_MAX", "512");
                set("VICTORY_TEXTURE_STAGE_MB", "64");
                set("VICTORY_RIPE_BLOCKS", "2");
                set("VICTORY_CARD_ROOM_FOR_TARGETS_MB", "768");
                set("VICTORY_CARD_BUFFERS_MIN_KB", "128");
                set("VICTORY_LEVELS_ON_DEMAND", "1");
                e.Remove("VICTORY_POOL_FULLEST");
                e.Remove("VICTORY_WINDOWS_BAR_MB");
            }
            // 2 GB of the game's buffers only on cards of 11 GB and more: on 8 GB cards it overfilled video
            // memory (6.9 of 7.0 GB, 7 GB spilled to RAM) and the game ran at 3 FPS (players, 27/11/2026).
            if (fightsBox.Checked && !m.Modest && !m.UltraLow && !integratedOnly && vramGb >= 11) set("VICTORY_CARD_BUFFERS_MB", "2048");
            if (panelBox.Checked) set("VICTORY_PANEL", "1");
            if (borderlessBox.Checked) { set("VICTORY_BORDERLESS", "1"); set("VICTORY_BORDERLESS_DPI", "0"); }
            if (fps60Box.Checked && !m.UltraLow) set("VICTORY_FPS_MAX", "60");
            // VIctory's own frame generation, for cards without Smooth Motion or NVSmooth30.
            if (smoothBox.Checked && OwnFrameGen) set("VICTORY_VI_FRAMEGEN", "1");
            // Advanced Tweaks: the player's values over the preset they start from (Custom.cs).
            if (m.Custom) ApplyCustom(e);
            if (OutputHeight == 720)
            {
                string given;
                var s = EffectiveScale(e.TryGetValue("VICTORY_RENDER_SCALE", out given) ? given : null);
                if (s == null) { e.Remove("VICTORY_RENDER_SCALE"); e.Remove("VICTORY_OWN_UPSCALER"); }
                else { set("VICTORY_RENDER_SCALE", s); set("VICTORY_OWN_UPSCALER", "1"); }
            }
            // DLSS Frame Generation or FSR 3 (Hardware.cs): over the preset's upscaler, so after its scale is known.
            VendorFrameGen(e);
            FsrUpscaling(e);
            set("VICTORY_VRAM_RESERVE", reserveBox.Checked ? "max" : "0");
            if (cpuBox.Checked)
            {
                // Half the logical cores for the copy and refresh helpers (2-12), the write watch asked early.
                set("VICTORY_WIDE_THREADS", Math.Max(2, Math.Min(12, Environment.ProcessorCount / 2)).ToString());
                set("VICTORY_ASK_EARLY", "2");
            }
            // (Anti-stretch, VICTORY_RELAY=1 + VICTORY_CARD_STEPS=0, is off for everyone: on edupode's RTX 5070 Ti it
            // turned the whole picture cyan and dark without bringing faces back, and an RTX 4070 player ran at
            // 2-3 FPS with it, 27/11/2026. The box is hidden; a saved quality=1 is ignored.)
            if (facesBox.Checked) set("VICTORY_TEXTURE_NO_WAIT", "0");
            if (loadingBox.Checked) set("VICTORY_BAKE_NO_WAIT", "1");
            // The F3 panel's "FRAME GEN" line: the driver's frame generation runs outside the emulator.
            set("VICTORY_PANEL_FRAMEGEN", !smoothBox.Checked ? "OFF" : rtxFrameGen ? "SMOOTH MOTION" : NvSmoothHere ? "NVSMOOTH30"
                : e.ContainsKey("VICTORY_FRAMEGEN") ? "DLSS FG" : e.ContainsKey("VICTORY_FSR_FRAMEGEN") ? "AMD FG" : OwnFrameGen ? "VI 2X" : "OFF");
            // The game ends with this code to be started again by the launcher (crates\victory-gpu\src\host\menu.rs,
            // restart); the first start of a new victory.exe plays 10 seconds (as VIctory-NVIDIA has it since c1cbdc9) and asks for it (it would stay near 14 FPS).
            set("VICTORY_RESTART_EXIT", RestartExitCode.ToString());
            var mark = WarmupMark;
            if (mark != null && !File.Exists(mark)) { set("VICTORY_WARMUP_RESTART", "10"); set("VICTORY_WARMUP_MARK", mark); }
            // The F1 menu (crates\victory-gpu\src\host\menu.rs): the launcher's choices as it shows them (it does not
            // change them there: the Play page does), then the switches a player changed in it.
            if (!e.ContainsKey("VICTORY_MENU_DLSS")) set("VICTORY_MENU_DLSS", "off");
            if (!e.ContainsKey("VICTORY_MENU_FRAMEGEN")) set("VICTORY_MENU_FRAMEGEN", "0");
            if (!e.ContainsKey("VICTORY_MENU_FG_TYPE")) set("VICTORY_MENU_FG_TYPE", "auto");
            set("VICTORY_MENU_FPS", e.ContainsKey("VICTORY_FPS_MAX") ? e["VICTORY_FPS_MAX"] : "1000");
            set("VICTORY_MENU_CONSOLE", "series-s");
            if (!e.ContainsKey("VICTORY_FPS_LINE")) set("VICTORY_FPS_LINE", "1");
            if (!e.ContainsKey("VICTORY_PANEL")) set("VICTORY_PANEL", "0");
            ApplyMenuFile(e);
            ApplyTestEnv(e);
            return e;
        }

        // The switches changed in the game's F1 menu, over the preset's: every VICTORY_ line of
        // %LOCALAPPDATA%\victory\victory-settings.txt but the launcher's own choices (VICTORY_MENU_..., MFG Unlocker),
        // which the Play page makes. An empty value unsets it; "RESET ALL" in F1 deletes the file. Logged once.
        string menuFileLogged;
        // The switches the last read of the F1 menu's file set (the session log marks them "F1").
        volatile HashSet<string> menuKeys = new HashSet<string>();
        void ApplyMenuFile(Dictionary<string, string> e)
        {
            try
            {
                var file = Path.Combine(VictoryHome, "victory-settings.txt");
                if (!File.Exists(file)) return;
                var applied = new List<string>();
                var overrides = new List<string>();
                var keys = new HashSet<string>();
                foreach (var raw in File.ReadAllLines(file))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    var i = line.IndexOf('=');
                    if (i <= 0) continue;
                    var k = line.Substring(0, i).Trim();
                    var v = line.Substring(i + 1).Trim();
                    if (!k.StartsWith("VICTORY_") || k.StartsWith("VICTORY_MENU_") || k == "VICTORY_MFG_UNLOCKER"
                        || k == "VICTORY_GAME_DIR" || k == "VICTORY_DATA_DIR" || k == "VICTORY_CACHE_DIR") continue;
                    string before;
                    bool had = e.TryGetValue(k, out before);
                    // F1 wins over the launcher: said where it changes what the launcher chose.
                    if (had && before != v) overrides.Add(k + ": " + L("launcher", "launcher", "lanceur") + " " + before + " → F1 " + (v.Length == 0 ? L("(unset)", "(apagado)", "(retiré)") : v));
                    if (v.Length == 0) e.Remove(k); else e[k] = v;
                    applied.Add(k + "=" + v);
                    keys.Add(k);
                }
                menuKeys = keys;
                // Said again whenever what F1 changes is not what was said last (not only once per launcher).
                var said = string.Join(" ", applied.ToArray()) + "|" + string.Join(";", overrides.ToArray());
                if (applied.Count > 0 && said != menuFileLogged)
                {
                    menuFileLogged = said;
                    Log(L("Settings from the game's F1 menu: ", "Definicoes do menu F1 do jogo: ", "Réglages du menu F1 du jeu : ") + string.Join(" ", applied.ToArray()));
                    if (overrides.Count > 0)
                        Log(L("F1 changes over the launcher's choices (F1 wins; RESET ALL in F1 gives them back): ", "O F1 muda escolhas do launcher (o F1 ganha; RESET ALL no F1 repoe-as): ", "F1 change des choix du lanceur (F1 l'emporte ; RESET ALL dans F1 les rend) : ") + string.Join("; ", overrides.ToArray()));
                }
            }
            catch (Exception) { }
        }

        const int RestartExitCode = 75;

        // %LOCALAPPDATA%\victory\warmed\launcher-<the exe's SHA-256, 16 digits>: this victory.exe had its warm-up start.
        // (`victory play` keeps its own marks there, by another hash.) null: the exe could not be read.
        string warmupFor, warmupMark;
        string WarmupMark
        {
            get
            {
                var exe = VictoryExe;
                if (exe == null) return null;
                try
                {
                    var stamp = exe + "|" + File.GetLastWriteTimeUtc(exe).Ticks + "|" + new FileInfo(exe).Length;
                    if (stamp == warmupFor) return warmupMark;
                    string hex;
                    using (var sha = System.Security.Cryptography.SHA256.Create())
                    using (var f = File.OpenRead(exe))
                        hex = BitConverter.ToString(sha.ComputeHash(f)).Replace("-", "").ToLowerInvariant().Substring(0, 16);
                    warmupMark = Path.Combine(VictoryHome, "warmed", "launcher-" + hex);
                    warmupFor = stamp;
                    return warmupMark;
                }
                catch (Exception) { return null; }
            }
        }

        // Test switches, for trying an engine change before a release: %LOCALAPPDATA%\VIctory\test-env.txt, one
        // KEY=VALUE a line (# starts a comment), only VICTORY_* keys; they go over the preset's. Logged at PLAY,
        // and in the game log's summary, so a report says what was on. Delete the file to go back.
        void ApplyTestEnv(Dictionary<string, string> e)
        {
            try
            {
                var file = Path.Combine(Path.GetDirectoryName(dataRoot), "test-env.txt");
                if (!File.Exists(file)) return;
                foreach (var raw in File.ReadAllLines(file))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    var i = line.IndexOf('=');
                    if (i <= 0) continue;
                    var k = line.Substring(0, i).Trim();
                    if (!k.StartsWith("VICTORY_") || k == "VICTORY_GAME_DIR" || k == "VICTORY_DATA_DIR") continue;
                    e[k] = line.Substring(i + 1).Trim();
                }
            }
            catch (Exception) { }
        }

        Process Start(string args, string logFile) { return Start(args, logFile, null); }

        Process Start(string args, string logFile, Action<string> onLine)
        {
            // The window is closing (its tools were just stopped): nothing new starts. The callers catch this.
            if (ClosingNow()) throw new InvalidOperationException(L("The launcher is closing.", "O launcher esta a fechar.", "Le lanceur se ferme."));
            var psi = new ProcessStartInfo(VictoryExe, args) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, WorkingDirectory = UseAmdEngine ? AmdEngineDir : root };
            var env = Environment2();
            foreach (var kv in env) psi.EnvironmentVariables[kv.Key] = kv.Value;
            Directory.CreateDirectory(Path.GetDirectoryName(logFile));
            var writer = new StreamWriter(logFile, true, new UTF8Encoding(false)) { AutoFlush = true };
            // The settings the game really starts with, and where each came from: the launcher (its page and
            // preset) or the F1 menu over it. The page, F1 and this list then always tell the same story.
            if (args.StartsWith("run "))
            {
                var f1 = menuKeys;
                try
                {
                    writer.WriteLine("# " + ExeLine());
                    writer.WriteLine("# effective settings (" + (UseAmdEngine ? "AMD engine" : "engine " + ReadSetting("engine")) + "; [F1] = changed in the game's F1 menu, over the launcher):");
                    foreach (var kv in env.Where(kv => kv.Key.StartsWith("VICTORY_")).OrderBy(kv => kv.Key, StringComparer.Ordinal))
                        writer.WriteLine("#   " + kv.Key + "=" + kv.Value + (f1.Contains(kv.Key) ? "   [F1]" : ""));
                }
                catch (Exception) { }
            }
            var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
            DataReceivedEventHandler sink = (s, e) => {
                if (e.Data == null) return;
                lock (writer) writer.WriteLine(e.Data);
                if (onLine != null) onLine(e.Data);
            };
            p.OutputDataReceived += sink;
            p.ErrorDataReceived += sink;
            p.Exited += (s, e) => { lock (writer) writer.Dispose(); };
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            if (!args.StartsWith("run ")) lock (tools) { tools.RemoveAll(t => { try { return t.HasExited; } catch (Exception) { return true; } }); tools.Add(p); }
            return p;
        }

        void Busy(bool busy)
        {
            if (InvokeRequired) { BeginInvoke(new Action<bool>(Busy), busy); return; }
            busyNow = busy;
            playButton.Enabled = !busy; prepareButton.Enabled = !busy; buildButton.Enabled = !busy;
            playButton.Invalidate(); prepareButton.Invalidate(); buildButton.Invalidate();
            if (!busy) Refresh2();
        }

        // ---------------------------------------------------------------- actions
        void Play()
        {
            // Setup still at work (building, translating shaders): two VIctory processes on one cache. An AMD
            // player started the game twice while shaders were being prepared and it ended at start (27/11/2026).
            // Integrated graphics only: asked once, the player decides.
            if (integratedOnly && ReadSetting("igpu_ack") != "1")
            {
                var body = L("Integrated graphics (Intel Iris Xe / UHD, AMD Radeon Graphics) found, and no graphics card of its own.\r\n\r\nThe game is very likely to crash or miss graphics: integrated graphics borrow the PC's RAM, which the game already needs, and the driver refuses some of its shaders. A graphics card with 4 GB or more of its own memory is needed.\r\n\r\nThe launcher has picked Ultra Low with the heavy options off. You can still try.",
                             "Foi encontrada uma grafica integrada (Intel Iris Xe / UHD, AMD Radeon Graphics) e nenhuma placa grafica dedicada.\r\n\r\nE muito provavel que o jogo feche ou que faltem graficos: a grafica integrada usa a RAM do PC, de que o jogo ja precisa, e o driver recusa alguns shaders. E preciso uma placa grafica com 4 GB ou mais de memoria propria.\r\n\r\nO launcher escolheu o Ultra baixo com as opcoes pesadas desligadas. Podes tentar na mesma.",
                             "Graphique intégré trouvé (Intel Iris Xe / UHD, AMD Radeon Graphics), sans carte graphique dédiée.\r\n\r\nLe jeu risque fort de planter ou d'avoir des graphismes manquants : le graphique intégré emprunte la RAM du PC, dont le jeu a déjà besoin, et le pilote refuse certains shaders. Il faut une carte graphique avec 4 Go ou plus de mémoire à elle.\r\n\r\nLe launcher a choisi Ultra bas avec les options lourdes désactivées. Tu peux essayer quand même.");
                var answer = Notice(L("Integrated graphics", "Grafica integrada", "Graphique intégré"), body,
                    L("Play anyway", "Jogar na mesma", "Jouer quand même"), L("Cancel", "Cancelar", "Annuler"));
                if (answer != 1) return;
                WriteSetting("igpu_ack", "1");
            }
            if (setupBusy)
            {
                MessageBox.Show(this, L("Setup is still working (building VIctory or preparing shaders). PLAY opens when it finishes.",
                                        "O Preparar ainda esta a trabalhar (a compilar o VIctory ou a preparar shaders). O JOGAR abre quando acabar."), "Julia's Launcher");
                return;
            }
            if (!ClearOtherPreparations()) return;
            // An engine older than the source is yesterday's: it was built before the code that is here now,
            // and playing it hides every change (2026-11-28: a session on a victory.exe of the day before, with
            // VICTORY_FSR=1 in its environment and no FSR in it). Rebuild first, and say why.
            if (EngineOutdated)
            {
                Log(L("Not starting: ", "Nao arranco: ", "Je ne démarre pas : ") + OutdatedEngineLine());
                Log(L("Rebuilding the engine from this folder first; PLAY opens again when it is done.",
                      "A recompilar o motor a partir desta pasta primeiro; o JOGAR abre outra vez quando acabar.",
                      "Je recompile d'abord le moteur depuis ce dossier ; JOUER se rouvre quand c'est fini."));
                Build();
                return;
            }
            SaveSettings();
            var state = State();
            if (state == GameState.Running)
            {
                if (MessageBox.Show(this, L("VIctory is already running. Close it and start again?", "O VIctory ja esta a correr. Fechar e arrancar de novo?"),
                    "Julia's Launcher", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                CloseGames();
            }
            else if (state == GameState.Stuck)
            {
                Log(L("The last session had stopped answering: closing it.", "A ultima sessao tinha deixado de responder: a fecha-la."));
                CloseGames();
            }
            // The pipelines of the last session, still being made: the warm-up before this one makes them too.
            if (afterWarm != null && !afterWarm.HasExited)
                try { RunWait("taskkill", "/PID " + afterWarm.Id + " /T /F", false); } catch (Exception) { }
            afterWarm = null;
            // A game file that cannot be a program (a download that stopped, a placeholder): say so
            // instead of VIctory's "malformed image".
            if (exeProblem != null)
            {
                MessageBox.Show(this, exeProblem, "Julia's Launcher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // What is missing (virtual memory, power, Resizable BAR...): asked once per launcher session.
            var missing = Missing();
            if (missing.Count > 0 && !warnedThisSession)
            {
                var ask = L("The game will run worse (or close) without these:\r\n\r\n", "O jogo vai correr pior (ou fechar) sem isto:\r\n\r\n")
                    + string.Join("\r\n", missing.Select(x => "  •  " + x).ToArray())
                    + L("\r\n\r\nYes: play anyway.   No: open Setup to fix them.", "\r\n\r\nSim: jogar na mesma.   Nao: abrir o Preparar para corrigir.");
                if (MessageBox.Show(this, ask, "Julia's Launcher", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) { Show(PageSetup); return; }
                warnedThisSession = true;
            }
            // Too little memory to promise: the window would stay white (Session.cs).
            if (!EnoughMemory()) { Show(PagePerf); return; }
            // Asked on every PLAY until the player shares or says not to ask again (Hardware.cs).
            AskToShare();
            // Once: a log of each game sent when it closes (SessionLogs.cs).
            AskSessionLogs();
            // Little RAM free: what holds it, once (Session.cs). The first game on an engine: how its pipelines get built.
            if (!HeavyAppsCheck()) return;
            PipelineHint();
            // The saves as they are, before the game can change them (Saves.cs).
            BackupSavesBeforePlay();
            long cardUsed;
            GpuUsers(out cardUsed);
            if (cardUsed > (2L << 30))
                Log(string.Format(L("Other apps already hold {0:0.0} GB of the card: the Performance page frees it.", "Outras apps ja ocupam {0:0.0} GB da placa: a pagina Desempenho liberta-a."), cardUsed / 1073741824.0));
            ApplyFrameGen();
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var log = Path.Combine(dataRoot, "logs", "session-" + stamp + ".log");
            Log(L("Starting ", "A iniciar ") + Selected + ". Log: " + log);
            Busy(true);
            var game = GameExe;
            bool warmFirst = warmBox.Checked;
            bool frameGenOn = smoothBox.Checked;
            new Thread(() => {
                try
                {
                    // DLSS Frame Generation or FSR 3 picked: NVIDIA's, AMD's or MFG Unlocker's files, where missing.
                    FetchFrameGenFiles(log, frameGenOn);
                    if (warmFirst) WarmWithBar(log);
                    if (ClosingNow()) return;
                    var started = DateTime.Now;
                    Process p;
                    int code;
                    // The game may end asking to be started again (RestartExitCode): the first start of a new
                    // victory.exe, warmed up (WarmupMark), or a restart from its F1 menu. At most three times.
                    for (int round = 0; ; round++)
                    {
                        p = Start("run \"" + game + "\" --console series-s --execute", log);
                        gameProc = p; gameHadWindow = false; windowGone = null; Anim.GameRunning = true;
                        WatchStart(log, p);
                        // From here the game's own state keeps PLAY off (RUNNING), not the launcher's task,
                        // so a game that ends badly never leaves PLAY waiting.
                        Busy(false);
                        p.WaitForExit();
                        // NVSmooth30's version.dll is there for the game only (tools must not load it).
                        RemoveNvSmooth();
                        code = 0;
                        try { code = p.ExitCode; } catch (Exception) { }
                        if (code != RestartExitCode || round >= 3 || gameProc != p || ClosingNow()) break;
                        Log(L("The game asked to start again (the first start of this version, warmed up, or the F1 menu): starting it again.",
                              "O jogo pediu para arrancar de novo (o primeiro arranque desta versao, ja aquecido, ou o menu F1): a arrancar outra vez.",
                              "Le jeu a demandé à redémarrer (le premier lancement de cette version, réchauffé, ou le menu F1) : il redémarre."));
                        Busy(true);
                        try { Invoke(new Action(ApplyFrameGen)); } catch (Exception) { }
                        Thread.Sleep(1500);
                    }
                    if (code == RestartExitCode) code = 0;
                    // A warm-up start (the first of a new victory.exe) that failed at once is not asked for again:
                    // without its mark every later start was a warm-up too, with its notice and restart, and ended the
                    // same way (0.3.21: an RX 7600 nine times in a row in the first frame, 0x80070057, while the same
                    // PC played 17 minutes on 0.3.19 without one). The next start is an ordinary one.
                    if (code != 0 && (DateTime.Now - started).TotalMinutes < 2)
                    {
                        try
                        {
                            var warm = WarmupMark;
                            if (warm != null && !File.Exists(warm))
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(warm));
                                File.WriteAllText(warm, "the warm-up start of this victory.exe failed at once: not asked for again\r\n");
                                Log(L("The first start of this version failed during its warm-up: the next start skips the warm-up.",
                                      "O primeiro arranque desta versao falhou durante o aquecimento: o proximo arranque salta o aquecimento.",
                                      "Le premier lancement de cette version a échoué pendant le réchauffage : le prochain lancement le saute."));
                            }
                        }
                        catch (Exception) { }
                    }
                    SendSessionLog(log, code, started, DateTime.Now);
                    // This game's FPS for the players' averages (sent with the hardware report, when sharing is on).
                    KeepFps(log, started, DateTime.Now);
                    if (gameProc != p) return; // closed from the launcher (Close game, or PLAY again)
                    gameProc = null;
                    if (code != 0)
                        Log(string.Format(L("The game ended with an error (code {0}). Log: {1}", "O jogo terminou com um erro (codigo {0}). Log: {1}"), code, log));
                    // The game's package without its own DLLs (VIctory says which in the log): the player has to copy
                    // them, and no engine runs it, so it does not count against this one (VI-F2TB-8822, 28/11/2026: a
                    // missing amd_fidelityfx_loader_xbox.dll showed as "code 1" and moved an RX 6500 XT to Belobug's engine).
                    var absentDlls = code != 0 ? MissingDllsInLog(log) : null;
                    if (absentDlls != null)
                        Log(string.Format(L("The game's folder is missing its own files: {0}. Copy them from the complete game package next to the game's .exe ({1}), then press PLAY again.",
                            "Faltam ficheiros do proprio jogo na pasta: {0}. Copia-os do pacote completo do jogo para junto do .exe do jogo ({1}) e carrega em JOGAR outra vez.",
                            "Il manque des fichiers du jeu lui-même dans son dossier : {0}. Copie-les depuis le paquet complet du jeu à côté du .exe du jeu ({1}), puis appuie de nouveau sur JOUER."),
                            string.Join(", ", absentDlls), Path.GetDirectoryName(game)));
                    EngineFallback(absentDlls == null && code != 0 && (DateTime.Now - started).TotalMinutes < 3);
                    if (ClosingNow()) return;
                    Log(L("Game closed. Preparing this session's pipelines for the next one...", "Jogo fechado. A preparar as pipelines desta sessao para a proxima..."));
                    var w = Start("warm --used", log);
                    afterWarm = w;
                    BeginInvoke(new Action(Refresh2));
                    // Stops on its own if it runs past 10 minutes; PLAY ends it too.
                    if (!w.WaitForExit(10 * 60 * 1000))
                        try { RunWait("taskkill", "/PID " + w.Id + " /T /F", false); } catch (Exception) { }
                    if (afterWarm == w) { afterWarm = null; Log(L("Done.", "Feito.")); }
                }
                catch (Exception ex)
                {
                    Log(L("Could not start: ", "Nao arrancou: ") + ex.Message + (ex.Message.Contains("denied") ? L("  (Defender: see Help)", "  (Defender: ve a Ajuda)") : ""));
                }
                finally
                {
                    try { if (busyNow) Busy(false); } catch (Exception) { }
                    Post(Refresh2);
                }
            }) { IsBackground = true }.Start();
        }

        // Makes the pipelines earlier sessions drew with (victory warm --used) and shows how far it is.
        void WarmWithBar(string log)
        {
            int total = 0;
            BeginInvoke(new Action(() => { if (!loadBar.Visible && loadBarAnchor != null) loadBar.Top = loadBarAnchor.Top + 42; loadBar.Visible = true; loadBar.Set(0, L("Loading pipelines...", "A carregar pipelines...")); }));
            var w = Start("warm --used", log, line => {
                var m = Regex.Match(line, @"; (\d+) to make");
                if (m.Success) total = int.Parse(m.Groups[1].Value);
                var p = Regex.Match(line, @"^(\d+) of (\d+), (\d+) s");
                if (p.Success)
                {
                    int n = int.Parse(p.Groups[1].Value), of = int.Parse(p.Groups[2].Value);
                    BeginInvoke(new Action(() => loadBar.Set(of > 0 ? (float)n / of : 1f, string.Format(L("Loading pipelines: {0} of {1}", "A carregar pipelines: {0} de {1}"), n, of))));
                    Stage(1, of > 0 ? (float)n / of : 1f, string.Format(L("Pipelines: {0:N0} of {1:N0} loaded before the game · {2} s", "Pipelines: {0:N0} de {1:N0} carregadas antes do jogo · {2} s"), n, of, p.Groups[3].Value), true);
                }
                else if (m.Success)
                    BeginInvoke(new Action(() => loadBar.Set(total == 0 ? 1f : 0f, total == 0 ? L("Pipelines ready.", "Pipelines prontas.") : string.Format(L("Loading pipelines: 0 of {0}", "A carregar pipelines: 0 de {0}"), total))));
            });
            w.WaitForExit();
            Stage(1, 1f, "", false);
            Log(total == 0 ? L("Pipelines were already ready.", "As pipelines ja estavam prontas.") : string.Format(L("{0} pipelines loaded.", "{0} pipelines carregadas."), total));
            BeginInvoke(new Action(() => { loadBar.Set(1f, L("Starting the game...", "A abrir o jogo...")); }));
            Thread.Sleep(600);
            BeginInvoke(new Action(() => { loadBar.Visible = false; }));
        }

        void Prepare()
        {
            RunSteps(steps.Where(st => st.Fix == (Func<bool>)PrepareBlocking).ToArray());
        }

        // Build: Rust and the C++ tools first when they are missing (the build alone "did nothing"
        // without them), then the emulator (or the AMD engine on an AMD card).
        void Build()
        {
            Show(PageSetup);
            var chain = steps.Take(4).Where(st => { int s; try { s = st.Check(); } catch (Exception) { s = 0; } return s == 0; }).ToList();
            var build = steps.First(st => st.Fix == (Func<bool>)(UseAmdEngine ? (Func<bool>)BuildAmdEngine : BuildBlocking));
            if (!chain.Contains(build)) chain.Add(build);
            RunSteps(chain.ToArray());
        }

        // ---------------------------------------------------------------- settings
        string ReadSetting(string key)
        {
            try
            {
                lock (settingsLock)
                {
                    if (!File.Exists(settingsFile)) return null;
                    foreach (var line in File.ReadAllLines(settingsFile))
                        if (line.StartsWith(key + "=")) return Unmangle(line.Substring(key.Length + 1));
                }
            }
            catch (Exception) { }
            return null;
        }

        // A path like C:\Users\Ã©lÃ¨ve is UTF-8 read back as Windows-1252 (once or more): turn it back into
        // C:\Users\élève, or the game cannot create its folders there (Access denied).
        static string Unmangle(string s)
        {
            if (s == null) return null;
            var w1252 = System.Text.Encoding.GetEncoding(1252);
            for (int i = 0; i < 4 && (s.IndexOf('Ã') >= 0 || s.IndexOf('Â') >= 0); i++)
            {
                var t = System.Text.Encoding.UTF8.GetString(w1252.GetBytes(s));
                if (t == s || t.IndexOf('\uFFFD') >= 0) break;
                s = t;
            }
            return s;
        }

        // The cache's path must be plain (no accents): with a profile like C:\Users\élève it goes to C:\VIctory\cache.
        string DefaultCacheDir()
        {
            var home = Path.Combine(dataRoot, "cache");
            return home.All(c => c < 128) ? home : Path.Combine(Path.GetPathRoot(dataRoot), "VIctory", "cache");
        }

        void LoadSettings()
        {
            try
            {
                if (!File.Exists(settingsFile)) return;
                foreach (var line in File.ReadAllLines(settingsFile))
                {
                    var i = line.IndexOf('=');
                    if (i < 0) continue;
                    var k = line.Substring(0, i); var v = Unmangle(line.Substring(i + 1));
                    switch (k)
                    {
                        case "game": gameBox.Text = v; break;
                        case "exe": exeChoice = v.Length > 0 ? v : null; break;
                        case "mode": int m; if (int.TryParse(v, out m)) SelectMode(m); break;
                        case "panel": panelBox.Checked = v == "1"; break;
                        case "borderless": borderlessBox.Checked = v == "1"; break;
                        case "fps60": fps60Box.Checked = v == "1"; break;
                        case "reserve": reserveBox.Checked = v == "1"; break;
                        case "smooth": smoothBox.Checked = v == "1"; break;
                        case "quality": qualityBox.Checked = false; break;
                        case "faces": facesBox.Checked = v == "1"; break;
                        case "loading": loadingBox.Checked = v == "1"; break;
                        case "fights": fightsBox.Checked = v == "1"; break;
                        case "warm": warmBox.Checked = v == "1"; break;
                        case "cpu": cpuBox.Checked = v == "1"; break;
                        case "output": { int o; if (outputBox != null && int.TryParse(v, out o) && o >= 0 && o <= 3) outputBox.SelectedIndex = o; } break;
                    }
                }
            }
            catch (Exception) { }
        }

        void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsFile));
                Func<CheckBox, string> b = c => c.Checked ? "1" : "0";
                var mine = new[] {
                    "lang=" + (Pt ? "pt" : Fr ? "fr" : "en"), "cache=" + cacheDir, "data=" + dataDir,
                    "game=" + gameBox.Text.Trim(), "exe=" + (exeChoice ?? ""), "mode=" + modeIndex,
                    "panel=" + b(panelBox), "borderless=" + b(borderlessBox), "fps60=" + b(fps60Box), "reserve=" + b(reserveBox),
                    "smooth=" + b(smoothBox), "quality=" + b(qualityBox), "faces=" + b(facesBox), "loading=" + b(loadingBox), "fights=" + b(fightsBox), "warm=" + b(warmBox), "cpu=" + b(cpuBox), "output=" + (outputBox == null ? 0 : outputBox.SelectedIndex), "fsr=" + FsrMode,
                };
                // The other keys (shaders=, engine=, custom=, source=...) are kept: this used to drop them.
                var keys = new HashSet<string>(mine.Select(l => l.Substring(0, l.IndexOf('=') + 1)));
                lock (settingsLock)
                {
                    var others = File.Exists(settingsFile) ? File.ReadAllLines(settingsFile).Where(l => l.Contains("=") && !keys.Contains(l.Substring(0, l.IndexOf('=') + 1))).ToArray() : new string[0];
                    WriteLinesAtomic(settingsFile, mine.Concat(others).ToArray());
                }
            }
            catch (Exception) { }
        }
    }
}
