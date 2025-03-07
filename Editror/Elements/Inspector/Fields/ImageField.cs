using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using Avalonia;
using System;
using System.IO;
using System.Numerics;

namespace Editor
{
    internal class ImageField : Grid
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<ImageField, string>(nameof(Label), string.Empty);

        public static readonly StyledProperty<string> ImagePathProperty =
            AvaloniaProperty.Register<ImageField, string>(nameof(ImagePath));

        public static readonly StyledProperty<string[]> AllowedExtensionsProperty =
            AvaloniaProperty.Register<ImageField, string[]>(nameof(AllowedExtensions));

        public static readonly StyledProperty<string> PlaceholderTextProperty =
            AvaloniaProperty.Register<ImageField, string>(nameof(PlaceholderText), "None");

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<ImageField, bool>(nameof(IsReadOnly), false);

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string ImagePath
        {
            get => GetValue(ImagePathProperty);
            set => SetValue(ImagePathProperty, value);
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

        public bool IsReadOnly
        {
            get => GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }


        public event EventHandler<string> ImageChanged;

        private TextBlock _labelControl;
        private Border _imageContainer;
        private Image _imagePreview;
        private TextBlock _placeholderText;
        private StackPanel _buttonPanel;
        private Button _browseButton;
        private Button _clearButton;
        private Border _dropIndicator;
        private int _width;
        private int _height;

        public ImageField(int width = 64, int height = 64)
        {
            _width = width;
            _height = height;

            InitializeComponent();
            SetupEventHandlers();
        }

        private void InitializeComponent()
        {
            Margin = new Thickness(4, 0);
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            _labelControl = new TextBlock
            {
                Classes = { "propertyLabel" }
            };
            Grid.SetColumn(_labelControl, 0);

            var contentPanel = new StackPanel
            {
                Spacing = 8,
                Orientation = Orientation.Vertical,
            };
            Grid.SetColumn(contentPanel, 1);

            _imageContainer = new Border
            {
                Classes = { "imageContainer" },
                Height = _height,
                Width = _width,
            };

            var overlayGrid = new Grid();

            _imagePreview = new Image
            {
                Classes = { "imagePreview" },
            };

            _placeholderText = new TextBlock
            {
                Classes = { "placeholderText" },
                Text = PlaceholderText,
            };

            overlayGrid.Children.Add(_imagePreview);
            overlayGrid.Children.Add(_placeholderText);
            _imageContainer.Child = overlayGrid;

            //_buttonPanel = new StackPanel
            //{
            //    Classes = { "buttonPanel" },
            //    Orientation = Orientation.Horizontal,
            //    Spacing = 4
            //};
            //_browseButton = new Button
            //{
            //    Content = "Выбрать",
            //    Classes = { "actionButton" },
            //    IsEnabled = !IsReadOnly
            //};
            //_clearButton = new Button
            //{
            //    Content = "Очистить",
            //    Classes = { "actionButton" },
            //    IsEnabled = !IsReadOnly
            //};
            //_buttonPanel.Children.Add(_browseButton);
            //_buttonPanel.Children.Add(_clearButton);

            _dropIndicator = new Border
            {
                Classes = { "dropIndicator" },
                IsVisible = false
            };

            contentPanel.Children.Add(_imageContainer);
            //contentPanel.Children.Add(_buttonPanel);

            Children.Add(_labelControl);
            Children.Add(contentPanel);
            overlayGrid.Children.Add(_dropIndicator);

            EnableDragDrop();
        }

        private void SetupEventHandlers()
        {
            this.PropertyChanged += (s, e) =>
            {
                if (e.Property == LabelProperty)
                {
                    _labelControl.Text = Label;
                }
                else if (e.Property == ImagePathProperty)
                {
                    UpdateImagePreview();
                }
                else if (e.Property == PlaceholderTextProperty)
                {
                    _placeholderText.Text = PlaceholderText;
                }
                else if (e.Property == IsReadOnlyProperty)
                {
                    _browseButton.IsEnabled = !IsReadOnly;
                    _clearButton.IsEnabled = !IsReadOnly;
                }
            };

            //_browseButton.Click += OnBrowseButtonClick;
            //_clearButton.Click += OnClearButtonClick;

            _labelControl.Text = Label;
            _placeholderText.Text = PlaceholderText;
            //_browseButton.IsEnabled = !IsReadOnly;
            //_clearButton.IsEnabled = !IsReadOnly;

            UpdateImagePreview();
        }

        private void UpdateImagePreview()
        {
            bool hasImage = !string.IsNullOrEmpty(ImagePath);

            if (hasImage && File.Exists(ImagePath))
            {
                try
                {
                    _imagePreview.Source = new Bitmap(ImagePath);
                    _imagePreview.IsVisible = true;
                    _placeholderText.IsVisible = false;
                }
                catch (Exception ex)
                {
                    _imagePreview.Source = null;
                    _imagePreview.IsVisible = false;
                    _placeholderText.IsVisible = true;
                    _placeholderText.Text = $"Ошибка загрузки: {ex.Message}";
                }
            }
            else
            {
                _imagePreview.Source = null;
                _imagePreview.IsVisible = false;
                _placeholderText.IsVisible = true;
                _placeholderText.Text = PlaceholderText;
            }

            //_clearButton.IsEnabled = !IsReadOnly && hasImage;
        }

        private void EnableDragDrop()
        {
            DragDrop.SetAllowDrop(this, true);

            this.AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
            this.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
            this.AddHandler(DragDrop.DropEvent, OnDrop);
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (IsReadOnly) return;

            if (e.Data.Contains(DataFormats.Text))
            {
                try
                {
                    var jsonData = e.Data.Get(DataFormats.Text) as string;
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        var fileEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<FileSelectionEvent>(jsonData);

                        if (IsValidImageFile(fileEvent.FileExtension))
                        {
                            ShowDropIndicator(_imageContainer);
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

        private void OnDragLeave(object sender, DragEventArgs e)
        {
            HideDropIndicator();
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            HideDropIndicator();

            if (IsReadOnly) return;

            if (e.Data.Contains(DataFormats.Text))
            {
                try
                {
                    var jsonData = e.Data.Get(DataFormats.Text) as string;
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        var fileEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<FileSelectionEvent>(jsonData);
                        SetImage(fileEvent);
                    }
                }
                catch (Exception ex)
                {
                    Status.SetStatus($"Ошибка при обработке файла: {ex.Message}");
                }
            }

            e.Handled = true;
        }

        public void SetImage(FileSelectionEvent fileEvent)
        {
            if (fileEvent != null)
            {
                if (IsValidImageFile(fileEvent.FileExtension))
                {
                    ImagePath = fileEvent.FileFullPath;
                    ImageChanged?.Invoke(this, ImagePath);
                    Status.SetStatus($"Изображение выбрано: {fileEvent.FileName}");
                }
                else
                {
                    Status.SetStatus($"Неподдерживаемый тип файла: {fileEvent.FileExtension}");
                }
            }
        }

        public void SetImage(string imagePath)
        {
            if (!string.IsNullOrEmpty(imagePath))
            {
                var extension = Path.GetExtension(imagePath);
                if (!string.IsNullOrEmpty(extension))
                {
                    if (IsValidImageFile(extension))
                    {
                        ImagePath = imagePath;
                        ImageChanged?.Invoke(this, ImagePath);
                        Status.SetStatus($"Изображение выбрано: {ImagePath}");
                    }
                    else
                    {
                        Status.SetStatus($"Неподдерживаемый тип файла: {extension}");
                    }
                }
                else
                {
                    Status.SetStatus($"Нераспознанный формат");
                }
            }
        }

        private void ShowDropIndicator(Control target)
        {
            if (_dropIndicator != null && target != null)
            {
                var bounds = target.Bounds;
                _dropIndicator.Width = bounds.Width;
                _dropIndicator.Height = bounds.Height;

                var position = target.TranslatePoint(new Point(0, 0), this);
                if (position.HasValue)
                {
                    Canvas.SetLeft(_dropIndicator, position.Value.X);
                    Canvas.SetTop(_dropIndicator, position.Value.Y);
                    _dropIndicator.IsVisible = true;
                }
            }
        }

        private void HideDropIndicator()
        {
            if (_dropIndicator != null)
            {
                _dropIndicator.IsVisible = false;
            }
        }
        private bool IsValidImageFile(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return false;

            if (AllowedExtensions == null || AllowedExtensions.Length == 0)
            {
                var defaultImageExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tga", ".tiff" };
                return Array.Exists(defaultImageExtensions, ext =>
                    extension.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
            }

            return Array.Exists(AllowedExtensions, ext =>
                extension.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }

        //private async void OnBrowseButtonClick(object sender, RoutedEventArgs e)
        //{
        //    if (IsReadOnly) return;

        //    var dialog = new OpenFileDialog
        //    {
        //        AllowMultiple = false,
        //        Title = "Выберите изображение"
        //    };

        //    // Настраиваем фильтры по расширениям
        //    if (AllowedExtensions != null && AllowedExtensions.Length > 0)
        //    {
        //        dialog.Filters.Add(new FileDialogFilter
        //        {
        //            Name = "Поддерживаемые форматы",
        //            Extensions = Array.ConvertAll(AllowedExtensions, ext => ext.TrimStart('.'))
        //        });
        //    }
        //    else
        //    {
        //        dialog.Filters.Add(new FileDialogFilter
        //        {
        //            Name = "Изображения",
        //            Extensions = new System.Collections.Generic.List<string> { "png", "jpg", "jpeg", "bmp", "gif", "tga", "tiff" }
        //        });
        //    }

        //    var result = await dialog.ShowAsync(
        //        Window.GetTopLevel(this) as Window);

        //    if (result != null && result.Length > 0)
        //    {
        //        ImagePath = result[0];
        //        ImageChanged?.Invoke(this, ImagePath);
        //        Status.SetStatus($"Изображение выбрано: {Path.GetFileName(ImagePath)}");
        //    }
        //}

        public void Clear()
        {
            if (IsReadOnly) return;

            ImagePath = null;
            ImageChanged?.Invoke(this, null);
            UpdateImagePreview();
        }

        private void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            Clear();
        }
    }
}