using System.Collections.ObjectModel;
using System.Collections.Generic;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using AtomEngine;
using System.IO;
using Avalonia;
using System;
using Tmds.DBus.Protocol;

namespace Editor
{
    internal class ChatSessionController : Grid
    {
        public event EventHandler BackRequested;
        public event EventHandler<ChatMessage> MessageSent;
        public event EventHandler<string> FileAttached;
        public event EventHandler<string> FileRemoved;

        private Chat _currentChat;
        private ObservableCollection<ChatMessage> _messages = new ObservableCollection<ChatMessage>();
        private List<string> _attachments = new List<string>();

        private Grid _mainGrid;
        private Button _backButton;
        private TextBlock _chatTitleText;
        private ScrollViewer _messagesScrollViewer;
        private StackPanel _messagesPanel;
        private TextBox _messageInputBox;
        private Button _attachFileButton;
        private Button _sendButton;
        private StackPanel _attachmentsPanel;

        public ChatSessionController()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            _mainGrid = new Grid();
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); 
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star }); 
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); 
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); 

            // Header
            var headerPanel = new StackPanel
            {
                Classes = { "chatHeader" }
            };

            _backButton = new Button
            {
                Content = "←",
                Classes = { "backButton" }
            };

            _backButton.Click += (s, e) => BackRequested?.Invoke(this, EventArgs.Empty);

            _chatTitleText = new TextBlock
            {
                Text = "Chat",
                Classes = { "chatSessionTitle" }
            };

            headerPanel.Children.Add(_backButton);
            headerPanel.Children.Add(_chatTitleText);

            _messagesPanel = new StackPanel
            {
                Classes = { "messagesPanel" }
            };

            _messagesScrollViewer = new ScrollViewer
            {
                Classes = { "messagesArea" },
                Content = _messagesPanel
            };

            _attachmentsPanel = new StackPanel
            {
                Classes = { "attachmentsPanel" },
                IsVisible = false
            };

            var inputPanel = new Grid
            {
                Margin = new Thickness(10, 5)
            };

            inputPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Attach button
            inputPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star }); // Text input
            inputPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Send button

            _attachFileButton = new Button
            {
                Content = "📎",
                Classes = { "attachButton" }
            };

            _attachFileButton.Click += AttachFileButton_Click;

            _messageInputBox = new TextBox
            {
                Watermark = "Start new message here...",
                Classes = { "messageInput" }
            };

            _messageInputBox.KeyDown += MessageInputBox_KeyDown;

            _sendButton = new Button
            {
                Content = "➤",
                Classes = { "sendButton" }
            };

            _sendButton.Click += SendButton_Click;

            Grid.SetColumn(_attachFileButton, 0);
            Grid.SetColumn(_messageInputBox, 1);
            Grid.SetColumn(_sendButton, 2);

            inputPanel.Children.Add(_attachFileButton);
            inputPanel.Children.Add(_messageInputBox);
            inputPanel.Children.Add(_sendButton);

            Grid.SetRow(headerPanel, 0);
            Grid.SetRow(_messagesScrollViewer, 1);
            Grid.SetRow(_attachmentsPanel, 2);
            Grid.SetRow(inputPanel, 3);

            _mainGrid.Children.Add(headerPanel);
            _mainGrid.Children.Add(_messagesScrollViewer);
            _mainGrid.Children.Add(_attachmentsPanel);
            _mainGrid.Children.Add(inputPanel);

            this.Children.Add(_mainGrid);
        }

        private void MessageInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (e.KeyModifiers & KeyModifiers.Shift) != KeyModifiers.Shift)
            {
                SendMessage();
                e.Handled = true;
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void AttachFileButton_Click(object sender, RoutedEventArgs e)
        {
            AttachFile();
        }

        private async void AttachFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Choose file to attach",
                AllowMultiple = false
            };

            var result = await dialog.ShowAsync(Window.GetTopLevel(this) as Window);

            if (result != null && result.Length > 0)
            {
                var filePath = result[0];
                _attachments.Add(filePath);
                FileAttached?.Invoke(this, filePath);

                UpdateAttachmentsPanel();
            }
        }

        private void SendMessage()
        {
            var messageText = _messageInputBox.Text?.Trim();

            if (string.IsNullOrEmpty(messageText) && _attachments.Count == 0)
                return;

            var message = new ChatMessage
            {
                Content = messageText ?? string.Empty,
                Speaker = ChatSpeaker.User,
                Attachments = new List<string>(_attachments),
                ChatId = _currentChat.Id,
            };

            AddMessageToChat(message);

            _messageInputBox.Text = string.Empty;
            _attachments.Clear();
            UpdateAttachmentsPanel();

            MessageSent?.Invoke(this, message);
        }


        public void AgentResponse(ChatMessage message)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                AddMessageToChat(message);
            });
        }

        private void UpdateAttachmentsPanel()
        {
            _attachmentsPanel.Children.Clear();

            if (_attachments.Count > 0)
            {
                _attachmentsPanel.IsVisible = true;

                foreach (var attachment in _attachments)
                {
                    var attachmentPanel = new StackPanel
                    {
                        Classes = { "attachmentItem" }
                    };

                    var fileName = Path.GetFileName(attachment);
                    var attachmentText = new TextBlock
                    {
                        Text = fileName,
                        Classes = { "attachmentName" }
                    };

                    var removeButton = new Button
                    {
                        Content = "×",
                        Classes = { "removeAttachmentButton" }
                    };

                    var filePath = attachment;
                    removeButton.Click += (s, e) =>
                    {
                        _attachments.Remove(filePath);
                        FileRemoved?.Invoke(this, filePath);
                        UpdateAttachmentsPanel();
                    };

                    attachmentPanel.Children.Add(attachmentText);
                    attachmentPanel.Children.Add(removeButton);

                    _attachmentsPanel.Children.Add(attachmentPanel);
                }
            }
            else
            {
                _attachmentsPanel.IsVisible = false;
            }
        }

        public void SetChat(Chat chat)
        {
            if (chat == null)
            {
                DebLogger.Error("Попытка установить пустой чат");
                return;
            }

            _currentChat = chat;
            _chatTitleText.Text = chat.Title;

            _messages.Clear();
            _messagesPanel.Children.Clear();

            foreach (var message in chat.Messages)
            {
                AddMessageControl(message);
            }

            ScrollToBottom();
        }

        private void AddMessageToChat(ChatMessage message)
        {
            if (_currentChat != null)
            {
                _currentChat.AddMessage(message);
                AddMessageControl(message);
                ScrollToBottom();
            }
        }

        private void AddMessageControl(ChatMessage message)
        {
            var messagePanel = new StackPanel
            {
                Spacing = 5,
                Margin = new Thickness(0, 5),
                HorizontalAlignment = message.Speaker == ChatSpeaker.User ?
                    HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 300
            };

            var messageBackground = new SolidColorBrush(
                message.Speaker == ChatSpeaker.User ?
                Color.Parse("#2B5278") : Color.Parse("#383838"));

            var messageBorder = new Border
            {
                Classes = { "chatMessage", message.Speaker == ChatSpeaker.User ? "chatMessageUser" : "chatMessageAgent" }
            };

            var messageStackPanel = new StackPanel
            {
                Spacing = 5
            };

            if (!string.IsNullOrEmpty(message.Content))
            {
                var messageText = new TextBlock
                {
                    Text = message.Content,
                    Classes = { "messageText" }
                };

                messageStackPanel.Children.Add(messageText);
            }

            if (message.Attachments != null && message.Attachments.Count > 0)
            {
                foreach (var attachment in message.Attachments)
                {
                    var fileName = Path.GetFileName(attachment);
                    var attachmentPanel = new StackPanel
                    {
                        Classes = { "messageAttachment" }
                    };

                    var attachmentIcon = new TextBlock
                    {
                        Text = "📎",
                        Classes = { "attachmentIcon" }
                    };

                    var attachmentText = new TextBlock
                    {
                        Text = fileName,
                        Classes = { "attachmentLink" }
                    };

                    attachmentPanel.Children.Add(attachmentIcon);
                    attachmentPanel.Children.Add(attachmentText);

                    attachmentPanel.PointerPressed += (s, e) =>
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = attachment,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            DebLogger.Error($"Ошибка при открытии файла: {ex.Message}");
                        }
                    };

                    messageStackPanel.Children.Add(attachmentPanel);
                }
            }

            var timestampText = new TextBlock
            {
                Text = message.Timestamp.ToString("HH:mm"),
                Classes = { "messageTime", message.Speaker == ChatSpeaker.User ? "messageTimeUser" : "messageTimeAgent" }
            };

            messageBorder.Child = messageStackPanel;
            messagePanel.Children.Add(messageBorder);
            messagePanel.Children.Add(timestampText);

            _messagesPanel.Children.Add(messagePanel);
        }

        private void ScrollToBottom()
        {
            _messagesScrollViewer.ScrollToEnd();
        }
    }
}
