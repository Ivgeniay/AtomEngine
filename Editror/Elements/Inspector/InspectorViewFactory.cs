using Editor.Elements.Inspector.View;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Editor
{
    internal static class InspectorViewFactory
    {
        private static readonly Dictionary<Type, Func<PropertyDescriptor, IInspectorView>> _viewCreators
            = new Dictionary<Type, Func<PropertyDescriptor, IInspectorView>>();

        private static readonly List<(Func<Type, bool> TypePredicate, Func<PropertyDescriptor, IInspectorView> Creator)>
            _viewCreatorPredicates = new List<(Func<Type, bool> TypePredicate, Func<PropertyDescriptor, IInspectorView> Creator)>();

        static InspectorViewFactory()
        {
            AddOrUpdateViewMappingFabric(typeof(Int32), (descriptor) => new IntegerView(descriptor));
            AddOrUpdateViewMappingFabric(typeof(Single), (descriptor) => new FloatView(descriptor));
            AddOrUpdateViewMappingFabric(typeof(String), (descriptor) => new StringView(descriptor));
            AddOrUpdateViewMappingFabric(typeof(Boolean), (descriptor) => new BooleanView(descriptor));
            AddOrUpdateViewMappingFabric(typeof(System.Numerics.Vector2), (descriptor) => new Vector2View(descriptor));
            AddOrUpdateViewMappingFabric(typeof(System.Numerics.Vector3), (descriptor) => new Vector3View(descriptor));
            AddOrUpdateViewMappingFabric(typeof(ComponentPropertiesView), (descriptor) => new ComponentPropertiesView(descriptor));
            RegisterEnumViewHandler();
            RegisterCollectionHandler();
        }

        private static void RegisterCollectionHandler()
        {
            Func<Type, bool> TypePredicate = type => typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
            Func<PropertyDescriptor, IInspectorView> fabric = descriptor => new GenericCollectionView(descriptor);
            _viewCreatorPredicates.Add((TypePredicate, fabric));
        }

        private static void RegisterEnumViewHandler()
        {
            _viewCreatorPredicates.Add((type => type.IsEnum, descriptor => new EnumView(descriptor)));
        }

        public static void AddOrUpdateViewMappingFabric(Type type, Func<PropertyDescriptor, IInspectorView> creator)
        {
            _viewCreators[type] = creator;
        }

        public static IInspectorView CreateView(PropertyDescriptor descriptor)
        {
            foreach (var (predicate, fabric) in _viewCreatorPredicates)
            {
                if (predicate(descriptor.Type))
                {
                    return fabric(descriptor);
                }
            }

            if (_viewCreators.TryGetValue(descriptor.Type, out var creator)) return creator(descriptor);
            return new UnsupportedTypeView(descriptor);
        }
    }
}
