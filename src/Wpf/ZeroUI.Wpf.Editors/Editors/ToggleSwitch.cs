using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Core.Editors;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Editors
{
    /// <summary>
    /// Modern toggle switch for WPF with form data-binding support.
    /// </summary>
    public class ToggleSwitch : FrameworkElement, IZeroEditor
    {
        private bool _isInternalEditValueSync;

        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(
                nameof(IsChecked),
                typeof(bool),
                typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender, OnIsCheckedChanged));

        public static readonly DependencyProperty EditValueProperty =
            DependencyProperty.Register(
                nameof(EditValue),
                typeof(object),
                typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnEditValueChanged));

        public static readonly DependencyProperty IsModifiedProperty =
            DependencyProperty.Register(
                nameof(IsModified),
                typeof(bool),
                typeof(ToggleSwitch),
                new PropertyMetadata(false));

        public static readonly DependencyProperty ReadOnlyProperty =
            DependencyProperty.Register(
                nameof(ReadOnly),
                typeof(bool),
                typeof(ToggleSwitch),
                new PropertyMetadata(false));

        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        public object? EditValue
        {
            get => GetValue(EditValueProperty);
            set => SetValue(EditValueProperty, value);
        }

        public bool IsModified
        {
            get => (bool)GetValue(IsModifiedProperty);
            set => SetValue(IsModifiedProperty, value);
        }

        public bool ReadOnly
        {
            get => (bool)GetValue(ReadOnlyProperty);
            set => SetValue(ReadOnlyProperty, value);
        }

        public event EventHandler<bool>? CheckedChanged;
        public event EventHandler? EditValueChanged;

        public void Reset()
        {
            IsChecked = false;
            IsModified = false;
            EditValueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Clear() => Reset();

        public ToggleSwitch()
        {
            Width = 44;
            Height = 24;
            Cursor = Cursors.Hand;
            ZeroWpfTheme.ThemeChanged += () => InvalidateVisual();
        }

        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToggleSwitch sw)
            {
                if (!sw._isInternalEditValueSync)
                {
                    sw._isInternalEditValueSync = true;
                    sw.EditValue = e.NewValue;
                    sw._isInternalEditValueSync = false;
                }
                sw.IsModified = true;
                sw.CheckedChanged?.Invoke(sw, (bool)e.NewValue);
                sw.EditValueChanged?.Invoke(sw, EventArgs.Empty);
            }
        }

        private static void OnEditValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToggleSwitch sw && !sw._isInternalEditValueSync)
            {
                bool bVal = false;
                if (e.NewValue is bool b) bVal = b;
                else if (bool.TryParse(e.NewValue?.ToString(), out bool parsed)) bVal = parsed;

                sw._isInternalEditValueSync = true;
                sw.IsChecked = bVal;
                sw._isInternalEditValueSync = false;
                sw.EditValueChanged?.Invoke(sw, EventArgs.Empty);
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (ReadOnly || !IsEnabled) return;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                IsChecked = !IsChecked;
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            Brush trackBrush = IsChecked ? ZeroWpfTheme.PrimaryAccent : ZeroWpfTheme.BgInput;
            Pen trackPen = IsChecked ? ZeroWpfTheme.AccentPen : ZeroWpfTheme.BorderPen;

            // Track pill
            dc.DrawRoundedRectangle(trackBrush, trackPen, new Rect(0.5, 0.5, w - 1, h - 1), h / 2.0, h / 2.0);

            // Thumb
            double thumbDiameter = h - 6;
            double thumbX = IsChecked ? (w - thumbDiameter - 3) : 3;
            double thumbY = 3;

            dc.DrawEllipse(Brushes.White, null, new Point(thumbX + thumbDiameter / 2.0, thumbY + thumbDiameter / 2.0), thumbDiameter / 2.0, thumbDiameter / 2.0);
        }
    }

    /// <summary>
    /// Backward-compatibility alias for <see cref="ToggleSwitch"/>.
    /// </summary>
    [Obsolete("ZeroSwitch is deprecated. Use ToggleSwitch instead.")]
    public class ZeroSwitch : ToggleSwitch
    {
    }
}
