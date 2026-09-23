using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZeroUI.Wpf.Base;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Feedback
{
    public enum InfoBarSeverity
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Modern in-window banner notification alert matching WinUI 3 / Fluent standards.
    /// Supports Info, Success, Warning, and Error severity modes, custom action slot,
    /// dismiss button, and smooth collapse transition.
    /// </summary>
    public class InfoBar : ZeroWpfControlBase
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(InfoBar), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(InfoBar), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty SeverityProperty =
            DependencyProperty.Register(nameof(Severity), typeof(InfoBarSeverity), typeof(InfoBar), new PropertyMetadata(InfoBarSeverity.Info, OnSeverityChanged));

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                nameof(IsOpen),
                typeof(bool),
                typeof(InfoBar),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

        public static readonly DependencyProperty IsClosableProperty =
            DependencyProperty.Register(nameof(IsClosable), typeof(bool), typeof(InfoBar), new PropertyMetadata(true));

        public static readonly DependencyProperty ActionContentProperty =
            DependencyProperty.Register(nameof(ActionContent), typeof(object), typeof(InfoBar), new PropertyMetadata(null));

        public static readonly DependencyProperty CloseCommandProperty =
            DependencyProperty.Register(nameof(CloseCommand), typeof(ICommand), typeof(InfoBar), new PropertyMetadata(null));

        public static readonly RoutedEvent ClosedEvent =
            EventManager.RegisterRoutedEvent(nameof(Closed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(InfoBar));

        public event RoutedEventHandler Closed
        {
            add => AddHandler(ClosedEvent, value);
            remove => RemoveHandler(ClosedEvent, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public InfoBarSeverity Severity
        {
            get => (InfoBarSeverity)GetValue(SeverityProperty);
            set => SetValue(SeverityProperty, value);
        }

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public bool IsClosable
        {
            get => (bool)GetValue(IsClosableProperty);
            set => SetValue(IsClosableProperty, value);
        }

        public object? ActionContent
        {
            get => GetValue(ActionContentProperty);
            set => SetValue(ActionContentProperty, value);
        }

        public ICommand? CloseCommand
        {
            get => (ICommand?)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }

        static InfoBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(InfoBar), new FrameworkPropertyMetadata(typeof(InfoBar)));
        }

        public InfoBar()
        {
            SetResourceReference(BackgroundProperty, "ZeroUI.BgCard");
            SetResourceReference(ForegroundProperty, "ZeroUI.TextPrimary");
            SetResourceReference(BorderBrushProperty, "ZeroUI.BorderDefault");
            BorderThickness = new Thickness(1);
            Visibility = IsOpen ? Visibility.Visible : Visibility.Collapsed;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("PART_CloseButton") is Button closeBtn)
            {
                closeBtn.Click += (s, e) =>
                {
                    IsOpen = false;
                };
            }
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InfoBar bar)
            {
                var isOpen = (bool)e.NewValue;
                bar.Visibility = isOpen ? Visibility.Visible : Visibility.Collapsed;
                if (!isOpen)
                {
                    bar.RaiseEvent(new RoutedEventArgs(ClosedEvent, bar));
                    if (bar.CloseCommand != null && bar.CloseCommand.CanExecute(null))
                    {
                        bar.CloseCommand.Execute(null);
                    }
                }
            }
        }

        private static void OnSeverityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InfoBar bar)
            {
                bar.OnThemeChanged();
            }
        }
    }

    /// <summary>
    /// Legacy alias for <see cref="InfoBar"/>.
    /// </summary>
    [Obsolete("ZeroInfoBar is deprecated. Use InfoBar instead.")]
    public class ZeroInfoBar : InfoBar
    {
        static ZeroInfoBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZeroInfoBar), new FrameworkPropertyMetadata(typeof(InfoBar)));
        }
    }
}
