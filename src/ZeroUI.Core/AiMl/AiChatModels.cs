using System;
using System.Collections.Generic;

namespace ZeroUI.Core.AiMl
{
    /// <summary>
    /// Represents the role of the message author in an AI chat conversation.
    /// </summary>
    public enum ChatRole
    {
        User,
        Assistant,
        System,
        Tool
    }

    /// <summary>
    /// Represents the current runtime streaming or generation state of the AI assistant.
    /// </summary>
    public enum ChatStreamingState
    {
        Idle,
        Thinking,
        Streaming,
        Completed,
        Error
    }

    /// <summary>
    /// Represents a single message in an enterprise AI conversation session.
    /// </summary>
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public ChatRole Role { get; set; } = ChatRole.User;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? SenderName { get; set; }
        public string? ModelName { get; set; }
        public bool IsStreaming { get; set; }
        public string? ErrorMessage { get; set; }
        public object? Tag { get; set; }

        public ChatMessage() { }

        public ChatMessage(ChatRole role, string content, string? senderName = null)
        {
            Role = role;
            Content = content ?? string.Empty;
            SenderName = senderName;
            Timestamp = DateTime.UtcNow;
        }

        public static ChatMessage User(string content, string? senderName = "Operator") =>
            new(ChatRole.User, content, senderName);

        public static ChatMessage Assistant(string content, string? modelName = "ZeroCopilot") =>
            new(ChatRole.Assistant, content, modelName) { ModelName = modelName };

        public static ChatMessage System(string content) =>
            new(ChatRole.System, content, "System");
    }

    /// <summary>
    /// Represents an actionable quick prompt suggestion chip for operator assistance.
    /// </summary>
    public class ChatPromptAction
    {
        public string Title { get; set; } = string.Empty;
        public string PromptText { get; set; } = string.Empty;
        public string? IconKey { get; set; }

        public ChatPromptAction() { }

        public ChatPromptAction(string title, string promptText, string? iconKey = null)
        {
            Title = title;
            PromptText = promptText;
            IconKey = iconKey;
        }
    }
}
