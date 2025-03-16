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
        private uint _entityId;
        private ComponentInspector _componentInspector;
        private IEnumerable<IComponent> _components;
        private SceneManager _sceneManager;

        public EntityInspectable(uint entityId)
        {
            this._entityId = entityId;
            _sceneManager = ServiceHub.Get<SceneManager>();
            Update();
        }

        public EntityInspectable(Entity entity, IEnumerable<IComponent> compoonents)
        {
            _componentInspector = new ComponentInspector();
            _components = compoonents;
            _entityId = entity.Id;
        }

        public string Title => $"Entity ID:{_entityId}";

        public IEnumerable<Control> GetCustomControls(Panel parent)
        {
            if (_entityId == uint.MaxValue) 
                yield break;

            var panel = new StackPanel { Orientation = Orientation.Vertical };
            var addComponentButton = new Button
            {
                Content = "Add Component",
                Classes = { "inspectorActionButton" },
            };

            Command command = new Command(() =>
            {
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

                    var attributes = componentType.GetCustomAttributes(false);
                    var hideInSearch = attributes.Any(e => e.GetType() == typeof(HideInspectorSearchAttribute));
                    if (hideInSearch) continue;

                    TooltipCategoryComponentAttribute tCategoryAtribute =
                        attributes
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
                    ServiceHub.Get<SceneManager>().AddComponent(_entityId, (Type)selectedValue);
                };

                searchDialog.Closed += (s, e) =>
                {
                    var rootCanvas = MainWindow.MainCanvas_;
                    if (rootCanvas != null && rootCanvas.Children.Contains(searchDialog))
                    {
                        rootCanvas.Children.Remove(searchDialog);
                    }
                };

                searchDialog.Show(addComponentButton);
            });

            addComponentButton.Command = command;
            panel.Children.Add(addComponentButton);

            yield return panel;
        }

        public IEnumerable<PropertyDescriptor> GetProperties()
        {
            foreach (var component in _components)
            {
                bool isHideToInspector = component
                    .GetType()
                    .GetCustomAttributes(false)
                    .Any(e => e.GetType() == typeof(HideToInspectorAttribute));
                if (isHideToInspector) continue;

                var context = new EntityInspectorContext()
                {
                    EntityId = _entityId,
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

        public void Update()
        {
            _componentInspector = new ComponentInspector();

            var entityData = _sceneManager.CurrentScene.CurrentWorldData.Entities.Where(e => e.Id == _entityId).FirstOrDefault();
            if (entityData != null)
            {
                _entityId = entityData.Id;
                _components = entityData.Components.Values.ToList();
                if (_components == null) _components = new List<IComponent>();
            }
            else
            {
                _entityId = uint.MaxValue;
                _components = new List<IComponent>();
            }
        }
    }
}
