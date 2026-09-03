using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Overlays;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Editors
{
    public enum ImageScaleMode
    {
        Cover,
        Contain,
        Center,
        Stretch
    }

    public enum AvatarStatus
    {
        None,
        Online,
        Busy,
        Away,
        Offline
    }

    /// <summary>
    /// Modern anti-aliased image and avatar control for ZeroUI.
    /// Supports rounded borders, circular avatars, initials fallback, operator online/offline status dots,
    /// and click-to-zoom Lightbox modal preview.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Editors")]
    [DefaultProperty("Image")]
    [Description("Modern anti-aliased image and avatar control with initials fallback and zoom lightbox")]
    public class ZeroImage : Control
    {
        private Image? _image;
        private ImageScaleMode _scaleMode = ImageScaleMode.Cover;
        private bool _isCircle = false;
        private int _borderRadius = 8;
        private float _borderWidth = 1f;
        private Color? _borderColor;
        private string? _fallbackText;
        private Color? _fallbackColor;
        private AvatarStatus _status = AvatarStatus.None;
        private bool _enableZoomPreview = true;
        private bool _isHovered = false;

        public event EventHandler? ImageClicked;

        public ZeroImage()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(64, 64);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;

            ZeroTheme.ThemeChanged += (s, e) => Invalidate();
            ZeroUIConfig.CornerStyleChanged += (s, e) => Invalidate();
        }

        [Category("Appearance")]
        [DefaultValue(null)]
        public Image? Image
        {
            get => _image;
            set
            {
                _image = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(ImageScaleMode.Cover)]
        public ImageScaleMode ScaleMode
        {
            get => _scaleMode;
            set
            {
                _scaleMode = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        public bool IsCircle
        {
            get => _isCircle;
            set
            {
                _isCircle = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(8)]
        public int BorderRadius
        {
            get => _borderRadius;
            set
            {
                _borderRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(1f)]
        public float BorderWidth
        {
            get => _borderWidth;
            set
            {
                _borderWidth = Math.Max(0f, value);
                Invalidate();
            }
        }

        [Category("Appearance")]
        public Color BorderColor
        {
            get => _borderColor ?? ZeroTheme.Colors.Border;
            set
            {
                _borderColor = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(null)]
        public string? FallbackText
        {
            get => _fallbackText;
            set
            {
                _fallbackText = value;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(null)]
        public Color? FallbackColor
        {
            get => _fallbackColor;
            set
            {
                _fallbackColor = value;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(AvatarStatus.None)]
        public AvatarStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                Invalidate();
            }
        }

        [Category("Behavior")]
        [DefaultValue(true)]
        public bool EnableZoomPreview
        {
            get => _enableZoomPreview;
            set => _enableZoomPreview = value;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            ImageClicked?.Invoke(this, EventArgs.Empty);

            if (_enableZoomPreview && _image != null)
            {
                ShowLightboxPreview();
            }
        }

        public void ShowLightboxPreview()
        {
            if (_image == null) return;
            var previewPanel = new LightboxViewerPanel(_image);
            IWin32Window parentWindow = (IWin32Window?)FindForm() ?? this;
            ZeroModal.Show(
                parentWindow,
                $"Xem Chi Tiết Hình Ảnh ({_image.Width} x {_image.Height} px)",
                previewPanel,
                okText: "Đóng",
                showCancel: false,
                width: 680,
                height: 520);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var palette = ZeroTheme.Colors;
            Rectangle clientRect = new Rectangle(0, 0, Width, Height);

            // 1. Fill parent background to eliminate black corner clipping artifacts
            Color parentBg = ZeroUIConfig.GetParentBackground(this, palette.Background);
            using (var brushParent = new SolidBrush(parentBg))
            {
                g.FillRectangle(brushParent, clientRect);
            }

            // 2. Calculate Clipping Path
            using (var clipPath = CreateShapePath(clientRect))
            {
                g.SetClip(clipPath);

                if (_image != null)
                {
                    // Draw Image with selected scale mode
                    DrawScaledImage(g, _image, clientRect, _scaleMode);
                }
                else
                {
                    // Draw Fallback Initials
                    DrawFallbackInitials(g, clientRect, palette);
                }

                g.ResetClip();

                // Draw Border
                if (_borderWidth > 0f)
                {
                    Color bColor = _isHovered ? palette.Primary : BorderColor;
                    using var pen = new Pen(bColor, _borderWidth);
                    g.DrawPath(pen, clipPath);
                }
            }

            // Draw Operator/Asset Status Badge Dot
            if (_status != AvatarStatus.None)
            {
                DrawStatusDot(g, clientRect, palette);
            }
        }

        private void DrawScaledImage(Graphics g, Image img, Rectangle targetRect, ImageScaleMode mode)
        {
            switch (mode)
            {
                case ImageScaleMode.Stretch:
                    g.DrawImage(img, targetRect);
                    break;

                case ImageScaleMode.Center:
                    int cx = targetRect.X + (targetRect.Width - img.Width) / 2;
                    int cy = targetRect.Y + (targetRect.Height - img.Height) / 2;
                    g.DrawImage(img, new Rectangle(cx, cy, img.Width, img.Height));
                    break;

                case ImageScaleMode.Contain:
                    float ratioContain = Math.Min((float)targetRect.Width / img.Width, (float)targetRect.Height / img.Height);
                    int destW = (int)(img.Width * ratioContain);
                    int destH = (int)(img.Height * ratioContain);
                    int destX = targetRect.X + (targetRect.Width - destW) / 2;
                    int destY = targetRect.Y + (targetRect.Height - destH) / 2;
                    g.DrawImage(img, new Rectangle(destX, destY, destW, destH));
                    break;

                case ImageScaleMode.Cover:
                default:
                    float ratioCover = Math.Max((float)targetRect.Width / img.Width, (float)targetRect.Height / img.Height);
                    int srcW = (int)(targetRect.Width / ratioCover);
                    int srcH = (int)(targetRect.Height / ratioCover);
                    int srcX = Math.Max(0, (img.Width - srcW) / 2);
                    int srcY = Math.Max(0, (img.Height - srcH) / 2);
                    g.DrawImage(img, targetRect, new Rectangle(srcX, srcY, srcW, srcH), GraphicsUnit.Pixel);
                    break;
            }
        }

        private void DrawFallbackInitials(Graphics g, Rectangle rect, ZeroThemePalette palette)
        {
            Color bgColor = _fallbackColor ?? GetDeterministicColor(_fallbackText ?? Name);
            using (var brushBg = new SolidBrush(bgColor))
            {
                g.FillRectangle(brushBg, rect);
            }

            string initials = ExtractInitials(_fallbackText);
            if (!string.IsNullOrEmpty(initials))
            {
                float fontSize = Math.Max(8f, Math.Min(rect.Width, rect.Height) * 0.38f);
                using var font = new Font(Font.FontFamily, fontSize, FontStyle.Bold);
                using var brushText = new SolidBrush(Color.White);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(initials, font, brushText, rect, sf);
            }
            else
            {
                // Draw default user avatar glyph (👤)
                using var iconFont = new Font("Segoe UI Emoji", Math.Min(rect.Width, rect.Height) * 0.45f);
                using var brushIcon = new SolidBrush(Color.White);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("👤", iconFont, brushIcon, rect, sf);
            }
        }

        private void DrawStatusDot(Graphics g, Rectangle rect, ZeroThemePalette palette)
        {
            Color dotColor = _status switch
            {
                AvatarStatus.Online => palette.Success,
                AvatarStatus.Busy => palette.Danger,
                AvatarStatus.Away => palette.Warning,
                _ => Color.FromArgb(100, 116, 139) // Slate Offline
            };

            int dotSize = Math.Max(10, Math.Min(rect.Width, rect.Height) / 4);
            int dotX = rect.Right - dotSize - 1;
            int dotY = rect.Bottom - dotSize - 1;
            var dotRect = new Rectangle(dotX, dotY, dotSize, dotSize);

            // Cutout halo matching parent background
            Color cutoutBg = Parent?.BackColor ?? palette.Background;
            if (cutoutBg == Color.Transparent) cutoutBg = palette.Surface;

            var cutoutRect = new Rectangle(dotX - 2, dotY - 2, dotSize + 4, dotSize + 4);
            using (var brushCutout = new SolidBrush(cutoutBg))
            {
                g.FillEllipse(brushCutout, cutoutRect);
            }

            using (var brushDot = new SolidBrush(dotColor))
            {
                g.FillEllipse(brushDot, dotRect);
            }
        }

        private GraphicsPath CreateShapePath(Rectangle r)
        {
            var path = new GraphicsPath();
            if (_isCircle)
            {
                path.AddEllipse(r);
                return path;
            }

            int effRadius = ZeroUIConfig.GetEffectiveRadius(_borderRadius);
            return ZeroUIConfig.CreateRoundedRectangle(r, effRadius);
        }

        private static string ExtractInitials(string? text)
        {
            if (text == null || string.IsNullOrWhiteSpace(text)) return "";
            var words = text.Trim().Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "";
            if (words.Length == 1)
            {
                return words[0].Length >= 2 ? words[0].Substring(0, 2).ToUpper() : words[0].ToUpper();
            }
            return (words[0].Substring(0, 1) + words[words.Length - 1].Substring(0, 1)).ToUpper();
        }

        private static Color GetDeterministicColor(string seed)
        {
            int hash = Math.Abs(seed.GetHashCode());
            Color[] colors = new[]
            {
                Color.FromArgb(79, 70, 229),   // Indigo
                Color.FromArgb(16, 185, 129),  // Emerald
                Color.FromArgb(14, 165, 233),  // Sky
                Color.FromArgb(245, 158, 11),  // Amber
                Color.FromArgb(236, 72, 153),  // Pink
                Color.FromArgb(139, 92, 246),  // Purple
                Color.FromArgb(6, 182, 212)    // Cyan
            };
            return colors[hash % colors.Length];
        }

        /// <summary>
        /// Inner Lightbox panel displaying high-res zoomable image.
        /// </summary>
        private class LightboxViewerPanel : Control
        {
            private readonly Image _image;
            private float _zoomFactor = 1.0f;
            private int _rotationDegrees = 0;

            public LightboxViewerPanel(Image image)
            {
                _image = image;
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw, true);
                BackColor = Color.FromArgb(10, 15, 28);

                // Top Toolbar for Zoom & Rotation
                var topTools = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 38,
                    BackColor = Color.FromArgb(20, 25, 45),
                    Padding = new Padding(8, 2, 8, 2)
                };

                var btnZoomIn = CreateToolButton("➕ Phóng to", () => { _zoomFactor = Math.Min(4.0f, _zoomFactor + 0.25f); Invalidate(); });
                var btnZoomOut = CreateToolButton("➖ Thu nhỏ", () => { _zoomFactor = Math.Max(0.25f, _zoomFactor - 0.25f); Invalidate(); });
                var btnRotate = CreateToolButton("↷ Xoay 90°", () => { _rotationDegrees = (_rotationDegrees + 90) % 360; Invalidate(); });
                var btnReset = CreateToolButton("⟲ Vừa khung", () => { _zoomFactor = 1.0f; _rotationDegrees = 0; Invalidate(); });

                topTools.Controls.Add(btnReset);
                topTools.Controls.Add(btnRotate);
                topTools.Controls.Add(btnZoomOut);
                topTools.Controls.Add(btnZoomIn);
                Controls.Add(topTools);
            }

            private ZeroButton CreateToolButton(string text, Action onClick)
            {
                var btn = new ZeroButton
                {
                    Text = text,
                    Dock = DockStyle.Left,
                    Width = 96,
                    ButtonStyle = ZeroButtonStyle.Secondary,
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold)
                };
                btn.Click += (s, e) => onClick();
                return btn;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                var displayRect = new Rectangle(0, 38, Width, Height - 38);

                // Center image
                float ratio = Math.Min((float)displayRect.Width / _image.Width, (float)displayRect.Height / _image.Height) * _zoomFactor;
                int destW = (int)(_image.Width * ratio);
                int destH = (int)(_image.Height * ratio);
                int destX = displayRect.X + (displayRect.Width - destW) / 2;
                int destY = displayRect.Y + (displayRect.Height - destH) / 2;

                g.TranslateTransform(destX + (destW / 2f), destY + (destH / 2f));
                g.RotateTransform(_rotationDegrees);
                g.DrawImage(_image, -destW / 2, -destH / 2, destW, destH);
                g.ResetTransform();
            }
        }
    }
}
