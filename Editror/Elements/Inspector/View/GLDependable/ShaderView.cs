using AtomEngine;
using AtomEngine.RenderEntity;
using Avalonia.Controls;
using Avalonia.Layout;
using Silk.NET.Core.Native;
using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace Editor
{
    internal class ShaderView : GLDependableViewBase
    {
        public ShaderView(PropertyDescriptor descriptor) : base(descriptor) { }

        private string? GettingStartValue()
        {
            FieldInfo targetField = null;
            object targetObject = null;

            var target = context.Component;
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
                    return ServiceHub.Get<MetadataManager>().GetPathByGuid((string)guid);
                }
            }
            return null;
        }

        public override Control GetView()
        {
            ObjectField objectField = new ObjectField();
            objectField.AllowedExtensions = new string[] { ".mat" };
            objectField.Label = descriptor.Name;

            EntityInspectorContext context = (EntityInspectorContext)descriptor.Context;

            string? resourseGuid = GettingGUID();
            if (resourseGuid != null)
            {
                objectField.ObjectPath = ServiceHub.Get<MetadataManager>().GetPathByGuid(resourseGuid);
            }
            else
            {
                objectField.ObjectPath = string.Empty;
            }

            SceneManager sceneManager = ServiceHub.Get<SceneManager>();
            objectField.ObjectChanged += (sender, e) =>
            {
                if (e != null)
                {
                    var metaData = ServiceHub.Get<MetadataManager>().LoadMetadata(e+".meta");
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

                if (context != null) sceneManager.ComponentChange(context.EntityId, context.Component);
            };
            return objectField;
        }

    }
}