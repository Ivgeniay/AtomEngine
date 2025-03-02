using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using System;

namespace Editor
{
    internal class InspectorController : Grid, IWindowed
    {
        private StackPanel _container;
        private ScrollViewer _scrollViewer;
        private IInspectable _currentInspectable;
        private bool isOpend = false;

        public Action<object> OnClose { get; set; }

        public InspectorController()
        {
            InitialializeUI();
        }

        private void InitialializeUI()
        {
            _container = new StackPanel
            {
                Classes = { "innerContainer" },
            };

            _scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            _scrollViewer.Content = _container;

            Children.Add(_scrollViewer);

            //EnableDropInInspector();
        }

        public void Inspect(IInspectable inspectable)
        {
            _currentInspectable = inspectable;
            if (isOpend) RefreshView();
            else Clean();
        }
        
        private void RefreshView()
        {
            Clean();

            if (_currentInspectable == null) return;

            // Заголовок
            _container.Children.Add(new TextBlock
            {
                Text = _currentInspectable.Title,
                Classes = { "inspectorTitle" },
                Margin = new Thickness(0, 0, 0, 5)
            });

            var _innerContainer = new StackPanel { 
                Classes = { "innerConteiner" } 
            };

            _container.Children.Add(_innerContainer);

            var customControls = _currentInspectable.GetCustomControls();
            if (customControls != null)
            {
                foreach (var control in customControls)
                {
                    _innerContainer.Children.Add(control);
                }
                _innerContainer.Children.Add(new Separator
                {
                    Margin = new Thickness(0, 5, 0, 5)
                });
            }

            foreach (var property in _currentInspectable.GetProperties())
            {
                var view = InspectorViewFactory.CreateView(property);
                _innerContainer.Children.Add(view.GetView());
            }
        }

        public void Open()
        {
            isOpend = true;
            if (_currentInspectable == null) return;
            Clean();
            RefreshView();
        }

        public void Close()
        {
            Clean();
            isOpend = false;
        }

        public void CleanInspected()
        {
            Clean();
            RefreshView();
        }

        private void Clean()
        {
            _container.Children.Clear();
        }

        private void EnableDropInInspector()
        {
            DragDrop.SetAllowDrop(this, true);

            var dropIndicator = new Border
            {
                BorderThickness = new Thickness(2),
                BorderBrush = new SolidColorBrush(Colors.DodgerBlue),
                Background = new SolidColorBrush(Color.FromArgb(50, 30, 144, 255)),
                IsVisible = false,
                IsHitTestVisible = false
            };

            this.Children.Add(dropIndicator);

            this.AddHandler(DragDrop.DragEnterEvent, (sender, e) =>
            {
                if (e.Data.Contains(DataFormats.Text))
                {
                    dropIndicator.Width = _scrollViewer.Bounds.Width;
                    dropIndicator.Height = _scrollViewer.Bounds.Height;
                    dropIndicator.IsVisible = true;
                    e.Handled = true;
                }
            });

            this.AddHandler(DragDrop.DragLeaveEvent, (sender, e) =>
            {
                dropIndicator.IsVisible = false;
                e.Handled = true;
            });

            this.AddHandler(DragDrop.DropEvent, (sender, e) =>
            {
                dropIndicator.IsVisible = false;

                if (e.Data.Contains(DataFormats.Text))
                {
                    try
                    {
                        var jsonData = e.Data.Get(DataFormats.Text) as string;
                        if (!string.IsNullOrEmpty(jsonData))
                        {
                            var fileEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<FileSelectionEvent>(jsonData);
                            var inspectable = InspectorDistributor.GetInspectable(fileEvent);

                            if (inspectable != null)
                            {
                                Inspect(inspectable);
                                Status.SetStatus($"Инспектирую: {fileEvent.FileName}");
                            }
                            else
                            {
                                Status.SetStatus($"Не могу инспектировать файл: {fileEvent.FileName}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Status.SetStatus($"Ошибка при обработке перетаскиваемого файла: {ex.Message}");
                    }
                }

                e.Handled = true;
            });
        }

        public void Dispose()
        {
            OnClose?.Invoke(this);
        }
    }
}
