using System;

namespace Editor
{
    internal interface IWindowed : IDisposable { 
        public Action<object> OnClose { get; }
    }
}
