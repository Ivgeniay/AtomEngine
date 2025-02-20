using Avalonia.Controls.Primitives;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using System;

namespace Editor
{
    public class ConsoleController : Grid, ILogger
    {
        private ScrollViewer _scrollViewer;
        private StackPanel _logPanel;
        private TextBox _commandInput;
        private Button _clearButton;
        private ComboBox _filterComboBox;
        private const int MaxLogEntries = 1000;
        private List<LogEntry> _logEntries = new List<LogEntry>();

        public LogLevel LogLevel { get; set; } = LogLevel.All;
        global::LogLevel ILogger.LogLevel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private class LogEntry
        {
            public string Message { get; set; }
            public LogLevel Level { get; set; }
            public DateTime Timestamp { get; set; }

            public LogEntry(string message, LogLevel level)
            {
                Message = message;
                Level = level;
                Timestamp = DateTime.Now;
            }

            public IBrush GetColor()
            {
                return Level switch
                {
                    LogLevel.Debug => Brushes.Gray,
                    LogLevel.Info => Brushes.White,
                    LogLevel.Warn => Brushes.Gold,
                    LogLevel.Error => Brushes.Tomato,
                    LogLevel.Fatal => Brushes.Crimson,
                    _ => Brushes.White
                };
            }

            public string GetTimestampString()
            {
                return Timestamp.ToString("HH:mm:ss.fff");
            }

            public string GetLevelString()
            {
                return Level.ToString().ToUpper();
            }
        }

        public ConsoleController()
        {
            InitializeUI();
            Log("Console initialized", LogLevel.Info);
        }

        private void InitializeUI()
        {
            this.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            this.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            this.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Панель инструментов
            var toolbarPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Margin = new Thickness(0, 0, 0, 5)
            };

            _clearButton = new Button
            {
                Content = "Clear",
                Classes = { "toolButton" },
                Padding = new Thickness(8, 3)
            };
            _clearButton.Click += (s, e) => ClearLogs();

            _filterComboBox = new ComboBox
            {
                Width = 120,
                SelectedIndex = 0
            };

            _filterComboBox.Items.Add("All");
            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
            {
                if (level != LogLevel.None && level != LogLevel.All)
                {
                    _filterComboBox.Items.Add(level.ToString());
                }
            }
            _filterComboBox.Items.Add("None");

            _filterComboBox.SelectionChanged += (s, e) =>
            {
                var selectedItem = _filterComboBox.SelectedItem?.ToString();
                if (selectedItem == "All")
                {
                    SetMaxLevelFilter(LogLevel.All);
                }
                else if (selectedItem == "None")
                {
                    SetMaxLevelFilter(LogLevel.None);
                }
                else if (Enum.TryParse<LogLevel>(selectedItem, out var level))
                {
                    SetMaxLevelFilter(level);
                }
                RefreshLogDisplay();
            };

            toolbarPanel.Children.Add(_filterComboBox);
            toolbarPanel.Children.Add(_clearButton);

            _logPanel = new StackPanel
            {
                Spacing = 2,
                Margin = new Thickness(5)
            };

            _scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = _logPanel,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30))
            };

            _commandInput = new TextBox
            {
                Watermark = "Enter command...",
                Margin = new Thickness(0, 5, 0, 0)
            };
            _commandInput.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Enter)
                {
                    ProcessCommand(_commandInput.Text);
                    _commandInput.Text = string.Empty;
                }
            };

            Grid.SetRow(_scrollViewer, 0);
            Grid.SetRow(toolbarPanel, 1);
            Grid.SetRow(_commandInput, 2);

            this.Children.Add(_scrollViewer);
            this.Children.Add(toolbarPanel);
            this.Children.Add(_commandInput);
        }

        private void ProcessCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            Log($"> {command}", LogLevel.Debug);

            if (command.StartsWith("/"))
            {
                var parts = command.Substring(1).Split(new[] { ' ' }, 2);
                var cmd = parts[0].ToLower();
                var args = parts.Length > 1 ? parts[1] : string.Empty;

                switch (cmd)
                {
                    case "clear":
                        ClearLogs();
                        break;
                    case "help":
                        ShowHelp();
                        break;
                    case "filter":
                        if (Enum.TryParse<LogLevel>(args, true, out var level))
                        {
                            SetMaxLevelFilter(level);
                            Log($"Filter set to {level}", LogLevel.Info);
                            RefreshLogDisplay();
                        }
                        else
                        {
                            Log($"Unknown log level: {args}", LogLevel.Error);
                        }
                        break;
                    case "enable":
                        if (Enum.TryParse<LogLevel>(args, true, out var enableLevel))
                        {
                            EnableLevel(enableLevel);
                            Log($"Enabled {enableLevel} logs", LogLevel.Info);
                            RefreshLogDisplay();
                        }
                        else
                        {
                            Log($"Unknown log level: {args}", LogLevel.Error);
                        }
                        break;
                    case "disable":
                        if (Enum.TryParse<LogLevel>(args, true, out var disableLevel))
                        {
                            DisableLevel(disableLevel);
                            Log($"Disabled {disableLevel} logs", LogLevel.Info);
                            RefreshLogDisplay();
                        }
                        else
                        {
                            Log($"Unknown log level: {args}", LogLevel.Error);
                        }
                        break;
                    default:
                        Log($"Unknown command: {cmd}", LogLevel.Warn);
                        break;
                }
            }
            // Простое эхо для других команд
            else
            {
                Info("Echo:", command);
            }
        }

        private void ShowHelp()
        {
            Info("Available commands:");
            Info("/clear - Clear console");
            Info("/help - Show this help");
            Info("/filter <level> - Set max log level filter");
            Info("/enable <level> - Enable specific log level");
            Info("/disable <level> - Disable specific log level");
            Info("Log levels: Debug, Info, Warn, Error, Fatal, All, None");
        }

        private void ClearLogs()
        {
            _logEntries.Clear();
            _logPanel.Children.Clear();
            Log("Console cleared", LogLevel.Debug);
        }

        private void RefreshLogDisplay()
        {
            _logPanel.Children.Clear();
            foreach (var entry in _logEntries)
            {
                if ((entry.Level & LogLevel) != 0)
                {
                    AddLogEntryToPanel(entry);
                }
            }
            ScrollToEnd();
        }

        public void Log(string message, LogLevel logLevel)
        {
            if (message == null) return;
            if ((logLevel & LogLevel) == 0) return;

            var entry = new LogEntry(message, logLevel);
            _logEntries.Add(entry);

            // Ограничиваем количество записей
            if (_logEntries.Count > MaxLogEntries)
            {
                _logEntries.RemoveAt(0);
                if (_logPanel.Children.Count > 0)
                {
                    _logPanel.Children.RemoveAt(0);
                }
            }

            AddLogEntryToPanel(entry);
            ScrollToEnd();
        }

        private void AddLogEntryToPanel(LogEntry entry)
        {
            var logText = new TextBlock
            {
                Text = $"[{entry.GetTimestampString()}] [{entry.GetLevelString()}] {entry.Message}",
                TextWrapping = TextWrapping.Wrap,
                Foreground = entry.GetColor(),
                FontFamily = new FontFamily("Consolas, Menlo, Monospace"),
                Margin = new Thickness(0, 0, 0, 1)
            };
            _logPanel.Children.Add(logText);
        }

        private void ScrollToEnd()
        {
            _scrollViewer.ScrollToEnd();
        }

        public void Debug(params object[] args) => Log(string.Join(" ", args), LogLevel.Debug);
        public void Info(params object[] args) => Log(string.Join(" ", args), LogLevel.Info);
        public void Warn(params object[] args) => Log(string.Join(" ", args), LogLevel.Warn);
        public void Error(params object[] args) => Log(string.Join(" ", args), LogLevel.Error);
        public void Fatal(params object[] args) => Log(string.Join(" ", args), LogLevel.Fatal);

        public void SetMaxLevelFilter(LogLevel logLevel)
        {
            LogLevel = logLevel == LogLevel.None ? LogLevel.None : (LogLevel)((int)logLevel * 2 - 1);
        }
        public void EnableLevel(LogLevel logLevel)
        {
            LogLevel |= logLevel;
        }
        public void DisableLevel(LogLevel logLevel)
        {
            LogLevel &= ~logLevel;
        }

    }
}
