// The Setup page: everything a first run needs, checked and done for the player. Each step says
// whether it is done and has a button that does it; "Do everything for me" runs the missing ones
// in order. Steps that change Windows ask for administrator rights (the UAC prompt) or a
// confirmation first; nothing restarts the PC without a yes.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VIctoryLauncher
{
    // ---------------------------------------------------------------- shared drawing helpers of the Setup,
    // Shaders, Advanced Tweaks, saves and What's new pages (kept here: Widgets.cs belongs to another file).
    static class Kit
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)] static extern int SetWindowTheme(IntPtr hwnd, string app, string list);

        // Dark scroll bars and drop-downs where Windows has them (Windows 10 1809 and later); nothing otherwise.
        public static void Dark(Control c, string theme)
        {
            Action apply = () => { try { SetWindowTheme(c.Handle, theme, null); } catch (Exception) { } };
            if (c.IsHandleCreated) apply();
            c.HandleCreated += (s, e) => apply();
        }

        const TextFormatFlags Wrap = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.TextBoxControl;

        // The height a wrapped text needs at `width` (a few pixels of slack: a label never cuts its last line).
        public static int TextH(string text, Font f, int width)
        {
            return TextRenderer.MeasureText(text ?? "", f, new Size(Math.Max(20, width), 100000), Wrap).Height + 4;
        }

        public static int TextW(string text, Font f)
        {
            return TextRenderer.MeasureText(text ?? "", f, new Size(100000, 100), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
        }

        // A button wide enough for its label (Pill shrinks the font when it is not, but fitting is better).
        public static int PillW(string text, float pt, int min)
        {
            using (var f = new Font("Segoe UI", pt)) return Math.Max(min, TextW(text, f) + 40);
        }

        public static Label Text(string text, Font f, Color color, Color back, int x, int y, int w)
        {
            return new Label { Text = text, Font = f, ForeColor = color, BackColor = back, UseMnemonic = false, AutoSize = false, Location = new Point(x, y), Size = new Size(w, TextH(text, f, w)) };
        }

        // Pills in rows from x, at most maxW wide in all, a new row when one does not fit; returns the y below them.
        public static int Row(Control parent, int x, int y, int maxW, params Pill[] pills)
        {
            int cx = x, rowH = 0;
            foreach (var p in pills)
            {
                int w = Math.Max(p.Width, PillW(p.Text, p.Font.Size, 110));
                if (cx > x && cx + w > x + maxW) { cx = x; y += rowH + 10; rowH = 0; }
                p.Location = new Point(cx, y);
                p.Size = new Size(w, p.Height);
                parent.Controls.Add(p);
                cx += w + 10;
                rowH = Math.Max(rowH, p.Height);
            }
            return y + rowH;
        }

        // `text` cut with "..." so it fits `width` pixels in `f` (the loading bars draw their text without a limit).
        public static string Clip(string text, Font f, int width)
        {
            text = text ?? "";
            if (TextW(text, f) <= width) return text;
            int lo = 0, hi = text.Length;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (TextW(text.Substring(0, mid) + "...", f) <= width) lo = mid; else hi = mid - 1;
            }
            return text.Substring(0, lo).TrimEnd() + "...";
        }

        // A one-line text field on a rounded inset (a plain TextBox draws a stock square frame).
        public static RoundCard Field(Control parent, TextBox tb, int x, int y, int w) { return Field(parent, tb, x, y, w, 36); }

        public static RoundCard Field(Control parent, TextBox tb, int x, int y, int w, int h)
        {
            tb.BorderStyle = BorderStyle.None;
            var card = new RoundCard { Location = new Point(x, y), Size = new Size(w, h), BackColor = Theme.Back, Edge = Theme.Line, Radius = 10 };
            tb.BackColor = card.BackColor;
            tb.Width = w - 28;
            if (tb.Multiline) { tb.Size = new Size(w - 28, h - 20); tb.Location = new Point(14, 10); Dark(tb, "DarkMode_Explorer"); }
            else tb.Location = new Point(14, (card.Height - tb.Height) / 2);
            tb.GotFocus += (s, e) => { card.Edge = Theme.Cyan; card.Invalidate(); };
            tb.LostFocus += (s, e) => { card.Edge = Theme.Line; card.Invalidate(); };
            card.Click += (s, e) => tb.Focus();
            card.Controls.Add(tb);
            parent.Controls.Add(card);
            return card;
        }
    }

    // A rounded card: `BackColor` is its fill (controls on it clear with it), the corners show the parent's colour.
    class RoundCard : Panel
    {
        public Color Edge = Theme.Rule;
        public float Radius = 10;
        // Flat: no fill and no frame, only a hairline above and below (its parent's colour behind).
        public bool Flat;
        public RoundCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            Theme.Smooth(g);
            if (Flat)
            {
                using (var b = new SolidBrush(Theme.Rule)) { g.FillRectangle(b, 0, 0, Width, 1); g.FillRectangle(b, 0, Height - 1, Width, 1); }
                return;
            }
            Theme.Panel(g, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), BackColor, Edge, Radius);
        }
    }


    // The thin line the setup nodes sit on: a hairline, and a butter line that grows down it (eased) as the steps are done.
    class TimelineLine : Control
    {
        readonly Mix fill;
        public TimelineLine()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            fill = new Mix(this);
        }
        public void SetFill(float px) { fill.To(px, 520); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            using (var b = new SolidBrush(Theme.Line)) g.FillRectangle(b, 1, 0, 1, Height);
            if (!fill.Running && fill.V > Height) fill.Set(Height);
            float h = Math.Min(Height, fill.V);
            if (h > 0.5f) using (var b = new SolidBrush(Theme.Good)) g.FillRectangle(b, 0, 0, 3, h);
        }
    }
    // A status as a drawn mark: a round badge (done, to do, running, not needed, failed) or a small pill with a word.
    // Kind: 0 to do, 1 done, 2 running, 3 not needed, 4 checking, 5 failed.
    class StatusMark : Control
    {
        public int Kind = 4;
        public bool Round = true, Node, Flat;
        public string Number = "";
        readonly Mix fill, burst;
        bool pulseReg;
        float pulse = -1f;
        public StatusMark()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(30, 30);
            Font = Theme.UiBold(8f);
            fill = new Mix(this); burst = new Mix(this);
        }
        // A node: the disc fills from its middle when the step is done (with one ring sent out), and a running one pulses.
        public void Set(int kind, string text)
        {
            bool wasDone = Kind == 1;
            Kind = kind; Text = text;
            if (Node)
            {
                fill.To(kind == 1 ? 1f : 0f, 380);
                if (kind == 1 && !wasDone) { burst.Set(0f); burst.To(1f, 750); }
                if (kind == 2 && !pulseReg) { pulseReg = true; Anim.Register(this, false, Pulse); }
                else if (pulseReg) Anim.Poke(this);
            }
            Invalidate();
        }
        int Pulse(double t)
        {
            if (Kind != 2) { if (pulse >= 0f) { pulse = -1f; Invalidate(); } return 4000; }
            pulse = (float)(0.5 + 0.5 * Math.Sin(t * 2 * Math.PI / 1.5));
            Invalidate();
            return 66;
        }
        public static Color ColorOf(int kind) { return kind == 1 ? Theme.Good : kind == 0 ? Theme.Warn : kind == 2 ? Theme.Cyan : kind == 5 ? Theme.Bad : Theme.Dim; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            Theme.Smooth(g);
            var c = ColorOf(Kind);
            if (Node)
            {
                // A milestone node: the step's number in a ring (a filled butter disc when done).
                float nd = Math.Min(Width, Height) - 2;
                var nb = new RectangleF((Width - nd) / 2f, (Height - nd) / 2f, nd, nd);
                if (!fill.Running) fill.Set(Kind == 1 ? 1f : 0f);
                float fv = fill.V;
                if (Kind == 2 && pulse >= 0f)
                    using (var b = new SolidBrush(Color.FromArgb((int)(18 + 52 * pulse), c))) g.FillEllipse(b, nb);
                if (fv < 0.999f)
                {
                    using (var b = new SolidBrush(Kind == 2 && pulse >= 0f ? Fx.Lerp(Theme.Back, Theme.Plum, pulse * 0.6f) : Theme.Back)) g.FillEllipse(b, nb.X + 2, nb.Y + 2, nd - 4, nd - 4);
                    using (var p = new Pen(c, Kind == 2 ? 2f + 0.8f * Math.Max(0f, pulse) : 1.5f)) g.DrawEllipse(p, nb.X + 1, nb.Y + 1, nd - 2, nd - 2);
                }
                if (fv > 0.01f)
                {
                    float dd = nd * Fx.Out(fv);
                    using (var b = new SolidBrush(Theme.Good)) g.FillEllipse(b, nb.X + (nd - dd) / 2f, nb.Y + (nd - dd) / 2f, dd, dd);
                }
                if (burst.Running)
                {
                    float bk = burst.V, bd = nd * (0.8f + 0.2f * bk);
                    using (var p = new Pen(Fx.Alpha(Theme.Good, 1f - bk), 2.5f)) g.DrawEllipse(p, nb.X + (nd - bd) / 2f, nb.Y + (nd - bd) / 2f, bd, bd);
                }
                using (var f = Theme.UiBold(8f))
                using (var b = new SolidBrush(Fx.Lerp(c, Theme.OnAccent, fv)))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                    g.DrawString(Number ?? "", f, b, nb, sf);
                return;
            }
            if (Flat)
            {
                // Just the word, in the state's colour.
                using (var b = new SolidBrush(c))
                using (var f = Theme.UiBold(8.5f))
                using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
                    g.DrawString(Text ?? "", f, b, new RectangleF(0, 0, Width, Height), sf);
                return;
            }
            if (!Round)
            {
                var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
                Theme.Panel(g, r, Color.FromArgb(Kind == 1 || Kind == 2 ? 40 : 28, c), Color.FromArgb(Kind == 3 || Kind == 4 ? 70 : 130, c), r.Height / 2f);
                using (var b = new SolidBrush(c))
                using (var f = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
                    g.DrawString(Text ?? "", Font, b, r, f);
                return;
            }
            float d = Math.Min(Width, Height) - 2;
            var box = new RectangleF((Width - d) / 2f, (Height - d) / 2f, d, d);
            float cx = box.X + d / 2, cy = box.Y + d / 2, u = d / 30f;
            if (Kind == 1)
            {
                using (var b = new SolidBrush(c)) g.FillEllipse(b, box);
                using (var p = new Pen(Theme.OnAccent, Math.Max(1.6f, 2.4f * u)) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round, LineJoin = System.Drawing.Drawing2D.LineJoin.Round })
                    g.DrawLines(p, new[] { new PointF(cx - 6 * u, cy + 0.5f * u), new PointF(cx - 1.5f * u, cy + 5 * u), new PointF(cx + 6.5f * u, cy - 5 * u) });
                return;
            }
            using (var p = new Pen(c, Math.Max(1.4f, 1.8f * u))) g.DrawEllipse(p, box.X + 1, box.Y + 1, d - 2, d - 2);
            using (var p = new Pen(c, Math.Max(1.4f, 2.2f * u)) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round })
            using (var b = new SolidBrush(c))
            {
                if (Kind == 0) { g.DrawLine(p, cx, cy - 6 * u, cx, cy + 1 * u); g.FillEllipse(b, cx - 1.6f * u, cy + 4.5f * u, 3.2f * u, 3.2f * u); }
                else if (Kind == 5) { g.DrawLine(p, cx - 4.5f * u, cy - 4.5f * u, cx + 4.5f * u, cy + 4.5f * u); g.DrawLine(p, cx - 4.5f * u, cy + 4.5f * u, cx + 4.5f * u, cy - 4.5f * u); }
                else if (Kind == 2) { for (int i = -1; i <= 1; i++) g.FillEllipse(b, cx + i * 6 * u - 1.8f * u, cy - 1.8f * u, 3.6f * u, 3.6f * u); }
                else g.DrawLine(p, cx - 4 * u, cy, cx + 4 * u, cy);
            }
        }
    }

    // A drop-down in the theme: a rounded face, a chevron, themed list items (a stock ComboBox is grey or white).
    class Dropdown : ComboBox
    {
        bool hover;
        // The height of the closed face (what Windows gives an owner-drawn list: the item and its frame).
        public int Face { get { return ItemHeight + 6; } }
        public Dropdown()
        {
            DropDownStyle = ComboBoxStyle.DropDownList;
            FlatStyle = FlatStyle.Flat;
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 24;
            BackColor = Theme.Card;
            ForeColor = Theme.Text;
            Font = Theme.Ui(9.5f);
            Kit.Dark(this, "DarkMode_CFD");
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool face = (e.State & DrawItemState.ComboBoxEdit) != 0;
            bool sel = !face && (e.State & DrawItemState.Selected) != 0;
            using (var b = new SolidBrush(sel ? Theme.Selected : Theme.Card)) e.Graphics.FillRectangle(b, e.Bounds);
            var r = new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 14, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, GetItemText(Items[e.Index]), Font, r, sel ? Theme.Cyan : Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
        // Drawn over what Windows painted: the face with its rounded border and chevron.
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg != 0xF || Width < 30 || Height < 10) return;
            try
            {
                using (var g = Graphics.FromHwnd(Handle))
                {
                    g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
                    Theme.Smooth(g);
                    var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
                    Theme.Panel(g, r, Enabled ? (hover || DroppedDown ? Theme.CardHover : Theme.Card) : Theme.Back, DroppedDown ? Theme.Cyan : hover ? Theme.LineStrong : Theme.Line, 10);
                    var text = SelectedIndex >= 0 ? GetItemText(SelectedItem) : "";
                    TextRenderer.DrawText(g, text, Font, new Rectangle(12, 0, Width - 38, Height), Enabled ? Theme.Text : Theme.Dim, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                    float cx = Width - 18, cy = Height / 2f;
                    using (var p = new Pen(Enabled ? Theme.Cyan : Theme.Dim, 1.8f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round })
                        g.DrawLines(p, new[] { new PointF(cx - 4, cy - 2), new PointF(cx, cy + 2), new PointF(cx + 4, cy - 2) });
                }
            }
            catch (Exception) { }
        }
    }

    // A list in the theme (the backups): rounded selection, no stock frame.
    class ThemedList : ListBox
    {
        public ThemedList()
        {
            DrawMode = DrawMode.OwnerDrawFixed;
            ItemHeight = 32;
            BorderStyle = BorderStyle.None;
            BackColor = Theme.Card;
            ForeColor = Theme.Text;
            Font = Theme.Ui(9.5f);
            IntegralHeight = false;
            Kit.Dark(this, "DarkMode_Explorer");
        }
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            var g = e.Graphics;
            using (var b = new SolidBrush(BackColor)) g.FillRectangle(b, e.Bounds);
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            Theme.Smooth(g);
            if (sel) Theme.Panel(g, new RectangleF(e.Bounds.X + 2, e.Bounds.Y + 2, e.Bounds.Width - 5, e.Bounds.Height - 5), Theme.Selected, Theme.Selected, 8);
            TextRenderer.DrawText(g, GetItemText(Items[e.Index]), Font, new Rectangle(e.Bounds.X + 12, e.Bounds.Y, e.Bounds.Width - 20, e.Bounds.Height), sel ? Theme.Cyan : Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }

    partial class LauncherForm
    {
        // 1: done, 0: to do, -1: not known (or nothing to do here).
        class Step
        {
            public string Title, Why, FixText;
            public Func<int> Check;
            public Func<bool> Fix;        // runs on a worker thread; true if it did its part
            public bool Auto = true;      // part of "Do everything for me"
            public Func<bool> Applies;    // null: every PC; false: not for this one (another card maker)
            public StatusMark Badge, Chip;
            public int NodeY;
            public Label Text, Detail;
            public Pill Button;
            public int State = -2;       // -2: not checked yet; -1: not needed on this PC; -3: the check itself failed
            public string CheckError;    // why the last check failed (State -3)
        }

        readonly List<Step> steps = new List<Step>();
        LoadBar setupBar;
        Pill everything;
        volatile bool setupBusy;

        [DllImport("kernel32.dll")] static extern bool GetFirmwareType(out uint type);

        string CargoExe
        {
            get
            {
                return FoundCargo() ?? "cargo";
            }
        }

        static bool Uefi()
        {
            try { uint t; return GetFirmwareType(out t) && t == 2; } catch (Exception) { return false; }
        }

        // ---------------------------------------------------------------- page
        // A poster head, then the steps as a vertical milestone timeline: a thin line, numbered nodes (their
        // colour is the step's state), the step's title and a muted line, the action as a small ghost pill.
        Panel SetupPage()
        {
            var page = Page();
            const int X = 40, W = 900;

            var introText = L("Each step checks your PC. Press its button, or 'Do everything for me' to run the missing ones in order. Windows asks before anything that needs administrator rights.",
                              "Cada passo verifica o teu PC. Carrega no botao dele, ou em 'Fazer tudo por mim' para correr os que faltam por ordem. O Windows pergunta antes de tudo o que precisa de administrador.",
                              "Chaque étape vérifie ton PC. Appuie sur son bouton, ou sur 'Tout faire pour moi' pour lancer celles qui manquent dans l'ordre. Windows demande avant tout ce qui exige les droits administrateur.");
            var everyText = L("Do everything for me", "Fazer tudo por mim", "Tout faire pour moi") + "  →";
            everyW = Kit.PillW(everyText, 11f, 230);
            setupHead = Head(L("Setup", "Preparar", "Préparer"), L("Setup", "Preparar", "Préparer"), L("done for you.", "feito por ti.", "fait pour toi."), introText);
            setupHead.RightLabel = L("Progress", "Progresso", "Avancement");
            setupHead.RightValue = L("Checking your PC...", "A verificar o teu PC...", "Vérification de ton PC...");
            everything = new Pill { Main = true, Text = everyText, Font = new Font("Segoe UI Semibold", 11f), Size = new Size(everyW, 46) };
            everything.Click += (s, e) => DoEverything();
            setupHead.Controls.Add(everything);
            Action placeEvery = () => everything.Location = new Point(Math.Max(40, setupHead.Width - 40 - everyW), 84);
            setupHead.Resize += (s, e) => placeEvery();
            placeEvery();
            page.Controls.Add(setupHead);
            setupAnchor = setupHead;
            int y = setupHead.Height + 8;

            // The progress bar (hidden until a step runs; Progress() places it again from setupAnchor).
            setupBar = new LoadBar { Location = new Point(X, y), Size = new Size(W, 30), Visible = false };
            page.Controls.Add(setupBar);
            y += 44;

            DefineSteps();
            var stepFont = Theme.Ui(9f);
            var titleFont = Theme.UiBold(10.5f);
            // One button width for every step, from the longest label (so the column lines up).
            int bw = 110;
            foreach (var st in steps) bw = Math.Max(bw, Kit.PillW(st.FixText, 9f, 110));
            bw = Math.Min(bw, 190);
            int textX = X + 56, textW = W - 56 - bw - 28;
            // The status word is as wide as its longest.
            int chipTextW = 0;
            using (var chipFont = Theme.UiBold(8.5f))
                foreach (var word in new[] { L("Not needed", "Nao preciso", "Inutile"), L("Checking...", "A verificar...", "Vérification..."), L("Not done", "Nao feito", "Pas fait"), L("Running", "A correr", "En cours"), L("To do", "Por fazer", "À faire"), L("Done", "Feito", "Fait") })
                    chipTextW = Math.Max(chipTextW, Kit.TextW(word, chipFont));
            int firstY = y, number = 0, lastNodeY = y;
            foreach (var st in steps)
            {
                number++;
                // The title, then the status word right after it (at the end of the line when the title is long).
                int titleW = Kit.TextW(st.Title, titleFont) + 6;
                int chipW = chipTextW + 14;
                int titleLabelW = Math.Min(titleW, textW - chipW - 6);
                int titleH = Kit.TextH(st.Title, titleFont, titleLabelW);
                int detailH = Kit.TextH(st.Why, stepFont, textW);
                int rowH = Math.Max(62, 2 + Math.Max(titleH, 22) + 2 + detailH + 16);
                st.Badge = new StatusMark { Node = true, Number = number.ToString("00"), Location = new Point(X, y), Size = new Size(30, 30), Kind = 4 };
                st.Text = Kit.Text(st.Title, titleFont, Theme.Text, Theme.Back, textX, y + 2, titleLabelW);
                st.Chip = new StatusMark { Flat = true, Round = false, Kind = 4, Size = new Size(chipW, 22), Location = new Point(textX + titleLabelW + 6, y + 3), Text = L("Checking...", "A verificar...", "Vérification...") };
                st.Detail = Kit.Text(st.Why, stepFont, Theme.Muted, Theme.Back, textX, y + 2 + Math.Max(titleH, 22) + 2, textW);
                st.Button = new Pill { Text = st.FixText, Font = Theme.Ui(9f), Size = new Size(bw, 30) };
                st.Button.Location = new Point(X + W - bw, y);
                var me = st;
                st.Button.Click += (s, e) => RunSteps(new[] { me });
                page.Controls.Add(st.Badge); page.Controls.Add(st.Text); page.Controls.Add(st.Chip); page.Controls.Add(st.Detail); page.Controls.Add(st.Button);
                lastNodeY = y; st.NodeY = y;
                y += rowH;
            }
            // The thin line the nodes sit on, behind them.
            timelineFirstY = firstY;
            tline = new TimelineLine { Location = new Point(X + 14, firstY + 15), Size = new Size(3, Math.Max(1, lastNodeY - firstY)) };
            var line = tline;
            page.Controls.Add(line);
            line.SendToBack();
            ShowSummary();
            page.Controls.Add(new Label { Location = new Point(X, y + 8), Size = new Size(10, 14), BackColor = Theme.Back });
            return page;
        }

        int everyW = 236, timelineFirstY;
        TimelineLine tline;
        PageHead setupHead;
        // The head: always shown, so the progress bar is placed from it.
        Control setupAnchor;

        // "3 of 9 ready" over the steps this PC needs (on the head's right, under "Progress").
        void ShowSummary()
        {
            if (setupHead == null) return;
            // A step whose check failed counts as known and not ready: "Everything is ready" never rests on it.
            int known = steps.Count(s => s.State != -1 && s.State != -2), ready = steps.Count(s => s.State == 1);
            int notChecked = steps.Count(s => s.State == -3);
            setupHead.RightValue = known == 0
                ? L("Checking your PC...", "A verificar o teu PC...", "Vérification de ton PC...")
                : ready == known
                    ? L("Everything is ready.", "Esta tudo pronto.", "Tout est prêt.")
                    : string.Format(L("{0} of {1} ready", "{0} de {1} prontos", "{0} sur {1} prêts"), ready, known)
                        + (notChecked > 0 ? string.Format(L(", {0} could not be checked", ", {0} por verificar", ", {0} non vérifiés"), notChecked) : "");
            setupHead.RightColor = known > 0 && ready == known ? Theme.Good : Theme.Cyan;
            // The line down the nodes fills as far as the steps are done in a row (a step this PC does not need passes).
            if (tline != null)
            {
                int last = -1;
                for (int i = 0; i < steps.Count; i++) { int sv = steps[i].State; if (sv == 1 || sv == -1) last = i; else break; }
                tline.SetFill(last >= 0 ? steps[last].NodeY - timelineFirstY : 0);
            }
            setupHead.Invalidate();
        }

        void DefineSteps()
        {
            steps.Clear();
            steps.Add(new Step {
                Title = L("Rust (builds VIctory)", "Rust (compila o VIctory)"),
                Why = L("The compiler VIctory is written for. Installed with winget (rustup).", "O compilador do VIctory. Instalado com o winget (rustup)."),
                FixText = L("Install", "Instalar"),
                // Not needed with a ready-made emulator (a release or the installer put victory.exe here).
                Check = () => NothingToBuild ? -1 : HasCargo() ? 1 : 0,
                Fix = () => Winget("Rustlang.Rustup", null),
            });
            steps.Add(new Step {
                Title = L("Visual Studio Build Tools (C++)", "Visual Studio Build Tools (C++)"),
                Why = L("The C++ linker and Windows SDK Rust needs. A large download (2-6 GB); Windows asks for permission.", "O linker de C++ e o Windows SDK de que o Rust precisa. Download grande (2-6 GB); o Windows pede autorizacao."),
                FixText = L("Install", "Instalar"),
                Check = () => NothingToBuild ? -1 : HasCppTools() ? 1 : 0,
                Fix = () => Winget("Microsoft.VisualStudio.2022.BuildTools", "--quiet --wait --norestart --add Microsoft.VisualStudio.Workload.VCTools --includeRecommended"),
            });
            steps.Add(new Step {
                Title = L("Build VIctory", "Compilar o VIctory"),
                Why = L("Compiles the emulator from this folder (several minutes, once; again after an update).", "Compila o emulador a partir desta pasta (alguns minutos, uma vez; de novo depois de atualizar)."),
                FixText = L("Build", "Compilar"),
                Check = () => UseAmdEngine ? (OwnVictoryExe != null ? 1 : -1) : VictoryExe != null ? 1 : 0,
                Fix = BuildBlocking,
            });
            steps.Add(new Step {
                Title = L("AMD alternative engine (Belobug)", "Motor alternativo AMD (Belobug)"),
                Why = L("Only if you chose it in Hardware & ID: VIctory-Belobug-AMD, Belobug's fork for Radeon (GPL-3.0). Downloads its source and builds it once.",
                        "So se o escolheste em Hardware e ID: VIctory-Belobug-AMD, o fork do Belobug para Radeon (GPL-3.0). Descarrega o codigo e compila-o uma vez."),
                FixText = L("Build it", "Compilar"),
                Check = () => !UseAmdEngine ? -1 : VictoryExe != null ? 1 : 0,
                Fix = BuildAmdEngine,
            });
            steps.Add(new Step {
                Title = L("Windows Security: 'Access denied' when building", "Seguranca do Windows: 'Acesso negado' ao compilar"),
                Why = L("Only if a build from source ends with 'Access denied': Controlled folder access blocks freshly built programs. Opens Windows Security; allow victory.exe there yourself. The launcher never changes antivirus settings.",
                        "So se uma compilacao acabar com 'Acesso negado': o Acesso controlado a pastas bloqueia programas acabados de compilar. Abre a Seguranca do Windows; permite la o victory.exe tu mesmo. O launcher nunca muda definicoes do antivirus."),
                FixText = L("Open it", "Abrir"),
                Check = () => -1,
                Auto = false,
                Fix = () => { OpenUrl("windowsdefender://ransomwareprotection"); return false; },
            });
            steps.Add(new Step {
                Title = L("Your game folder", "A pasta do jogo"),
                Why = L("Looks for game_scarlett_master.exe (or another game_scarlett*.exe) in Downloads, Desktop, Documents, Games folders and the root of each drive.", "Procura o game_scarlett_master.exe (ou outro game_scarlett*.exe) em Transferencias, Ambiente de trabalho, Documentos, pastas Games e na raiz de cada disco."),
                FixText = L("Find it", "Encontrar"),
                Check = () => { string g = null; Invoke(new Action(() => g = GameExe)); return File.Exists(g) ? 1 : 0; },
                Fix = FindGame,
            });
            steps.Add(new Step {
                Title = L("Shaders prepared", "Shaders preparados"),
                Why = L("Translates the game's shaders and makes their pipelines once (about 5 minutes), so the game does not stop to do it.", "Traduz os shaders do jogo e prepara as pipelines uma vez (cerca de 5 minutos), para o jogo nao parar para isso."),
                FixText = L("Prepare", "Preparar"),
                Check = () => ShadersReady() ? 1 : 0,
                Fix = PrepareBlocking,
            });
            steps.Add(new Step {
                Title = L("Power: plugged in, best performance", "Energia: ligado a corrente, melhor desempenho"),
                Why = L("Sets Windows' power mode to 'Best performance'. A laptop on battery runs the CPU slower: plug it in (and use the maker's performance mode, e.g. PredatorSense).",
                        "Poe o modo de energia do Windows em 'Melhor desempenho'. Um portatil na bateria corre mais devagar: liga-o a corrente (e usa o modo de desempenho do fabricante, p. ex. PredatorSense)."),
                FixText = L("Set it", "Definir"),
                Check = () => BestPower() && SystemInformation.PowerStatus.PowerLineStatus != PowerLineStatus.Offline ? 1 : 0,
                Fix = () => { RunWait("powercfg", "/overlaysetactive ded574b5-45a0-4f42-8737-46345c09c238", false); return BestPower(); },
            });
            steps.Add(new Step {
                Title = L("Virtual memory managed by Windows", "Memoria virtual gerida pelo Windows"),
                Why = L("The game and VIctory commit 25+ GB; a fixed, small page file ends them. Windows asks for permission; it counts after a restart.",
                        "O jogo e o VIctory reservam 25+ GB; um ficheiro de paginacao fixo e pequeno fecha-os. O Windows pede autorizacao; conta depois de reiniciar."),
                FixText = L("Set it", "Definir"),
                Check = () => PagefileOk() ? 1 : 0,
                Fix = FixPagefile,
            });
            steps.Add(new Step {
                Title = L("Dedicated graphics card for VIctory", "Placa grafica dedicada para o VIctory"),
                Why = L("On a laptop with two graphics cards, Windows may give VIctory the slow built-in one. Sets 'High performance' for victory.exe (Windows > Graphics settings).",
                        "Num portatil com duas placas, o Windows pode dar ao VIctory a integrada, lenta. Poe 'Alto desempenho' para o victory.exe (Windows > Definicoes de graficos)."),
                FixText = L("Set it", "Definir"),
                Check = () => VictoryExe == null ? -1 : GpuPreferenceSet() || (probeAdapter.Length > 0 && GpuPreferenceOk() && !(hasNvidia && hasRadeon)) ? 1 : 0,
                Fix = () => { bool ok = SetGpuPreference(); ProbeGpu(); return ok; },
            });
            steps.Add(new Step {
                Title = "NVIDIA Smooth Motion",
                Why = L("Driver frame generation for RTX 40/50, written into the driver's profile for victory.exe (no NVIDIA App needed).", "Geracao de frames do driver para RTX 40/50, escrita no perfil do driver para o victory.exe (sem NVIDIA App)."),
                FixText = L("Turn on", "Ligar"),
                Check = () => !rtxFrameGen ? -1 : (SmoothMotion.Status() == "on" ? 1 : 0),
                Applies = () => rtxFrameGen,
                Fix = () => { var err = SmoothMotion.Set(true); smoothState = SmoothMotion.Status(); if (err != null) Log("Smooth Motion: " + err); BeginInvoke(new Action(() => { UpdateBanner(); UpdateSmooth(); })); return err == null; },
            });
            steps.Add(new Step {
                Title = "Resizable BAR",
                Why = L("Lets the CPU write straight into all of the video memory (faster copies). It is switched on in the PC's BIOS/UEFI: the guide shows how.",
                        "Deixa o processador escrever diretamente em toda a VRAM (copias mais rapidas). Liga-se na BIOS/UEFI do PC: o guia mostra como."),
                FixText = L("Show me how", "Mostra-me como"),
                Check = () => rebar ? 1 : RebarMissing() ? 0 : -1,
                Fix = () => { BeginInvoke(new Action(ShowRebarGuide)); return false; },
                Auto = false,
            });
            steps.Add(new Step {
                Title = L("Resizable BAR in the NVIDIA driver", "Resizable BAR no driver da NVIDIA"),
                Why = L("NVIDIA's driver uses Resizable BAR only for the games on its list: this puts victory.exe on it (rBAR Enable, Enable v2, Options in its profile).",
                        "O driver da NVIDIA so usa o Resizable BAR nos jogos da lista dele: isto poe o victory.exe nessa lista (rBAR Enable, Enable v2, Options no perfil)."),
                FixText = L("Turn on", "Ligar"),
                Check = () => !hasNvidia || !rebarBios ? -1 : SmoothMotion.RebarSet() ? 1 : 0,
                Applies = () => hasNvidia && rebarBios,
                Fix = () => {
                    var err = SmoothMotion.SetRebar(true);
                    // The driver keeps its Resizable BAR settings for administrators (-137, NVAPI_INVALID_USER_PRIVILEGE):
                    // without them "Turn on" failed and the step stayed red. Asked once more as administrator.
                    if (err != null && err.Contains("-137"))
                        err = Elevated(Application.ExecutablePath, "--rebar-profile") && SmoothMotion.RebarSet() ? null : L("Windows did not give administrator rights, or the driver refused it.", "O Windows nao deu permissoes de administrador, ou o driver recusou.", "Windows n'a pas donné les droits administrateur, ou le pilote a refusé.");
                    if (err != null) Log("rBAR: " + err); else Log(L("Resizable BAR is on in victory.exe's driver profile.", "Resizable BAR ligado no perfil do driver do victory.exe."));
                    return err == null;
                },
            });
        }

        // ---------------------------------------------------------------- running steps
        void CheckAll()
        {
            if (!IsHandleCreated || IsDisposed) return;
            if (setupBusy) return;
            new Thread(() => {
                foreach (var st in steps)
                {
                    var me = st;
                    int s = CheckStep(st);
                    Post(() => PaintStep(me, s));
                }
            }) { IsBackground = true }.Start();
        }

        // A step's check: its state, or -3 when the check itself threw (no access, WMI, a file that could not be read).
        // That used to be -1 and showed as "Not needed", so a PC whose check failed looked ready (28/11/2026).
        int CheckStep(Step st)
        {
            try { st.CheckError = null; return st.Check(); }
            catch (Exception e)
            {
                bool first = st.CheckError == null;
                st.CheckError = e.Message;
                if (first) Log(st.Title + ": " + L("could not be checked", "nao foi possivel verificar", "vérification impossible") + " (" + e.Message + ")");
                return -3;
            }
        }

        void PaintStep(Step st, int state)
        {
            PaintStep(st, state, false);
        }

        // A step's badge, status word and button: done / to do / failed (it was tried and still is not) / not needed / running.
        void PaintStep(Step st, int state, bool failed)
        {
            st.State = state;
            int kind = state == 1 ? 1 : state == 0 ? (failed ? 5 : 0) : state == -3 ? 5 : 3;
            string word = kind == 1 ? L("Done", "Feito", "Fait")
                : state == -3 ? L("Could not check", "Nao verificado", "Non vérifié")
                : kind == 5 ? L("Not done", "Nao feito", "Pas fait") : kind == 0 ? L("To do", "Por fazer", "À faire") : L("Not needed", "Nao preciso", "Inutile");
            st.Badge.Set(kind, word);
            st.Chip.Set(kind, word);
            // The step still to do gets the accent button; a finished one a quiet outline.
            st.Button.Main = false;
            st.Button.Enabled = !setupBusy;
            st.Button.Invalidate();
            ShowSummary();
        }

        // The step being worked on.
        void MarkRunning(Step st)
        {
            var word = L("Running", "A correr", "En cours");
            st.Badge.Set(2, word);
            st.Chip.Set(2, word);
        }

        void DoEverything()
        {
            // (Steps for another kind of PC are left out: on an AMD card "Do everything" tried NVIDIA's
            // Smooth Motion and rBAR profile and showed them as failed, 27/11/2026.)
            RunSteps(steps.Where(s => s.Auto && s.State != 1 && (s.Applies == null || s.Applies())).ToArray());
        }

        void RunSteps(Step[] which)
        {
            if (setupBusy || which.Length == 0) return;
            setupBusy = true;
            foreach (var st in steps) { st.Button.Enabled = false; st.Button.Invalidate(); }
            everything.Enabled = false; everything.Invalidate();
            new Thread(() => {
              // Whatever happens in a step, Setup stops being busy at the end: a step that threw past its own
              // catch left setupBusy on, and PLAY stayed grey until the launcher was opened again.
              try
              {
                int i = 0;
                foreach (var st in which)
                {
                    i++;
                    var me = st;
                    int now = CheckStep(st);
                    if (now == 1 && which.Length > 1) { Post(() => PaintStep(me, 1)); continue; }
                    if (ClosingNow()) break; // the window is closing: no further step starts
                    Progress(string.Format("{0} ({1}/{2})...", st.Title, i, which.Length), (float)(i - 1) / which.Length);
                    Post(() => MarkRunning(me));
                    bool ok;
                    try { ok = st.Fix(); } catch (Exception e) { ok = false; Log(st.Title + ": " + e.Message); }
                    now = CheckStep(st);
                    bool tried = ok;
                    Post(() => PaintStep(me, now, !tried));
                    Log((now == 1 ? "✓ " : ok ? "• " : "✗ ") + st.Title);
                    // Nothing after a build can work without it.
                    if (now == 0 && st.Fix == (Func<bool>)BuildBlocking && which.Length > 1) break;
                }
                Progress(L("Done.", "Feito."), 1f);
                Thread.Sleep(800);
              }
              catch (Exception e) { try { File.AppendAllText(Program.ErrorFile, DateTime.Now + "  setup: " + e + Environment.NewLine); } catch (Exception) { } }
              finally
              {
                setupBusy = false;
                Post(() => {
                    setupBar.Visible = false;
                    if (loadBar != null) loadBar.Visible = false;
                    everything.Enabled = true; everything.Invalidate();
                    foreach (var st in steps) { st.Button.Enabled = true; st.Button.Invalidate(); }
                    Refresh2();
                });
              }
            }) { IsBackground = true }.Start();
        }

        float setupProgress;

        // Both bars (Setup page and Play page) show the same work; the Shaders page mirrors them.
        void Progress(string text, float part)
        {
            if (InvokeRequired) { try { if (!IsDisposed) BeginInvoke(new Action<string, float>(Progress), text, part); } catch (Exception) { } return; }
            setupProgress = part;
            // A hidden control is not moved when its page scrolls: placed again from a control that is
            // always shown before it appears, or it lands on top of the first step after a scroll.
            if (setupBar != null && setupAnchor != null && !setupBar.Visible) setupBar.Top = setupAnchor.Top + setupAnchor.Height + 10;
            if (loadBar != null && loadBarAnchor != null && !loadBar.Visible) loadBar.Top = loadBarAnchor.Top + 42;
            foreach (var bar in new[] { setupBar, loadBar })
                if (bar != null) { bar.Visible = true; bar.Set(part, Kit.Clip(text, bar.Font, Math.Max(80, bar.Width - 70))); }
        }

        // The Play page's "Show activity" button, which the loading bar and the log sit under.
        Control loadBarAnchor;

        // ---------------------------------------------------------------- the steps' work
        bool Winget(string id, string overrideArgs)
        {
            if (string.IsNullOrEmpty(RunCapture("where", "winget")))
            {
                Log(L("winget is missing (App Installer from the Microsoft Store). Opening the download page.", "Falta o winget (App Installer da Microsoft Store). A abrir a pagina de download."));
                OpenUrl(id.StartsWith("Rust") ? "https://rustup.rs" : "https://visualstudio.microsoft.com/visual-cpp-build-tools/");
                return false;
            }
            var args = "install --id " + id + " -e --silent --accept-source-agreements --accept-package-agreements";
            if (overrideArgs != null) args += " --override \"" + overrideArgs + "\"";
            Log("winget " + args);
            return RunWait("winget", args, false) == 0;
        }

        static bool HasCppTools()
        {
            var vswhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer", "vswhere.exe");
            if (!File.Exists(vswhere)) return false;
            var s = RunCapture(vswhere, "-products * -prerelease -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath");
            return !string.IsNullOrEmpty(s.Trim());
        }

        // A ready-made emulator is here (a release, or the installer): nothing has to be compiled.
        // A ready-made emulator is here (a release, or the installer): nothing has to be compiled.
        // Rust and the C++ tools are asked for only while an engine still has to be compiled: the engine in use is
        // there already (a release's victory.exe, a build from source, or Belobug's AMD engine built once). Players
        // with both installed were asked again after an update (27/11/2026).
        bool NothingToBuild { get { return VictoryExe != null || (!UseAmdEngine && (File.Exists(Path.Combine(root, "victory.exe")) || File.Exists(Path.Combine(AppDir, "victory.exe")))); } }

        // cargo.exe where rustup puts it: %CARGO_HOME%in when set, else %USERPROFILE%\.cargoin, or anywhere on PATH.
        static string FoundCargo()
        {
            var home = Environment.GetEnvironmentVariable("CARGO_HOME");
            if (!string.IsNullOrEmpty(home) && File.Exists(Path.Combine(home, "bin", "cargo.exe"))) return Path.Combine(home, "bin", "cargo.exe");
            var mine = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cargo", "bin", "cargo.exe");
            if (File.Exists(mine)) return mine;
            return RunCapture("where", "cargo").Contains("cargo") ? "cargo" : null;
        }

        static bool HasCargo() { return FoundCargo() != null; }

        // Where this repository's source is: beside the launcher (or a folder above it), or the copy
        // the launcher downloaded. A launcher copied on its own to Downloads has none beside it
        // ("could not find Cargo.toml in ...\Downloads"); then the source is fetched first.
        string sourceSeen;

        string SourceDir
        {
            get
            {
                Func<string, bool> isSource = d => d != null && File.Exists(Path.Combine(d, "Cargo.toml")) && File.Exists(Path.Combine(d, ".victory-root"));
                for (var d = new DirectoryInfo(root); d != null; d = d.Parent)
                    if (isSource(d.FullName))
                    {
                        // Remembered, so a copy of the launcher elsewhere (Desktop, Downloads) finds it again.
                        if (sourceSeen != d.FullName) { sourceSeen = d.FullName; if (ReadSetting("source") != d.FullName) WriteSetting("source", d.FullName); }
                        return d.FullName;
                    }
                var known = ReadSetting("source");
                if (isSource(known)) return known;
                var mine = Path.Combine(Path.GetDirectoryName(dataRoot), "source", "cold-project-vi");
                return File.Exists(Path.Combine(mine, "Cargo.toml")) ? mine : null;
            }
        }

        // The edupode branch's source from GitHub's zip (no git needed) into %LOCALAPPDATA%\VIctory\source.
        bool EnsureSource()
        {
            if (SourceDir != null) return true;
            var to = Path.Combine(Path.GetDirectoryName(dataRoot), "source", "cold-project-vi");
            Log(L("The launcher is not inside the source folder: downloading the source (main branch)...", "O launcher nao esta dentro da pasta do codigo: a descarregar o codigo (ramo main)..."));
            return DownloadZipInto("https://github.com/felicienradio/cold-project-vi/archive/refs/heads/main.zip", to, L("the source", "o codigo"));
        }

        // A GitHub source zip (one folder inside) unpacked as `to`.
        bool DownloadZipInto(string url, string to, string what)
        {
            var zip = Path.Combine(Path.GetTempPath(), "vi-" + Guid.NewGuid().ToString("N") + ".zip");
            Progress(string.Format(L("Downloading {0}...", "A descarregar {0}..."), what), 0.02f);
            try
            {
                System.Net.ServicePointManager.SecurityProtocol |= (System.Net.SecurityProtocolType)3072;
                using (var web = new System.Net.WebClient()) { web.Headers["User-Agent"] = "Julias-Launcher"; web.DownloadFile(url, zip); }
                var tmp = to + ".new";
                if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
                System.IO.Compression.ZipFile.ExtractToDirectory(zip, tmp);
                var inner = Directory.GetDirectories(tmp).Single();
                if (Directory.Exists(to)) Directory.Delete(to, true);
                Directory.CreateDirectory(Path.GetDirectoryName(to));
                Directory.Move(inner, to);
                Directory.Delete(tmp, true);
                Log(string.Format(L("Downloaded {0} into {1}", "{0} descarregado para {1}"), what, to));
                return true;
            }
            catch (Exception e)
            {
                Log(string.Format(L("{0} could not be downloaded: {1}", "Nao deu para descarregar {0}: {1}"), what, e.Message));
                return false;
            }
            finally { try { File.Delete(zip); } catch (Exception) { } }
        }

        bool BuildBlocking()
        {
            if (!EnsureSource()) return false;
            return BuildIn(SourceDir, "VIctory", "build.log");
        }

        // Belobug's AMD fork at the commit the launcher knows, from GitHub's source zip (no git
        // needed), into engines\amd; then built like this repository.
        bool BuildAmdEngine()
        {
            var dir = AmdEngineDir;
            if (!File.Exists(Path.Combine(dir, "Cargo.toml")))
            {
                if (!DownloadZipInto(AmdRepo + "/archive/" + AmdCommit + ".zip", dir, L("Belobug's AMD engine", "o motor AMD do Belobug"))) return false;
                var example = Path.Combine(dir, "victory.example.toml");
                if (File.Exists(example) && !File.Exists(Path.Combine(dir, "victory.toml"))) File.Copy(example, Path.Combine(dir, "victory.toml"));
            }
            return BuildIn(dir, L("the AMD engine (Belobug)", "o motor AMD (Belobug)"), "build-amd.log");
        }

        bool BuildIn(string folder, string what, string logName)
        {
            var log = Path.Combine(dataRoot, "logs", logName);
            Directory.CreateDirectory(Path.GetDirectoryName(log));
            int crates = 0;
            var psi = new ProcessStartInfo(CargoExe, "build --locked --profile diag -p victory --bin victory --features diagnostics") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, WorkingDirectory = folder };
            var cargoBin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cargo", "bin");
            psi.EnvironmentVariables["PATH"] = cargoBin + ";" + Environment.GetEnvironmentVariable("PATH");
            // A storage folder on another drive (Storage.cs): the build goes there, not into the source folder.
            if (StorageRoot != null) psi.EnvironmentVariables["CARGO_TARGET_DIR"] = BuildTarget(folder);
            Log(string.Format(L("Building {0}... Log: ", "A compilar {0}... Log: "), what) + log);
            using (var writer = new StreamWriter(log, false))
            {
                Process p;
                try { p = Process.Start(psi); }
                catch (Exception e) { Log(L("Could not start cargo: ", "Nao deu para iniciar o cargo: ") + e.Message); return false; }
                DataReceivedEventHandler sink = (s, e) => {
                    if (e.Data == null) return;
                    lock (writer) writer.WriteLine(e.Data);
                    var m = Regex.Match(e.Data, @"^\s*Compiling (\S+)");
                    if (m.Success)
                    {
                        crates++;
                        Progress(string.Format(L("Building {2}: {0} ({1} parts)", "A compilar {2}: {0} ({1} partes)"), m.Groups[1].Value, crates, what), Math.Min(0.95f, crates / 330f));
                    }
                };
                p.OutputDataReceived += sink; p.ErrorDataReceived += sink;
                p.BeginOutputReadLine(); p.BeginErrorReadLine();
                p.WaitForExit();
                Log(p.ExitCode == 0 ? what + L(": built.", ": compilado.") : L("The build failed: see ", "A compilacao falhou: ve ") + log);
                BeginInvoke(new Action(() => { Refresh2(); ProbeGpu(); }));
                if (p.ExitCode == 0) EnsureAgility();
                if (p.ExitCode != 0)
                {
                    writer.Flush();
                    TellErrors(log, 0);
                    BeginInvoke(new Action(() => MessageBox.Show(this, L("The build failed. The reason is in the Activity box and in ", "A compilacao falhou. O motivo esta na caixa Atividade e em ") + log
                        + L("\r\n\r\nHelp > Send errors to Discord makes a package with it.", "\r\n\r\nAjuda > Enviar erros no Discord faz um pacote com isto."), "Julia's Launcher", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
                }
                return p.ExitCode == 0;
            }
        }

        bool PrepareBlocking()
        {
            string dir = null, game = null;
            Invoke(new Action(() => { SaveSettings(); dir = gameBox.Text.Trim(); game = GameExe; }));
            if (VictoryExe == null || !File.Exists(game)) { Log(L("Build VIctory and choose the game folder first.", "Compila o VIctory e escolhe a pasta do jogo primeiro.")); return false; }
            if (!RoomForCache()) return false;
            // Another preparation (left by a launcher window that closed) on the same cache: stopped first, if the player says so.
            bool go = true;
            Invoke(new Action(() => { go = ClearOtherPreparations(); }));
            if (!go) return false;
            var log = Path.Combine(dataRoot, "logs", "prepare.log");
            long mark = 0;
            try { if (File.Exists(log)) mark = new FileInfo(log).Length; } catch (Exception) { }
            Log(L("Translating the game's shaders and preparing pipelines (about 5 minutes)... Log: ", "A traduzir os shaders do jogo e a preparar pipelines (cerca de 5 minutos)... Log: ") + log);
            Progress(L("Translating the game's shaders (about 5 minutes)...", "A traduzir os shaders do jogo (cerca de 5 minutos)..."), 0.05f);
            WriteSetting("shaders", "0");
            Stage(0, 0f, L("Shaders: starting...", "Shaders: a comecar..."), true);
            Stage(1, 0f, L("Pipelines: after the shaders", "Pipelines: depois dos shaders"), true);
            // `progress <read> of <total> MB, <n> shaders, <s> s` (victory shaders): the game's files read so far.
            int translated = -1;
            var p = Start("shaders \"" + dir + "\"", log, line => {
                var done = Regex.Match(line, @"^(\d+) shaders in the cache");
                if (done.Success) translated = int.Parse(done.Groups[1].Value);
                var m = Regex.Match(line, @"^progress (\d+) of (\d+) MB, (\d+) shaders, (\d+) s");
                if (!m.Success) return;
                float part = float.Parse(m.Groups[1].Value) / Math.Max(1f, float.Parse(m.Groups[2].Value));
                var text = string.Format(L("Shaders: {0:N0} translated · {1:0}% of the game's files · {2} s", "Shaders: {0:N0} traduzidos · {1:0}% dos ficheiros do jogo · {2} s"),
                    int.Parse(m.Groups[3].Value), part * 100, m.Groups[4].Value);
                Stage(0, part, text, true);
                Progress(text, 0.05f + 0.55f * part);
            });
            p.WaitForExit();
            Stage(0, 1f, p.ExitCode == 0 ? L("Shaders: done", "Shaders: feito") : L("Shaders: failed (see the errors below)", "Shaders: falhou (ve os erros em baixo)"), true);
            if (p.ExitCode != 0)
            {
                Log(string.Format(L("Translating the shaders failed (code {0}).", "A traducao dos shaders falhou (codigo {0})."), p.ExitCode));
                TellErrors(log, mark);
                return false;
            }
            // A dump gives about 25,000 shaders; a dozen means the game's files were not read (a folder of other
            // files, or a damaged dump). It used to say "Shaders ready" anyway (a player's report, 27/11/2026).
            if (translated >= 0 && translated < 1000)
            {
                Stage(0, 1f, L("Shaders: failed (see the errors below)", "Shaders: falhou (ve os erros em baixo)"), false);
                Log(string.Format(L("Only {0:N0} shaders were translated (a complete dump gives about 25,000): this folder does not hold the game's files, or the dump is damaged.",
                    "So {0:N0} shaders foram traduzidos (um dump completo da cerca de 25 000): esta pasta nao tem os ficheiros do jogo, ou o dump esta estragado.",
                    "Seulement {0:N0} shaders ont été traduits (un dump complet en donne environ 25 000) : ce dossier ne contient pas les fichiers du jeu, ou le dump est abîmé."), translated));
                if (MissingDumpFiles(dir).Length > 0) Log(MissingDumpText(dir));
                else Log(L("Download or extract the dump again, into a folder of its own.", "Volta a descarregar ou extrair o dump, para uma pasta so dele.", "Télécharge ou extrais le dump à nouveau, dans un dossier à lui."));
                TellErrors(log, mark);
                return false;
            }
            if (ClosingNow()) return false; // the window is closing: the pipelines are not started
            Progress(L("Preparing pipelines...", "A preparar pipelines..."), 0.6f);
            var w = Start("warm --used", log, line => {
                var m = Regex.Match(line, @"^(\d+) of (\d+), (\d+) s");
                if (m.Success)
                {
                    int n = int.Parse(m.Groups[1].Value), of = int.Parse(m.Groups[2].Value);
                    Progress(string.Format(L("Preparing pipelines: {0} of {1}", "A preparar pipelines: {0} de {1}"), n, of), 0.6f + 0.4f * n / Math.Max(1, of));
                    Stage(1, (float)n / Math.Max(1, of), string.Format(L("Pipelines: {0:N0} of {1:N0} made · {2} s", "Pipelines: {0:N0} de {1:N0} feitas · {2} s"), n, of, m.Groups[3].Value), true);
                }
                var none = Regex.Match(line, @"; 0 to make");
                if (none.Success) Stage(1, 1f, L("Pipelines: all ready", "Pipelines: todas prontas"), true);
            });
            w.WaitForExit();
            Stage(1, 1f, L("Pipelines: done", "Pipelines: feito"), false);
            Stage(0, 1f, L("Shaders: done", "Shaders: feito"), false);
            if (w.ExitCode != 0)
            {
                // The shaders are there; pipelines are made again before each game (and after it).
                Log(string.Format(L("Preparing pipelines stopped (code {0}); the shaders are ready, PLAY makes the pipelines.", "A preparacao das pipelines parou (codigo {0}); os shaders estao prontos, o JOGAR faz as pipelines."), w.ExitCode));
                TellErrors(log, mark);
            }
            Log(L("Shaders ready. You can press PLAY.", "Shaders prontos. Podes carregar em JOGAR."));
            WriteSetting("shaders", "1");
            WriteSetting("shaders_driver", driverVersion);
            return true;
        }

        // The shader cache's drive needs room: a full disk leaves half a cache behind.
        bool RoomForCache()
        {
            const long need = 12L << 30;
            try
            {
                Directory.CreateDirectory(ActiveCacheDir);
                var drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(ActiveCacheDir)));
                if (drive.AvailableFreeSpace >= need) return true;
                Log(string.Format(L("Only {0:0.0} GB free on {1}: the shaders need about 12 GB. Free some space and try again.",
                    "So {0:0.0} GB livres em {1}: os shaders precisam de cerca de 12 GB. Liberta espaco e tenta de novo."), drive.AvailableFreeSpace / 1073741824.0, drive.Name));
                return false;
            }
            catch (Exception) { return true; }
        }

        // The lines of a log written since `mark` that say what went wrong (at most 8).
        void TellErrors(string log, long mark)
        {
            try
            {
                using (var f = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    f.Seek(Math.Min(mark, f.Length), SeekOrigin.Begin);
                    var lines = new StreamReader(f).ReadToEnd().Split('\n')
                        .Select(s => s.TrimEnd('\r'))
                        .Where(s => Regex.IsMatch(s, @"(?i)\b(error|panicked|failed|denied|not found|no space)\b"))
                        .Reverse().Take(8).Reverse().ToList();
                    foreach (var line in lines) Log("   " + (line.Length > 220 ? line.Substring(0, 220) + "..." : line));
                    if (lines.Count == 0) Log(L("   (no error lines; the whole log: ", "   (sem linhas de erro; o log todo: ") + log + ")");
                }
            }
            catch (Exception) { }
        }

        // Ready: a finished preparation (launcher.ini's shaders=1, written only when it ended well)
        // whose cache is still there, or a cache as large as one (made by another way of starting VIctory).
        bool ShadersReady()
        {
            // The game has ~25,000 shaders: a handful means the preparation saw almost nothing (a folder
            // with only the executable showed 12 as "ready", 27/11/2026). Counted folder by folder: one walk of
            // the whole tree ended at the first entry the game or the warm-up was writing or renaming, and the
            // step showed "not prepared" (and Fix it) on PCs with 25,000+ shaders.
            return CountUpTo(Path.Combine(ActiveCacheDir, "shaders"), 1000) >= 1000;
        }

        static int CountUpTo(string dir, int most)
        {
            int n = 0;
            var pending = new Stack<string>();
            pending.Push(dir);
            while (pending.Count > 0 && n < most)
            {
                var d = pending.Pop();
                try { foreach (var f in Directory.EnumerateFiles(d)) if (++n >= most) break; } catch (Exception) { }
                try { foreach (var s in Directory.EnumerateDirectories(d)) pending.Push(s); } catch (Exception) { }
            }
            return n;
        }

        bool FindGame()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var places = new List<string> {
                Path.Combine(home, "Downloads"), Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Path.Combine(home, "Games"),
            };
            foreach (var d in DriveInfo.GetDrives())
                if (d.DriveType == DriveType.Fixed && d.IsReady) { places.Add(d.RootDirectory.FullName); places.Add(Path.Combine(d.RootDirectory.FullName, "Games")); }
            Progress(L("Looking for game_scarlett*.exe...", "A procurar o game_scarlett*.exe..."), 0.3f);
            // A complete dump first (the game's files beside the executable), anywhere in these places; a lone
            // executable only when no complete dump is found (a player's Downloads held the .exe among other files).
            string loose = null;
            foreach (var place in places.Distinct())
            {
                var found = Search(place, 4, ref loose);
                if (found != null) return UseGameFolder(Path.GetDirectoryName(found));
            }
            if (loose != null)
            {
                UseGameFolder(Path.GetDirectoryName(loose));
                Log(MissingDumpText(Path.GetDirectoryName(loose)));
                return false;
            }
            Log(L("The game was not found: choose its folder with Browse on the Play page.", "O jogo nao foi encontrado: escolhe a pasta com Procurar na pagina Jogar."));
            return false;
        }

        bool UseGameFolder(string dir)
        {
            Invoke(new Action(() => { gameBox.Text = dir; SaveSettings(); }));
            Log(L("Game found: ", "Jogo encontrado: ") + dir);
            return true;
        }

        // The game's own files that every dump has beside game_scarlett_master.exe. Without them the folder holds
        // only the executable (or it sits among other files): the shaders do not prepare (12 of ~25,000) and the
        // game ends at once.
        static readonly string[] DumpFiles = { "shaders.rpf", "levels.rpf" };

        static string[] MissingDumpFiles(string dir)
        {
            try { return DumpFiles.Where(f => !File.Exists(Path.Combine(dir, f))).ToArray(); }
            catch (Exception) { return DumpFiles; }
        }

        static string MissingDumpText(string dir)
        {
            return string.Format(L("The game's files are not beside the executable in {0} ({1} missing). Move the whole dump (game_scarlett_master.exe with its .rpf files and its data and pack folders) into a folder of its own, for example C:\\Games\\GTA6, and choose that folder on the Play page.",
                "Os ficheiros do jogo nao estao ao lado do executavel em {0} (falta {1}). Move o dump inteiro (o game_scarlett_master.exe com os ficheiros .rpf e as pastas data e pack) para uma pasta so dele, por exemplo C:\\Games\\GTA6, e escolhe essa pasta na pagina Jogar.",
                "Les fichiers du jeu ne sont pas à côté de l'exécutable dans {0} ({1} manquant). Déplace tout le dump (game_scarlett_master.exe avec ses fichiers .rpf et ses dossiers data et pack) dans un dossier à lui, par exemple C:\\Games\\GTA6, et choisis ce dossier dans la page Jouer."),
                dir, string.Join(", ", MissingDumpFiles(dir)));
        }

        // The first folder under dir with a game_scarlett*.exe and the game's files beside it; loose is the first
        // executable seen without them.
        static string Search(string dir, int depth, ref string loose)
        {
            try
            {
                if (!Directory.Exists(dir)) return null;
                var f = Path.Combine(dir, "game_scarlett_master.exe");
                // Dumps with another build's name (game_scarlett_*.exe): the Play page lets the player pick.
                var other = GameExes(dir);
                var exe = File.Exists(f) ? f : other.Length > 0 ? Path.Combine(dir, other[0]) : null;
                if (exe != null)
                {
                    if (MissingDumpFiles(dir).Length == 0) return exe;
                    if (loose == null) loose = exe;
                }
                if (depth == 0) return null;
                foreach (var sub in Directory.EnumerateDirectories(dir))
                {
                    var name = Path.GetFileName(sub);
                    if (name.StartsWith("$") || name.Equals("Windows", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Program", StringComparison.OrdinalIgnoreCase) || name.Equals("AppData", StringComparison.OrdinalIgnoreCase)) continue;
                    var r = Search(sub, depth - 1, ref loose);
                    if (r != null) return r;
                }
            }
            catch (Exception) { }
            return null;
        }

        static bool BestPower()
        {
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes"))
                {
                    var v = k == null ? null : k.GetValue("ActiveOverlayAcPowerScheme") as string;
                    return v != null && v.Equals("ded574b5-45a0-4f42-8737-46345c09c238", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception) { return false; }
        }

        // Virtual memory the game can grow into: managed by Windows on all drives ("?:\pagefile.sys"),
        // system-managed size on a drive ("C:\pagefile.sys 0 0"), or fixed sizes of 32 GB and more.
        // (The old check only took the first: a system-managed C: showed as off.)
        public static bool PagefileOk()
        {
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"))
                {
                    var v = k == null ? null : k.GetValue("PagingFiles") as string[];
                    if (v == null) return false;
                    long most = 0;
                    foreach (var entry in v)
                    {
                        var parts = entry.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 0) continue;
                        if (parts[0].StartsWith("?:", StringComparison.Ordinal)) return true;
                        if (parts.Length == 1 || (parts.Length >= 3 && parts[1] == "0" && parts[2] == "0")) return true;
                        long max;
                        if (parts.Length >= 3 && long.TryParse(parts[2], out max)) most += max;
                    }
                    return most >= 32768;
                }
            }
            catch (Exception) { return false; }
        }

        // The script both the Setup step and the installer run as administrator: Windows manages the
        // page file on every drive. Set-CimInstance fails on some PCs ("Generic failure"), so the
        // registry value it stands for is written too.
        // Done by the launcher itself, run as administrator (`--pagefile`), with no PowerShell: an encoded
        // PowerShell command run hidden is what antivirus programs look for in malware, and Windows flagged
        // the launcher for it (27/11/2026). Set through WMI, and the registry value it stands for is written
        // too (WMI fails on some PCs with "Generic failure").
        public static bool SetManagedPagefile()
        {
            bool done = false;
            try
            {
                using (var s = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                    foreach (System.Management.ManagementObject o in s.Get())
                    {
                        o["AutomaticManagedPagefile"] = true;
                        o.Put();
                        done = true;
                    }
            }
            catch (Exception) { }
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true))
                    if (k != null) { k.SetValue("PagingFiles", new[] { @"?:\pagefile.sys" }, RegistryValueKind.MultiString); done = true; }
            }
            catch (Exception) { }
            return done;
        }

        bool FixPagefile()
        {
            bool ok = Elevated(Application.ExecutablePath, "--pagefile");
            if (ok && PagefileOk())
            {
                Log(L("Virtual memory is now managed by Windows. Restart Windows for it to count.", "A memoria virtual passou a ser gerida pelo Windows. Reinicia o Windows para contar."));
                return true;
            }
            Log(L("Virtual memory could not be set. By hand: Windows key > 'Advanced system settings' > Performance: Settings > Advanced > Virtual memory: Change > tick 'Automatically manage paging file size for all drives' > OK, then restart.",
                  "A memoria virtual nao ficou definida. A mao: tecla Windows > 'Definicoes avancadas do sistema' > Desempenho: Definicoes > Avancadas > Memoria virtual: Alterar > marca 'Gerir automaticamente o tamanho do ficheiro de paginacao para todas as unidades' > OK e reinicia."));
            try { Process.Start("SystemPropertiesPerformance.exe"); } catch (Exception) { }
            return false;
        }

        // Runs a program with administrator rights (the UAC prompt) and waits; false if refused or failed.
        bool Elevated(string file, string args)
        {
            try
            {
                var p = Process.Start(new ProcessStartInfo(file, args) { Verb = "runas", UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
                p.WaitForExit();
                return p.ExitCode == 0;
            }
            catch (Win32Exception) { Log(L("Administrator rights were not given.", "Nao foram dadas permissoes de administrador.")); return false; }
        }

        static int RunWait(string file, string args, bool window)
        {
            try
            {
                // taskkill and powercfg answer at once: 30 s, then they are stopped and the call says -1.
                // robocopy (a move of the caches) and winget (an install) take as long as they take.
                bool quick = file == "taskkill" || file == "powercfg";
                using (var p = Process.Start(new ProcessStartInfo(file, args) { UseShellExecute = false, CreateNoWindow = !window }))
                {
                    if (!p.WaitForExit(quick ? 30000 : Timeout.Infinite))
                    {
                        try { p.Kill(); } catch (Exception) { }
                        return -1;
                    }
                    return p.ExitCode;
                }
            }
            catch (Exception) { return -1; }
        }

        static void OpenUrl(string url)
        {
            try { Process.Start(url); } catch (Exception) { }
        }

        void WriteSetting(string key, string value)
        {
            try
            {
                lock (settingsLock)
                {
                    var lines = File.Exists(settingsFile) ? File.ReadAllLines(settingsFile).Where(l => !l.StartsWith(key + "=")).ToList() : new List<string>();
                    lines.Add(key + "=" + value);
                    Directory.CreateDirectory(Path.GetDirectoryName(settingsFile));
                    WriteLinesAtomic(settingsFile, lines.ToArray());
                }
            }
            catch (Exception) { }
        }

        // ---------------------------------------------------------------- Resizable BAR
        // Whether the card can have Resizable BAR at all: GTX 10/16 and RTX 20 cannot; early RTX 30
        // cards need NVIDIA's firmware update first.
        int RebarCardSupport()
        {
            // GTX of any series, RTX 2050-2080 (not the "RTX 2000 Ada" pro card), the TITANs before Ampere, Quadro
            // before RTX, MX laptop chips; Radeon RX 400/500 (Polaris), Vega and older.
            var n = probeAdapter.Length > 0 ? probeAdapter : gpuName;
            if (Regex.IsMatch(n, @"\bGTX\b|RTX\s*20[5-8]0|TITAN\s*(X|V|RTX)|\bMX\s*\d|Quadro\s+[KMP]\d|\bRX\s*[45][5-9]0\b|\bVega\b|\bR[79]\s", RegexOptions.IgnoreCase)) return 0;
            if (Regex.IsMatch(n, @"RTX\s*30\d\d", RegexOptions.IgnoreCase)) return 2;
            return 1;
        }

        // Resizable BAR as a warning ("Running without", the Fix it button): only when the player can switch it on.
        // Not on cards that cannot have it (a GTX 1650 was told to fix it on every start), not when the BIOS has it on
        // and only Windows' D3D12 does not offer it to games (Windows 10, older Windows 11: nothing to switch), and not
        // before anything was read (a Radeon showed "ReBAR Off" until `victory gpu` answered, then the warning went).
        bool RebarMissing()
        {
            if (rebar || rebarBios || ReadSetting("rebar_ack") == "1" || integratedOnly || RebarCardSupport() == 0) return false;
            // NVIDIA: nvidia-smi's BAR1 was read and says 256 MB, and D3D12 does not offer it either.
            if (hasNvidia && barRead) return rebarHeap != 1;
            // Other cards: D3D12 knows GPU upload heaps (a Windows new enough to say) and says no.
            return rebarHeap == 0 && probeOptions16;
        }

        // The menus by motherboard maker (from the board's own name in Windows).
        string[] RebarSteps(string maker)
        {
            var m = (maker ?? "").ToUpperInvariant();
            if (m.Contains("ASUS")) return new[] {
                L("ASUS: Advanced > PCI Subsystem Settings > Above 4G Decoding: Enabled, then Re-Size BAR Support: Auto/Enabled.", "ASUS: Advanced > PCI Subsystem Settings > Above 4G Decoding: Enabled, depois Re-Size BAR Support: Auto/Enabled."),
                L("Boot > CSM (Compatibility Support Module) > Launch CSM: Disabled.", "Boot > CSM (Compatibility Support Module) > Launch CSM: Disabled.") };
            if (m.Contains("MICRO-STAR") || m.Contains("MSI")) return new[] {
                L("MSI: Settings > Advanced > PCI Subsystem Settings > Above 4G memory/Crypto Currency mining: Enabled, then Re-Size BAR Support: Enabled", "MSI: Settings > Advanced > PCI Subsystem Settings > Above 4G memory/Crypto Currency mining: Enabled, depois Re-Size BAR Support: Enabled"),
                L("(newer boards: the 'Smart Access Memory / Re-Size BAR' button on the EZ Mode page). Turn CSM off under Boot.", "(placas recentes: o botao 'Smart Access Memory / Re-Size BAR' na pagina EZ Mode). Desliga o CSM em Boot.") };
            if (m.Contains("GIGABYTE")) return new[] {
                L("Gigabyte/AORUS: Settings > IO Ports > Above 4G Decoding: Enabled, then Re-Size BAR Support: Auto.", "Gigabyte/AORUS: Settings > IO Ports > Above 4G Decoding: Enabled, depois Re-Size BAR Support: Auto."),
                L("Boot > CSM Support: Disabled.", "Boot > CSM Support: Disabled.") };
            if (m.Contains("ASROCK")) return new[] {
                L("ASRock: Advanced > PCI Configuration > Above 4G Decoding: Enabled, then Re-Size BAR Support: Enabled.", "ASRock: Advanced > PCI Configuration > Above 4G Decoding: Enabled, depois Re-Size BAR Support: Enabled."),
                L("Boot > CSM (Compatibility Support Module): Disabled.", "Boot > CSM (Compatibility Support Module): Disabled.") };
            return new[] {
                L("Look for 'Above 4G Decoding' and 'Re-Size BAR' (or 'Resizable BAR', 'Smart Access Memory') under Advanced > PCI, and turn CSM off under Boot.",
                  "Procura 'Above 4G Decoding' e 'Re-Size BAR' (ou 'Resizable BAR', 'Smart Access Memory') em Advanced > PCI, e desliga o CSM em Boot."),
                L("Laptops (Acer, Lenovo, HP, Dell...): usually no switch; it comes with the maker's BIOS update for your model.", "Portateis (Acer, Lenovo, HP, Dell...): normalmente nao ha opcao; vem com a atualizacao de BIOS do fabricante para o teu modelo.") };
        }

        // A dialog in the theme (dark title bar, plum back, fixed size).
        Form Dialog(string title, int width, int height)
        {
            var f = new Form { Text = title, ClientSize = new Size(width, height), BackColor = Theme.Back, ForeColor = Theme.Text, Font = Theme.Ui(9.75f), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, ShowInTaskbar = false, Icon = Icon };
            f.HandleCreated += (s, e) => {
                int on = 1;
                try { if (DwmSetWindowAttribute(f.Handle, 20, ref on, 4) != 0) DwmSetWindowAttribute(f.Handle, 19, ref on, 4); } catch (Exception) { }
                int caption = Theme.Side.R | (Theme.Side.G << 8) | (Theme.Side.B << 16);
                try { DwmSetWindowAttribute(f.Handle, 35, ref caption, 4); } catch (Exception) { }
            };
            return f;
        }

        void ShowRebarGuide()
        {
            bool uefi = Uefi();
            string maker = hw != null ? hw.BoardMaker : "", board = hw != null ? hw.Board : "";
            if (hw == null)
                try { maker = Wmi("SELECT Manufacturer FROM Win32_BaseBoard", "Manufacturer"); board = Wmi("SELECT Product FROM Win32_BaseBoard", "Product"); } catch (Exception) { }
            int card = RebarCardSupport();
            var f = Dialog("Resizable BAR", 860, 720);
            f.Controls.Add(new Heading(L("Resizable BAR", "Resizable BAR", "Resizable BAR")) { Location = new Point(24, 18), Width = 810 });
            Func<bool, string, string> line = (ok, what) => (ok ? "✓  " : "✗  ") + what;
            var lines = new List<string> {
                line(rebar, rebarHeap == 1 ? L("The game gets it: D3D12 offers mapped video memory.", "O jogo tem-no: o D3D12 oferece memoria de video mapeada.")
                    : rebarHeap == 0 && rebarBios ? L("On in the BIOS and on the card, but Windows does not offer it to games (see 3).", "Ligado na BIOS e na placa, mas o Windows nao o oferece aos jogos (ve o 3).")
                    : L("The game does not get it.", "O jogo nao o tem.")),
                line(rebarBios || !hasNvidia, hasNvidia ? (rebarBios ? L("The card reports a large BAR (nvidia-smi: BAR1 of 1 GB or more).", "A placa indica um BAR grande (nvidia-smi: BAR1 de 1 GB ou mais).") : L("The card reports a 256 MB BAR: off in the BIOS, or the card's firmware lacks it.", "A placa indica um BAR de 256 MB: desligado na BIOS, ou o firmware da placa nao o tem.")) : L("Not an NVIDIA card: only the game's side is checked.", "Nao e uma placa NVIDIA: so o lado do jogo e verificado.")),
                line(uefi, uefi ? L("The PC starts in UEFI mode (needed).", "O PC arranca em modo UEFI (necessario).") : L("The PC starts in Legacy/CSM mode: Resizable BAR needs UEFI (convert the disk with mbr2gpt first).", "O PC arranca em Legacy/CSM: o Resizable BAR precisa de UEFI (converte o disco com o mbr2gpt primeiro).")),
                line(card != 0, card == 0 ? string.Format(L("{0} cannot use Resizable BAR (only RTX 30 and newer, RX 5000 and newer).", "A {0} nao suporta Resizable BAR (so RTX 30 ou mais recente, RX 5000 ou mais recente)."), gpuName)
                    : card == 2 ? string.Format(L("{0}: early RTX 30 cards need NVIDIA's Resizable BAR firmware update (see 2).", "{0}: as primeiras RTX 30 precisam da atualizacao de firmware Resizable BAR da NVIDIA (ve o 2)."), gpuName)
                    : string.Format(L("{0} supports it.", "A {0} suporta-o."), gpuName)),
            };
            // (the status lines and the instructions are drawn below, once the steps are built)

            var steps = new List<string>();
            steps.Add(L("1.  Motherboard: ", "1.  Placa-mae: ") + (maker + " " + board).Trim() + L(". Update its BIOS first, then in the BIOS:", ". Atualiza a BIOS primeiro, depois na BIOS:"));
            foreach (var s in RebarSteps(maker)) steps.Add("       " + s);
            steps.Add(L("       Save and exit (usually F10). Back in Windows, press 'Check again'.", "       Guarda e sai (normalmente F10). De volta ao Windows, carrega em 'Verificar de novo'."));
            steps.Add(L("2.  Graphics card: update the driver. RTX 3060/3060 Ti/3070/3080/3090 bought before April 2021 may need NVIDIA's", "2.  Placa grafica: atualiza o driver. RTX 3060/3060 Ti/3070/3080/3090 compradas antes de abril de 2021 podem precisar da"));
            steps.Add(L("       Resizable BAR firmware update (button below; from your card maker's site for some models).", "       atualizacao de firmware Resizable BAR da NVIDIA (botao em baixo; no site do fabricante da placa para alguns modelos)."));
            steps.Add(L("3.  Windows: games get it through D3D12's GPU upload heaps: Windows 11 24H2 or newer and a recent driver.", "3.  Windows: os jogos tem-no pelos GPU upload heaps do D3D12: Windows 11 24H2 ou mais recente e um driver recente."));
            steps.Add(L("       On but not offered: Windows Update (to 24H2+) and the newest NVIDIA/AMD driver, then restart.", "       Ligado mas nao oferecido: Windows Update (para 24H2+) e o driver NVIDIA/AMD mais recente, depois reinicia."));
            steps.Add("");
            steps.Add(L("If you cannot turn it on (older card, laptop without the option, Windows 10):", "Se nao o conseguires ligar (placa antiga, portatil sem a opcao, Windows 10):"));
            steps.Add(L("   •  The game still works: the launcher leaves the direct video-memory writes off on its own.", "   •  O jogo funciona na mesma: o launcher deixa a escrita direta na VRAM desligada sozinho."));
            steps.Add(L("   •  Pick Modest PC or Ultra Low, keep 'Keep the most video memory' on, and close apps on the Performance page:", "   •  Escolhe PC modesto ou Ultra baixo, deixa 'Guardar o maximo de VRAM' ligado e fecha apps na pagina Desempenho:"));
            steps.Add(L("      without Resizable BAR the copies to the card are slower, so less to copy helps most.", "      sem Resizable BAR as copias para a placa sao mais lentas, por isso copiar menos e o que mais ajuda."));
            steps.Add(L("   •  'I can't turn it on' stops the launcher warning you about it.", "   •  'Nao o consigo ligar' faz o launcher deixar de te avisar."));
            // Both cards in one scrolling area between the heading and the buttons: nothing is cut, whatever the language.
            int bodyW = f.ClientSize.Width - 48;
            var body = new Panel { Location = new Point(24, 58), Size = new Size(bodyW, f.ClientSize.Height - 58 - 76), AutoScroll = true, BackColor = Theme.Back };
            Kit.Dark(body, "DarkMode_Explorer");
            f.Controls.Add(body);
            int cardW = bodyW - SystemInformation.VerticalScrollBarWidth - 6, innerW = cardW - 40;
            var statusCard = new RoundCard { Location = new Point(0, 0), Width = cardW };
            int sy = 16;
            var rowFont = Theme.Ui(9.5f);
            foreach (var ln in lines)
            {
                bool good = ln.StartsWith("✓");
                var what = ln.Substring(Math.Min(3, ln.Length));
                var mark = new StatusMark { Location = new Point(16, sy), Size = new Size(22, 22), Kind = good ? 1 : 0 };
                var lb = Kit.Text(what, rowFont, good ? Theme.Soft : Theme.Warn, Theme.Card, 48, sy + 1, cardW - 48 - 16);
                statusCard.Controls.Add(mark); statusCard.Controls.Add(lb);
                sy += Math.Max(26, lb.Height + 2) + 6;
            }
            statusCard.Height = sy + 10;
            body.Controls.Add(statusCard);
            var helpCard = new RoundCard { Location = new Point(0, statusCard.Height + 12), Width = cardW };
            int hy = 16;
            foreach (var ln in steps)
            {
                if (ln.Trim().Length == 0) { hy += 10; continue; }
                int indent = ln.Length - ln.TrimStart().Length;
                var t = ln.TrimStart();
                bool head = indent == 0;
                int dx = head ? 0 : indent >= 6 ? 26 : 12;
                var lb = Kit.Text(t, head ? Theme.UiBold(9.75f) : Theme.Ui(9.5f), head ? Theme.Text : Theme.Soft, Theme.Card, 20 + dx, hy, innerW - dx);
                helpCard.Controls.Add(lb);
                hy += lb.Height + (head ? 2 : 0);
            }
            helpCard.Height = hy + 14;
            body.Controls.Add(helpCard);
            body.Controls.Add(new Label { Location = new Point(0, helpCard.Bottom), Size = new Size(10, 8), BackColor = Theme.Back });

            // The buttons (only the ones that apply are made, so there is no gap).
            var bios = new Pill { Main = true, Text = L("Restart into the BIOS", "Reiniciar para a BIOS", "Redémarrer dans le BIOS"), Size = new Size(200, 40), Enabled = uefi && !rebar, Font = Theme.Ui(10f) };
            bios.Click += (s, e) => {
                var ask = L("Save your work first. The PC restarts in 5 seconds and opens its BIOS/UEFI settings. Restart now?", "Guarda o teu trabalho primeiro. O PC reinicia em 5 segundos e abre as definicoes da BIOS/UEFI. Reiniciar agora?", "Enregistre ton travail d'abord. Le PC redémarre dans 5 secondes et ouvre les réglages du BIOS/UEFI. Redémarrer maintenant ?");
                if (MessageBox.Show(f, ask, "Resizable BAR", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    new Thread(() => Elevated("shutdown", "/r /fw /t 5")) { IsBackground = true }.Start();
            };
            var buttons = new List<Pill> { bios };
            if (card == 2)
            {
                var vbios = new Pill { Text = L("NVIDIA firmware update", "Firmware da NVIDIA", "Firmware NVIDIA"), Size = new Size(180, 40) };
                vbios.Click += (s, e) => OpenUrl("https://www.nvidia.com/en-us/geforce/news/geforce-rtx-30-series-resizable-bar-support/");
                buttons.Add(vbios);
            }
            var again = new Pill { Text = L("Check again", "Verificar de novo", "Vérifier à nouveau"), Size = new Size(150, 40) };
            again.Click += (s, e) => { f.Close(); DetectSystem(); ProbeGpu(); CheckAll(); };
            buttons.Add(again);
            if (!rebar)
            {
                var cant = new Pill { Text = L("I can't turn it on", "Nao o consigo ligar", "Je ne peux pas l'activer"), Size = new Size(170, 40) };
                cant.Click += (s, e) => { WriteSetting("rebar_ack", "1"); Log(L("Resizable BAR: no more warnings. The game runs without it.", "Resizable BAR: sem mais avisos. O jogo corre sem ele.")); f.Close(); CheckAll(); Refresh2(); };
                buttons.Add(cant);
            }
            var bar = new Panel { Dock = DockStyle.Bottom, Height = 76, BackColor = Theme.Back };
            bar.Paint += (s, e) => { using (var pen = new Pen(Theme.Line)) e.Graphics.DrawLine(pen, 0, 0, bar.Width, 0); };
            f.Controls.Add(bar);
            Kit.Row(bar, 24, 18, f.ClientSize.Width - 48, buttons.ToArray());
            f.ShowDialog(this);
        }
    }

    // The installer's one administrator pass (installer\VI.iss runs `Julia's Launcher.exe --admin-setup
    // <install folder>`): the Windows settings the Setup page would ask for one by one.
    static class AdminTasks
    {
        public static int Run(string[] args)
        {
            var dir = args.SkipWhile(a => a != "--admin-setup").Skip(1).FirstOrDefault() ?? AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            var log = new List<string>();
            // Best performance power mode.
            try
            {
                var p = Process.Start(new ProcessStartInfo("powercfg", "/overlaysetactive ded574b5-45a0-4f42-8737-46345c09c238") { UseShellExecute = false, CreateNoWindow = true });
                p.WaitForExit(60000);
                log.Add("power: " + (p.ExitCode == 0 ? "ok" : "code " + p.ExitCode));
            }
            catch (Exception e) { log.Add("power: " + e.Message); }
            // Virtual memory managed by Windows (counts after a restart).
            if (!LauncherForm.PagefileOk()) log.Add("virtual memory: " + (LauncherForm.SetManagedPagefile() ? "ok" : "failed"));
            // (No Defender exception any more: the launcher never changes antivirus settings.)
            // The high-performance graphics card for victory.exe.
            try
            {
                using (var k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\DirectX\UserGpuPreferences"))
                    foreach (var exe in new[] { Path.Combine(dir, "victory.exe"), Path.Combine(dir, "engines", "amd", "target", "diag", "victory.exe") })
                        k.SetValue(exe, "GpuPreference=2;");
                log.Add("gpu preference: ok");
            }
            catch (Exception e) { log.Add("gpu preference: " + e.Message); }
            // Smooth Motion in the driver's profile (RTX 40/50; ignored elsewhere).
            try { var err = SmoothMotion.Set(true); log.Add("smooth motion: " + (err ?? "ok")); } catch (Exception e) { log.Add("smooth motion: " + e.Message); }
            // Resizable BAR in victory.exe's driver profile (counts only where the BIOS has it on).
            try { var err = SmoothMotion.SetRebar(true); log.Add("rbar profile: " + (err ?? "ok")); } catch (Exception e) { log.Add("rbar profile: " + e.Message); }
            try { File.WriteAllLines(Path.Combine(Path.GetTempPath(), "vi-admin-setup.txt"), log); } catch (Exception) { }
            return 0;
        }
    }
}
