using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ZeroUI.WinForms.Documents
{
    public partial class ZAiChatBox
    {
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

        #region Attachments & Uploads

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

        #endregion

        #region Suggestions

        private void OnPromptSuggestionsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => RebuildPromptSuggestions();

        public void RebuildPromptSuggestions()
        {
            _suggestionsPanel.SuspendLayout();
            _suggestionsPanel.Controls.Clear();

            foreach (var suggestion in _promptSuggestions)
            {
                var chip = new Button
                {
                    Text = suggestion,
                    Font = new Font("Segoe UI", 8.25f),
                    ForeColor = Color.FromArgb(226, 232, 240),
                    BackColor = Color.FromArgb(28, 38, 56),
                    FlatStyle = FlatStyle.Flat,
                    AutoSize = true,
                    Height = 26,
                    Padding = new Padding(8, 2, 8, 2),
                    Margin = new Padding(0, 0, 6, 6),
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
                    chip.BackColor = Color.FromArgb(28, 38, 56);
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
