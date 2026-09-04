using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.Core.Scada;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Industrial
{
    /// <summary>
    /// Single-HWND high-performance SCADA plant mimic canvas.
    /// Solves the Windows 10,000 HWND limit trap by rendering hundreds of process elements
    /// (Tanks, Valves, Pumps, Pipes, Sensors) on a single virtual viewport with Pan, Zoom, and Viewport Culling.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [Description("Single-HWND high-performance P&ID plant mimic canvas with pan, zoom, and spatial culling")]
    public class ZeroPlantMimicCanvas : Control
    {
        private readonly List<IScadaDrawable> _elements = new List<IScadaDrawable>();
        private float _zoomFactor = 1.0f;
        private float _panOffsetX = 0f;
        private float _panOffsetY = 0f;
        private bool _isPanning = false;
        private Point _lastMousePos;
        private IScadaDrawable? _hoveredElement;
        private IScadaDrawable? _selectedElement;

        public event EventHandler<IScadaDrawable>? ElementClicked;

        [Category("Viewport")]
        [DefaultValue(1.0f)]
        public float ZoomFactor
        {
            get => _zoomFactor;
            set
            {
                _zoomFactor = Math.Max(0.25f, Math.Min(4.0f, value));
                Invalidate();
            }
        }

        [Category("Viewport")]
        [DefaultValue(0f)]
        public float PanOffsetX
        {
            get => _panOffsetX;
            set { _panOffsetX = value; Invalidate(); }
        }

        [Category("Viewport")]
        [DefaultValue(0f)]
        public float PanOffsetY
        {
            get => _panOffsetY;
            set { _panOffsetY = value; Invalidate(); }
        }

        [Browsable(false)]
        public List<IScadaDrawable> Elements => _elements;

        public ZeroPlantMimicCanvas()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.Selectable, true);

            Size = new Size(800, 500);
            BackColor = Color.FromArgb(15, 23, 42); // Industrial slate dark
        }

        public void AddElement(IScadaDrawable element)
        {
            if (element == null) return;
            if (!_elements.Contains(element))
            {
                _elements.Add(element);
                Invalidate();
            }
        }

        public void RemoveElement(IScadaDrawable element)
        {
            if (element == null) return;
            if (_elements.Remove(element))
            {
                Invalidate();
            }
        }

        public void ClearElements()
        {
            _elements.Clear();
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            if (e.Button == MouseButtons.Middle || (e.Button == MouseButtons.Left && ModifierKeys == Keys.Space))
            {
                _isPanning = true;
                _lastMousePos = e.Location;
                Cursor = Cursors.SizeAll;
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                var worldPt = ScreenToWorld(e.Location);
                IScadaDrawable? hit = FindElementAt(worldPt.X, worldPt.Y);

                if (_selectedElement != null) _selectedElement.IsSelected = false;
                _selectedElement = hit;
                if (_selectedElement != null)
                {
                    _selectedElement.IsSelected = true;
                    ElementClicked?.Invoke(this, _selectedElement);
                }

                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isPanning)
            {
                float dx = e.X - _lastMousePos.X;
                float dy = e.Y - _lastMousePos.Y;
                _panOffsetX += dx;
                _panOffsetY += dy;
                _lastMousePos = e.Location;
                Invalidate();
                return;
            }

            var worldPt = ScreenToWorld(e.Location);
            var hit = FindElementAt(worldPt.X, worldPt.Y);

            if (hit != _hoveredElement)
            {
                if (_hoveredElement != null) _hoveredElement.IsHovered = false;
                _hoveredElement = hit;
                if (_hoveredElement != null) _hoveredElement.IsHovered = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isPanning)
            {
                _isPanning = false;
                Cursor = Cursors.Default;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            float zoomDelta = e.Delta > 0 ? 1.15f : 0.85f;
            float newZoom = Math.Max(0.25f, Math.Min(4.0f, _zoomFactor * zoomDelta));

            // Zoom centered on mouse pointer
            float mouseWorldX = (e.X - _panOffsetX) / _zoomFactor;
            float mouseWorldY = (e.Y - _panOffsetY) / _zoomFactor;

            _panOffsetX = e.X - mouseWorldX * newZoom;
            _panOffsetY = e.Y - mouseWorldY * newZoom;
            _zoomFactor = newZoom;

            Invalidate();
        }

        private PointF ScreenToWorld(Point screenPt)
        {
            return new PointF(
                (screenPt.X - _panOffsetX) / _zoomFactor,
                (screenPt.Y - _panOffsetY) / _zoomFactor
            );
        }

        private IScadaDrawable? FindElementAt(float wx, float wy)
        {
            for (int i = _elements.Count - 1; i >= 0; i--)
            {
                var el = _elements[i];
                if (!el.IsVisible) continue;
                if (el.HitTest(wx, wy))
                {
                    return el;
                }
            }
            return null;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool isDark = ZeroTheme.IsDark;
            Color canvasBg = isDark ? Color.FromArgb(15, 23, 42) : Color.FromArgb(241, 245, 249);
            g.Clear(canvasBg);

            // 1. Draw Grid Lines (transformed)
            DrawGrid(g, isDark);

            // 2. Setup Viewport Matrix
            var matrix = new Matrix();
            matrix.Translate(_panOffsetX, _panOffsetY);
            matrix.Scale(_zoomFactor, _zoomFactor);
            g.Transform = matrix;

            // Viewport bounds in world coordinates for spatial culling
            float vwLeft = -_panOffsetX / _zoomFactor;
            float vwTop = -_panOffsetY / _zoomFactor;
            float vwRight = vwLeft + (Width / _zoomFactor);
            float vwBottom = vwTop + (Height / _zoomFactor);
            var viewportRect = new RectangleF(vwLeft, vwTop, vwRight - vwLeft, vwBottom - vwTop);

            // 3. Render Elements with Spatial Culling
            using (var elemBorderPen = new Pen(Color.FromArgb(59, 130, 246), 1.5f))
            using (var selPen = new Pen(Color.FromArgb(245, 158, 11), 2.5f) { DashStyle = DashStyle.Dash })
            using (var textBrush = new SolidBrush(isDark ? Color.White : Color.Black))
            using (var font = new Font("Segoe UI", 8f, FontStyle.Bold))
            {
                for (int i = 0; i < _elements.Count; i++)
                {
                    var el = _elements[i];
                    if (!el.IsVisible) continue;

                    var elBounds = new RectangleF(el.X, el.Y, el.Width, el.Height);

                    // Viewport Culling: Skip items that are outside visible view
                    if (!viewportRect.IntersectsWith(elBounds))
                        continue;

                    // Draw Element Container / Placeholder Vector
                    using (var bodyBrush = new SolidBrush(isDark ? Color.FromArgb(30, 41, 59) : Color.White))
                    {
                        g.FillRectangle(bodyBrush, elBounds);
                    }

                    if (el.IsSelected)
                    {
                        g.DrawRectangle(selPen, elBounds.X - 2, elBounds.Y - 2, elBounds.Width + 4, elBounds.Height + 4);
                    }
                    else
                    {
                        g.DrawRectangle(elemBorderPen, elBounds.X, elBounds.Y, elBounds.Width, elBounds.Height);
                    }

                    // Draw Tag Label
                    g.DrawString(el.Label, font, textBrush, el.X + 4, el.Y + 4);
                }
            }

            g.ResetTransform();

            // 4. Viewport HUD (Zoom & Element count readout)
            using (var hudFont = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var hudBrush = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                string hud = $"ZOOM: {_zoomFactor * 100:0}% | OBJECTS: {_elements.Count} | PAN: [SPACE+DRAG]";
                g.DrawString(hud, hudFont, hudBrush, 10, Height - 22);
            }
        }

        private void DrawGrid(Graphics g, bool isDark)
        {
            Color gridColor = isDark ? Color.FromArgb(25, 35, 55) : Color.FromArgb(226, 232, 240);
            using (var gridPen = new Pen(gridColor, 1f))
            {
                float gridSpacing = 40f * _zoomFactor;
                float startX = _panOffsetX % gridSpacing;
                float startY = _panOffsetY % gridSpacing;

                for (float x = startX; x < Width; x += gridSpacing)
                {
                    g.DrawLine(gridPen, x, 0, x, Height);
                }

                for (float y = startY; y < Height; y += gridSpacing)
                {
                    g.DrawLine(gridPen, 0, y, Width, y);
                }
            }
        }
    }
}
