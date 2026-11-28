// Julia's Launcher: the game session (what keeps PLAY on or off, a stuck session closed) and the
// choice of the game's executable when a dump has more than one.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace VIctoryLauncher
{
    enum GameState { None, Running, Stuck }

    partial class LauncherForm
    {
        [DllImport("user32.dll")] static extern bool IsHungAppWindow(IntPtr hwnd);

        // The game this launcher started, and the pipelines of its session made after it closed.
        Process gameProc, afterWarm;
        // When the game this launcher started lost its window (its process still there).
        DateTime? windowGone;
        bool gameHadWindow;

        // What is running now. A game is a victory.exe with a window, or the one this launcher
        // started; tools (replay, warm, shader translation) are victory.exe too, with no window.
        // Stuck: a window that has not answered for 5 seconds (a crash, an error dialog), or the
        // game this launcher started still there 15 seconds after its window closed.
        GameState State()
        {
            var state = GameState.None;
            // (gameProc is set to null by another thread when the game ends: read once.)
            var gp = gameProc;
            foreach (var p in Process.GetProcessesByName("victory"))
            {
                try
                {
                    bool ours = gp != null && !gp.HasExited && p.Id == gp.Id;
                    var window = p.MainWindowHandle;
                    if (ours)
                    {
                        if (window != IntPtr.Zero) { gameHadWindow = true; windowGone = null; }
                        else if (gameHadWindow && windowGone == null) windowGone = DateTime.Now;
                    }
                    if (!ours && window == IntPtr.Zero) continue;
                    bool stuck = (window != IntPtr.Zero && IsHungAppWindow(window))
                        || (ours && windowGone != null && (DateTime.Now - windowGone.Value).TotalSeconds > 15);
                    if (stuck) return GameState.Stuck;
                    state = GameState.Running;
                }
                catch (Exception) { }
                finally { p.Dispose(); }
            }
            return state;
        }

        // Tool processes (shader translation, pipelines) this window started: stopped if it closes mid-way,
        // and never taken for someone else's.
        readonly List<Process> tools = new List<Process>();

        bool Ours(int id)
        {
            Func<Process, bool> same = p => { try { return p != null && !p.HasExited && p.Id == id; } catch (Exception) { return false; } };
            if (same(gameProc) || same(afterWarm)) return true;
            lock (tools) return tools.Any(same);
        }

        // A log line from a worker thread that never throws: the window may be closed by then (Log posts to it).
        void SafeLog(string line)
        {
            try { Log(line); } catch (Exception) { }
        }

        // VIctory preparations (shaders, warm, precompile) this window did not start: most often one a launcher
        // window left running when it closed. Read from each process's command line. A helper that one of this
        // window's own processes started is ours too (its parent is), never "another window's".
        List<int> OtherPreparations()
        {
            var ids = new List<int>();
            try
            {
                var rows = new List<Tuple<int, int, string>>();
                using (var q = new System.Management.ManagementObjectSearcher("SELECT ProcessId, ParentProcessId, CommandLine FROM Win32_Process WHERE Name = 'victory.exe'"))
                using (var found = q.Get())
                    foreach (System.Management.ManagementObject o in found)
                        using (o)
                            rows.Add(Tuple.Create(Convert.ToInt32(o["ProcessId"]), Convert.ToInt32(o["ParentProcessId"]), o["CommandLine"] as string ?? ""));
                Func<int, bool> ownTree = id =>
                {
                    for (int hops = 0; hops < 16; hops++)
                    {
                        if (Ours(id)) return true;
                        var row = rows.FirstOrDefault(r => r.Item1 == id);
                        if (row == null) return false;
                        id = row.Item2;
                    }
                    return false;
                };
                foreach (var r in rows)
                    if (System.Text.RegularExpressions.Regex.IsMatch(r.Item3, @"\s(shaders|warm|precompile)(\s|$)") && !ownTree(r.Item1)) ids.Add(r.Item1);
            }
            catch (Exception) { }
            return ids;
        }

        // Before PLAY or a preparation: two VIctory processes on one cache run slowly and can leave it half written
        // (an AMD player's game ended at start that way, 27/11/2026). Asks to stop the other one; false to wait.
        bool ClearOtherPreparations()
        {
            var others = OtherPreparations();
            if (others.Count == 0) return true;
            var text = L("Shaders are still being prepared by another launcher window (or one that was closed). Two at once run slowly and can damage the cache.\r\n\r\nStop it and go on? Nothing is lost: what is already translated stays in the cache.",
                         "Os shaders ainda estao a ser preparados por outra janela do launcher (ou uma que foi fechada). Dois ao mesmo tempo correm devagar e podem estragar a cache.\r\n\r\nParar e continuar? Nada se perde: o que ja foi traduzido fica na cache.",
                         "Les shaders sont encore en préparation par une autre fenêtre du launcher (ou une fenêtre fermée). Deux à la fois sont lents et peuvent abîmer le cache.\r\n\r\nL'arrêter et continuer ? Rien n'est perdu : ce qui est déjà traduit reste dans le cache.");
            if (MessageBox.Show(this, text, "Julia's Launcher", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return false;
            // The question may have stood open for minutes: only what is still the same kind of process is stopped
            // (a number freed in the meantime may belong to something else now).
            var still = OtherPreparations();
            foreach (var id in others) if (still.Contains(id)) try { RunWait("taskkill", "/PID " + id + " /T /F", false); } catch (Exception) { }
            Log(L("Stopped a preparation left running by another launcher window.", "Parada uma preparacao deixada a correr por outra janela do launcher."));
            return true;
        }

        // Closing in the middle of a preparation: asked, and the preparation stopped with the window, so it is not
        // left running for the next window to trip over. The game itself is never closed here.
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (e.Cancel) return;
            List<Process> running;
            lock (tools) running = tools.Where(t => { try { return !t.HasExited; } catch (Exception) { return false; } }).ToList();
            if (running.Count == 0) return;
            if (e.CloseReason == CloseReason.UserClosing &&
                MessageBox.Show(this, L("Shaders or pipelines are still being prepared. Stop and close? It carries on where it stopped next time.",
                                        "Os shaders ou as pipelines ainda estao a ser preparados. Parar e fechar? Continua onde parou da proxima vez.",
                                        "Les shaders ou les pipelines sont encore en préparation. Arrêter et fermer ? Ça reprend là où ça s'est arrêté la prochaine fois."),
                    "Julia's Launcher", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) { e.Cancel = true; return; }
            foreach (var t in running)
                try { if (!t.HasExited) RunWait("taskkill", "/PID " + t.Id + " /T /F", false); } catch (Exception) { }
        }

        // ---------------------------------------------------------------- before PLAY: pipelines and memory

        bool pipelineHintShown, heavyAppsAsked;

        // The first game on an engine whose pipelines were never built ahead: the driver compiles each one
        // during play (hundreds of draws missing a frame for the first minutes). This run writes them down,
        // so the next one can build them before it opens: said once, with what to do.
        void PipelineHint()
        {
            if (pipelineHintShown || !warmBox.Checked) return;
            var pdir = Path.Combine(ActiveCacheDir, "pipelines");
            if (Lines(Path.Combine(pdir, "warm-done.txt")) > 0) return;
            pipelineHintShown = true;
            MessageBox.Show(this, L("First game on this engine: its pipelines are not built yet, so the first minutes stutter and some objects are missing while your graphics driver compiles them.\r\n\r\n"
                                  + "This game writes them down. To have them built before the game opens:\r\n"
                                  + "  1.  Play 5-10 minutes in the world, then close the game.\r\n"
                                  + "  2.  Wait until Activity says \"Done\" (the launcher builds them).\r\n"
                                  + "  3.  Start the game again.",
                                    "Primeiro jogo neste motor: as pipelines ainda nao foram feitas, por isso os primeiros minutos engasgam e faltam objetos enquanto o driver as compila.\r\n\r\n"
                                  + "Este jogo regista-as. Para serem feitas antes do jogo abrir:\r\n"
                                  + "  1.  Joga 5-10 minutos no mundo e fecha o jogo.\r\n"
                                  + "  2.  Espera que a Atividade diga \"Feito\" (o launcher fa-las).\r\n"
                                  + "  3.  Abre o jogo outra vez.",
                                    "Premier jeu sur ce moteur : ses pipelines ne sont pas encore faites, donc les premières minutes saccadent et des objets manquent pendant que ton pilote les compile.\r\n\r\n"
                                  + "Ce jeu les note. Pour qu'elles soient faites avant l'ouverture du jeu :\r\n"
                                  + "  1.  Joue 5-10 minutes dans le monde, puis ferme le jeu.\r\n"
                                  + "  2.  Attends que l'Activité dise « Terminé » (le launcher les fait).\r\n"
                                  + "  3.  Relance le jeu."),
                "Julia's Launcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Apps that hold a lot of RAM and are easy to close for a game (browsers, chat, music).
        static readonly string[] HeavyApps = { "firefox", "chrome", "msedge", "opera", "brave", "vivaldi", "Discord", "Spotify", "Teams", "ms-teams", "slack", "steamwebhelper", "EpicGamesLauncher" };

        // Little RAM free: the game (about 20 GB in the world) and Windows swap to disk and textures
        // arrive slowly. Names what holds memory and how much, once per launcher window; false to cancel.
        bool HeavyAppsCheck()
        {
            if (heavyAppsAsked) return true;
            double freeGb;
            try { freeGb = new Microsoft.VisualBasic.Devices.ComputerInfo().AvailablePhysicalMemory / 1073741824.0; } catch (Exception) { return true; }
            if (freeGb >= 10) return true;
            var held = new List<Tuple<string, double>>();
            foreach (var name in HeavyApps)
            {
                long bytes = 0; string title = null;
                foreach (var p in Process.GetProcessesByName(name))
                    try { bytes += p.PrivateMemorySize64; if (title == null) title = p.ProcessName; } catch (Exception) { } finally { p.Dispose(); }
                if (bytes > (512L << 20)) held.Add(Tuple.Create(title ?? name, bytes / 1073741824.0));
            }
            if (held.Count == 0) return true;
            heavyAppsAsked = true;
            var list = string.Join("\r\n", held.OrderByDescending(h => h.Item2).Select(h => string.Format("  •  {0}: {1:0.0} GB", h.Item1, h.Item2)).ToArray());
            var ask = string.Format(L("Only {0:0.0} GB of RAM is free, and the game uses about 20 GB in the world. Short of RAM, Windows swaps to disk and textures load slowly.\r\n\r\nThese apps hold memory:\r\n{1}\r\n\r\nClose them first for a smoother game. Play anyway?",
                                      "So ha {0:0.0} GB de RAM livre e o jogo usa cerca de 20 GB no mundo. Sem RAM, o Windows usa o disco e as texturas carregam devagar.\r\n\r\nEstas apps ocupam memoria:\r\n{1}\r\n\r\nFecha-as primeiro para um jogo mais fluido. Jogar na mesma?",
                                      "Seulement {0:0.0} Go de RAM libre, et le jeu en utilise environ 20 Go dans le monde. À court de RAM, Windows passe par le disque et les textures arrivent lentement.\r\n\r\nCes applis occupent de la mémoire :\r\n{1}\r\n\r\nFerme-les d'abord pour un jeu plus fluide. Jouer quand même ?"),
                freeGb, list);
            return MessageBox.Show(this, ask, "Julia's Launcher", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        // Ends every game (and what it started) and the pipelines being made after a session.
        void CloseGames()
        {
            var ids = new List<int>();
            var gp = gameProc; var aw = afterWarm;
            foreach (var p in Process.GetProcessesByName("victory"))
            {
                try
                {
                    bool ours = gp != null && !gp.HasExited && p.Id == gp.Id;
                    bool tool = aw != null && !aw.HasExited && p.Id == aw.Id;
                    if (ours || tool || p.MainWindowHandle != IntPtr.Zero) ids.Add(p.Id);
                }
                catch (Exception) { }
                finally { p.Dispose(); }
            }
            // Forgotten first: the thread waiting on the game checks gameProc/afterWarm the moment the process
            // dies, and must see "closed from the launcher" (not start the next pipelines, and not have its new
            // afterWarm wiped by this method afterwards).
            gameProc = null; afterWarm = null; windowGone = null; gameHadWindow = false;
            foreach (var id in ids)
            {
                RunWait("taskkill", "/PID " + id + " /T /F", false);
                Log(string.Format(L("Closed VIctory (process {0}).", "VIctory fechado (processo {0})."), id));
            }
        }

        // ---------------------------------------------------------------- what is ready
        // Checked on a worker thread every few seconds; the window shows the result (Refresh2).
        volatile string exeProblem;
        volatile int shaderFiles;
        volatile bool pagefileOk = true, powerOk = true, gpuPrefOk = true, shadersOk = true;
        bool warnedThisSession;

        void StartChecks()
        {
            new Thread(() => {
                while (!IsDisposed)
                {
                    try { RunChecks(); } catch (Exception) { }
                    try { BeginInvoke(new Action(Refresh2)); } catch (Exception) { return; }
                    Thread.Sleep(8000);
                }
            }) { IsBackground = true, Priority = ThreadPriority.BelowNormal }.Start();
        }

        void RunChecks()
        {
            string game = null;
            Invoke(new Action(() => game = GameExe));
            exeProblem = ExeProblem(game);
            // Folder by folder (Setup.cs, CountUpTo): one entry renamed mid-walk no longer makes it 0.
            shaderFiles = CountUpTo(Path.Combine(ActiveCacheDir, "shaders"), int.MaxValue);
            shadersOk = ShadersReady();
            pagefileOk = PagefileOk();
            powerOk = BestPower() && SystemInformation.PowerStatus.PowerLineStatus != PowerLineStatus.Offline;
            gpuPrefOk = GpuPreferenceOk();
        }

        // Why the game's executable cannot be run, or null: a file that stopped downloading, a
        // placeholder, an encrypted package (VIctory's "malformed image: Could not read file magic").
        string ExeProblem(string path)
        {
            try
            {
                if (path == null || !File.Exists(path)) return null;
                var info = new FileInfo(path);
                var head = new byte[2];
                int got;
                using (var f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) got = f.Read(head, 0, 2);
                if (info.Length < (1L << 20) || got < 2 || head[0] != (byte)'M' || head[1] != (byte)'Z')
                    return string.Format(L("{0} is not a working program ({1:N0} bytes): the dump is incomplete or still encrypted. Download or extract it again.",
                        "{0} nao e um programa que funcione ({1:N0} bytes): o dump esta incompleto ou ainda encriptado. Volta a descarregar ou extrair."), Path.GetFileName(path), info.Length);
                // Only the executable, not the game's data (the whole dump is ~100 GB): the game ends at once
                // with code 1 (an RTX 3060 player's "Desktop\EXE" folder held 122 MB, 27/11/2026).
                long seen = 0;
                foreach (var f in Directory.EnumerateFiles(Path.GetDirectoryName(path), "*", SearchOption.AllDirectories))
                {
                    try { seen += new FileInfo(f).Length; } catch (Exception) { }
                    if (seen >= (20L << 30)) break;
                }
                if (seen < (20L << 30))
                    return string.Format(L("This game folder holds only {0:0.0} GB: the game's data is not in it (the whole dump is about 100 GB). Choose the folder of the complete dump, with all its files beside game_scarlett_master.exe.",
                        "Esta pasta do jogo so tem {0:0.0} GB: os dados do jogo nao estao nela (o dump completo tem cerca de 100 GB). Escolhe a pasta do dump completo, com todos os ficheiros ao lado do game_scarlett_master.exe."), seen / 1073741824.0);
                // Big enough, but the game's own files are not beside the executable (it sits in Downloads among
                // other files): the shaders do not prepare and the game cannot start.
                if (MissingDumpFiles(Path.GetDirectoryName(path)).Length > 0) return MissingDumpText(Path.GetDirectoryName(path));
                // The game's own DLLs: without one it ends at once with code 1, whatever the engine.
                var absent = MissingTitleDlls(Path.GetDirectoryName(path));
                if (absent.Length > 0)
                    return string.Format(L("The game's folder is missing its own files: {0}. Copy them from the complete game package next to {1}.",
                        "Faltam ficheiros do proprio jogo na pasta: {0}. Copia-os do pacote completo do jogo para junto do {1}.",
                        "Il manque des fichiers du jeu lui-même dans son dossier : {0}. Copie-les depuis le paquet complet du jeu à côté de {1}."),
                        string.Join(", ", absent), Path.GetFileName(path));
                // Empty program libraries beside it end the game the same way.
                var empty = Directory.GetFiles(Path.GetDirectoryName(path), "*.dll").Where(d => new FileInfo(d).Length < 64).Select(Path.GetFileName).Take(3).ToArray();
                if (empty.Length > 0)
                    return string.Format(L("The dump has empty files ({0}): download or extract it again.", "O dump tem ficheiros vazios ({0}): volta a descarregar ou extrair."), string.Join(", ", empty));
            }
            catch (Exception) { }
            return null;
        }

        // The DLLs of its own that the game (1.0.2.1) imports and VIctory does not provide: without one it cannot
        // start ("missing dlls" in its log, code 1). The upscaler and the C++ runtime beside them are not needed.
        static readonly string[] TitleDlls = { "amd_fidelityfx_loader_xbox.dll", "bink2scarlett.dll", "libhttpclient.gdk.dll", "xboxbvhbuilder.dll" };

        static string[] MissingTitleDlls(string dir)
        {
            try { return TitleDlls.Where(d => !File.Exists(Path.Combine(dir, d))).ToArray(); }
            catch (Exception) { return new string[0]; }
        }

        // The DLLs VIctory said were missing when the game started ("missing dlls   a.dll, b.dll not in <dir>, ..."),
        // from the end of a session's log; null if it said nothing of them.
        static string[] MissingDllsInLog(string log)
        {
            try
            {
                if (log == null || !File.Exists(log)) return null;
                string text;
                using (var f = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    f.Seek(Math.Max(0, f.Length - (256 << 10)), SeekOrigin.Begin);
                    using (var r = new StreamReader(f)) text = r.ReadToEnd();
                }
                foreach (var raw in text.Split('\n'))
                {
                    var line = raw.Trim();
                    if (!line.StartsWith("missing dlls")) continue;
                    var rest = line.Substring("missing dlls".Length).Trim();
                    var at = rest.IndexOf(" not in ", StringComparison.Ordinal);
                    if (at <= 0) continue;
                    return rest.Substring(0, at).Split(',').Select(d => d.Trim()).Where(d => d.Length > 0).ToArray();
                }
            }
            catch (Exception) { }
            return null;
        }

        // What the game would run without, in words for the warning.
        List<string> Missing()
        {
            var m = new List<string>();
            if (integratedOnly) m.Add(L("a graphics card with 4 GB or more (only integrated graphics found)", "uma placa grafica com 4 GB ou mais (so ha grafica integrada)", "une carte graphique de 4 Go ou plus (seulement un graphique intégré)"));
            if (!pagefileOk) m.Add(L("virtual memory managed by Windows", "memoria virtual gerida pelo Windows"));
            if (!powerOk) m.Add(L("best performance power (plugged in)", "energia em melhor desempenho (ligado a corrente)"));
            if (RebarMissing()) m.Add("Resizable BAR");
            if (!gpuPrefOk) m.Add(L("the dedicated graphics card for VIctory", "a placa grafica dedicada para o VIctory"));
            if (!shadersOk) m.Add(L("prepared shaders", "shaders preparados"));
            return m;
        }

        // ---------------------------------------------------------------- memory, and a game that cannot start
        // What Windows can still promise (RAM plus virtual memory not yet committed), in GB.
        static double CommitFreeGb()
        {
            try
            {
                using (var s = new System.Management.ManagementObjectSearcher("SELECT FreeVirtualMemory FROM Win32_OperatingSystem"))
                    foreach (System.Management.ManagementObject o in s.Get())
                        return Convert.ToDouble(o["FreeVirtualMemory"]) / (1024.0 * 1024.0);
            }
            catch (Exception) { }
            return -1;
        }

        // How much more Windows can promise by growing a page file it manages itself ("?:\pagefile.sys"
        // or "C:\pagefile.sys 0 0"), in GB: it grows on demand up to 3x the RAM, an eighth of the drive
        // and what the drive has free. FreeVirtualMemory leaves that out, so with a managed page file the
        // warning came at nearly every PLAY on a PC with 18 GB of RAM free (27/11/2026).
        double PagefileGrowthGb()
        {
            try
            {
                if (!PagefileOk()) return 0;
                double allocatedMb = 0;
                string drive = "C:\\";
                using (var s = new System.Management.ManagementObjectSearcher("SELECT Name, AllocatedBaseSize FROM Win32_PageFileUsage"))
                    foreach (System.Management.ManagementObject o in s.Get())
                    {
                        allocatedMb += Convert.ToDouble(o["AllocatedBaseSize"]);
                        var name = Convert.ToString(o["Name"]);
                        if (!string.IsNullOrEmpty(name) && name.Length >= 3) drive = name.Substring(0, 3);
                    }
                var d = new DriveInfo(drive);
                double gb = 1024.0 * 1024 * 1024;
                double most = Math.Min(Math.Max(3 * ramGb, 4), d.TotalSize / 8.0 / gb);
                double byDisk = allocatedMb / 1024.0 + Math.Max(0, d.AvailableFreeSpace / gb - 2);
                return Math.Max(0, Math.Min(most, byDisk) - allocatedMb / 1024.0);
            }
            catch (Exception) { return 0; }
        }

        // The game reserves about 24 GB as it starts: with less to promise, D3D12 refuses the device
        // ("not enough memory resources", 0x8007000E) and the window stays white.
        const double GameCommitGb = 24;

        bool EnoughMemory()
        {
            var now = CommitFreeGb();
            if (now < 0 || now >= GameCommitGb) return true;
            var free = now + PagefileGrowthGb();
            if (free >= GameCommitGb) return true;
            var ask = string.Format(L("Windows can only promise {0:0.0} GB more memory (RAM plus virtual memory); the game needs about {1:0} GB as it starts. With less, its window stays white.\r\n\r\n" +
                                      "•  Close apps (Performance page: what holds memory).\r\n•  Let Windows manage virtual memory (Setup), then restart the PC.\r\n\r\nPlay anyway?",
                                      "O Windows so pode prometer mais {0:0.0} GB de memoria (RAM mais memoria virtual); o jogo precisa de cerca de {1:0} GB ao arrancar. Com menos, a janela fica branca.\r\n\r\n" +
                                      "•  Fecha apps (pagina Desempenho: o que ocupa memoria).\r\n•  Deixa o Windows gerir a memoria virtual (Preparar) e reinicia o PC.\r\n\r\nJogar na mesma?",
                                      "Windows ne peut garantir que {0:0.0} Go de mémoire en plus (RAM et mémoire virtuelle) ; le jeu a besoin d'environ {1:0} Go au démarrage. Avec moins, sa fenêtre reste blanche.\r\n\r\n" +
                                      "•  Ferme des applis (page Performances : ce qui occupe la mémoire).\r\n•  Laisse Windows gérer la mémoire virtuelle (Installation), puis redémarre le PC.\r\n\r\nJouer quand même ?"), free, GameCommitGb);
            return MessageBox.Show(this, ask, "Julia's Launcher", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        // Reads the session's log for the first minute: when the game cannot start (no D3D12 device,
        // a broken game file, a crash) the player is told why instead of looking at a white window.
        void WatchStart(string log, Process game)
        {
            new Thread(() => {
              // Nothing here may throw: an unhandled exception on this thread ends the launcher.
              try
              {
                var until = DateTime.Now.AddSeconds(90);
                while (DateTime.Now < until && !game.HasExited)
                {
                    Thread.Sleep(2000);
                    string text;
                    try
                    {
                        using (var f = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            if (f.Length > (8L << 20)) f.Seek(-(8L << 20), SeekOrigin.End);
                            text = new StreamReader(f).ReadToEnd();
                        }
                    }
                    catch (Exception) { continue; }
                    if (text.Contains("presenting to a window")) return;
                    string why = null;
                    if (text.Contains("no host D3D12 device") && text.Contains("8007000E"))
                        why = L("Windows ran out of memory to start the graphics (0x8007000E), so the window stays white.\r\n\r\nClose other apps, let Windows manage virtual memory (Setup) and restart the PC, then play again.",
                                "O Windows ficou sem memoria para iniciar os graficos (0x8007000E), por isso a janela fica branca.\r\n\r\nFecha outras apps, deixa o Windows gerir a memoria virtual (Preparar) e reinicia o PC, depois joga de novo.");
                    else if (text.Contains("no host D3D12 device"))
                        why = L("The graphics card could not start D3D12, so the window stays white. Update the graphics driver and restart the PC.",
                                "A placa grafica nao conseguiu iniciar o D3D12, por isso a janela fica branca. Atualiza o driver grafico e reinicia o PC.");
                    else if (text.Contains("malformed image"))
                        why = L("The game's files are damaged or incomplete (malformed image). Download or extract the dump again.",
                                "Os ficheiros do jogo estao estragados ou incompletos (malformed image). Volta a descarregar ou extrair o dump.");
                    if (why == null) continue;
                    try
                    {
                        BeginInvoke(new Action(() => {
                            Log(why.Replace("\r\n", " "));
                            // The game may have closed on its own while this was posted: nothing left to ask.
                            bool gone;
                            try { gone = game.HasExited; } catch (Exception) { gone = true; }
                            if (gone) return;
                            if (MessageBox.Show(this, why + L("\r\n\r\nClose the game now?", "\r\n\r\nFechar o jogo agora?"), "Julia's Launcher", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                            {
                                CloseGames();
                                Refresh2();
                            }
                        }));
                    }
                    catch (Exception) { }
                    return;
                }
              }
              catch (Exception) { }
            }) { IsBackground = true }.Start();
        }

        // ---------------------------------------------------------------- the game's executable
        ComboBox exeBox;
        // The one chosen (launcher.ini's exe=), when the folder has several.
        string exeChoice;
        string exesOf;
        bool fillingExes;

        // The folder's game_scarlett*.exe, game_scarlett_master.exe first.
        static string[] GameExes(string dir)
        {
            try
            {
                if (dir.Length == 0 || !Directory.Exists(dir)) return new string[0];
                return Directory.GetFiles(dir, "game_scarlett*.exe")
                    .Where(f => f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    .Select(Path.GetFileName)
                    .OrderBy(n => n.Equals("game_scarlett_master.exe", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception) { return new string[0]; }
        }

        string GameExe
        {
            get
            {
                var dir = gameBox.Text.Trim();
                var all = GameExes(dir);
                var pick = exeChoice != null && all.Contains(exeChoice, StringComparer.OrdinalIgnoreCase) ? exeChoice
                    : all.Length > 0 ? all[0] : "game_scarlett_master.exe";
                return Path.Combine(dir, pick);
            }
        }

        ComboBox ExeBox(int x, int y, int width)
        {
            exeBox = new Dropdown {
                Location = new Point(x, y), Width = width,
                Visible = false,
            };
            exeBox.SelectedIndexChanged += (s, e) => {
                if (fillingExes || exeBox.SelectedItem == null) return;
                var name = (string)exeBox.SelectedItem;
                if (string.Equals(name, exeChoice, StringComparison.OrdinalIgnoreCase)) return;
                exeChoice = name;
                SaveSettings();
                Log(L("Game executable: ", "Executavel do jogo: ") + name);
                Refresh2();
            };
            return exeBox;
        }

        // The list follows the folder: shown only when there is a choice to make.
        void RefreshExes()
        {
            if (exeBox == null) return;
            var dir = gameBox.Text.Trim();
            var all = GameExes(dir);
            var key = dir + "|" + string.Join("|", all) + "|" + exeChoice;
            if (key == exesOf) return;
            bool listChanged = exesOf == null || !exesOf.StartsWith(dir + "|" + string.Join("|", all) + "|");
            exesOf = key;
            fillingExes = true;
            exeBox.Items.Clear();
            foreach (var n in all) exeBox.Items.Add(n);
            exeBox.Visible = all.Length > 1;
            if (all.Length > 0) exeBox.SelectedItem = Path.GetFileName(GameExe);
            fillingExes = false;
            if (all.Length > 1 && listChanged)
                Log(string.Format(L("The game folder has {0} executables; using {1} (change it beside the folder).",
                    "A pasta do jogo tem {0} executaveis; a usar o {1} (muda-o ao lado da pasta)."), all.Length, Path.GetFileName(GameExe)));
        }
    }
}
