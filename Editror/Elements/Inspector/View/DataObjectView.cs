using System.Collections.Generic;
using Avalonia.Controls;
using System.Reflection;
using System.Linq;
using AtomEngine;
using EngineLib;
using System;

namespace Editor
{
    internal class DataObjectView : BasePropertyView
    {
        private InspectorViewFactory _inspectorViewFactory;
        private object _dataObject;

        public DataObjectView(PropertyDescriptor descriptor) : base(descriptor)
        {
            _inspectorViewFactory = ServiceHub.Get<InspectorViewFactory>();
            _dataObject = descriptor.Value;
        }

        public override Control GetView()
        {
            if (_dataObject == null || !(_dataObject is IDataSerializable))
            {
                return new TextBlock
                {
                    Text = "Объект не реализует интерфейс IDataSerializable или null"
                };
            }

            var panel = new StackPanel
            {
                Spacing = 4,
                Margin = new Avalonia.Thickness(0, 5, 0, 5)
            };

            var headerPanel = CreateBaseLayout();
            panel.Children.Add(headerPanel);
            var properties = GetObjectProperties(_dataObject);

            foreach (var property in properties)
            {
                var view = _inspectorViewFactory.CreateView(property);
                panel.Children.Add(view.GetView());
            }

            return panel;
        }

        private IEnumerable<PropertyDescriptor> GetObjectProperties(object obj)
        {
            if (obj == null) yield break;

            var type = obj.GetType();

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsInitOnly && !f.IsLiteral);

            foreach (var field in fields)
            {
                if (field.GetCustomAttribute<HideInInspectorAttribute>() != null)
                    continue;

                bool isReadOnly = field.GetCustomAttribute<ReadOnlyAttribute>() != null;

                yield return new PropertyDescriptor
                {
                    Name = field.Name,
                    Type = field.FieldType,
                    Value = field.GetValue(obj),
                    IsReadOnly = isReadOnly,
                    OnValueChanged = value =>
                    {
                        try
                        {
                            field.SetValue(obj, Convert.ChangeType(value, field.FieldType));
                        }
                        catch (Exception ex)
                        {
                            DebLogger.Error($"Ошибка при установке значения поля {field.Name}: {ex.Message}");
                        }
                    }
                };
            }
        }
    }
}
