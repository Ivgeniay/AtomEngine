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
        public IEnumerable<PropertyDescriptor> CreateDescriptors(IComponent component)
        {
            var type = component.GetType();

            var members = type
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m is FieldInfo);
                //.Where(m => m is PropertyInfo || m is FieldInfo);

            foreach (var member in members)
            {
                if (member.GetCustomAttribute<NonSerializedAttribute>() != null 
                    || member.GetCustomAttribute<JsonIgnoreAttribute>() != null
                    || member.GetCustomAttribute<HideInInspectorAttribute>() != null
                    )
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
}
