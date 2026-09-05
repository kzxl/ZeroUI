using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Core.Editors;
using ZeroUI.Core.Localization;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Editors
{
    public class WpfTokenEventArgs : EventArgs
    {
        public string Token { get; }
        public int Index { get; }

        public WpfTokenEventArgs(string token, int index)
        {
            Token = token;
            Index = index;
        }
    }

    /// <summary>
    /// Modern anti-aliased Tag/Token/Chip input editor for ZeroUI WPF.
    /// Displays discrete tag badges with dismiss buttons, inline keyboard typing,
    /// backspace deletion, and theme synchronization. Implements <see cref="IZeroEditor"/>.
    /// </summary>
    public class TokenEdit : Control, IZeroEditor
    {
        private readonly ObservableCollection<string> _tokens = new ObservableCollection<string>();
        private WrapPanel? _wrapPanel;
        private TextBox? _inputBox;

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(TokenEdit), new PropertyMetadata(null));

        public static readonly DependencyProperty ReadOnlyProperty =
            DependencyProperty.Register(nameof(ReadOnly), typeof(bool), typeof(TokenEdit), new PropertyMetadata(false));

        public string Placeholder
        {
            get => (string?)GetValue(PlaceholderProperty) ?? ZeroLocalizer.GetString(ZeroStringId.TokenEditPlaceholder);
            set => SetValue(PlaceholderProperty, value);
        }

        public bool ReadOnly
        {
            get => (bool)GetValue(ReadOnlyProperty);
            set => SetValue(ReadOnlyProperty, value);
        }

        public ObservableCollection<string> Tokens => _tokens;

        public object? EditValue
        {
            get => _tokens.ToList();
            set
            {
                _tokens.Clear();
                if (value is IEnumerable<string> list)
                {
                    foreach (var item in list)
                    {
                        if (!string.IsNullOrWhiteSpace(item)) _tokens.Add(item.Trim());
                    }
                }
                else if (value is string s && !string.IsNullOrWhiteSpace(s))
                {
                    foreach (var item in s.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        _tokens.Add(item.Trim());
                    }
                }
                IsModified = true;
                EditValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsModified { get; set; }

        public event EventHandler<WpfTokenEventArgs>? TokenAdded;
        public event EventHandler<WpfTokenEventArgs>? TokenRemoved;
        public event EventHandler? TokensChanged;
        public event EventHandler? EditValueChanged;

        static TokenEdit()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TokenEdit), new FrameworkPropertyMetadata(typeof(TokenEdit)));
        }

        public TokenEdit()
        {
            Background = ZeroWpfTheme.BgInput;
            BorderBrush = ZeroWpfTheme.BorderDefault;
            BorderThickness = new Thickness(1);
            MinHeight = 36;
            FontSize = 13.0;
            Cursor = Cursors.IBeam;

            _tokens.CollectionChanged += (s, e) =>
            {
                RebuildTokensUI();
                IsModified = true;
                TokensChanged?.Invoke(this, EventArgs.Empty);
                EditValueChanged?.Invoke(this, EventArgs.Empty);
            };

            BuildVisualTemplate();
        }

        private void BuildVisualTemplate()
        {
            var border = new Border
            {
                Background = Background,
                BorderBrush = BorderBrush,
                BorderThickness = BorderThickness,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 4, 6, 4)
            };

            _wrapPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            _inputBox = new TextBox
            {
                MinWidth = 80,
                Height = 26,
                Background = Brushes.Transparent,
                Foreground = ZeroWpfTheme.TextPrimary,
                BorderThickness = new Thickness(0),
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2)
            };
            _inputBox.KeyDown += InputBox_KeyDown;

            border.Child = _wrapPanel;
            AddVisualChild(border);
            AddLogicalChild(border);

            RebuildTokensUI();
        }

        private void RebuildTokensUI()
        {
            if (_wrapPanel == null || _inputBox == null) return;
            _wrapPanel.Children.Clear();

            for (int i = 0; i < _tokens.Count; i++)
            {
                int index = i;
                string token = _tokens[i];

                var badgeBorder = new Border
                {
                    Background = ZeroWpfTheme.BgCard,
                    BorderBrush = ZeroWpfTheme.BorderDefault,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(8, 2, 6, 2),
                    Margin = new Thickness(2, 2, 4, 2),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var sp = new StackPanel { Orientation = Orientation.Horizontal };
                var textBlock = new TextBlock
                {
                    Text = token,
                    Foreground = ZeroWpfTheme.TextPrimary,
                    FontSize = 12.0,
                    VerticalAlignment = VerticalAlignment.Center
                };
                sp.Children.Add(textBlock);

                if (!ReadOnly)
                {
                    var closeBtn = new TextBlock
                    {
                        Text = " ✕",
                        Foreground = ZeroWpfTheme.TextMuted,
                        FontSize = 10.0,
                        Cursor = Cursors.Hand,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(4, 0, 0, 0)
                    };
                    closeBtn.MouseEnter += (s, e) => closeBtn.Foreground = ZeroWpfTheme.DangerAccent;
                    closeBtn.MouseLeave += (s, e) => closeBtn.Foreground = ZeroWpfTheme.TextMuted;
                    closeBtn.MouseDown += (s, e) =>
                    {
                        e.Handled = true;
                        RemoveToken(index);
                    };
                    sp.Children.Add(closeBtn);
                }

                badgeBorder.Child = sp;
                _wrapPanel.Children.Add(badgeBorder);
            }

            if (!ReadOnly)
            {
                _wrapPanel.Children.Add(_inputBox);
            }
        }

        public void AddToken(string token)
        {
            if (ReadOnly || string.IsNullOrWhiteSpace(token)) return;
            string clean = token.Trim();
            if (!_tokens.Contains(clean))
            {
                _tokens.Add(clean);
                TokenAdded?.Invoke(this, new WpfTokenEventArgs(clean, _tokens.Count - 1));
            }
        }

        public void RemoveToken(int index)
        {
            if (ReadOnly || index < 0 || index >= _tokens.Count) return;
            string token = _tokens[index];
            _tokens.RemoveAt(index);
            TokenRemoved?.Invoke(this, new WpfTokenEventArgs(token, index));
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (ReadOnly) return;

            if (e.Key == Key.Enter || e.Key == Key.OemComma)
            {
                e.Handled = true;
                string text = _inputBox?.Text.Trim().TrimEnd(',') ?? string.Empty;
                if (!string.IsNullOrEmpty(text))
                {
                    AddToken(text);
                    if (_inputBox != null) _inputBox.Text = string.Empty;
                }
            }
            else if (e.Key == Key.Back && string.IsNullOrEmpty(_inputBox?.Text) && _tokens.Count > 0)
            {
                RemoveToken(_tokens.Count - 1);
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            if (!ReadOnly)
            {
                _inputBox?.Focus();
            }
        }

        public void Reset()
        {
            _tokens.Clear();
            IsModified = false;
            EditValueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear()
        {
            Reset();
        }
    }

    /// <summary>
    /// Backward-compatibility alias for <see cref="TokenEdit"/>.
    /// </summary>
    public class ZeroTokenEdit : TokenEdit
    {
    }
}
