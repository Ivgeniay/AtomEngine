using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia;
using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor
{
    /// <summary>
    /// Диалог для отображения прогресса операций
    /// </summary>
    public class ProgressDialog : Window
    {
        private readonly ProgressBar _progressBar;
        private readonly TextBlock _statusText;
        private readonly Button _cancelButton;
        private bool _isCancelled = false;

        public event EventHandler CancelRequested;

        public ProgressDialog(string title, string message, bool showCancelButton = false)
        {
            Title = title;
            Width = 400;
            Height = 150;
            CanResize = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.Parse("#2D2D30"));

            var mainGrid = new Grid
            {
                Margin = new Thickness(20)
            };

            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _statusText = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(_statusText, 0);

            _progressBar = new ProgressBar
            {
                Value = 0,
                Minimum = 0,
                Maximum = 100,
                Height = 20,
                Margin = new Thickness(0, 10, 0, 15)
            };
            Grid.SetRow(_progressBar, 1);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            _cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                IsVisible = showCancelButton,
                Classes = { "dialogButton" }
            };

            _cancelButton.Click += (s, e) =>
            {
                _isCancelled = true;
                CancelRequested?.Invoke(this, EventArgs.Empty);
                UpdateStatus("Cancelling operation...");
            };

            buttonPanel.Children.Add(_cancelButton);
            Grid.SetRow(buttonPanel, 2);

            mainGrid.Children.Add(_statusText);
            mainGrid.Children.Add(_progressBar);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        /// <summary>
        /// Обновляет текст статуса
        /// </summary>
        public void SetStatus(string status)
        {
            UpdateStatus(status);
        }

        private void UpdateStatus(string status)
        {
            if (_statusText == null) return;

            if (Application.Current != null)
            {
                // Для безопасного обновления UI из любого потока
                //Application.Current.MainLoop.DispatchAsync(() => {
                //    _statusText.Text = status;
                //});
            }
            else
            {
                _statusText.Text = status;
            }
        }

        /// <summary>
        /// Обновляет значение прогресса (0-100)
        /// </summary>
        public void SetProgress(double value)
        {
            if (_progressBar == null) return;

            if (Application.Current != null)
            {
                // Для безопасного обновления UI из любого потока
                //Application.Current.MainLoop.DispatchAsync(() => {
                //    _progressBar.Value = Math.Clamp(value, 0, 100);
                //});
            }
            else
            {
                _progressBar.Value = Math.Clamp(value, 0, 100);
            }
        }

        /// <summary>
        /// Показывает модальный прогресс-диалог с текущим прогрессом
        /// </summary>
        public static async Task ShowProgress(Window owner, string title, string message,
            Func<ProgressDialog, Task> action, bool canCancel = false)
        {
            var dialog = new ProgressDialog(title, message, canCancel);
            dialog.SetProgress(0);

            // Запускаем задачу
            var progressTask = Task.Run(async () =>
            {
                try
                {
                    await action(dialog);
                }
                catch (Exception ex)
                {
                    dialog.SetStatus($"Error: {ex.Message}");
                    await Task.Delay(2000); // Даем пользователю время прочитать сообщение об ошибке
                }
            });

            // Показываем диалог
            await dialog.ShowDialog(owner);

            // Ждем завершения задачи (макс 5 секунд), в случае закрытия окна
            await Task.WhenAny(progressTask, Task.Delay(5000));
        }

        /// <summary>
        /// Проверяет, была ли операция отменена пользователем
        /// </summary>
        public bool IsCancelled => _isCancelled;
    }
}