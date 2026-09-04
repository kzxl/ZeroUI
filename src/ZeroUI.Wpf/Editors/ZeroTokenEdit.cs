using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
    /// backspace deletion, and theme synchronization.
    /// </summary>
    public class ZeroTokenEdit : Control
    {
        private readonly ObservableCollection<string> _tokens = new ObservableCollection<string>();
        private WrapPanel? _wrapPanel;
        private TextBox? _inputBox;

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(ZeroTokenEdit), new PropertyMetadata("Type and press Enter..."));

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public ObservableCollection<string> Tokens => _tokens;

        public event EventHandler<WpfTokenEventArgs>? TokenAdded;
        public event EventHandler<WpfTokenEventArgs>? TokenRemoved;
        public event EventHandler? TokensChanged;

        static ZeroTokenEdit()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZeroTokenEdit), new FrameworkPropertyMetadata(typeof(ZeroTokenEdit)));
        }

        public ZeroTokenEdit()
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
                TokensChanged?.Invoke(this, EventArgs.Empty);
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

                badgeBorder.Child = sp;
                _wrapPanel.Children.Add(badgeBorder);
            }

            _wrapPanel.Children.Add(_inputBox);
        }

        public void AddToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return;
            string clean = token.Trim();
            if (!_tokens.Contains(clean))
            {
                _tokens.Add(clean);
                TokenAdded?.Invoke(this, new WpfTokenEventArgs(clean, _tokens.Count - 1));
            }
        }

        public void RemoveToken(int index)
        {
            if (index < 0 || index >= _tokens.Count) return;
            string token = _tokens[index];
            _tokens.RemoveAt(index);
            TokenRemoved?.Invoke(this, new WpfTokenEventArgs(token, index));
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
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
            _inputBox?.Focus();
        }
    }
}
