using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ZeroUI.Core.AiMl;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Documents
{
    /// <summary>
    /// Enterprise AI chat box control for WinForms with streaming token display, message bubbles,
    /// quick prompt suggestions, and responsive conversation layout.
    /// </summary>
    [ToolboxItem(true)]
    [Category("ZeroUI")]
    [DefaultEvent("SendMessageRequested")]
    public class ZAiChatBox : Control
    {
        private readonly Panel _headerPanel;
        private readonly Panel _messagesContainer;
        private readonly FlowLayoutPanel _suggestionsPanel;
        private readonly Panel _inputPanel;
        private readonly TextBox _promptInputBox;
        private readonly Button _sendButton;
        private readonly Button _stopButton;
        private readonly Button _clearButton;
        private readonly Label _titleLabel;
        private readonly Label _statusLabel;

        private ObservableCollection<ChatMessage> _messages;
        private ObservableCollection<string> _promptSuggestions;
        private string _assistantName = "ZeroCopilot";
        private string _modelName = "ZeroInference Edge";
        private bool _isGenerating;
        private ChatStreamingState _streamingState = ChatStreamingState.Idle;

        public ZAiChatBox()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            _messages = new ObservableCollection<ChatMessage>();
            _promptSuggestions = new ObservableCollection<string>();

            _messages.CollectionChanged += OnMessagesCollectionChanged;
            _promptSuggestions.CollectionChanged += OnPromptSuggestionsCollectionChanged;

            BackColor = Color.FromArgb(15, 23, 42); // Slate 900

            // 1. Header Panel
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(12, 8, 12, 8)
            };

            var botIcon = new Label
            {
                Text = "🤖",
                Font = new Font("Segoe UI Emoji", 14f, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(12, 10)
            };
            _headerPanel.Controls.Add(botIcon);

            _titleLabel = new Label
            {
                Text = $"{_assistantName} ({_modelName})",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 245, 249),
                AutoSize = true,
                Location = new Point(44, 6)
            };
            _headerPanel.Controls.Add(_titleLabel);

            _statusLabel = new Label
            {
                Text = "Ready",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(52, 211, 153),
                AutoSize = true,
                Location = new Point(44, 26)
            };
            _headerPanel.Controls.Add(_statusLabel);

            _clearButton = new Button
            {
                Text = "🗑 Clear",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(68, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 80, 10),
                Cursor = Cursors.Hand
            };
            _clearButton.FlatAppearance.BorderSize = 0;
            _clearButton.Click += (s, e) =>
            {
                ClearMessages();
                ClearChatRequested?.Invoke(this, EventArgs.Empty);
            };
            _headerPanel.Controls.Add(_clearButton);

            // 2. Input Panel (Dock Bottom)
            _inputPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 72,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(12, 8, 12, 8)
            };

            _promptInputBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill
            };
            _promptInputBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    SubmitPrompt();
                }
            };

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 90,
                Padding = new Padding(8, 0, 0, 0)
            };

            _sendButton = new Button
            {
                Text = "Send ➢",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(79, 70, 229),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand
            };
            _sendButton.FlatAppearance.BorderSize = 0;
            _sendButton.Click += (s, e) => SubmitPrompt();
            buttonPanel.Controls.Add(_sendButton);

            _stopButton = new Button
            {
                Text = "⏹ Stop",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _stopButton.FlatAppearance.BorderSize = 0;
            _stopButton.Click += (s, e) => StopGenerationRequested?.Invoke(this, EventArgs.Empty);
            buttonPanel.Controls.Add(_stopButton);

            _inputPanel.Controls.Add(_promptInputBox);
            _inputPanel.Controls.Add(buttonPanel);

            // 3. Suggestions Panel (Dock Bottom above Input)
            _suggestionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.FromArgb(20, 29, 47),
                Padding = new Padding(12, 4, 12, 4),
                WrapContents = false,
                AutoScroll = true
            };

            // 4. Messages Container (Dock Fill)
            _messagesContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(15, 23, 42),
                Padding = new Padding(14)
            };

            Controls.Add(_messagesContainer);
            Controls.Add(_suggestionsPanel);
            Controls.Add(_inputPanel);
            Controls.Add(_headerPanel);

            ZeroTheme.ThemeChanged += OnThemeChanged;
            Resize += (s, e) =>
            {
                _clearButton.Location = new Point(Width - 80, 10);
                RebuildAllMessages();
            };
        }

        #region Properties

        [Category("ZeroUI")]
        [Description("Conversation message history collection.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ObservableCollection<ChatMessage> Messages
        {
            get => _messages;
            set
            {
                if (_messages != value)
                {
                    _messages.CollectionChanged -= OnMessagesCollectionChanged;
                    _messages = value ?? new ObservableCollection<ChatMessage>();
                    _messages.CollectionChanged += OnMessagesCollectionChanged;
                    RebuildAllMessages();
                }
            }
        }

        [Category("ZeroUI")]
        [Description("Actionable quick prompt suggestions chips.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ObservableCollection<string> PromptSuggestions
        {
            get => _promptSuggestions;
            set
            {
                if (_promptSuggestions != value)
                {
                    _promptSuggestions.CollectionChanged -= OnPromptSuggestionsCollectionChanged;
                    _promptSuggestions = value ?? new ObservableCollection<string>();
                    _promptSuggestions.CollectionChanged += OnPromptSuggestionsCollectionChanged;
                    RebuildPromptSuggestions();
                }
            }
        }

        [Category("ZeroUI")]
        [Description("AI Assistant display name.")]
        [DefaultValue("ZeroCopilot")]
        public string AssistantName
        {
            get => _assistantName;
            set
            {
                _assistantName = value;
                _titleLabel.Text = $"{_assistantName} ({_modelName})";
            }
        }

        [Category("ZeroUI")]
        [Description("Target AI model identifier.")]
        [DefaultValue("ZeroInference Edge")]
        public string ModelName
        {
            get => _modelName;
            set
            {
                _modelName = value;
                _titleLabel.Text = $"{_assistantName} ({_modelName})";
            }
        }

        [Category("ZeroUI")]
        [Description("Indicates whether the AI is currently generating tokens.")]
        [DefaultValue(false)]
        public bool IsGenerating
        {
            get => _isGenerating;
            set
            {
                _isGenerating = value;
                _sendButton.Visible = !_isGenerating;
                _stopButton.Visible = _isGenerating;
            }
        }

        [Category("ZeroUI")]
        [Description("Current streaming state of the assistant.")]
        [DefaultValue(ChatStreamingState.Idle)]
        public ChatStreamingState StreamingState
        {
            get => _streamingState;
            set
            {
                _streamingState = value;
                _statusLabel.Text = value switch
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

        #region Prompt Execution

        private void SubmitPrompt()
        {
            string text = _promptInputBox.Text.Trim();
            if (string.IsNullOrEmpty(text) || _isGenerating) return;

            _promptInputBox.Clear();
            AppendUserMessage(text);
            SendMessageRequested?.Invoke(this, text);
        }

        #endregion

        #region Message Bubbles

        private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildAllMessages();

        private void OnPromptSuggestionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildPromptSuggestions();

        public void RebuildAllMessages()
        {
            _messagesContainer.SuspendLayout();
            _messagesContainer.Controls.Clear();

            int yOffset = 10;
            int availableWidth = Math.Max(200, _messagesContainer.ClientSize.Width - 40);

            if (_messages.Count == 0)
            {
                var emptyLabel = new Label
                {
                    Text = "No conversation history. Ask a question to begin.",
                    Font = new Font("Segoe UI", 10f),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    AutoSize = true,
                    Location = new Point((availableWidth - 280) / 2, 40)
                };
                _messagesContainer.Controls.Add(emptyLabel);
                _messagesContainer.ResumeLayout();
                return;
            }

            foreach (var msg in _messages)
            {
                var bubble = CreateBubbleControl(msg, availableWidth);
                bubble.Location = new Point(msg.Role == ChatRole.User ? availableWidth - bubble.Width + 14 : 14, yOffset);
                _messagesContainer.Controls.Add(bubble);
                yOffset += bubble.Height + 12;
            }

            _messagesContainer.ResumeLayout();
            ScrollToBottom();
        }

        private Control CreateBubbleControl(ChatMessage msg, int containerWidth)
        {
            bool isUser = msg.Role == ChatRole.User;
            int maxBubbleWidth = Math.Min(600, (int)(containerWidth * 0.85));

            var bubble = new Panel
            {
                BackColor = isUser ? Color.FromArgb(30, 41, 59) : Color.FromArgb(17, 24, 39),
                Padding = new Padding(10, 8, 10, 8)
            };

            // Header: Sender + Timestamp
            var senderLabel = new Label
            {
                Text = isUser ? (msg.SenderName ?? "Operator") : (msg.ModelName ?? _assistantName),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = isUser ? Color.FromArgb(165, 180, 252) : Color.FromArgb(52, 211, 153),
                AutoSize = true,
                Location = new Point(8, 6)
            };
            bubble.Controls.Add(senderLabel);

            var timeLabel = new Label
            {
                Text = msg.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Location = new Point(senderLabel.Right + 8, 7)
            };
            bubble.Controls.Add(timeLabel);

            // Message text box
            var txt = new TextBox
            {
                Text = msg.Content,
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = bubble.BackColor,
                ForeColor = Color.FromArgb(241, 245, 249),
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(8, 28)
            };

            // Measure height
            using var g = CreateGraphics();
            var size = g.MeasureString(msg.Content, txt.Font, maxBubbleWidth - 24);
            int textWidth = Math.Max(120, Math.Min(maxBubbleWidth - 24, (int)size.Width + 10));
            int textHeight = Math.Max(24, (int)size.Height + 10);

            txt.Size = new Size(textWidth, textHeight);
            bubble.Controls.Add(txt);

            int totalBubbleWidth = Math.Max(textWidth + 24, 180);
            int totalBubbleHeight = textHeight + 36;

            // Copy button for assistant
            if (!isUser)
            {
                var copyBtn = new Button
                {
                    Text = "📋 Copy",
                    Font = new Font("Segoe UI", 8f),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(54, 22),
                    Location = new Point(totalBubbleWidth - 62, totalBubbleHeight - 26),
                    Cursor = Cursors.Hand
                };
                copyBtn.FlatAppearance.BorderSize = 0;
                copyBtn.Click += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(msg.Content))
                    {
                        Clipboard.SetText(msg.Content);
                    }
                };
                bubble.Controls.Add(copyBtn);
            }

            bubble.Size = new Size(totalBubbleWidth, totalBubbleHeight);

            // Custom border paint
            bubble.Paint += (s, e) =>
            {
                var penColor = isUser ? Color.FromArgb(99, 102, 241) : Color.FromArgb(51, 65, 85);
                using var p = new Pen(penColor, 1f);
                e.Graphics.DrawRectangle(p, 0, 0, bubble.Width - 1, bubble.Height - 1);
            };

            return bubble;
        }

        public void RebuildPromptSuggestions()
        {
            _suggestionsPanel.SuspendLayout();
            _suggestionsPanel.Controls.Clear();

            foreach (var suggestion in _promptSuggestions)
            {
                var chip = new Button
                {
                    Text = suggestion,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Color.FromArgb(226, 232, 240),
                    BackColor = Color.FromArgb(30, 41, 59),
                    FlatStyle = FlatStyle.Flat,
                    AutoSize = true,
                    Margin = new Padding(0, 0, 6, 0),
                    Cursor = Cursors.Hand
                };
                chip.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
                chip.Click += (s, e) =>
                {
                    _promptInputBox.Text = suggestion;
                    _promptInputBox.Focus();
                    _promptInputBox.SelectionStart = suggestion.Length;
                };
                _suggestionsPanel.Controls.Add(chip);
            }

            _suggestionsPanel.ResumeLayout();
        }

        #endregion

        #region Public Methods

        public void AppendUserMessage(string text)
        {
            var msg = ChatMessage.User(text);
            _messages.Add(msg);
        }

        public ChatMessage AppendAssistantMessage(string initialText = "")
        {
            var msg = ChatMessage.Assistant(initialText, _modelName);
            _messages.Add(msg);
            return msg;
        }

        public void StreamToken(string messageId, string token)
        {
            var msg = _messages.FirstOrDefault(m => m.Id == messageId);
            if (msg != null)
            {
                msg.Content += token;
                msg.IsStreaming = true;
                RebuildAllMessages();
            }
        }

        public void CompleteStreaming(string messageId)
        {
            var msg = _messages.FirstOrDefault(m => m.Id == messageId);
            if (msg != null)
            {
                msg.IsStreaming = false;
                RebuildAllMessages();
            }
        }

        public void ClearMessages()
        {
            _messages.Clear();
            RebuildAllMessages();
        }

        public void ScrollToBottom()
        {
            _messagesContainer.VerticalScroll.Value = _messagesContainer.VerticalScroll.Maximum;
            _messagesContainer.PerformLayout();
        }

        #endregion

        #region Theme & Cleanup

        private void OnThemeChanged(object? sender, EventArgs e) => Invalidate();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ZeroTheme.ThemeChanged -= OnThemeChanged;
            }
            base.Dispose(disposing);
        }

        #endregion
    }

    #region Backward Compatibility Shims

    [Obsolete("AiChatBox is deprecated. Please migrate to ZAiChatBox instead.")]
    [ToolboxItem(false)]
    public class AiChatBox : ZAiChatBox { }

    [Obsolete("ZeroAiChatBox is deprecated. Please migrate to ZAiChatBox instead.")]
    [ToolboxItem(false)]
    public class ZeroAiChatBox : ZAiChatBox { }

    [Obsolete("ZCopilotBox is deprecated. Please migrate to ZAiChatBox instead.")]
    [ToolboxItem(false)]
    public class ZCopilotBox : ZAiChatBox { }

    #endregion
}
