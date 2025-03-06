using System.Reflection;
using System;

namespace Editor
{
    internal abstract class GLDependableViewBase : BasePropertyView
    {
        protected EntityInspectorContext context;
        protected GLDependableViewBase(PropertyDescriptor descriptor) : base(descriptor)
        {
            if (descriptor.Context != null && descriptor.Context is EntityInspectorContext context)
            {
                this.context = context;
            }
        }

        protected string? GettingGUID()
        {
            if (context == null) return null;

            FieldInfo targetField = null;
            object targetObject = null;

            var target = context.Component;
            if (target != null)
            {
                Type targetType = target.GetType();
                targetField = targetType.GetField(descriptor.Name + "GUID", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }

            if (targetField != null)
            {
                var guid = targetField.GetValue(target);
                if (guid != null)
                {
                    return guid.ToString();
                }
            }
            return null;
        }
    }
}
