// Julia's Launcher, Hardware & ID: the reference PC (the laptop the presets are measured on) and the players'
// PCs as GitHub publishes them every 6 hours (docs/hardware-stats.json, averages only: never a row or an ID,
// a group of fewer than 3 PCs counts as "other"), as a table and bar charts.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace VIctoryLauncher
{
    // Horizontal bars, one per line, in a card: a name, a rounded bar as long as its share (each line its own
    // colour from the theme's dots), the number, and a "you" tag on the line this PC belongs to.
    class BarChart : Control
    {
        public List<Tuple<string, double, string>> Items = new List<Tuple<string, double, string>>();
        public string Empty = "";
        public Func<string, bool> Mine;
        public static string YouText = "you";
        public BarChart()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = Theme.Ui(9f);
        }
        static readonly Color[] Colours = { Theme.HotPink, Theme.Peach, Theme.Cyan, Theme.Lilac };
        const int line = 28;
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            Theme.Smooth(g);
            Theme.Panel(g, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Theme.Card, Theme.Line, 14);
            if (Items.Count == 0)
            {
                // Loading ("...") or nothing to show: in the card's first line.
                using (var b = new SolidBrush(Theme.Dim))
                using (var f = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                    g.DrawString(Empty ?? "", Font, b, new RectangleF(16, 10, Math.Max(10, Width - 32), line), f);
                return;
            }
            double most = Math.Max(1e-9, Items.Max(i => i.Item2));
            // The columns follow the card's width: the names take up to 210 px, the numbers what they measure.
            float nameW = Math.Min(210, Width * 0.3f);
            float numW = 0;
            foreach (var it in Items) numW = Math.Max(numW, g.MeasureString(it.Item3 ?? "", Font).Width);
            float youW;
            using (var tf = Theme.UiBold(7.5f)) youW = (float)Math.Ceiling(g.MeasureString(YouText ?? "", tf).Width) + 8;
            numW = Math.Min(numW + 4 + youW + 6, Width * 0.4f);
            float full = Math.Max(20, Width - nameW - numW - 28);
            int y = 10;
            int k = 0;
            const int bh = line - 18;
            foreach (var it in Items)
            {
                bool mine = Mine != null && Mine(it.Item1);
                using (var b = new SolidBrush(mine ? Theme.Text : Theme.Soft))
                using (var nf = mine ? Theme.UiBold(9f) : Theme.Ui(9f))
                using (var f = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Center })
                    g.DrawString(it.Item1 ?? "", nf, b, new RectangleF(16, y, nameW - 22, line), f);
                float w = (float)(full * it.Item2 / most);
                // The track, then the bar over it (never shorter than a round end).
                using (var path = Theme.Round(new RectangleF(nameW, y + 9, full, bh), bh / 2f))
                using (var b = new SolidBrush(Theme.Quiet)) g.FillPath(b, path);
                using (var path = Theme.Round(new RectangleF(nameW, y + 9, Math.Min(full, Math.Max(bh, w)), bh), bh / 2f))
                using (var b = new SolidBrush(Colours[k % Colours.Length])) g.FillPath(b, path);
                float tx = nameW + full + 12;
                float textW = (float)Math.Ceiling(g.MeasureString(it.Item3 ?? "", Font).Width);
                using (var b = new SolidBrush(Theme.Dim))
                using (var f = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
                    g.DrawString(it.Item3 ?? "", Font, b, new RectangleF(tx, y, Math.Max(10, Math.Min(textW + 2, Width - tx - 8)), line), f);
                if (mine)
                    using (var tf = Theme.UiBold(7.5f))
                    {
                        float tagW = (float)Math.Ceiling(g.MeasureString(YouText ?? "", tf).Width) + 8;
                        var tag = new RectangleF(Math.Min(tx + textW + 4, Width - tagW - 12), y + 6, tagW, line - 12);
                        using (var path = Theme.Round(tag, tag.Height / 2f))
                        using (var b = new SolidBrush(Theme.Cyan)) g.FillPath(b, path);
                        using (var b = new SolidBrush(Theme.OnAccent))
                        using (var cf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                            g.DrawString(YouText ?? "", tf, b, tag, cf);
                    }
                y += line;
                k++;
            }
        }
        public int Wanted { get { return Math.Max(44, Items.Count * 28 + 20); } }
    }

    // Facts in a card, two columns: "Label : value" lines, the label muted, the value in full colour,
    // long values wrapped, a hairline between lines.
    class InfoCard : Control
    {
        // Shown while there are no lines yet (reading the hardware, say).
        public string Empty = "";
        public InfoCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        string[] Lines() { return (Text ?? "").Replace("\r", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries); }
        static void Split(string l, out string k, out string v)
        {
            int i = l.IndexOf(':');
            k = i > 0 ? l.Substring(0, i).Trim() : "";
            v = i > 0 ? l.Substring(i + 1).Trim() : l.Trim();
        }
        // The rows' heights and the labels' column for this width.
        List<float> MeasureRows(Graphics g, out float labelW)
        {
            var heights = new List<float>();
            labelW = 0;
            var lines = Lines();
            if (lines.Length == 0) return heights;
            using (var lf = Theme.Ui(9f))
            using (var vf = Theme.UiBold(9.5f))
            {
                foreach (var l in lines) { string k, v; Split(l, out k, out v); labelW = Math.Max(labelW, g.MeasureString(k, lf).Width); }
                labelW = Math.Min(labelW + 20, Width * 0.36f);
                foreach (var l in lines)
                {
                    string k, v; Split(l, out k, out v);
                    var size = g.MeasureString(v, vf, (int)Math.Max(60, Width - 32 - labelW));
                    heights.Add(Math.Max(30, size.Height + 12));
                }
            }
            return heights;
        }
        // The height the card needs at its width.
        public int Fit()
        {
            using (var bmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bmp))
            {
                float labelW;
                var rows = MeasureRows(g, out labelW);
                return rows.Count == 0 ? 52 : (int)Math.Ceiling(rows.Sum() + 16);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            Theme.Smooth(g);
            Theme.Panel(g, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Theme.Card, Theme.Line, 14);
            float labelW;
            var heights = MeasureRows(g, out labelW);
            var lines = Lines();
            if (lines.Length == 0)
            {
                using (var b = new SolidBrush(Theme.Dim))
                using (var f = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
                    g.DrawString(Empty ?? "", Font, b, new RectangleF(16, 0, Math.Max(10, Width - 32), Height), f);
                return;
            }
            float y = 8;
            using (var lf = Theme.Ui(9f))
            using (var vf = Theme.UiBold(9.5f))
            using (var lb = new SolidBrush(Theme.Dim))
            using (var vb = new SolidBrush(Theme.Text))
            using (var pen = new Pen(Theme.Line))
            using (var kf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Center })
            using (var wf = new StringFormat { LineAlignment = StringAlignment.Center })
                for (int n = 0; n < lines.Length; n++)
                {
                    string k, v; Split(lines[n], out k, out v);
                    float h = heights[n];
                    if (n > 0) g.DrawLine(pen, 16, (float)Math.Round(y) + 0.5f, Width - 16, (float)Math.Round(y) + 0.5f);
                    g.DrawString(k, lf, lb, new RectangleF(16, y, labelW - 8, h), kf);
                    g.DrawString(v, vf, vb, new RectangleF(16 + labelW, y, Width - 32 - labelW, h), wf);
                    y += h;
                }
        }
    }

    // A table drawn in the theme: a header row, one row per line, every row shown (no scroll bars),
    // the row this PC belongs to in the accent.
    class StatsTable : Control
    {
        public string[] Headers = new string[0];
        // Each column's part of the width.
        public float[] Parts = new float[0];
        public List<string[]> Rows = new List<string[]>();
        public int Highlight = -1;
        // Columns whose text sits at the right (numbers).
        public bool[] RightAlign = new bool[0];
        public string Empty = "";
        const int RowH = 30, HeadH = 34;
        public StatsTable()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = Theme.Ui(9.5f);
        }
        public int Wanted { get { return HeadH + Math.Max(1, Rows.Count) * RowH + 8; } }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            Theme.Smooth(g);
            var card = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            Theme.Panel(g, card, Theme.Card, Theme.Line, 14);
            float inner = Width - 32;
            var xs = new float[Parts.Length + 1];
            xs[0] = 16;
            float total = Math.Max(1e-6f, Parts.Sum());
            for (int i = 0; i < Parts.Length; i++) xs[i + 1] = xs[i] + inner * Parts[i] / total;
            using (var f = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Center })
            using (var far = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Far })
            {
                Func<int, StringFormat> fmt = i => i < RightAlign.Length && RightAlign[i] ? far : f;
                using (var hf = Theme.UiBold(8f))
                using (var b = new SolidBrush(Theme.Dim))
                    for (int i = 0; i < Headers.Length && i < Parts.Length; i++)
                        g.DrawString((Headers[i] ?? "").ToUpperInvariant(), hf, b, new RectangleF(xs[i], 0, xs[i + 1] - xs[i] - 8, HeadH), fmt(i));
                using (var p = new Pen(Theme.Line)) g.DrawLine(p, 14, HeadH + 0.5f, Width - 14, HeadH + 0.5f);
                if (Rows.Count == 0)
                {
                    using (var b = new SolidBrush(Theme.Dim)) g.DrawString(Empty ?? "", Font, b, new RectangleF(16, HeadH, inner, RowH), f);
                    return;
                }
                float y = HeadH + 2;
                for (int r = 0; r < Rows.Count; r++)
                {
                    bool mine = r == Highlight;
                    var rowRect = new RectangleF(8, y + 1, Width - 16, RowH - 2);
                    if (mine)
                        using (var path = Theme.Round(rowRect, 10))
                        using (var b = new SolidBrush(Theme.Selected)) g.FillPath(b, path);
                    else if (r % 2 == 1)
                        using (var path = Theme.Round(rowRect, 10))
                        using (var b = new SolidBrush(Theme.Stripe)) g.FillPath(b, path);
                    using (var font = mine ? Theme.UiBold(9.5f) : Theme.Ui(9.5f))
                        for (int i = 0; i < Rows[r].Length && i < Parts.Length; i++)
                            using (var b = new SolidBrush(mine ? Theme.Cyan : i == 0 ? Theme.Text : Theme.Soft))
                                g.DrawString(Rows[r][i] ?? "", font, b, new RectangleF(xs[i], y, xs[i + 1] - xs[i] - 8, RowH), fmt(i));
                    y += RowH;
                }
            }
        }
    }

    // A rounded card that holds one control (a read-only text box, say) so it sits in the theme
    // instead of in a stock Windows frame.
    class CardPanel : Panel
    {
        public CardPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Padding = new Padding(14, 10, 8, 10);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            Theme.Smooth(g);
            Theme.Panel(g, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Theme.Card, Theme.Line, 14);
        }
    }

    partial class LauncherForm
    {
        public const string StatsJsonUrl = "https://raw.githubusercontent.com/felicienradio/cold-project-vi/main/docs/hardware-stats.json";
        public const string StatsPageUrl = "https://github.com/felicienradio/cold-project-vi/blob/main/docs/HARDWARE-STATS.md";

        // A control grows to `height`; what is below it on its page moves down by the difference.
        static void GrowTo(Control c, int height)
        {
            int grow = height - c.Height;
            if (grow == 0 || c.Parent == null) return;
            int top = c.Top;
            c.Height = height;
            foreach (Control other in c.Parent.Controls)
                if (other != c && other.Top > top) other.Top += grow;
        }

        // A wrapping label takes the height its text needs at its width (at least what it has); what is below moves down.
        static void FitLabel(Label l)
        {
            var need = TextRenderer.MeasureText(l.Text ?? "", l.Font, new Size(l.Width, 0), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + 4;
            if (need > l.Height) GrowTo(l, need);
        }

        // The laptop every preset is measured on (edupode's), for players to compare their PC with.
        int ReferencePcSection(Panel page, int X, int y, int W)
        {
            page.Controls.Add(new Heading(L("Reference PC (the presets are measured on it)", "PC de referencia (as predefinicoes sao medidas nele)", "PC de référence (les préréglages sont mesurés dessus)")) { Location = new Point(X, y), Width = W });
            y += 32;
            var lines = new[] {
                L("Laptop:  Acer Predator Helios Neo 16 (PHN16-73), plugged in, performance mode", "Portatil:  Acer Predator Helios Neo 16 (PHN16-73), ligado a corrente, modo desempenho", "Portable :  Acer Predator Helios Neo 16 (PHN16-73), sur secteur, mode performance"),
                L("Graphics: NVIDIA GeForce RTX 5070 Ti Laptop GPU, 12 GB, Resizable BAR on", "Grafica:  NVIDIA GeForce RTX 5070 Ti Laptop GPU, 12 GB, Resizable BAR ligado", "Graphique : NVIDIA GeForce RTX 5070 Ti Laptop GPU, 12 Go, Resizable BAR activé"),
                L("CPU:      Intel Core Ultra 9 275HX (24 cores)", "CPU:      Intel Core Ultra 9 275HX (24 nucleos)", "CPU :      Intel Core Ultra 9 275HX (24 cœurs)"),
                L("Memory:   32 GB RAM, 1 TB NVMe SSD, Windows 11", "Memoria:  32 GB de RAM, SSD NVMe de 1 TB, Windows 11", "Mémoire :  32 Go de RAM, SSD NVMe de 1 To, Windows 11"),
                L("Recommended (720p → 1080p): about 22-26 FPS in the open world, 35+ in cutscenes, 6-8 GB of video memory, ~20 GB of RAM.",
                  "Recomendado (720p → 1080p): cerca de 22-26 FPS no mundo aberto, 35+ nas cutscenes, 6-8 GB de VRAM, ~20 GB de RAM.",
                  "Recommandé (720p → 1080p) : environ 22-26 FPS en monde ouvert, 35+ en cinématiques, 6-8 Go de VRAM, ~20 Go de RAM."),
            };
            var card = new InfoCard { Location = new Point(X, y), Size = new Size(W, 200), Text = string.Join("\r\n", lines) };
            card.Height = card.Fit();
            page.Controls.Add(card);
            return y + card.Height + 16;
        }

        // The players' PCs: a summary line, the graphics cards as a table, and charts. Filled when GitHub answers.
        int PlayersPcsSection(Panel page, int X, int y, int W)
        {
            page.Controls.Add(new Heading(L("Players' PCs (updated every 6 hours)", "PCs dos jogadores (atualizado de 6 em 6 horas)", "PC des joueurs (mis à jour toutes les 6 heures)")) { Location = new Point(X, y), Width = W });
            y += 32;
            var head = new Label { Location = new Point(X, y), Size = new Size(W - 170, 40), ForeColor = Theme.Dim, BackColor = Theme.Back, Font = Theme.Ui(9f),
                Text = L("Reading the averages from GitHub...", "A ler as medias do GitHub...", "Lecture des moyennes sur GitHub...") };
            page.Controls.Add(head);
            var open = new Pill { Text = L("Open on GitHub", "Abrir no GitHub", "Ouvrir sur GitHub"), Location = new Point(X + W - 160, y), Size = new Size(160, 32) };
            open.Click += (s, e) => OpenUrl(StatsPageUrl);
            page.Controls.Add(open);
            y += 46;
            BarChart.YouText = L("you", "tu", "toi");
            var table = new StatsTable { Location = new Point(X, y), Size = new Size(W, 72), Empty = "...",
                Headers = new[] { L("Graphics card", "Placa grafica", "Carte graphique"), L("PCs", "PCs", "PC"), L("Video memory", "VRAM", "VRAM"), L("FPS (median)", "FPS (mediana)", "FPS (médian)"), L("Resizable BAR for the game", "Resizable BAR para o jogo", "Resizable BAR pour le jeu") },
                Parts = new[] { 3.6f, 0.9f, 1.4f, 1.6f, 2.4f }, RightAlign = new[] { false, true, true, true, true } };
            page.Controls.Add(table);
            y += table.Height + 16;
            var charts = new List<Tuple<string, BarChart>>();
            foreach (var title in new[] {
                L("Graphics card makers", "Fabricantes de placas", "Fabricants de cartes"),
                L("RAM", "RAM", "RAM"),
                L("Windows", "Windows", "Windows"),
                L("How many have it", "Quantos tem", "Combien l'ont") })
            {
                var c = new BarChart { Location = new Point(X, y + 26), Size = new Size(W, 60), Empty = "..." };
                var h = new Label { Location = new Point(X, y), Size = new Size(W, 22), Text = title, ForeColor = Theme.Text, BackColor = Theme.Back, Font = Theme.UiBold(9.5f) };
                page.Controls.Add(h); page.Controls.Add(c);
                charts.Add(Tuple.Create(title, c));
                y += 26 + 60 + 16;
            }
            int bottom = y;
            new Thread(() => {
                Dictionary<string, object> s = null;
                // Three tries, a few seconds apart (a slow first connection, GitHub's cache).
                for (int attempt = 0; attempt < 3 && s == null; attempt++)
                {
                    if (attempt > 0) Thread.Sleep(4000);
                    try
                    {
                        ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
                        using (var web = new WebClient())
                        {
                            web.Headers[HttpRequestHeader.UserAgent] = "Julias-Launcher";
                            s = new JavaScriptSerializer().DeserializeObject(Encoding.UTF8.GetString(web.DownloadData(StatsJsonUrl))) as Dictionary<string, object>;
                        }
                    }
                    catch (Exception) { }
                }
                // This page is made while the window is still being made: an answer that came before the window had
                // a handle was dropped (BeginInvoke refused it) and the page said "Reading the averages from
                // GitHub..." for good (28/11/2026). It waits for the window now, up to a minute.
                for (int i = 0; i < 600 && !IsHandleCreated && !IsDisposed; i++) Thread.Sleep(100);
                Post(() => {
                    // An answer that is not what was expected shows the same "could not be read" state.
                    try { FillStats(s, head, table, charts); }
                    catch (Exception) { try { FillStats(null, head, table, charts); } catch (Exception) { } }
                });
            }) { IsBackground = true }.Start();
            return bottom;
        }

        static double Num(Dictionary<string, object> d, string k) { object v; return d != null && d.TryGetValue(k, out v) && v != null ? Convert.ToDouble(v) : 0; }
        static Dictionary<string, object> Obj(Dictionary<string, object> d, string k) { object v; return d != null && d.TryGetValue(k, out v) ? v as Dictionary<string, object> : null; }
        static IEnumerable<Dictionary<string, object>> Rows(Dictionary<string, object> d, string k)
        {
            object v;
            var list = d != null && d.TryGetValue(k, out v) ? v as object[] : null;
            return list == null ? Enumerable.Empty<Dictionary<string, object>>() : list.OfType<Dictionary<string, object>>();
        }

        // A card's name without its maker, lower case ("AMD Radeon RX 9070 XT" -> "radeon rx 9070 xt").
        static string CardKey(string name)
        {
            var n = (name ?? "").ToLowerInvariant();
            foreach (var cut in new[] { "amd ", "nvidia ", "geforce ", "intel(r) ", "intel " }) n = n.Replace(cut, "");
            return n.Replace("(tm)", "").Trim();
        }

        // This PC's graphics card maker as the statistics name them.
        string MyVendor()
        {
            var g = (gpuName ?? "").ToLowerInvariant();
            if (g.Contains("radeon") || g.Contains("amd")) return "amd";
            if (g.Contains("geforce") || g.Contains("rtx") || g.Contains("gtx") || g.Contains("nvidia")) return "nvidia";
            if (g.Contains("intel") || g.Contains("arc")) return "intel";
            return "";
        }

        void FillStats(Dictionary<string, object> s, Label head, StatsTable table, List<Tuple<string, BarChart>> charts)
        {
            if (s == null)
            {
                head.ForeColor = Theme.Warn;
                head.Text = L("GitHub could not be reached: the averages show when it can.", "Sem ligacao ao GitHub: as medias aparecem quando houver.", "GitHub injoignable : les moyennes s'afficheront plus tard.");
                FitLabel(head);
                var none = L("No data yet", "Sem dados ainda", "Pas encore de données");
                table.Empty = none; table.Invalidate();
                foreach (var c in charts) { c.Item2.Empty = none; c.Item2.Invalidate(); }
                return;
            }
            head.ForeColor = Theme.Dim;
            var avg = Obj(s, "average");
            var pct = Obj(s, "percent");
            object updated;
            DateTime when;
            var at = s.TryGetValue("updated", out updated) && DateTime.TryParse(updated as string, null, System.Globalization.DateTimeStyles.RoundtripKind, out when) ? when.ToLocalTime().ToString("g") : "?";
            double pcs = Num(s, "pcs");
            head.Text = string.Format(L("{0:N0} PCs share their hardware. On average: {1:0.#} GB of RAM, {2:0.#} GB of video memory, {3:0} CPU threads. Updated {4}.",
                                        "{0:N0} PCs partilham o hardware. Em media: {1:0.#} GB de RAM, {2:0.#} GB de VRAM, {3:0} threads de CPU. Atualizado a {4}.",
                                        "{0:N0} PC partagent leur matériel. En moyenne : {1:0.#} Go de RAM, {2:0.#} Go de VRAM, {3:0} threads CPU. Mis à jour le {4}."),
                pcs, Num(avg, "ram_gb"), Num(avg, "vram_gb"), Num(avg, "cpu_threads"), at);
            var fps = Obj(s, "fps");
            head.Text += Num(fps, "fps_median") > 0
                ? string.Format(L(" FPS measured on {0:N0} PCs: {1:0.#} median, {2:0.#} in the slowest 5 %.", " FPS medidos em {0:N0} PCs: {1:0.#} de mediana, {2:0.#} nos 5 % mais lentos.", " FPS mesurés sur {0:N0} PC : {1:0.#} médians, {2:0.#} dans les 5 % les plus lents."), Num(fps, "fps_pcs"), Num(fps, "fps_median"), Num(fps, "fps_low"))
                : L(" FPS: not enough measured games yet (launcher 0.3.21+, sharing on).", " FPS: ainda poucos jogos medidos (launcher 0.3.21+, partilha ligada).", " FPS : pas encore assez de parties mesurées (lanceur 0.3.21+, partage activé).");
            FitLabel(head);
            table.Empty = L("No data yet", "Sem dados ainda", "Pas encore de données");
            foreach (var c in charts) c.Item2.Empty = table.Empty;
            table.Rows.Clear();
            table.Highlight = -1;
            var mine = CardKey(gpuName);
            foreach (var r in Rows(s, "gpus"))
            {
                var name = r["name"] as string;
                if (name == "other") name = L("other (fewer than 3 PCs each)", "outras (menos de 3 PCs cada)", "autres (moins de 3 PC chacune)");
                else if (mine.Length > 0 && CardKey(name) == mine) table.Highlight = table.Rows.Count;
                table.Rows.Add(new[] { name, Num(r, "pcs").ToString("N0"),
                    r.ContainsKey("vram_gb") ? Num(r, "vram_gb").ToString("0") + " GB" : "",
                    // Measured by the players' launchers (0.3.21 and later), for cards with 3 PCs or more that played.
                    r.ContainsKey("fps_median") ? Num(r, "fps_median").ToString("0.#") + " (" + Num(r, "fps_pcs").ToString("0") + ")" : "–",
                    r.ContainsKey("rebar_percent") ? Num(r, "rebar_percent").ToString("0") + " %" : "" });
            }
            GrowTo(table, table.Wanted);
            table.Invalidate();
            Func<string, List<Tuple<string, double, string>>> share = key => Rows(s, key)
                .Select(r => Tuple.Create(key == "ram_gb" && (r["name"] as string) != "other" ? r["name"] + " GB" : r["name"] as string, Num(r, "pcs"), Num(r, "pcs").ToString("N0") + "  (" + (pcs > 0 ? Num(r, "pcs") / pcs * 100 : 0).ToString("0") + " %)"))
                .ToList();
            var sets = new List<List<Tuple<string, double, string>>> {
                share("gpu_vendors"),
                share("ram_gb"),
                share("windows"),
                new List<Tuple<string, double, string>> {
                    Tuple.Create(L("Resizable BAR for the game", "Resizable BAR para o jogo", "Resizable BAR pour le jeu"), Num(pct, "rebar_for_the_game"), Num(pct, "rebar_for_the_game").ToString("0") + " %"),
                    Tuple.Create(L("Resizable BAR in the BIOS (NVIDIA)", "Resizable BAR na BIOS (NVIDIA)", "Resizable BAR dans le BIOS (NVIDIA)"), Num(pct, "rebar_in_bios"), Num(pct, "rebar_in_bios").ToString("0") + " %"),
                    Tuple.Create(L("8 GB of video memory or less", "8 GB de VRAM ou menos", "8 Go de VRAM ou moins"), Num(pct, "vram_8_gb_or_less"), Num(pct, "vram_8_gb_or_less").ToString("0") + " %"),
                    Tuple.Create(L("16 GB of RAM or less", "16 GB de RAM ou menos", "16 Go de RAM ou moins"), Num(pct, "ram_16_gb_or_less"), Num(pct, "ram_16_gb_or_less").ToString("0") + " %"),
                    Tuple.Create(L("Laptops", "Portateis", "Portables"), Num(pct, "laptops"), Num(pct, "laptops").ToString("0") + " %"),
                    Tuple.Create(L("SSD", "SSD", "SSD"), Num(pct, "ssd"), Num(pct, "ssd").ToString("0") + " %"),
                },
            };
            // Which line of each chart this PC belongs to: its card's maker, its RAM, its Windows.
            var vendor = MyVendor();
            var ram = ((int)Math.Round(ramGb)).ToString() + " GB";
            var win = Environment.OSVersion.Version.Build >= 22000 ? "11" : "10";
            var mines = new Func<string, bool>[] {
                n => vendor.Length > 0 && (n ?? "").ToLowerInvariant().Contains(vendor),
                n => ramGb > 0 && n == ram,
                n => (n ?? "").Contains(win),
                null,
            };
            // Each chart takes the room its lines need; what is below moves down by the difference.
            for (int i = 0; i < charts.Count; i++)
            {
                var c = charts[i].Item2;
                c.Items = sets[i];
                c.Mine = i < mines.Length ? mines[i] : null;
                GrowTo(c, c.Wanted);
                c.Invalidate();
            }
        }
    }
}
