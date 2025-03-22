using Editor.Elements.Inspector.View;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using AtomEngine;
using EngineLib;
using System;

namespace Editor
{
    public class InspectorViewFactory : IService
    {
        private readonly Dictionary<Type, Func<PropertyDescriptor, IInspectorView>> _viewCreators
            = new Dictionary<Type, Func<PropertyDescriptor, IInspectorView>>();
        private readonly Dictionary<Func<Type, bool>, Func<PropertyDescriptor, IInspectorView>> _viewCreatorPredicates
            = new Dictionary<Func<Type, bool>, Func<PropertyDescriptor, IInspectorView>>();

        public Task InitializeAsync()
        {
            return Task.Run(() =>
            {
                AddOrUpdateViewMappingFabric(typeof(System.Int32), (descriptor) => new IntegerView(descriptor));
                AddOrUpdateViewMappingFabric(typeof(System.UInt32), (descriptor) => new UIntegerView(descriptor));
                AddOrUpdateViewMappingFabric(typeof(System.Single), (descriptor) => new FloatView(descriptor));
                AddOrUpdateViewMappingFabric(typeof(System.String), (descriptor) => new StringView(descriptor));
                AddOrUpdateViewMappingFabric(typeof(System.Boolean), (descriptor) => new BooleanView(descriptor));
                AddOrUpdateViewMappingFabric(typeof(System.Numerics.Vector2), (descriptor) => new Vector2View(descriptor));
                AddOrUpdateViewMappingFabric(typeof(System.Numerics.Vector3), (descriptor) => new Vector3View(descriptor));
                AddOrUpdateViewMappingFabric(typeof(Silk.NET.Maths.Vector2D<float>), (descriptor) => new Vector2SilkView(descriptor));
                AddOrUpdateViewMappingFabric(typeof(Silk.NET.Maths.Vector3D<float>), (descriptor) => new Vector3SilkView(descriptor));
                AddOrUpdateViewMappingFabric(typeof(Editor.ComponentPropertiesView), (descriptor) => new ComponentPropertiesView(descriptor));

                AddOrUpdateViewFabric(
                    type => type.IsAssignableTo(typeof(OpenglLib.Material)),
                    descriptor => new MaterialView(descriptor)
                    );
                AddOrUpdateViewFabric(
                    type => typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string),
                    descriptor => new GenericCollectionView(descriptor)
                    );
                AddOrUpdateViewFabric(
                    type => type.IsEnum,
                    descriptor => new EnumView(descriptor)
                    );
                AddOrUpdateViewFabric(
                    type => type.IsAssignableTo(typeof(AtomEngine.RenderEntity.ShaderBase)),
                    descriptor => new ShaderView(descriptor)
                    );
                AddOrUpdateViewFabric(
                    type => type.IsAssignableTo(typeof(AtomEngine.RenderEntity.MeshBase)),
                    descriptor => new MeshView(descriptor)
                    );
                AddOrUpdateViewFabric(
                    type => type.IsAssignableTo(typeof(OpenglLib.Texture)),
                    descriptor => new TextureView(descriptor)
                    );
                AddOrUpdateViewFabric(
                    type => typeof(AtomEngine.IDataSerializable).IsAssignableFrom(type),
                    descriptor => new DataObjectView(descriptor)
                );

            });
        }

        public void AddOrUpdateViewMappingFabric(Type type, Func<PropertyDescriptor, IInspectorView> creator) =>
            _viewCreators[type] = creator;
        public void AddOrUpdateViewFabric(Func<Type, bool> typePredictor, Func<PropertyDescriptor, IInspectorView> creator) =>
            _viewCreatorPredicates[typePredictor] = creator;

        public IInspectorView CreateView(PropertyDescriptor descriptor)
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
