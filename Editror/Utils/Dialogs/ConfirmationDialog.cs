using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Interactivity;
using AtomEngine;

namespace Editor
{
    /// <summary>
    /// Простой диалог подтверждения
    /// </summary>
    public class ConfirmationDialog : Window
    {
        private readonly Button _yesButton;
        private readonly Button _noButton;
        private readonly Button _cancelButton;

        public enum DialogResult
        {
            Yes,
            No,
            Cancel
        }

        private DialogResult _result = DialogResult.Cancel;

        public ConfirmationDialog(string title, string message, bool showCancel = true)
        {
            Title = title;
            Classes.Add("dialog");
            SystemDecorations = SystemDecorations.None;

            var mainGrid = new Grid
            {
                Margin = new Thickness(20)
            };

            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var messageText = new TextBlock
            {
                Text = message,
                Classes = { "dialogText" }
            };
            Grid.SetRow(messageText, 1);

            var buttonPanel = new StackPanel
            {
                Classes = { "dialogButtonPanel" }
            };

            _yesButton = new Button
            {
                Content = "Yes",
                Width = 80,
                Classes = { "primaryButton", "dialogButton" }
            };

            _noButton = new Button
            {
                Content = "No",
                Width = 80,
                Classes = { "dialogButton" }
            };

            _cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                IsVisible = showCancel,
                Classes = { "dialogButton" }
            };

            _yesButton.Click += (s, e) =>
            {
                _result = DialogResult.Yes;
                Close();
            };

            _noButton.Click += (s, e) =>
            {
                _result = DialogResult.No;
                Close();
            };

            _cancelButton.Click += (s, e) =>
            {
                _result = DialogResult.Cancel;
                Close();
            };

            buttonPanel.Children.Add(_yesButton);
            buttonPanel.Children.Add(_noButton);

            if (showCancel)
            {
                buttonPanel.Children.Add(_cancelButton);
            }

            Grid.SetRow(buttonPanel, 2);

            mainGrid.Children.Add(messageText);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        /// <summary>
        /// Отображает диалог подтверждения и возвращает результат
        /// </summary>
        public static async Task<DialogResult> Show(Window owner, string title, string message, bool showCancel = true)
        {
            var dialog = new ConfirmationDialog(title, message, showCancel);
            await dialog.ShowDialog(owner);
            return dialog._result;
        }
    }
}