using Avalonia.Controls;
using System;

namespace Editor
{
    public interface IInspectorView : IDisposable
    {
        Control GetView();
    }
}
