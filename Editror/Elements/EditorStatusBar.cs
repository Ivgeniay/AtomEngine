using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia;
using System;

namespace Editor
{
    public class EditorStatusBar : IStatusProvider
    {
        private Border _container;
        private TextBlock _statusText;
        private TextBlock _positionText;
        private TextBlock _selectionText;
        private TextBlock _encodingText;
        private TextBlock _modeText;

        public EditorStatusBar(Border container)
        {
            _container = container;

            if (_container == null)
                throw new ArgumentNullException(nameof(container), "StatusBar container cannot be null");

            CreateStatusBar();
            Status.RegisterStatusProvider(this);
        }

        private void CreateStatusBar()
        {
            if (_container == null) return;

            var statusBarBorder = new Border
            {
                Classes = { "statusBar" },
                Height = 22
            };

            var statusBarGrid = new Grid
            {
                Margin = new Thickness(5, 0)
            };

            statusBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            statusBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            statusBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            statusBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            statusBarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _statusText = new TextBlock
            {
                Classes = { "statusText" },
                VerticalAlignment = VerticalAlignment.Center,
                Text = ""
            };

            _positionText = new TextBlock
            {
                Classes = { "statusInfoText" },
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0),
                Text = "Ln 1, Col 1"
            };

            _selectionText = new TextBlock
            {
                Classes = { "statusInfoText" },
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0),
                Text = "Sel 0|0"
            };

            _encodingText = new TextBlock
            {
                Classes = { "statusInfoText" },
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0),
                Text = "UTF-8"
            };

            _modeText = new TextBlock
            {
                Classes = { "statusInfoText" },
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0),
                Text = "Normal Mode"
            };

            Grid.SetColumn(_statusText, 0);
            Grid.SetColumn(_positionText, 1);
            Grid.SetColumn(_selectionText, 2);
            Grid.SetColumn(_encodingText, 3);
            Grid.SetColumn(_modeText, 4);

            statusBarGrid.Children.Add(_statusText);
            statusBarGrid.Children.Add(_positionText);
            statusBarGrid.Children.Add(_selectionText);
            statusBarGrid.Children.Add(_encodingText);
            statusBarGrid.Children.Add(_modeText);

            statusBarBorder.Child = statusBarGrid;
            _container.Child = statusBarBorder;
        }

        /// <summary>
        /// Устанавливает основной текст статус-бара
        /// </summary>
        public void SetStatus(string text)
        {
            if (_statusText == null) return;
            _statusText.Text = text ?? "Ready";
        }

        /// <summary>
        /// Устанавливает информацию о текущей позиции курсора
        /// </summary>
        public void SetPosition(int line, int column)
        {
            if (_positionText == null) return;
            _positionText.Text = $"Ln {Math.Max(1, line)}, Col {Math.Max(1, column)}";
        }

        /// <summary>
        /// Устанавливает информацию о выделении текста
        /// </summary>
        public void SetSelection(int characters, int lines)
        {
            if (_selectionText == null) return;
            _selectionText.Text = $"Sel {Math.Max(0, characters)}|{Math.Max(0, lines)}";
        }

        /// <summary>
        /// Устанавливает информацию о кодировке
        /// </summary>
        public void SetEncoding(string encoding)
        {
            if (_encodingText == null) return;
            _encodingText.Text = !string.IsNullOrEmpty(encoding) ? encoding : "UTF-8";
        }

        /// <summary>
        /// Устанавливает информацию о текущем режиме
        /// </summary>
        public void SetMode(string mode)
        {
            if (_modeText == null) return;
            _modeText.Text = !string.IsNullOrEmpty(mode) ? mode : "Normal Mode";
        }
    }
}