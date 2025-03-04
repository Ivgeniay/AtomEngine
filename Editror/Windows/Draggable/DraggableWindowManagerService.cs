using Avalonia.Controls;
using System;
using System.Threading.Tasks;

namespace Editor
{
    internal class DraggableWindowManagerService : IService, IDisposable
    {
        private DraggableWindowManager _dManager;
        public Task InitializeAsync() => Task.CompletedTask;
        public void SetCanvas(Canvas canvas) => _dManager = new DraggableWindowManager(canvas);

        public void RegisterController(MainControllers name, Control controller) =>
            _dManager.RegisterController(name, controller);
        public void RegisterOpenHandler(MainControllers type, Action<Control> handler) =>
            _dManager.RegisterOpenHandler(type, handler);
        public void RegisterCloseHandler(MainControllers type, Action<Control> handler) =>
            _dManager.RegisterCloseHandler(type,handler);
        public DraggableWindow OpenWindow(MainControllers type, double left = 10, double top = 10, double width = 250, double height = 400) =>
            _dManager.OpenWindow(type, left, top, width, height);
        public DraggableWindow CreateWindow(string title, Control content = null, double left = 10, double top = 10, double width = 200, double height = 150) =>
            _dManager.CreateWindow(title, content, left, top, width, height);

        public void Dispose()
        {
            _dManager?.Dispose();
        }

        internal void OpenStartedWindow()
        {
            _dManager.OpenStartedWindow();
        }
    }
}