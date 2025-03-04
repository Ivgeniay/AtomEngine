using Avalonia.Controls;
using System;
using System.IO;
using System.Reflection;

namespace Editor
{
    internal class MeshView : BasePropertyView
    {
        public MeshView(PropertyDescriptor descriptor) : base(descriptor) { }

        private string? GettingStartValue()
        {
            FieldInfo targetField = null;
            object targetObject = null;

            var target = descriptor.Context;
            if (target != null)
            {
                Type targetType = target.GetType();
                targetField = targetType.GetField(descriptor.Name + "GUID", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }

            if (targetField != null)
            {
                var guid = targetField.GetValue(target);
                if (guid != null)
                {
                    return ServiceHub.Get<MeshManager>().GetPath((string)guid);
                }
            }
            return null;
        }

        public override Control GetView()
        {
            ObjectField objectField = new ObjectField();
            objectField.AllowedExtensions = new string[] { ".obj" };
            objectField.Label = descriptor.Name;
            objectField.ObjectPath = Path.GetFileName(GettingStartValue()) ?? string.Empty;


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
            };
            return objectField;
        }
    }
}
