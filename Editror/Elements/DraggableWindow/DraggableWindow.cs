using Avalonia;
using Avalonia.Controls;
using System;

namespace Editor
{
    internal class DraggableWindow : Border, IWindowed
    {
        public Action<object> OnClose { get; set; }
        public void Dispose()
        {
            OnClose?.Invoke(this);
        }
    }
}
