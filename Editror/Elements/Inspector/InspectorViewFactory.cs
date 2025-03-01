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
            AddOrUpdateViewMappingFabric(typeof(System.Int32), (descriptor) => new IntegerView(descriptor));
            AddOrUpdateViewMappingFabric(typeof(System.Single), (descriptor) => new FloatView(descriptor));
            AddOrUpdateViewMappingFabric(typeof(System.String), (descriptor) => new StringView(descriptor));
            AddOrUpdateViewMappingFabric(typeof(System.Boolean), (descriptor) => new BooleanView(descriptor));
            AddOrUpdateViewMappingFabric(typeof(System.Numerics.Vector2), (descriptor) => new Vector2View(descriptor));
            AddOrUpdateViewMappingFabric(typeof(System.Numerics.Vector3), (descriptor) => new Vector3View(descriptor));
            AddOrUpdateViewMappingFabric(typeof(Silk.NET.Maths.Vector2D<float>), (descriptor) => new Vector2SilkView(descriptor));
            AddOrUpdateViewMappingFabric(typeof(Silk.NET.Maths.Vector3D<float>), (descriptor) => new Vector3SilkView(descriptor));
            AddOrUpdateViewMappingFabric(typeof(Editor.ComponentPropertiesView), (descriptor) => new ComponentPropertiesView(descriptor));

            RegisterEnumViewHandler();
            RegisterCollectionHandler();
            ShaderViewHandler();
            TextureViewHandler();
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

        private static void ShaderViewHandler()
        {
            Func<Type, bool> TypePredicate = type => type.IsAssignableTo(typeof(AtomEngine.RenderEntity.ShaderBase));
            Func<PropertyDescriptor, IInspectorView> fabric = descriptor => new ShaderView(descriptor);
            _viewCreatorPredicates.Add((TypePredicate, fabric));
        }

        private static void TextureViewHandler()
        {
            Func<Type, bool> TypePredicate = type => type.IsAssignableTo(typeof(OpenglLib.Texture));
            Func<PropertyDescriptor, IInspectorView> fabric = descriptor => new TextureView(descriptor);
            _viewCreatorPredicates.Add((TypePredicate, fabric));
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
