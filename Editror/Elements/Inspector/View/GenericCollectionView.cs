using System.Collections.Generic;
using System.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using System.Linq;
using Avalonia;
using System;

namespace Editor
{
    internal class GenericCollectionView : BasePropertyView
    {
        public GenericCollectionView(PropertyDescriptor descriptor) : base(descriptor) { }

        public override Control GetView()
        {
            var mainPanel = new StackPanel { Spacing = 8 };
            var headerGrid = CreateBaseLayout();
            mainPanel.Children.Add(headerGrid);

            Type elementType = GetElementType(Descriptor.Type);
            if (elementType == null)
                return new TextBlock { Text = $"Unsupported collection type: {Descriptor.Type.Name}" };

            var itemsList = ConvertToObjectList(Descriptor.Value as IEnumerable);

            var itemsPanel = new StackPanel { Margin = new Thickness(10, 0, 0, 0), Spacing = 3 };
            RenderItems(itemsPanel, itemsList, elementType);
            mainPanel.Children.Add(itemsPanel);

            var addButton = new Button
            {
                Content = $"Add {GetFriendlyTypeName(elementType)}",
                HorizontalAlignment = HorizontalAlignment.Left,
                Classes = { "inspectorButton" },
                IsEnabled = !Descriptor.IsReadOnly,
                Margin = new Thickness(10, 5, 0, 0)
            };

            addButton.Click += (s, e) => {
                itemsList.Add(CreateDefaultValue(elementType));
                UpdateCollection(itemsList, elementType);
                RenderItems(itemsPanel, itemsList, elementType);
            };

            mainPanel.Children.Add(addButton);
            return mainPanel;
        }

        private void RenderItems(StackPanel panel, List<object> items, Type elementType)
        {
            panel.Children.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                int index = i;
                var item = items[i];

                var itemGrid = new Grid();
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var indexLabel = new TextBlock
                {
                    Text = $"[{i}]",
                    Classes = { "propertyLabel" },
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 5, 0)
                };

                var itemDescriptor = new PropertyDescriptor
                {
                    Name = string.Empty,
                    Type = elementType,
                    Value = item,
                    IsReadOnly = Descriptor.IsReadOnly,
                    OnValueChanged = newValue => {
                        items[index] = newValue;
                        UpdateCollection(items, elementType);
                    }
                };

                var itemControl = InspectorViewFactory.CreateView(itemDescriptor).GetView();

                var removeButton = new Button
                {
                    Content = "✕",
                    Width = 24,
                    Height = 24,
                    Classes = { "inspectorButton" },
                    IsEnabled = !Descriptor.IsReadOnly,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 0, 0, 0)
                };

                removeButton.Click += (s, e) => {
                    items.RemoveAt(index);
                    UpdateCollection(items, elementType);
                    RenderItems(panel, items, elementType);
                };

                Grid.SetColumn(indexLabel, 0);
                Grid.SetColumn(itemControl, 1);
                Grid.SetColumn(removeButton, 2);

                itemGrid.Children.Add(indexLabel);
                itemGrid.Children.Add(itemControl);
                itemGrid.Children.Add(removeButton);

                panel.Children.Add(itemGrid);
            }
        }

        private Type GetElementType(Type collectionType)
        {
            if (collectionType.IsGenericType)
                return collectionType.GetGenericArguments()[0];

            if (collectionType.IsArray)
                return collectionType.GetElementType();

            foreach (var iface in collectionType.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return iface.GetGenericArguments()[0];
            }

            return null;
        }

        private List<object> ConvertToObjectList(IEnumerable collection)
        {
            var result = new List<object>();
            if (collection != null)
            {
                foreach (var item in collection)
                    result.Add(item);
            }
            return result;
        }

        private void UpdateCollection(List<object> items, Type elementType)
        {
            object result;

            if (Descriptor.Type.IsArray)
            {
                Array array = Array.CreateInstance(elementType, items.Count);
                for (int i = 0; i < items.Count; i++)
                    array.SetValue(items[i], i);
                result = array;
            }
            else
            {
                Type listType = typeof(List<>).MakeGenericType(elementType);
                var typedList = Activator.CreateInstance(listType);
                var addMethod = listType.GetMethod("Add");

                foreach (var item in items)
                    addMethod.Invoke(typedList, new[] { item });

                result = typedList;
            }

            Descriptor.OnValueChanged?.Invoke(result);
        }

        private object CreateDefaultValue(Type type)
        {
            if (type == typeof(string))
                return string.Empty;
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            if (type.IsEnum)
                return Enum.GetValues(type).Cast<object>().FirstOrDefault();

            try
            {
                return Activator.CreateInstance(type);
            }
            catch
            {
                return null;
            }
        }

        private string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(int)) return "Integer";
            if (type == typeof(float)) return "Float";
            if (type == typeof(string)) return "String";
            return type.Name;
        }
    }
}
