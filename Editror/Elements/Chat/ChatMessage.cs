using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace Editor
{
    internal class ChatMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ChatId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public ChatSpeaker Speaker { get; set; }
        public List<string> Attachments { get; set; } = new List<string>();
        public MessageStatus Status { get; set; } = MessageStatus.Sent;

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}
