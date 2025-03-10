using Avalonia.Controls;
using AtomEngine;
using System;

namespace Editor
{
    internal class ChatController : Grid, IWindowed
    {
        public Action<object> OnClose { get; set; }

        private ChatListController _chatListController;
        private ChatSessionController _chatSessionController;
        private ChatManager _chatManager;
        private Chat _currentChat;
        private bool _isOpen = false;

        public ChatController()
        {
            _chatManager = new ChatManager();
            InitializeUI();
            AttachEventHandlers();
        }

        public void AgentResponse(ChatMessage message)
        {
            if (message.ChatId == _currentChat.Id)
            {
                _chatSessionController.AgentResponse(message);
            }
            else
            {
                _chatManager.AddMessage(message);
            }

        }

        private void InitializeUI()
        {
            _chatListController = new ChatListController();
            _chatSessionController = new ChatSessionController();

            _chatSessionController.IsVisible = false;

            this.Children.Add(_chatListController);
            this.Children.Add(_chatSessionController);
        }

        private void AttachEventHandlers()
        {
            _chatListController.ChatSelected += ChatListController_ChatSelected;
            _chatListController.NewChatRequested += ChatListController_NewChatRequested;
            _chatListController.ChatDeleted += ChatListController_ChatDeleted;

            _chatSessionController.BackRequested += ChatSessionController_BackRequested;
            _chatSessionController.MessageSent += ChatSessionController_MessageSent;
            _chatSessionController.FileAttached += ChatSessionController_FileAttached;
            _chatSessionController.FileRemoved += ChatSessionController_FileRemoved;
        }

        private void ChatListController_ChatSelected(object? sender, Chat chat)
        {
            if (chat == null)
            {
                DebLogger.Error("Выбран пустой чат");
                return;
            }

            _currentChat = chat;
            ShowChatSession(chat);
        }

        private void ChatListController_NewChatRequested(object? sender, EventArgs e)
        {
            _currentChat = _chatManager.CreateNewChat();
            ShowChatSession(_currentChat);
        }

        private void ChatListController_ChatDeleted(object? sender, Chat chat)
        {
            if (chat == null)
            {
                DebLogger.Error("Попытка удалить пустой чат");
                return;
            }

            if (_currentChat != null && _currentChat.Id == chat.Id)
            {
                _currentChat = null;
            }

            _chatManager.DeleteChat(chat.Id);
            ShowChatList();

            DebLogger.Debug($"Удален чат: {chat.Title}");
        }

        private void ChatSessionController_BackRequested(object? sender, EventArgs e)
        {
            ShowChatList();
        }

        private void ChatSessionController_MessageSent(object? sender, ChatMessage message)
        {
            DebLogger.Debug($"Отправлено сообщение: {message}");
            _ = _chatManager.SaveChatsAsync();

            if (!string.IsNullOrEmpty(message.Content))
            {
                var timer = new System.Threading.Timer(_ =>
                {
                    var mes = new ChatMessage()
                    {
                        Content = $"Получен ваш запрос: \"{message.Content}\"\nЭто автоматический ответ.",
                        ChatId = message.ChatId,
                        Speaker = ChatSpeaker.Agent,
                    };

                    AgentResponse(mes);
                    DebLogger.Debug($"Ответ: {mes}");

                }, null, 1000, System.Threading.Timeout.Infinite);
            }
        }

        private void ChatSessionController_FileAttached(object? sender, string filePath)
        {
            DebLogger.Debug($"Прикреплен файл: {filePath}");
        }

        private void ChatSessionController_FileRemoved(object? sender, string filePath)
        {
            DebLogger.Debug($"Удален файл: {filePath}");
        }

        private void ShowChatList()
        {
            _chatListController.UpdateChatList(_chatManager.Chats);
            _chatListController.IsVisible = true;
            _chatSessionController.IsVisible = false;
        }

        private void ShowChatSession(Chat chat)
        {
            if (chat == null)
            {
                DebLogger.Error("Попытка показать пустой чат");
                return;
            }

            _chatSessionController.SetChat(chat);
            _chatListController.IsVisible = false;
            _chatSessionController.IsVisible = true;
        }

        public async void Open()
        {
            _isOpen = true;
            await _chatManager.LoadChatsAsync();
            _chatListController.UpdateChatList(_chatManager.Chats);

            ShowChatList();
        }

        public void Close()
        {
            _isOpen = false;

            _ = _chatManager.SaveChatsAsync();

            OnClose?.Invoke(this);
        }

        public void Dispose()
        {
            _chatListController.ChatSelected -= ChatListController_ChatSelected;
            _chatListController.NewChatRequested -= ChatListController_NewChatRequested;
            _chatListController.ChatDeleted -= ChatListController_ChatDeleted;

            _chatSessionController.BackRequested -= ChatSessionController_BackRequested;
            _chatSessionController.MessageSent -= ChatSessionController_MessageSent;
            _chatSessionController.FileAttached -= ChatSessionController_FileAttached;
            _chatSessionController.FileRemoved -= ChatSessionController_FileRemoved;
        }

        public void Redraw()
        {
            if (_isOpen)
            {
                if (_chatSessionController.IsVisible && _currentChat != null)
                {
                    _chatSessionController.SetChat(_currentChat);
                }
                else
                {
                    ShowChatList();
                }
            }
        }
    }
}
