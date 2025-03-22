using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using System;
using System.Linq;
using System.IO;

namespace Editor
{
    public class ObjectInputField : Grid
    {
        public static readonly StyledProperty<string> ObjectPathProperty =
            AvaloniaProperty.Register<ObjectInputField, string>(nameof(ObjectPath));

        public static readonly StyledProperty<string[]> AllowedExtensionsProperty =
            AvaloniaProperty.Register<ObjectInputField, string[]>(nameof(AllowedExtensions));

        public static readonly StyledProperty<string> PlaceholderTextProperty =
            AvaloniaProperty.Register<ObjectInputField, string>(nameof(PlaceholderText), "None");

        public string ObjectPath
        {
            get => GetValue(ObjectPathProperty);
            set => SetValue(ObjectPathProperty, value);
        }

        public string[] AllowedExtensions
        {
            get => GetValue(AllowedExtensionsProperty);
            set => SetValue(AllowedExtensionsProperty, value);
        }

        public string PlaceholderText
        {
            get => GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        public event EventHandler<string> ObjectChanged;

        private Border _mainBorder;
        private Image _previewImage;
        private TextBlock _objectNameText;
        private Button _browseButton;
        private Button _clearButton;
        public object Value;

        public ObjectInputField()
        {
            InitializeComponent();

            PropertyChanged += (s, e) => {
                if (e.Property == ObjectPathProperty)
                {
                    UpdateUI();
                }
            };
        }

        private void InitializeComponent()
        {
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _mainBorder = new Border
            {
                Classes = { "objectFieldBorder" },
                Padding = new Thickness(4),
                Height = 24
            };

            Children.Add(_mainBorder);
            Grid.SetColumnSpan(_mainBorder, 3);

            _previewImage = new Image
            {
                Width = 16,
                Height = 16,
                Margin = new Thickness(4, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            _objectNameText = new TextBlock
            {
                Text = PlaceholderText,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0),
                Foreground = new SolidColorBrush(Colors.White)
            };

            _browseButton = new Button
            {
                Content = "⋯",
                Width = 24,
                Height = 24,
                Padding = new Thickness(0),
                Margin = new Thickness(2, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Classes = { "objectFieldButton" }
            };
            _browseButton.Click += OnBrowseButtonClick;

            _clearButton = new Button
            {
                Content = "×",
                Width = 24,
                Height = 24,
                Padding = new Thickness(0),
                Margin = new Thickness(2, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Classes = { "objectFieldButton" }
            };
            _clearButton.Click += OnClearButtonClick;

            Grid.SetColumn(_previewImage, 0);
            Grid.SetColumn(_objectNameText, 1);
            Grid.SetColumn(_browseButton, 2);
            Grid.SetColumn(_clearButton, 2);

            Children.Add(_previewImage);
            Children.Add(_objectNameText);
            Children.Add(_browseButton);
            Children.Add(_clearButton);

            EnableDragDrop();

            UpdateUI();
        }

        private void EnableDragDrop()
        {
            DragDrop.SetAllowDrop(this, true);

            this.AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
            this.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            this.AddHandler(DragDrop.DropEvent, OnDrop);
        }

        private void OnDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Text))
            {
                try
                {
                    var jsonData = e.Data.Get(DataFormats.Text) as string;
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        var fileEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<DragDropEventArgs>(jsonData);

                        if (IsValidFileType(fileEvent.FileExtension))
                        {
                            _mainBorder.BorderBrush = new SolidColorBrush(Colors.DodgerBlue);
                            _mainBorder.Background = new SolidColorBrush(Color.FromArgb(50, 30, 144, 255));
                            e.DragEffects = DragDropEffects.Copy;
                        }
                        else
                        {
                            e.DragEffects = DragDropEffects.None;
                        }
                    }
                }
                catch
                {
                    e.DragEffects = DragDropEffects.None;
                }
            }

            e.Handled = true;
        }

        private void OnDragLeave(object? sender, DragEventArgs e)
        {
            ResetDragVisual();
            e.Handled = true;
        }

        private void OnDrop(object? sender, DragEventArgs e)
        {
            ResetDragVisual();

            if (e.Data.Contains(DataFormats.Text))
            {
                try
                {
                    var jsonData = e.Data.Get(DataFormats.Text) as string;
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        var fileEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<FileEvent>(jsonData);

                        if (IsValidFileType(fileEvent.FileExtension))
                        {
                            ObjectPath = fileEvent.FileFullPath;
                            ObjectChanged?.Invoke(this, jsonData);
                        }
                        else
                        {
                            Status.SetStatus($"Неподдерживаемый тип файла: {fileEvent.FileExtension}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Status.SetStatus($"Ошибка при обработке файла: {ex.Message}");
                }
            }

            e.Handled = true;
        }

        public void ResetValue(bool withIvoke = true)
        {
            ObjectPath = string.Empty;
            if (withIvoke) ObjectChanged?.Invoke(this, null);
            UpdateUI();
        }

        private void ResetDragVisual()
        {
            _mainBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(45, 45, 45));
            _mainBorder.Background = new SolidColorBrush(Color.FromRgb(56, 56, 56));
        }

        private bool IsValidFileType(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return false;

            if (AllowedExtensions == null || AllowedExtensions.Length == 0)
                return true;

            return AllowedExtensions.Any(ext =>
                extension.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }

        private async void OnBrowseButtonClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Title = "Выберите объект"
            };

            if (AllowedExtensions != null && AllowedExtensions.Length > 0)
            {
                dialog.Filters.Add(new FileDialogFilter
                {
                    Name = "Поддерживаемые типы",
                    Extensions = AllowedExtensions.Select(ext => ext.TrimStart('.')).ToList()
                });
            }

            var result = await dialog.ShowAsync(
                Window.GetTopLevel(this) as Window);

            if (result != null && result.Length > 0)
            {
                ObjectPath = result[0];
                ObjectChanged?.Invoke(this, ObjectPath);
                Status.SetStatus($"Объект выбран: {Path.GetFileName(ObjectPath)}");
            }
        }

        private void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            ObjectPath = null;
            ObjectChanged?.Invoke(this, null);
            UpdateUI();
        }

        internal void UpdateUI()
        {
            bool hasObject = !string.IsNullOrEmpty(ObjectPath);

            _objectNameText.Text = hasObject
                ? Path.GetFileName(ObjectPath)
                : PlaceholderText;

            _objectNameText.Foreground = hasObject
                ? new SolidColorBrush(Colors.White)
                : new SolidColorBrush(Color.FromRgb(170, 170, 170));

            _clearButton.IsVisible = hasObject;

            // Устанавливаем соответствующую иконку в зависимости от типа файла
            if (hasObject)
            {
                string extension = Path.GetExtension(ObjectPath).ToLowerInvariant();
                // Можно добавить логику для отображения разных иконок в зависимости от типа файла
                // Например, через словарь ресурсов
            }
        }
    }
}