// Julia's Launcher: updates from GitHub, the launcher's only way in. Every 5 minutes (and when it opens)
// it reads manifest.json of the latest release of felicienradio/cold-project-vi and its signature
// (manifest.sig, RSA-SHA256 by edupode's release key, which never leaves edupode's PC: the
// public half is in ReleaseKey.cs). A file is installed only when the signature and its SHA-256
// match, only names on the list below are taken, and only from that repository's releases.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace VIctoryLauncher
{
    static class Updater
    {
        // This launcher's version; a release with a higher one is an update.
        // felicienradio's 0.3.1 with edupode's fixes: above 0.3.1, on edupode's release channel and key.
        public const string Version = "0.3.25";
        public const string ManifestUrl = "https://github.com/felicienradio/cold-project-vi/releases/latest/download/manifest.json";
        public const string SignatureUrl = "https://github.com/felicienradio/cold-project-vi/releases/latest/download/manifest.sig";
        public const string DownloadPrefix = "https://github.com/felicienradio/cold-project-vi/releases/download/";
        // Every release of the repository, newest first, whatever branch it was cut from (main or edupode),
        // pre-releases included: the newest one that carries a signed manifest is the update. A release
        // without one (a draft turned public, a release from main with other files) no longer hides it.
        public const string ReleasesApi = "https://api.github.com/repos/felicienradio/cold-project-vi/releases?per_page=15";
        // What an update may bring, and where each goes (beside the launcher).
        // (D3D12/D3D12Core.dll from 0.3.7: a release may list it once launchers older than 0.3.7 are gone,
        // since those refuse an update list with a name they do not know.)
        public static readonly string[] Allowed = { "Julia's Launcher.exe", "victory.exe", "tools/nvsmooth30/version.dll", "D3D12/D3D12Core.dll" };

        public static Version Number(string v)
        {
            var digits = new string((v ?? "").TrimStart('v').TakeWhile(c => char.IsDigit(c) || c == '.').ToArray()).Trim('.');
            Version r;
            return System.Version.TryParse(digits.Count(c => c == '.') == 0 ? digits + ".0" : digits, out r) ? r : new Version(0, 0);
        }
    }

    class UpdateFile { public string Name, Url, Sha256; public long Size; }

    class Manifest
    {
        public string Version = "", Notes = "", NotesPt = "";
        public List<UpdateFile> Files = new List<UpdateFile>();
    }

    partial class LauncherForm
    {
        Manifest pending;           // downloaded, checked, waiting for the game to close
        string pendingDir;
        DateTime lastCheck;
        string updateState = "";
        Label updVersion, updState, updLast;
        TextBox updNotes;
        CheckBox autoUpdateBox;
        Pill installNow;
        System.Threading.Timer updateTimer;
        int checking;
        // The version this launcher installed without restarting (its files came, the launcher's own did not
        // change): not downloaded and installed again every 5 minutes.
        string installedVersion;
        bool installing;

        void StartUpdates()
        {
            // The launcher an update replaced: gone once the new one runs.
            try { var old = Path.Combine(root, "Julia's Launcher.old.exe"); if (File.Exists(old)) File.Delete(old); } catch (Exception) { }
            updateTimer = new System.Threading.Timer(_ => CheckForUpdate(false), null, 3000, 5 * 60 * 1000);
            FormClosed += (s, e) => { try { updateTimer.Dispose(); } catch (Exception) { } };
        }

        void CheckForUpdate(bool loud)
        {
            if (Interlocked.Exchange(ref checking, 1) == 1) return;
            try
            {
                lastCheck = DateTime.Now;
                if (pending != null) { TryInstall(false); return; }
                SetUpdateState(L("Checking GitHub...", "A verificar o GitHub..."));
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
                byte[] manifestBytes, sig;
                string manifestUrl, signatureUrl;
                if (!NewestSignedRelease(out manifestUrl, out signatureUrl))
                {
                    // (The API refused or is out of reach: the release marked latest, as before.)
                    manifestUrl = Updater.ManifestUrl; signatureUrl = Updater.SignatureUrl;
                }
                using (var web = new WebClient())
                {
                    web.Headers[HttpRequestHeader.UserAgent] = "Julias-Launcher";
                    try { manifestBytes = web.DownloadData(manifestUrl); }
                    catch (WebException e)
                    {
                        var r = e.Response as HttpWebResponse;
                        SetUpdateState(r != null && r.StatusCode == HttpStatusCode.NotFound
                            ? L("Up to date (no release published yet).", "Atualizado (ainda nao ha versoes publicadas).")
                            : L("Could not reach GitHub; trying again in 5 minutes.", "Sem ligacao ao GitHub; nova tentativa daqui a 5 minutos."));
                        return;
                    }
                    sig = Convert.FromBase64String(Encoding.ASCII.GetString(web.DownloadData(signatureUrl)).Trim());
                }
                if (!SignedByEdupode(manifestBytes, sig)) { SetUpdateState(L("An update was refused: its signature is not the project's.", "Um update foi recusado: a assinatura nao e a do projeto.")); return; }
                var m = ParseManifest(Encoding.UTF8.GetString(manifestBytes));
                if (m == null) { SetUpdateState(L("An update was refused: its list could not be read.", "Um update foi recusado: a lista nao se le.")); return; }
                if (Updater.Number(m.Version) <= Updater.Number(Updater.Version) || m.Version == installedVersion)
                {
                    SetUpdateState(string.Format(L("Up to date (v{0}).", "Atualizado (v{0})."), Updater.Version));
                    if (loud) Log(L("No update: this is the latest version.", "Sem updates: esta e a versao mais recente."));
                    return;
                }
                SetUpdateState(string.Format(L("Downloading v{0}...", "A descarregar a v{0}..."), m.Version));
                var dir = Path.Combine(Path.GetDirectoryName(dataRoot), "updates", m.Version);
                Directory.CreateDirectory(dir);
                using (var web = new WebClient())
                {
                    web.Headers[HttpRequestHeader.UserAgent] = "Julias-Launcher";
                    foreach (var f in m.Files)
                    {
                        var to = Path.Combine(dir, f.Name.Replace('/', '\\'));
                        Directory.CreateDirectory(Path.GetDirectoryName(to));
                        if (!(File.Exists(to) && Sha256(to) == f.Sha256)) web.DownloadFile(f.Url, to);
                        if (new FileInfo(to).Length != f.Size || Sha256(to) != f.Sha256)
                        {
                            File.Delete(to);
                            SetUpdateState(string.Format(L("v{0} was refused: {1} does not match its SHA-256.", "A v{0} foi recusada: o {1} nao bate com o SHA-256."), m.Version, f.Name));
                            return;
                        }
                    }
                }
                pending = m; pendingDir = dir;
                try { BeginInvoke(new Action(() => { Log(string.Format(L("Update v{0} downloaded and checked.", "Update v{0} descarregado e verificado."), m.Version)); RefreshUpdates(); })); } catch (Exception) { }
                TryInstall(false);
            }
            catch (Exception e) { SetUpdateState(L("Update check failed: ", "A verificacao falhou: ") + e.Message); }
            finally { Interlocked.Exchange(ref checking, 0); }
        }

        // Installs a checked update when the game is not running and no task is (or right away when asked).
        void TryInstall(bool now)
        {
            if (pending == null) return;
            try
            {
                BeginInvoke(new Action(() => {
                    if (pending == null) return;
                    bool auto = ReadSetting("autoupdate") != "0";
                    if (!now && !auto) { SetUpdateState(string.Format(L("v{0} is ready: press 'Install now'.", "A v{0} esta pronta: carrega em 'Instalar agora'."), pending.Version)); return; }
                    if (State() != GameState.None || busyNow || setupBusy)
                    {
                        SetUpdateState(string.Format(L("v{0} is ready: it installs when the game closes.", "A v{0} esta pronta: instala-se quando o jogo fechar."), pending.Version));
                        return;
                    }
                    Install();
                }));
            }
            catch (Exception) { }
        }

        // Any victory.exe at all (a game, or a tool another launcher window left): it holds the file an update replaces.
        static bool VictoryRunning()
        {
            try
            {
                var all = Process.GetProcessesByName("victory");
                foreach (var p in all) p.Dispose();
                return all.Length > 0;
            }
            catch (Exception) { return false; }
        }

        void Install()
        {
            var m = pending;
            // Twice at once (the button and the timer, or the message below holding the window open): the second does nothing.
            if (m == null || installing) return;
            installing = true;
            try { InstallNow(m); }
            finally { installing = false; }
        }

        void InstallNow(Manifest m)
        {
            bool launcher = false;
            try
            {
                if (busyNow || setupBusy)
                {
                    SetUpdateState(string.Format(L("v{0} is ready: it installs when the current task ends.", "A v{0} esta pronta: instala-se quando a tarefa atual acabar.", "v{0} est prête : elle s'installe à la fin de la tâche en cours."), m.Version));
                    return;
                }
                if (m.Files.Any(f => f.Name == "victory.exe" || f.Name.StartsWith("D3D12/")) && VictoryRunning())
                {
                    SetUpdateState(string.Format(L("v{0} is ready: it installs when VIctory is closed.", "A v{0} esta pronta: instala-se quando o VIctory fechar.", "v{0} est prête : elle s'installe quand VIctory est fermé."), m.Version));
                    return;
                }
                // The files as they were checked on download: nothing is copied unless every one still matches.
                foreach (var f in m.Files)
                {
                    var checkedFile = Path.Combine(pendingDir, f.Name.Replace('/', '\\'));
                    if (!File.Exists(checkedFile) || new FileInfo(checkedFile).Length != f.Size || Sha256(checkedFile) != f.Sha256)
                    {
                        SetUpdateState(string.Format(L("v{0} was refused: {1} does not match its SHA-256.", "A v{0} foi recusada: o {1} nao bate com o SHA-256."), m.Version, f.Name));
                        pending = null;
                        return;
                    }
                }
                // One transaction (28/11/2026): every file is first copied beside the one it replaces (".new"), then
                // each one in use is moved aside (".bak") and the new one takes its name. If any step fails, every
                // file gets its old version back, so a full disk or an interrupted copy never leaves a mix of two
                // versions; the update stays pending and is tried again.
                launcher = m.Files.Any(f => f.Name == "Julia's Launcher.exe");
                var others = m.Files.Where(f => f.Name != "Julia's Launcher.exe")
                    .Select(f => new { From = Path.Combine(pendingDir, f.Name.Replace('/', '\\')), To = Path.Combine(AppDir, f.Name.Replace('/', '\\')), f.Size })
                    .ToList();
                long need = m.Files.Sum(f => f.Size) + (64L << 20);
                try
                {
                    var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(AppDir)));
                    if (drive.AvailableFreeSpace < need)
                    {
                        SetUpdateState(string.Format(L("v{0} needs {1} MB free on {2} to install; it is tried again later.", "A v{0} precisa de {1} MB livres em {2} para instalar; tenta outra vez mais tarde.", "v{0} a besoin de {1} Mo libres sur {2} ; nouvel essai plus tard."), m.Version, need >> 20, drive.Name));
                        return;
                    }
                }
                catch (Exception) { } // a drive that cannot be asked: the copies below still check themselves
                var staged = new List<string>();
                var swapped = new List<string>();   // targets whose old file is now ".bak" (or that had none)
                var hadOld = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string me = Application.ExecutablePath, oldMe = Path.Combine(root, "Julia's Launcher.old.exe");
                bool meMoved = false;
                try
                {
                    foreach (var o in others)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(o.To));
                        foreach (var leftover in new[] { o.To + ".new", o.To + ".bak" }) if (File.Exists(leftover)) File.Delete(leftover);
                        File.Copy(o.From, o.To + ".new", true);
                        staged.Add(o.To);
                        if (new FileInfo(o.To + ".new").Length != o.Size) throw new IOException(Path.GetFileName(o.To) + ": " + L("copied short", "copia incompleta", "copie incomplète"));
                    }
                    foreach (var o in others)
                    {
                        if (File.Exists(o.To)) { File.Move(o.To, o.To + ".bak"); hadOld.Add(o.To); }
                        swapped.Add(o.To);
                        File.Move(o.To + ".new", o.To);
                    }
                    if (launcher)
                    {
                        // A running program may be renamed but not overwritten: the new one takes its name.
                        if (File.Exists(oldMe)) File.Delete(oldMe);
                        File.Move(me, oldMe);
                        meMoved = true;
                        File.Copy(Path.Combine(pendingDir, "Julia's Launcher.exe"), me, true);
                    }
                }
                catch (Exception)
                {
                    // Everything back as it was, the launcher first: never leave the folder without its program.
                    if (meMoved)
                    {
                        try { if (File.Exists(me)) File.Delete(me); } catch (Exception) { }
                        try { if (!File.Exists(me)) File.Move(oldMe, me); } catch (Exception) { }
                    }
                    var lost = new List<string>();
                    foreach (var to in swapped)
                    {
                        try
                        {
                            if (File.Exists(to)) File.Delete(to);
                            if (hadOld.Contains(to)) File.Move(to + ".bak", to);
                        }
                        catch (Exception) { lost.Add(Path.GetFileName(to)); }
                    }
                    foreach (var to in staged) try { if (File.Exists(to + ".new")) File.Delete(to + ".new"); } catch (Exception) { }
                    Log(lost.Count == 0
                        ? string.Format(L("Update v{0} could not be installed; the previous files were put back.", "O update v{0} nao se instalou; os ficheiros anteriores foram repostos.", "La mise à jour v{0} n'a pas pu s'installer ; les fichiers précédents sont remis."), m.Version)
                        : string.Format(L("Update v{0} could not be installed, and {1} could not be put back: reinstall with VI-Setup.", "O update v{0} nao se instalou, e {1} nao foi reposto: reinstala com o VI-Setup.", "La mise à jour v{0} n'a pas pu s'installer, et {1} n'a pas pu être remis : réinstalle avec VI-Setup."), m.Version, string.Join(", ", lost)));
                    throw;
                }
                foreach (var to in hadOld) try { File.Delete(to + ".bak"); } catch (Exception) { }
                Log(string.Format(L("Updated to v{0}.", "Atualizado para a v{0}."), m.Version));
                // Shaders made by an older translator are made again (the Shaders page shows it).
                if (m.Files.Any(f => f.Name == "victory.exe")) WriteSetting("shaders", "0");
                pending = null;
                if (launcher)
                {
                    MessageBox.Show(this, string.Format(L("Julia's Launcher was updated to v{0}. It opens again now.", "O Julia's Launcher foi atualizado para a v{0}. Volta a abrir agora."), m.Version), "Julia's Launcher");
                    using (var cur = Process.GetCurrentProcess())
                    using (Process.Start(Application.ExecutablePath, "--wait-pid " + cur.Id)) { }
                    Application.Exit();
                    return;
                }
                installedVersion = m.Version;
                SetUpdateState(string.Format(L("Updated to v{0}.", "Atualizado para a v{0}."), m.Version));
            }
            catch (Exception e)
            {
                SetUpdateState(L("Installing the update failed: ", "A instalacao do update falhou: ") + e.Message);
                try { File.AppendAllText(Program.ErrorFile, DateTime.Now + "  update: " + e + Environment.NewLine); } catch (Exception) { }
            }
        }

        // The manifest and signature of the newest release (any branch, pre-releases too) that has both;
        // false when the list cannot be read (no network, GitHub's limit of 60 asks an hour reached).
        static bool NewestSignedRelease(out string manifestUrl, out string signatureUrl)
        {
            manifestUrl = signatureUrl = null;
            try
            {
                string json;
                using (var web = new WebClient())
                {
                    web.Headers[HttpRequestHeader.UserAgent] = "Julias-Launcher";
                    web.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
                    json = Encoding.UTF8.GetString(web.DownloadData(Updater.ReleasesApi));
                }
                var list = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.DeserializeObject(json) as object[];
                if (list == null) return false;
                foreach (var item in list)
                {
                    var rel = item as Dictionary<string, object>;
                    if (rel == null || (rel.ContainsKey("draft") && rel["draft"] is bool && (bool)rel["draft"])) continue;
                    var assets = rel.ContainsKey("assets") ? rel["assets"] as object[] : null;
                    if (assets == null) continue;
                    string m = null, s = null;
                    foreach (var a in assets)
                    {
                        var asset = a as Dictionary<string, object>;
                        if (asset == null) continue;
                        var name = asset["name"] as string;
                        var url = asset["browser_download_url"] as string;
                        if (url == null || !url.StartsWith(Updater.DownloadPrefix)) continue;
                        if (name == "manifest.json") m = url;
                        if (name == "manifest.sig") s = url;
                    }
                    if (m != null && s != null) { manifestUrl = m; signatureUrl = s; return true; }
                }
            }
            catch (Exception) { }
            return false;
        }

        // Signed with one of the release keys (edupode's or felicienradio's, ReleaseKey.All).
        static bool SignedByEdupode(byte[] data, byte[] signature)
        {
            foreach (var key in ReleaseKey.All)
            {
                try
                {
                    using (var rsa = new RSACryptoServiceProvider())
                    {
                        rsa.FromXmlString(key);
                        if (rsa.VerifyData(data, "SHA256", signature)) return true;
                    }
                }
                catch (Exception) { }
            }
            return false;
        }

        // The emulator matches this launcher's version: when the launcher is not run from a source folder and its
        // own update left a checked victory.exe under updates\<this version>, a different victory.exe where updates
        // install it is replaced by that one (copied beside, swapped, put back if anything fails). An RX 6500 XT
        // had launcher 0.3.24 with 0.3.21's victory.exe (28/11/2026), so no engine fix since reached it.
        void RepairEngine()
        {
            new Thread(() => {
                try
                {
                    Thread.Sleep(5000);
                    if (UseAmdEngine) return;
                    var src = SourceDir;
                    if (src != null && (root + "\\").StartsWith(src.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) return;
                    var good = Path.Combine(Path.GetDirectoryName(dataRoot), "updates", Updater.Version, "victory.exe");
                    var target = Path.Combine(AppDir, "victory.exe");
                    if (!File.Exists(good)) return;
                    if (File.Exists(target) && new FileInfo(target).Length == new FileInfo(good).Length && Sha256(target) == Sha256(good)) return;
                    if (VictoryRunning() || busyNow || setupBusy) return; // the next opening tries again
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    var fresh = target + ".new";
                    var old = target + ".bak";
                    if (File.Exists(fresh)) File.Delete(fresh);
                    File.Copy(good, fresh, true);
                    if (new FileInfo(fresh).Length != new FileInfo(good).Length) { File.Delete(fresh); return; }
                    bool had = File.Exists(target);
                    if (had) { if (File.Exists(old)) File.Delete(old); File.Move(target, old); }
                    try { File.Move(fresh, target); }
                    catch (Exception)
                    {
                        if (had && !File.Exists(target)) File.Move(old, target);
                        throw;
                    }
                    if (had) try { File.Delete(old); } catch (Exception) { }
                    Log(string.Format(L("The emulator did not match this launcher (v{0}): the one its update brought is in place now.",
                        "O emulador nao correspondia a este launcher (v{0}): o que a atualizacao trouxe esta agora no lugar.",
                        "L'émulateur ne correspondait pas à ce lanceur (v{0}) : celui de sa mise à jour est maintenant en place."), Updater.Version));
                    Post(Refresh2);
                }
                catch (Exception e)
                {
                    try { File.AppendAllText(Program.ErrorFile, DateTime.Now + "  repair engine: " + e + Environment.NewLine); } catch (Exception) { }
                }
            }) { IsBackground = true }.Start();
        }

        static string Sha256(string file)
        {
            using (var sha = SHA256.Create())
            using (var f = File.OpenRead(file))
                return string.Concat(sha.ComputeHash(f).Select(b => b.ToString("x2")).ToArray());
        }

        // The list, held to what an update may bring: known names, the repository's own release
        // downloads, a SHA-256 for each.
        static Manifest ParseManifest(string json)
        {
            try
            {
                var o = new JavaScriptSerializer().DeserializeObject(json) as Dictionary<string, object>;
                if (o == null) return null;
                var m = new Manifest { Version = o["version"] as string ?? "" };
                object v;
                if (o.TryGetValue("notes", out v)) m.Notes = v as string ?? "";
                if (o.TryGetValue("notes_pt", out v)) m.NotesPt = v as string ?? "";
                foreach (var item in (object[])o["files"])
                {
                    var f = item as Dictionary<string, object>;
                    var u = new UpdateFile { Name = f["name"] as string, Url = f["url"] as string, Sha256 = (f["sha256"] as string ?? "").ToLowerInvariant(), Size = Convert.ToInt64(f["size"]) };
                    if (!Updater.Allowed.Contains(u.Name) || u.Url == null || !u.Url.StartsWith(Updater.DownloadPrefix) || u.Sha256.Length != 64) return null;
                    m.Files.Add(u);
                }
                return m.Version.Length > 0 ? m : null;
            }
            catch (Exception) { return null; }
        }

        void SetUpdateState(string text)
        {
            updateState = text;
            try { BeginInvoke(new Action(RefreshUpdates)); } catch (Exception) { }
        }

        // ---------------------------------------------------------------- Updates page
        Panel UpdatesPage()
        {
            var page = Page();
            const int X = 40, W = 900;
            int y = PageTop(page, L("Updates", "Atualizacoes", "Mises à jour"), L("Updates", "Atualizacoes", "Mises à jour"), L("stay current.", "sempre em dia.", "toujours à jour."), null);
            updVersion = new Label { Location = new Point(X, y), Size = new Size(W, 28), Font = Theme.Mono(12f), ForeColor = Theme.Text, BackColor = Theme.Back };
            page.Controls.Add(updVersion);
            y += 32;
            updState = new Label { Location = new Point(X, y), Size = new Size(W, 24), ForeColor = Theme.Cyan, BackColor = Theme.Back };
            page.Controls.Add(updState);
            y += 26;
            updLast = new Label { Location = new Point(X, y), Size = new Size(W, 22), ForeColor = Theme.Dim, BackColor = Theme.Back, Font = Theme.Mono(8.5f) };
            page.Controls.Add(updLast);
            y += 32;
            var check = new Pill { Main = true, Text = L("Check now", "Verificar agora"), Location = new Point(X, y), Size = new Size(160, 38) };
            check.Click += (s, e) => new Thread(() => CheckForUpdate(true)) { IsBackground = true }.Start();
            page.Controls.Add(check);
            installNow = new Pill { Text = L("Install now", "Instalar agora"), Location = new Point(X + 170, y), Size = new Size(160, 38), Enabled = false };
            installNow.Click += (s, e) => {
                if (State() != GameState.None) { MessageBox.Show(this, L("Close the game first.", "Fecha o jogo primeiro.")); return; }
                Install();
            };
            page.Controls.Add(installNow);
            y += 52;
            autoUpdateBox = new OptionCard { Text = L("Install updates on their own", "Instalar updates sozinho"), Line = L("While the game is not running; checked every 5 minutes", "Quando o jogo nao esta a correr; verificado de 5 em 5 minutos"),
                Location = new Point(X, y), Size = new Size(W, 58), Checked = ReadSetting("autoupdate") != "0" };
            autoUpdateBox.CheckedChanged += (s, e) => { WriteSetting("autoupdate", autoUpdateBox.Checked ? "1" : "0"); if (autoUpdateBox.Checked) TryInstall(false); };
            page.Controls.Add(autoUpdateBox);
            y += 70;
            page.Controls.Add(new Heading(L("What changed", "O que mudou")) { Location = new Point(X, y), Width = W });
            y += 30;
            updNotes = new TextBox { Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, ForeColor = Theme.Text, Font = Theme.Ui(9.5f), ScrollBars = ScrollBars.Vertical };
            Kit.Dark(updNotes, "DarkMode_Explorer");
            page.Controls.Add(FieldFor(updNotes, X, y, W, 220, true));
            y += 232;
            page.Controls.Add(new Label { Location = new Point(X, y), Size = new Size(W, 80), ForeColor = Theme.Dim, BackColor = Theme.Back, Font = Theme.Ui(9f),
                Text = L("Updates come only from github.com/felicienradio/cold-project-vi releases. Each one is signed with the project's release key; a file is installed only when the signature and its SHA-256 match. Anyone can check a release's files against the source code (README: 'Checking a release').",
                         "Os updates so vem das versoes de github.com/felicienradio/cold-project-vi. Cada um e assinado com a chave de releases do projeto; um ficheiro so e instalado se a assinatura e o SHA-256 baterem certo. Qualquer pessoa pode comparar os ficheiros de uma versao com o codigo (README: 'Verificar uma versao').") });
            return page;
        }

        void RefreshUpdates()
        {
            if (updVersion == null) return;
            updVersion.Text = "v" + Updater.Version;
            updState.Text = updateState;
            updLast.Text = lastCheck == DateTime.MinValue ? "" : L("Last check: ", "Ultima verificacao: ") + lastCheck.ToString("HH:mm:ss") + L("  ·  next in 5 minutes", "  ·  proxima daqui a 5 minutos");
            installNow.Enabled = pending != null;
            installNow.Invalidate();
            updNotes.Text = pending == null ? "" : (Pt && pending.NotesPt.Length > 0 ? pending.NotesPt : pending.Notes).Replace("\n", "\r\n");
            updateBadge.Text = pending != null ? "● " + string.Format(L("Update v{0} ready", "Update v{0} pronto"), pending.Version) : "";
            var top = updateBadge.Parent;
            if (top != null) { var w = top.Width; top.Width = w + 1; top.Width = w; }
        }
    }
}
