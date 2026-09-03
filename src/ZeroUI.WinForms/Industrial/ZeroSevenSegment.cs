using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ZeroUI.WinForms.Native;

namespace ZeroUI.WinForms.Industrial
{
    /// <summary>
    /// Preset color palettes for industrial 7-segment displays.
    /// </summary>
    public enum SevenSegmentColorPreset
    {
        Custom,
        NeonEmerald,
        NeonCyan,
        NeonAmber,
        NeonRed,
        CrispWhite,
        UltraViolet
    }

    /// <summary>
    /// Display mode for leading zeros in numeric values.
    /// </summary>
    public enum LeadingZeroDisplayMode
    {
        Blank,
        DimmedGhost,
        LitZero
    }

    /// <summary>
    /// Frame and bezel styling for the LED acrylic enclosure.
    /// </summary>
    public enum SevenSegmentFrameStyle
    {
        RecessedBezel,
        AcrylicGlass,
        Borderless
    }

    /// <summary>
    /// Industrial 7-Segment Digital LED Display for SCADA & MES telemetry.
    /// Features authentic beveled polygon segment geometry, configurable slant (italic) angle,
    /// smart colon separators, decimal point integration, multi-stage neon glow, acrylic reflection,
    /// and comprehensive alphanumeric and industrial status messaging support.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI - Industrial & SCADA")]
    [DefaultProperty("Value")]
    [Description("Industrial 7-Segment Digital LED Display for SCADA & MES telemetry")]
    public class ZeroSevenSegment : Control
    {
        private string _value = "1420";
        private Color _segmentColor = Color.FromArgb(52, 211, 153); // Emerald Neon Green
        private Color _dimColor = Color.FromArgb(20, 45, 35);       // Subtle unlit ghost segment
        private int _digitCount = 6;
        private float _slantAngle = 7f;                             // Authentic industrial tilt (0 - 15 deg)
        private float _segmentGap = 1.5f;                           // Gap between beveled segments
        private int _segmentThickness = 0;                          // 0 = Auto calculated from height
        private SevenSegmentColorPreset _colorPreset = SevenSegmentColorPreset.NeonEmerald;
        private LeadingZeroDisplayMode _leadingZeroMode = LeadingZeroDisplayMode.Blank;
        private SevenSegmentFrameStyle _frameStyle = SevenSegmentFrameStyle.RecessedBezel;
        private HorizontalAlignment _textAlignment = HorizontalAlignment.Right;
        private bool _showGhostSegments = true;
        private bool _showGlow = true;
        private bool _showGlassReflection = true;
        private bool _blinkColon = false;
        private bool _blink = false;
        private int _blinkInterval = 500;
        private string _unit = "";
        private Color _unitColor = Color.Empty;

        private readonly Timer _blinkTimer;
        private bool _blinkPhase = true;

        // 7-segment bitmask mapping (Bits 0..6: A, B, C, D, E, F, G)
        // Bit 0 (0x01): Top (A)
        // Bit 1 (0x02): Top-Right (B)
        // Bit 2 (0x04): Bottom-Right (C)
        // Bit 3 (0x08): Bottom (D)
        // Bit 4 (0x10): Bottom-Left (E)
        // Bit 5 (0x20): Top-Left (F)
        // Bit 6 (0x40): Middle (G)
        private static readonly byte[] CharPatterns = new byte[128];

        static ZeroSevenSegment()
        {
            // Digits 0-9
            CharPatterns['0'] = 0x3F; // A B C D E F
            CharPatterns['1'] = 0x06; // B C
            CharPatterns['2'] = 0x5B; // A B D E G
            CharPatterns['3'] = 0x4F; // A B C D G
            CharPatterns['4'] = 0x66; // B C F G
            CharPatterns['5'] = 0x6D; // A C D F G
            CharPatterns['6'] = 0x7D; // A C D E F G
            CharPatterns['7'] = 0x07; // A B C
            CharPatterns['8'] = 0x7F; // A B C D E F G
            CharPatterns['9'] = 0x6F; // A B C D F G

            // Symbols
            CharPatterns['-'] = 0x40; // G
            CharPatterns['_'] = 0x08; // D
            CharPatterns['='] = 0x48; // D G
            CharPatterns[' '] = 0x00;
            CharPatterns['['] = 0x39; // A D E F
            CharPatterns[']'] = 0x0F; // A B C D

            // Letters for SCADA/MES status (STOP, RUN, PASS, FAIL, COOL, HEAT, ALARM, Err, OFF, ON)
            CharPatterns['A'] = 0x77; CharPatterns['a'] = 0x77;
            CharPatterns['B'] = 0x7C; CharPatterns['b'] = 0x7C; // b
            CharPatterns['C'] = 0x39; CharPatterns['c'] = 0x58; // C / c
            CharPatterns['D'] = 0x5E; CharPatterns['d'] = 0x5E; // d
            CharPatterns['E'] = 0x79; CharPatterns['e'] = 0x79;
            CharPatterns['F'] = 0x71; CharPatterns['f'] = 0x71;
            CharPatterns['G'] = 0x3D; CharPatterns['g'] = 0x6F;
            CharPatterns['H'] = 0x76; CharPatterns['h'] = 0x74; // H / h
            CharPatterns['I'] = 0x06; CharPatterns['i'] = 0x04;
            CharPatterns['J'] = 0x1E; CharPatterns['j'] = 0x1E;
            CharPatterns['K'] = 0x75; CharPatterns['k'] = 0x74;
            CharPatterns['L'] = 0x38; CharPatterns['l'] = 0x30;
            CharPatterns['M'] = 0x54; CharPatterns['m'] = 0x54; // Industrial 7-seg standard (displays as n)
            CharPatterns['N'] = 0x54; CharPatterns['n'] = 0x54; // n
            CharPatterns['O'] = 0x3F; CharPatterns['o'] = 0x5C; // O / o
            CharPatterns['P'] = 0x73; CharPatterns['p'] = 0x73;
            CharPatterns['Q'] = 0x67; CharPatterns['q'] = 0x67;
            CharPatterns['R'] = 0x50; CharPatterns['r'] = 0x50; // r
            CharPatterns['S'] = 0x6D; CharPatterns['s'] = 0x6D; // S (5)
            CharPatterns['T'] = 0x78; CharPatterns['t'] = 0x78; // t
            CharPatterns['U'] = 0x3E; CharPatterns['u'] = 0x1C; // U / u
            CharPatterns['V'] = 0x1C; CharPatterns['v'] = 0x1C;
            CharPatterns['W'] = 0x2A; CharPatterns['w'] = 0x2A;
            CharPatterns['X'] = 0x76; CharPatterns['x'] = 0x76;
            CharPatterns['Y'] = 0x6E; CharPatterns['y'] = 0x6E; // y
            CharPatterns['Z'] = 0x5B; CharPatterns['z'] = 0x5B;
        }

        public ZeroSevenSegment()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor, true);

            Size = new Size(180, 52);
            BackColor = Color.FromArgb(12, 18, 32); // Industrial dark acrylic enclosure

            _blinkTimer = new Timer { Interval = _blinkInterval };
            _blinkTimer.Tick += (s, e) =>
            {
                if (_blinkColon || _blink)
                {
                    _blinkPhase = !_blinkPhase;
                    Invalidate();
                }
            };

            if (!ZeroDesignHelper.IsInDesignMode(this))
            {
                _blinkTimer.Start();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _blinkTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Public Properties

        [Category("Appearance")]
        [DefaultValue("1420")]
        [Description("The text or numeric value to display on the 7-segment LED.")]
        public string Value
        {
            get => _value;
            set { _value = value ?? ""; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("The primary illuminated color of the LED segments.")]
        public Color SegmentColor
        {
            get => _segmentColor;
            set
            {
                _segmentColor = value;
                _dimColor = Color.FromArgb(28, Math.Max(8, value.R / 5), Math.Max(8, value.G / 5), Math.Max(8, value.B / 5));
                _colorPreset = SevenSegmentColorPreset.Custom;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [Description("The color of unlit/ghost segments when ShowGhostSegments is enabled.")]
        public Color DimColor
        {
            get => _dimColor;
            set { _dimColor = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(SevenSegmentColorPreset.NeonEmerald)]
        [Description("Quick color presets tailored for industrial SCADA and MES monitoring.")]
        public SevenSegmentColorPreset ColorPreset
        {
            get => _colorPreset;
            set
            {
                _colorPreset = value;
                switch (value)
                {
                    case SevenSegmentColorPreset.NeonEmerald:
                        _segmentColor = Color.FromArgb(52, 211, 153);
                        _dimColor = Color.FromArgb(20, 45, 35);
                        break;
                    case SevenSegmentColorPreset.NeonCyan:
                        _segmentColor = Color.FromArgb(56, 189, 248);
                        _dimColor = Color.FromArgb(16, 38, 54);
                        break;
                    case SevenSegmentColorPreset.NeonAmber:
                        _segmentColor = Color.FromArgb(245, 158, 11);
                        _dimColor = Color.FromArgb(48, 32, 14);
                        break;
                    case SevenSegmentColorPreset.NeonRed:
                        _segmentColor = Color.FromArgb(239, 68, 68);
                        _dimColor = Color.FromArgb(48, 18, 18);
                        break;
                    case SevenSegmentColorPreset.CrispWhite:
                        _segmentColor = Color.FromArgb(248, 250, 252);
                        _dimColor = Color.FromArgb(35, 40, 48);
                        break;
                    case SevenSegmentColorPreset.UltraViolet:
                        _segmentColor = Color.FromArgb(192, 132, 252);
                        _dimColor = Color.FromArgb(38, 24, 52);
                        break;
                    case SevenSegmentColorPreset.Custom:
                        break;
                }
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(6)]
        [Description("Minimum number of digit slots allocated on the display.")]
        public int DigitCount
        {
            get => _digitCount;
            set { _digitCount = Math.Max(1, Math.Min(16, value)); Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(7f)]
        [Description("Italic slant angle in degrees (0 - 15 deg) for authentic industrial meters.")]
        public float SlantAngle
        {
            get => _slantAngle;
            set
            {
                _slantAngle = Math.Max(0f, Math.Min(15f, value));
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(1.5f)]
        [Description("Physical isolation gap between adjacent beveled segments.")]
        public float SegmentGap
        {
            get => _segmentGap;
            set
            {
                _segmentGap = Math.Max(0.5f, Math.Min(5f, value));
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(0)]
        [Description("Fixed segment thickness in pixels (0 = auto-calculated proportional to height).")]
        public int SegmentThickness
        {
            get => _segmentThickness;
            set
            {
                _segmentThickness = Math.Max(0, value);
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(LeadingZeroDisplayMode.Blank)]
        [Description("How leading zeros are presented (Blank, DimmedGhost, or fully LitZero).")]
        public LeadingZeroDisplayMode LeadingZeroMode
        {
            get => _leadingZeroMode;
            set { _leadingZeroMode = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(false)]
        [Browsable(false)]
        [Description("Backward-compatible wrapper: true for LitZero, false for Blank.")]
        public bool ShowLeadingZeros
        {
            get => _leadingZeroMode == LeadingZeroDisplayMode.LitZero;
            set
            {
                _leadingZeroMode = value ? LeadingZeroDisplayMode.LitZero : LeadingZeroDisplayMode.Blank;
                Invalidate();
            }
        }

        [Category("Appearance")]
        [DefaultValue(HorizontalAlignment.Right)]
        [Description("Text alignment within the display area.")]
        public HorizontalAlignment TextAlignment
        {
            get => _textAlignment;
            set { _textAlignment = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(SevenSegmentFrameStyle.RecessedBezel)]
        [Description("Aesthetic frame and casing style.")]
        public SevenSegmentFrameStyle FrameStyle
        {
            get => _frameStyle;
            set { _frameStyle = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        [Description("Display unlit ghost segments for authentic digital LED physics.")]
        public bool ShowGhostSegments
        {
            get => _showGhostSegments;
            set { _showGhostSegments = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        [Description("Enable realistic multi-stage neon glow bloom around lit segments.")]
        public bool ShowGlow
        {
            get => _showGlow;
            set { _showGlow = value; Invalidate(); }
        }

        [Category("Appearance")]
        [DefaultValue(true)]
        [Description("Enable subtle acrylic glass sheen reflection across the upper display.")]
        public bool ShowGlassReflection
        {
            get => _showGlassReflection;
            set { _showGlassReflection = value; Invalidate(); }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("Blink the colon separator at a 1Hz rate (ideal for Takt time and clocks).")]
        public bool BlinkColon
        {
            get => _blinkColon;
            set { _blinkColon = value; Invalidate(); }
        }

        [Category("Behavior")]
        [DefaultValue(false)]
        [Description("Flash the entire readout for alarm or critical threshold conditions.")]
        public bool Blink
        {
            get => _blink;
            set { _blink = value; Invalidate(); }
        }

        [Category("Behavior")]
        [DefaultValue(500)]
        [Description("Blink rate interval in milliseconds.")]
        public int BlinkInterval
        {
            get => _blinkInterval;
            set
            {
                _blinkInterval = Math.Max(50, value);
                if (_blinkTimer != null) _blinkTimer.Interval = _blinkInterval;
            }
        }

        [Category("Appearance")]
        [DefaultValue("")]
        [Description("Measurement unit badge displayed on the right edge (e.g., 's', 'pcs', '°C', 'BAR').")]
        public string Unit
        {
            get => _unit;
            set { _unit = value ?? ""; Invalidate(); }
        }

        [Category("Appearance")]
        [Description("Custom color for the measurement unit badge.")]
        public Color UnitColor
        {
            get => _unitColor;
            set { _unitColor = value; Invalidate(); }
        }

        #endregion

        #region Parsing & Layout Model

        private struct DisplayItem
        {
            public char Character;
            public bool HasDecimal;
            public bool IsColon;
            public bool IsDimmed;
        }

        private List<DisplayItem> ParseValue()
        {
            var rawItems = new List<DisplayItem>();
            string text = _value ?? "";

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ':')
                {
                    rawItems.Add(new DisplayItem { IsColon = true });
                }
                else if (c == '.' || c == ',')
                {
                    if (rawItems.Count > 0 && !rawItems[rawItems.Count - 1].IsColon)
                    {
                        var last = rawItems[rawItems.Count - 1];
                        last.HasDecimal = true;
                        rawItems[rawItems.Count - 1] = last;
                    }
                    else
                    {
                        rawItems.Add(new DisplayItem { Character = ' ', HasDecimal = true });
                    }
                }
                else
                {
                    rawItems.Add(new DisplayItem { Character = c });
                }
            }

            // Check for clock format (colon)
            bool hasColon = false;
            for (int i = 0; i < rawItems.Count; i++)
            {
                if (rawItems[i].IsColon) { hasColon = true; break; }
            }

            if (!hasColon)
            {
                if (_leadingZeroMode == LeadingZeroDisplayMode.DimmedGhost)
                {
                    for (int i = 0; i < rawItems.Count - 1; i++)
                    {
                        if (rawItems[i].Character == '0' && !rawItems[i].HasDecimal)
                        {
                            var item = rawItems[i];
                            item.IsDimmed = true;
                            rawItems[i] = item;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else if (_leadingZeroMode == LeadingZeroDisplayMode.Blank)
                {
                    for (int i = 0; i < rawItems.Count - 1; i++)
                    {
                        if (rawItems[i].Character == '0' && !rawItems[i].HasDecimal)
                        {
                            var item = rawItems[i];
                            item.Character = ' ';
                            rawItems[i] = item;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            int digitCountInValue = 0;
            for (int i = 0; i < rawItems.Count; i++)
            {
                if (!rawItems[i].IsColon) digitCountInValue++;
            }

            int totalDigits = Math.Max(_digitCount, digitCountInValue);
            int padCount = totalDigits - digitCountInValue;

            if (padCount <= 0) return rawItems;

            var paddedList = new List<DisplayItem>(rawItems.Count + padCount);
            char padChar = (_leadingZeroMode == LeadingZeroDisplayMode.LitZero || _leadingZeroMode == LeadingZeroDisplayMode.DimmedGhost) ? '0' : ' ';
            bool isDimmed = (_leadingZeroMode == LeadingZeroDisplayMode.DimmedGhost);

            DisplayItem createPadItem() => new DisplayItem
            {
                Character = padChar,
                IsDimmed = isDimmed
            };

            if (_textAlignment == HorizontalAlignment.Right)
            {
                for (int i = 0; i < padCount; i++) paddedList.Add(createPadItem());
                paddedList.AddRange(rawItems);
            }
            else if (_textAlignment == HorizontalAlignment.Left)
            {
                paddedList.AddRange(rawItems);
                for (int i = 0; i < padCount; i++) paddedList.Add(new DisplayItem { Character = ' ' });
            }
            else // Center
            {
                int leftPad = padCount / 2;
                int rightPad = padCount - leftPad;
                for (int i = 0; i < leftPad; i++) paddedList.Add(createPadItem());
                paddedList.AddRange(rawItems);
                for (int i = 0; i < rightPad; i++) paddedList.Add(new DisplayItem { Character = ' ' });
            }

            return paddedList;
        }

        #endregion

        #region Rendering Engine

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = Width;
            int h = Height;

            // 1. Draw Enclosure Background & Bezel
            DrawEnclosure(g, w, h);

            // 2. Compute Layout Metrics
            int padX = 8;
            int padY = 6;
            int availableW = w - (padX * 2);
            int availableH = h - (padY * 2);

            // Reserve room for Unit badge if specified
            int unitReservedWidth = 0;
            if (!string.IsNullOrEmpty(_unit))
            {
                using var testFont = new Font("Segoe UI", Math.Max(7.5f, h * 0.18f), FontStyle.Bold);
                var unitSize = g.MeasureString(_unit, testFont);
                unitReservedWidth = (int)unitSize.Width + 6;
                availableW -= unitReservedWidth;
            }

            var items = ParseValue();

            int digitSlotCount = 0;
            int colonSlotCount = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].IsColon) colonSlotCount++;
                else digitSlotCount++;
            }

            float digitSlotRatio = 1.0f;
            float colonSlotRatio = 0.38f;
            float itemGap = 3.5f;

            float totalUnits = (digitSlotCount * digitSlotRatio) + (colonSlotCount * colonSlotRatio);
            float totalGaps = Math.Max(0, items.Count - 1) * itemGap;
            float digitW = totalUnits > 0 ? (availableW - totalGaps) / totalUnits : availableW;
            digitW = Math.Max(8f, digitW);
            float colonW = digitW * colonSlotRatio;
            float digitH = availableH;

            int thickness = _segmentThickness > 0 ? _segmentThickness : Math.Max(3, (int)(digitH * 0.115f));
            float gap = Math.Max(0.8f, _segmentGap);

            // Slant skew transformation factor
            float shearX = - (float)Math.Tan(_slantAngle * Math.PI / 180.0);

            // Global blink suppression
            bool isDisplayDark = _blink && !_blinkPhase;

            float currentX = padX;

            // 3. Render Each Digit / Colon
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];

                if (item.IsColon)
                {
                    DrawColon(g, currentX, padY, colonW, digitH, shearX, isDisplayDark);
                    currentX += colonW + itemGap;
                }
                else
                {
                    DrawBeveledDigit(g, currentX, padY, digitW, digitH, item, thickness, gap, shearX, isDisplayDark);
                    currentX += digitW + itemGap;
                }
            }

            // 4. Render Unit Badge
            if (!string.IsNullOrEmpty(_unit))
            {
                DrawUnitBadge(g, w - padX - unitReservedWidth + 2, padY, unitReservedWidth, digitH);
            }

            // 5. Acrylic Glass Reflection Overlay
            if (_showGlassReflection)
            {
                DrawGlassSheen(g, w, h);
            }
        }

        private void DrawEnclosure(Graphics g, int w, int h)
        {
            if (_frameStyle == SevenSegmentFrameStyle.Borderless)
            {
                using var bgBrush = new SolidBrush(BackColor);
                g.FillRectangle(bgBrush, 0, 0, w, h);
                return;
            }

            // Recessed Dark Acrylic Bezel
            using (var brush = new LinearGradientBrush(
                new Point(0, 0), new Point(0, h),
                Color.FromArgb(14, 20, 34), Color.FromArgb(8, 12, 22)))
            {
                g.FillRectangle(brush, 0, 0, w, h);
            }

            if (_frameStyle == SevenSegmentFrameStyle.RecessedBezel)
            {
                // Outer industrial frame border
                using var outerPen = new Pen(Color.FromArgb(40, 52, 70), 1f);
                g.DrawRectangle(outerPen, 0, 0, w - 1, h - 1);

                // Top/Left inner shadow
                using var shadowPen = new Pen(Color.FromArgb(6, 9, 16), 1.2f);
                g.DrawLine(shadowPen, 1, 1, w - 2, 1);
                g.DrawLine(shadowPen, 1, 1, 1, h - 2);

                // Bottom/Right inner rim highlight
                using var rimPen = new Pen(Color.FromArgb(30, 42, 60), 1f);
                g.DrawLine(rimPen, 1, h - 2, w - 2, h - 2);
                g.DrawLine(rimPen, w - 2, 1, w - 2, h - 2);
            }
            else if (_frameStyle == SevenSegmentFrameStyle.AcrylicGlass)
            {
                using var borderPen = new Pen(Color.FromArgb(30, 41, 59), 1f);
                g.DrawRectangle(borderPen, 0, 0, w - 1, h - 1);
            }
        }

        private void DrawBeveledDigit(Graphics g, float x, float y, float w, float h, DisplayItem item, int t, float gap, float shearX, bool isDisplayDark)
        {
            byte mask = 0;
            char c = item.Character;

            if (c >= 0 && c < CharPatterns.Length)
            {
                mask = CharPatterns[c];
            }
            else if (c == '°')
            {
                mask = 0x63; // A, B, F, G
            }

            var state = g.Save();

            // Apply Italic Slant Transformation
            if (_slantAngle > 0)
            {
                float cx = x + w / 2f;
                float cy = y + h / 2f;
                using var matrix = new Matrix();
                matrix.Translate(cx, cy);
                matrix.Shear(shearX, 0);
                matrix.Translate(-cx, -cy);
                g.MultiplyTransform(matrix);
            }

            float halfH = h / 2f;
            float t2 = t * 0.5f;

            // Compute 7 Beveled Hexagonal Segment Polygons
            // Segment A (Top horizontal)
            var polyA = new PointF[]
            {
                new PointF(x + t2 + gap, y + t2),
                new PointF(x + t + gap, y),
                new PointF(x + w - t - gap, y),
                new PointF(x + w - t2 - gap, y + t2),
                new PointF(x + w - t - gap, y + t),
                new PointF(x + t + gap, y + t)
            };

            // Segment B (Top-Right vertical)
            float rx = x + w - t;
            var polyB = new PointF[]
            {
                new PointF(rx + t2, y + t2 + gap),
                new PointF(rx + t, y + t + gap),
                new PointF(rx + t, y + halfH - t2 - gap),
                new PointF(rx + t2, y + halfH - gap),
                new PointF(rx, y + halfH - t2 - gap),
                new PointF(rx, y + t + gap)
            };

            // Segment C (Bottom-Right vertical)
            var polyC = new PointF[]
            {
                new PointF(rx + t2, y + halfH + gap),
                new PointF(rx + t, y + halfH + t2 + gap),
                new PointF(rx + t, y + h - t - gap),
                new PointF(rx + t2, y + h - t2 - gap),
                new PointF(rx, y + h - t - gap),
                new PointF(rx, y + halfH + t2 + gap)
            };

            // Segment D (Bottom horizontal)
            var polyD = new PointF[]
            {
                new PointF(x + t2 + gap, y + h - t2),
                new PointF(x + t + gap, y + h - t),
                new PointF(x + w - t - gap, y + h - t),
                new PointF(x + w - t2 - gap, y + h - t2),
                new PointF(x + w - t - gap, y + h),
                new PointF(x + t + gap, y + h)
            };

            // Segment E (Bottom-Left vertical)
            var polyE = new PointF[]
            {
                new PointF(x + t2, y + halfH + gap),
                new PointF(x + t, y + halfH + t2 + gap),
                new PointF(x + t, y + h - t - gap),
                new PointF(x + t2, y + h - t2 - gap),
                new PointF(x, y + h - t - gap),
                new PointF(x, y + halfH + t2 + gap)
            };

            // Segment F (Top-Left vertical)
            var polyF = new PointF[]
            {
                new PointF(x + t2, y + t2 + gap),
                new PointF(x + t, y + t + gap),
                new PointF(x + t, y + halfH - t2 - gap),
                new PointF(x + t2, y + halfH - gap),
                new PointF(x, y + halfH - t2 - gap),
                new PointF(x, y + t + gap)
            };

            // Segment G (Center horizontal)
            var polyG = new PointF[]
            {
                new PointF(x + t2 + gap, y + halfH),
                new PointF(x + t + gap, y + halfH - t2),
                new PointF(x + w - t - gap, y + halfH - t2),
                new PointF(x + w - t2 - gap, y + halfH),
                new PointF(x + w - t - gap, y + halfH + t2),
                new PointF(x + t + gap, y + halfH + t2)
            };

            bool isDimmedLeading = item.IsDimmed;

            if (isDimmedLeading)
            {
                // Subdued luminous color for leading zero so the '0' digit is clearly visible and hollow in the center
                Color dimmedLitColor = Color.FromArgb(100, _segmentColor);
                DrawDimmedSegment(g, polyA, (mask & 0x01) != 0, dimmedLitColor);
                DrawDimmedSegment(g, polyB, (mask & 0x02) != 0, dimmedLitColor);
                DrawDimmedSegment(g, polyC, (mask & 0x04) != 0, dimmedLitColor);
                DrawDimmedSegment(g, polyD, (mask & 0x08) != 0, dimmedLitColor);
                DrawDimmedSegment(g, polyE, (mask & 0x10) != 0, dimmedLitColor);
                DrawDimmedSegment(g, polyF, (mask & 0x20) != 0, dimmedLitColor);
                DrawDimmedSegment(g, polyG, (mask & 0x40) != 0, dimmedLitColor);
            }
            else
            {
                DrawSingleSegment(g, polyA, (mask & 0x01) != 0, t, isDisplayDark);
                DrawSingleSegment(g, polyB, (mask & 0x02) != 0, t, isDisplayDark);
                DrawSingleSegment(g, polyC, (mask & 0x04) != 0, t, isDisplayDark);
                DrawSingleSegment(g, polyD, (mask & 0x08) != 0, t, isDisplayDark);
                DrawSingleSegment(g, polyE, (mask & 0x10) != 0, t, isDisplayDark);
                DrawSingleSegment(g, polyF, (mask & 0x20) != 0, t, isDisplayDark);
                DrawSingleSegment(g, polyG, (mask & 0x40) != 0, t, isDisplayDark);
            }

            // Render Integrated Decimal Point (DP)
            float dpSize = Math.Max(2.5f, t * 0.9f);
            float dpX = x + w + gap * 0.5f;
            float dpY = y + h - dpSize;
            bool dpLit = item.HasDecimal && !isDisplayDark;

            if (dpLit)
            {
                if (_showGlow)
                {
                    using var glowBrush = new SolidBrush(Color.FromArgb(60, _segmentColor));
                    g.FillEllipse(glowBrush, dpX - 1.5f, dpY - 1.5f, dpSize + 3f, dpSize + 3f);
                }
                using var dpBrush = new SolidBrush(_segmentColor);
                g.FillEllipse(dpBrush, dpX, dpY, dpSize, dpSize);
            }
            else if (_showGhostSegments)
            {
                using var ghostBrush = new SolidBrush(_dimColor);
                g.FillEllipse(ghostBrush, dpX, dpY, dpSize, dpSize);
            }

            g.Restore(state);
        }

        private void DrawDimmedSegment(Graphics g, PointF[] points, bool isZeroSegment, Color dimmedLitColor)
        {
            if (isZeroSegment)
            {
                using var brush = new SolidBrush(dimmedLitColor);
                g.FillPolygon(brush, points);
            }
            // Do not draw segment G so the leading '0' remains distinctly hollow and authentic
        }

        private void DrawSingleSegment(Graphics g, PointF[] points, bool lit, int t, bool isDisplayDark)
        {
            if (lit && !isDisplayDark)
            {
                if (_showGlow)
                {
                    // Soft Bloom Neon Outer Aura
                    using var outerGlowPen = new Pen(Color.FromArgb(32, _segmentColor), t * 1.5f) { LineJoin = LineJoin.Round };
                    g.DrawPolygon(outerGlowPen, points);

                    // High-density Inner Glow
                    using var innerGlowPen = new Pen(Color.FromArgb(85, _segmentColor), t * 0.7f) { LineJoin = LineJoin.Round };
                    g.DrawPolygon(innerGlowPen, points);
                }

                // Core Solid Lit Segment
                using (var fillBrush = new SolidBrush(_segmentColor))
                {
                    g.FillPolygon(fillBrush, points);
                }

                // Ultra-bright High-luminance Center Core
                Color coreColor = Color.FromArgb(
                    255,
                    Math.Min(255, _segmentColor.R + 75),
                    Math.Min(255, _segmentColor.G + 75),
                    Math.Min(255, _segmentColor.B + 75));
                using var corePen = new Pen(coreColor, Math.Max(1f, t * 0.26f)) { LineJoin = LineJoin.Round };
                g.DrawPolygon(corePen, points);
            }
            else if (_showGhostSegments)
            {
                using var ghostBrush = new SolidBrush(_dimColor);
                g.FillPolygon(ghostBrush, points);
            }
        }

        private void DrawColon(Graphics g, float x, float y, float w, float h, float shearX, bool isDisplayDark)
        {
            var state = g.Save();

            if (_slantAngle > 0)
            {
                float cx = x + w / 2f;
                float cy = y + h / 2f;
                using var matrix = new Matrix();
                matrix.Translate(cx, cy);
                matrix.Shear(shearX, 0);
                matrix.Translate(-cx, -cy);
                g.MultiplyTransform(matrix);
            }

            bool colonLit = (!_blinkColon || _blinkPhase) && !isDisplayDark;
            float dotSize = Math.Max(3f, h * 0.095f);
            float dotX = x + (w / 2f) - (dotSize / 2f);
            float dot1Y = y + (h * 0.33f) - (dotSize / 2f);
            float dot2Y = y + (h * 0.67f) - (dotSize / 2f);

            if (colonLit)
            {
                if (_showGlow)
                {
                    using var glowBrush = new SolidBrush(Color.FromArgb(60, _segmentColor));
                    g.FillEllipse(glowBrush, dotX - 2, dot1Y - 2, dotSize + 4, dotSize + 4);
                    g.FillEllipse(glowBrush, dotX - 2, dot2Y - 2, dotSize + 4, dotSize + 4);
                }

                using var brush = new SolidBrush(_segmentColor);
                g.FillEllipse(brush, dotX, dot1Y, dotSize, dotSize);
                g.FillEllipse(brush, dotX, dot2Y, dotSize, dotSize);

                Color coreColor = Color.FromArgb(
                    255,
                    Math.Min(255, _segmentColor.R + 80),
                    Math.Min(255, _segmentColor.G + 80),
                    Math.Min(255, _segmentColor.B + 80));
                using var coreBrush = new SolidBrush(coreColor);
                float cSize = dotSize * 0.5f;
                float offset = (dotSize - cSize) / 2f;
                g.FillEllipse(coreBrush, dotX + offset, dot1Y + offset, cSize, cSize);
                g.FillEllipse(coreBrush, dotX + offset, dot2Y + offset, cSize, cSize);
            }
            else if (_showGhostSegments)
            {
                using var ghostBrush = new SolidBrush(_dimColor);
                g.FillEllipse(ghostBrush, dotX, dot1Y, dotSize, dotSize);
                g.FillEllipse(ghostBrush, dotX, dot2Y, dotSize, dotSize);
            }

            g.Restore(state);
        }

        private void DrawUnitBadge(Graphics g, float x, float y, float w, float h)
        {
            Color uColor = _unitColor.IsEmpty ? Color.FromArgb(170, _segmentColor) : _unitColor;
            using var font = new Font("Segoe UI", Math.Max(7.5f, h * 0.22f), FontStyle.Bold);
            using var brush = new SolidBrush(uColor);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Far
            };

            var rect = new RectangleF(x, y, w, h - 2);
            g.DrawString(_unit, font, brush, rect, format);
        }

        private void DrawGlassSheen(Graphics g, int w, int h)
        {
            int sheenHeight = (int)(h * 0.44f);
            using (var glassBrush = new LinearGradientBrush(
                new Rectangle(0, 0, w, sheenHeight),
                Color.FromArgb(18, 255, 255, 255),
                Color.FromArgb(0, 255, 255, 255),
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(glassBrush, 1, 1, w - 2, sheenHeight);
            }

            // Crisp inner top glass reflection highlight line
            using var sheenPen = new Pen(Color.FromArgb(28, 255, 255, 255), 1f);
            g.DrawLine(sheenPen, 2, 1, w - 3, 1);
        }

        #endregion
    }
}
