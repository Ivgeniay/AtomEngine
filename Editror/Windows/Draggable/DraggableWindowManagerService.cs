using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Controls;
using System.Linq;
using EngineLib;
using System;

namespace Editor
{
    internal class DraggableWindowManagerService : IService, IDisposable
    {
        private DraggableWindowManager _dManager;
        private bool _scenViewIsOpen = false;

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public void SetCanvas(Canvas canvas) => _dManager = new DraggableWindowManager(canvas);

        public void RegisterController(MainControllers name, Control controller) =>
            _dManager.RegisterController(name, controller);
        public void RegisterOpenHandler(MainControllers type, Action<Control> handler) =>
            _dManager.RegisterOpenHandler(type, handler);
        public void RegisterCloseHandler(MainControllers type, Action<Control> handler) =>
            _dManager.RegisterCloseHandler(type,handler);

        public void CloseWindow(MainControllers type)
        {
            if (type == MainControllers.SceneRender) _scenViewIsOpen = false;
            _dManager.CloseWindow(type);
        }

        public DraggableWindow OpenWindow(MainControllers type, double left = 10, double top = 10, double width = 250, double height = 400)
        {
            if (type == MainControllers.SceneRender) _scenViewIsOpen = true;
            return _dManager.OpenWindow(type, left, top, width, height);
        }

        public DraggableWindow CreateWindow(string title, Control content = null, double left = 10, double top = 10, double width = 200, double height = 150) =>
            _dManager.CreateWindow(title, content, left, top, width, height);

        public void Dispose()
        {
            _dManager?.Dispose();
        }

        internal void OpenStartedWindow()
        {
            var opened = _dManager.OpenStartedWindow();
            if (opened.Any(e => e == MainControllers.SceneRender)) _scenViewIsOpen = true;
        }

        public void Upload()
        {
            Dispatcher.UIThread.Invoke(new Action(() =>
            {
                if (_scenViewIsOpen)
                {
                    OpenWindow(MainControllers.SceneRender);
                }
            }));
        }

        public void Unload()
        {
            Dispatcher.UIThread.Invoke(new Action(() =>
            {
                if (_scenViewIsOpen)
                {
                    _dManager.CloseWindow(MainControllers.SceneRender);
                }
            }));
        }
    }
}