using AtomEngine;
using AtomEngine.RenderEntity;
using Avalonia.Controls;
using Avalonia.Layout;
using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

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

        private string? GettingStartValue()
        {
            FieldInfo targetField = null;
            object targetObject = null;

            //var target = descriptor.OnValueChanged.Target;
            //if (target != null)
            //{
            //    Type targetType = target.GetType();
            //    var componentField = targetType.GetField("component", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            //    if (componentField != null)
            //    {
            //        targetObject = componentField.GetValue(target);
            //        if (targetObject != null)
            //        {
            //            targetField = targetObject.GetType().GetField(descriptor.Name + "GUID", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            //        }
            //    }
            //}

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
                    return ServiceHub.Get<MaterialManager>().GetPath((string)guid);
                }
            }
            return null;
        }

        public override Control GetView()
        {
            ObjectField objectField = new ObjectField();
            objectField.AllowedExtensions = new string[] { ".mat" };
            objectField.Label = descriptor.Name;
            objectField.ObjectPath = Path.GetFileName(GettingStartValue()) ?? string.Empty;


            objectField.ObjectChanged += (sender, e) =>
            {
                if (e != null)
                {
                    var materialAsset = ServiceHub.Get<MaterialManager>().LoadMaterial(e);
                    descriptor.OnValueChanged?.Invoke(new GLValueRedirection()
                    {
                        Value = materialAsset.Guid,
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