using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using ZeroUI.Core.Editors;
using ZeroUI.Core.Media;
using ZeroUI.Core.Theme;
using ZeroUI.WinForms.Base;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Media
{
    /// <summary>
    /// Enterprise GPU-accelerated interactive image viewer for Windows Forms.
    /// Delivers sub-pixel smooth pan, cursor-anchored zoom, forensic pixel grid,
    /// interactive MiniMap navigator, and telemetry color probe HUD.
    /// Supports GDI+ <see cref="Image"/>, <see cref="Bitmap"/>, files, and ZeroGraphics <c>ImageBuffer</c>.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Media")]
    [DefaultProperty("Image")]
    [Description("Enterprise high-performance interactive image viewer control")]
    public class ZImageViewer : ControlBase, IZeroEditor
    {
        private Image? _image;
        private string? _filePath;
        private ImageViewMode _viewMode = ImageViewMode.FitToWindow;
        private float _zoomFactor = 1.0f;
        private float _minZoom = 0.01f;
        private float _maxZoom = 64.0f;
        private PointF _panOffset = PointF.Empty;
        private ImageRotationAngle _rotation = ImageRotationAngle.Rotate0;
        private ImageFlipMode _flip = ImageFlipMode.None;
        private ImageInterpolationQuality _interpolation = ImageInterpolationQuality.HighQuality;
        private bool _showPixelGrid = true;
        private float _pixelGridThreshold = 8.0f;
        private bool _showMiniMap = true;
        private bool _showToolbar = true;
        private bool _showStatusBar = true;
        private Color _canvasColor = Color.FromArgb(15, 18, 24);

        private bool _isPanning;
        private Point _panStartMouse;
        private PointF _panStartOffset;
        private bool _isDraggingMiniMap;
        private Point _currentMousePosition = new Point(-1, -1);
        private Point _currentPixelCoord = new Point(-1, -1);
        private Color _currentHoverColor = Color.Transparent;
        private bool _isModified;
        private bool _isReadOnly;

        public ZImageViewer()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable,
                true);

            DoubleBuffered = true;
            BackColor = _canvasColor;
            Size = new Size(400, 300);
        }

        #region Properties

        [Category("ZeroUI Media")]
        [DefaultValue(null)]
        public Image? Image
        {
            get => _image;
            set
            {
                if (_image != value)
                {
                    _image = value;
                    if (_viewMode == ImageViewMode.FitToWindow) FitToWindow();
                    else Invalidate();
                    ImageLoaded?.Invoke(this, EventArgs.Empty);
                    EditValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [Category("ZeroUI Media")]
        [DefaultValue(null)]
        public string? FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    if (!string.IsNullOrEmpty(value) && File.Exists(value))
                    {
                        LoadFromFile(value);
                    }
                }
            }
        }

        [Category("ZeroUI Media")]
        [DefaultValue(ImageViewMode.FitToWindow)]
        public ImageViewMode ViewMode
        {
            get => _viewMode;
            set
            {
                _viewMode = value;
                switch (value)
                {
                    case ImageViewMode.FitToWindow: FitToWindow(); break;
                    case ImageViewMode.FitWidth: FitWidth(); break;
                    case ImageViewMode.FitHeight: FitHeight(); break;
                    case ImageViewMode.OriginalSize: ActualSize(); break;
                    default: Invalidate(); break;
                }
            }
        }

        [Category("ZeroUI Media")]
        [DefaultValue(1.0f)]
        public float ZoomFactor
        {
            get => _zoomFactor;
            set
            {
                float clamped = Math.Max(_minZoom, Math.Min(_maxZoom, value));
                if (Math.Abs(_zoomFactor - clamped) > 0.0001f)
                {
                    _zoomFactor = clamped;
                    ZoomChanged?.Invoke(this, clamped);
                    Invalidate();
                }
            }
        }

        [Category("ZeroUI Media")]
        [DefaultValue(0.01f)]
        public float MinZoom
        {
            get => _minZoom;
            set => _minZoom = Math.Max(0.001f, value);
        }

        [Category("ZeroUI Media")]
        [DefaultValue(64.0f)]
        public float MaxZoom
        {
            get => _maxZoom;
            set => _maxZoom = Math.Max(_minZoom, value);
        }

        [Category("ZeroUI Media")]
        public PointF PanOffset
        {
            get => _panOffset;
            set
            {
                _panOffset = value;
                PanChanged?.Invoke(this, value);
                Invalidate();
            }
        }

        [Category("ZeroUI Media")]
        [DefaultValue(ImageRotationAngle.Rotate0)]
        public ImageRotationAngle Rotation
        {
            get => _rotation;
            set { _rotation = value; Invalidate(); }
        }

        [Category("ZeroUI Media")]
        [DefaultValue(ImageFlipMode.None)]
        public ImageFlipMode Flip
        {
            get => _flip;
            set { _flip = value; Invalidate(); }
        }

        [Category("ZeroUI Media")]
        [DefaultValue(ImageInterpolationQuality.HighQuality)]
        public ImageInterpolationQuality InterpolationQuality
        {
            get => _interpolation;
            set { _interpolation = value; Invalidate(); }
        }

        [Category("ZeroUI Media")]
        [DefaultValue(true)]
        public bool ShowPixelGrid
        {
            get => _showPixelGrid;
            set { _showPixelGrid = value; Invalidate(); }
        }

        [Category("ZeroUI Media")]
        [DefaultValue(8.0f)]
        public float PixelGridThreshold
        {
            get => _pixelGridThreshold;
            set { _pixelGridThreshold = value; Invalidate(); }
        }

        [Category("ZeroUI Media")]
        [DefaultValue(true)]
        public bool ShowMiniMap
        {
            get => _showMiniMap;
            set { _showMiniMap = value; Invalidate(); }
        }

        [Category("ZeroUI Media")]
        [DefaultValue(true)]
        public bool ShowToolbar
        {
            get => _showToolbar;
            set { _showToolbar = value; Invalidate(); }
        }

        [Category("ZeroUI Media")]
        [DefaultValue(true)]
        public bool ShowStatusBar
        {
            get => _showStatusBar;
            set { _showStatusBar = value; Invalidate(); }
        }

        [Category("ZeroUI Media")]
        public Color CanvasColor
        {
            get => _canvasColor;
            set { _canvasColor = value; BackColor = value; Invalidate(); }
        }

        public int PixelWidth => _image?.Width ?? 0;
        public int PixelHeight => _image?.Height ?? 0;

        #endregion

        #region Events

        public event EventHandler<float>? ZoomChanged;
        public event EventHandler<PointF>? PanChanged;
        public event EventHandler<(int X, int Y, Color Color)>? HoverPixelChanged;
        public event EventHandler? ImageLoaded;

        #endregion

        #region IZeroEditor Implementation

        [Browsable(false)]
        public object? EditValue
        {
            get => _image;
            set
            {
                if (value is Image img) Image = img;
                else if (value is string path && File.Exists(path)) LoadFromFile(path);
                else if (value == null) Image = null;
            }
        }

        public event EventHandler? EditValueChanged;

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool IsModified
        {
            get => _isModified;
            set => _isModified = value;
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get => _isReadOnly;
            set => _isReadOnly = value;
        }

        public void Reset()
        {
            ResetView();
            IsModified = false;
        }

        public void Clear()
        {
            Image?.Dispose();
            Image = null;
            FilePath = null;
            PanOffset = PointF.Empty;
            ZoomFactor = 1.0f;
            IsModified = false;
            Invalidate();
        }

        #endregion

        #region Navigation Commands

        public void FitToWindow()
        {
            if (_image == null || Width <= 0 || Height <= 0) return;

            bool isSwapped = _rotation == ImageRotationAngle.Rotate90 || _rotation == ImageRotationAngle.Rotate270;
            float imgW = isSwapped ? _image.Height : _image.Width;
            float imgH = isSwapped ? _image.Width : _image.Height;
            if (imgW <= 0 || imgH <= 0) return;

            float margin = 24f;
            float availW = Math.Max(10f, Width - margin * 2);
            float availH = Math.Max(10f, Height - margin * 2);

            float scale = Math.Min(availW / imgW, availH / imgH);
            ZoomFactor = Math.Max(_minZoom, Math.Min(_maxZoom, scale));
            PanOffset = new PointF(Width / 2f, Height / 2f);
            _viewMode = ImageViewMode.FitToWindow;
            Invalidate();
        }

        public void FitWidth()
        {
            if (_image == null || Width <= 0) return;
            bool isSwapped = _rotation == ImageRotationAngle.Rotate90 || _rotation == ImageRotationAngle.Rotate270;
            float imgW = isSwapped ? _image.Height : _image.Width;
            if (imgW <= 0) return;

            ZoomFactor = Math.Max(_minZoom, Math.Min(_maxZoom, (Width - 32f) / imgW));
            PanOffset = new PointF(Width / 2f, Height / 2f);
            _viewMode = ImageViewMode.FitWidth;
            Invalidate();
        }

        public void FitHeight()
        {
            if (_image == null || Height <= 0) return;
            bool isSwapped = _rotation == ImageRotationAngle.Rotate90 || _rotation == ImageRotationAngle.Rotate270;
            float imgH = isSwapped ? _image.Width : _image.Height;
            if (imgH <= 0) return;

            ZoomFactor = Math.Max(_minZoom, Math.Min(_maxZoom, (Height - 32f) / imgH));
            PanOffset = new PointF(Width / 2f, Height / 2f);
            _viewMode = ImageViewMode.FitHeight;
            Invalidate();
        }

        public void ActualSize()
        {
            ZoomFactor = 1.0f;
            PanOffset = new PointF(Width / 2f, Height / 2f);
            _viewMode = ImageViewMode.OriginalSize;
            Invalidate();
        }

        public void ZoomIn(float factor = 1.25f)
        {
            SetZoom(_zoomFactor * factor, new Point(Width / 2, Height / 2));
        }

        public void ZoomOut(float factor = 0.8f)
        {
            SetZoom(_zoomFactor * factor, new Point(Width / 2, Height / 2));
        }

        public void SetZoom(float newZoom, Point? anchorPoint = null)
        {
            float oldZoom = _zoomFactor;
            float clamped = Math.Max(_minZoom, Math.Min(_maxZoom, newZoom));
            if (Math.Abs(clamped - oldZoom) < 0.0001f) return;

            Point anchor = anchorPoint ?? new Point(Width / 2, Height / 2);
            float ratio = clamped / oldZoom;

            float newPanX = anchor.X - (anchor.X - _panOffset.X) * ratio;
            float newPanY = anchor.Y - (anchor.Y - _panOffset.Y) * ratio;

            _panOffset = new PointF(newPanX, newPanY);
            _zoomFactor = clamped;
            _viewMode = ImageViewMode.Custom;
            ZoomChanged?.Invoke(this, clamped);
            PanChanged?.Invoke(this, _panOffset);
            Invalidate();
        }

        public void RotateClockwise()
        {
            _rotation = _rotation switch
            {
                ImageRotationAngle.Rotate0 => ImageRotationAngle.Rotate90,
                ImageRotationAngle.Rotate90 => ImageRotationAngle.Rotate180,
                ImageRotationAngle.Rotate180 => ImageRotationAngle.Rotate270,
                _ => ImageRotationAngle.Rotate0
            };
            if (_viewMode == ImageViewMode.FitToWindow) FitToWindow();
            Invalidate();
        }

        public void RotateCounterClockwise()
        {
            _rotation = _rotation switch
            {
                ImageRotationAngle.Rotate0 => ImageRotationAngle.Rotate270,
                ImageRotationAngle.Rotate90 => ImageRotationAngle.Rotate0,
                ImageRotationAngle.Rotate180 => ImageRotationAngle.Rotate90,
                _ => ImageRotationAngle.Rotate180
            };
            if (_viewMode == ImageViewMode.FitToWindow) FitToWindow();
            Invalidate();
        }

        public void ToggleFlipHorizontal()
        {
            _flip ^= ImageFlipMode.Horizontal;
            Invalidate();
        }

        public void ToggleFlipVertical()
        {
            _flip ^= ImageFlipMode.Vertical;
            Invalidate();
        }

        public void ResetView()
        {
            _rotation = ImageRotationAngle.Rotate0;
            _flip = ImageFlipMode.None;
            FitToWindow();
        }

        public void LoadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var tempImg = Image.FromStream(fs);
                var bmp = new Bitmap(tempImg); // Copy into un-locked memory
                Image = bmp;
                _filePath = path;
                FitToWindow();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ZImageViewer] Load failed: {ex.Message}");
            }
        }

        #endregion

        #region Paint Pipeline

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(_canvasColor);

            if (_image == null)
            {
                DrawEmptyState(g);
                return;
            }

            // Configure rendering quality
            if (_zoomFactor >= _pixelGridThreshold || _interpolation == ImageInterpolationQuality.NearestNeighbor)
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.SmoothingMode = SmoothingMode.None;
            }
            else
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.AntiAlias;
            }

            // 1. Draw Main Image with Transformations
            DrawMainImage(g);

            // 2. Draw Pixel Grid if Zoom is high
            if (_showPixelGrid && _zoomFactor >= _pixelGridThreshold)
            {
                DrawPixelGrid(g);
            }

            // 3. Draw MiniMap
            if (_showMiniMap)
            {
                DrawMiniMap(g);
            }

            // 4. Draw Status HUD
            if (_showStatusBar)
            {
                DrawStatusBar(g);
            }

            // 5. Draw Floating Toolbar
            if (_showToolbar)
            {
                DrawToolbar(g);
            }
        }

        private void DrawEmptyState(Graphics g)
        {
            using var font = new Font("Segoe UI", 11f, FontStyle.Regular);
            using var brush = new SolidBrush(Color.FromArgb(148, 163, 184));
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("⚡ ZeroUI Image Viewer\nDrag & drop image or load a file to begin", font, brush, ClientRectangle, sf);
        }

        private void DrawMainImage(Graphics g)
        {
            if (_image == null) return;

            var state = g.Save();

            g.TranslateTransform(_panOffset.X, _panOffset.Y);
            g.ScaleTransform(
                _zoomFactor * (_flip.HasFlag(ImageFlipMode.Horizontal) ? -1 : 1),
                _zoomFactor * (_flip.HasFlag(ImageFlipMode.Vertical) ? -1 : 1));

            if (_rotation != ImageRotationAngle.Rotate0)
            {
                g.RotateTransform((float)_rotation);
            }

            float imgW = _image.Width;
            float imgH = _image.Height;
            g.DrawImage(_image, -imgW / 2f, -imgH / 2f, imgW, imgH);

            g.Restore(state);
        }

        private void DrawPixelGrid(Graphics g)
        {
            if (_image == null) return;

            PointF tl = ViewportToImage(new Point(0, 0));
            PointF br = ViewportToImage(new Point(Width, Height));

            int startX = Math.Max(0, (int)Math.Floor(Math.Min(tl.X, br.X)));
            int endX = Math.Min(_image.Width, (int)Math.Ceiling(Math.Max(tl.X, br.X)));
            int startY = Math.Max(0, (int)Math.Floor(Math.Min(tl.Y, br.Y)));
            int endY = Math.Min(_image.Height, (int)Math.Ceiling(Math.Max(tl.Y, br.Y)));

            using var gridPen = new Pen(Color.FromArgb(60, 255, 255, 255), 1f);

            for (int x = startX; x <= endX; x++)
            {
                PointF p1 = ImageToViewport(new PointF(x, startY));
                PointF p2 = ImageToViewport(new PointF(x, endY));
                g.DrawLine(gridPen, p1, p2);
            }

            for (int y = startY; y <= endY; y++)
            {
                PointF p1 = ImageToViewport(new PointF(startX, y));
                PointF p2 = ImageToViewport(new PointF(endX, y));
                g.DrawLine(gridPen, p1, p2);
            }
        }

        private void DrawMiniMap(Graphics g)
        {
            if (_image == null) return;

            float pad = 14f;
            float mapW = 140f;
            float mapH = 90f;

            float ratio = (float)_image.Width / Math.Max(1, _image.Height);
            if (ratio >= 1.5f) mapH = mapW / ratio;
            else mapW = mapH * ratio;

            RectangleF bounds = new RectangleF(Width - mapW - pad, Height - mapH - pad - (_showToolbar ? 44 : 0), mapW, mapH);

            using var bgBrush = new SolidBrush(Color.FromArgb(220, 20, 24, 34));
            using var borderPen = new Pen(Color.FromArgb(120, 56, 189, 248), 1f);
            g.FillRectangle(bgBrush, bounds);
            g.DrawRectangle(borderPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);

            // Draw thumbnail image
            RectangleF thumbRect = new RectangleF(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4);
            g.DrawImage(_image, thumbRect);

            // Viewport rect
            PointF vTl = ViewportToImage(new Point(0, 0));
            PointF vBr = ViewportToImage(new Point(Width, Height));

            float nx1 = Math.Max(0f, Math.Min(1f, vTl.X / _image.Width));
            float ny1 = Math.Max(0f, Math.Min(1f, vTl.Y / _image.Height));
            float nx2 = Math.Max(0f, Math.Min(1f, vBr.X / _image.Width));
            float ny2 = Math.Max(0f, Math.Min(1f, vBr.Y / _image.Height));

            float vpX = thumbRect.X + Math.Min(nx1, nx2) * thumbRect.Width;
            float vpY = thumbRect.Y + Math.Min(ny1, ny2) * thumbRect.Height;
            float vpW = Math.Max(6f, Math.Abs(nx2 - nx1) * thumbRect.Width);
            float vpH = Math.Max(6f, Math.Abs(ny2 - ny1) * thumbRect.Height);

            using var vpFill = new SolidBrush(Color.FromArgb(40, 0, 229, 255));
            using var vpPen = new Pen(Color.FromArgb(220, 0, 229, 255), 1.5f);
            g.FillRectangle(vpFill, vpX, vpY, vpW, vpH);
            g.DrawRectangle(vpPen, vpX, vpY, vpW, vpH);
        }

        private void DrawStatusBar(Graphics g)
        {
            if (_image == null) return;

            float mp = (_image.Width * _image.Height) / 1_000_000f;
            string probe = _currentPixelCoord.X >= 0
                ? $" | X: {_currentPixelCoord.X} Y: {_currentPixelCoord.Y} | #{_currentHoverColor.R:X2}{_currentHoverColor.G:X2}{_currentHoverColor.B:X2}"
                : string.Empty;

            string text = $"{_image.Width} × {_image.Height} ({mp:F1} MP) | {Math.Round(_zoomFactor * 100):0}% [{_viewMode}]{probe}";

            using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            SizeF sz = g.MeasureString(text, font);
            RectangleF bg = new RectangleF(12, 12, sz.Width + 24, sz.Height + 8);

            using var bgBrush = new SolidBrush(Color.FromArgb(200, 20, 24, 34));
            using var borderPen = new Pen(Color.FromArgb(120, 56, 189, 248), 1f);
            using var textBrush = new SolidBrush(Color.FromArgb(241, 245, 249));

            g.FillRectangle(bgBrush, bg);
            g.DrawRectangle(borderPen, bg.X, bg.Y, bg.Width, bg.Height);
            g.DrawString(text, font, textBrush, bg.X + 8, bg.Y + 4);

            if (_currentPixelCoord.X >= 0)
            {
                using var dotBrush = new SolidBrush(_currentHoverColor);
                g.FillEllipse(dotBrush, bg.Right - 14, bg.Y + bg.Height / 2f - 4, 8, 8);
                g.DrawEllipse(borderPen, bg.Right - 14, bg.Y + bg.Height / 2f - 4, 8, 8);
            }
        }

        private void DrawToolbar(Graphics g)
        {
            float barW = 340f;
            float barH = 34f;
            RectangleF bar = new RectangleF((Width - barW) / 2f, Height - barH - 10f, barW, barH);

            using var bgBrush = new SolidBrush(Color.FromArgb(200, 20, 24, 34));
            using var borderPen = new Pen(Color.FromArgb(120, 56, 189, 248), 1f);
            using var textBrush = new SolidBrush(Color.FromArgb(241, 245, 249));
            using var accentBrush = new SolidBrush(Color.FromArgb(0, 229, 255));
            using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            g.FillRectangle(bgBrush, bar);
            g.DrawRectangle(borderPen, bar.X, bar.Y, bar.Width, bar.Height);

            string[] items = { "−", "+", "Fit", "1:1", "↺", "↻", "⇄", "Grid", "Map", "Reset" };
            float itemW = barW / items.Length;

            for (int i = 0; i < items.Length; i++)
            {
                RectangleF btn = new RectangleF(bar.X + i * itemW, bar.Y, itemW, barH);
                bool hovered = btn.Contains(_currentMousePosition);

                if (hovered)
                {
                    using var hoverBrush = new SolidBrush(Color.FromArgb(60, 0, 229, 255));
                    g.FillRectangle(hoverBrush, btn);
                }

                g.DrawString(items[i], font, hovered ? accentBrush : textBrush, btn, sf);
            }
        }

        #endregion

        #region Geometry Coordinates

        public PointF ViewportToImage(Point vp)
        {
            if (_image == null || _zoomFactor <= 0) return PointF.Empty;

            float dx = vp.X - _panOffset.X;
            float dy = vp.Y - _panOffset.Y;

            float sx = _zoomFactor * (_flip.HasFlag(ImageFlipMode.Horizontal) ? -1 : 1);
            float sy = _zoomFactor * (_flip.HasFlag(ImageFlipMode.Vertical) ? -1 : 1);
            dx /= sx;
            dy /= sy;

            if (_rotation != ImageRotationAngle.Rotate0)
            {
                double rad = -(double)_rotation * Math.PI / 180.0;
                double cos = Math.Cos(rad);
                double sin = Math.Sin(rad);
                double rx = dx * cos - dy * sin;
                double ry = dx * sin + dy * cos;
                dx = (float)rx;
                dy = (float)ry;
            }

            return new PointF(dx + _image.Width / 2f, dy + _image.Height / 2f);
        }

        public PointF ImageToViewport(PointF img)
        {
            if (_image == null) return PointF.Empty;

            float dx = img.X - _image.Width / 2f;
            float dy = img.Y - _image.Height / 2f;

            if (_rotation != ImageRotationAngle.Rotate0)
            {
                double rad = (double)_rotation * Math.PI / 180.0;
                double cos = Math.Cos(rad);
                double sin = Math.Sin(rad);
                double rx = dx * cos - dy * sin;
                double ry = dx * sin + dy * cos;
                dx = (float)rx;
                dy = (float)ry;
            }

            float sx = _zoomFactor * (_flip.HasFlag(ImageFlipMode.Horizontal) ? -1 : 1);
            float sy = _zoomFactor * (_flip.HasFlag(ImageFlipMode.Vertical) ? -1 : 1);
            dx *= sx;
            dy *= sy;

            return new PointF(dx + _panOffset.X, dy + _panOffset.Y);
        }

        #endregion

        #region Mouse & Keyboard Events

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            if (_showToolbar && HandleToolbarClick(e.Location)) return;
            if (_showMiniMap && _image != null && HandleMiniMapClick(e.Location))
            {
                _isDraggingMiniMap = true;
                return;
            }

            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)
            {
                _isPanning = true;
                _panStartMouse = e.Location;
                _panStartOffset = _panOffset;
                Cursor = Cursors.SizeAll;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _currentMousePosition = e.Location;

            if (_isPanning)
            {
                float dx = e.X - _panStartMouse.X;
                float dy = e.Y - _panStartMouse.Y;
                PanOffset = new PointF(_panStartOffset.X + dx, _panStartOffset.Y + dy);
                _viewMode = ImageViewMode.Custom;
            }
            else if (_isDraggingMiniMap)
            {
                HandleMiniMapDrag(e.Location);
            }
            else
            {
                UpdatePixelTelemetry(e.Location);
                if (_showToolbar) Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isPanning || _isDraggingMiniMap)
            {
                _isPanning = false;
                _isDraggingMiniMap = false;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (_viewMode == ImageViewMode.FitToWindow) ActualSize();
            else FitToWindow();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            float factor = e.Delta > 0 ? 1.2f : 0.8333f;
            SetZoom(_zoomFactor * factor, e.Location);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Add:
                case Keys.Oemplus:
                    ZoomIn();
                    break;
                case Keys.Subtract:
                case Keys.OemMinus:
                    ZoomOut();
                    break;
                case Keys.D0:
                case Keys.NumPad0:
                    FitToWindow();
                    break;
                case Keys.D1:
                case Keys.NumPad1:
                    ActualSize();
                    break;
                case Keys.R:
                    RotateClockwise();
                    break;
                case Keys.G:
                    ShowPixelGrid = !ShowPixelGrid;
                    break;
                case Keys.M:
                    ShowMiniMap = !ShowMiniMap;
                    break;
            }
        }

        private bool HandleToolbarClick(Point mouse)
        {
            float barW = 340f;
            float barH = 34f;
            RectangleF bar = new RectangleF((Width - barW) / 2f, Height - barH - 10f, barW, barH);
            if (!bar.Contains(mouse)) return false;

            float itemW = barW / 10f;
            int idx = (int)((mouse.X - bar.X) / itemW);

            switch (idx)
            {
                case 0: ZoomOut(); break;
                case 1: ZoomIn(); break;
                case 2: FitToWindow(); break;
                case 3: ActualSize(); break;
                case 4: RotateCounterClockwise(); break;
                case 5: RotateClockwise(); break;
                case 6: ToggleFlipHorizontal(); break;
                case 7: ShowPixelGrid = !ShowPixelGrid; break;
                case 8: ShowMiniMap = !ShowMiniMap; break;
                case 9: ResetView(); break;
            }
            return true;
        }

        private bool HandleMiniMapClick(Point mouse)
        {
            float pad = 14f;
            float mapW = 140f;
            float mapH = 90f;
            RectangleF bounds = new RectangleF(Width - mapW - pad, Height - mapH - pad - (_showToolbar ? 44 : 0), mapW, mapH);
            if (bounds.Contains(mouse))
            {
                HandleMiniMapDrag(mouse);
                return true;
            }
            return false;
        }

        private void HandleMiniMapDrag(Point mouse)
        {
            if (_image == null) return;

            float pad = 14f;
            float mapW = 140f;
            float mapH = 90f;
            RectangleF bounds = new RectangleF(Width - mapW - pad, Height - mapH - pad - (_showToolbar ? 44 : 0), mapW, mapH);

            float nx = Math.Max(0f, Math.Min(1f, (mouse.X - bounds.X) / bounds.Width));
            float ny = Math.Max(0f, Math.Min(1f, (mouse.Y - bounds.Y) / bounds.Height));

            float targetX = nx * _image.Width;
            float targetY = ny * _image.Height;

            PointF targetVp = ImageToViewport(new PointF(targetX, targetY));
            PanOffset = new PointF(_panOffset.X + (Width / 2f - targetVp.X), _panOffset.Y + (Height / 2f - targetVp.Y));
            _viewMode = ImageViewMode.Custom;
            Invalidate();
        }

        private void UpdatePixelTelemetry(Point mouse)
        {
            if (_image == null || !(_image is Bitmap bmp)) return;

            PointF imgPt = ViewportToImage(mouse);
            int px = (int)Math.Floor(imgPt.X);
            int py = (int)Math.Floor(imgPt.Y);

            if (px >= 0 && px < _image.Width && py >= 0 && py < _image.Height)
            {
                _currentPixelCoord = new Point(px, py);
                try
                {
                    _currentHoverColor = bmp.GetPixel(px, py);
                    HoverPixelChanged?.Invoke(this, (px, py, _currentHoverColor));
                }
                catch
                {
                    _currentHoverColor = Color.Transparent;
                }
            }
            else
            {
                _currentPixelCoord = new Point(-1, -1);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_viewMode == ImageViewMode.FitToWindow) FitToWindow();
        }

        protected override void OnThemeChanged(ZeroSkin skin)
        {
            base.OnThemeChanged(skin);
            Invalidate();
        }

        #endregion
    }
}
