using AtomEngine;
using AtomEngine.RenderEntity;
using Avalonia.Controls;
using Avalonia.Layout;
using System.IO;

namespace Editor
{
    internal class ShaderView : BasePropertyView
    {
        private readonly PropertyDescriptor _descriptor;
        private string _materialGuid;

        public ShaderView(PropertyDescriptor descriptor) : base(descriptor)
        {
            _descriptor = descriptor;

            if (descriptor.Value is ShaderBase material)
            {

            }
        }

        public override Control GetView()
        {
            var grid = CreateBaseLayout();

            var objField = new ObjectField();

            objField.Value = 

            objField.AllowedExtensions = new string[] { ".mat" };
            objField.ObjectChanged += (sender, e) =>
            {
                DebLogger.Debug(e);
                var materialAsset = ServiceHub.Get<MaterialManager>().LoadMaterial(e);
                if (materialAsset != null)
                {
                    _descriptor.OnValueChanged?.Invoke(new GLValueRedirection()
                    {
                        Value = materialAsset.Guid,
                    });
                }
            };

            Grid.SetColumn(objField, 1);
            grid.Children.Add(objField);

            return grid;
        }

    }
}