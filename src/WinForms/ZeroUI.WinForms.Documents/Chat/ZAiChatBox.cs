using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZeroUI.Core.AiMl;
using ZeroUI.WinForms.Theme;

namespace ZeroUI.WinForms.Documents
{
    /// <summary>
    /// Enterprise AI chat box control for WinForms with fluid animations, token streaming,
    /// markdown formatting, attachment ingestion, and atomic undo actions.
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

        private readonly FlowLayoutPanel _attachmentToolbar;
        private readonly Button _btnUploadImage;
        private readonly Button _btnUploadPdf;
        private bool _enableAttachments = true;

        private ObservableCollection<ChatMessage> _messages;
        private ObservableCollection<string> _promptSuggestions;
        private string _assistantName = "ZeroCopilot";
        private string _modelName = "ZeroInference Edge";
        private bool _isGenerating;
        private ChatStreamingState _streamingState = ChatStreamingState.Idle;

        private Control? _thinkingBubble;
        private bool _cancelStreaming;
        private bool _suppressRebuild;

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
                Text = "Sẵn sàng",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(52, 211, 153),
                AutoSize = true,
                Location = new Point(44, 26)
            };
            _headerPanel.Controls.Add(_statusLabel);

            _clearButton = new Button
            {
                Text = "🗑 Clear",
                Font = new Font("Segoe UI", 8.5f),
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
                Width = 92,
                Padding = new Padding(8, 0, 0, 0)
            };

            _sendButton = new Button
            {
                Text = "Gửi ➢",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(79, 70, 229),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand
            };
            _sendButton.FlatAppearance.BorderSize = 0;
            _sendButton.MouseEnter += (s, e) => _sendButton.BackColor = Color.FromArgb(99, 102, 241);
            _sendButton.MouseLeave += (s, e) => _sendButton.BackColor = Color.FromArgb(79, 70, 229);
            _sendButton.Click += (s, e) => SubmitPrompt();
            buttonPanel.Controls.Add(_sendButton);

            _stopButton = new Button
            {
                Text = "⏹ Dừng",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _stopButton.FlatAppearance.BorderSize = 0;
            _stopButton.Click += (s, e) =>
            {
                _cancelStreaming = true;
                StopGenerationRequested?.Invoke(this, EventArgs.Empty);
            };
            buttonPanel.Controls.Add(_stopButton);

            _inputPanel.Controls.Add(_promptInputBox);
            _inputPanel.Controls.Add(buttonPanel);

            // 2.5 Attachment Toolbar (Dock Bottom above Input)
            _attachmentToolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.FromArgb(20, 28, 44),
                Padding = new Padding(12, 4, 12, 4),
                WrapContents = false
            };

            _btnUploadImage = new Button
            {
                Text = "📷 Tải hình ảnh (PO / Báo giá)",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(147, 197, 253),
                BackColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(190, 28),
                Margin = new Padding(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            _btnUploadImage.FlatAppearance.BorderColor = Color.FromArgb(59, 130, 246);
            _btnUploadImage.FlatAppearance.BorderSize = 1;
            _btnUploadImage.MouseEnter += (s, e) => { _btnUploadImage.BackColor = Color.FromArgb(37, 99, 235); _btnUploadImage.ForeColor = Color.White; };
            _btnUploadImage.MouseLeave += (s, e) => { _btnUploadImage.BackColor = Color.FromArgb(30, 41, 59); _btnUploadImage.ForeColor = Color.FromArgb(147, 197, 253); };
            _btnUploadImage.Click += (s, e) => TriggerImageUpload();

            _btnUploadPdf = new Button
            {
                Text = "📄 Tải tệp PDF",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 114, 182),
                BackColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 28),
                Cursor = Cursors.Hand
            };
            _btnUploadPdf.FlatAppearance.BorderColor = Color.FromArgb(236, 72, 153);
            _btnUploadPdf.FlatAppearance.BorderSize = 1;
            _btnUploadPdf.MouseEnter += (s, e) => { _btnUploadPdf.BackColor = Color.FromArgb(219, 39, 119); _btnUploadPdf.ForeColor = Color.White; };
            _btnUploadPdf.MouseLeave += (s, e) => { _btnUploadPdf.BackColor = Color.FromArgb(30, 41, 59); _btnUploadPdf.ForeColor = Color.FromArgb(244, 114, 182); };
            _btnUploadPdf.Click += (s, e) => TriggerPdfUpload();

            _attachmentToolbar.Controls.Add(_btnUploadImage);
            _attachmentToolbar.Controls.Add(_btnUploadPdf);

            // 3. Suggestions Panel (Dock Bottom above Attachment Toolbar)
            _suggestionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.FromArgb(17, 24, 39),
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

            // Add in reverse docking order so top/bottom dock stack properly:
            Controls.Add(_messagesContainer);
            Controls.Add(_suggestionsPanel);
            Controls.Add(_attachmentToolbar);
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
                    ChatStreamingState.Thinking => "Đang suy nghĩ...",
                    ChatStreamingState.Streaming => "Đang phản hồi...",
                    ChatStreamingState.Error => "Lỗi xử lý",
                    _ => "Sẵn sàng"
                };
            }
        }

        [Category("ZeroUI")]
        [Description("Enables or disables the attachment toolbar (Images & PDFs).")]
        [DefaultValue(true)]
        public bool EnableAttachments
        {
            get => _enableAttachments;
            set
            {
                _enableAttachments = value;
                _attachmentToolbar.Visible = value;
            }
        }

        #endregion

        #region Events

        public event EventHandler<string>? SendMessageRequested;
        public event EventHandler? StopGenerationRequested;
        public event EventHandler? ClearChatRequested;
        public event EventHandler<(string FilePath, string FileType)>? FileAttached;
        public event EventHandler<string>? UndoRequested;

        #endregion

        #region Attachments & Actions

        private void TriggerImageUpload()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Chọn ảnh chụp đơn hàng / báo giá / PO",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                AppendUserMessage($"📷 [Tải hình ảnh]: {System.IO.Path.GetFileName(ofd.FileName)}");
                FileAttached?.Invoke(this, (ofd.FileName, "image"));
            }
        }

        private void TriggerPdfUpload()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Chọn tệp PDF đơn hàng / PO",
                Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                AppendUserMessage($"📄 [Tải tệp PDF]: {System.IO.Path.GetFileName(ofd.FileName)}");
                FileAttached?.Invoke(this, (ofd.FileName, "pdf"));
            }
        }

        public ChatMessage AppendAssistantActionMessage(string content, bool canUndo = true)
        {
            HideThinkingIndicator();
            var msg = ChatMessage.Assistant(content, _modelName);
            msg.CanUndo = canUndo;
            _messages.Add(msg);
            return msg;
        }

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

        #region Message Bubbles & Animation

        private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_suppressRebuild) return;

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count == 1)
            {
                var newMsg = (ChatMessage)e.NewItems[0]!;
                AppendSingleMessageBubble(newMsg, animate: true);
            }
            else
            {
                RebuildAllMessages();
            }
        }

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
                    Text = "Chưa có lịch sử hội thoại. Đặt câu hỏi hoặc tải chứng từ để bắt đầu.",
                    Font = new Font("Segoe UI", 9.5f),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    AutoSize = true,
                    Location = new Point((availableWidth - 360) / 2, 40),
                    Tag = "emptyState"
                };
                _messagesContainer.Controls.Add(emptyLabel);
                _messagesContainer.ResumeLayout();
                return;
            }

            foreach (var msg in _messages)
            {
                var bubble = CreateBubbleControl(msg, availableWidth, out _, out _);
                bubble.Location = new Point(msg.Role == ChatRole.User ? availableWidth - bubble.Width + 14 : 14, yOffset);
                _messagesContainer.Controls.Add(bubble);
                yOffset += bubble.Height + 12;
            }

            _messagesContainer.ResumeLayout();
            ScrollToBottom();
        }

        private void AppendSingleMessageBubble(ChatMessage msg, bool animate = false)
        {
            HideEmptyState();
            int availableWidth = Math.Max(200, _messagesContainer.ClientSize.Width - 40);
            int yOffset = 10;

            foreach (Control c in _messagesContainer.Controls)
            {
                if (c != _thinkingBubble && c.Bottom + 12 > yOffset)
                {
                    yOffset = c.Bottom + 12;
                }
            }

            var bubble = CreateBubbleControl(msg, availableWidth, out _, out _);
            int targetLeft = msg.Role == ChatRole.User ? availableWidth - bubble.Width + 14 : 14;

            if (animate && msg.Role == ChatRole.User)
            {
                AddUserBubbleAnimated(bubble, availableWidth, targetLeft, yOffset);
            }
            else
            {
                bubble.Location = new Point(targetLeft, yOffset);
                _messagesContainer.Controls.Add(bubble);
            }

            if (_thinkingBubble != null)
            {
                _thinkingBubble.Location = new Point(14, bubble.Bottom + 12);
                _thinkingBubble.BringToFront();
            }

            ScrollToBottom();
        }

        private void AddUserBubbleAnimated(Control bubble, int availableWidth, int targetLeft, int yOffset)
        {
            bubble.Location = new Point(availableWidth + 30, yOffset);
            _messagesContainer.Controls.Add(bubble);

            var timer = new System.Windows.Forms.Timer { Interval = 16 };
            int step = 0;
            int startX = availableWidth + 30;

            timer.Tick += (s, e) =>
            {
                step++;
                float t = Math.Min(1.0f, step / 4.0f);
                float ease = 1f - (1f - t) * (1f - t); // EaseOutQuad
                bubble.Left = (int)(startX + (targetLeft - startX) * ease);

                if (step >= 4)
                {
                    bubble.Left = targetLeft;
                    timer.Stop();
                    timer.Dispose();
                }
            };
            timer.Start();
        }

        private Control CreateBubbleControl(ChatMessage msg, int containerWidth, out RichTextBox? outRtb, out Panel? outFooterPanel)
        {
            bool isUser = msg.Role == ChatRole.User;
            int maxBubbleWidth = Math.Min(650, (int)(containerWidth * 0.88));

            var bubble = new Panel
            {
                BackColor = isUser ? Color.FromArgb(30, 41, 59) : Color.FromArgb(17, 24, 39),
                Padding = new Padding(12, 8, 12, 8)
            };

            // 1. Header: Sender + Timestamp
            var senderLabel = new Label
            {
                Text = isUser ? (msg.SenderName ?? "Operator") : (msg.ModelName ?? _assistantName),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = isUser ? Color.FromArgb(165, 180, 252) : Color.FromArgb(52, 211, 153),
                AutoSize = true,
                Location = new Point(12, 7)
            };
            bubble.Controls.Add(senderLabel);

            var timeLabel = new Label
            {
                Text = msg.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Location = new Point(senderLabel.Right + 8, 8)
            };
            bubble.Controls.Add(timeLabel);

            // 2. Rich Text Content Box
            var rtb = new RichTextBox
            {
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.None,
                BackColor = bubble.BackColor,
                ForeColor = isUser ? Color.FromArgb(241, 245, 249) : Color.FromArgb(226, 232, 240),
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(12, 28),
                Cursor = Cursors.IBeam
            };
            outRtb = rtb;

            int contentWidth = maxBubbleWidth - 28;
            rtb.Width = contentWidth;

            Color textFore = isUser ? Color.FromArgb(241, 245, 249) : Color.FromArgb(226, 232, 240);
            Color highlight = isUser ? Color.White : Color.FromArgb(56, 189, 248);
            SetMarkdownText(rtb, msg.Content ?? "", textFore, highlight);

            int textHeight = CalculateRichTextBoxHeight(rtb, contentWidth);
            rtb.Height = textHeight;

            int totalBubbleWidth = Math.Max(contentWidth + 28, msg.CanUndo ? 280 : 180);
            if (isUser)
            {
                using var g = CreateGraphics();
                var measure = g.MeasureString(msg.Content ?? "", rtb.Font, contentWidth);
                totalBubbleWidth = Math.Max(130, Math.Min(maxBubbleWidth, (int)measure.Width + 32));
                rtb.Width = totalBubbleWidth - 24;
                textHeight = CalculateRichTextBoxHeight(rtb, rtb.Width);
                rtb.Height = textHeight;
            }
            bubble.Controls.Add(rtb);

            // 3. Footer Action Bar (Assistant Only)
            Panel? footerPanel = null;
            if (!isUser)
            {
                footerPanel = new Panel
                {
                    Size = new Size(totalBubbleWidth, 30),
                    Location = new Point(0, 28 + textHeight + 6),
                    BackColor = Color.Transparent
                };

                if (msg.CanUndo)
                {
                    var undoBtn = new Button
                    {
                        Text = "↺ Hoàn tác",
                        Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                        ForeColor = Color.FromArgb(251, 191, 36),
                        BackColor = Color.FromArgb(40, 245, 158, 11),
                        FlatStyle = FlatStyle.Flat,
                        Size = new Size(96, 24),
                        Location = new Point(12, 3),
                        Cursor = Cursors.Hand
                    };
                    undoBtn.FlatAppearance.BorderColor = Color.FromArgb(245, 158, 11);
                    undoBtn.FlatAppearance.BorderSize = 1;
                    undoBtn.MouseEnter += (s, e) => undoBtn.BackColor = Color.FromArgb(70, 245, 158, 11);
                    undoBtn.MouseLeave += (s, e) => undoBtn.BackColor = Color.FromArgb(40, 245, 158, 11);
                    undoBtn.Click += (s, e) => UndoRequested?.Invoke(this, msg.Id);
                    footerPanel.Controls.Add(undoBtn);
                }

                var copyBtn = new Button
                {
                    Text = "📋 Sao chép",
                    Font = new Font("Segoe UI", 8f),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    BackColor = Color.FromArgb(30, 41, 59),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(84, 24),
                    Location = new Point(totalBubbleWidth - 96, 3),
                    Cursor = Cursors.Hand
                };
                copyBtn.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
                copyBtn.FlatAppearance.BorderSize = 1;
                copyBtn.MouseEnter += (s, e) => { copyBtn.ForeColor = Color.White; copyBtn.BackColor = Color.FromArgb(51, 65, 85); };
                copyBtn.MouseLeave += (s, e) => { copyBtn.ForeColor = Color.FromArgb(148, 163, 184); copyBtn.BackColor = Color.FromArgb(30, 41, 59); };
                copyBtn.Click += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(msg.Content))
                    {
                        Clipboard.SetText(msg.Content);
                    }
                };
                footerPanel.Controls.Add(copyBtn);
                bubble.Controls.Add(footerPanel);
            }
            outFooterPanel = footerPanel;

            // 4. Overall Bubble Sizing
            int totalBubbleHeight = isUser ? (28 + textHeight + 10) : (28 + textHeight + 6 + 30 + 8);
            bubble.Size = new Size(totalBubbleWidth, totalBubbleHeight);

            // 5. Custom Border Paint
            bubble.Paint += (s, e) =>
            {
                var penColor = isUser ? Color.FromArgb(99, 102, 241) : Color.FromArgb(51, 65, 85);
                using var p = new Pen(penColor, 1f);
                e.Graphics.DrawRectangle(p, 0, 0, bubble.Width - 1, bubble.Height - 1);
            };

            return bubble;
        }

        #endregion

        #region Thinking Indicator & Streaming Animations

        public void ShowThinkingIndicator(string text = "Đang phân tích...")
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowThinkingIndicator(text)));
                return;
            }

            StreamingState = ChatStreamingState.Thinking;

            if (_thinkingBubble != null)
            {
                var lbl = _thinkingBubble.Controls.OfType<Label>().FirstOrDefault(l => (string?)l.Tag == "statusText");
                if (lbl != null) lbl.Text = text;
                ScrollToBottom();
                return;
            }

            int yOffset = 10;
            foreach (Control c in _messagesContainer.Controls)
            {
                if (c.Bottom + 12 > yOffset) yOffset = c.Bottom + 12;
            }

            var pnl = new Panel
            {
                BackColor = Color.FromArgb(17, 24, 39),
                Size = new Size(320, 58),
                Location = new Point(14, yOffset),
                Padding = new Padding(10, 6, 10, 6)
            };

            pnl.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(51, 65, 85), 1f);
                e.Graphics.DrawRectangle(p, 0, 0, pnl.Width - 1, pnl.Height - 1);
            };

            var header = new Label
            {
                Text = _modelName ?? _assistantName,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 211, 153),
                AutoSize = true,
                Location = new Point(10, 6)
            };
            pnl.Controls.Add(header);

            var dots = new PulsingDotsControl
            {
                Location = new Point(10, 26),
                Size = new Size(60, 24)
            };
            pnl.Controls.Add(dots);

            var lblText = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Location = new Point(72, 28),
                Tag = "statusText"
            };
            pnl.Controls.Add(lblText);

            _thinkingBubble = pnl;
            _messagesContainer.Controls.Add(pnl);
            ScrollToBottom();
        }

        public void HideThinkingIndicator()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(HideThinkingIndicator));
                return;
            }

            if (_thinkingBubble != null)
            {
                _messagesContainer.Controls.Remove(_thinkingBubble);
                _thinkingBubble.Dispose();
                _thinkingBubble = null;
            }
        }

        public async Task<ChatMessage> AppendAssistantActionMessageAnimatedAsync(
            string fullContent,
            bool canUndo = true,
            int delayMs = 16)
        {
            HideThinkingIndicator();

            var msg = ChatMessage.Assistant(fullContent, _modelName);
            msg.CanUndo = canUndo;

            _suppressRebuild = true;
            _messages.Add(msg);
            _suppressRebuild = false;

            int availableWidth = Math.Max(200, _messagesContainer.ClientSize.Width - 40);
            int yOffset = 10;
            foreach (Control c in _messagesContainer.Controls)
            {
                if (c.Bottom + 12 > yOffset) yOffset = c.Bottom + 12;
            }

            var tempMsg = ChatMessage.Assistant("", _modelName);
            tempMsg.CanUndo = canUndo;
            var bubble = CreateBubbleControl(tempMsg, availableWidth, out var rtb, out var footerPanel);
            bubble.Location = new Point(14, yOffset);

            if (footerPanel != null)
            {
                footerPanel.Visible = false;
            }

            _messagesContainer.Controls.Add(bubble);
            ScrollToBottom();

            if (rtb == null)
            {
                return msg;
            }

            _isGenerating = true;
            StreamingState = ChatStreamingState.Streaming;
            _sendButton.Visible = false;
            _stopButton.Visible = true;
            _cancelStreaming = false;

            // Tokenize by word groups
            var tokens = Regex.Matches(fullContent, @"(\S+\s*|\s+)").Cast<Match>().Select(m => m.Value).ToList();
            var sb = new System.Text.StringBuilder();

            Color textFore = Color.FromArgb(226, 232, 240);
            Color highlight = Color.FromArgb(56, 189, 248);

            for (int i = 0; i < tokens.Count; i += 2)
            {
                if (_cancelStreaming)
                {
                    sb.Clear();
                    sb.Append(fullContent);
                    SetMarkdownText(rtb, sb.ToString(), textFore, highlight);
                    break;
                }

                sb.Append(tokens[i]);
                if (i + 1 < tokens.Count) sb.Append(tokens[i + 1]);

                SetMarkdownText(rtb, sb.ToString(), textFore, highlight);
                int h = CalculateRichTextBoxHeight(rtb, rtb.Width);
                rtb.Height = h;
                bubble.Height = h + (canUndo ? 76 : 64);
                ScrollToBottom();
                await Task.Delay(delayMs);
            }

            // Final render pass
            SetMarkdownText(rtb, fullContent, textFore, highlight);
            int finalH = CalculateRichTextBoxHeight(rtb, rtb.Width);
            rtb.Height = finalH;
            bubble.Height = finalH + (canUndo ? 76 : 64);

            if (footerPanel != null)
            {
                footerPanel.Location = new Point(0, 28 + finalH + 6);
                footerPanel.Visible = true;
            }

            _isGenerating = false;
            StreamingState = ChatStreamingState.Completed;
            _stopButton.Visible = false;
            _sendButton.Visible = true;
            ScrollToBottom();

            return msg;
        }

        #endregion

        #region Markdown & Sizing Helpers

        private static void SetMarkdownText(RichTextBox rtb, string text, Color defaultColor, Color highlightColor)
        {
            rtb.Clear();
            if (string.IsNullOrEmpty(text)) return;

            string normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
            using var baseFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            using var boldFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            using var italicFont = new Font("Segoe UI", 9.5f, FontStyle.Italic);

            rtb.SuspendLayout();
            int i = 0;
            while (i < normalized.Length)
            {
                if (normalized[i] == '\n')
                {
                    rtb.SelectionFont = baseFont;
                    rtb.SelectionColor = defaultColor;
                    rtb.AppendText(Environment.NewLine);
                    i++;
                }
                else if (normalized.Length - i >= 4 && normalized.Substring(i, 2) == "**")
                {
                    int end = normalized.IndexOf("**", i + 2, StringComparison.Ordinal);
                    if (end > i + 2)
                    {
                        string boldText = normalized.Substring(i + 2, end - (i + 2));
                        rtb.SelectionFont = boldFont;
                        rtb.SelectionColor = highlightColor;
                        rtb.AppendText(boldText);
                        i = end + 2;
                        continue;
                    }
                    rtb.SelectionFont = baseFont;
                    rtb.SelectionColor = defaultColor;
                    rtb.AppendText("*");
                    i++;
                }
                else if (normalized[i] == '*' && normalized.Length - i >= 3)
                {
                    int end = normalized.IndexOf('*', i + 1);
                    if (end > i + 1 && (end + 1 >= normalized.Length || normalized[end + 1] != '*'))
                    {
                        string italicText = normalized.Substring(i + 1, end - (i + 1));
                        rtb.SelectionFont = italicFont;
                        rtb.SelectionColor = Color.FromArgb(203, 213, 225);
                        rtb.AppendText(italicText);
                        i = end + 1;
                        continue;
                    }
                    rtb.SelectionFont = baseFont;
                    rtb.SelectionColor = defaultColor;
                    rtb.AppendText("*");
                    i++;
                }
                else if (normalized[i] == '[')
                {
                    int end = normalized.IndexOf(']', i + 1);
                    if (end > i + 1 && end - i < 40 && !normalized.Substring(i + 1, end - (i + 1)).Contains("\n"))
                    {
                        string tagText = normalized.Substring(i, end - i + 1);
                        rtb.SelectionFont = boldFont;
                        rtb.SelectionColor = Color.FromArgb(56, 189, 248);
                        rtb.AppendText(tagText);
                        i = end + 1;
                        continue;
                    }
                    rtb.SelectionFont = baseFont;
                    rtb.SelectionColor = defaultColor;
                    rtb.AppendText("[");
                    i++;
                }
                else
                {
                    int nextSpecial = normalized.IndexOfAny(new[] { '\n', '*', '[' }, i);
                    if (nextSpecial == -1)
                    {
                        string rest = normalized.Substring(i);
                        rtb.SelectionFont = baseFont;
                        rtb.SelectionColor = defaultColor;
                        rtb.AppendText(rest);
                        break;
                    }
                    else
                    {
                        string span = normalized.Substring(i, nextSpecial - i);
                        rtb.SelectionFont = baseFont;
                        rtb.SelectionColor = defaultColor;
                        rtb.AppendText(span);
                        i = nextSpecial;
                    }
                }
            }
            rtb.ResumeLayout();
        }

        private static int CalculateRichTextBoxHeight(RichTextBox rtb, int width)
        {
            rtb.Width = width;
            if (rtb.TextLength == 0) return 24;

            Point p = rtb.GetPositionFromCharIndex(rtb.TextLength - 1);
            int lineH = TextRenderer.MeasureText("A", rtb.Font).Height;
            return Math.Max(26, p.Y + lineH + 12);
        }

        private void HideEmptyState()
        {
            var emptyLabel = _messagesContainer.Controls.OfType<Label>().FirstOrDefault(l => (string?)l.Tag == "emptyState");
            if (emptyLabel != null)
            {
                _messagesContainer.Controls.Remove(emptyLabel);
                emptyLabel.Dispose();
            }
        }

        #endregion

        #region Suggestions

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
                chip.MouseEnter += (s, e) =>
                {
                    chip.BackColor = Color.FromArgb(49, 46, 129);
                    chip.ForeColor = Color.White;
                    chip.FlatAppearance.BorderColor = Color.FromArgb(99, 102, 241);
                };
                chip.MouseLeave += (s, e) =>
                {
                    chip.BackColor = Color.FromArgb(30, 41, 59);
                    chip.ForeColor = Color.FromArgb(226, 232, 240);
                    chip.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
                };
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
            HideThinkingIndicator();
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
            HideThinkingIndicator();
            _messages.Clear();
            RebuildAllMessages();
        }

        public void ScrollToBottom()
        {
            if (_messagesContainer.Controls.Count > 0)
            {
                _messagesContainer.ScrollControlIntoView(_messagesContainer.Controls[_messagesContainer.Controls.Count - 1]);
            }
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

    #region Pulsing Dots Animation Control

    internal sealed class PulsingDotsControl : Control
    {
        private readonly System.Windows.Forms.Timer _timer;
        private int _tick;

        public PulsingDotsControl()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor, true);

            BackColor = Color.Transparent;
            Size = new Size(64, 24);

            _timer = new System.Windows.Forms.Timer { Interval = 40 };
            _timer.Tick += (s, e) =>
            {
                _tick++;
                Invalidate();
            };
            _timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            for (int i = 0; i < 3; i++)
            {
                float phase = _tick * 0.18f + i * 0.9f;
                float yOffset = (float)(Math.Sin(phase) * 3.5);
                int alpha = (int)(140 + 115 * Math.Sin(phase));
                alpha = Math.Max(40, Math.Min(255, alpha));

                using var brush = new SolidBrush(Color.FromArgb(alpha, 52, 211, 153));
                e.Graphics.FillEllipse(brush, 6 + i * 16, 8 + yOffset, 8, 8);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    #endregion

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
