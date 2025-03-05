using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using System.Linq;
using AtomEngine;
using EngineLib;
using System;

namespace Editor
{
    public class EntityInspectable : IInspectable
    {
        private readonly Entity _entity;
        private readonly ComponentInspector _componentInspector;
        private readonly IEnumerable<IComponent> _components;

        public EntityInspectable(Entity entity, IEnumerable<IComponent> compoonents)
        {
            _componentInspector = new ComponentInspector();
            _components = compoonents;
            _entity = entity;
        }

        public string Title => $"Entity ID:{_entity.Id}";

        public IEnumerable<Control> GetCustomControls(Panel parent)
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical };
            var addComponentButton = new Button
            {
                Content = "Add Component",
                Classes = { "inspectorActionButton" },
            };

            Command command = new Command(() =>
            {
                DebLogger.Debug($"Add Component at {_entity}");

                List<SearchPopupItem> popUpItems = new List<SearchPopupItem>();
                ComponentService cs = ServiceHub.Get<ComponentService>();

                IEnumerable<Type> componentTypes = cs.GetComponentTypes();
                foreach(var componentType in componentTypes ) 
                {
                    bool isContinue = false;
                    foreach(var component in _components)
                    {
                        if (component.GetType() == componentType)
                        {
                            isContinue = true;
                            break;
                        }
                    }
                    if (isContinue) continue;

                    TooltipCategoryComponentAttribute tCategoryAtribute =
                        componentType.GetCustomAttributes(false)
                            .OfType<TooltipCategoryComponentAttribute>()
                            .FirstOrDefault();

                    popUpItems.Add(
                        new SearchPopupItem(componentType.Name, componentType)
                        {
                            Category = tCategoryAtribute == null ? ComponentCategory.Other.ToString() : tCategoryAtribute.ComponentCategory.ToString(),
                        });
                }
                popUpItems.Sort(new SearchPopupItemCategoryComparer());

                ComponentSearchDialog searchDialog = new ComponentSearchDialog(popUpItems);

                searchDialog.ItemSelected += (selectedValue) =>
                {
                    DebLogger.Debug($"Выбран элемент: {selectedValue}");
                    ServiceHub.Get<SceneManager>().AddComponent(_entity.Id, (Type)selectedValue);
                };

                searchDialog.Closed += (s, e) =>
                {
                    var rootCanvas = MainWindow.MainCanvas_;
                    if (rootCanvas != null && rootCanvas.Children.Contains(searchDialog))
                    {
                        rootCanvas.Children.Remove(searchDialog);
                    }
                };

                var rootCanvas = MainWindow.MainCanvas_;
                if (rootCanvas != null)
                {
                    var existingDialogs = rootCanvas.Children.OfType<ComponentSearchDialog>().ToList();
                    foreach (var dlg in existingDialogs)
                    {
                        rootCanvas.Children.Remove(dlg);
                    }

                    rootCanvas.Children.Add(searchDialog);
                    searchDialog.Show(addComponentButton);
                }
                else
                {
                    DebLogger.Error("Не удалось найти корневой Canvas для отображения диалога");
                }
            });

            addComponentButton.Command = command;
            panel.Children.Add(addComponentButton);
            yield return panel;
        }

        public IEnumerable<PropertyDescriptor> GetProperties()
        {
            foreach (var component in _components)
            {
                var context = new EntityInspectorContext()
                {
                    EntityId = _entity.Id,
                    Component = component,
                };
                yield return new PropertyDescriptor
                {
                    Name = $"Component: {component.GetType().Name}",
                    Type = typeof(ComponentPropertiesView),
                    Value = _componentInspector.CreateDescriptors(component, context).ToList(),
                    Context = context,
                };
            }
        }
    }
}
