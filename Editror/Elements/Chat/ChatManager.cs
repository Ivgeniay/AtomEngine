using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using AtomEngine;

namespace Editor
{
    internal class ChatManager
    {
        private readonly string _chatStoragePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Editor", "chats.json");

        public ChatManager() { 
            var directoryManager = ServiceHub.Get<DirectoryExplorer>();
            var cachePath = directoryManager.GetPath(DirectoryType.Cache);
            _chatStoragePath = Path.Combine(cachePath, "chats.json");
        }

        public List<Chat> Chats { get; private set; } = new List<Chat>();

        public async Task LoadChatsAsync()
        {
            try
            {
                if (!File.Exists(_chatStoragePath))
                {
                    Chats = new List<Chat>();
                    return;
                }

                string json = await File.ReadAllTextAsync(_chatStoragePath);
                Chats = JsonConvert.DeserializeObject<List<Chat>>(json) ?? new List<Chat>();
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при загрузке чатов: {ex.Message}");
                Chats = new List<Chat>();
            }
        }

        public async Task SaveChatsAsync()
        {
            try
            {
                var directory = Path.GetDirectoryName(_chatStoragePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(Chats, GlobalDeserializationSettings.Settings);
                await File.WriteAllTextAsync(_chatStoragePath, json);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при сохранении чатов: {ex.Message}");
            }
        }

        public Chat CreateNewChat()
        {
            var chat = new Chat();
            Chats.Add(chat);
            _ = SaveChatsAsync();
            return chat;
        }

        public void DeleteChat(Guid chatId)
        {
            var chatToRemove = Chats.FirstOrDefault(c => c.Id == chatId);
            if (chatToRemove != null)
            {
                Chats.Remove(chatToRemove);
                _ = SaveChatsAsync();
            }
        }

        public void AddMessage(ChatMessage message)
        {
            var chat = Chats.FirstOrDefault(c => c.Id == message.ChatId);
            if (chat != null)
            {
                chat.AddMessage(message);
                _ = SaveChatsAsync();
            }
        }
    }
}
