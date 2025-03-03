using System;

namespace Editor
{
    internal interface IWindowed : IDisposable { 
        public Action<object> OnClose { get; }
        public void Open();
        public void Close();
        public void Redraw();
    }
}
