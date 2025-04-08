using Avalonia.Controls.Primitives;
using System.Collections.Generic;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Linq;
using AtomEngine;
using Avalonia;
using System;
using CommonLib;
using EngineLib;

namespace Editor
{
    public class ConsoleController : ContentControl, IWindowed, ILogger
    {
        private Grid _mainGrid;
        private ScrollViewer _scrollViewer;
        private StackPanel _logPanel;
        private TextBox _commandInput;
        private Button _clearButton;
        private ComboBox _filterComboBox;
        private int MaxLogEntries = 100;
        private List<LogEntry> _logEntries = new List<LogEntry>();
        private bool _isOpen = false;

        private Border _lastClickedContainer;
        private DateTime _lastClickTime;
        private int _clickCount;
        private readonly TimeSpan _doubleClickThreshold = TimeSpan.FromMilliseconds(500);

        public LogLevel LogLevel { get; set; } = LogLevel.All;

        public Action<object> OnClose { get; set; }

        private List<ConsoleCommand> _commands = new List<ConsoleCommand>();
        private class LogEntry : AtomEngine.DebLogger.LogEntry
        {
            public LogEntry(string message, LogLevel level) : base(message, level) { }
            public LogEntry(string message, LogLevel level, CallerInfo callerInfo) : base(message, level, callerInfo) { }
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
        }

        public ConsoleController()
        {
            InitializeUI();
            InitializeDefaultCommands();
        }

        public void RegisterConsoleCommand(ConsoleCommand command)
        {
            if (!_commands.Contains(command)) _commands.Add(command);
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
            _mainGrid = new Grid();
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

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
            _filterComboBox.SelectedIndex = 0;

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

            _mainGrid.Children.Add(_scrollViewer);
            _mainGrid.Children.Add(toolbarPanel);
            _mainGrid.Children.Add(_commandInput);

            this.Content = _mainGrid;
        }

        private void ProcessCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;

            DebLogger.Debug($"> {command}", LogLevel.Debug);

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

        public void Log(string message, LogLevel logLevel) => Log(message, logLevel, null);
        public void Log(string message, LogLevel logLevel, DebLogger.LogEntry _entry = null)
        {
            if (message == null) return;
            if ((logLevel & LogLevel) == 0) return;

            var entry = new LogEntry(message, logLevel);
            if (_entry != null)
            {
                entry.CallerInfo = _entry.CallerInfo;
            }
            else
            {
                try { entry.CallerInfo = StackTraceHelper.GetCallerInfo(3); }
                catch { }
            }

            _logEntries.Add(entry);


            Dispatcher.UIThread.Invoke(new Action(() =>
            {
                if (_logEntries.Count > MaxLogEntries)
                {
                    _logEntries.RemoveAt(0);
                    if (_logPanel.Children.Count > 0)
                    {
                        _logPanel.Children.RemoveAt(0);
                    }
                }
            }));

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
            var logText = new SelectableTextBlock
            {
                Text = $"[{entry.GetTimestampString()}] [{entry.GetLevelString()}] {entry.Message}",
                TextWrapping = TextWrapping.Wrap,
                Foreground = entry.GetColor(),
                FontFamily = new FontFamily("Consolas, Menlo, Monospace"),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var container = new Border
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(5),
                Background = new SolidColorBrush(Colors.Transparent),
                Child = logText
            };

            container.Tag = entry;
            container.PointerPressed += Container_PointerPressed;

            _logPanel.Children.Add(container);


            //var container = new Border
            //{
            //    HorizontalAlignment = HorizontalAlignment.Stretch,
            //    Margin = new Thickness(0, 0, 0, 1),
            //};
            //var logText = new TextBox
            //{
            //    Text = $"[{entry.GetTimestampString()}] [{entry.GetLevelString()}] {entry.Message}",
            //    TextWrapping = TextWrapping.Wrap,
            //    IsReadOnly = true,
            //    AcceptsReturn = true,
            //    AcceptsTab = true,
            //    CaretBrush = Brushes.Transparent,
            //    Foreground = entry.GetColor(),
            //    FontFamily = new FontFamily("Consolas, Menlo, Monospace"),
            //    HorizontalAlignment = HorizontalAlignment.Stretch
            //};
            //logText.Tag = entry;
            //logText.PointerPressed += LogText_PointerPressed;
            //container.Child = logText;
            //_logPanel.Children.Add(container);
        }
        
        private void Container_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            var container = sender as Border;
            if (container == null) return;

            var currentTime = DateTime.Now;
            if (container == _lastClickedContainer &&
                (currentTime - _lastClickTime) < _doubleClickThreshold)
            {
                _clickCount++;

                if (_clickCount >= 3)
                {
                    if (container.Tag is LogEntry entry && entry.CallerInfo.IsValid)
                    {
                        _clickCount = 0;
                        OpenSourceInIDE(entry.CallerInfo);
                    }
                }
            }
            else
            {
                _clickCount = 1;
                _lastClickedContainer = container;
            }

            _lastClickTime = currentTime;
        }

        
        private void OpenSourceInIDE(CallerInfo callerInfo)
        {
            if (string.IsNullOrEmpty(callerInfo.FilePath) || callerInfo.LineNumber <= 0)
            {
                Log("Невозможно открыть файл: недостаточно информации о месте вызова", LogLevel.Warn);
                return;
            }
            try
            {
                var scriptGenerator = ServiceHub.Get<ScriptProjectGenerator>();
                if (scriptGenerator != null)
                {
                    scriptGenerator.OpenProjectInIDE(callerInfo.FilePath);
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка при открытии файла в IDE: {ex.Message}", LogLevel.Error);
            }
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

        public void Open()
        {
            DebLogger.AddLogger(this);
            _isOpen = true;

            Dispatcher.UIThread.Invoke(new Action(() =>
            {
                var logs = DebLogger.GetLogs();
                foreach (var log in logs)
                {
                    if (log != null)
                    {
                        Log(log.Message, log.Level, log);
                    }
                }
            }));
        }

        public void Close()
        {
            _isOpen = false;
            DebLogger.RemoveLogger(this);
            _logEntries.Clear();
            _logPanel.Children.Clear();

            OnClose?.Invoke(this);
        }

        public void Dispose()
        {
        }

        public void Redraw()
        {
        }
    }
}