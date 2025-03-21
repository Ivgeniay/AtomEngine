using System.Collections.Generic;
using Avalonia.Controls;
using System.Linq;
using System;
using EngineLib;

namespace Editor
{
    internal class DraggableWindowManager : IDisposable
    {
        private readonly Canvas _mainCanvas;
        private readonly DraggableWindowFactory _windowFactory;
        private readonly Dictionary<MainControllers, DraggableWindow> _borderMap = new Dictionary<MainControllers, DraggableWindow>();
        private readonly Dictionary<MainControllers, Control> _controllers = new Dictionary<MainControllers, Control>();
        private readonly Dictionary<MainControllers, Action<Control>> _openHandlers = new Dictionary<MainControllers, Action<Control>>();
        private readonly Dictionary<MainControllers, Action<Control>> _closeHandlers = new Dictionary<MainControllers, Action<Control>>();
        private WindowManagerConfiguration _config;

        public DraggableWindowManager(Canvas mainCanvas)
        {
            _mainCanvas = mainCanvas ?? throw new ArgumentNullException(nameof(mainCanvas));
            _windowFactory = new DraggableWindowFactory(_mainCanvas);
            _config = ServiceHub.Get<Configuration>().GetConfiguration<WindowManagerConfiguration>(ConfigurationSource.WindowManagerConfigs);
        }

        public void RegisterController(MainControllers name, Control controller)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            _controllers[name] = controller;
        }
        public void RegisterOpenHandler(MainControllers type, Action<Control> handler)
        {
            _openHandlers[type] = handler;
        }
        public void RegisterCloseHandler(MainControllers type, Action<Control> handler)
        {
            _closeHandlers[type] = handler;
        }

        internal void CloseWindow(MainControllers type)
        {
            _closeHandlers[type](_controllers[type]);
            _windowFactory.CloseWindow(_borderMap[type]);
        }

        public DraggableWindow OpenWindow(MainControllers type, double left = 10, double top = 10, double width = 250, double height = 400)
        {
            if (!_controllers.TryGetValue(type, out var controller))
            {
                Status.SetStatus($"Контроллер {type} не зарегистрирован");
                return null;
            }
            WindowConfiguration config = _config.Configurations.Where(e => e.Key == type).FirstOrDefault().Value;
            if (config !=null && config.IsOpen)
            {
                if (_borderMap.TryGetValue(type, out DraggableWindow existWindow))
                {
                    existWindow.Focus();
                    return existWindow;
                }
            }

            string windowName = type.ToString();
            double left_ = left;
            double top_ = top;
            double width_ = width;
            double height_ = height;
            if (config != null)
            {
                left_ = config.Left;
                top_ = config.Top;
                width_ = config.Width;
                height_ = config.Height;
                windowName = config.Title;
            }
            else
            {
                config = new WindowConfiguration
                {
                    Title = windowName,
                    Width = width_,
                    Height = height_,
                    Left = left_,
                    Top = top_,
                };
                _config.Configurations.Add(type, config);
            }
            config.IsOpen = true;
            var window = _windowFactory.CreateWindow(windowName, controller, left_, top_, width_, height_);

            
            window.SizeChanged += (sender, e) =>
            {
                config.Width = e.NewSize.Width;
                config.Height = e.NewSize.Height;
            };
            window.OnPositionChange += (sender, e) =>
            {
                config.Left = e.X;
                config.Top = e.Y;
            };

            if (_openHandlers.TryGetValue(type, out var openHandler))
            {
                openHandler(controller);
            }

            window.OnClose += (sender) =>
            {
                if (_closeHandlers.TryGetValue(type, out var closeHandler))
                {
                    closeHandler(controller);
                }
                config.IsOpen = false;
            };

            _borderMap[type] = window;
            return window;
        }

        public DraggableWindow CreateWindow(string title, Control content = null, double left = 10, double top = 10, double width = 200, double height = 150)
        {
            return _windowFactory.CreateWindow(title, content, left, top, width, height);
        }

        public void Dispose()
        {
            ServiceHub.Get<Configuration>().SafeConfiguration(ConfigurationSource.WindowManagerConfigs, _config);
        }

        internal IEnumerable<MainControllers> OpenStartedWindow()
        {
            List<MainControllers> mainControllers = new List<MainControllers>();

            foreach (var type in _controllers)
            {
                var pair = _config.Configurations.FirstOrDefault(e => e.Key == type.Key);
                if (pair.Value != null && pair.Value.IsOpen)
                {
                    var conf = pair.Value;
                    OpenWindow(type.Key, conf.Left, conf.Top, conf.Width, conf.Height);
                    mainControllers.Add(pair.Key);
                }
            }

            return mainControllers;
        }
    }
}