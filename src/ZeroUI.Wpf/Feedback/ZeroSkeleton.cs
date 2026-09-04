using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Feedback
{
    public enum WpfSkeletonShape
    {
        Rectangle,
        RoundedRectangle,
        Circle
    }

    /// <summary>
    /// Modern hardware-accelerated animated Skeleton/Shimmer loading placeholder for ZeroUI WPF.
    /// Provides smooth gradient shimmer waves imitating UI cards, text, and avatars
    /// while asynchronous data requests resolve.
    /// </summary>
    public class ZeroSkeleton : Control
    {
        private Rectangle? _rectShape;
        private Ellipse? _circleShape;

        public static readonly DependencyProperty ShapeProperty =
            DependencyProperty.Register(nameof(Shape), typeof(WpfSkeletonShape), typeof(ZeroSkeleton), new PropertyMetadata(WpfSkeletonShape.RoundedRectangle, OnShapeChanged));

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(ZeroSkeleton), new PropertyMetadata(new CornerRadius(4), OnCornerRadiusChanged));

        public WpfSkeletonShape Shape
        {
            get => (WpfSkeletonShape)GetValue(ShapeProperty);
            set => SetValue(ShapeProperty, value);
        }

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        static ZeroSkeleton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZeroSkeleton), new FrameworkPropertyMetadata(typeof(ZeroSkeleton)));
        }

        public ZeroSkeleton()
        {
            Height = 24;
            Width = 160;
            BuildShimmerVisual();
        }

        private void BuildShimmerVisual()
        {
            var grid = new Grid();

            // Setup hardware-accelerated LinearGradientBrush shimmer
            var lgb = new LinearGradientBrush
            {
                StartPoint = new Point(-1, 0),
                EndPoint = new Point(2, 0)
            };

            Color baseColor = Color.FromArgb(40, 128, 128, 128);
            Color shimmer = Color.FromArgb(90, 200, 200, 200);

            lgb.GradientStops.Add(new GradientStop(baseColor, 0.0));
            lgb.GradientStops.Add(new GradientStop(shimmer, 0.5));
            lgb.GradientStops.Add(new GradientStop(baseColor, 1.0));

            // Start shimmer translation animation
            var animStart = new PointAnimation
            {
                From = new Point(-1.5, 0),
                To = new Point(1.5, 0),
                Duration = TimeSpan.FromSeconds(1.6),
                RepeatBehavior = RepeatBehavior.Forever
            };
            var animEnd = new PointAnimation
            {
                From = new Point(0, 0),
                To = new Point(3.0, 0),
                Duration = TimeSpan.FromSeconds(1.6),
                RepeatBehavior = RepeatBehavior.Forever
            };

            lgb.BeginAnimation(LinearGradientBrush.StartPointProperty, animStart);
            lgb.BeginAnimation(LinearGradientBrush.EndPointProperty, animEnd);

            _rectShape = new Rectangle
            {
                Fill = lgb,
                RadiusX = CornerRadius.TopLeft,
                RadiusY = CornerRadius.TopLeft,
                Visibility = (Shape == WpfSkeletonShape.Circle) ? Visibility.Collapsed : Visibility.Visible
            };
            grid.Children.Add(_rectShape);

            _circleShape = new Ellipse
            {
                Fill = lgb,
                Visibility = (Shape == WpfSkeletonShape.Circle) ? Visibility.Visible : Visibility.Collapsed
            };
            grid.Children.Add(_circleShape);

            AddVisualChild(grid);
            AddLogicalChild(grid);
        }

        private static void OnShapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZeroSkeleton s)
            {
                var shape = (WpfSkeletonShape)e.NewValue;
                if (s._rectShape != null) s._rectShape.Visibility = (shape == WpfSkeletonShape.Circle) ? Visibility.Collapsed : Visibility.Visible;
                if (s._circleShape != null) s._circleShape.Visibility = (shape == WpfSkeletonShape.Circle) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZeroSkeleton s && s._rectShape != null)
            {
                var cr = (CornerRadius)e.NewValue;
                s._rectShape.RadiusX = cr.TopLeft;
                s._rectShape.RadiusY = cr.TopLeft;
            }
        }
    }
}
