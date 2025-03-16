using Avalonia.Controls;
using Silk.NET.Core.Native;
using System;
using System.IO;
using System.Reflection;

namespace Editor
{
    internal class MeshView : GLDependableViewBase
    {
        public MeshView(PropertyDescriptor descriptor) : base(descriptor) { }


        public override Control GetView()
        {
            ObjectField objectField = new ObjectField();
            objectField.AllowedExtensions = new string[] { ".obj" };
            objectField.Label = descriptor.Name;

            string? meshGuid = GettingGUID();
            if (!string.IsNullOrWhiteSpace(meshGuid))
            {
                objectField.ObjectPath = ServiceHub.Get<MeshManager>().GetPath(meshGuid);
            }
            else
            {
                objectField.ObjectPath = string.Empty;
            }
            objectField.IsEnabled = !descriptor.IsReadOnly;

            SceneManager sceneManager = ServiceHub.Get<SceneManager>();
            objectField.ObjectChanged += (sender, e) =>
            {
                if (e != null)
                {
                    var metaData = ServiceHub.Get<MetadataManager>().GetMetadata(e);
                    descriptor.OnValueChanged?.Invoke(new GLValueRedirection()
                    {
                        Value = metaData.Guid,
                    });
                }
                else
                {
                    descriptor.OnValueChanged?.Invoke(new GLValueRedirection()
                    {
                        Value = string.Empty,
                    });
                }

                //if (context != null) sceneManager.ComponentChange(context.EntityId, context.Component);
            };
            return objectField;
        }
    }
}
