using System.Collections.ObjectModel;
using Avalonia.Controls.Templates;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia;
using System;
using System.Linq;

namespace Editor
{
    internal class ChatListController : Grid
    {
        public event EventHandler<Chat> ChatSelected;
        public event EventHandler NewChatRequested;
        public event EventHandler<Chat> ChatDeleted;

        private StackPanel _mainPanel;
        private TextBlock _titleText;
        private ListBox _chatListBox;
        private Button _newChatButton;
        private TextBlock _emptyChatListMessage;

        private ObservableCollection<Chat> _chats = new ObservableCollection<Chat>();

        public ChatListController()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            _mainPanel = new StackPanel
            {
                Spacing = 10,
                Margin = new Thickness(10)
            };

            _titleText = new TextBlock
            {
                Text = "Chats",
                Classes = { "chatListTitle" }
            };

            _chatListBox = new ListBox
            {
                ItemsSource = _chats,
                MinHeight = 150,
                Classes = { "chatList" }
            };

            _chatListBox.ItemTemplate = new FuncDataTemplate<Chat>((chat, scope) =>
            {
                if (chat == null)
                {
                    return new TextBlock { Text = "Empty chat" };
                }

                var outerGrid = new Grid();
                outerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                outerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var infoGrid = new Grid();
                infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
                infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var chatTitle = new TextBlock
                {
                    Text = chat.Title,
                    Classes = { "chatTitle" }
                };

                var timestampText = new TextBlock
                {
                    Text = chat.LastActivity.ToString("HH:mm"),
                    Classes = { "chatTime" }
                };

                var deleteButton = new Button
                {
                    Content = "✕",
                    Classes = { "deleteChatButton" },
                    Tag = chat
                };

                deleteButton.Click += (s, e) =>
                {
                    if (s is Button button && button.Tag is Chat chatToDelete)
                    {
                        ChatDeleted?.Invoke(this, chatToDelete);
                        e.Handled = true;
                    }
                };

                Grid.SetColumn(chatTitle, 0);
                Grid.SetColumn(timestampText, 1);
                Grid.SetColumn(deleteButton, 2);

                infoGrid.Children.Add(chatTitle);
                infoGrid.Children.Add(timestampText);
                infoGrid.Children.Add(deleteButton);

                var lastMessage = chat.GetLastMessage();
                var lastMessageText = new TextBlock
                {
                    Text = lastMessage != null ? lastMessage.Content : string.Empty,
                    Classes = { "chatPreview" }
                };

                Grid.SetRow(infoGrid, 0);
                Grid.SetRow(lastMessageText, 1);

                outerGrid.Children.Add(infoGrid);
                outerGrid.Children.Add(lastMessageText);

                return outerGrid;
            });

            _chatListBox.SelectionChanged += (s, e) =>
            {
                if (_chatListBox.SelectedItem is Chat selectedChat)
                {
                    ChatSelected?.Invoke(this, selectedChat);
                }
            };

            _newChatButton = new Button
            {
                Content = "Start new chat",
                Classes = { "newChatButton" }
            };

            _newChatButton.Click += (s, e) =>
            {
                NewChatRequested?.Invoke(this, EventArgs.Empty);
            };

            _emptyChatListMessage = new TextBlock
            {
                Text = "There are no activve chat.\nPress to button below to start new chat.",
                Classes = { "emptyChatMessage" },
                IsVisible = false
            };

            _mainPanel.Children.Add(_titleText);
            _mainPanel.Children.Add(_chatListBox);
            _mainPanel.Children.Add(_emptyChatListMessage);
            _mainPanel.Children.Add(_newChatButton);

            this.Children.Add(_mainPanel);
        }

        public void UpdateChatList(List<Chat> chats)
        {
            _chats.Clear();
            if (chats != null)
            {
                foreach (var chat in chats.Where(c => c != null).OrderByDescending(c => c.LastActivity))
                {
                    _chats.Add(chat);
                }
            }

            _emptyChatListMessage.IsVisible = _chats.Count == 0;
            _chatListBox.IsVisible = _chats.Count > 0;
        }
    }
}
