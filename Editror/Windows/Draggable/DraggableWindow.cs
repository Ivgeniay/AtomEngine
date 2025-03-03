using Avalonia.Controls;
using Avalonia;
using System;

namespace Editor
{
    internal class DraggableWindow : Border, IWindowed
    {
        public Action<object> OnClose { get; set; }
        public Action<DraggableWindow, Vector> OnPositionChange { get; set; }

        public void Close()
        {
            OnClose?.Invoke(this);
        }

        public void Dispose()
        {
        }

        public void Open()
        {
        }

        public void Redraw()
        {
        }
    }
}
