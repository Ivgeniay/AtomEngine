using System.Collections.Generic;
using Avalonia.Controls;
using System.Linq;
using AtomEngine;

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

        public IEnumerable<Control> GetCustomControls()
        {
            return null;
        }

        public IEnumerable<PropertyDescriptor> GetProperties()
        {
            foreach (var component in _components)
            {
                yield return new PropertyDescriptor
                {
                    Name = $"Component: {component.GetType().Name}",
                    Type = typeof(ComponentPropertiesView),
                    Value = _componentInspector.CreateDescriptors(component).ToList()
                };
            }
        }
    }
}
