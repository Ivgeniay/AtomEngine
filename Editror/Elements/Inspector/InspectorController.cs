using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using System;

namespace Editor
{
    internal class InspectorController : Grid, IWindowed
    {
        private StackPanel _container;
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
                Classes = {  }
                //Spacing = 2,
                //Margin = new Thickness(3)
            };

            var scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            scrollViewer.Content = _container;
            Children.Add(scrollViewer);
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

        public void Dispose()
        {
            OnClose?.Invoke(this);
        }
    }
}
