using System;
using System.Collections.Generic;

namespace Editor
{
    internal static class InspectorViewFactory
    {
        private static readonly Dictionary<string, Func<PropertyDescriptor, IInspectorView>> _viewCreators
            = new Dictionary<string, Func<PropertyDescriptor, IInspectorView>>();

        static InspectorViewFactory()
        {
            RegisterDefaultViews();
        }

        private static void RegisterDefaultViews()
        {
            Register("Int32", (descriptor) => new IntegerView(descriptor));
            Register("Single", (descriptor) => new FloatView(descriptor));
            Register("String", (descriptor) => new StringView(descriptor));
            Register("Boolean", (descriptor) => new BooleanView(descriptor));
            Register("ComponentProperties", (descriptor) => new ComponentPropertiesView(descriptor));
        }

        public static void Register(string type, Func<PropertyDescriptor, IInspectorView> creator)
        {
            _viewCreators[type] = creator;
        }

        public static IInspectorView CreateView(PropertyDescriptor descriptor)
        {
            if (_viewCreators.TryGetValue(descriptor.Type, out var creator))
            {
                return creator(descriptor);
            }
            return new UnsupportedTypeView(descriptor);
        }
    }
}
