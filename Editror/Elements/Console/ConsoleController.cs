using Avalonia.Controls.Primitives;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using System;
using System.Linq;
using Avalonia.Threading;
using AtomEngine;

namespace Editor
{

    public class ConsoleController : Grid, ILogger
    {
        public static ConsoleController Instance;
        private ScrollViewer _scrollViewer;
        private StackPanel _logPanel;
        private TextBox _commandInput;
        private Button _clearButton;
        private ComboBox _filterComboBox;
        private const int MaxLogEntries = 1000;
        private List<LogEntry> _logEntries = new List<LogEntry>();

        public LogLevel LogLevel { get; set; } = LogLevel.All;
        private List<ConsoleCommand> _commands = new List<ConsoleCommand>();
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
            ConsoleController.Instance = this;
            InitializeUI();
            InitializeDefaultCommands();
            Log("Console initialized", LogLevel.Info);
        }
        public void RegisterConsoleCommand(ConsoleCommand command)
        {
            if (!_commands.Contains(command))_commands.Add(command);
        }
        private void InitializeDefaultCommands()
        {
            _commands.Add(new ConsoleCommand()
            {
                CommandName = "clear",
                Description = "Clear console",
                Action = (e) => ClearLogs()
            });
            _commands.Add(new ConsoleCommand()
            {
                CommandName = "help",
                Description = "Show this help",
                Action = (e) => ShowHelp()
            });
            _commands.Add(new ConsoleCommand()
            {
                CommandName = "filter",
                Description = "Set max log level filter",
                Action = (e) =>
                {
                    if (Enum.TryParse<LogLevel>(e, true, out var level))
                    {
                        SetMaxLevelFilter(level);
                        Log($"Filter set to {level}", LogLevel.Info);
                        RefreshLogDisplay();
                    }
                    else
                    {
                        Log($"Unknown log level: {e}", LogLevel.Error);
                    }
                }
            });
            _commands.Add(new ConsoleCommand()
            {
                CommandName = "enable",
                Description = "Enable specific log level",
                Action = (e) =>
                {
                    if (Enum.TryParse<LogLevel>(e, true, out var enableLevel))
                    {
                        EnableLevel(enableLevel);
                        Log($"Enabled {enableLevel} logs", LogLevel.Info);
                        RefreshLogDisplay();
                    }
                    else
                    {
                        Log($"Unknown log level: {e}", LogLevel.Error);
                    }
                }
            });
            _commands.Add(new ConsoleCommand()
            {
                CommandName = "disable",
                Description = "Disable specific log level",
                Action = (e) => {
                    if (Enum.TryParse<LogLevel>(e, true, out var disableLevel))
                    {
                        DisableLevel(disableLevel);
                        Log($"Disabled {disableLevel} logs", LogLevel.Info);
                        RefreshLogDisplay();
                    }
                    else
                    {
                        Log($"Unknown log level: {e}", LogLevel.Error);
                    }
                }
            });
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
            _filterComboBox.SelectedItem = 0;
            _filterComboBox.SelectedValue = "All";

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
                    SetOnlyLevel(level);
                }
                RefreshLogDisplay();
            };

            toolbarPanel.Children.Add(_filterComboBox);
            toolbarPanel.Children.Add(_clearButton);

            _logPanel = new StackPanel
            {
                Spacing = 2,
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Width = double.NaN
            };

            _scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = _logPanel,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
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

                var consoleCommand = _commands.Where(e => e.CommandName == cmd).FirstOrDefault();
                if (consoleCommand != null)
                {
                    if (consoleCommand.Action != null)
                        consoleCommand.Action(args);
                }
                else
                {
                    Log($"Unknown command: {cmd}", LogLevel.Warn);
                }
            }
            else
            {
                Info("Echo:", command);
            }
        }

        private void ShowHelp()
        {
            Info("--------------------------");
            Info("Available commands:");
            foreach (var command in _commands)
            {
                Info($"/{command.CommandName} - {command.Description}");
            }
            Info("--------------------------");
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
            if (Dispatcher.UIThread.CheckAccess()) AddLogEntryToPanelCore(entry);
            else Dispatcher.UIThread.Post(() => AddLogEntryToPanelCore(entry));
        }

        private void AddLogEntryToPanelCore(LogEntry entry)
        {
            var container = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 1),
                //Padding = new Thickness(5) // Добавляем отступ для текста
            };

            //var logText = new TextBlock
            //{
            //    Text = $"[{entry.GetTimestampString()}] [{entry.GetLevelString()}] {entry.Message}",
            //    TextWrapping = TextWrapping.Wrap,
            //    Foreground = entry.GetColor(),
            //    FontFamily = new FontFamily("Consolas, Menlo, Monospace"),
            //    HorizontalAlignment = HorizontalAlignment.Stretch
            //};
            var logText = new TextBox
            {
                Text = $"[{entry.GetTimestampString()}] [{entry.GetLevelString()}] {entry.Message}",
                TextWrapping = TextWrapping.Wrap,
                IsReadOnly = true,
                AcceptsReturn = true,
                AcceptsTab = true,
                CaretBrush = Brushes.Transparent,
                Foreground = entry.GetColor(),
                FontFamily = new FontFamily("Consolas, Menlo, Monospace"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            container.Child = logText;
            _logPanel.Children.Add(container);
        }

        private void ScrollToEnd()
        {
            if (Dispatcher.UIThread.CheckAccess()) ScrollToEndCore();
            else Dispatcher.UIThread.Post(() => ScrollToEndCore());
        }

        private void ScrollToEndCore() => _scrollViewer.ScrollToEnd();

        public void Debug(params object[] args) => Log(string.Join(" ", args), LogLevel.Debug);
        public void Info(params object[] args) => Log(string.Join(" ", args), LogLevel.Info);
        public void Warn(params object[] args) => Log(string.Join(" ", args), LogLevel.Warn);
        public void Error(params object[] args) => Log(string.Join(" ", args), LogLevel.Error);
        public void Fatal(params object[] args) => Log(string.Join(" ", args), LogLevel.Fatal);

        public void SetMaxLevelFilter(LogLevel maxLevel)
        {
            if (maxLevel == LogLevel.None)
            {
                LogLevel = LogLevel.None;
                return;
            }

            LogLevel result = LogLevel.None;
            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
            {
                if (level == LogLevel.None || level == LogLevel.All)
                    continue;

                result |= level;
                if (level == maxLevel)
                    break;
            }

            LogLevel = result;
        }
        public void EnableLevel(LogLevel logLevel)
        {
            LogLevel |= logLevel;
        }
        public void DisableLevel(LogLevel logLevel)
        {
            LogLevel &= ~logLevel;
        }
        public void SetOnlyLevel(LogLevel logLevel)
        {
            LogLevel = LogLevel.None;
            LogLevel |= logLevel;
        }

    }
}
