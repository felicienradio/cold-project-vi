// Julia's Launcher: a standard log of each game, sent when the game closes, for players who said yes.
// Asked once, apart from hardware sharing (which never sends logs): launcher.ini's share_logs=1 or 0.
// The package is small and the same every time: a summary (hardware, preset, options, how long the game ran,
// how it ended) and the last part of the session's log, the user name taken out of every path. It goes to
// the same private report box as "Send errors" (POST /v1/report, kind "session"), read only by edupode.
using System;
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
        const int SessionTail = 256 * 1024;

        bool SessionLogsOn { get { return ReadSetting("share_logs") == "1"; } }

        // Once, before a game: what is sent and when, yes or no. Asked of everyone who has not answered yet.
        void AskSessionLogs()
        {
            if (ReadSetting("share_logs") != null) return;
            var f = new Form { Text = L("Game logs", "Logs do jogo", "Logs du jeu"), ClientSize = new Size(660, 470), BackColor = Theme.Back, ForeColor = Theme.Text,
                Font = Theme.Ui(9.75f), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Icon = Icon };
            f.Controls.Add(new Heading(L("New: send a log of each game?", "Novo: enviar uma log de cada jogo?", "Nouveau : envoyer un log de chaque partie ?")) { Location = new Point(20, 16), Width = 620 });
            var text = new Label { Location = new Point(20, 50), Size = new Size(620, 340), ForeColor = Theme.Text, BackColor = Theme.Back };
            text.Text = string.Join("\r\n", Pt ? new[] {
                "Quando o jogo fecha, o Julia's Launcher pode enviar ao edupode uma log padrao dessa sessao.",
                "",
                "PORQUE AJUDA",
                "   As logs mostram onde o jogo fica lento, que shaders falham e porque fecha, em PCs que nao",
                "   temos. E assim que as proximas otimizacoes sao escolhidas.",
                "",
                "O QUE E ENVIADO, CADA VEZ QUE O JOGO FECHA",
                "   Um resumo (placa grafica, processador, RAM, Windows, predefinicao e opcoes, quanto tempo",
                "   correu, como acabou) e a parte final da log do jogo (cerca de 256 KB).",
                "   O teu nome de utilizador e tirado de todos os caminhos. So o edupode le as logs.",
                "",
                "NUNCA E ENVIADO",
                "   Os teus ficheiros, os teus saves, o teu IP.",
                "",
                "Podes mudar quando quiseres em Hardware e ID.",
            } : Fr ? new[] {
                "Quand le jeu se ferme, Julia's Launcher peut envoyer à edupode un log standard de la partie.",
                "",
                "POURQUOI ÇA AIDE",
                "   Les logs montrent où le jeu ralentit, quels shaders échouent et pourquoi il se ferme, sur des PC",
                "   que nous n'avons pas. C'est ainsi que les prochaines optimisations sont choisies.",
                "",
                "CE QUI EST ENVOYÉ, À CHAQUE FERMETURE DU JEU",
                "   Un résumé (carte graphique, processeur, RAM, Windows, préréglage et options, durée de la",
                "   partie, comment elle a fini) et la fin du log du jeu (environ 256 Ko).",
                "   Ton nom d'utilisateur est retiré de tous les chemins. Seul edupode lit les logs.",
                "",
                "JAMAIS ENVOYÉ",
                "   Tes fichiers, tes sauvegardes, ton adresse IP.",
                "",
                "Tu peux changer d'avis à tout moment dans Matériel et ID.",
            } : new[] {
                "When the game closes, Julia's Launcher can send edupode a standard log of that game.",
                "",
                "WHY IT HELPS",
                "   Logs show where the game slows down, which shaders fail and why it closes, on PCs we do not",
                "   have. That is how the next optimizations are chosen.",
                "",
                "WHAT IS SENT, EACH TIME THE GAME CLOSES",
                "   A summary (graphics card, processor, RAM, Windows, preset and options, how long the game ran,",
                "   how it ended) and the last part of the game's log (about 256 KB).",
                "   Your user name is taken out of every path. Only edupode reads the logs.",
                "",
                "NEVER SENT",
                "   Your files, your saves, your IP address.",
                "",
                "You can change it any time on Hardware & ID.",
            });
            f.Controls.Add(text);
            var yes = new Pill { Main = true, Text = L("Yes, send game logs", "Sim, enviar as logs", "Oui, envoyer les logs"), Location = new Point(20, 408), Size = new Size(250, 42) };
            var no = new Pill { Text = L("No", "Nao", "Non"), Location = new Point(280, 412), Size = new Size(140, 34) };
            yes.Click += (s, e) => { SetSessionLogs(true); f.Close(); };
            no.Click += (s, e) => { SetSessionLogs(false); f.Close(); };
            f.Controls.Add(yes); f.Controls.Add(no);
            f.ShowDialog(this);
        }

        void SetSessionLogs(bool on)
        {
            WriteSetting("share_logs", on ? "1" : "0");
            if (logsBox != null && logsBox.Checked != on) logsBox.Checked = on;
            Log(on ? L("Game logs: one is sent to edupode when each game closes. Thank you!", "Logs do jogo: uma e enviada ao edupode quando cada jogo fecha. Obrigado!", "Logs du jeu : un est envoyé à edupode à la fin de chaque partie. Merci !")
                   : L("Game logs are not sent.", "As logs do jogo nao sao enviadas.", "Les logs du jeu ne sont pas envoyés."));
        }

        OptionCard logsBox;

        // The switch on Hardware & ID, under hardware sharing.
        int SessionLogsSection(Panel page, int X, int y, int W)
        {
            logsBox = new OptionCard { Text = L("Send a log of each game to edupode", "Enviar uma log de cada jogo ao edupode", "Envoyer un log de chaque partie à edupode"),
                Line = L("When the game closes: a summary and the end of its log, your user name taken out.", "Quando o jogo fecha: um resumo e o fim da log, sem o teu nome de utilizador.", "À la fermeture du jeu : un résumé et la fin du log, sans ton nom d'utilisateur."),
                Location = new Point(X, y), Size = new Size(W, 58), Checked = SessionLogsOn };
            logsBox.CheckedChanged += (s, e) => { if (logsBox.Checked != SessionLogsOn) SetSessionLogs(logsBox.Checked); };
            page.Controls.Add(logsBox);
            return y + 66;
        }

        // After a game: the package, sent in the background. Nothing is shown unless it was asked for.
        void SendSessionLog(string log, int code, DateTime started, DateTime ended)
        {
            if (!SessionLogsOn) return;
            string summary;
            try { summary = SessionSummary(code, started, ended); }
            catch (Exception) { return; }
            var hwNow = hw;
            new Thread(() => {
                try
                {
                    // (Read here, not before the thread: this can be the thread that waits for the game, and it must not throw.)
                    var viId = (hwNow ?? ReadHardware()).Id;
                    byte[] data;
                    using (var mem = new MemoryStream())
                    {
                        using (var z = new ZipArchive(mem, ZipArchiveMode.Create, true))
                        {
                            Put(z, "summary.txt", Encoding.UTF8.GetBytes(summary));
                            Put(z, Path.GetFileName(log), Encoding.UTF8.GetBytes(Scrub(Tail(log, SessionTail))));
                        }
                        data = mem.ToArray();
                    }
                    if (data.Length > MaxUpload) return;
                    ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
                    var req = (HttpWebRequest)WebRequest.Create(ReportEndpoint);
                    req.Method = "POST";
                    req.ContentType = "application/zip";
                    req.UserAgent = "Julias-Launcher";
                    req.Timeout = 60000;
                    req.Headers["X-VI-Id"] = viId;
                    req.Headers["X-VI-Kind"] = "session";
                    req.ContentLength = data.Length;
                    using (var s = req.GetRequestStream()) s.Write(data, 0, data.Length);
                    using (var r = (HttpWebResponse)req.GetResponse()) { }
                    SafeLog(L("Game log sent to edupode.", "Log do jogo enviada ao edupode.", "Log du jeu envoyé à edupode."));
                }
                catch (Exception) { }
            }) { IsBackground = true }.Start();
        }

        static void Put(ZipArchive z, string name, byte[] bytes)
        {
            var e = z.CreateEntry(name, CompressionLevel.Optimal);
            using (var s = e.Open()) s.Write(bytes, 0, bytes.Length);
        }

        static string Tail(string file, int bytes)
        {
            try
            {
                using (var f = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (f.Length > bytes) f.Seek(-bytes, SeekOrigin.End);
                    return new StreamReader(f).ReadToEnd();
                }
            }
            catch (Exception) { return ""; }
        }

        // The same first lines as an error report, and how the game went.
        string SessionSummary(int code, DateTime started, DateTime ended)
        {
            var h = hw ?? ReadHardware();
            string options = "", folder = null;
            Invoke(new Action(() => {
                options = string.Join("\r\n", Environment2().Where(kv => kv.Key != "VICTORY_GAME_DIR").Select(kv => kv.Key + "=" + kv.Value).ToArray());
                folder = gameBox.Text.Trim();
            }));
            var sb = new StringBuilder();
            sb.AppendLine("Julia's Launcher game log");
            sb.AppendLine("ID: " + h.Id);
            sb.AppendLine("Kind: session");
            sb.AppendLine("Launcher: v" + Updater.Version + "   Engine: " + EngineName + "   Preset: " + Selected.Name + "   Output: " + OutputHeight + "p");
            sb.AppendLine("GPU: " + h.Gpu + " (" + h.VramMb / 1024 + " GB, driver " + h.Driver + ")   " + RebarText() + "   VIctory runs on: " + probeAdapter);
            sb.AppendLine("CPU: " + h.Cpu + "   RAM: " + h.RamGb + " GB   Board: " + h.BoardMaker + " " + h.Board);
            sb.AppendLine("Windows: " + h.Os + "   Disk: " + h.Disk + (h.Laptop ? "   laptop" : ""));
            sb.AppendLine("Shaders cached: " + shaderFiles);
            sb.AppendLine(ExeLine());
            sb.AppendLine(GameFolderListing(folder, false));
            sb.AppendLine(string.Format("Played: {0:0.0} min   Ended: {1}", (ended - started).TotalMinutes, code == 0 ? "normally (code 0)" : "with an error (code " + code + ")"));
            return Scrub(sb.ToString()) + "\r\nOptions:\r\n" + Scrub(options);
        }
    }
}
