using System;
using System.Linq;
using Xunit;
using ZeroUI.Core.AiMl;
using WpfChatBox = ZeroUI.Wpf.Documents.ZAiChatBox;
using WinFormsChatBox = ZeroUI.WinForms.Documents.ZAiChatBox;

namespace ZeroUI.Desktop.Tests
{
    public class AiChatControlsTests
    {
        #region WPF ZAiChatBox Tests

        [Fact]
        public void Wpf_ZAiChatBox_InitialState_And_MessageManagement_Works()
        {
            StaTestRunner.Run(() =>
            {
                var chat = new WpfChatBox();

                Assert.Equal("ZeroCopilot", chat.AssistantName);
                Assert.Equal("ZeroInference Edge", chat.ModelName);
                Assert.False(chat.IsGenerating);
                Assert.Equal(ChatStreamingState.Idle, chat.StreamingState);
                Assert.Empty(chat.Messages);

                // Add prompt suggestions
                chat.PromptSuggestions.Add("Explain OEE breakdown");
                chat.PromptSuggestions.Add("Check vibration alarms");
                Assert.Equal(2, chat.PromptSuggestions.Count);

                // User message
                chat.AppendUserMessage("What is the current line efficiency?");
                Assert.Single(chat.Messages);
                Assert.Equal(ChatRole.User, chat.Messages[0].Role);
                Assert.Equal("What is the current line efficiency?", chat.Messages[0].Content);

                // Assistant message
                var botMsg = chat.AppendAssistantMessage("Current line OEE is 87.4%.");
                Assert.Equal(2, chat.Messages.Count);
                Assert.Equal(ChatRole.Assistant, botMsg.Role);
                Assert.Equal("Current line OEE is 87.4%.", botMsg.Content);

                // Clear
                chat.ClearMessages();
                Assert.Empty(chat.Messages);
            });
        }

        [Fact]
        public void Wpf_ZAiChatBox_StreamingTokens_AppendsProperly()
        {
            StaTestRunner.Run(() =>
            {
                var chat = new WpfChatBox();
                var msg = chat.AppendAssistantMessage();

                chat.IsGenerating = true;
                chat.StreamingState = ChatStreamingState.Streaming;

                chat.StreamToken(msg.Id, "Zero");
                chat.StreamToken(msg.Id, "Platform");
                chat.StreamToken(msg.Id, " AI");

                Assert.Equal("ZeroPlatform AI", msg.Content);
                Assert.True(msg.IsStreaming);

                chat.CompleteStreaming(msg.Id);
                chat.IsGenerating = false;
                chat.StreamingState = ChatStreamingState.Completed;

                Assert.False(msg.IsStreaming);
                Assert.Equal("ZeroPlatform AI", msg.Content);
            });
        }

        #endregion

        #region WinForms ZAiChatBox Tests

        [Fact]
        public void WinForms_ZAiChatBox_InitialState_And_MessageManagement_Works()
        {
            using var chat = new WinFormsChatBox();

            Assert.Equal("ZeroCopilot", chat.AssistantName);
            Assert.Equal("ZeroInference Edge", chat.ModelName);
            Assert.False(chat.IsGenerating);
            Assert.Equal(ChatStreamingState.Idle, chat.StreamingState);
            Assert.Empty(chat.Messages);

            chat.PromptSuggestions.Add("Summarize batch 402");
            Assert.Single(chat.PromptSuggestions);

            chat.AppendUserMessage("Show telemetry summary.");
            Assert.Single(chat.Messages);
            Assert.Equal(ChatRole.User, chat.Messages[0].Role);

            var botMsg = chat.AppendAssistantMessage("Telemetry normal.");
            Assert.Equal(2, chat.Messages.Count);
            Assert.Equal(ChatRole.Assistant, botMsg.Role);

            chat.ClearMessages();
            Assert.Empty(chat.Messages);
        }

        [Fact]
        public void WinForms_ZAiChatBox_StreamingTokens_AppendsProperly()
        {
            using var chat = new WinFormsChatBox();
            var msg = chat.AppendAssistantMessage();

            chat.IsGenerating = true;
            chat.StreamingState = ChatStreamingState.Streaming;

            chat.StreamToken(msg.Id, "Token 1 ");
            chat.StreamToken(msg.Id, "Token 2");

            Assert.Equal("Token 1 Token 2", msg.Content);
            Assert.True(msg.IsStreaming);

            chat.CompleteStreaming(msg.Id);
            Assert.False(msg.IsStreaming);
            Assert.Equal("Token 1 Token 2", msg.Content);
        }

        #endregion
    }
}
