using AtomEngine;
using Avalonia.Controls;
using Avalonia.Layout;
using Silk.NET.Assimp;
using System.Collections.Generic;
using System.Linq;

namespace Editor
{
    internal class ModelInspectable : IInspectable
    {
        private ModelMetadata _modelMetadata;

        public ModelInspectable(ModelMetadata modelMetadata)
        {
            _modelMetadata = modelMetadata;
        }

        public string Title => $"Model";

        public IEnumerable<Control> GetCustomControls(Panel parent)
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical };
            var exportTexturesBtn = new Button
            {
                Content = "Export textures",
                Classes = { "inspectorActionButton" },
            };
            Command command = new Command(() =>
            {
                DebLogger.Info("Export textures");
            });
            exportTexturesBtn.Command = command;

            panel.Children.Add(exportTexturesBtn);
            yield return panel;
        }

        public IEnumerable<PropertyDescriptor> GetProperties()
        {
            yield return new PropertyDescriptor
            {
                Name = "Meshes",
                Type = typeof(IEnumerable<NodeModelData>),
                IsReadOnly = true,
                Value = _modelMetadata.MeshesData,
                OnValueChanged = value =>
                {
                    if (value is IEnumerable<NodeModelData> interfacedValue)
                    {
                        _modelMetadata.MeshesData = interfacedValue.ToList<NodeModelData>();
                    }
                }
            };
        }

        public void Update() { }
    }
}
