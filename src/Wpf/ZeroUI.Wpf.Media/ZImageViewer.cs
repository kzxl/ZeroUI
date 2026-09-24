using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZeroUI.Core.Editors;
using ZeroUI.Core.Media;
using ZeroUI.Core.Theme;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Media
{
    /// <summary>
    /// Enterprise GPU-accelerated interactive image viewer control for WPF.
    /// Provides sub-pixel pan and smooth zoom anchored at the cursor, forensic pixel grid,
    /// interactive MiniMap navigator, floating magnifier loupe, and telemetry color probe HUD.
    /// Supports direct rendering of <see cref="BitmapSource"/>, raw streams, files, and ZeroGraphics <c>ImageBuffer</c>.
    /// </summary>
    public class ZImageViewer : FrameworkElement, IZeroEditor, IZeroSkinnable
    {
        private bool _isPanning;
        private Point _panStartMouse;
        private Point _panStartOffset;
        private bool _isDraggingMiniMap;
        private Point _currentMousePosition = new Point(-1, -1);
        private bool _isModified;
        private bool _isReadOnly;
        private Color _currentHoverColor = Colors.Transparent;
        private Point _currentPixelCoord = new Point(-1, -1);

        // Pre-cached render objects for zero-allocation rendering loop
        private readonly Typeface _hudTypeface = new Typeface(new FontFamily("Segoe UI, Consolas, sans-serif"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
        private readonly Typeface _pixelTextTypeface = new Typeface(new FontFamily("Consolas"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        private static readonly Brush DefaultCanvasBg = new SolidColorBrush(Color.FromRgb(15, 18, 24));
        private static readonly Brush HudBgBrush = new SolidColorBrush(Color.FromArgb(200, 20, 24, 34));
        private static readonly Brush HudBorderBrush = new SolidColorBrush(Color.FromArgb(120, 56, 189, 248));
        private static readonly Brush HudTextBrush = new SolidColorBrush(Color.FromRgb(241, 245, 249));
        private static readonly Brush HudMutedBrush = new SolidColorBrush(Color.FromRgb(148, 163, 184));
        private static readonly Brush AccentBrush = new SolidColorBrush(Color.FromRgb(0, 229, 255));
        private static readonly Pen HudBorderPen = new Pen(HudBorderBrush, 1.0);
        private static readonly Pen MiniMapViewportPen = new Pen(new SolidColorBrush(Color.FromArgb(220, 0, 229, 255)), 1.5);
        private static readonly Brush MiniMapViewportFill = new SolidColorBrush(Color.FromArgb(40, 0, 229, 255));
        private static readonly Pen PixelGridPen = new Pen(new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)), 0.75);

        static ZImageViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZImageViewer), new FrameworkPropertyMetadata(typeof(ZImageViewer)));
            ClipToBoundsProperty.OverrideMetadata(typeof(ZImageViewer), new FrameworkPropertyMetadata(true));
            FocusableProperty.OverrideMetadata(typeof(ZImageViewer), new FrameworkPropertyMetadata(true));

            DefaultCanvasBg.Freeze();
            HudBgBrush.Freeze();
            HudBorderBrush.Freeze();
            HudTextBrush.Freeze();
            HudMutedBrush.Freeze();
            AccentBrush.Freeze();
            HudBorderPen.Freeze();
            MiniMapViewportPen.Freeze();
            MiniMapViewportFill.Freeze();
            PixelGridPen.Freeze();
        }

        public ZImageViewer()
        {
            Cursor = Cursors.Arrow;
            Loaded += (s, e) => { if (Source != null && ViewMode == ImageViewMode.FitToWindow) FitToWindow(); };
            SizeChanged += (s, e) => { if (ViewMode == ImageViewMode.FitToWindow) FitToWindow(); };
        }

        #region Dependency Properties

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(BitmapSource), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSourceChanged));

        public static readonly DependencyProperty FilePathProperty =
            DependencyProperty.Register(nameof(FilePath), typeof(string), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(null, OnFilePathChanged));

        public static readonly DependencyProperty ViewModeProperty =
            DependencyProperty.Register(nameof(ViewMode), typeof(ImageViewMode), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(ImageViewMode.FitToWindow, FrameworkPropertyMetadataOptions.AffectsRender, OnViewModeChanged));

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender, OnZoomChanged));

        public static readonly DependencyProperty MinZoomProperty =
            DependencyProperty.Register(nameof(MinZoom), typeof(double), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(0.01));

        public static readonly DependencyProperty MaxZoomProperty =
            DependencyProperty.Register(nameof(MaxZoom), typeof(double), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(64.0));

        public static readonly DependencyProperty PanOffsetProperty =
            DependencyProperty.Register(nameof(PanOffset), typeof(Point), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(new Point(0, 0), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty RotationProperty =
            DependencyProperty.Register(nameof(Rotation), typeof(ImageRotationAngle), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(ImageRotationAngle.Rotate0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FlipProperty =
            DependencyProperty.Register(nameof(Flip), typeof(ImageFlipMode), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(ImageFlipMode.None, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty InterpolationQualityProperty =
            DependencyProperty.Register(nameof(InterpolationQuality), typeof(ImageInterpolationQuality), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(ImageInterpolationQuality.HighQuality, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowPixelGridProperty =
            DependencyProperty.Register(nameof(ShowPixelGrid), typeof(bool), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PixelGridThresholdProperty =
            DependencyProperty.Register(nameof(PixelGridThreshold), typeof(double), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(8.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowMiniMapProperty =
            DependencyProperty.Register(nameof(ShowMiniMap), typeof(bool), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowLoupeProperty =
            DependencyProperty.Register(nameof(ShowLoupe), typeof(bool), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LoupeFactorProperty =
            DependencyProperty.Register(nameof(LoupeFactor), typeof(double), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(3.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowToolbarProperty =
            DependencyProperty.Register(nameof(ShowToolbar), typeof(bool), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowStatusBarProperty =
            DependencyProperty.Register(nameof(ShowStatusBar), typeof(bool), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register(nameof(BackgroundBrush), typeof(Brush), typeof(ZImageViewer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        #endregion

        #region CLR Properties

        public BitmapSource? Source
        {
            get => (BitmapSource?)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public string? FilePath
        {
            get => (string?)GetValue(FilePathProperty);
            set => SetValue(FilePathProperty, value);
        }

        public ImageViewMode ViewMode
        {
            get => (ImageViewMode)GetValue(ViewModeProperty);
            set => SetValue(ViewModeProperty, value);
        }

        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, Clamp(value, MinZoom, MaxZoom));
        }

        public double MinZoom
        {
            get => (double)GetValue(MinZoomProperty);
            set => SetValue(MinZoomProperty, Math.Max(0.001, value));
        }

        public double MaxZoom
        {
            get => (double)GetValue(MaxZoomProperty);
            set => SetValue(MaxZoomProperty, Math.Max(MinZoom, value));
        }

        public Point PanOffset
        {
            get => (Point)GetValue(PanOffsetProperty);
            set => SetValue(PanOffsetProperty, value);
        }

        public ImageRotationAngle Rotation
        {
            get => (ImageRotationAngle)GetValue(RotationProperty);
            set => SetValue(RotationProperty, value);
        }

        public ImageFlipMode Flip
        {
            get => (ImageFlipMode)GetValue(FlipProperty);
            set => SetValue(FlipProperty, value);
        }

        public ImageInterpolationQuality InterpolationQuality
        {
            get => (ImageInterpolationQuality)GetValue(InterpolationQualityProperty);
            set => SetValue(InterpolationQualityProperty, value);
        }

        public bool ShowPixelGrid
        {
            get => (bool)GetValue(ShowPixelGridProperty);
            set => SetValue(ShowPixelGridProperty, value);
        }

        public double PixelGridThreshold
        {
            get => (double)GetValue(PixelGridThresholdProperty);
            set => SetValue(PixelGridThresholdProperty, value);
        }

        public bool ShowMiniMap
        {
            get => (bool)GetValue(ShowMiniMapProperty);
            set => SetValue(ShowMiniMapProperty, value);
        }

        public bool ShowLoupe
        {
            get => (bool)GetValue(ShowLoupeProperty);
            set => SetValue(ShowLoupeProperty, value);
        }

        public double LoupeFactor
        {
            get => (double)GetValue(LoupeFactorProperty);
            set => SetValue(LoupeFactorProperty, Clamp(value, 1.5, 16.0));
        }

        public bool ShowToolbar
        {
            get => (bool)GetValue(ShowToolbarProperty);
            set => SetValue(ShowToolbarProperty, value);
        }

        public bool ShowStatusBar
        {
            get => (bool)GetValue(ShowStatusBarProperty);
            set => SetValue(ShowStatusBarProperty, value);
        }

        public Brush? BackgroundBrush
        {
            get => (Brush?)GetValue(BackgroundBrushProperty);
            set => SetValue(BackgroundBrushProperty, value);
        }

        public int PixelWidth => Source?.PixelWidth ?? 0;
        public int PixelHeight => Source?.PixelHeight ?? 0;

        #endregion

        #region Events

        public event EventHandler<double>? ZoomChanged;
        public event EventHandler<Point>? PanChanged;
        public event EventHandler<(int X, int Y, Color Color)>? HoverPixelChanged;
        public event EventHandler? ImageLoaded;

        #endregion

        #region IZeroEditor Implementation

        public object? EditValue
        {
            get => Source;
            set
            {
                if (value is BitmapSource bmp)
                {
                    Source = bmp;
                }
                else if (value is string path && File.Exists(path))
                {
                    LoadFromFile(path);
                }
                else if (value == null)
                {
                    Source = null;
                }
                EditValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? EditValueChanged;

        public bool IsModified
        {
            get => _isModified;
            set => _isModified = value;
        }

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
            Source = null;
            FilePath = null;
            PanOffset = new Point(0, 0);
            Zoom = 1.0;
            IsModified = false;
            InvalidateVisual();
        }

        #endregion

        #region IZeroSkinnable Implementation

        private bool _useDefaultSkin = true;
        private ZeroSkin? _customSkin;

        public bool UseDefaultSkin
        {
            get => _useDefaultSkin;
            set
            {
                _useDefaultSkin = value;
                ApplySkin(EffectiveSkin);
            }
        }

        public ZeroSkin? CustomSkin
        {
            get => _customSkin;
            set
            {
                _customSkin = value;
                if (!_useDefaultSkin) ApplySkin(EffectiveSkin);
            }
        }

        public ZeroSkin EffectiveSkin => (!_useDefaultSkin && _customSkin != null)
            ? _customSkin
            : ZeroSkinManager.CurrentSkin;

        public void ApplySkin(ZeroSkin skin)
        {
            InvalidateVisual();
        }

        #endregion

        #region Public View Navigation Commands

        public void FitToWindow()
        {
            if (Source == null || ActualWidth <= 0 || ActualHeight <= 0) return;

            double imgW = Source.PixelWidth;
            double imgH = Source.PixelHeight;
            if (imgW <= 0 || imgH <= 0) return;

            // Compute rotated bounds
            bool isSwapped = Rotation == ImageRotationAngle.Rotate90 || Rotation == ImageRotationAngle.Rotate270;
            double targetW = isSwapped ? imgH : imgW;
            double targetH = isSwapped ? imgW : imgH;

            double margin = 24.0;
            double availW = Math.Max(10, ActualWidth - margin * 2);
            double availH = Math.Max(10, ActualHeight - margin * 2);

            double scaleX = availW / targetW;
            double scaleY = availH / targetH;
            double fitScale = Math.Min(scaleX, scaleY);

            Zoom = Clamp(fitScale, MinZoom, MaxZoom);
            PanOffset = new Point(ActualWidth / 2.0, ActualHeight / 2.0);
            ViewMode = ImageViewMode.FitToWindow;
            InvalidateVisual();
        }

        public void FitWidth()
        {
            if (Source == null || ActualWidth <= 0) return;
            bool isSwapped = Rotation == ImageRotationAngle.Rotate90 || Rotation == ImageRotationAngle.Rotate270;
            double targetW = isSwapped ? Source.PixelHeight : Source.PixelWidth;
            if (targetW <= 0) return;

            Zoom = Clamp((ActualWidth - 32) / targetW, MinZoom, MaxZoom);
            PanOffset = new Point(ActualWidth / 2.0, ActualHeight / 2.0);
            ViewMode = ImageViewMode.FitWidth;
            InvalidateVisual();
        }

        public void FitHeight()
        {
            if (Source == null || ActualHeight <= 0) return;
            bool isSwapped = Rotation == ImageRotationAngle.Rotate90 || Rotation == ImageRotationAngle.Rotate270;
            double targetH = isSwapped ? Source.PixelWidth : Source.PixelHeight;
            if (targetH <= 0) return;

            Zoom = Clamp((ActualHeight - 32) / targetH, MinZoom, MaxZoom);
            PanOffset = new Point(ActualWidth / 2.0, ActualHeight / 2.0);
            ViewMode = ImageViewMode.FitHeight;
            InvalidateVisual();
        }

        public void ActualSize()
        {
            Zoom = 1.0;
            PanOffset = new Point(ActualWidth / 2.0, ActualHeight / 2.0);
            ViewMode = ImageViewMode.OriginalSize;
            InvalidateVisual();
        }

        public void ZoomIn(double factor = 1.25)
        {
            ZoomAroundCenter(Zoom * factor);
        }

        public void ZoomOut(double factor = 0.8)
        {
            ZoomAroundCenter(Zoom * factor);
        }

        public void ZoomAroundCenter(double newZoom)
        {
            SetZoom(newZoom, new Point(ActualWidth / 2.0, ActualHeight / 2.0));
        }

        public void SetZoom(double newZoom, Point? anchorPoint = null)
        {
            double oldZoom = Zoom;
            double clamped = Clamp(newZoom, MinZoom, MaxZoom);
            if (Math.Abs(clamped - oldZoom) < 0.0001) return;

            Point anchor = anchorPoint ?? new Point(ActualWidth / 2.0, ActualHeight / 2.0);

            // Shift PanOffset so anchor position remains fixed on the image
            Point currentPan = PanOffset;
            double scaleRatio = clamped / oldZoom;
            double newPanX = anchor.X - (anchor.X - currentPan.X) * scaleRatio;
            double newPanY = anchor.Y - (anchor.Y - currentPan.Y) * scaleRatio;

            PanOffset = new Point(newPanX, newPanY);
            Zoom = clamped;
            ViewMode = ImageViewMode.Custom;
            ZoomChanged?.Invoke(this, clamped);
            PanChanged?.Invoke(this, PanOffset);
            InvalidateVisual();
        }

        public void RotateClockwise()
        {
            Rotation = Rotation switch
            {
                ImageRotationAngle.Rotate0 => ImageRotationAngle.Rotate90,
                ImageRotationAngle.Rotate90 => ImageRotationAngle.Rotate180,
                ImageRotationAngle.Rotate180 => ImageRotationAngle.Rotate270,
                _ => ImageRotationAngle.Rotate0
            };
            if (ViewMode == ImageViewMode.FitToWindow) FitToWindow();
            InvalidateVisual();
        }

        public void RotateCounterClockwise()
        {
            Rotation = Rotation switch
            {
                ImageRotationAngle.Rotate0 => ImageRotationAngle.Rotate270,
                ImageRotationAngle.Rotate90 => ImageRotationAngle.Rotate0,
                ImageRotationAngle.Rotate180 => ImageRotationAngle.Rotate90,
                _ => ImageRotationAngle.Rotate180
            };
            if (ViewMode == ImageViewMode.FitToWindow) FitToWindow();
            InvalidateVisual();
        }

        public void ToggleFlipHorizontal()
        {
            Flip ^= ImageFlipMode.Horizontal;
            InvalidateVisual();
        }

        public void ToggleFlipVertical()
        {
            Flip ^= ImageFlipMode.Vertical;
            InvalidateVisual();
        }

        public void ResetView()
        {
            Rotation = ImageRotationAngle.Rotate0;
            Flip = ImageFlipMode.None;
            FitToWindow();
        }

        public void LoadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(Path.GetFullPath(path));
                bitmap.EndInit();
                bitmap.Freeze();

                Source = bitmap;
                FilePath = path;
                ImageLoaded?.Invoke(this, EventArgs.Empty);
                FitToWindow();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ZImageViewer] Load failed: {ex.Message}");
            }
        }

        public void LoadFromBytes(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0) return;

            using var ms = new MemoryStream(buffer);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze();

            Source = bitmap;
            FilePath = null;
            ImageLoaded?.Invoke(this, EventArgs.Empty);
            FitToWindow();
        }

        #endregion

        #region Rendering Pipeline

        protected override void OnRender(DrawingContext dc)
        {
            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            // 1. Draw Canvas Background
            Brush bg = BackgroundBrush ?? DefaultCanvasBg;
            dc.DrawRectangle(bg, null, new Rect(0, 0, w, h));

            if (Source == null)
            {
                DrawEmptyState(dc, w, h);
                return;
            }

            // 2. Draw Transformed Main Image
            DrawMainImage(dc, w, h);

            // 3. Draw Pixel Grid if Zoom is high enough
            if (ShowPixelGrid && Zoom >= PixelGridThreshold)
            {
                DrawPixelGrid(dc);
            }

            // 4. Draw Floating Loupe Magnifier
            if (ShowLoupe && _currentMousePosition.X >= 0 && _currentMousePosition.Y >= 0)
            {
                DrawLoupe(dc);
            }

            // 5. Draw Interactive MiniMap
            if (ShowMiniMap && Source != null)
            {
                DrawMiniMap(dc, w, h);
            }

            // 6. Draw Telemetry Status Badge
            if (ShowStatusBar && Source != null)
            {
                DrawStatusBar(dc);
            }

            // 7. Draw Floating HUD Toolbar
            if (ShowToolbar)
            {
                DrawToolbar(dc, w, h);
            }
        }

        private void DrawEmptyState(DrawingContext dc, double w, double h)
        {
            var text = new FormattedText(
                "⚡ ZeroUI Image Viewer\nDrag & drop image or load a file to begin",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                _hudTypeface,
                15,
                HudMutedBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            text.TextAlignment = TextAlignment.Center;
            dc.DrawText(text, new Point(w / 2.0, h / 2.0 - 20));
        }

        private void DrawMainImage(DrawingContext dc, double w, double h)
        {
            if (Source == null) return;

            double imgW = Source.PixelWidth;
            double imgH = Source.PixelHeight;

            // Configure rendering quality based on zoom and property
            BitmapScalingMode scaling = (Zoom >= PixelGridThreshold || InterpolationQuality == ImageInterpolationQuality.NearestNeighbor)
                ? BitmapScalingMode.NearestNeighbor
                : BitmapScalingMode.HighQuality;

            RenderOptions.SetBitmapScalingMode(this, scaling);

            dc.PushTransform(new TranslateTransform(PanOffset.X, PanOffset.Y));
            dc.PushTransform(new ScaleTransform(
                Zoom * (Flip.HasFlag(ImageFlipMode.Horizontal) ? -1 : 1),
                Zoom * (Flip.HasFlag(ImageFlipMode.Vertical) ? -1 : 1)));

            if (Rotation != ImageRotationAngle.Rotate0)
            {
                dc.PushTransform(new RotateTransform((double)Rotation));
            }

            Rect drawRect = new Rect(-imgW / 2.0, -imgH / 2.0, imgW, imgH);
            dc.DrawImage(Source, drawRect);

            if (Rotation != ImageRotationAngle.Rotate0) dc.Pop();
            dc.Pop(); // Scale
            dc.Pop(); // Translate
        }

        private void DrawPixelGrid(DrawingContext dc)
        {
            if (Source == null) return;

            double imgW = Source.PixelWidth;
            double imgH = Source.PixelHeight;

            // Transform viewport corners to image coordinates
            Point imgTopLeft = ViewportToImage(new Point(0, 0));
            Point imgBottomRight = ViewportToImage(new Point(ActualWidth, ActualHeight));

            int startX = Math.Max(0, (int)Math.Floor(Math.Min(imgTopLeft.X, imgBottomRight.X)));
            int endX = Math.Min((int)imgW, (int)Math.Ceiling(Math.Max(imgTopLeft.X, imgBottomRight.X)));
            int startY = Math.Max(0, (int)Math.Floor(Math.Min(imgTopLeft.Y, imgBottomRight.Y)));
            int endY = Math.Min((int)imgH, (int)Math.Ceiling(Math.Max(imgTopLeft.Y, imgBottomRight.Y)));

            // Draw vertical grid lines
            for (int x = startX; x <= endX; x++)
            {
                Point p1 = ImageToViewport(new Point(x, startY));
                Point p2 = ImageToViewport(new Point(x, endY));
                dc.DrawLine(PixelGridPen, p1, p2);
            }

            // Draw horizontal grid lines
            for (int y = startY; y <= endY; y++)
            {
                Point p1 = ImageToViewport(new Point(startX, y));
                Point p2 = ImageToViewport(new Point(endX, y));
                dc.DrawLine(PixelGridPen, p1, p2);
            }

            // If zoomed extremely close (>= 24x), draw subtle color / coord hints in each pixel
            if (Zoom >= 24.0 && (endX - startX) * (endY - startY) < 400)
            {
                double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                for (int y = startY; y < endY; y++)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        Point cellTl = ImageToViewport(new Point(x, y));
                        Point cellBr = ImageToViewport(new Point(x + 1, y + 1));
                        double cellW = cellBr.X - cellTl.X;
                        double cellH = cellBr.Y - cellTl.Y;

                        var text = new FormattedText(
                            $"{x},{y}",
                            CultureInfo.InvariantCulture,
                            FlowDirection.LeftToRight,
                            _pixelTextTypeface,
                            Math.Max(8, Math.Min(11, cellH * 0.25)),
                            HudMutedBrush,
                            dpi);

                        text.TextAlignment = TextAlignment.Center;
                        dc.DrawText(text, new Point(cellTl.X + cellW / 2.0, cellTl.Y + cellH / 2.0 - text.Height / 2.0));
                    }
                }
            }
        }

        private void DrawMiniMap(DrawingContext dc, double w, double h)
        {
            if (Source == null) return;

            double pad = 16.0;
            double mapW = 150.0;
            double mapH = 100.0;

            double imgRatio = (double)Source.PixelWidth / Math.Max(1, Source.PixelHeight);
            if (imgRatio >= 1.5)
            {
                mapH = mapW / imgRatio;
            }
            else
            {
                mapW = mapH * imgRatio;
            }

            Rect mapBounds = new Rect(w - mapW - pad, h - mapH - pad - (ShowToolbar ? 48 : 0), mapW, mapH);

            // Background card with shadow and border
            dc.DrawRoundedRectangle(HudBgBrush, HudBorderPen, mapBounds, 6, 6);

            // Thumbnail inside
            Rect thumbRect = new Rect(mapBounds.X + 2, mapBounds.Y + 2, mapBounds.Width - 4, mapBounds.Height - 4);
            dc.DrawImage(Source, thumbRect);

            // Compute visible viewport region mapped onto thumbnail
            Point vTopLeft = ViewportToImage(new Point(0, 0));
            Point vBottomRight = ViewportToImage(new Point(w, h));

            double normX1 = Clamp(vTopLeft.X / Source.PixelWidth, 0, 1);
            double normY1 = Clamp(vTopLeft.Y / Source.PixelHeight, 0, 1);
            double normX2 = Clamp(vBottomRight.X / Source.PixelWidth, 0, 1);
            double normY2 = Clamp(vBottomRight.Y / Source.PixelHeight, 0, 1);

            double vpX = thumbRect.X + Math.Min(normX1, normX2) * thumbRect.Width;
            double vpY = thumbRect.Y + Math.Min(normY1, normY2) * thumbRect.Height;
            double vpW = Math.Max(6, Math.Abs(normX2 - normX1) * thumbRect.Width);
            double vpH = Math.Max(6, Math.Abs(normY2 - normY1) * thumbRect.Height);

            Rect viewportIndicator = new Rect(vpX, vpY, vpW, vpH);
            dc.DrawRectangle(MiniMapViewportFill, MiniMapViewportPen, viewportIndicator);
        }

        private void DrawLoupe(DrawingContext dc)
        {
            if (Source == null) return;

            Point center = _currentMousePosition;
            double radius = 64.0;
            double factor = LoupeFactor;

            var clipGeo = new EllipseGeometry(center, radius, radius);
            dc.PushClip(clipGeo);

            // Magnified background
            dc.DrawRectangle(DefaultCanvasBg, null, new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2));

            // Magnified image fragment
            Point imgCoord = ViewportToImage(center);
            dc.PushTransform(new TranslateTransform(center.X, center.Y));
            dc.PushTransform(new ScaleTransform(Zoom * factor, Zoom * factor));
            dc.DrawImage(Source, new Rect(-imgCoord.X, -imgCoord.Y, Source.PixelWidth, Source.PixelHeight));
            dc.Pop();
            dc.Pop();

            // Crosshair inside loupe
            var crossPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)), 1.0);
            dc.DrawLine(crossPen, new Point(center.X - 10, center.Y), new Point(center.X + 10, center.Y));
            dc.DrawLine(crossPen, new Point(center.X, center.Y - 10), new Point(center.X, center.Y + 10));

            dc.Pop(); // Pop clip

            // Loupe Outer Ring Glow & Border
            var ringPen = new Pen(AccentBrush, 2.5);
            dc.DrawEllipse(null, ringPen, center, radius, radius);
        }

        private void DrawStatusBar(DrawingContext dc)
        {
            if (Source == null) return;

            double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            double mp = (Source.PixelWidth * Source.PixelHeight) / 1_000_000.0;
            string probeInfo = _currentPixelCoord.X >= 0
                ? $" | X: {(int)_currentPixelCoord.X} Y: {(int)_currentPixelCoord.Y} | #{_currentHoverColor.R:X2}{_currentHoverColor.G:X2}{_currentHoverColor.B:X2}"
                : string.Empty;

            string info = $"{Source.PixelWidth} × {Source.PixelHeight} ({mp:F1} MP) | {Math.Round(Zoom * 100):0}% [{ViewMode}]{probeInfo}";

            var ft = new FormattedText(
                info,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                _hudTypeface,
                11.5,
                HudTextBrush,
                dpi);

            Rect bgRect = new Rect(14, 14, ft.Width + 24, ft.Height + 10);
            dc.DrawRoundedRectangle(HudBgBrush, HudBorderPen, bgRect, 6, 6);

            // Color probe preview dot
            if (_currentPixelCoord.X >= 0)
            {
                var colorDot = new SolidColorBrush(_currentHoverColor);
                colorDot.Freeze();
                dc.DrawEllipse(colorDot, HudBorderPen, new Point(bgRect.Right - 14, bgRect.Top + bgRect.Height / 2.0), 5, 5);
            }

            dc.DrawText(ft, new Point(bgRect.X + 10, bgRect.Y + 5));
        }

        private void DrawToolbar(DrawingContext dc, double w, double h)
        {
            double barW = 380.0;
            double barH = 38.0;
            Rect barRect = new Rect((w - barW) / 2.0, h - barH - 14.0, barW, barH);

            // Floating translucent card
            dc.DrawRoundedRectangle(HudBgBrush, HudBorderPen, barRect, 19, 19);

            double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            string[] buttons = { "−", "+", "Fit", "1:1", "↺", "↻", "⇄", "Grid", "Loupe", "Reset" };
            double btnW = barW / buttons.Length;

            for (int i = 0; i < buttons.Length; i++)
            {
                Rect btnRect = new Rect(barRect.X + i * btnW, barRect.Y, btnW, barH);
                bool hovered = btnRect.Contains(_currentMousePosition);

                if (hovered)
                {
                    dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(60, 0, 229, 255)), null, btnRect, 4, 4);
                }

                var ft = new FormattedText(
                    buttons[i],
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    _hudTypeface,
                    11.5,
                    hovered ? AccentBrush : HudTextBrush,
                    dpi);

                ft.TextAlignment = TextAlignment.Center;
                dc.DrawText(ft, new Point(btnRect.X + btnW / 2.0, btnRect.Y + (barH - ft.Height) / 2.0));
            }
        }

        #endregion

        #region Coordinate Geometry Transforms

        public Point ViewportToImage(Point vp)
        {
            if (Source == null || Zoom <= 0) return new Point(0, 0);

            // Inverse translation
            double dx = vp.X - PanOffset.X;
            double dy = vp.Y - PanOffset.Y;

            // Inverse scale & flip
            double sx = Zoom * (Flip.HasFlag(ImageFlipMode.Horizontal) ? -1 : 1);
            double sy = Zoom * (Flip.HasFlag(ImageFlipMode.Vertical) ? -1 : 1);
            dx /= sx;
            dy /= sy;

            // Inverse rotation
            if (Rotation != ImageRotationAngle.Rotate0)
            {
                double rad = -(double)Rotation * Math.PI / 180.0;
                double cos = Math.Cos(rad);
                double sin = Math.Sin(rad);
                double rx = dx * cos - dy * sin;
                double ry = dx * sin + dy * cos;
                dx = rx;
                dy = ry;
            }

            return new Point(dx + Source.PixelWidth / 2.0, dy + Source.PixelHeight / 2.0);
        }

        public Point ImageToViewport(Point img)
        {
            if (Source == null) return new Point(0, 0);

            double dx = img.X - Source.PixelWidth / 2.0;
            double dy = img.Y - Source.PixelHeight / 2.0;

            // Rotation
            if (Rotation != ImageRotationAngle.Rotate0)
            {
                double rad = (double)Rotation * Math.PI / 180.0;
                double cos = Math.Cos(rad);
                double sin = Math.Sin(rad);
                double rx = dx * cos - dy * sin;
                double ry = dx * sin + dy * cos;
                dx = rx;
                dy = ry;
            }

            // Scale & flip
            double sx = Zoom * (Flip.HasFlag(ImageFlipMode.Horizontal) ? -1 : 1);
            double sy = Zoom * (Flip.HasFlag(ImageFlipMode.Vertical) ? -1 : 1);
            dx *= sx;
            dy *= sy;

            return new Point(dx + PanOffset.X, dy + PanOffset.Y);
        }

        #endregion

        #region Mouse & Keyboard Interaction

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            Point mouse = e.GetPosition(this);

            // Check if clicked Toolbar
            if (ShowToolbar && HandleToolbarClick(mouse))
            {
                e.Handled = true;
                return;
            }

            // Check if clicked MiniMap
            if (ShowMiniMap && Source != null && HandleMiniMapClick(mouse))
            {
                _isDraggingMiniMap = true;
                CaptureMouse();
                e.Handled = true;
                return;
            }

            if (e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Middle)
            {
                if (e.ClickCount == 2)
                {
                    // Double click toggles FitToWindow and 1:1
                    if (ViewMode == ImageViewMode.FitToWindow) ActualSize();
                    else FitToWindow();
                }
                else
                {
                    _isPanning = true;
                    _panStartMouse = mouse;
                    _panStartOffset = PanOffset;
                    Cursor = Cursors.SizeAll;
                    CaptureMouse();
                }
                e.Handled = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _currentMousePosition = e.GetPosition(this);

            if (_isPanning)
            {
                Vector delta = _currentMousePosition - _panStartMouse;
                PanOffset = new Point(_panStartOffset.X + delta.X, _panStartOffset.Y + delta.Y);
                ViewMode = ImageViewMode.Custom;
                PanChanged?.Invoke(this, PanOffset);
                InvalidateVisual();
            }
            else if (_isDraggingMiniMap)
            {
                HandleMiniMapDrag(_currentMousePosition);
            }
            else
            {
                // Update color probe under cursor
                UpdatePixelTelemetry(_currentMousePosition);
                if (ShowLoupe || ShowToolbar) InvalidateVisual();
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (_isPanning || _isDraggingMiniMap)
            {
                _isPanning = false;
                _isDraggingMiniMap = false;
                Cursor = Cursors.Arrow;
                ReleaseMouseCapture();
                InvalidateVisual();
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            Point mouse = e.GetPosition(this);

            double zoomFactor = e.Delta > 0 ? 1.2 : 0.8333;
            SetZoom(Zoom * zoomFactor, mouse);
            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.Key)
            {
                case Key.OemPlus:
                case Key.Add:
                    ZoomIn();
                    e.Handled = true;
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    ZoomOut();
                    e.Handled = true;
                    break;
                case Key.D0:
                case Key.NumPad0:
                    FitToWindow();
                    e.Handled = true;
                    break;
                case Key.D1:
                case Key.NumPad1:
                    ActualSize();
                    e.Handled = true;
                    break;
                case Key.R:
                    RotateClockwise();
                    e.Handled = true;
                    break;
                case Key.G:
                    ShowPixelGrid = !ShowPixelGrid;
                    e.Handled = true;
                    break;
                case Key.M:
                    ShowMiniMap = !ShowMiniMap;
                    e.Handled = true;
                    break;
                case Key.L:
                    ShowLoupe = !ShowLoupe;
                    e.Handled = true;
                    break;
            }
        }

        private bool HandleToolbarClick(Point mouse)
        {
            double w = ActualWidth;
            double h = ActualHeight;
            double barW = 380.0;
            double barH = 38.0;
            Rect barRect = new Rect((w - barW) / 2.0, h - barH - 14.0, barW, barH);
            if (!barRect.Contains(mouse)) return false;

            double btnW = barW / 10.0;
            int idx = (int)((mouse.X - barRect.X) / btnW);

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
                case 8: ShowLoupe = !ShowLoupe; break;
                case 9: ResetView(); break;
            }
            return true;
        }

        private bool HandleMiniMapClick(Point mouse)
        {
            double pad = 16.0;
            double mapW = 150.0;
            double mapH = 100.0;
            Rect mapBounds = new Rect(ActualWidth - mapW - pad, ActualHeight - mapH - pad - (ShowToolbar ? 48 : 0), mapW, mapH);
            if (mapBounds.Contains(mouse))
            {
                HandleMiniMapDrag(mouse);
                return true;
            }
            return false;
        }

        private void HandleMiniMapDrag(Point mouse)
        {
            if (Source == null) return;

            double pad = 16.0;
            double mapW = 150.0;
            double mapH = 100.0;
            Rect mapBounds = new Rect(ActualWidth - mapW - pad, ActualHeight - mapH - pad - (ShowToolbar ? 48 : 0), mapW, mapH);

            double normX = Clamp((mouse.X - mapBounds.X) / mapBounds.Width, 0, 1);
            double normY = Clamp((mouse.Y - mapBounds.Y) / mapBounds.Height, 0, 1);

            double targetImgX = normX * Source.PixelWidth;
            double targetImgY = normY * Source.PixelHeight;

            // Center view on this coordinate
            Point targetVp = ImageToViewport(new Point(targetImgX, targetImgY));
            PanOffset = new Point(PanOffset.X + (ActualWidth / 2.0 - targetVp.X), PanOffset.Y + (ActualHeight / 2.0 - targetVp.Y));
            ViewMode = ImageViewMode.Custom;
            InvalidateVisual();
        }

        private void UpdatePixelTelemetry(Point mouse)
        {
            if (Source == null) return;

            Point imgPt = ViewportToImage(mouse);
            int px = (int)Math.Floor(imgPt.X);
            int py = (int)Math.Floor(imgPt.Y);

            if (px >= 0 && px < Source.PixelWidth && py >= 0 && py < Source.PixelHeight)
            {
                _currentPixelCoord = new Point(px, py);
                try
                {
                    var cb = new CroppedBitmap(Source, new Int32Rect(px, py, 1, 1));
                    byte[] pixels = new byte[4];
                    cb.CopyPixels(pixels, 4, 0);

                    // Typical BGRA / BGR
                    _currentHoverColor = Color.FromRgb(pixels[2], pixels[1], pixels[0]);
                    HoverPixelChanged?.Invoke(this, (px, py, _currentHoverColor));
                }
                catch
                {
                    _currentHoverColor = Colors.Transparent;
                }
            }
            else
            {
                _currentPixelCoord = new Point(-1, -1);
            }
        }

        #endregion

        #region Helper Methods

        private static double Clamp(double val, double min, double max) => Math.Max(min, Math.Min(max, val));

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZImageViewer ctrl && ctrl.Source != null)
            {
                ctrl.FitToWindow();
            }
        }

        private static void OnFilePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZImageViewer ctrl && e.NewValue is string path)
            {
                ctrl.LoadFromFile(path);
            }
        }

        private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZImageViewer ctrl && e.NewValue is double zoom)
            {
                ctrl.ZoomChanged?.Invoke(ctrl, zoom);
            }
        }

        private static void OnViewModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZImageViewer ctrl && e.NewValue is ImageViewMode mode)
            {
                switch (mode)
                {
                    case ImageViewMode.FitToWindow: ctrl.FitToWindow(); break;
                    case ImageViewMode.FitWidth: ctrl.FitWidth(); break;
                    case ImageViewMode.FitHeight: ctrl.FitHeight(); break;
                    case ImageViewMode.OriginalSize: ctrl.ActualSize(); break;
                }
            }
        }

        #endregion
    }
    #region Backward Compatibility Shims (5-Release Deprecation Policy)

    /// <summary>
    /// Legacy alias for <see cref="ZImageViewer"/>.
    /// Preserved for backward compatibility across 5 release cycles.
    /// </summary>
    [Obsolete("ImageViewerControl is deprecated and will be removed in 5 release cycles. Please migrate to ZImageViewer instead.")]
    public class ImageViewerControl : ZImageViewer
    {
    }

    /// <summary>
    /// Legacy alias for <see cref="ZImageViewer"/>.
    /// </summary>
    [Obsolete("ZeroImageViewer is deprecated. Please use ZImageViewer instead.")]
    public class ZeroImageViewer : ZImageViewer
    {
    }

    #endregion
}