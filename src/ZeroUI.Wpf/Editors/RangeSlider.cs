using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Core.Editors;

namespace ZeroUI.Wpf.Editors
{
    /// <summary>
    /// Dual-thumb range slider control for selecting a continuous range [LowerValue, UpperValue] within [Minimum, Maximum].
    /// Supports dragging individual thumbs, dragging the middle range bar, step snapping, and numeric display.
    /// </summary>
    [TemplatePart(Name = "PART_Track", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_ActiveRange", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_LowerThumb", Type = typeof(Thumb))]
    [TemplatePart(Name = "PART_UpperThumb", Type = typeof(Thumb))]
    public class RangeSlider : Control, IZeroEditor
    {
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(double),
                typeof(RangeSlider),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnRangeBoundsChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(double),
                typeof(RangeSlider),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender, OnRangeBoundsChanged));

        public static readonly DependencyProperty LowerValueProperty =
            DependencyProperty.Register(
                nameof(LowerValue),
                typeof(double),
                typeof(RangeSlider),
                new FrameworkPropertyMetadata(20.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender, OnLowerValueChanged));

        public static readonly DependencyProperty UpperValueProperty =
            DependencyProperty.Register(
                nameof(UpperValue),
                typeof(double),
                typeof(RangeSlider),
                new FrameworkPropertyMetadata(80.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender, OnUpperValueChanged));

        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register(
                nameof(Step),
                typeof(double),
                typeof(RangeSlider),
                new PropertyMetadata(1.0));

        public static readonly DependencyProperty MinRangeProperty =
            DependencyProperty.Register(
                nameof(MinRange),
                typeof(double),
                typeof(RangeSlider),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty IsRangeDraggableProperty =
            DependencyProperty.Register(
                nameof(IsRangeDraggable),
                typeof(bool),
                typeof(RangeSlider),
                new PropertyMetadata(true));

        public static readonly DependencyProperty ShowRangeTextProperty =
            DependencyProperty.Register(
                nameof(ShowRangeText),
                typeof(bool),
                typeof(RangeSlider),
                new PropertyMetadata(true));

        public static readonly DependencyProperty PrefixProperty =
            DependencyProperty.Register(
                nameof(Prefix),
                typeof(string),
                typeof(RangeSlider),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty SuffixProperty =
            DependencyProperty.Register(
                nameof(Suffix),
                typeof(string),
                typeof(RangeSlider),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty FormatStringProperty =
            DependencyProperty.Register(
                nameof(FormatString),
                typeof(string),
                typeof(RangeSlider),
                new PropertyMetadata("F0"));

        public static readonly DependencyProperty ActiveRangeBrushProperty =
            DependencyProperty.Register(
                nameof(ActiveRangeBrush),
                typeof(Brush),
                typeof(RangeSlider),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ReadOnlyProperty =
            DependencyProperty.Register(
                nameof(ReadOnly),
                typeof(bool),
                typeof(RangeSlider),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsModifiedProperty =
            DependencyProperty.Register(
                nameof(IsModified),
                typeof(bool),
                typeof(RangeSlider),
                new PropertyMetadata(false));

        public static readonly DependencyProperty EditValueProperty =
            DependencyProperty.Register(
                nameof(EditValue),
                typeof(object),
                typeof(RangeSlider),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double LowerValue
        {
            get => (double)GetValue(LowerValueProperty);
            set => SetValue(LowerValueProperty, value);
        }

        public double UpperValue
        {
            get => (double)GetValue(UpperValueProperty);
            set => SetValue(UpperValueProperty, value);
        }

        public double Step
        {
            get => (double)GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }

        public double MinRange
        {
            get => (double)GetValue(MinRangeProperty);
            set => SetValue(MinRangeProperty, value);
        }

        public bool IsRangeDraggable
        {
            get => (bool)GetValue(IsRangeDraggableProperty);
            set => SetValue(IsRangeDraggableProperty, value);
        }

        public bool ShowRangeText
        {
            get => (bool)GetValue(ShowRangeTextProperty);
            set => SetValue(ShowRangeTextProperty, value);
        }

        public string Prefix
        {
            get => (string)GetValue(PrefixProperty);
            set => SetValue(PrefixProperty, value);
        }

        public string Suffix
        {
            get => (string)GetValue(SuffixProperty);
            set => SetValue(SuffixProperty, value);
        }

        public string FormatString
        {
            get => (string)GetValue(FormatStringProperty);
            set => SetValue(FormatStringProperty, value);
        }

        public Brush? ActiveRangeBrush
        {
            get => (Brush?)GetValue(ActiveRangeBrushProperty);
            set => SetValue(ActiveRangeBrushProperty, value);
        }

        public bool ReadOnly
        {
            get => (bool)GetValue(ReadOnlyProperty);
            set => SetValue(ReadOnlyProperty, value);
        }

        public bool IsModified
        {
            get => (bool)GetValue(IsModifiedProperty);
            set => SetValue(IsModifiedProperty, value);
        }

        public object? EditValue
        {
            get => GetValue(EditValueProperty);
            set => SetValue(EditValueProperty, value);
        }

        public string FormattedRangeText =>
            $"{Prefix}{LowerValue.ToString(FormatString)}{Suffix} — {Prefix}{UpperValue.ToString(FormatString)}{Suffix}";

        public event EventHandler<(double Lower, double Upper)>? RangeChanged;
        public event EventHandler? EditValueChanged;

        private FrameworkElement? _track;
        private FrameworkElement? _activeRange;
        private Thumb? _lowerThumb;
        private Thumb? _upperThumb;
        private bool _isUpdating;
        private Point _rangeDragStart;
        private double _dragStartLower;
        private double _dragStartUpper;

        static RangeSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RangeSlider), new FrameworkPropertyMetadata(typeof(RangeSlider)));
        }

        public RangeSlider()
        {
            SizeChanged += (s, e) => UpdateLayoutPositions();
            Loaded += (s, e) => UpdateLayoutPositions();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _track = GetTemplateChild("PART_Track") as FrameworkElement;
            _activeRange = GetTemplateChild("PART_ActiveRange") as FrameworkElement;
            _lowerThumb = GetTemplateChild("PART_LowerThumb") as Thumb;
            _upperThumb = GetTemplateChild("PART_UpperThumb") as Thumb;

            if (_lowerThumb != null)
            {
                _lowerThumb.DragDelta += OnLowerThumbDragDelta;
            }

            if (_upperThumb != null)
            {
                _upperThumb.DragDelta += OnUpperThumbDragDelta;
            }

            if (_activeRange != null)
            {
                _activeRange.MouseLeftButtonDown += OnActiveRangeMouseDown;
                _activeRange.MouseMove += OnActiveRangeMouseMove;
                _activeRange.MouseLeftButtonUp += OnActiveRangeMouseUp;
            }

            UpdateLayoutPositions();
        }

        private void OnLowerThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (ReadOnly || _track == null || _track.ActualWidth <= 0) return;

            double span = Maximum - Minimum;
            if (span <= 0) return;

            double deltaVal = (e.HorizontalChange / _track.ActualWidth) * span;
            double newVal = SnapToStep(Math.Max(Minimum, Math.Min(UpperValue - MinRange, LowerValue + deltaVal)));
            if (Math.Abs(newVal - LowerValue) > 0.0001)
            {
                LowerValue = newVal;
                IsModified = true;
            }
        }

        private void OnUpperThumbDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (ReadOnly || _track == null || _track.ActualWidth <= 0) return;

            double span = Maximum - Minimum;
            if (span <= 0) return;

            double deltaVal = (e.HorizontalChange / _track.ActualWidth) * span;
            double newVal = SnapToStep(Math.Min(Maximum, Math.Max(LowerValue + MinRange, UpperValue + deltaVal)));
            if (Math.Abs(newVal - UpperValue) > 0.0001)
            {
                UpperValue = newVal;
                IsModified = true;
            }
        }

        private void OnActiveRangeMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ReadOnly || !IsRangeDraggable || _activeRange == null) return;

            _activeRange.CaptureMouse();
            _rangeDragStart = e.GetPosition(this);
            _dragStartLower = LowerValue;
            _dragStartUpper = UpperValue;
            e.Handled = true;
        }

        private void OnActiveRangeMouseMove(object sender, MouseEventArgs e)
        {
            if (ReadOnly || !IsRangeDraggable || _activeRange == null || !_activeRange.IsMouseCaptured || _track == null || _track.ActualWidth <= 0)
                return;

            double span = Maximum - Minimum;
            if (span <= 0) return;

            Point current = e.GetPosition(this);
            double deltaPixels = current.X - _rangeDragStart.X;
            double deltaVal = (deltaPixels / _track.ActualWidth) * span;

            double rangeLength = _dragStartUpper - _dragStartLower;
            double newLower = _dragStartLower + deltaVal;
            double newUpper = _dragStartUpper + deltaVal;

            if (newLower < Minimum)
            {
                newLower = Minimum;
                newUpper = Minimum + rangeLength;
            }
            else if (newUpper > Maximum)
            {
                newUpper = Maximum;
                newLower = Maximum - rangeLength;
            }

            newLower = SnapToStep(newLower);
            newUpper = SnapToStep(newUpper);

            if (Math.Abs(newLower - LowerValue) > 0.0001 || Math.Abs(newUpper - UpperValue) > 0.0001)
            {
                _isUpdating = true;
                try
                {
                    LowerValue = newLower;
                    UpperValue = newUpper;
                    IsModified = true;
                    OnRangeValuesUpdated();
                }
                finally
                {
                    _isUpdating = false;
                }
            }
            e.Handled = true;
        }

        private void OnActiveRangeMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_activeRange != null && _activeRange.IsMouseCaptured)
            {
                _activeRange.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private double SnapToStep(double val)
        {
            if (Step <= 0) return val;
            double snapped = Math.Round(val / Step) * Step;
            return Math.Max(Minimum, Math.Min(Maximum, snapped));
        }

        public void UpdateLayoutPositions()
        {
            if (_track == null || _lowerThumb == null || _upperThumb == null || _activeRange == null) return;

            double trackWidth = _track.ActualWidth;
            if (trackWidth <= 0) return;

            double span = Maximum - Minimum;
            if (span <= 0) span = 1.0;

            double lowerPct = Math.Max(0.0, Math.Min(1.0, (LowerValue - Minimum) / span));
            double upperPct = Math.Max(0.0, Math.Min(1.0, (UpperValue - Minimum) / span));

            double thumbHalfWidth = _lowerThumb.ActualWidth > 0 ? _lowerThumb.ActualWidth / 2.0 : 8.0;
            double usableWidth = Math.Max(0, trackWidth - (thumbHalfWidth * 2));

            double lowerX = lowerPct * usableWidth;
            double upperX = upperPct * usableWidth;

            Canvas.SetLeft(_lowerThumb, lowerX);
            Canvas.SetLeft(_upperThumb, upperX);

            Canvas.SetLeft(_activeRange, lowerX + thumbHalfWidth);
            _activeRange.Width = Math.Max(0, upperX - lowerX);
        }

        private void OnRangeValuesUpdated()
        {
            UpdateLayoutPositions();
            EditValue = (LowerValue, UpperValue);
            RangeChanged?.Invoke(this, (LowerValue, UpperValue));
            EditValueChanged?.Invoke(this, EventArgs.Empty);
        }

        private static void OnRangeBoundsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RangeSlider slider)
            {
                slider.UpdateLayoutPositions();
            }
        }

        private static void OnLowerValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RangeSlider slider && !slider._isUpdating)
            {
                slider.OnRangeValuesUpdated();
            }
        }

        private static void OnUpperValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RangeSlider slider && !slider._isUpdating)
            {
                slider.OnRangeValuesUpdated();
            }
        }

        public void Reset()
        {
            LowerValue = Minimum;
            UpperValue = Maximum;
            IsModified = false;
        }

        public void Clear()
        {
            Reset();
        }
    }
}
