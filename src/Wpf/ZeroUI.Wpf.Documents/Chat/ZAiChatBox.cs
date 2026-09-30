using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ZeroUI.Core.AiMl;
using ZeroUI.Wpf.Theme;

namespace ZeroUI.Wpf.Documents
{
    /// <summary>
    /// Enterprise AI chat box control for WPF with streaming token display, Markdown-styled message bubbles,
    /// prompt suggestions, and responsive conversation layout.
    /// </summary>
    public class ZAiChatBox : Control
    {
        private Border _rootBorder = null!;
        private ScrollViewer _messagesScrollViewer = null!;
        private StackPanel _messagesPanel = null!;
        private TextBox _promptInputBox = null!;
        private Button _sendButton = null!;
        private Button _stopButton = null!;
        private Button _clearButton = null!;
        private TextBlock _statusTextBlock = null!;
        private StackPanel _suggestionsPanel = null!;
        private TextBlock _modelNameTextBlock = null!;
        private bool _isInternalScroll = false;

        static ZAiChatBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ZAiChatBox), new FrameworkPropertyMetadata(typeof(ZAiChatBox)));
        }

        public ZAiChatBox()
        {
            Messages = new ObservableCollection<ChatMessage>();
            PromptSuggestions = new ObservableCollection<string>();

            Messages.CollectionChanged += OnMessagesCollectionChanged;
            PromptSuggestions.CollectionChanged += OnPromptSuggestionsCollectionChanged;

            InitializeComponentHierarchy();
            Loaded += (s, e) => RebuildAllMessages();
        }

        #region Dependency Properties

        public static readonly DependencyProperty MessagesProperty =
            DependencyProperty.Register(nameof(Messages), typeof(ObservableCollection<ChatMessage>), typeof(ZAiChatBox),
                new PropertyMetadata(null, OnMessagesPropertyChanged));

        public static readonly DependencyProperty PromptSuggestionsProperty =
            DependencyProperty.Register(nameof(PromptSuggestions), typeof(ObservableCollection<string>), typeof(ZAiChatBox),
                new PropertyMetadata(null));

        public static readonly DependencyProperty AssistantNameProperty =
            DependencyProperty.Register(nameof(AssistantName), typeof(string), typeof(ZAiChatBox),
                new PropertyMetadata("ZeroCopilot", OnAssistantNameChanged));

        public static readonly DependencyProperty ModelNameProperty =
            DependencyProperty.Register(nameof(ModelName), typeof(string), typeof(ZAiChatBox),
                new PropertyMetadata("ZeroInference Edge", OnModelNameChanged));

        public static readonly DependencyProperty IsGeneratingProperty =
            DependencyProperty.Register(nameof(IsGenerating), typeof(bool), typeof(ZAiChatBox),
                new PropertyMetadata(false, OnIsGeneratingChanged));

        public static readonly DependencyProperty StreamingStateProperty =
            DependencyProperty.Register(nameof(StreamingState), typeof(ChatStreamingState), typeof(ZAiChatBox),
                new PropertyMetadata(ChatStreamingState.Idle, OnStreamingStateChanged));

        public ObservableCollection<ChatMessage> Messages
        {
            get => (ObservableCollection<ChatMessage>)GetValue(MessagesProperty);
            set => SetValue(MessagesProperty, value);
        }

        public ObservableCollection<string> PromptSuggestions
        {
            get => (ObservableCollection<string>)GetValue(PromptSuggestionsProperty);
            set => SetValue(PromptSuggestionsProperty, value);
        }

        public string AssistantName
        {
            get => (string)GetValue(AssistantNameProperty);
            set => SetValue(AssistantNameProperty, value);
        }

        public string ModelName
        {
            get => (string)GetValue(ModelNameProperty);
            set => SetValue(ModelNameProperty, value);
        }

        public bool IsGenerating
        {
            get => (bool)GetValue(IsGeneratingProperty);
            set => SetValue(IsGeneratingProperty, value);
        }

        public ChatStreamingState StreamingState
        {
            get => (ChatStreamingState)GetValue(StreamingStateProperty);
            set => SetValue(StreamingStateProperty, value);
        }

        private static void OnMessagesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZAiChatBox chat)
            {
                if (e.OldValue is ObservableCollection<ChatMessage> oldCol)
                    oldCol.CollectionChanged -= chat.OnMessagesCollectionChanged;
                if (e.NewValue is ObservableCollection<ChatMessage> newCol)
                    newCol.CollectionChanged += chat.OnMessagesCollectionChanged;
                chat.RebuildAllMessages();
            }
        }

        private static void OnAssistantNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZAiChatBox chat && chat._modelNameTextBlock != null)
                chat._modelNameTextBlock.Text = $"{e.NewValue} ({chat.ModelName})";
        }

        private static void OnModelNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZAiChatBox chat && chat._modelNameTextBlock != null)
                chat._modelNameTextBlock.Text = $"{chat.AssistantName} ({e.NewValue})";
        }

        private static void OnIsGeneratingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZAiChatBox chat)
            {
                bool generating = (bool)e.NewValue;
                chat._sendButton.Visibility = generating ? Visibility.Collapsed : Visibility.Visible;
                chat._stopButton.Visibility = generating ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static void OnStreamingStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZAiChatBox chat && chat._statusTextBlock != null)
            {
                var state = (ChatStreamingState)e.NewValue;
                chat._statusTextBlock.Text = state switch
                {
                    ChatStreamingState.Thinking => "Thinking...",
                    ChatStreamingState.Streaming => "Generating response...",
                    ChatStreamingState.Error => "Error during generation",
                    _ => "Ready"
                };
            }
        }

        #endregion

        #region Events

        public event EventHandler<string>? SendMessageRequested;
        public event EventHandler? StopGenerationRequested;
        public event EventHandler? ClearChatRequested;

        #endregion

        #region UI Layout Construction

        private void InitializeComponentHierarchy()
        {
            _rootBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)), // Slate 900
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)), // Slate 700
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Messages
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Suggestions
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Input deck

            // 1. Header
            var headerBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(14, 10, 14, 10)
            };
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var botIcon = new TextBlock
            {
                Text = "🤖",
                FontSize = 18,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            titleStack.Children.Add(botIcon);

            var titleInfoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _modelNameTextBlock = new TextBlock
            {
                Text = $"{AssistantName} ({ModelName})",
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                FontSize = 13
            };
            _statusTextBlock = new TextBlock
            {
                Text = "Ready",
                Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153)), // Emerald 400
                FontSize = 11
            };
            titleInfoStack.Children.Add(_modelNameTextBlock);
            titleInfoStack.Children.Add(_statusTextBlock);
            titleStack.Children.Add(titleInfoStack);
            headerGrid.Children.Add(titleStack);

            _clearButton = new Button
            {
                Content = "🗑 Clear",
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Padding = new Thickness(8, 4, 8, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            _clearButton.Click += (s, e) =>
            {
                ClearMessages();
                ClearChatRequested?.Invoke(this, EventArgs.Empty);
            };
            Grid.SetColumn(_clearButton, 1);
            headerGrid.Children.Add(_clearButton);
            headerBorder.Child = headerGrid;
            mainGrid.Children.Add(headerBorder);

            // 2. Messages Scroller
            _messagesScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(14)
            };
            _messagesPanel = new StackPanel();
            _messagesScrollViewer.Content = _messagesPanel;
            Grid.SetRow(_messagesScrollViewer, 1);
            mainGrid.Children.Add(_messagesScrollViewer);

            // 3. Suggestions Panel
            _suggestionsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(14, 4, 14, 6)
            };
            Grid.SetRow(_suggestionsPanel, 2);
            mainGrid.Children.Add(_suggestionsPanel);

            // 4. Input Deck
            var inputBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(160, 30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(12, 10, 12, 10)
            };
            var inputGrid = new Grid();
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _promptInputBox = new TextBox
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                Foreground = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 8, 10, 8),
                FontSize = 13,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 110,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 8, 0)
            };
            _promptInputBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                {
                    e.Handled = true;
                    SubmitPrompt();
                }
            };
            inputGrid.Children.Add(_promptInputBox);

            var buttonStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Bottom };

            _sendButton = new Button
            {
                Content = "Send ➢",
                Background = new SolidColorBrush(Color.FromRgb(79, 70, 229)), // Indigo
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Padding = new Thickness(14, 8, 14, 8)
            };
            _sendButton.Click += (s, e) => SubmitPrompt();
            buttonStack.Children.Add(_sendButton);

            _stopButton = new Button
            {
                Content = "⏹ Stop",
                Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)), // Red
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Padding = new Thickness(14, 8, 14, 8),
                Visibility = Visibility.Collapsed
            };
            _stopButton.Click += (s, e) => StopGenerationRequested?.Invoke(this, EventArgs.Empty);
            buttonStack.Children.Add(_stopButton);

            Grid.SetColumn(buttonStack, 1);
            inputGrid.Children.Add(buttonStack);
            inputBorder.Child = inputGrid;
            Grid.SetRow(inputBorder, 3);
            mainGrid.Children.Add(inputBorder);

            _rootBorder.Child = mainGrid;
            AddVisualChild(_rootBorder);
            AddLogicalChild(_rootBorder);
        }

        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index) => _rootBorder;

        protected override Size MeasureOverride(Size constraint)
        {
            _rootBorder.Measure(constraint);
            return _rootBorder.DesiredSize;
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            _rootBorder.Arrange(new Rect(arrangeBounds));
            return arrangeBounds;
        }

        #endregion

        #region Prompt Execution

        private void SubmitPrompt()
        {
            string text = _promptInputBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(text) || IsGenerating) return;

            _promptInputBox.Clear();
            AppendUserMessage(text);
            SendMessageRequested?.Invoke(this, text);
        }

        #endregion

        #region Message Bubbles Rendering

        private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildAllMessages();
        }

        private void OnPromptSuggestionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildPromptSuggestions();
        }

        public void RebuildAllMessages()
        {
            if (_messagesPanel == null) return;
            _messagesPanel.Children.Clear();

            if (Messages == null || Messages.Count == 0)
            {
                var placeholder = new TextBlock
                {
                    Text = "No conversation history. Ask a question to begin.",
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 30, 0, 0)
                };
                _messagesPanel.Children.Add(placeholder);
                return;
            }

            foreach (var msg in Messages)
            {
                var bubble = CreateMessageBubble(msg);
                _messagesPanel.Children.Add(bubble);
            }

            ScrollToBottom();
        }

        private UIElement CreateMessageBubble(ChatMessage msg)
        {
            bool isUser = msg.Role == ChatRole.User;

            var container = new Border
            {
                Margin = new Thickness(0, 4, 0, 8),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 650
            };

            var bubbleBorder = new Border
            {
                Background = isUser
                    ? new SolidColorBrush(Color.FromRgb(30, 41, 59))
                    : new SolidColorBrush(Color.FromRgb(17, 24, 39)),
                BorderBrush = isUser
                    ? new SolidColorBrush(Color.FromRgb(99, 102, 241)) // Violet border for user
                    : new SolidColorBrush(Color.FromRgb(51, 65, 85)), // Slate border for assistant
                BorderThickness = new Thickness(1),
                CornerRadius = isUser ? new CornerRadius(10, 10, 2, 10) : new CornerRadius(10, 10, 10, 2),
                Padding = new Thickness(12, 8, 12, 8)
            };

            var bubbleStack = new StackPanel();

            // Header of bubble
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var senderTextBlock = new TextBlock
            {
                Text = isUser ? (msg.SenderName ?? "Operator") : (msg.ModelName ?? AssistantName),
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                Foreground = isUser ? new SolidColorBrush(Color.FromRgb(165, 180, 252)) : new SolidColorBrush(Color.FromRgb(52, 211, 153))
            };
            headerGrid.Children.Add(senderTextBlock);

            var timeTextBlock = new TextBlock
            {
                Text = msg.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(timeTextBlock, 1);
            headerGrid.Children.Add(timeTextBlock);
            bubbleStack.Children.Add(headerGrid);

            // Body text / Markdown rendering
            var contentBlock = new TextBox
            {
                Text = msg.Content,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                BorderThickness = new Thickness(0),
                FontSize = 13,
                Padding = new Thickness(0),
                Cursor = Cursors.Arrow
            };
            bubbleStack.Children.Add(contentBlock);

            // Copy action for assistant messages
            if (!isUser)
            {
                var copyButton = new Button
                {
                    Content = "📋 Copy",
                    Background = Brushes.Transparent,
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    BorderThickness = new Thickness(0),
                    FontSize = 10,
                    Cursor = Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                copyButton.Click += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(msg.Content))
                    {
                        Clipboard.SetText(msg.Content);
                    }
                };
                bubbleStack.Children.Add(copyButton);
            }

            bubbleBorder.Child = bubbleStack;
            container.Child = bubbleBorder;
            return container;
        }

        public void RebuildPromptSuggestions()
        {
            if (_suggestionsPanel == null) return;
            _suggestionsPanel.Children.Clear();

            foreach (var suggestion in PromptSuggestions)
            {
                var chip = new Button
                {
                    Content = suggestion,
                    Background = new SolidColorBrush(Color.FromArgb(140, 30, 41, 59)),
                    Foreground = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10, 4, 10, 4),
                    Margin = new Thickness(0, 0, 8, 0),
                    Cursor = Cursors.Hand,
                    FontSize = 11
                };
                chip.Click += (s, e) =>
                {
                    _promptInputBox.Text = suggestion;
                    _promptInputBox.Focus();
                    _promptInputBox.CaretIndex = suggestion.Length;
                };
                _suggestionsPanel.Children.Add(chip);
            }
        }

        #endregion

        #region Public Methods

        public void AppendUserMessage(string text)
        {
            var msg = ChatMessage.User(text);
            Messages.Add(msg);
        }

        public ChatMessage AppendAssistantMessage(string initialText = "")
        {
            var msg = ChatMessage.Assistant(initialText, ModelName);
            Messages.Add(msg);
            return msg;
        }

        public void StreamToken(string messageId, string token)
        {
            var msg = Messages.FirstOrDefault(m => m.Id == messageId);
            if (msg != null)
            {
                msg.Content += token;
                msg.IsStreaming = true;
                RebuildAllMessages();
            }
        }

        public void CompleteStreaming(string messageId)
        {
            var msg = Messages.FirstOrDefault(m => m.Id == messageId);
            if (msg != null)
            {
                msg.IsStreaming = false;
                RebuildAllMessages();
            }
        }

        public void ClearMessages()
        {
            Messages.Clear();
            RebuildAllMessages();
        }

        public void ScrollToBottom()
        {
            _messagesScrollViewer?.ScrollToBottom();
        }

        #endregion
    }

    #region Backward Compatibility Shims

    [Obsolete("AiChatBox is deprecated. Please migrate to ZAiChatBox instead.")]
    public class AiChatBox : ZAiChatBox { }

    [Obsolete("ZeroAiChatBox is deprecated. Please migrate to ZAiChatBox instead.")]
    public class ZeroAiChatBox : ZAiChatBox { }

    [Obsolete("ZCopilotBox is deprecated. Please migrate to ZAiChatBox instead.")]
    public class ZCopilotBox : ZAiChatBox { }

    #endregion
}
