// Julia's Launcher: "Send errors". The player picks launcher errors or game errors, ticks what happened
// and writes a line; the launcher puts the right files in a zip on the Desktop (user name taken out of
// every path), opens the folder and offers the ways to send it: straight to edupode (a compact copy of the
// package to the Cloudflare Worker, POST /v1/report, which only takes it in), a GitHub issue opened in the
// browser already filled in, or the Discord channel. Nothing is sent unless the player presses a button.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace VIctoryLauncher
{
    partial class LauncherForm
    {
        void ReportErrors()
        {
            var f = new Form { Text = L("Send errors", "Enviar erros"), ClientSize = new Size(640, 560), BackColor = Theme.Back, ForeColor = Theme.Text,
                Font = Theme.Ui(9.75f), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Icon = Icon };
            const int X = 20, W = 600;
            int y = 16;
            f.Controls.Add(new Heading(L("What kind of problem?", "Que tipo de problema?")) { Location = new Point(X, y), Width = W });
            y += 32;
            var launcherKind = new OptionCard { Text = L("Launcher errors", "Erros do launcher"), Line = L("The launcher closes, a button does nothing, Setup or an update fails", "O launcher fecha, um botao nao faz nada, o Preparar ou um update falha"), Location = new Point(X, y), Size = new Size(W, 58) };
            y += 64;
            var gameKind = new OptionCard { Text = L("Game errors", "Erros do jogo"), Line = L("The game closes or freezes, black squares, shaders, low FPS", "O jogo fecha ou congela, quadrados pretos, shaders, FPS baixos"), Location = new Point(X, y), Size = new Size(W, 58), Checked = true };
            launcherKind.CheckedChanged += (s, e) => { if (launcherKind.Checked) gameKind.Checked = false; else if (!gameKind.Checked) launcherKind.Checked = true; };
            gameKind.CheckedChanged += (s, e) => { if (gameKind.Checked) launcherKind.Checked = false; else if (!launcherKind.Checked) gameKind.Checked = true; };
            f.Controls.Add(launcherKind); f.Controls.Add(gameKind);
            y += 70;
            f.Controls.Add(new Heading(L("What happened? (tick any)", "O que aconteceu? (marca o que for)")) { Location = new Point(X, y), Width = W });
            y += 30;
            string[] picks = {
                L("My shader is not rendering properly", "Os shaders nao aparecem bem"),
                L("Virtual memory not turning on", "A memoria virtual nao liga"),
                L("Resizable BAR shows off but it is on", "O Resizable BAR aparece desligado mas esta ligado"),
                L("The game closes or crashes", "O jogo fecha ou crasha"),
                L("PLAY does nothing / stays grey", "O JOGAR nao faz nada / fica cinzento"),
                L("Preparing shaders fails", "Preparar os shaders falha"),
                L("Low FPS or stutter", "FPS baixos ou engasgos"),
                L("Black squares or wrong colours", "Quadrados pretos ou cores erradas"),
                L("Frame generation test (how it ran)", "Teste da geracao de frames (como correu)"),
                L("AMD card: tried the edupode engine", "Placa AMD: experimentei o motor edupode"),
            };
            var boxes = new List<Toggle>();
            for (int i = 0; i < picks.Length; i++)
            {
                var t = new Toggle { Text = picks[i], Location = new Point(X + (i % 2) * 300, y + (i / 2) * 30), Width = 296 };
                boxes.Add(t); f.Controls.Add(t);
            }
            y += (picks.Length + 1) / 2 * 30 + 8;
            f.Controls.Add(Small(L("In your words (what you did, what you saw):", "Por palavras tuas (o que fizeste, o que viste):"), X, y, W));
            y += 22;
            var words = new TextBox { Location = new Point(X, y), Size = new Size(W, 70), Multiline = true, BackColor = Theme.Card, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, Font = Theme.Ui(9.5f) };
            f.Controls.Add(words);
            y += 82;
            var make = new Pill { Main = true, Text = L("Make the package", "Criar o pacote"), Location = new Point(X + W - 200, y), Size = new Size(200, 40) };
            var cancel = new Pill { Text = L("Cancel", "Cancelar"), Location = new Point(X + W - 330, y + 3), Size = new Size(120, 34) };
            cancel.Click += (s, e) => f.Close();
            make.Click += (s, e) => {
                make.Enabled = false; make.Text = L("Collecting...", "A juntar...");
                bool game = gameKind.Checked;
                var ticked = boxes.Where(b => b.Checked).Select(b => b.Text).ToList();
                var said = words.Text.Trim();
                new Thread(() => {
                    string zip = null, summary = null, error = null;
                    try { zip = MakeReport(game, ticked, said, out summary); }
                    catch (Exception ex) { error = ex.Message; }
                    try
                    {
                        BeginInvoke(new Action(() => {
                            try { if (!f.IsDisposed) f.Close(); } catch (Exception) { }
                            if (zip == null) { MessageBox.Show(this, L("The package could not be made: ", "Nao deu para criar o pacote: ") + error, "Julia's Launcher"); return; }
                            try { Clipboard.SetText(summary); } catch (Exception) { }
                            try { Process.Start("explorer.exe", "/select,\"" + zip + "\""); } catch (Exception) { }
                            var body = string.Format(L("Done: {0} is on your Desktop.\r\n\r\n" +
                                                       "•  Send: one click, no account needed. The logs (the end of each one, settings; your user name taken out) go to the developers' private report box, and a public bug report opens on GitHub (felicienradio/cold-project-vi) with the summary only: your PC's parts, versions, preset, settings and what you ticked and wrote. You can follow it there.\r\n" +
                                                       "•  Send privately: the same, without the public GitHub report.",
                                                       "Feito: {0} esta no Ambiente de trabalho.\r\n\r\n" +
                                                       "•  Enviar: um clique, sem conta. Os logs (o fim de cada um, definicoes; sem o teu nome de utilizador) vao para a caixa privada de relatorios dos programadores, e abre-se um relatorio publico no GitHub (felicienradio/cold-project-vi) so com o resumo: as pecas do PC, versoes, predefinicao, definicoes e o que marcaste e escreveste. Podes acompanha-lo la.\r\n" +
                                                       "•  Enviar em privado: o mesmo, sem o relatorio publico no GitHub.",
                                                       "Terminé : {0} est sur ton Bureau.\r\n\r\n" +
                                                       "•  Envoyer : un clic, sans compte. Les logs (la fin de chacun, les réglages ; sans ton nom d'utilisateur) partent dans la boîte à rapports privée des développeurs, et un rapport de bug public s'ouvre sur GitHub (felicienradio/cold-project-vi) avec seulement le résumé : les composants de ton PC, les versions, le préréglage, les réglages et ce que tu as coché et écrit. Tu pourras le suivre là-bas.\r\n" +
                                                       "•  Envoyer en privé : la même chose, sans le rapport public sur GitHub."), Path.GetFileName(zip));
                            var answer = Notice(L("Send the report", "Enviar o relatorio"), body, L("Send", "Enviar", "Envoyer"), L("Send privately", "Enviar em privado", "Envoyer en privé"));
                            if (answer == 1) SendReport(zip, game, true);
                            if (answer == 2) SendReport(zip, game, false);
                        }));
                    }
                    catch (Exception) { }
                }) { IsBackground = true }.Start();
            };
            f.Controls.Add(make); f.Controls.Add(cancel);
            f.ClientSize = new Size(640, y + 60);
            f.ShowDialog(this);
        }

        // The package: a folder and its zip on the Desktop. Returns the zip's path.
        string MakeReport(bool game, List<string> ticked, string said, out string summary)
        {
            var h = hw ?? ReadHardware();
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var name = "VI-" + (game ? "game" : "launcher") + "-errors-" + h.Id.Replace("VI-", "") + "-" + stamp;
            var dir = Path.Combine(desktop, name);
            Directory.CreateDirectory(dir);
            string activity = "", options = "";
            Invoke(new Action(() => {
                activity = logBox.Text;
                options = string.Join("\r\n", Environment2().Where(kv => kv.Key != "VICTORY_GAME_DIR").Select(kv => kv.Key + "=" + kv.Value).ToArray());
            }));
            var missing = Missing();
            var sb = new StringBuilder();
            sb.AppendLine("Julia's Launcher error report");
            sb.AppendLine("ID: " + h.Id);
            sb.AppendLine("Kind: " + (game ? "game errors" : "launcher errors"));
            sb.AppendLine("Launcher: v" + Updater.Version + "   Engine: " + EngineName + "   Preset: " + Selected.Name);
            sb.AppendLine("GPU: " + h.Gpu + " (" + h.VramMb / 1024 + " GB, driver " + h.Driver + ")   " + RebarText() + "   VIctory runs on: " + probeAdapter);
            sb.AppendLine("GPU type: " + (integratedOnly ? "integrated only" : "dedicated") + "   Adapters: " + string.Join("; ", adapters.ConvertAll(a => a.VendorName + " " + a.Name + " (" + a.DedicatedMb + " MB own, " + a.SharedMb + " MB shared" + (a.Integrated ? ", integrated" : "") + ")").ToArray()));
            sb.AppendLine("CPU: " + h.Cpu + "   RAM: " + h.RamGb + " GB   Board: " + h.BoardMaker + " " + h.Board);
            sb.AppendLine("Windows: " + h.Os + "   Disk: " + h.Disk + (h.Laptop ? "   laptop" : ""));
            sb.AppendLine("Missing: " + (missing.Count == 0 ? "nothing" : string.Join(", ", missing.ToArray())));
            sb.AppendLine("Shaders cached: " + shaderFiles + (exeProblem != null ? "   Game file: " + exeProblem : ""));
            sb.AppendLine(ExeLine());
            if (ticked.Count > 0) sb.AppendLine("Ticked: " + string.Join("; ", ticked.ToArray()));
            if (said.Length > 0) sb.AppendLine("In their words: " + said);
            summary = Scrub(sb.ToString());
            File.WriteAllText(Path.Combine(dir, "summary.txt"), summary + "\r\nOptions:\r\n" + Scrub(options));
            File.WriteAllText(Path.Combine(dir, "launcher-activity.txt"), Scrub(activity));
            CopyScrubbed(settingsFile, Path.Combine(dir, "launcher.ini"));
            CopyScrubbed(Program.ErrorFile, Path.Combine(dir, "launcher-errors.txt"));
            var logs = Path.Combine(dataRoot, "logs");
            if (Directory.Exists(logs))
            {
                // Launcher errors: the builds and preparations; game errors: also the last three sessions.
                var wanted = Directory.GetFiles(logs, "*.log").Where(p => !Path.GetFileName(p).StartsWith("session-")).ToList();
                if (game) wanted.AddRange(Directory.GetFiles(logs, "session-*.log").OrderByDescending(File.GetLastWriteTime).Take(3));
                foreach (var p in wanted) CopyScrubbed(p, Path.Combine(dir, Path.GetFileName(p)));
            }
            if (game)
            {
                // The game folder as it is (names and sizes, paths relative to it) and what `victory gpu` says.
                string listing = "";
                try
                {
                    string folder = null;
                    Invoke(new Action(() => folder = gameBox.Text.Trim()));
                    listing = GameFolderListing(folder, true);
                }
                catch (Exception) { }
                File.WriteAllText(Path.Combine(dir, "game-folder.txt"), listing);
                if (OwnVictoryExe != null) File.WriteAllText(Path.Combine(dir, "victory-gpu.txt"), Scrub(RunCapture(OwnVictoryExe, "gpu")));
            }
            var zip = dir + ".zip";
            if (File.Exists(zip)) File.Delete(zip);
            ZipFile.CreateFromDirectory(dir, zip, CompressionLevel.Optimal, false);
            return zip;
        }

        // The game's folder for a report, paths relative to it (no user name): its size and file count; each
        // folder at its top with its files and size; and, in full, every file at its top and every .rpf below.
        // Two PCs whose games stopped where others start (a component of the game that did not start) are
        // compared with one that plays from this (28/11/2026). Compact: one line, for every game's summary.
        static string GameFolderListing(string folder, bool full)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return full ? "(no game folder)\r\n" : "Game folder: none";
            long total = 0; int files = 0; bool cut = false;
            var tops = new SortedDictionary<string, long[]>(StringComparer.OrdinalIgnoreCase);
            var rpfs = new List<string>();
            long rpfBytes = 0; int rpfCount = 0;
            var root = folder.TrimEnd('\\') + "\\";
            try
            {
                foreach (var p in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
                {
                    if (++files > 200000) { cut = true; break; }
                    long len = 0;
                    try { len = new FileInfo(p).Length; } catch (Exception) { }
                    total += len;
                    var rel = p.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? p.Substring(root.Length) : Path.GetFileName(p);
                    var slash = rel.IndexOf('\\');
                    var top = slash < 0 ? "" : rel.Substring(0, slash);
                    long[] t;
                    if (!tops.TryGetValue(top, out t)) tops[top] = t = new long[2];
                    t[0]++; t[1] += len;
                    if (rel.EndsWith(".rpf", StringComparison.OrdinalIgnoreCase))
                    {
                        rpfCount++; rpfBytes += len;
                        if (full && rpfs.Count < 800) rpfs.Add(string.Format("{0,16:N0}  {1}", len, rel));
                    }
                }
            }
            catch (Exception) { cut = true; }
            Func<long, string> gb = b => (b / 1073741824.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " GB";
            if (!full)
                return string.Format("Game folder: {0:N0} files, {1}{2}; .rpf: {3:N0} ({4}); folders: {5}", files, gb(total), cut ? " (cut)" : "", rpfCount, gb(rpfBytes),
                    string.Join(", ", tops.Where(kv => kv.Key.Length > 0).Select(kv => kv.Key + " " + kv.Value[0] + "/" + gb(kv.Value[1])).ToArray()));
            var sb = new StringBuilder();
            sb.AppendLine(string.Format("{0:N0} files, {1}{2}; .rpf: {3:N0} files, {4}", files, gb(total), cut ? " (listing cut)" : "", rpfCount, gb(rpfBytes)));
            sb.AppendLine();
            sb.AppendLine("Files at the top:");
            try
            {
                foreach (var p in Directory.GetFiles(folder).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                    sb.AppendLine(string.Format("{0,16:N0}  {1}", new FileInfo(p).Length, Path.GetFileName(p)));
            }
            catch (Exception) { }
            sb.AppendLine();
            sb.AppendLine("Folders at the top (files, size):");
            foreach (var kv in tops.Where(kv => kv.Key.Length > 0))
                sb.AppendLine(string.Format("{0,8:N0}  {1,10}  {2}", kv.Value[0], gb(kv.Value[1]), kv.Key));
            sb.AppendLine();
            sb.AppendLine(".rpf files:");
            foreach (var l in rpfs) sb.AppendLine(l);
            if (rpfCount > rpfs.Count) sb.AppendLine("  ... " + (rpfCount - rpfs.Count) + " more");
            return sb.ToString();
        }

        public const string ReportEndpoint = "https://vi-hardware.edupodept.workers.dev/v1/report";
        const int MaxUpload = 1800 * 1024;

        // A compact copy of the package (text files whole, the last part of each log) sent to the
        // Worker, which keeps it for edupode only. Says the report's number, or why it failed.
        void SendReport(string zip, bool game, bool open)
        {
            var dir = zip.Substring(0, zip.Length - 4);
            var hwNow = hw;
            new Thread(() => {
                string said, issue = null, viId = "";
                bool sent = false;
                try
                {
                    viId = (hwNow ?? ReadHardware()).Id;
                    byte[] data = null;
                    foreach (var tail in new[] { 400 * 1024, 150 * 1024, 40 * 1024 })
                    {
                        data = CompactZip(dir, tail);
                        if (data.Length <= MaxUpload) break;
                    }
                    if (data.Length > MaxUpload) throw new Exception(L("the package is too big to send; use the GitHub issue", "o pacote e grande demais; usa a issue do GitHub"));
                    ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
                    var req = (HttpWebRequest)WebRequest.Create(ReportEndpoint);
                    req.Method = "POST";
                    req.ContentType = "application/zip";
                    req.UserAgent = "Julias-Launcher";
                    req.Timeout = 60000;
                    req.Headers["X-VI-Id"] = viId;
                    req.Headers["X-VI-Kind"] = game ? "game" : "launcher";
                    if (open) req.Headers["X-VI-Public"] = "1";
                    req.ContentLength = data.Length;
                    using (var s = req.GetRequestStream()) s.Write(data, 0, data.Length);
                    string answer;
                    using (var r = (HttpWebResponse)req.GetResponse())
                    using (var rd = new StreamReader(r.GetResponseStream())) answer = rd.ReadToEnd();
                    var n = System.Text.RegularExpressions.Regex.Match(answer, @"""n""\s*:\s*(\d+)").Groups[1].Value;
                    // The public issue's address, when the box opened one (it answers {"n":…, "issue":"https://github.com/…"}).
                    var m = System.Text.RegularExpressions.Regex.Match(answer, @"""issue""\s*:\s*""(https://github\.com/[^""]+)""");
                    issue = m.Success ? m.Groups[1].Value : null;
                    said = issue != null
                        ? string.Format(L("Report #{0} sent. Its public bug report is on GitHub: {1}\r\n\r\nYour hardware ID is {2} (copied).",
                                          "Relatorio n.o {0} enviado. O relatorio publico esta no GitHub: {1}\r\n\r\nO teu ID de hardware e {2} (copiado).",
                                          "Rapport n° {0} envoyé. Son rapport public est sur GitHub : {1}\r\n\r\nTon ID matériel est {2} (copié)."), n, issue, viId)
                        : string.Format(L("Sent to edupode: report #{0}.\r\n\r\nYour hardware ID is {1} (copied). Paste it on Discord, in \"{2}\", so edupode can find your reports.",
                                          "Enviado ao edupode: relatorio n.o {0}.\r\n\r\nO teu ID de hardware e {1} (copiado). Cola-o no Discord, em \"{2}\", para o edupode encontrar os teus relatorios."), n, viId, DiscordChannel);
                    sent = true;
                }
                catch (WebException e)
                {
                    var r = e.Response as HttpWebResponse;
                    said = r != null && (int)r.StatusCode == 429
                        ? L("Too many reports this hour: try again later, or use the GitHub issue.", "Demasiados relatorios nesta hora: tenta mais tarde, ou usa a issue do GitHub.")
                        : L("Could not send the report: ", "Nao deu para enviar o relatorio: ") + e.Message;
                    if (r != null) r.Close();
                }
                catch (Exception e) { said = L("Could not send the report: ", "Nao deu para enviar o relatorio: ") + e.Message; }
                SafeLog(said);
                try
                {
                    BeginInvoke(new Action(() => {
                        if (sent) { try { Clipboard.SetText(viId); } catch (Exception) { } }
                        if (sent && issue != null)
                        {
                            if (MessageBox.Show(this, said + L("\r\n\r\nOpen it now?", "\r\n\r\nAbrir agora?", "\r\n\r\nL'ouvrir maintenant ?"), "Julia's Launcher", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                                OpenUrl(issue);
                        }
                        else if (sent && MessageBox.Show(this, said + L("\r\n\r\nOpen Discord now?", "\r\n\r\nAbrir o Discord agora?"), "Julia's Launcher", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                            OpenUrl(DiscordInvite);
                        else if (!sent) MessageBox.Show(this, said, "Julia's Launcher");
                    }));
                }
                catch (Exception) { }
            }) { IsBackground = true }.Start();
        }

        // The package's text files whole and the last `tail` bytes of each log, zipped in memory.
        static byte[] CompactZip(string dir, int tail)
        {
            using (var mem = new MemoryStream())
            {
                using (var z = new ZipArchive(mem, ZipArchiveMode.Create, true))
                    foreach (var f in Directory.GetFiles(dir))
                    {
                        var bytes = File.ReadAllBytes(f);
                        if (f.EndsWith(".log", StringComparison.OrdinalIgnoreCase) && bytes.Length > tail)
                            bytes = bytes.Skip(bytes.Length - tail).ToArray();
                        var e = z.CreateEntry(Path.GetFileName(f), CompressionLevel.Optimal);
                        using (var s = e.Open()) s.Write(bytes, 0, bytes.Length);
                    }
                return mem.ToArray();
            }
        }

        // A new issue on edupode's repository, filled in through the address (title, body, label): GitHub
        // opens it for the player to attach the zip and submit. The summary is already scrubbed of the user
        // name; the address stays under the length browsers and GitHub take.
        static string IssueUrl(bool game, List<string> ticked, string said, string summary, string zipName)
        {
            var first = ticked.Count > 0 ? ticked[0] : said.Length > 0 ? said : (game ? "Game problem" : "Launcher problem");
            if (first.Length > 70) first = first.Substring(0, 70) + "...";
            var title = "[" + (game ? "game" : "launcher") + "] " + first;
            var text = new StringBuilder();
            text.AppendLine("<!-- Drag " + zipName + " from your Desktop into this box before submitting. -->");
            text.AppendLine("**Package:** drag `" + zipName + "` (on your Desktop) here.");
            text.AppendLine();
            text.AppendLine("```text");
            text.AppendLine(summary.Length > 4500 ? summary.Substring(0, 4500) + "\n..." : summary.TrimEnd());
            text.AppendLine("```");
            return "https://github.com/edupode/cold-project-vi/issues/new?labels=" + Uri.EscapeDataString(game ? "bug,game" : "bug,launcher")
                + "&title=" + Uri.EscapeDataString(title) + "&body=" + Uri.EscapeDataString(text.ToString());
        }

        // Paths name the player (C:\Users\<name>): the name is taken out.
        static string Scrub(string text)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var t = (text ?? "").Replace(home, "%USERPROFILE%");
            // Other spellings of the profile path (C:/Users/<name>, a short name); a bare word is left
            // alone (the name may be part of other words).
            return System.Text.RegularExpressions.Regex.Replace(t, @"(?i)([\\/]Users[\\/])[^\\/\r\n""']+", "$1<user>");
        }

        // At most the last 20 MB of a log, scrubbed.
        static void CopyScrubbed(string from, string to)
        {
            try
            {
                if (!File.Exists(from)) return;
                using (var f = new FileStream(from, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (f.Length > (20L << 20)) f.Seek(-(20L << 20), SeekOrigin.End);
                    File.WriteAllText(to, Scrub(new StreamReader(f).ReadToEnd()));
                }
            }
            catch (Exception) { }
        }
    }
}
