using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ZeroUI.Wpf.Editors;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Navigation
{
    public class WpfWizardPageValidatingEventArgs : CancelEventArgs
    {
        public int PageIndex { get; }
        public ZeroWizardPage Page { get; }
        public string? ErrorMessage { get; set; }

        public WpfWizardPageValidatingEventArgs(int pageIndex, ZeroWizardPage page)
        {
            PageIndex = pageIndex;
            Page = page;
        }
    }

    /// <summary>
    /// Represents an individual step/page container in a ZeroWizard sequence for WPF.
    /// </summary>
    public class ZeroWizardPage : ContentControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(ZeroWizardPage), new PropertyMetadata("Step Title"));

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(ZeroWizardPage), new PropertyMetadata("Configure this step."));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(string), typeof(ZeroWizardPage), new PropertyMetadata("📋"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        public string Icon
        {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public event EventHandler<WpfWizardPageValidatingEventArgs>? ValidatingStep;

        internal bool ValidatePage(int index, out string? error)
        {
            error = null;
            if (ValidatingStep != null)
            {
                var args = new WpfWizardPageValidatingEventArgs(index, this);
                ValidatingStep(this, args);
                if (args.Cancel)
                {
                    error = args.ErrorMessage;
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Modern Enterprise Multi-Step Wizard container for ZeroUI WPF.
    /// Guides users through sequential configuration steps with step indicators,
    /// per-step validation, back/next/finish navigation, and dark/light theming.
    /// </summary>
    public class ZeroWizard : Control
    {
        private readonly ObservableCollection<ZeroWizardPage> _pages = new ObservableCollection<ZeroWizardPage>();
        private int _currentStep = 0;

        private TextBlock? _titleBlock;
        private TextBlock? _subBlock;
        private StackPanel? _stepsIndicatorStack;
        private ContentControl? _pageContentHost;

        private Button? _btnBack;
        private Button? _btnNext;
        private Button? _btnFinish;
        private Button? _btnCancel;

        public ObservableCollection<ZeroWizardPage> Pages => _pages;

        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                if (value >= 0 && value < _pages.Count && _currentStep != value)
                {
                    _currentStep = value;
                    UpdateWizardView();
                }
            }
        }

        public event EventHandler? StepChanged;
        public event EventHandler? Finished;
        public event EventHandler? Cancelled;

        static ZeroWizard()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZeroWizard), new FrameworkPropertyMetadata(typeof(ZeroWizard)));
        }

        public ZeroWizard()
        {
            Background = ZeroWpfTheme.BgCard;
            BorderBrush = ZeroWpfTheme.BorderDefault;
            BorderThickness = new Thickness(1);
            Width = 700;
            Height = 500;

            _pages.CollectionChanged += (s, e) =>
            {
                if (_pages.Count == 1) UpdateWizardView();
                else RebuildStepIndicators();
            };

            BuildVisualTemplate();
        }

        private void BuildVisualTemplate()
        {
            var rootBorder = new Border
            {
                Background = Background,
                BorderBrush = BorderBrush,
                BorderThickness = BorderThickness,
                CornerRadius = new CornerRadius(6)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64, GridUnitType.Pixel) }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });   // Page Content
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56, GridUnitType.Pixel) }); // Footer

            // 1. Header
            var headerBorder = new Border
            {
                Background = ZeroWpfTheme.BgInput,
                BorderBrush = ZeroWpfTheme.BorderDefault,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(18, 10, 18, 10)
            };

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _titleBlock = new TextBlock { FontSize = 14.0, FontWeight = FontWeights.Bold, Foreground = ZeroWpfTheme.TextPrimary };
            _subBlock = new TextBlock { FontSize = 11.5, Foreground = ZeroWpfTheme.TextMuted, Margin = new Thickness(0, 2, 0, 0) };
            titleStack.Children.Add(_titleBlock);
            titleStack.Children.Add(_subBlock);
            Grid.SetColumn(titleStack, 0);
            headerGrid.Children.Add(titleStack);

            _stepsIndicatorStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(_stepsIndicatorStack, 1);
            headerGrid.Children.Add(_stepsIndicatorStack);

            headerBorder.Child = headerGrid;
            Grid.SetRow(headerBorder, 0);
            mainGrid.Children.Add(headerBorder);

            // 2. Page Content Host
            _pageContentHost = new ContentControl { Margin = new Thickness(18) };
            Grid.SetRow(_pageContentHost, 1);
            mainGrid.Children.Add(_pageContentHost);

            // 3. Footer Command Bar
            var footerBorder = new Border
            {
                Background = ZeroWpfTheme.BgInput,
                BorderBrush = ZeroWpfTheme.BorderDefault,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(18, 10, 18, 10)
            };

            var footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _btnCancel = new Button { Content = "Cancel", Width = 80, Height = 32, Margin = new Thickness(0, 0, 8, 0) };
            _btnCancel.Click += (s, e) => Cancelled?.Invoke(this, EventArgs.Empty);
            Grid.SetColumn(_btnCancel, 0);
            footerGrid.Children.Add(_btnCancel);

            var actionsStack = new StackPanel { Orientation = Orientation.Horizontal };
            _btnBack = new Button { Content = "◀ Back", Width = 90, Height = 32, Margin = new Thickness(4, 0, 4, 0) };
            _btnBack.Click += (s, e) => PreviousStep();
            actionsStack.Children.Add(_btnBack);

            _btnNext = new Button { Content = "Next ▶", Width = 90, Height = 32, Margin = new Thickness(4, 0, 4, 0) };
            _btnNext.Click += (s, e) => NextStep();
            actionsStack.Children.Add(_btnNext);

            _btnFinish = new Button { Content = "✔ Finish", Width = 100, Height = 32, Margin = new Thickness(4, 0, 4, 0), Visibility = Visibility.Collapsed };
            _btnFinish.Click += (s, e) => CompleteWizard();
            actionsStack.Children.Add(_btnFinish);

            Grid.SetColumn(actionsStack, 2);
            footerGrid.Children.Add(actionsStack);

            footerBorder.Child = footerGrid;
            Grid.SetRow(footerBorder, 2);
            mainGrid.Children.Add(footerBorder);

            rootBorder.Child = mainGrid;
            AddVisualChild(rootBorder);
            AddLogicalChild(rootBorder);

            UpdateWizardView();
        }

        public bool NextStep()
        {
            if (_pages.Count == 0 || _currentStep >= _pages.Count - 1) return false;

            var curPage = _pages[_currentStep];
            if (!curPage.ValidatePage(_currentStep, out string? error))
            {
                if (!string.IsNullOrEmpty(error))
                {
                    MessageBox.Show(error, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return false;
            }

            _currentStep++;
            UpdateWizardView();
            return true;
        }

        public void PreviousStep()
        {
            if (_currentStep > 0)
            {
                _currentStep--;
                UpdateWizardView();
            }
        }

        public void CompleteWizard()
        {
            if (_pages.Count == 0) return;
            var curPage = _pages[_currentStep];
            if (curPage.ValidatePage(_currentStep, out string? error))
            {
                Finished?.Invoke(this, EventArgs.Empty);
            }
            else if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateWizardView()
        {
            if (_pages.Count > 0 && _currentStep >= 0 && _currentStep < _pages.Count)
            {
                var cur = _pages[_currentStep];
                if (_titleBlock != null) _titleBlock.Text = $"{cur.Icon}  {cur.Title}";
                if (_subBlock != null) _subBlock.Text = cur.Subtitle;
                if (_pageContentHost != null) _pageContentHost.Content = cur;
            }

            bool isLast = (_currentStep == _pages.Count - 1);
            if (_btnBack != null) _btnBack.IsEnabled = (_currentStep > 0);
            if (_btnNext != null) _btnNext.Visibility = isLast ? Visibility.Collapsed : Visibility.Visible;
            if (_btnFinish != null) _btnFinish.Visibility = isLast ? Visibility.Visible : Visibility.Collapsed;

            RebuildStepIndicators();
            StepChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RebuildStepIndicators()
        {
            if (_stepsIndicatorStack == null) return;
            _stepsIndicatorStack.Children.Clear();

            for (int i = 0; i < _pages.Count; i++)
            {
                bool isDone = i < _currentStep;
                bool isCurrent = i == _currentStep;

                var dot = new Border
                {
                    Width = 24,
                    Height = 24,
                    CornerRadius = new CornerRadius(12),
                    Background = (isCurrent || isDone) ? ZeroWpfTheme.PrimaryAccent : ZeroWpfTheme.BgCard,
                    BorderBrush = (isCurrent || isDone) ? ZeroWpfTheme.PrimaryAccent : ZeroWpfTheme.BorderDefault,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(4, 0, 4, 0)
                };

                var num = new TextBlock
                {
                    Text = isDone ? "✓" : (i + 1).ToString(),
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = (isCurrent || isDone) ? Brushes.White : ZeroWpfTheme.TextSecondary,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                dot.Child = num;
                _stepsIndicatorStack.Children.Add(dot);
            }
        }

        public void AddPage(ZeroWizardPage page)
        {
            _pages.Add(page);
        }
    }
}
