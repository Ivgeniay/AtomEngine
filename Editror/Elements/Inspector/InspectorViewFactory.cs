using Editor.Elements.Inspector.View;
using System;
using System.Collections.Generic;

namespace Editor
{
    internal static class InspectorViewFactory
    {
        private static readonly Dictionary<Type, Func<PropertyDescriptor, IInspectorView>> _viewCreators
            = new Dictionary<Type, Func<PropertyDescriptor, IInspectorView>>();

        static InspectorViewFactory()
        {
            AddOrUpdateViewMappingFabric(typeof(Int32), (descriptor) => new IntegerView(descriptor));
            AddOrUpdateViewMappingFabric(typeof(Single), (descriptor) => new FloatView(descriptor));
            AddOrUpdateViewMappingFabric(typeof(String), (descriptor) => new StringView(descriptor));
            AddOrUpdateViewMappingFabric(typeof(Boolean), (descriptor) => new BooleanView(descriptor));
            AddOrUpdateViewMappingFabric(typeof(System.Numerics.Vector2), (descriptor) => new Vector2View(descriptor));
            AddOrUpdateViewMappingFabric(typeof(System.Numerics.Vector3), (descriptor) => new Vector3View(descriptor));
            AddOrUpdateViewMappingFabric(typeof(ComponentPropertiesView), (descriptor) => new ComponentPropertiesView(descriptor));
        }


        public static void AddOrUpdateViewMappingFabric(Type type, Func<PropertyDescriptor, IInspectorView> creator)
        {
            _viewCreators[type] = creator;
        }

        public static IInspectorView CreateView(PropertyDescriptor descriptor)
        {
            if (_viewCreators.TryGetValue(descriptor.Type, out var creator)) return creator(descriptor);
            return new UnsupportedTypeView(descriptor);
        }
    }
}
