﻿using AtomEngine;
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
        public ShaderView(PropertyDescriptor descriptor) : base(descriptor) { }

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