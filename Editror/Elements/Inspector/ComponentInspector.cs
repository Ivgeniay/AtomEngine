using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;
using System;

namespace Editor
{
    public class ComponentInspector
    {
        private Dictionary<IComponent, IEnumerable<MemberInfo>> _componentMap = new Dictionary<IComponent, IEnumerable<MemberInfo>>();
        private Dictionary<IComponent, bool> _isGlDependableMap = new Dictionary<IComponent, bool>();

        public IEnumerable<PropertyDescriptor> CreateDescriptors(IComponent component)
        {
            var type = component.GetType();

            _isGlDependableMap[component] = type.CustomAttributes.Any(e => e.AttributeType == typeof(GLDependableAttribute));

            var members = type
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m =>
                {
                    if (m is FieldInfo field)
                    {
                        if (!_isGlDependableMap[component] & GLDependableTypes.IsDependableType(field.FieldType))
                        {
                            return false;
                        }

                        if (m.Name.IndexOf("k__", StringComparison.Ordinal) != -1)
                        {
                            return false;
                        }

                        return true;
                    }
                    return false;
                });
            //.Where(m => m is PropertyInfo || m is FieldInfo);
            _componentMap[component] = members;

            foreach (var member in members)
            {
                if (member.GetCustomAttribute<NonSerializedAttribute>() != null
                    || member.GetCustomAttribute<JsonIgnoreAttribute>() != null
                    || member.GetCustomAttribute<HideInInspectorAttribute>() != null
                    )
                    continue;

                if (member is FieldInfo field && 
                    field.Name.EndsWith("GUID") && 
                    field.IsPrivate && 
                    field.FieldType == typeof(string))
                        continue;

                var descriptor = CreateDescriptorForMember(component, member);
                if (descriptor != null)
                    yield return descriptor;
            }
        }

        private PropertyDescriptor CreateDescriptorForMember(IComponent component, MemberInfo member)
        {
            var value = GetValue(component, member);
            var memberType = GetMemberType(member);

            bool allowNull = memberType == typeof(string)
                    || memberType.IsClass
                    || (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(Nullable<>));

            if (value == null && !allowNull) return null;

            return new PropertyDescriptor
            {
                Context = component,
                Name = member.Name,
                Type = memberType,
                Value = value,
                IsReadOnly = IsReadOnly(member),
                OnValueChanged = newValue => SetValue(component, member, newValue)
            };
        }

        private bool IsReadOnly(MemberInfo member)
        {
            return member switch
            {
                PropertyInfo prop => !prop.CanWrite,
                FieldInfo field => field.IsInitOnly || field.IsLiteral,
                _ => true
            };
        }

        private Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                PropertyInfo prop => prop.PropertyType,
                FieldInfo field => field.FieldType,
                _ => typeof(object)
            };
        }

        private object GetValue(IComponent component, MemberInfo member)
        {
            return member switch
            {
                PropertyInfo prop => prop.GetValue(component),
                FieldInfo field => field.GetValue(component),
                _ => null
            };
        }

        private void SetValue(IComponent component, MemberInfo member, object value)
        {
            if (value is GLValueRedirection redirection)
            {
                var guidMember = _componentMap[component].FirstOrDefault(m => m.Name == member.Name + "GUID");
                if (guidMember != null)
                {
                    var val = GetValue(component, guidMember);
                    DebLogger.Debug($"{guidMember.Name} Before: {val}");
                    SetValue(component, guidMember, redirection.Value);
                    val = GetValue(component, guidMember);
                    DebLogger.Debug($"{guidMember.Name} After: {val}");
                }
                return;
            }

            switch (member)
            {
                case PropertyInfo prop:
                    var convertedValue = Convert.ChangeType(value, prop.PropertyType);
                    prop.SetValue(component, convertedValue);
                    break;
                case FieldInfo field:
                    var fieldValue = Convert.ChangeType(value, field.FieldType);
                    field.SetValue(component, fieldValue);
                    break;
            }
        }
    }

    public class GLValueRedirection
    {
        public object Value { get; set; }
    }
}
