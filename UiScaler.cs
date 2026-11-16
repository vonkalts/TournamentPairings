using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TournamentPairings
{
    /// <summary>
    /// Scales a code-built form:
    /// - everything grows/shrinks with the window size (proportionally)
    /// - plus a user zoom (Ctrl + / Ctrl - / Ctrl 0, Ctrl + mouse wheel), saved between runs
    /// - Windows display scaling (125%, 150%...) is applied to sizes too, not only to fonts
    /// The design layout (bounds as built in code) is recorded once; every layout is computed
    /// from it, and WinForms anchoring is replaced by the same anchor rules applied here.
    /// </summary>
    public sealed class UiScaler
    {
        private const double MinZoom = 0.5;
        private const double MaxZoom = 3.0;
        private const double ZoomStep = 0.1;
        private const double MinWindowFraction = 0.5;

        private static double _zoom = LoadZoom();
        private static readonly List<UiScaler> Active = new();
        private static readonly Dictionary<(string, float, FontStyle), Font> FontCache = new();
        private static bool _wheelFilterInstalled;

        private sealed record Item(Control Control, Rectangle Bounds, AnchorStyles Anchor, Font Font);
        private sealed record GridItem(DataGridView Grid, int[] ColumnWidths, int HeaderHeight, int RowHeight, int RowHeadersWidth);

        private readonly Form _form;
        private readonly Action? _afterLayout;
        private readonly Size _designWindow;
        private readonly Size _frame; // window borders + title bar (do not scale)
        private readonly Font _formFont;
        private readonly Dictionary<Control, List<Item>> _children = new();
        private readonly Dictionary<Control, Size> _designClient = new();
        private readonly List<GridItem> _grids = new();
        private bool _inLayout;

        /// <summary>
        /// Call at the end of the form constructor, after all controls are built.
        /// afterLayout: code that positions controls itself (e.g. text-width dependent labels).
        /// </summary>
        public static void Attach(Form form, Action? afterLayout = null) => _ = new UiScaler(form, afterLayout);

        private UiScaler(Form form, Action? afterLayout)
        {
            _form = form;
            _afterLayout = afterLayout;
            _designWindow = form.Size;
            _frame = form.Size - form.ClientSize;
            _formFont = form.Font;

            Snapshot(form);

            form.AutoScaleMode = AutoScaleMode.None;
            form.KeyPreview = true;

            double dpi = form.DeviceDpi / 96.0;
            var work = Screen.FromPoint(Cursor.Position).WorkingArea;
            form.MinimumSize = new Size(
                (int)(_designWindow.Width * dpi * MinWindowFraction),
                (int)(_designWindow.Height * dpi * MinWindowFraction));
            form.Size = new Size(
                Math.Min((int)(_designWindow.Width * dpi), work.Width),
                Math.Min((int)(_designWindow.Height * dpi), work.Height));

            // A window shrunk to fit the screen must not open partly off-screen
            if (form.Width == work.Width || form.Height == work.Height)
                form.StartPosition = FormStartPosition.CenterScreen;

            form.ClientSizeChanged += (_, __) => ApplyLayout();
            form.Activated += (_, __) => ApplyLayout(); // zoom may have changed in another window
            form.KeyDown += OnKeyDown;
            form.FormClosed += (_, __) => Active.Remove(this);

            Active.Add(this);
            if (!_wheelFilterInstalled)
            {
                Application.AddMessageFilter(new CtrlWheelFilter());
                _wheelFilterInstalled = true;
            }

            ApplyLayout();
        }

        private void Snapshot(Control container)
        {
            _designClient[container] = container.ClientSize;
            var items = new List<Item>();

            foreach (Control c in container.Controls)
            {
                items.Add(new Item(c, c.Bounds, c.Anchor, c.Font));

                if (c is DataGridView g)
                {
                    _grids.Add(new GridItem(
                        g,
                        g.Columns.Cast<DataGridViewColumn>().Select(col => col.Width).ToArray(),
                        g.ColumnHeadersHeight,
                        g.RowTemplate.Height,
                        g.RowHeadersWidth));
                }

                if (c is Panel)
                    Snapshot(c);

                // Anchors are applied by LayoutChildren from now on
                c.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            }

            _children[container] = items;
        }

        private void ApplyLayout()
        {
            if (_inLayout || _form.WindowState == FormWindowState.Minimized) return;
            _inLayout = true;

            try
            {
                // Re-run if scrollbars appearing/disappearing changed the client area
                for (int pass = 0; pass < 3; pass++)
                {
                    var clientBefore = _form.ClientSize;

                    double dpi = _form.DeviceDpi / 96.0;
                    var design = _designClient[_form];
                    var available = _form.Size - _frame; // client area without scrollbars
                    double windowFactor = Math.Min(
                        available.Width / (design.Width * dpi),
                        available.Height / (design.Height * dpi));
                    double fontScale = _zoom * windowFactor;
                    double scale = fontScale * dpi; // pixel scale for positions/sizes

                    // Truncate so that at 100% zoom the content fits exactly (no scrollbars from rounding)
                    var needed = new Size((int)(design.Width * scale), (int)(design.Height * scale));

                    // Scrollbars only when the zoomed content is bigger than the window
                    bool overflow = needed.Width > available.Width || needed.Height > available.Height;
                    if (_form.AutoScroll != overflow) _form.AutoScroll = overflow;
                    _form.AutoScrollMinSize = overflow ? needed : Size.Empty;

                    var area = new Size(
                        Math.Max(_form.ClientSize.Width, needed.Width),
                        Math.Max(_form.ClientSize.Height, needed.Height));

                    _form.SuspendLayout();
                    var formFont = GetFont(_formFont, fontScale);
                    if (!ReferenceEquals(_form.Font, formFont)) _form.Font = formFont;
                    LayoutChildren(_form, area, scale, fontScale, _form.AutoScrollPosition);
                    foreach (var g in _grids) ScaleGrid(g, scale);
                    _form.ResumeLayout(true);

                    _afterLayout?.Invoke();

                    if (_form.ClientSize == clientBefore) break;
                }
            }
            finally
            {
                _inLayout = false;
            }
        }

        private void LayoutChildren(Control container, Size area, double scale, double fontScale, Point offset)
        {
            var design = _designClient[container];
            int dx = area.Width - R(design.Width * scale);
            int dy = area.Height - R(design.Height * scale);

            container.SuspendLayout();

            foreach (var it in _children[container])
            {
                var b = it.Bounds;
                var (x, w) = Anchor1D(it.Anchor.HasFlag(AnchorStyles.Left), it.Anchor.HasFlag(AnchorStyles.Right), R(b.X * scale), R(b.Width * scale), dx);
                var (y, h) = Anchor1D(it.Anchor.HasFlag(AnchorStyles.Top), it.Anchor.HasFlag(AnchorStyles.Bottom), R(b.Y * scale), R(b.Height * scale), dy);

                var c = it.Control;
                var font = GetFont(it.Font, fontScale);
                if (!ReferenceEquals(c.Font, font)) c.Font = font;

                if (c.AutoSize)
                    c.Location = new Point(x + offset.X, y + offset.Y);
                else
                    c.SetBounds(x + offset.X, y + offset.Y, w, h);

                if (_children.ContainsKey(c))
                    LayoutChildren(c, c.ClientSize, scale, fontScale, Point.Empty);
            }

            container.ResumeLayout(true);
        }

        // Same rules as WinForms anchoring: both sides => stretch, far side only => move, none => keep centered
        private static (int pos, int size) Anchor1D(bool near, bool far, int pos, int size, int delta)
        {
            if (near && far) return (pos, Math.Max(0, size + delta));
            if (far) return (pos + delta, size);
            if (near) return (pos, size);
            return (pos + delta / 2, size);
        }

        private static void ScaleGrid(GridItem g, double scale)
        {
            var grid = g.Grid;

            for (int i = 0; i < g.ColumnWidths.Length && i < grid.Columns.Count; i++)
            {
                var col = grid.Columns[i];
                if (col.InheritedAutoSizeMode != DataGridViewAutoSizeColumnMode.Fill)
                    col.Width = Math.Max(col.MinimumWidth, R(g.ColumnWidths[i] * scale));
            }

            // Never smaller than the text needs (fonts are rounded to 0.5pt, sizes are not)
            int textHeight = grid.Font.Height;
            grid.ColumnHeadersHeight = Math.Max(textHeight + 8, R(g.HeaderHeight * scale));
            grid.RowHeadersWidth = Math.Max(4, R(g.RowHeadersWidth * scale));

            int rowHeight = Math.Max(textHeight + 6, R(g.RowHeight * scale));
            grid.RowTemplate.Height = rowHeight;
            foreach (DataGridViewRow row in grid.Rows)
                row.Height = rowHeight;
        }

        // Fonts are cached (rounded to 0.5pt) so resizing does not create a new font on every pixel
        private static Font GetFont(Font baseFont, double scale)
        {
            float size = (float)Math.Max(5, Math.Round(baseFont.SizeInPoints * scale * 2) / 2);
            var key = (baseFont.FontFamily.Name, size, baseFont.Style);

            if (!FontCache.TryGetValue(key, out var font))
            {
                font = new Font(baseFont.FontFamily, size, baseFont.Style, GraphicsUnit.Point);
                FontCache[key] = font;
            }

            return font;
        }

        private static int R(double v) => (int)Math.Round(v);

        // =======================
        // Zoom
        // =======================
        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (!e.Control) return;

            double? zoom = e.KeyCode switch
            {
                Keys.Oemplus or Keys.Add => _zoom + ZoomStep,
                Keys.OemMinus or Keys.Subtract => _zoom - ZoomStep,
                Keys.D0 or Keys.NumPad0 => 1.0,
                _ => null
            };

            if (zoom == null) return;

            SetZoom(zoom.Value);
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void SetZoom(double zoom)
        {
            _zoom = Math.Round(Math.Clamp(zoom, MinZoom, MaxZoom), 2);
            SaveZoom();
            ApplyLayout();
        }

        private sealed class CtrlWheelFilter : IMessageFilter
        {
            private const int WM_MOUSEWHEEL = 0x020A;

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != WM_MOUSEWHEEL || (Control.ModifierKeys & Keys.Control) == 0) return false;

                var scaler = Active.LastOrDefault(s => s._form == Form.ActiveForm);
                if (scaler == null) return false;

                int delta = (short)(((long)m.WParam >> 16) & 0xFFFF);
                scaler.SetZoom(_zoom + Math.Sign(delta) * ZoomStep);
                return true;
            }
        }

        private static string ZoomPath => Path.Combine(Storage.AppDataDir, "zoom.txt");

        private static double LoadZoom()
        {
            try
            {
                if (!File.Exists(ZoomPath)) return 1.0;
                return Math.Clamp(double.Parse(File.ReadAllText(ZoomPath), CultureInfo.InvariantCulture), MinZoom, MaxZoom);
            }
            catch
            {
                return 1.0; // unreadable file => default zoom
            }
        }

        private static void SaveZoom()
        {
            try
            {
                Storage.EnsureFolders();
                File.WriteAllText(ZoomPath, _zoom.ToString(CultureInfo.InvariantCulture));
            }
            catch
            {
                // ignore: zoom still works for this session
            }
        }
    }
}
