using System;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ZeroUI.Core.AiMl;

namespace ZeroUI.WinForms.Documents
{
    public partial class ZAiChatBox
    {
        #region Message Bubbles & Layout

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

        public void RebuildAllMessages()
        {
            _bubbleCache.Clear();
            _messagesContainer.SuspendLayout();
            _messagesContainer.Controls.Clear();

            int yOffset = 10;
            int containerW = Math.Max(200, _messagesContainer.ClientSize.Width - 24);

            if (_messages.Count == 0)
            {
                var emptyLabel = new Label
                {
                    Text = "Chưa có lịch sử hội thoại. Đặt câu hỏi hoặc tải chứng từ để bắt đầu.",
                    Font = new Font("Segoe UI", 9.5f),
                    ForeColor = Color.FromArgb(100, 116, 139),
                    AutoSize = true,
                    Location = new Point(Math.Max(10, (containerW - 360) / 2), 40),
                    Tag = "emptyState"
                };
                _messagesContainer.Controls.Add(emptyLabel);
                _messagesContainer.ResumeLayout();
                return;
            }

            foreach (var msg in _messages)
            {
                var bubble = CreateBubbleControl(msg, containerW, out var rtb, out var footer);
                if (rtb != null)
                {
                    _bubbleCache[msg.Id] = new BubbleViewHolder
                    {
                        BubblePanel = bubble,
                        RichTextBox = rtb,
                        FooterPanel = footer,
                        Message = msg
                    };
                }

                int targetLeft = msg.Role == ChatRole.User
                    ? Math.Max(8, _messagesContainer.ClientSize.Width - bubble.Width - 14)
                    : 10;
                bubble.Location = new Point(targetLeft, yOffset);
                _messagesContainer.Controls.Add(bubble);
                yOffset += bubble.Height + 12;
            }

            _messagesContainer.ResumeLayout();
            ScrollToBottom();
        }

        private void AppendSingleMessageBubble(ChatMessage msg, bool animate = false)
        {
            HideEmptyState();
            int containerW = Math.Max(200, _messagesContainer.ClientSize.Width - 24);
            int yOffset = 10;

            foreach (Control c in _messagesContainer.Controls)
            {
                if (c != _thinkingBubble && c.Bottom + 12 > yOffset)
                {
                    yOffset = c.Bottom + 12;
                }
            }

            var bubble = CreateBubbleControl(msg, containerW, out var rtb, out var footer);
            if (rtb != null)
            {
                _bubbleCache[msg.Id] = new BubbleViewHolder
                {
                    BubblePanel = bubble,
                    RichTextBox = rtb,
                    FooterPanel = footer,
                    Message = msg
                };
            }

            int targetLeft = msg.Role == ChatRole.User
                ? Math.Max(8, _messagesContainer.ClientSize.Width - bubble.Width - 14)
                : 10;

            if (animate && msg.Role == ChatRole.User)
            {
                AddUserBubbleAnimated(bubble, targetLeft, yOffset);
            }
            else
            {
                bubble.Location = new Point(targetLeft, yOffset);
                _messagesContainer.Controls.Add(bubble);
            }

            if (_thinkingBubble != null)
            {
                _thinkingBubble.Location = new Point(10, bubble.Bottom + 12);
                _thinkingBubble.BringToFront();
            }

            ScrollToBottom();
        }

        private void AddUserBubbleAnimated(Control bubble, int targetLeft, int yOffset)
        {
            int maxSafeRight = _messagesContainer.ClientSize.Width - 12;
            int startX = Math.Min(maxSafeRight - bubble.Width, targetLeft + 20);

            if (startX <= targetLeft)
            {
                bubble.Location = new Point(targetLeft, yOffset);
                _messagesContainer.Controls.Add(bubble);
                return;
            }

            bubble.Location = new Point(startX, yOffset);
            _messagesContainer.Controls.Add(bubble);

            var timer = new System.Windows.Forms.Timer { Interval = 16 };
            int step = 0;

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

            int maxAllowedWidth = Math.Max(180, containerWidth - 24);
            int maxBubbleWidth = Math.Min(580, maxAllowedWidth);

            var bubble = new Panel
            {
                BackColor = isUser ? Color.FromArgb(30, 41, 59) : Color.FromArgb(17, 24, 39),
                Padding = new Padding(12, 8, 12, 8),
                Tag = msg.Id
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

            int contentWidth = Math.Max(120, maxBubbleWidth - 28);
            rtb.Width = contentWidth;

            Color textFore = isUser ? Color.FromArgb(241, 245, 249) : Color.FromArgb(226, 232, 240);
            Color highlight = isUser ? Color.White : Color.FromArgb(56, 189, 248);
            SetMarkdownText(rtb, msg.Content ?? "", textFore, highlight);

            int textHeight = CalculateRichTextBoxHeight(rtb, contentWidth);
            rtb.Height = textHeight;

            int minBubbleWidth = Math.Min(maxBubbleWidth, msg.CanUndo ? 210 : 150);
            int totalBubbleWidth = Math.Max(minBubbleWidth, Math.Min(maxBubbleWidth, contentWidth + 28));

            if (isUser)
            {
                using var g = CreateGraphics();
                var measure = g.MeasureString(msg.Content ?? "", rtb.Font, contentWidth);
                totalBubbleWidth = Math.Max(120, Math.Min(maxBubbleWidth, (int)measure.Width + 30));
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
                        Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                        ForeColor = Color.FromArgb(251, 191, 36),
                        BackColor = Color.FromArgb(40, 245, 158, 11),
                        FlatStyle = FlatStyle.Flat,
                        Size = new Size(90, 24),
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
                    Size = new Size(80, 24),
                    Location = new Point(Math.Max(105, totalBubbleWidth - 88), 3),
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

        public void ScrollToBottom()
        {
            if (_messagesContainer.Controls.Count > 0)
            {
                _messagesContainer.AutoScrollPosition = new Point(0, _messagesContainer.DisplayRectangle.Height);
            }
        }

        #endregion
    }
}
