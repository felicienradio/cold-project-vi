// The launcher's own drawn controls, in the PayDrop style: a deep plum window, cards one
// shade up with a 1 px border, round corners, one butter-yellow accent.
// Plain GDI+, .NET Framework 4, C# 5.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace VIctoryLauncher
{
    static class Theme
    {
        // PayDrop palette: deep plum, a magenta glow, butter yellow as the one accent (the main button,
        // switches on), peach and hot pink beside it, white hairlines and orbit rings.
        public static Color Back = Color.FromArgb(36, 12, 32);
        public static Color Side = Color.FromArgb(28, 9, 25);
        public static Color Card = Color.FromArgb(46, 17, 41);
        public static Color CardHover = Color.FromArgb(58, 22, 52);
        public static Color Selected = Color.FromArgb(82, 26, 68);
        public static Color Line = Color.FromArgb(82, 55, 77);
        // The one accent (the names stay from the older themes: Pink, Cyan are the accent, Violet its pressed side).
        public static Color Pink = Color.FromArgb(255, 244, 163);
        public static Color Violet = Color.FromArgb(236, 220, 120);
        public static Color Cyan = Color.FromArgb(255, 244, 163);
        public static Color AccentHover = Color.FromArgb(255, 249, 200);
        // Text drawn on the accent.
        public static Color OnAccent = Color.FromArgb(42, 10, 36);
        public static Color Text = Color.FromArgb(255, 246, 252);
        public static Color Dim = Color.FromArgb(214, 186, 206);
        // Text between the two: descriptions that still need to read well.
        public static Color Soft = Color.FromArgb(236, 220, 231);
        public static Color Good = Color.FromArgb(255, 244, 163);
        public static Color Warn = Color.FromArgb(255, 150, 136);
        public static Color Bad = Color.FromArgb(255, 110, 140);
        // PayDrop's other colours: the hot pink and the peach of its milestone dots.
        public static Color HotPink = Color.FromArgb(255, 79, 184);
        public static Color Peach = Color.FromArgb(255, 178, 166);
        public static Color Plum = Color.FromArgb(106, 20, 80);
        // Switches: the track when off, hovered, disabled; a disabled knob.
        public static Color TrackOff = Color.FromArgb(96, 58, 90);
        public static Color TrackHover = Color.FromArgb(116, 72, 108);
        public static Color TrackDisabled = Color.FromArgb(62, 34, 57);
        public static Color KnobDisabled = Color.FromArgb(124, 98, 118);
        // A quiet fill (an unpicked pill, a segmented track) and a stronger hairline (hover, outlined buttons).
        public static Color Quiet = Color.FromArgb(74, 36, 66);
        public static Color LineStrong = Color.FromArgb(150, 112, 142);
        // The "Tested" badge.
        public static Color BadgeBack = Color.FromArgb(92, 28, 76);
        public static Color BadgeText = Color.FromArgb(255, 168, 222);
        // Hover fill of a side-bar button, a checked option's border, the lilac of the charts' fourth colour.
        public static Color NavHover = Color.FromArgb(44, 16, 40);
        public static Color CardOn = Color.FromArgb(150, 92, 134);
        public static Color Lilac = Color.FromArgb(206, 164, 255);
        // A faint white wash (chips, zebra rows), a switch knob, the soft shadow under it.
        public static Color Wash = Color.FromArgb(34, 255, 255, 255);
        public static Color Stripe = Color.FromArgb(14, 255, 255, 255);
        public static Color Knob = Color.White;
        public static Color Shadow = Color.FromArgb(46, 0, 0, 0);
        // The backdrop's glows and rings.
        public static Color GlowMagenta = Color.FromArgb(224, 64, 168);
        public static Color GlowPeach = Color.FromArgb(255, 170, 160);
        public static Color Ring = Color.FromArgb(150, 255, 255, 255);
        // Hairlines and quiet text of the editorial look: a rule on the plum page, a white hairline for the posters,
        // the thin outline of a ghost button, the muted lavender of a label's second line, an even fainter one.
        public static Color Rule = Color.FromArgb(66, 42, 61);
        public static Color HairWhite = Color.FromArgb(58, 255, 255, 255);
        public static Color Outline = Color.FromArgb(120, 255, 255, 255);
        public static Color Muted = Color.FromArgb(182, 152, 176);
        public static Color Faint = Color.FromArgb(142, 114, 136);
        // The base of overlays drawn over the page (hover washes, glass chips, rings): white on the dark
        // themes, black on the light one.
        public static Color Ink = Color.White;
        // A third glow of the page's backdrop.
        public static Color Glow2 = Color.FromArgb(246, 74, 196);
        // Whether the theme is dark (the window's title bar follows it), and whether the headline's second
        // run is the italic serif (Plum's editorial look) or the interface's own type.
        public static bool Dark = true, SerifAccent = true;
        public static string Current = "plum";

        // ---------------------------------------------------------------- themes
        // launcher.ini's theme=. The colours above are Plum's; Use sets the others before any window is made
        // (Program.Main), and the picker in the side bar restarts the launcher to change it.
        public static readonly string[] Names = { "plum", "midnight", "vice", "graphite", "daylight" };

        public static string Label(string name, int lang)
        {
            switch (name)
            {
                case "midnight": return new[] { "Midnight", "Meia-noite", "Minuit" }[lang];
                case "vice": return "Vice";
                case "graphite": return new[] { "Graphite", "Grafite", "Graphite" }[lang];
                case "daylight": return new[] { "Daylight", "Dia", "Jour" }[lang];
                default: return new[] { "Plum", "Ameixa", "Prune" }[lang];
            }
        }

        // The two colours the picker shows for a theme: its page and its accent.
        public static Color PreviewBack(string name)
        {
            switch (name)
            {
                case "midnight": return C(12, 17, 27);
                case "vice": return C(16, 10, 24);
                case "graphite": return C(20, 20, 22);
                case "daylight": return C(246, 247, 250);
                default: return C(36, 12, 32);
            }
        }
        public static Color PreviewAccent(string name)
        {
            switch (name)
            {
                case "midnight": return C(56, 189, 248);
                case "vice": return C(255, 92, 170);
                case "graphite": return C(255, 145, 60);
                case "daylight": return C(37, 99, 235);
                default: return C(255, 244, 163);
            }
        }

        static Color C(int r, int g, int b) { return Color.FromArgb(r, g, b); }
        static Color A(int a, int r, int g, int b) { return Color.FromArgb(a, r, g, b); }

        public static void Use(string name)
        {
            if (Array.IndexOf(Names, name) < 0) name = "plum";
            Current = name;
            switch (name)
            {
                case "midnight":
                    // Night navy, one clear cyan.
                    Set(C(12, 17, 27), C(9, 13, 21), C(19, 26, 39), C(26, 35, 52), C(20, 52, 74), C(37, 48, 66),
                        C(56, 189, 248), C(14, 145, 210), C(125, 211, 252), C(4, 20, 32),
                        C(232, 238, 246), C(148, 163, 184), C(203, 213, 225), C(74, 222, 128), C(251, 191, 36), C(248, 113, 113),
                        C(244, 114, 182), C(147, 197, 253), C(30, 58, 95),
                        C(51, 65, 85), C(71, 85, 105), C(30, 41, 59), C(100, 116, 139), C(30, 41, 59), C(100, 116, 139),
                        C(12, 64, 96), C(125, 211, 252), C(17, 24, 36), C(56, 130, 170), C(167, 139, 250),
                        C(14, 116, 190), C(99, 102, 241), C(56, 189, 248), C(30, 39, 54), C(148, 163, 184), C(100, 116, 139), true);
                    SerifAccent = false;
                    break;
                case "vice":
                    // A Vice City night: deep purple-black, hot pink, a teal second colour, a sunset glow.
                    Set(C(16, 10, 24), C(11, 7, 17), C(27, 18, 40), C(36, 24, 52), C(58, 22, 66), C(56, 40, 74),
                        C(255, 92, 170), C(230, 60, 145), C(255, 140, 200), C(28, 6, 20),
                        C(250, 242, 255), C(190, 170, 205), C(228, 214, 238), C(64, 232, 214), C(255, 184, 92), C(255, 96, 120),
                        C(255, 112, 96), C(64, 232, 214), C(70, 28, 96),
                        C(72, 52, 94), C(92, 68, 118), C(44, 30, 60), C(120, 100, 140), C(48, 32, 66), C(140, 110, 165),
                        C(70, 24, 72), C(255, 160, 210), C(24, 16, 36), C(180, 80, 150), C(64, 232, 214),
                        C(236, 52, 160), C(255, 130, 70), C(40, 200, 220), C(44, 30, 60), C(176, 154, 196), C(130, 110, 150), true);
                    SerifAccent = false;
                    break;
                case "graphite":
                    // Neutral charcoal with a warm orange.
                    Set(C(20, 20, 22), C(15, 15, 17), C(30, 30, 33), C(40, 40, 44), C(64, 42, 22), C(50, 50, 55),
                        C(255, 145, 60), C(232, 118, 36), C(255, 178, 110), C(32, 16, 4),
                        C(242, 242, 244), C(160, 160, 168), C(212, 212, 218), C(120, 220, 150), C(255, 196, 90), C(255, 108, 108),
                        C(255, 108, 96), C(255, 196, 140), C(60, 44, 32),
                        C(62, 62, 68), C(80, 80, 88), C(40, 40, 44), C(110, 110, 118), C(44, 44, 48), C(110, 110, 118),
                        C(70, 44, 20), C(255, 190, 130), C(26, 26, 29), C(180, 110, 50), C(170, 150, 255),
                        C(200, 90, 30), C(90, 90, 100), C(255, 140, 50), C(38, 38, 42), C(150, 150, 158), C(108, 108, 116), true);
                    SerifAccent = false;
                    break;
                case "daylight":
                    // Light: white cards on a soft grey page, one blue.
                    Set(C(246, 247, 250), C(236, 238, 243), C(255, 255, 255), C(241, 243, 247), C(225, 236, 255), C(218, 222, 230),
                        C(37, 99, 235), C(29, 78, 216), C(59, 130, 246), C(255, 255, 255),
                        C(17, 24, 39), C(100, 110, 125), C(55, 65, 81), C(22, 163, 74), C(217, 119, 6), C(220, 38, 38),
                        C(219, 39, 119), C(96, 165, 250), C(219, 234, 254),
                        C(203, 213, 225), C(176, 190, 210), C(226, 232, 240), C(148, 163, 184), C(233, 236, 242), C(148, 163, 184),
                        C(219, 234, 254), C(29, 78, 216), C(229, 232, 239), C(96, 165, 250), C(139, 92, 246),
                        C(147, 197, 253), C(196, 181, 253), C(191, 219, 254), C(226, 230, 236), C(107, 114, 128), C(156, 163, 175), false);
                    SerifAccent = false;
                    break;
                default:
                    break; // Plum: the colours as declared above.
            }
        }

        // Every colour of a theme at once, in the order the fields are declared above (the accent is Pink and
        // Cyan both; the washes and hairlines follow the theme's lightness).
        static void Set(Color back, Color side, Color card, Color cardHover, Color selected, Color line,
            Color accent, Color pressed, Color accentHover, Color onAccent,
            Color text, Color dim, Color soft, Color good, Color warn, Color bad,
            Color hotPink, Color peach, Color plum,
            Color trackOff, Color trackHover, Color trackDisabled, Color knobDisabled, Color quiet, Color lineStrong,
            Color badgeBack, Color badgeText, Color navHover, Color cardOn, Color lilac,
            Color glow1, Color glow2, Color glow3, Color rule, Color muted, Color faint, bool dark)
        {
            Back = back; Side = side; Card = card; CardHover = cardHover; Selected = selected; Line = line;
            Pink = accent; Cyan = accent; Violet = pressed; AccentHover = accentHover; OnAccent = onAccent;
            Text = text; Dim = dim; Soft = soft; Good = good; Warn = warn; Bad = bad;
            HotPink = hotPink; Peach = peach; Plum = plum;
            TrackOff = trackOff; TrackHover = trackHover; TrackDisabled = trackDisabled; KnobDisabled = knobDisabled; Quiet = quiet; LineStrong = lineStrong;
            BadgeBack = badgeBack; BadgeText = badgeText; NavHover = navHover; CardOn = cardOn; Lilac = lilac;
            GlowMagenta = glow1; GlowPeach = glow2; Glow2 = glow3; Rule = rule; Muted = muted; Faint = faint;
            Dark = dark;
            Ink = dark ? Color.White : Color.Black;
            Knob = Color.White;
            Wash = dark ? A(30, 255, 255, 255) : A(14, 0, 0, 0);
            Stripe = dark ? A(12, 255, 255, 255) : A(8, 0, 0, 0);
            Shadow = dark ? A(50, 0, 0, 0) : A(34, 0, 0, 0);
            Ring = dark ? A(110, 255, 255, 255) : A(60, 0, 0, 0);
            HairWhite = dark ? A(42, 255, 255, 255) : A(30, 0, 0, 0);
            Outline = dark ? A(110, 255, 255, 255) : A(70, 0, 0, 0);
        }

        // What a control paints first: the glow of the poster it sits on, or its parent's colour.
        public static void ClearBack(Control c, Graphics g)
        {
            var bd = c.Parent as IBackdrop;
            if (bd != null)
            {
                var st = g.Save();
                g.TranslateTransform(-c.Left, -c.Top);
                bd.PaintBackdrop(g, c.Bounds);
                g.Restore(st);
            }
            else g.Clear(c.Parent != null ? c.Parent.BackColor : Back);
        }

        // A soft elliptical glow: `alpha` at its centre, nothing at its edge.
        public static void Glow(Graphics g, float cx, float cy, float rx, float ry, Color c, int alpha)
        {
            using (var p = new GraphicsPath())
            {
                p.AddEllipse(cx - rx, cy - ry, rx * 2, ry * 2);
                using (var b = new PathGradientBrush(p) { CenterColor = Color.FromArgb(alpha, c), SurroundColors = new[] { Color.FromArgb(0, c) } })
                    g.FillPath(b, p);
            }
        }

        // The width of a text without GDI+'s side padding.
        public static float TextW(Graphics g, string text, Font f)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            using (var sf = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces })
                return g.MeasureString(text, f, PointF.Empty, sf).Width;
        }

        // Letter-spaced text (negative spacing tightens it, as PayDrop's heavy headline is); returns where it ended.
        public static float TrackedWidth(Graphics g, string text, Font f, float spacing)
        {
            float x = 0;
            foreach (char ch in text ?? "") x += TextW(g, ch.ToString(), f) + spacing;
            return x;
        }
        public static float Tracked(Graphics g, string text, Font f, Brush b, float x, float y, float spacing)
        {
            using (var sf = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces })
                foreach (char ch in text ?? "")
                {
                    var s = ch.ToString();
                    g.DrawString(s, f, b, x, y, sf);
                    x += g.MeasureString(s, f, PointF.Empty, sf).Width + spacing;
                }
            return x;
        }

        // From the top of a line to its baseline, in pixels.
        public static float AscentPx(Graphics g, Font f)
        {
            var fam = f.FontFamily;
            return f.SizeInPoints * g.DpiY / 72f * fam.GetCellAscent(f.Style) / fam.GetEmHeight(f.Style);
        }

        // A tiny two-line label: the label in white, its value under it in a muted colour.
        public static void Meta(Graphics g, string label, string value, Color valueColor, RectangleF box, StringAlignment align)
        {
            var f = Fx.MetaFont;
            using (var sf = new StringFormat { Alignment = align, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None })
            {
                // The label quiet, the value (what is read) in the text colour unless it carries a meaning of its own.
                using (var b = new SolidBrush(Muted)) g.DrawString(label ?? "", f, b, new RectangleF(box.X, box.Y, box.Width, 16), sf);
                using (var vf = Theme.UiBold(8.75f))
                using (var b = new SolidBrush(valueColor == Muted || valueColor == Dim || valueColor == Soft ? Text : valueColor)) g.DrawString(value ?? "", vf, b, new RectangleF(box.X, box.Y + 16, box.Width, 18), sf);
            }
        }

        // The badge an option tested by the developers shows (set to the form's language).
        public static string TestedText = "Tested";
        public static string ExperimentalText = "Experimental";

        // A keyboard focus ring (Tab) just inside a control's edge.
        public static void FocusRing(Graphics g, RectangleF r, float radius, Color c)
        {
            using (var p = new Pen(Color.FromArgb(210, c), 1.5f))
            using (var path = Round(r, radius)) g.DrawPath(p, path);
        }

        // PayDrop's backdrop inside `r` (a card's own rectangle): a magenta glow at the left, a peach one
        // at the top right, two thin orbit rings at the right with its four milestone dots on them.
        public static void DrawOrbits(Graphics g, RectangleF r)
        {
            Action<float, float, float, Color, int> glow = (cx, cy, radius, color, alpha) => {
                using (var p = new GraphicsPath())
                {
                    p.AddEllipse(cx - radius, cy - radius, radius * 2, radius * 2);
                    using (var b = new PathGradientBrush(p) { CenterColor = Color.FromArgb(alpha, color), SurroundColors = new[] { Color.FromArgb(0, color) } })
                        g.FillPath(b, p);
                }
            };
            glow(r.X + r.Width * 0.16f, r.Y + r.Height * 0.70f, r.Height * 2.2f, GlowMagenta, 120);
            glow(r.Right - r.Width * 0.04f, r.Y + r.Height * 0.05f, r.Height * 1.3f, GlowPeach, 80);
            float ox = r.X + r.Width * 0.80f, oy = r.Y + r.Height * 0.62f;
            float[] rings = { r.Height * 1.05f, r.Height * 0.66f };
            using (var pen = new Pen(Ring, 1f))
                foreach (var rr in rings) g.DrawEllipse(pen, ox - rr, oy - rr, rr * 2, rr * 2);
            // The dots, as on PayDrop's milestone orbit: hot pink, plum, peach, butter.
            var dots = new[] {
                Tuple.Create(rings[0], 200f, HotPink), Tuple.Create(rings[1], 300f, Plum),
                Tuple.Create(rings[1], 150f, Peach), Tuple.Create(rings[0], 330f, Cyan),
            };
            foreach (var d in dots)
            {
                double a = d.Item2 * Math.PI / 180.0;
                float x = ox + (float)Math.Cos(a) * d.Item1, y = oy + (float)Math.Sin(a) * d.Item1;
                using (var halo = new SolidBrush(Color.FromArgb(60, Theme.Ink))) g.FillEllipse(halo, x - 9, y - 9, 18, 18);
                using (var b = new SolidBrush(d.Item3)) g.FillEllipse(b, x - 6, y - 6, 12, 12);
            }
        }

        static string mono;
        // Cascadia Mono where Windows has it, Consolas otherwise.
        public static string MonoName
        {
            get
            {
                if (mono != null) return mono;
                mono = "Consolas";
                try
                {
                    using (var fonts = new InstalledFontCollection())
                        foreach (var f in fonts.Families)
                            if (f.Name == "Cascadia Mono") { mono = f.Name; break; }
                }
                catch (Exception) { }
                return mono;
            }
        }
        public static Font Mono(float size) { return new Font(MonoName, size); }
        public static Font Ui(float size) { return new Font("Segoe UI", size); }
        public static Font UiBold(float size) { return new Font("Segoe UI Semibold", size); }

        public static GraphicsPath Round(RectangleF r, float radius)
        {
            var p = new GraphicsPath();
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 0) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void Smooth(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        public static void Panel(Graphics g, RectangleF r, Color fill, Color border) { Panel(g, r, fill, border, 10); }

        public static void Panel(Graphics g, RectangleF r, Color fill, Color border, float radius)
        {
            using (var path = Round(r, radius))
            {
                using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                using (var p = new Pen(border)) g.DrawPath(p, path);
            }
        }
    }

    // A theme in the side bar's picker: a circle of the theme's page colour with its accent inside, a ring
    // around the one in use, a lighter ring on hover. Its name is its tooltip.
    class ThemeDot : Control
    {
        public string ThemeName = "plum";
        bool hover;
        public ThemeDot()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            Size = new Size(30, 30);
            Cursor = Cursors.Hand;
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Side);
            Theme.Smooth(g);
            bool on = ThemeName == Theme.Current;
            var outer = new RectangleF(2, 2, Width - 5, Height - 5);
            if (on || hover)
                using (var p = new Pen(on ? Theme.Cyan : Theme.LineStrong, on ? 2f : 1.2f)) g.DrawEllipse(p, outer);
            var disc = new RectangleF(6, 6, Width - 13, Height - 13);
            using (var b = new SolidBrush(Theme.PreviewBack(ThemeName))) g.FillEllipse(b, disc);
            using (var p = new Pen(Color.FromArgb(70, Theme.Ink))) g.DrawEllipse(p, disc);
            // The accent as the lower right half of the disc.
            using (var b = new SolidBrush(Theme.PreviewAccent(ThemeName))) g.FillPie(b, disc.X, disc.Y, disc.Width, disc.Height, -45f, 180f);
        }
    }

    // Colour, easing and cached fonts of the animated controls.
    static class Fx
    {
        public static Color Lerp(Color a, Color b, float t)
        {
            if (t <= 0f) return a;
            if (t >= 1f) return b;
            return Color.FromArgb((int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
        }
        // The same colour with its opacity scaled (k 0..1).
        public static Color Alpha(Color c, float k) { return Color.FromArgb(Math.Max(0, Math.Min(255, (int)(c.A * k))), c.R, c.G, c.B); }
        public static float Clamp(float v, float lo, float hi) { return v < lo ? lo : v > hi ? hi : v; }
        public static float SmoothStep(float e0, float e1, float x)
        {
            float t = Clamp((x - e0) / (e1 - e0), 0f, 1f);
            return t * t * (3f - 2f * t);
        }
        // Ease-out (fast start, soft landing) and ease-in-out.
        public static float Out(float p) { float q = 1f - p; return 1f - q * q * q; }
        public static float InOut(float p) { return p * p * (3f - 2f * p); }
        // Fonts the animated paths use every frame (never disposed).
        static Font meta, chipName, chipDetail;
        public static Font MetaFont { get { if (meta == null) meta = Theme.Ui(8.5f); return meta; } }
        public static Font ChipName { get { if (chipName == null) chipName = Theme.UiBold(9f); return chipName; } }
        public static Font ChipDetail { get { if (chipDetail == null) chipDetail = Theme.Ui(8f); return chipDetail; } }
    }

    // A value that eases toward a target (hover, press, a switch's knob, a fade). It is stepped by the shared
    // clock only while it moves; when animations are off, or the window is hidden, minimised or a game is
    // running, it jumps to its target at once.
    sealed class Mix
    {
        public float V;
        float a, b;
        long t0;
        int dur;
        bool run;
        readonly Action inval;
        public Mix(Control owner) { inval = delegate { if (!owner.IsDisposed) owner.Invalidate(); }; }
        public Mix(Action invalidate) { inval = invalidate; }
        public bool Running { get { return run; } }
        public float Target { get { return b; } }
        public void Set(float v) { V = a = b = v; run = false; }
        public void To(float target, int ms)
        {
            if (target == b && (run || V == target)) return;
            b = target;
            if (ms <= 0 || !Anim.Live) { V = target; run = false; inval(); return; }
            a = V; t0 = Anim.Now; dur = ms;
            if (!run) { run = true; Anim.Add(this); }
            Anim.Wake();
        }
        internal bool Step(long now)
        {
            if (!run) return false;
            float p = Math.Min(1f, (now - t0) / (float)dur);
            V = a + (b - a) * Fx.Out(p);
            inval();
            if (p >= 1f) { V = b; run = false; return false; }
            return true;
        }
        internal void Finish() { if (V != b || run) { V = b; run = false; inval(); } }
    }

    // THE animation clock: one System.Windows.Forms.Timer for the whole launcher. It runs only while something
    // moves and the window is shown (not minimised), animations are on and no game is running. Idle ambient
    // animations (orbits, glows, the PLAY shimmer) also need the launcher to be the foreground window and
    // are asked for a frame about every 50 ms; the timer sleeps for as long as nothing wants one.
    static class Anim
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SystemParametersInfo(uint action, uint param, ref int value, uint winIni);

        public static bool UserOn = true;          // launcher.ini: animations=0 turns everything off
        static bool systemOn = true;               // Windows: Show animations (SPI_GETCLIENTAREAANIMATION)
        public static volatile bool GameRunning;   // a game is running: no ticks at all
        static Form host;
        static Timer clock;
        static readonly System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        static readonly List<Mix> mixes = new List<Mix>();

        sealed class Ambient { public Control C; public bool NeedsFocus; public Func<double, int> Step; public long Due; }
        static readonly List<Ambient> ambients = new List<Ambient>();

        public static long Now { get { return watch.ElapsedMilliseconds; } }

        public static bool Live
        {
            get { return UserOn && systemOn && host != null && host.IsHandleCreated && host.Visible && host.WindowState != FormWindowState.Minimized && !GameRunning; }
        }

        static bool Foreground { get { return Form.ActiveForm == host; } }

        static void ReadSystem()
        {
            try
            {
                int v = 1;
                systemOn = SystemParametersInfo(0x1042, 0, ref v, 0) ? v != 0 : SystemInformation.UIEffectsEnabled;
            }
            catch (Exception) { systemOn = true; }
        }

        public static void Attach(Form f, bool userOn)
        {
            host = f; UserOn = userOn;
            ReadSystem();
            f.Activated += (s, e) => Wake();
            f.Deactivate += (s, e) => Wake();
            f.Resize += (s, e) => Wake();
            f.VisibleChanged += (s, e) => Wake();
            try
            {
                Microsoft.Win32.SystemEvents.UserPreferenceChanged += (s, e) => {
                    try { if (!f.IsDisposed && f.IsHandleCreated) f.BeginInvoke(new Action(() => { ReadSystem(); Wake(); })); } catch (Exception) { }
                };
            }
            catch (Exception) { }
        }

        internal static void Add(Mix m) { mixes.Add(m); }

        // An idle ambient animation of a control: `step(seconds)` returns the milliseconds until it wants the
        // next call (0: the next frame), and repaints only what changed.
        public static void Register(Control c, bool needsFocus, Func<double, int> step)
        {
            var a = new Ambient { C = c, NeedsFocus = needsFocus, Step = step, Due = 0 };
            ambients.Add(a);
            c.Disposed += (s, e) => ambients.Remove(a);
            Wake();
        }

        // Something changed that an ambient animation should notice now.
        public static void Poke(Control c)
        {
            foreach (var a in ambients) if (a.C == c) a.Due = 0;
            Wake();
        }

        public static void Wake()
        {
            try
            {
                if (host == null || host.IsDisposed) return;
                if (!Live) { Settle(); return; }
                if (clock == null) { clock = new Timer { Interval = 16 }; clock.Tick += Tick; }
                if (!clock.Enabled) { clock.Interval = 16; clock.Start(); }
            }
            catch (Exception) { }
        }

        static void Stop() { if (clock != null && clock.Enabled) clock.Stop(); }

        // Not live: every moving value lands on its target and the timer stops.
        static void Settle()
        {
            foreach (var m in mixes.ToArray()) m.Finish();
            mixes.Clear();
            Stop();
        }

        static void Tick(object sender, EventArgs e)
        {
            try
            {
                if (!Live) { Settle(); return; }
                long now = Now;
                int next = int.MaxValue;
                for (int i = mixes.Count - 1; i >= 0; i--)
                    if (!mixes[i].Step(now)) mixes.RemoveAt(i);
                if (mixes.Count > 0) next = 16;
                bool fg = Foreground;
                for (int i = 0; i < ambients.Count; i++)
                {
                    var a = ambients[i];
                    if (a.C.IsDisposed || !a.C.Visible || (a.NeedsFocus && !fg)) continue;
                    if (now < a.Due) { next = Math.Min(next, (int)(a.Due - now)); continue; }
                    int wait = a.Step(now / 1000.0);
                    if (wait <= 0) wait = 50;
                    a.Due = now + wait;
                    next = Math.Min(next, wait);
                }
                if (next == int.MaxValue) Stop();
                else clock.Interval = Math.Max(15, Math.Min(next, 5000));
            }
            catch (Exception) { Stop(); }
        }
    }


    // A switch with its text to the right; Checked as a CheckBox.
    class Toggle : CheckBox
    {
        readonly Mix hv, dn, kn;
        public Toggle()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor | ControlStyles.ResizeRedraw, true);
            hv = new Mix(this); dn = new Mix(this); kn = new Mix(this);
            AutoSize = false;
            Height = 30;
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
            ForeColor = Theme.Text;
        }
        protected override void OnMouseEnter(EventArgs e) { hv.To(1, 140); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hv.To(0, 200); dn.To(0, 120); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { dn.To(1, 70); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { dn.To(0, 140); base.OnMouseUp(e); }
        protected override void OnCheckedChanged(EventArgs e) { kn.To(Checked ? 1f : 0f, 190); base.OnCheckedChanged(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        public static void DrawSwitch(Graphics g, RectangleF track, bool on, bool hover, bool enabled) { DrawSwitch(g, track, on ? 1f : 0f, hover ? 1f : 0f, enabled, 0f); }
        public static void DrawSwitch(Graphics g, RectangleF track, bool on, bool hover, bool enabled, bool pressed) { DrawSwitch(g, track, on ? 1f : 0f, hover ? 1f : 0f, enabled, pressed ? 1f : 0f); }
        // `on` 0..1 is where the knob is (and how far the track has turned butter); hover and pressed 0..1.
        public static void DrawSwitch(Graphics g, RectangleF track, float on, float hover, bool enabled, float pressed)
        {
            Color off = Fx.Lerp(Theme.TrackOff, Theme.TrackHover, hover);
            Color acc = Fx.Lerp(Fx.Lerp(Theme.Cyan, Theme.AccentHover, hover), Theme.Violet, pressed);
            using (var path = Theme.Round(track, track.Height / 2))
            using (var b = new SolidBrush(!enabled ? Theme.TrackDisabled : Fx.Lerp(off, acc, on))) g.FillPath(b, path);
            float k = track.Height - 4;
            // The knob stretches a little while it is pressed, as a real switch does.
            float kw = enabled ? k + 3 * pressed : k;
            float kx = track.X + 2 + (track.Width - kw - 4) * on;
            if (enabled)
                using (var sb = new SolidBrush(Theme.Shadow)) g.FillEllipse(sb, kx, track.Y + 3, kw, k);
            using (var b = new SolidBrush(!enabled ? Theme.KnobDisabled : Fx.Lerp(Theme.Knob, Theme.OnAccent, on))) g.FillEllipse(b, kx, track.Y + 2, kw, k);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.ClearBack(this, g);
            Theme.Smooth(g);
            if (!kn.Running) kn.Set(Checked ? 1f : 0f);
            var track = new RectangleF(2, (int)((Height - 20) / 2), 36, 20);
            if (Focused && ShowFocusCues && Enabled) Theme.FocusRing(g, new RectangleF(track.X - 2, track.Y - 2, track.Width + 4, track.Height + 4), 12, Theme.Text);
            DrawSwitch(g, track, kn.V, hv.V, Enabled, dn.V);
            var text = new RectangleF(48, 0, Math.Max(0, Width - 50), Height);
            using (var b = new SolidBrush(Enabled ? ForeColor : Theme.Dim))
            using (var f = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None })
                g.DrawString(Text ?? "", Font, b, text, f);
        }
    }


    // A launch option as a flat row of the page: the title (with a small butter "tested" mark, a title
    // starting with "★ "), up to two muted lines saying what it does, the switch at the right and a hairline under
    // it. No card, no border.
    class OptionCard : CheckBox
    {
        public string Line = "";
        readonly Mix hv, dn, kn;
        public OptionCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            hv = new Mix(this); dn = new Mix(this); kn = new Mix(this);
            AutoSize = false;
            Size = new Size(400, 70);
            Cursor = Cursors.Hand;
            Font = Theme.UiBold(10f);
            ForeColor = Theme.Text;
        }
        protected override void OnMouseEnter(EventArgs e) { hv.To(1, 140); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hv.To(0, 200); dn.To(0, 120); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { dn.To(1, 70); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { dn.To(0, 140); base.OnMouseUp(e); }
        protected override void OnCheckedChanged(EventArgs e) { kn.To(Checked ? 1f : 0f, 190); base.OnCheckedChanged(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.ClearBack(this, g);
            Theme.Smooth(g);
            if (!kn.Running) kn.Set(Checked ? 1f : 0f);
            float h = Enabled ? hv.V : 0f, d = Enabled ? dn.V : 0f;
            if (h > 0.01f || d > 0.01f)
                using (var b = new SolidBrush(Color.FromArgb((int)(12 * h + 10 * d), Theme.Ink))) g.FillRectangle(b, 0, 0, Width, Height - 1);
            using (var p = new Pen(Fx.Lerp(Theme.Rule, Theme.LineStrong, h * 0.5f))) g.DrawLine(p, 0, Height - 1, Width, Height - 1);
            if (Focused && ShowFocusCues && Enabled) Theme.FocusRing(g, new RectangleF(1.5f, 1.5f, Width - 3f, Height - 4f), 6, Theme.Cyan);
            const int sw = 38;
            Toggle.DrawSwitch(g, new RectangleF(Width - sw - 4, (int)((Height - 1 - 22) / 2), sw, 22), kn.V, h, Enabled, d);
            var title = Text ?? "";
            bool tested = title.StartsWith("★");
            // "◇": not tested in the game yet, marked EXPERIMENTAL in the warning colour.
            bool trial = !tested && title.StartsWith("◇");
            if (tested) title = title.TrimStart('★', ' ');
            if (trial) title = title.TrimStart('◇', ' ');
            float textW = Math.Max(20, Width - sw - 4 - 16);
            using (var b = new SolidBrush(Enabled ? Theme.Text : Theme.Dim))
            using (var f = new StringFormat(StringFormat.GenericTypographic) { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, LineAlignment = StringAlignment.Near, HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None })
            using (var tf = Theme.UiBold(6.75f))
            {
                string mark = tested ? Theme.TestedText.ToUpperInvariant() : trial ? Theme.ExperimentalText.ToUpperInvariant() : "";
                float markW = tested || trial ? Theme.TrackedWidth(g, mark, tf, 1.1f) + 14 : 0;
                float tw = Math.Min(Theme.TextW(g, title, Font), textW - markW);
                g.DrawString(title, Font, b, new RectangleF(2, 10, Math.Max(10, tw + 2), 22), f);
                if (trial && Enabled)
                {
                    float mx = 2 + tw + 9;
                    using (var db = new SolidBrush(Theme.Warn)) g.FillEllipse(db, mx, 15.5f, 5, 5);
                    using (var mb = new SolidBrush(Theme.Warn)) Theme.Tracked(g, mark, tf, mb, mx + 10, 14, 1.1f);
                }
                if (tested && Enabled)
                {
                    // The tested mark: a small butter dot and the word in the eyebrow style.
                    float mx = 2 + tw + 9;
                    using (var db = new SolidBrush(Theme.Cyan)) g.FillEllipse(db, mx, 15.5f, 5, 5);
                    using (var mb = new SolidBrush(Theme.Cyan)) Theme.Tracked(g, mark, tf, mb, mx + 10, 14, 1.1f);
                }
            }
            // The description: whole lines only (no half-cut second line), the last one ends with "...".
            using (var lf = Theme.Ui(8.5f))
            using (var b = new SolidBrush(Enabled ? Fx.Lerp(Theme.Muted, Theme.Soft, h * 0.6f) : Theme.KnobDisabled))
            using (var f = new StringFormat { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.LineLimit })
                g.DrawString(Line ?? "", lf, b, new RectangleF(2, 32, textW, Math.Max(10, Height - 35)), f);
        }
    }

    // A preset to pick, set as type: the name large (butter with a butter underline when picked, muted otherwise),
    // the resolution small under it, one more muted line. No box.
    class ModeCard : Control
    {
        public string Title, Tag2, Line;
        bool selected;
        readonly Mix hv, dn, sel;
        public bool Selected { get { return selected; } set { if (selected == value) return; selected = value; sel.To(value ? 1f : 0f, 240); Invalidate(); } }
        public ModeCard()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            hv = new Mix(this); dn = new Mix(this); sel = new Mix(this);
            Cursor = Cursors.Hand;
            Size = new Size(120, 78);
            Font = Theme.Mono(8.5f);
        }
        protected override void OnMouseEnter(EventArgs e) { hv.To(1, 140); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hv.To(0, 200); dn.To(0, 120); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { dn.To(1, 70); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { dn.To(0, 140); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.ClearBack(this, g);
            Theme.Smooth(g);
            if (!sel.Running) sel.Set(selected ? 1f : 0f);
            float s = sel.V, h = Math.Max(hv.V, dn.V);
            // The rule under the preset: a hairline that warms on hover, and a thick butter one that grows from the left when picked.
            using (var b = new SolidBrush(Fx.Lerp(Theme.Rule, Theme.LineStrong, h))) g.FillRectangle(b, 0, Height - 1, Width, 1);
            if (s > 0.01f) using (var b = new SolidBrush(Theme.Cyan)) g.FillRectangle(b, 0, Height - 3, Width * s, 3);
            using (var nf = new StringFormat(StringFormat.GenericTypographic) { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None })
            {
                // The name as large as fits (the translations are longer).
                float size = 14f;
                Font tf = Theme.UiBold(size);
                while (size > 9f && Theme.TextW(g, Title ?? "", tf) > Width - 4) { tf.Dispose(); size -= 0.5f; tf = Theme.UiBold(size); }
                using (tf)
                using (var b = new SolidBrush(!Enabled ? Theme.Dim : Fx.Lerp(Fx.Lerp(Theme.Dim, Theme.Text, h), Theme.Cyan, s)))
                    g.DrawString(Title ?? "", tf, b, new RectangleF(1, 6 - h * 1f, Math.Max(10, Width - 2), 26), nf);
                using (var pf = Theme.Ui(8.75f))
                using (var b = new SolidBrush(Fx.Lerp(Theme.Muted, Theme.Soft, Math.Max(s, h * 0.6f))))
                    g.DrawString(Tag2 ?? "", pf, b, new RectangleF(1, 35, Math.Max(10, Width - 2), 16), nf);
                using (var lf = Theme.Ui(8f))
                using (var b = new SolidBrush(Theme.Faint))
                    g.DrawString(Line ?? "", lf, b, new RectangleF(1, 53, Math.Max(10, Width - 2), 16), nf);
            }
        }
    }

    // A button: the butter fill for the main action, a thin light outline otherwise (nothing behind it: it
    // takes what is under it, a card, the page or a poster's glow). Link: just the words (butter and underlined
    // when Main, i.e. picked), for choices that read as type. Shine: a soft sweep of light now and then (PLAY).
    class Pill : Button
    {
        public bool Link;
        bool main, shine, shineReg;
        float sweep = -1f;
        readonly Mix hv, dn, mn;
        public bool Main { get { return main; } set { if (main == value) return; main = value; mn.To(value ? 1f : 0f, 170); Invalidate(); if (shine) Anim.Poke(this); } }
        public bool Shine
        {
            get { return shine; }
            set
            {
                if (shine == value) return;
                shine = value;
                if (value && !shineReg) { shineReg = true; Anim.Register(this, true, Frame); }
            }
        }
        public Pill()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            hv = new Mix(this); dn = new Mix(this); mn = new Mix(this);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            ForeColor = Theme.Text;
            Font = Theme.Ui(9.5f);
            Height = 34;
        }
        // The sweep: about a second of light every 5.5 s while the button can be pressed; no frames in between.
        int Frame(double t)
        {
            if (!Enabled || !main || Link) { if (sweep >= 0) { sweep = -1f; Invalidate(); } return 5000; }
            const double period = 5.5;
            double p = t % period;
            if (p < 1.05) { sweep = (float)(p / 1.05); Invalidate(); return 33; }
            if (sweep >= 0) { sweep = -1f; Invalidate(); }
            return (int)((period - p) * 1000) + 5;
        }
        protected override void OnMouseEnter(EventArgs e) { hv.To(1, 140); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hv.To(0, 200); dn.To(0, 120); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { dn.To(1, 70); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { dn.To(0, 150); base.OnMouseUp(e); }
        protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode == Keys.Space) dn.To(1, 70); base.OnKeyDown(e); }
        protected override void OnKeyUp(KeyEventArgs e) { if (e.KeyCode == Keys.Space) dn.To(0, 150); base.OnKeyUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { if (!Enabled) { hv.Set(0f); dn.Set(0f); sweep = -1f; } else if (shine) Anim.Poke(this); Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.ClearBack(this, g);
            Theme.Smooth(g);
            float h = hv.V, d = dn.V, m = mn.V, hd = Math.Max(h, d);
            float sh = d * 1.4f;
            var r = new RectangleF(0.5f + sh, 0.5f + sh * 0.7f, Width - 1.5f - 2 * sh, Height - 1.5f - 1.4f * sh);
            // The buttons are whole pills.
            float pr = r.Height / 2f;
            var label = Text ?? "";
            if (Link)
            {
                using (var b = new SolidBrush(!Enabled ? Theme.KnobDisabled : Fx.Lerp(Fx.Lerp(Theme.Dim, Theme.Text, hd), Theme.Cyan, m)))
                using (var f = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None })
                {
                    bool bold = m >= 0.5f;
                    var font = bold ? new Font(Font.FontFamily, Font.Size, FontStyle.Bold) : Font;
                    g.DrawString(label, font, b, new RectangleF(0, 0, Width, Height - 2), f);
                    float u = Math.Max(m, h);
                    if (u > 0.01f && Enabled)
                    {
                        float tw = Math.Min(Width - 2, Theme.TextW(g, label, font)) * Fx.Out(u);
                        using (var ub = new SolidBrush(Fx.Lerp(Theme.LineStrong, Theme.Cyan, m))) g.FillRectangle(ub, (Width - tw) / 2f, Height - 5, tw, 2);
                    }
                    if (bold) font.Dispose();
                }
                if (Focused && ShowFocusCues && Enabled) Theme.FocusRing(g, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), 6, Theme.Cyan);
                return;
            }
            if (!Enabled) Theme.Panel(g, r, Theme.Wash, Color.FromArgb(60, Theme.Ink), pr);
            else
            {
                Color ghostFill = Color.FromArgb((int)(22 * h + 20 * d), Theme.Ink);
                Color ghostEdge = Fx.Lerp(Theme.Outline, Color.FromArgb(235, Theme.Ink), hd);
                Color mainFill = Fx.Lerp(Fx.Lerp(Theme.Cyan, Theme.AccentHover, h), Theme.Violet, d);
                Theme.Panel(g, r, Fx.Lerp(ghostFill, mainFill, m), Fx.Lerp(ghostEdge, mainFill, m), pr);
                if (sweep >= 0f && m > 0.9f)
                {
                    using (var path = Theme.Round(r, pr))
                    {
                        var st = g.Save();
                        g.SetClip(path, CombineMode.Intersect);
                        float bw = r.Height * 1.7f;
                        float x = r.X - bw + (r.Width + bw) * Fx.InOut(sweep);
                        var blend = new ColorBlend(3) { Positions = new[] { 0f, 0.5f, 1f }, Colors = new[] { Color.FromArgb(0, 255, 255, 255), Color.FromArgb(104, 255, 255, 255), Color.FromArgb(0, 255, 255, 255) } };
                        using (var b = new LinearGradientBrush(new RectangleF(x, r.Y, bw, r.Height), Color.White, Color.White, LinearGradientMode.Horizontal) { InterpolationColors = blend })
                            g.FillRectangle(b, x, r.Y - 1, bw, r.Height + 2);
                        g.Restore(st);
                    }
                }
            }
            // Keyboard focus (Tab): a ring just inside the edge, as round as the pill.
            if (Focused && ShowFocusCues && Enabled)
                Theme.FocusRing(g, new RectangleF(3.5f, 3.5f, Width - 7.5f, Height - 7.5f), (Height - 7.5f) / 2f, main ? Theme.OnAccent : Theme.Cyan);
            using (var b = new SolidBrush(Enabled ? Fx.Lerp(ForeColor, Theme.OnAccent, m) : Theme.Dim))
            using (var f = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None })
            {
                // A label longer than the button (Portuguese and French ones often are) gets a smaller font
                // before it is cut with "...".
                var font = Font;
                Font smaller = null;
                float room = Math.Max(10, r.Width - Math.Min(24, r.Height * 0.6f));
                for (float size = Font.Size - 0.5f; size >= 7.5f && g.MeasureString(label, font).Width > room; size -= 0.5f)
                {
                    if (smaller != null) smaller.Dispose();
                    font = smaller = new Font(Font.FontFamily, size, Font.Style);
                }
                g.DrawString(label, font, b, new RectangleF(r.X + 6, r.Y, Math.Max(10, r.Width - 12), r.Height), f);
                if (smaller != null) smaller.Dispose();
            }
        }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    }


    // A key (or a controller button) drawn as a key cap: "Left Ctrl / C" becomes two caps with a slash between.
    class KeyCap : Control
    {
        public bool Pad;
        public KeyCap()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = Theme.Mono(8.75f);
        }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.ClearBack(this, g);
            Theme.Smooth(g);
            var text = Text ?? "";
            using (var mid = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None })
            using (var capFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None })
            {
                if (text == "-" || text.Length == 0)
                {
                    using (var b = new SolidBrush(Theme.Dim)) g.DrawString("—", Font, b, new RectangleF(4, 0, Math.Max(10, Width - 4), Height), mid);
                    return;
                }
                float x = 0;
                var parts = text.Split(new[] { " / ", " + " }, StringSplitOptions.None);
                var joins = new List<string>();
                for (int i = 0, at = 0; i < parts.Length - 1; i++)
                {
                    int found = text.IndexOf(parts[i], at, StringComparison.Ordinal);
                    at = (found < 0 ? at : found) + parts[i].Length;
                    joins.Add(at + 3 <= text.Length ? text.Substring(at, 3).Trim() : "/");
                }
                for (int i = 0; i < parts.Length; i++)
                {
                    var w = (float)Math.Ceiling(g.MeasureString(parts[i], Font).Width) + 12;
                    if (x + w > Width) w = Math.Max(20, Width - x - 1);
                    var r = new RectangleF(x + 0.5f, 1.5f, w, Height - 4f);
                    Theme.Panel(g, r, Pad ? Theme.Back : Theme.Card, Pad ? Theme.Line : Theme.Cyan, 7);
                    using (var b = new SolidBrush(Pad ? Theme.Dim : Theme.Text))
                        g.DrawString(parts[i], Font, b, r, capFmt);
                    x += w + 4;
                    if (i < joins.Count)
                    {
                        var jw = g.MeasureString(joins[i], Font).Width;
                        using (var b = new SolidBrush(Theme.Dim)) g.DrawString(joins[i], Font, b, new RectangleF(x, 0, jw + 4, Height), mid);
                        x += jw + 2;
                    }
                    if (x >= Width) break;
                }
            }
        }
    }

    // A button of the side bar: a tiny number and the name; the active one is butter with a small dot, no box.
    // The dot grows and the colours ease when the page changes; a faint dot and a nudge of the number answer a hover.
    class NavButton : Control
    {
        public string Glyph;
        bool active;
        readonly Mix hv, dn, act;
        public bool Active { get { return active; } set { if (active == value) return; active = value; act.To(value ? 1f : 0f, 220); Invalidate(); } }
        public NavButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            hv = new Mix(this); dn = new Mix(this); act = new Mix(this);
            Cursor = Cursors.Hand;
            Size = new Size(196, 38);
            Font = Theme.Ui(10.5f);
        }
        protected override void OnMouseEnter(EventArgs e) { hv.To(1, 140); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hv.To(0, 200); dn.To(0, 120); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { dn.To(1, 70); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { dn.To(0, 140); base.OnMouseUp(e); }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Side);
            Theme.Smooth(g);
            if (!act.Running) act.Set(active ? 1f : 0f);
            float a = act.V, h = Math.Max(hv.V, dn.V);
            float cy = Height / 2f;
            // The dot: grows from nothing when the page becomes this one; a ghost of it while hovered.
            float dot = 6f * Fx.Clamp(a, 0f, 1f);
            if (dot > 0.2f) using (var b = new SolidBrush(Theme.Cyan)) g.FillEllipse(b, 15 - dot / 2f, cy - dot / 2f, dot, dot);
            if (h > 0.02f && a < 0.98f) using (var b = new SolidBrush(Color.FromArgb((int)(90 * h * (1f - a)), Theme.Cyan))) g.FillEllipse(b, 12, cy - 3, 6, 6);
            float nudge = 2f * h * (1f - a);
            using (var gf = Theme.Mono(8f))
            using (var b = new SolidBrush(Fx.Lerp(Fx.Lerp(Theme.Faint, Theme.Muted, h), Theme.Cyan, a)))
            using (var f = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                g.DrawString(Glyph ?? "", gf, b, new RectangleF(26 + nudge, 0, 26, Height), f);
            Font bold = a >= 0.5f ? Theme.UiBold(10.5f) : null;
            using (var b = new SolidBrush(Fx.Lerp(Fx.Lerp(Theme.Muted, Theme.Text, h), Theme.Cyan, a)))
            using (var f = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None })
                g.DrawString(Text ?? "", bold ?? Font, b, new RectangleF(54 + nudge, 0, Math.Max(10, Width - 60), Height), f);
            if (bold != null) bold.Dispose();
        }
    }

    // What a control drawn over a poster's glow asks it for: the glow behind a rectangle (poster coordinates).
    interface IBackdrop { void PaintBackdrop(Graphics g, Rectangle area); }

    // A full-width header painted on its own glow: deep plum, a soft magenta glow at the left and a peach one at
    // the top right that fades out at the bottom, so nothing shows where it ends. Buttons placed on it take the
    // glow behind them (IBackdrop) instead of a flat colour. The glow is painted once into a bitmap (again only
    // when its size or its drift changes), so repainting a button or a ring is a plain copy.
    class Poster : Control, IBackdrop
    {
        Bitmap bg;
        int bgW, bgH, bgKey = int.MinValue;
        // The glows' drift (pixels, in steps of 3) and breath (-8..8), set by a poster that animates them.
        protected int gMx, gMy, gPx, gPy, gBreath;
        protected readonly Mix intro;
        public Poster()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Width = 900;
            intro = new Mix(this);
            intro.Set(1f);
        }
        // 0..1: how far the headline has faded and slid in (a page was just opened).
        protected float Intro { get { return intro.V; } }
        public void PlayIntro() { intro.Set(0f); intro.To(1f, 240); }
        protected int BackKey() { return unchecked(gMx * 73856093 ^ gMy * 19349663 ^ gPx * 83492791 ^ gPy * 2654435 ^ gBreath * 97); }

        void Rebuild(int key)
        {
            if (bg == null || bgW != Width || bgH != Height)
            {
                if (bg != null) bg.Dispose();
                bg = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
                bgW = Width; bgH = Height;
            }
            bgKey = key;
            float br = gBreath / 8f;
            float ak = 1f + 0.05f * br, rk = 1f + 0.03f * br, bk = 1f - 0.05f * br, sk = 1f - 0.03f * br;
            using (var g = Graphics.FromImage(bg))
            {
                g.Clear(Theme.Back);
                Theme.Smooth(g);
                Theme.Glow(g, Width * 0.20f + gMx, Height * 0.62f + gMy, Width * 0.55f * rk, Height * 0.98f * rk, Theme.GlowMagenta, (int)(170 * ak));
                Theme.Glow(g, Width * 0.17f + gMx * 1.5f, Height * 0.68f + gMy * 1.5f, Width * 0.26f * rk, Height * 0.60f * rk, Theme.Glow2, (int)(120 * ak));
                Theme.Glow(g, Width * 0.98f + gPx, Height * 0.02f + gPy, Width * 0.32f * sk, Height * 0.80f * sk, Theme.GlowPeach, (int)(112 * bk));
                int fade = Math.Min(96, Height / 2);
                using (var b = new LinearGradientBrush(new Rectangle(0, Height - fade, Math.Max(1, Width), fade + 1), Color.FromArgb(0, Theme.Back), Theme.Back, 90f))
                    g.FillRectangle(b, 0, Height - fade, Width, fade + 1);
            }
        }

        public virtual void PaintBackdrop(Graphics g, Rectangle area)
        {
            if (Width <= 0 || Height <= 0) return;
            int key = BackKey();
            if (bg == null || bgW != Width || bgH != Height || bgKey != key) Rebuild(key);
            var r = Rectangle.Intersect(area, new Rectangle(0, 0, Width, Height));
            if (r.Width <= 0 || r.Height <= 0) return;
            g.DrawImage(bg, r, r.X, r.Y, r.Width, r.Height, GraphicsUnit.Pixel);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && bg != null) { bg.Dispose(); bg = null; }
            base.Dispose(disposing);
        }
        // The headline's big type: the heavy word then (a line under it, or after it) the italic serif run in butter.
        public static Font Heavy(float pt) { return new Font("Segoe UI Black", pt); }
        // Plum keeps its italic serif; the other themes write the second run in the interface's own type.
        public static Font Serif(float pt) { return Theme.SerifAccent ? new Font("Georgia", pt, FontStyle.Italic) : new Font("Segoe UI Semibold", pt); }
    }

    // The head of the Play page, as a poster: tiny two-line labels along the top and bottom rules, the giant
    // headline (a heavy word, an italic serif line in butter), one status sentence, the readiness steps as
    // milestone chips on two thin orbit rings. The PLAY button and the others sit on it as child controls.
    // Alive: the rings turn, the chips drift on them, the glows breathe and wander, and a liquid blob of
    // white, butter and peach in the middle leans toward the cursor (all of it only while the launcher is
    // the window in front and no game runs: Anim).
    class Hero : Poster
    {
        public const int Rows = 362, TopRule = 62, BottomRule = 314;
        public string Brand = "", BrandValue = "", RightLabel = "", RightValue = "";
        public string Title = "", Accent = "", Status = "";
        // The facts along the bottom rule: label, value, colour.
        public string[] FactLabels = new string[0], FactValues = new string[0];
        public Color[] FactColors = new Color[0];
        // The milestones: a name, what it says now, 1 done / 0 missing / -1 unknown.
        public string[] StepNames = new string[0], Steps = new string[0];
        int[] states = new int[0];
        bool statesSeen;
        readonly Mix[] dot = new Mix[4], burst = new Mix[4];
        public int[] States { get { return states; } set { SetStates(value); } }

        double orbitT;
        float leanX, leanY, tLeanX, tLeanY;
        long rippleAt = -100000;
        Bitmap blob;
        readonly byte[] blobBuf = new byte[BN * BN * 4];
        readonly float[] bx = new float[3], by = new float[3], br2 = new float[3];
        const int BN = 64;
        static readonly Pen ringPen = new Pen(Theme.Ring, 1f);
        static readonly Pen arcSoft = new Pen(Color.FromArgb(64, Theme.Ink), 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        static readonly Pen arcBright = new Pen(Color.FromArgb(200, Theme.Ink), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        public Hero()
        {
            Height = Rows;
            for (int i = 0; i < 4; i++) { dot[i] = new Mix(InvalOrbit); burst[i] = new Mix(InvalOrbit); }
            Anim.Register(this, true, Frame);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && blob != null) { blob.Dispose(); blob = null; }
            base.Dispose(disposing);
        }

        // The readiness card at the right of the headline (it used to be the orbit; the name stays for the
        // invalidations that animate it).
        public string StepsTitle = "Ready to play";
        const int CardW = 360;
        RectangleF StepsRect() { return new RectangleF(Width - 40 - CardW, TopRule + 26, CardW, BottomRule - TopRule - 52); }
        Rectangle OrbitRect()
        {
            var r = StepsRect();
            return Rectangle.Inflate(Rectangle.Round(r), 26, 8);
        }
        void InvalOrbit() { if (!IsDisposed) Invalidate(OrbitRect()); }

        // A milestone that turns done fills its dot and sends a ring out of it; one that was done from the start just is.
        void SetStates(int[] value)
        {
            var old = states;
            states = value ?? new int[0];
            for (int i = 0; i < 4 && i < states.Length; i++)
            {
                bool done = states[i] == 1, was = i < old.Length && old[i] == 1;
                if (!statesSeen) dot[i].Set(done ? 1f : 0f);
                else
                {
                    dot[i].To(done ? 1f : 0f, 480);
                    if (done && !was) { burst[i].Set(0f); burst[i].To(1f, 950); }
                }
            }
            if (states.Length > 0) statesSeen = true;
        }

        // PLAY was pressed: one soft ripple through the blob.
        public void Ripple() { if (!Anim.Live) return; rippleAt = Anim.Now; Anim.Poke(this); }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            float dx = e.X - (Width - 200), dy = e.Y - (TopRule + BottomRule) / 2f - 38;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d < 1f) { tLeanX = tLeanY = 0; }
            else { float k = Math.Min(10f, d * 0.03f); tLeanX = dx / d * k; tLeanY = dy / d * k; }
            base.OnMouseMove(e);
        }
        protected override void OnMouseLeave(EventArgs e) { tLeanX = tLeanY = 0; base.OnMouseLeave(e); }

        static int Q(double v) { return (int)Math.Round(v / 3.0) * 3; }

        // One ambient frame (about every 50 ms while the window is in front).
        int Frame(double t)
        {
            orbitT = t;
            int before = BackKey();
            const double tau = 2 * Math.PI;
            gBreath = (int)Math.Round(8 * Math.Sin(t * tau / 11.0));
            gMx = Q(Width * 0.030 * Math.Sin(t * tau / 37.0) + Width * 0.012 * Math.Sin(t * tau / 23.0 + 1.3));
            gMy = Q(Height * 0.060 * Math.Sin(t * tau / 31.0 + 0.7) + Height * 0.020 * Math.Sin(t * tau / 19.0));
            gPx = Q(-Width * 0.024 * Math.Sin(t * tau / 43.0 + 2.1) + Width * 0.010 * Math.Sin(t * tau / 27.0));
            gPy = Q(Height * 0.080 * Math.Sin(t * tau / 41.0 + 0.2));
            leanX += (tLeanX - leanX) * 0.16f; leanY += (tLeanY - leanY) * 0.16f;
            // Only the glows breathe now: the page is redrawn when they moved, nothing otherwise.
            if (BackKey() != before) Invalidate();
            return 50;
        }

        // The liquid blob: three soft circles that orbit slowly and merge and part (a metaball field on a 64 x 64 grid),
        // white, butter and peach; a faint halo where the field is weak. Drawn into a small bitmap, then scaled up.
        void UpdateBlob(double t)
        {
            if (blob == null) blob = new Bitmap(BN, BN, PixelFormat.Format32bppPArgb);
            double s = (Anim.Now - rippleAt) / 1000.0;
            float rip = s < 1.8 ? (float)(Math.Exp(-s * 2.4) * Math.Sin(s * 12.0)) : 0f;
            float lx = leanX / 10f, ly = leanY / 10f;
            double[] per = { 26, 19, 15 }, orb = { 0.18, 0.33, 0.30 }, ph = { 0.4, 2.2, 4.1 };
            float[] rad = { 0.45f, 0.35f, 0.27f };
            for (int i = 0; i < 3; i++)
            {
                double w = t * 2 * Math.PI / per[i] * (i == 2 ? -1 : 1) + ph[i];
                bx[i] = (float)(Math.Cos(w) * orb[i]) + lx * 0.05f * (i + 1);
                by[i] = (float)(Math.Sin(w * 0.85 + i) * orb[i] * 0.9) + ly * 0.05f * (i + 1);
                float r = rad[i] * (1f + 0.05f * (float)Math.Sin(t * 0.9 + i * 2.0) + 0.07f * rip);
                br2[i] = r * r;
            }
            const float rw = 255, gw = 246, bw = 252, rb = 255, gb = 244, bb = 163, rp = 255, gp = 178, bp = 166;
            for (int y = 0; y < BN; y++)
            {
                float v = (y + 0.5f) / BN * 2f - 1f;
                for (int x = 0; x < BN; x++)
                {
                    float u = (x + 0.5f) / BN * 2f - 1f;
                    float f = 0, w0 = 0, w1 = 0, w2 = 0;
                    for (int i = 0; i < 3; i++)
                    {
                        float dx = u - bx[i], dy = v - by[i];
                        float fi = br2[i] / (dx * dx + dy * dy + 0.0006f);
                        f += fi;
                        float wi = fi * fi;
                        if (i == 0) w0 = wi; else if (i == 1) w1 = wi; else w2 = wi;
                    }
                    int o = (y * BN + x) * 4;
                    float edge = 1f - Fx.SmoothStep(0.55f, 0.98f, (float)Math.Sqrt(u * u + v * v));
                    float a = Fx.SmoothStep(0.80f, 1.12f, f) * edge, halo = Fx.SmoothStep(0.35f, 1.0f, f) * 0.13f * edge;
                    float A = Math.Max(a, halo);
                    if (A <= 0.002f) { blobBuf[o] = blobBuf[o + 1] = blobBuf[o + 2] = blobBuf[o + 3] = 0; continue; }
                    float ws = w0 + w1 + w2 + 1e-6f;
                    float r = (w0 * rw + w1 * rb + w2 * rp) / ws, g = (w0 * gw + w1 * gb + w2 * gp) / ws, b = (w0 * bw + w1 * bb + w2 * bp) / ws;
                    float hi = Fx.SmoothStep(1.9f, 3.6f, f) * 0.4f;
                    r += (255 - r) * hi; g += (246 - g) * hi; b += (252 - b) * hi;
                    // Toward peach in the faint halo.
                    r += (rp - r) * (1f - a); g += (gp - g) * (1f - a); b += (bp - b) * (1f - a);
                    blobBuf[o] = (byte)(b * A); blobBuf[o + 1] = (byte)(g * A); blobBuf[o + 2] = (byte)(r * A); blobBuf[o + 3] = (byte)(A * 255f);
                }
            }
            var data = blob.LockBits(new Rectangle(0, 0, BN, BN), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            System.Runtime.InteropServices.Marshal.Copy(blobBuf, 0, data.Scan0, blobBuf.Length);
            blob.UnlockBits(data);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var clip = e.ClipRectangle;
            PaintBackdrop(g, clip);
            Theme.Smooth(g);
            float lx = 40, rx = Width - 40, k = Intro;
            if (clip.Top < TopRule)
            {
                Theme.Meta(g, Brand, BrandValue, Theme.Muted, new RectangleF(lx, 15, 320, 34), StringAlignment.Near);
                if (!string.IsNullOrEmpty(RightLabel)) Theme.Meta(g, RightLabel, RightValue, Theme.Muted, new RectangleF(rx - 320, 15, 320, 34), StringAlignment.Far);
            }
            using (var p = new Pen(Theme.HairWhite))
            {
                g.DrawLine(p, 0, TopRule, Width, TopRule);
                g.DrawLine(p, 0, BottomRule, Width, BottomRule);
            }
            if (clip.IntersectsWith(OrbitRect())) DrawSteps(g);
            if (clip.IntersectsWith(new Rectangle(0, TopRule, Math.Max(10, Width - 440), BottomRule - TopRule)))
            {
                g.TranslateTransform(0, (1f - k) * 10f);
                DrawHeadline(g, lx, k);
                using (var f = Theme.Ui(10.5f))
                using (var b = new SolidBrush(Fx.Alpha(Theme.Soft, k)))
                using (var sf = new StringFormat { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.LineLimit, HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None })
                    g.DrawString(Status ?? "", f, b, new RectangleF(lx, 204, Math.Max(100, Math.Min(440, Width - 446 - lx)), 44), sf);
                g.ResetTransform();
            }
            // The facts along the bottom rule: the first at the left, the last at the right, the others between.
            int n = FactLabels.Length;
            if (n > 0 && clip.Bottom > BottomRule)
            {
                float slot = (rx - lx) / n;
                for (int i = 0; i < n; i++)
                {
                    var al = i == 0 ? StringAlignment.Near : i == n - 1 ? StringAlignment.Far : StringAlignment.Center;
                    var c = i < FactColors.Length ? FactColors[i] : Theme.Muted;
                    Theme.Meta(g, FactLabels[i], i < FactValues.Length ? FactValues[i] : "", c, new RectangleF(lx + i * slot, BottomRule + 10, slot - 10, 34), al);
                }
            }
        }

        // "Ready" in heavy type, "when you are." under it in the italic serif, both as large as fit left of the rings.
        void DrawHeadline(Graphics g, float lx, float k)
        {
            float maxW = Math.Max(160, Width - 446 - lx - 16);
            float pt = 44f;
            Font hf = Heavy(pt), af = Serif(pt);
            while (pt > 24f && (Theme.TrackedWidth(g, Title ?? "", hf, -1.5f) > maxW || Theme.TextW(g, Accent ?? "", af) > maxW))
            {
                pt -= 2f; hf.Dispose(); af.Dispose(); hf = Heavy(pt); af = Serif(pt);
            }
            float scale = pt / 44f;
            float baseline = 130;
            using (hf) using (af)
            {
                using (var b = new SolidBrush(Fx.Alpha(Theme.Text, k))) Theme.Tracked(g, Title ?? "", hf, b, lx - 1, baseline - Theme.AscentPx(g, hf), -1.5f);
                using (var b = new SolidBrush(Fx.Alpha(Theme.Cyan, k)))
                using (var sf = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap })
                    g.DrawString(Accent ?? "", af, b, lx - 1, baseline + 56 * scale - Theme.AscentPx(g, af), sf);
            }
        }

        // The readiness card: a title with "3/4" and a thin progress bar, then the four steps as rows. A step's
        // circle fills with the accent and shows a tick when it is done (a ring spreads from it the moment it
        // turns done), is outlined in the warning colour while it is missing, faint while not known.
        static Font stepTitleFont, stepNameFont, stepDetailFont, tickFont;
        void DrawSteps(Graphics g)
        {
            if (stepTitleFont == null) { stepTitleFont = Theme.UiBold(11f); stepNameFont = Theme.UiBold(9.75f); stepDetailFont = Theme.Ui(8.75f); tickFont = Theme.UiBold(8f); }
            var r = StepsRect();
            if (r.Width < 120 || r.Height < 120) return;
            // A soft shadow, then the card.
            using (var path = Theme.Round(new RectangleF(r.X, r.Y + 6, r.Width, r.Height), 16))
            using (var b = new SolidBrush(Theme.Shadow)) g.FillPath(b, path);
            Theme.Panel(g, r, Fx.Alpha(Theme.Card, Theme.Dark ? 0.86f : 0.97f), Theme.Line, 16);
            int n = Math.Min(4, Steps.Length), done = 0;
            for (int i = 0; i < n && i < states.Length; i++) if (states[i] == 1) done++;
            float pad = 20;
            using (var sf = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
            {
                using (var b = new SolidBrush(Theme.Text)) g.DrawString(StepsTitle ?? "", stepTitleFont, b, new RectangleF(r.X + pad, r.Y + 16, r.Width - 2 * pad - 50, 22), sf);
                var count = done + "/" + n;
                using (var b = new SolidBrush(done == n && n > 0 ? Theme.Cyan : Theme.Muted))
                using (var far = new StringFormat(sf) { Alignment = StringAlignment.Far })
                    g.DrawString(count, stepTitleFont, b, new RectangleF(r.Right - pad - 60, r.Y + 16, 60, 22), far);
            }
            // The progress bar.
            var track = new RectangleF(r.X + pad, r.Y + 44, r.Width - 2 * pad, 4);
            using (var path = Theme.Round(track, 2)) using (var b = new SolidBrush(Theme.Quiet)) g.FillPath(b, path);
            float part = n == 0 ? 0f : 0f;
            for (int i = 0; i < n && i < 4; i++) part += dot[i].V / n;
            if (part > 0.005f)
                using (var path = Theme.Round(new RectangleF(track.X, track.Y, Math.Max(4, track.Width * part), track.Height), 2))
                using (var b = new SolidBrush(Theme.Cyan)) g.FillPath(b, path);
            // The rows.
            float top = r.Y + 60, rowH = (r.Bottom - 10 - top) / Math.Max(1, n);
            for (int i = 0; i < n; i++)
            {
                float y = top + i * rowH, cy = y + rowH / 2f;
                if (i > 0) using (var p = new Pen(Theme.Rule)) g.DrawLine(p, r.X + pad, y, r.Right - pad, y);
                int state = i < states.Length ? states[i] : -1;
                float fill = i < 4 ? dot[i].V : 0f, cx = r.X + pad + 9;
                if (i < 4 && burst[i].Running)
                {
                    float rr = 9f + 10f * Fx.Out(burst[i].V);
                    using (var p = new Pen(Fx.Alpha(Theme.Cyan, 1f - burst[i].V), 2f)) g.DrawEllipse(p, cx - rr, cy - rr, rr * 2, rr * 2);
                }
                var ring = state == 0 ? Theme.HotPink : state == 1 ? Theme.Cyan : Theme.Faint;
                using (var p = new Pen(ring, 1.6f)) g.DrawEllipse(p, cx - 9, cy - 9, 18, 18);
                if (fill > 0.01f)
                {
                    float d = 18f * fill;
                    using (var b = new SolidBrush(Theme.Cyan)) g.FillEllipse(b, cx - d / 2, cy - d / 2, d, d);
                    if (fill > 0.6f)
                        using (var b = new SolidBrush(Fx.Alpha(Theme.OnAccent, (fill - 0.6f) / 0.4f)))
                        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                            g.DrawString("✓", tickFont, b, new RectangleF(cx - 9, cy - 9, 18, 18), sf);
                }
                else if (state == 0)
                    using (var b = new SolidBrush(Fx.Alpha(Theme.HotPink, 0.18f))) g.FillEllipse(b, cx - 9, cy - 9, 18, 18);
                float tx = cx + 20;
                using (var sf = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter, LineAlignment = StringAlignment.Center })
                {
                    var name = i < StepNames.Length ? StepNames[i] : "";
                    float nameW = Math.Min(110, Theme.TextW(g, name, stepNameFont) + 4);
                    using (var b = new SolidBrush(Theme.Text)) g.DrawString(name, stepNameFont, b, new RectangleF(tx, cy - 11, nameW, 22), sf);
                    using (var b = new SolidBrush(state == 0 ? Theme.HotPink : Theme.Muted))
                    using (var far = new StringFormat(sf) { Alignment = StringAlignment.Far })
                        g.DrawString(Steps[i] ?? "", stepDetailFont, b, new RectangleF(tx + nameW + 8, cy - 11, r.Right - pad - (tx + nameW + 8), 22), far);
                }
            }
        }
    }

    // The head of the other pages: the same poster, smaller. Two tiny labels on the rule, the headline (heavy word
    // and italic serif run on one line), and a short quiet line under it. Pills may sit on it. The headline and
    // the line under it fade and slide in (about 200 ms) when the page opens.
    class PageHead : Poster
    {
        public string Eye = "", EyeValue = "", RightLabel = "", RightValue = "";
        public Color RightColor = Theme.Muted;
        public string Title = "", Accent = "", Sub = "";
        public int SubWidth = 540;
        public PageHead() { Height = 150; }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            PaintBackdrop(g, e.ClipRectangle);
            Theme.Smooth(g);
            float lx = 40, rx = Width - 40, k = Intro, dy = (1f - k) * 10f;
            Theme.Meta(g, Eye, EyeValue, Theme.Muted, new RectangleF(lx, 15, 320, 34), StringAlignment.Near);
            if (!string.IsNullOrEmpty(RightLabel)) Theme.Meta(g, RightLabel, RightValue, RightColor, new RectangleF(rx - 320, 15, 320, 34), StringAlignment.Far);
            using (var p = new Pen(Theme.HairWhite)) g.DrawLine(p, 0, Hero.TopRule, Width, Hero.TopRule);
            float maxW = Math.Max(200, Width - 2 * lx - (Controls.Count > 0 ? 250 : 0));
            float pt = 38f;
            Font hf = Heavy(pt), af = Serif(pt);
            while (pt > 22f && Theme.TrackedWidth(g, Title ?? "", hf, -1.5f) + 14 + Theme.TextW(g, Accent ?? "", af) > maxW)
            {
                pt -= 2f; hf.Dispose(); af.Dispose(); hf = Heavy(pt); af = Serif(pt);
            }
            float baseline = 122 + dy;
            using (hf) using (af)
            {
                float x;
                using (var b = new SolidBrush(Fx.Alpha(Theme.Text, k))) x = Theme.Tracked(g, Title ?? "", hf, b, lx - 1, baseline - Theme.AscentPx(g, hf), -1.5f);
                using (var b = new SolidBrush(Fx.Alpha(Theme.Cyan, k)))
                using (var sf = new StringFormat(StringFormat.GenericTypographic) { FormatFlags = StringFormatFlags.NoWrap })
                    g.DrawString(Accent ?? "", af, b, x + 12, baseline - Theme.AscentPx(g, af), sf);
            }
            if (!string.IsNullOrEmpty(Sub))
                using (var f = Theme.Ui(10f))
                    TextRenderer.DrawText(g, Sub, f, new Rectangle((int)lx, (int)(142 + dy), SubWidth, Math.Max(20, Height - 142)), Fx.Lerp(Theme.Back, Theme.Soft, k), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.TextBoxControl);
        }
    }


    // The foot of the window: a thin rule and the state, the preset, the engine and the hardware as tiny
    // two-line labels (the PLAY button, on the other pages, sits at its right).
    class FootMeta : Control
    {
        public string State = "", Preset = "", Engine = "", Hardware = "", Why = "";
        public string[] Labels = { "State", "Preset", "Engine", "Hardware", "Next step" };
        public Color StateColor = Theme.Good;
        public FootMeta()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }
        public void Set(string state, Color stateColor, string preset, string engine, string hardware, string why)
        {
            State = state; StateColor = stateColor; Preset = preset; Engine = engine; Hardware = hardware; Why = why ?? "";
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.Back);
            Theme.Smooth(g);
            float y = 13;
            Theme.Meta(g, Labels[0], State, StateColor, new RectangleF(40, y, 140, 34), StringAlignment.Near);
            if (Why.Length > 0)
            {
                Theme.Meta(g, Labels[4], Why, Theme.Dim, new RectangleF(200, y, Math.Max(40, Width - 200 - 12), 34), StringAlignment.Near);
                return;
            }
            float room = Width - 200 - 12;
            float w1 = Math.Min(240, room * 0.34f), w2 = Math.Min(190, room * 0.26f);
            Theme.Meta(g, Labels[1], Preset, Theme.Dim, new RectangleF(200, y, w1 - 14, 34), StringAlignment.Near);
            Theme.Meta(g, Labels[2], Engine, Theme.Dim, new RectangleF(200 + w1, y, w2 - 14, 34), StringAlignment.Near);
            if (room > 560) Theme.Meta(g, Labels[3], Hardware, Theme.Dim, new RectangleF(200 + w1 + w2, y, room - w1 - w2, 34), StringAlignment.Near);
        }
    }


    // "Ready to play" and its checks ("✓ Dump verified  ✓ Binary ready ..."), drawn in a bordered panel.
    class StatusStrip : Control
    {
        public string[] Items = new string[0];
        public int[] States = new int[0];   // 1 good, 0 bad, -1 neutral
        public string ReadyText = "Ready to play", NotReadyText = "Not ready yet";
        // Room kept at the right for the strip's own buttons.
        public int RightRoom = 232;
        public StatusStrip()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = 52;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.ClearBack(this, g);
            Theme.Smooth(g);
            Theme.Panel(g, new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f), Theme.Card, Theme.Line, 14);
            bool ready = Items.Length > 0;
            for (int i = 0; i < Items.Length; i++) if ((i < States.Length ? States[i] : -1) != 1) ready = false;
            var dot = ready ? Theme.Good : Theme.Warn;
            using (var b = new SolidBrush(Color.FromArgb(60, dot))) g.FillEllipse(b, 14, Height / 2f - 8, 16, 16);
            using (var b = new SolidBrush(dot)) g.FillEllipse(b, 17, Height / 2f - 5, 10, 10);
            float x = 38;
            float limit = Width - RightRoom;
            using (var tf = Theme.UiBold(10.5f))
            using (var b = new SolidBrush(Theme.Text))
            using (var f = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None })
            {
                var title = ready ? ReadyText : NotReadyText;
                float tw = (float)Math.Ceiling(g.MeasureString(title, tf).Width);
                float room = Math.Max(40, Math.Min(tw, limit - x));
                g.DrawString(title, tf, b, new RectangleF(x, 0, room, Height), f);
                x += room + 14;
            }
            using (var f = Theme.Ui(9f))
            using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None })
                for (int i = 0; i < Items.Length; i++)
                {
                    int s = i < States.Length ? States[i] : -1;
                    var mark = s == 1 ? "✓" : s == 0 ? "!" : "•";
                    var text = mark + "  " + Items[i];
                    var w = (float)Math.Ceiling(g.MeasureString(text, f).Width);
                    if (x + w > limit) break;
                    using (var b = new SolidBrush(s == 1 ? Theme.Soft : s == 0 ? Theme.Warn : Theme.Dim))
                        g.DrawString(text, f, b, new RectangleF(x, 0, w + 4, Height), sf);
                    x += w + 12;
                }
        }
    }

    // A loading bar with its text. The fill eases to each new value (the percentage counts with it) and, while
    // work is under way, a soft sheen slides along the filled part.
    class LoadBar : Control
    {
        float done, sheen = -1f;
        readonly Mix shown;
        public LoadBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = Theme.Mono(8.75f);
            shown = new Mix(this);
            Anim.Register(this, false, Frame);
        }
        public void Set(float part, string text)
        {
            float was = done;
            done = Math.Max(0f, Math.Min(1f, part));
            Text = text;
            // Hidden, or starting over: no animation from the old value.
            if (!Visible || done < was - 0.001f) { shown.Set(done); Invalidate(); }
            else shown.To(done, 280);
            Anim.Poke(this);
        }
        int Frame(double t)
        {
            if (shown.V > 0.002f && shown.V < 0.998f && done < 0.999f)
            {
                sheen = (float)((t % 1.7) / 1.7);
                Invalidate(new Rectangle(0, Math.Max(0, Height - 9), Width, 9));
                return 40;
            }
            if (sheen >= 0f) { sheen = -1f; Invalidate(new Rectangle(0, Math.Max(0, Height - 9), Width, 9)); }
            return 5000;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.ClearBack(this, g);
            Theme.Smooth(g);
            if (!shown.Running && shown.V != done) shown.Set(done);
            float v = shown.V;
            float trackW = Math.Max(6, Width - 1);
            var track = new RectangleF(0, Height - 7, trackW, 6);
            using (var path = Theme.Round(track, 3f))
            using (var b = new SolidBrush(Theme.Card)) g.FillPath(b, path);
            if (v > 0.001f)
            {
                var fill = new RectangleF(0, Height - 7, Math.Min(trackW, Math.Max(6, trackW * v)), 6);
                using (var path = Theme.Round(fill, 3f))
                {
                    using (var b = new SolidBrush(Theme.Cyan)) g.FillPath(b, path);
                    if (sheen >= 0f)
                    {
                        var st = g.Save();
                        g.SetClip(path, CombineMode.Intersect);
                        float bw = 90f, x = fill.X - bw + (fill.Width + bw) * sheen;
                        var blend = new ColorBlend(3) { Positions = new[] { 0f, 0.5f, 1f }, Colors = new[] { Color.FromArgb(0, 255, 255, 255), Color.FromArgb(140, 255, 255, 255), Color.FromArgb(0, 255, 255, 255) } };
                        using (var b = new LinearGradientBrush(new RectangleF(x, fill.Y, bw, fill.Height), Color.White, Color.White, LinearGradientMode.Horizontal) { InterpolationColors = blend })
                            g.FillRectangle(b, x, fill.Y - 1, bw, fill.Height + 2);
                        g.Restore(st);
                    }
                }
            }
            var pct = ((int)(v * 100 + 0.5f)).ToString() + "%";
            float pw = (float)Math.Ceiling(g.MeasureString(pct, Font).Width);
            float textH = Math.Max(10, Height - 9);
            // The text gives way (with "...") to the percentage instead of running under it.
            using (var f = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None })
            {
                using (var b = new SolidBrush(Theme.Text)) g.DrawString(Text ?? "", Font, b, new RectangleF(0, 0, Math.Max(10, Width - pw - 10), textH), f);
                using (var b = new SolidBrush(Theme.Dim))
                using (var rf = new StringFormat(f) { Alignment = StringAlignment.Far })
                    g.DrawString(pct, Font, b, new RectangleF(Width - pw - 4, 0, pw + 4, textH), rf);
            }
        }
    }


    // A section title ("Quality preset"), with an optional quiet note at the right and a hairline under it.
    class Heading : Control
    {
        public string Note = "";
        public bool Hair = true;
        public Heading(string text)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Text = text;
            Height = 26;
            Font = Theme.UiBold(11.5f);
        }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.ClearBack(this, g);
            Theme.Smooth(g);
            float noteW = 0;
            Font nf = null;
            if (!string.IsNullOrEmpty(Note))
            {
                nf = Theme.Ui(8.5f);
                noteW = Math.Min((float)Math.Ceiling(g.MeasureString(Note, nf).Width), Math.Max(0, Width * 0.5f));
            }
            using (var f = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap, HotkeyPrefix = System.Drawing.Text.HotkeyPrefix.None })
            {
                using (var b = new SolidBrush(Theme.Text))
                    g.DrawString(Text ?? "", Font, b, new RectangleF(-1, 0, Math.Max(10, Width - noteW - (noteW > 0 ? 12 : 0)), Height - 2), f);
                if (nf != null)
                {
                    using (var b = new SolidBrush(Theme.Muted)) g.DrawString(Note, nf, b, new RectangleF(Width - noteW - 2, 0, noteW + 2, Height - 2), f);
                    nf.Dispose();
                }
            }
            if (Hair) using (var p = new Pen(Theme.Rule)) g.DrawLine(p, 0, Height - 1, Width, Height - 1);
        }
    }

    // A small letter-spaced uppercase label ("DISPLAY"), with an optional hairline under it.
    class Eyebrow : Control
    {
        public bool Hair = true;
        public Color Tint = Theme.Muted;
        public Eyebrow(string text)
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Text = text;
            Height = 24;
        }
        protected override void OnTextChanged(EventArgs e) { Invalidate(); base.OnTextChanged(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.ClearBack(this, g);
            Theme.Smooth(g);
            using (var f = Theme.UiBold(7.5f))
            using (var b = new SolidBrush(Tint))
                Theme.Tracked(g, (Text ?? "").ToUpperInvariant(), f, b, 0, 2, 1.3f);
            if (Hair) using (var p = new Pen(Theme.Rule)) g.DrawLine(p, 0, Height - 1, Width, Height - 1);
        }
    }

    // A one-pixel hairline.
    class Rule : Control
    {
        public Rule() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); Height = 1; }
        protected override void OnPaint(PaintEventArgs e) { e.Graphics.Clear(Theme.Rule); }
    }
}
