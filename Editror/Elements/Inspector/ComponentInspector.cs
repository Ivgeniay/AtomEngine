using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AtomEngine;

namespace Editor
{
    public class ComponentInspector
    {
        public IEnumerable<PropertyDescriptor> CreateDescriptors(IComponent component)
        {
            var type = component.GetType();

            // Создаем дескриптор для самого компонента (например его тип и enabled/disabled)
            yield return new PropertyDescriptor
            {
                Name = "Type",
                Type = "String",
                Value = type.Name,
                IsReadOnly = true
            };

            // Получаем все public properties и fields
            var members = type
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m is FieldInfo);
                //.Where(m => m is PropertyInfo || m is FieldInfo);

            foreach (var member in members)
            {
                if (member.GetCustomAttribute<NonSerializedAttribute>() != null 
                    //|| member.GetCustomAttribute<HideInInspectorAttribute>() != null
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
            if (value == null) return null;

            return new PropertyDescriptor
            {
                Name = member.Name,
                Type = GetMemberType(member).Name,
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
                    // Конвертируем значение в правильный тип
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
