using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using EngineLib;
using System.Reflection;

namespace Editor
{
    internal abstract class BasePropertyView : IInspectorView
    {
        protected readonly PropertyDescriptor descriptor;

        protected BasePropertyView(PropertyDescriptor descriptor)
        {
            this.descriptor = descriptor;
        }

        public abstract Control GetView();

        protected Grid CreateBaseLayout()
        {
            var grid = new Grid
            {
                Margin = new Thickness(4, 0),
                ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(120) },
                        new ColumnDefinition { Width = GridLength.Star }
                    }
            };

            var label = new TextBlock
            {
                Text = descriptor.Name,
                Classes = { "propertyLabel" }
            };
            Grid.SetColumn(label, 0);
            grid.Children.Add(label);

            return grid;
        }


        protected bool IsSupportDirtyField(object context)
        {
            if (context is FieldInfo field) return IsSupportDirtyField((FieldInfo)context);
            return false;
        }
        protected bool IsSupportDirtyField(FieldInfo fieldInfo) =>
            fieldInfo.GetCustomAttribute<SupportDirtyAttribute>() == null;


    }
}
