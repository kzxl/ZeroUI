using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Core.Editors;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Editors
{
    /// <summary>
    /// Metadata token descriptor for <see cref="TokenPatternEditor"/>.
    /// </summary>
    public class TokenChipItem : INotifyPropertyChanged
    {
        private string _token = "";
        private string _displayName = "";
        private string _description = "";
        private string _sampleValue = "";

        public string Token
        {
            get => _token;
            set { _token = value; OnPropertyChanged(nameof(Token)); }
        }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(nameof(DisplayName)); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public string SampleValue
        {
            get => _sampleValue;
            set { _sampleValue = value; OnPropertyChanged(nameof(SampleValue)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public TokenChipItem() { }

        public TokenChipItem(string token, string displayName, string description, string sampleValue)
        {
            _token = token;
            _displayName = displayName;
            _description = description;
            _sampleValue = sampleValue;
        }

        public override string ToString() => $"{DisplayName} ({Token})";
    }

    /// <summary>
    /// Composite Filename / Text Pattern Editor with interactive token insertion chips,
    /// live evaluated preview, and keyboard shortcuts.
    /// </summary>
    public class TokenPatternEditor : Control, IZeroEditor
    {
        private UIElement? _child;
        private TextBlock? _lblTitle;
        private TextBox? _txtInput;
        private WrapPanel? _pnlChips;
        private TextBlock? _lblPreviewPrefix;
        private TextBlock? _txtPreview;
        private bool _isModified;
        private bool _suppressTextEvent;

        private readonly ObservableCollection<TokenChipItem> _tokens = new ObservableCollection<TokenChipItem>();
        private readonly Dictionary<string, string> _sampleValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        #region Dependency Properties

        public static readonly DependencyProperty LabelTextProperty =
            DependencyProperty.Register(nameof(LabelText), typeof(string), typeof(TokenPatternEditor),
                new PropertyMetadata("Filename Pattern", OnLabelTextChanged));

        public static readonly DependencyProperty PatternProperty =
            DependencyProperty.Register(nameof(Pattern), typeof(string), typeof(TokenPatternEditor),
                new FrameworkPropertyMetadata("{name}_export.{ext}", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPatternChanged));

        public static readonly DependencyProperty ShowLabelProperty =
            DependencyProperty.Register(nameof(ShowLabel), typeof(bool), typeof(TokenPatternEditor),
                new PropertyMetadata(true, OnShowLabelChanged));

        public static readonly DependencyProperty ShowChipsProperty =
            DependencyProperty.Register(nameof(ShowChips), typeof(bool), typeof(TokenPatternEditor),
                new PropertyMetadata(true, OnShowChipsChanged));

        public static readonly DependencyProperty ShowPreviewProperty =
            DependencyProperty.Register(nameof(ShowPreview), typeof(bool), typeof(TokenPatternEditor),
                new PropertyMetadata(true, OnShowPreviewChanged));

        public static readonly DependencyProperty ReadOnlyProperty =
            DependencyProperty.Register(nameof(ReadOnly), typeof(bool), typeof(TokenPatternEditor),
                new PropertyMetadata(false, OnReadOnlyChanged));

        public string LabelText
        {
            get => (string)GetValue(LabelTextProperty);
            set => SetValue(LabelTextProperty, value);
        }

        public string Pattern
        {
            get => (string)GetValue(PatternProperty);
            set => SetValue(PatternProperty, value);
        }

        public string Text
        {
            get => Pattern;
            set => Pattern = value;
        }

        public bool ShowLabel
        {
            get => (bool)GetValue(ShowLabelProperty);
            set => SetValue(ShowLabelProperty, value);
        }

        public bool ShowChips
        {
            get => (bool)GetValue(ShowChipsProperty);
            set => SetValue(ShowChipsProperty, value);
        }

        public bool ShowPreview
        {
            get => (bool)GetValue(ShowPreviewProperty);
            set => SetValue(ShowPreviewProperty, value);
        }

        public bool ReadOnly
        {
            get => (bool)GetValue(ReadOnlyProperty);
            set => SetValue(ReadOnlyProperty, value);
        }

        public ObservableCollection<TokenChipItem> Tokens => _tokens;
        public Dictionary<string, string> SampleValues => _sampleValues;

        #endregion

        #region Events

        public event EventHandler<string>? PatternChanged;

        #endregion

        #region IZeroEditor Implementation

        public object? EditValue
        {
            get => Pattern;
            set
            {
                if (value is string s) Pattern = s;
                else if (value == null) Pattern = "";
            }
        }

        public event EventHandler? EditValueChanged;

        public bool IsModified
        {
            get => _isModified;
            set => _isModified = value;
        }

        public void Reset()
        {
            Pattern = "{name}_export.{ext}";
            _isModified = false;
            UpdatePreview();
        }

        public void Clear()
        {
            Pattern = "";
            _isModified = false;
            UpdatePreview();
        }

        #endregion

        public TokenPatternEditor()
        {
            Focusable = true;
            InitializeDefaultTokens();
            ZeroWpfTheme.ThemeChanged += ApplyTheme;

            BuildVisualTree();
        }

        private void InitializeDefaultTokens()
        {
            _tokens.Add(new TokenChipItem("{name}", "{name}", "Original file name without extension", "IMG_2024"));
            _tokens.Add(new TokenChipItem("{ext}", "{ext}", "File extension", "jpg"));
            _tokens.Add(new TokenChipItem("{w}", "{w}", "Image pixel width", "4000"));
            _tokens.Add(new TokenChipItem("{h}", "{h}", "Image pixel height", "3000"));
            _tokens.Add(new TokenChipItem("{n:000}", "{n:000}", "Sequential index number (3 digits)", "001"));
            _tokens.Add(new TokenChipItem("{date}", "{date}", "Current ISO date (YYYY-MM-DD)", DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            _tokens.Add(new TokenChipItem("{date:yyyyMMdd}", "{date:yyyyMMdd}", "Compact date (YYYYMMDD)", DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)));
            _tokens.Add(new TokenChipItem("{parent}", "{parent}", "Parent folder name", "Photos"));

            foreach (var t in _tokens)
            {
                _sampleValues[t.Token] = t.SampleValue;
            }
        }

        private static void OnLabelTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TokenPatternEditor ctrl && ctrl._lblTitle != null)
            {
                ctrl._lblTitle.Text = e.NewValue as string ?? "";
            }
        }

        private static void OnPatternChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TokenPatternEditor ctrl)
            {
                string newPattern = e.NewValue as string ?? "";
                if (!ctrl._suppressTextEvent && ctrl._txtInput != null && ctrl._txtInput.Text != newPattern)
                {
                    ctrl._suppressTextEvent = true;
                    try
                    {
                        ctrl._txtInput.Text = newPattern;
                    }
                    finally
                    {
                        ctrl._suppressTextEvent = false;
                    }
                }
                ctrl.UpdatePreview();
                ctrl.PatternChanged?.Invoke(ctrl, newPattern);
                ctrl.EditValueChanged?.Invoke(ctrl, EventArgs.Empty);
            }
        }

        private static void OnShowLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TokenPatternEditor ctrl && ctrl._lblTitle != null)
            {
                ctrl._lblTitle.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static void OnShowChipsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TokenPatternEditor ctrl && ctrl._pnlChips != null)
            {
                ctrl._pnlChips.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static void OnShowPreviewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TokenPatternEditor ctrl)
            {
                var vis = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
                if (ctrl._lblPreviewPrefix != null) ctrl._lblPreviewPrefix.Visibility = vis;
                if (ctrl._txtPreview != null) ctrl._txtPreview.Visibility = vis;
            }
        }

        private static void OnReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TokenPatternEditor ctrl && ctrl._txtInput != null)
            {
                ctrl._txtInput.IsReadOnly = (bool)e.NewValue;
            }
        }

        private void BuildVisualTree()
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical };

            // 1. Label
            _lblTitle = new TextBlock
            {
                Text = LabelText,
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 4),
                Visibility = ShowLabel ? Visibility.Visible : Visibility.Collapsed
            };
            stack.Children.Add(_lblTitle);

            // 2. Input Box
            _txtInput = new TextBox
            {
                Text = Pattern,
                Height = 24,
                Padding = new Thickness(6, 2, 6, 2),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 6),
                IsReadOnly = ReadOnly
            };
            _txtInput.TextChanged += (s, e) =>
            {
                if (_suppressTextEvent) return;
                _suppressTextEvent = true;
                try
                {
                    Pattern = _txtInput.Text;
                    _isModified = true;
                }
                finally
                {
                    _suppressTextEvent = false;
                }
                UpdatePreview();
            };
            stack.Children.Add(_txtInput);

            // 3. Token Chips Bar
            _pnlChips = new WrapPanel
            {
                Margin = new Thickness(0, 0, 0, 4),
                Visibility = ShowChips ? Visibility.Visible : Visibility.Collapsed
            };
            RebuildChips();
            stack.Children.Add(_pnlChips);

            // 4. Live Preview
            var previewDock = new DockPanel { Margin = new Thickness(0, 2, 0, 0) };
            _lblPreviewPrefix = new TextBlock
            {
                Text = "Preview: ",
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = ShowPreview ? Visibility.Visible : Visibility.Collapsed
            };
            DockPanel.SetDock(_lblPreviewPrefix, Dock.Left);
            previewDock.Children.Add(_lblPreviewPrefix);

            _txtPreview = new TextBlock
            {
                Text = "",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = ShowPreview ? Visibility.Visible : Visibility.Collapsed
            };
            previewDock.Children.Add(_txtPreview);
            stack.Children.Add(previewDock);

            _child = stack;
            AddVisualChild(stack);
            AddLogicalChild(stack);

            ApplyTheme();
            UpdatePreview();
        }

        public void RebuildChips()
        {
            if (_pnlChips == null) return;
            _pnlChips.Children.Clear();

            foreach (var tokenItem in _tokens)
            {
                var chipBorder = new Border
                {
                    CornerRadius = new CornerRadius(3),
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = new Thickness(0, 0, 4, 4),
                    Cursor = Cursors.Hand,
                    ToolTip = $"{tokenItem.Description}\nExample: {tokenItem.SampleValue}\nClick to insert token",
                    Tag = tokenItem.Token
                };

                var txtChip = new TextBlock
                {
                    Text = tokenItem.DisplayName,
                    FontSize = 9.5
                };
                chipBorder.Child = txtChip;

                chipBorder.MouseEnter += (s, e) =>
                {
                    if (s is Border b) b.Background = ZeroWpfTheme.BgHover;
                };
                chipBorder.MouseLeave += (s, e) =>
                {
                    if (s is Border b) b.Background = ZeroWpfTheme.BgInput;
                };

                chipBorder.MouseLeftButtonUp += (s, e) =>
                {
                    if (ReadOnly) return;
                    if (s is Border b && b.Tag is string token)
                    {
                        InsertToken(token);
                    }
                };

                _pnlChips.Children.Add(chipBorder);
            }
            ApplyTheme();
        }

        /// <summary>
        /// Inserts the specified token at the current caret position of the input box.
        /// </summary>
        public void InsertToken(string token)
        {
            if (_txtInput == null || ReadOnly) return;

            int caret = _txtInput.CaretIndex;
            string current = _txtInput.Text ?? "";
            if (caret < 0 || caret > current.Length) caret = current.Length;

            string nextText = current.Insert(caret, token);
            _txtInput.Text = nextText;
            _txtInput.CaretIndex = caret + token.Length;
            _txtInput.Focus();
        }

        public void UpdatePreview()
        {
            if (_txtPreview == null) return;

            string pattern = Pattern ?? "";
            string result = pattern;

            // Evaluate samples
            foreach (var kvp in _sampleValues)
            {
                result = result.Replace(kvp.Key, kvp.Value);
            }

            // Fallback evaluation for date patterns like {date:yyyyMMdd}
            result = Regex.Replace(result, @"\{date(?::([^}]+))?\}", m =>
            {
                string fmt = m.Groups[1].Success && !string.IsNullOrEmpty(m.Groups[1].Value) ? m.Groups[1].Value : "yyyy-MM-dd";
                try
                {
                    return DateTime.Now.ToString(fmt, CultureInfo.InvariantCulture);
                }
                catch
                {
                    return DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
            });

            // Fallback evaluation for sequence patterns like {n:000}
            result = Regex.Replace(result, @"\{n(?::([^}]+))?\}", m =>
            {
                string fmt = m.Groups[1].Success && !string.IsNullOrEmpty(m.Groups[1].Value) ? m.Groups[1].Value : "000";
                try
                {
                    return 1.ToString(fmt, CultureInfo.InvariantCulture);
                }
                catch
                {
                    return "001";
                }
            });

            _txtPreview.Text = result;
        }

        public void ApplyTheme()
        {
            Background = Brushes.Transparent;
            Foreground = ZeroWpfTheme.TextPrimary;

            if (_lblTitle != null) _lblTitle.Foreground = ZeroWpfTheme.TextSecondary;

            if (_txtInput != null)
            {
                _txtInput.Background = ZeroWpfTheme.BgInput;
                _txtInput.Foreground = ZeroWpfTheme.TextPrimary;
                _txtInput.BorderBrush = ZeroWpfTheme.BorderSubtle;
            }

            if (_pnlChips != null)
            {
                foreach (UIElement child in _pnlChips.Children)
                {
                    if (child is Border b)
                    {
                        b.Background = ZeroWpfTheme.BgInput;
                        b.BorderBrush = ZeroWpfTheme.BorderSubtle;
                        b.BorderThickness = new Thickness(1);
                        if (b.Child is TextBlock tb)
                        {
                            tb.Foreground = ZeroWpfTheme.TextSecondary;
                        }
                    }
                }
            }

            if (_lblPreviewPrefix != null) _lblPreviewPrefix.Foreground = ZeroWpfTheme.TextMuted;
            if (_txtPreview != null) _txtPreview.Foreground = ZeroWpfTheme.PrimaryAccent;
        }

        #region Visual / Logical Tree Overrides

        protected override int VisualChildrenCount => _child != null ? 1 : 0;
        protected override Visual GetVisualChild(int index) => _child ?? throw new ArgumentOutOfRangeException(nameof(index));

        protected override Size MeasureOverride(Size constraint)
        {
            if (_child is UIElement el)
            {
                el.Measure(constraint);
                return el.DesiredSize;
            }
            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            if (_child is UIElement el)
            {
                el.Arrange(new Rect(arrangeBounds));
            }
            return arrangeBounds;
        }

        #endregion
    }
}
