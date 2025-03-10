using System.Collections.Generic;
using System;

namespace Editor
{
    internal class Chat
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "Новый чат";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastActivity { get; set; } = DateTime.Now;
        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public ChatMessage? GetLastMessage() => Messages.Count > 0 ? Messages[Messages.Count - 1] : null;
        public void AddMessage(ChatMessage message)
        {
            Messages.Add(message);
            LastActivity = DateTime.Now;
        }
    }
}
