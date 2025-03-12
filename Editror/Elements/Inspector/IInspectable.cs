using Avalonia.Controls;
using System.Collections.Generic;

namespace Editor
{
    public interface IInspectable
    {
        string Title { get; }
        IEnumerable<Control> GetCustomControls(Panel parent);
        IEnumerable<PropertyDescriptor> GetProperties();
        void Update();
    }
}
