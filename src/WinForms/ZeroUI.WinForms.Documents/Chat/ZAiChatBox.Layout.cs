using System;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ZeroUI.Core.AiMl;
using ZeroUI.WinForms.Theme;

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
            bool isDark = ZeroTheme.IsDark;

            int maxAllowedWidth = Math.Max(180, containerWidth - 24);
            int maxBubbleWidth = Math.Min(580, maxAllowedWidth);

            var bubbleBg = isUser
                ? (isDark ? Color.FromArgb(30, 41, 59) : Color.FromArgb(238, 242, 255))
                : (isDark ? Color.FromArgb(17, 24, 39) : Color.FromArgb(255, 255, 255));

            var bubble = new Panel
            {
                BackColor = bubbleBg,
                Padding = new Padding(12, 8, 12, 8),
                Tag = msg.Id
            };

            // 1. Header: Sender + Timestamp
            var senderColor = isUser
                ? (isDark ? Color.FromArgb(165, 180, 252) : Color.FromArgb(67, 56, 202))
                : (isDark ? Color.FromArgb(52, 211, 153) : Color.FromArgb(5, 150, 105));

            var timeColor = isDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(100, 116, 139);

            var senderLabel = new Label
            {
                Text = isUser ? (msg.SenderName ?? "Operator") : (string.IsNullOrWhiteSpace(msg.ModelName) ? _assistantName : msg.ModelName),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = senderColor,
                AutoSize = true,
                Location = new Point(12, 7)
            };
            bubble.Controls.Add(senderLabel);

            var timeLabel = new Label
            {
                Text = msg.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
                Font = new Font("Segoe UI", 8f),
                ForeColor = timeColor,
                AutoSize = true,
                Location = new Point(senderLabel.Right + 8, 8)
            };
            bubble.Controls.Add(timeLabel);

            // 2. Rich Text Content Box
            Color textFore = isUser
                ? (isDark ? Color.FromArgb(241, 245, 249) : Color.FromArgb(30, 27, 75))
                : (isDark ? Color.FromArgb(226, 232, 240) : Color.FromArgb(15, 23, 42));

            Color highlight = isUser
                ? (isDark ? Color.White : Color.FromArgb(67, 56, 202))
                : (isDark ? Color.FromArgb(56, 189, 248) : Color.FromArgb(2, 132, 199));

            var rtb = new RichTextBox
            {
                Multiline = true,
                WordWrap = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.None,
                BackColor = bubble.BackColor,
                ForeColor = textFore,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(12, 28),
                Cursor = Cursors.IBeam
            };
            outRtb = rtb;

            int contentWidth = Math.Max(120, maxBubbleWidth - 28);
            rtb.Width = contentWidth;

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

                var copyBtnBg = isDark ? Color.FromArgb(30, 41, 59) : Color.FromArgb(241, 245, 249);
                var copyBtnFore = isDark ? Color.FromArgb(148, 163, 184) : Color.FromArgb(71, 85, 105);
                var copyBtnBorder = isDark ? Color.FromArgb(71, 85, 105) : Color.FromArgb(203, 213, 225);

                var copyBtn = new Button
                {
                    Text = "📋 Sao chép",
                    Font = new Font("Segoe UI", 8f),
                    ForeColor = copyBtnFore,
                    BackColor = copyBtnBg,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(80, 24),
                    Location = new Point(Math.Max(105, totalBubbleWidth - 88), 3),
                    Cursor = Cursors.Hand
                };
                copyBtn.FlatAppearance.BorderColor = copyBtnBorder;
                copyBtn.FlatAppearance.BorderSize = 1;
                copyBtn.MouseEnter += (s, e) => { copyBtn.ForeColor = isDark ? Color.White : Color.FromArgb(15, 23, 42); copyBtn.BackColor = isDark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(226, 232, 240); };
                copyBtn.MouseLeave += (s, e) => { copyBtn.ForeColor = copyBtnFore; copyBtn.BackColor = copyBtnBg; };
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
                var penColor = isUser
                    ? (isDark ? Color.FromArgb(99, 102, 241) : Color.FromArgb(199, 210, 254))
                    : (isDark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(226, 232, 240));

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
            using var monoFont = new Font("Consolas", 9f, FontStyle.Regular);
            using var monoBoldFont = new Font("Consolas", 9f, FontStyle.Bold);

            rtb.SuspendLayout();
            int i = 0;
            while (i < normalized.Length)
            {
                // Check if current position is the start of a Markdown Table block
                if ((i == 0 || normalized[i - 1] == '\n') && normalized[i] == '|')
                {
                    if (TryRenderMarkdownTable(normalized, i, rtb, monoFont, monoBoldFont, defaultColor, highlightColor, out int nextIndex))
                    {
                        i = nextIndex;
                        continue;
                    }
                }

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

        private static bool TryRenderMarkdownTable(
            string text,
            int startIndex,
            RichTextBox rtb,
            Font monoFont,
            Font monoBoldFont,
            Color defaultColor,
            Color highlightColor,
            out int nextIndex)
        {
            nextIndex = startIndex;
            var lines = new System.Collections.Generic.List<string>();
            int cursor = startIndex;

            while (cursor < text.Length)
            {
                int newline = text.IndexOf('\n', cursor);
                string line = newline == -1 ? text.Substring(cursor) : text.Substring(cursor, newline - cursor);
                string trimmed = line.Trim();

                if (trimmed.StartsWith("|") && trimmed.EndsWith("|"))
                {
                    lines.Add(line);
                    cursor = newline == -1 ? text.Length : newline + 1;
                }
                else
                {
                    break;
                }
            }

            if (lines.Count < 2) return false;

            // Second line must be table separator e.g. |---|---|
            string sepTrimmed = lines[1].Trim();
            if (!sepTrimmed.Contains("-")) return false;

            // Parse cells helper
            static string[] ParseRowCells(string row)
            {
                var raw = row.Split('|');
                var cells = new System.Collections.Generic.List<string>();
                for (int i = 1; i < raw.Length - 1; i++)
                {
                    cells.Add(raw[i].Trim());
                }
                return cells.ToArray();
            }

            var headerCells = ParseRowCells(lines[0]);
            if (headerCells.Length == 0) return false;

            var bodyRows = new System.Collections.Generic.List<string[]>();
            for (int r = 2; r < lines.Count; r++)
            {
                bodyRows.Add(ParseRowCells(lines[r]));
            }

            // Calculate max width for each column
            int[] colWidths = new int[headerCells.Length];
            for (int c = 0; c < headerCells.Length; c++)
            {
                colWidths[c] = headerCells[c].Length;
                foreach (var row in bodyRows)
                {
                    if (c < row.Length && row[c].Length > colWidths[c])
                    {
                        colWidths[c] = row[c].Length;
                    }
                }
                colWidths[c] = Math.Max(colWidths[c], 3);
            }

            // Render Header
            rtb.SelectionFont = monoBoldFont;
            rtb.SelectionColor = highlightColor;
            for (int c = 0; c < headerCells.Length; c++)
            {
                rtb.AppendText(headerCells[c].PadRight(colWidths[c] + 2));
            }
            rtb.AppendText(Environment.NewLine);

            // Render Divider line
            int totalWidth = 0;
            for (int c = 0; c < colWidths.Length; c++) totalWidth += colWidths[c] + 2;
            rtb.SelectionFont = monoFont;
            rtb.SelectionColor = Color.FromArgb(148, 163, 184);
            rtb.AppendText(new string('─', Math.Min(totalWidth, 80)) + Environment.NewLine);

            // Render Data Rows
            foreach (var row in bodyRows)
            {
                for (int c = 0; c < headerCells.Length; c++)
                {
                    string cell = c < row.Length ? row[c] : string.Empty;
                    rtb.SelectionFont = monoFont;
                    rtb.SelectionColor = defaultColor;
                    rtb.AppendText(cell.PadRight(colWidths[c] + 2));
                }
                rtb.AppendText(Environment.NewLine);
            }

            nextIndex = cursor;
            return true;
        }

        private static int CalculateRichTextBoxHeight(RichTextBox rtb, int width)
        {
            if (rtb.TextLength == 0) return 24;
            rtb.Width = width;

            var pref = rtb.GetPreferredSize(new Size(width, 0));
            int height = pref.Height + 6;

            var flags = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl;
            var textMeasure = TextRenderer.MeasureText(rtb.Text, rtb.Font, new Size(width, int.MaxValue), flags);
            int safeH = Math.Max(height, textMeasure.Height + 12);

            return Math.Max(26, safeH);
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
