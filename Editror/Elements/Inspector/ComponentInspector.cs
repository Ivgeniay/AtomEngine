using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;
using System;
using OpenglLib;

namespace Editor
{
    public class ComponentInspector
    {
        private Dictionary<IComponent, IEnumerable<MemberInfo>> _componentMap = new Dictionary<IComponent, IEnumerable<MemberInfo>>();
        private Dictionary<IComponent, bool> _isGlDependableMap = new Dictionary<IComponent, bool>();
        private Dictionary<IComponent, EntityInspectorContext> _contexMap = new Dictionary<IComponent, EntityInspectorContext>();

        public IEnumerable<PropertyDescriptor> CreateDescriptors(IComponent component, EntityInspectorContext context)
        {
            var type = component.GetType();

            _isGlDependableMap[component] = type.CustomAttributes.Any(e => e.AttributeType == typeof(GLDependableAttribute));

            var members = type
                .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m =>
                {
                    if (m is FieldInfo field)
                    {
                        if (field.IsPrivate)
                        {
                            if (field.Name.EndsWith("GUID")) return true;
                            if (field.Name.EndsWith("InternalIndex")) return true;

                            bool isShowInInspector = field.GetCustomAttributes(false).Any(e => e.GetType() == typeof(ShowInInspectorAttribute));
                            if (isShowInInspector)
                            {
                                return true;
                            }
                            return false;
                        }

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
            _componentMap[component] = members;
            _contexMap[component] = context;

            foreach (var member in members)
            {
                if (member.GetCustomAttribute<NonSerializedAttribute>() != null
                    || member.GetCustomAttribute<JsonIgnoreAttribute>() != null
                    || member.GetCustomAttribute<HideInInspectorAttribute>() != null
                    )
                    continue;

                //if (member is FieldInfo field && 
                //    field.Name.EndsWith("GUID") && 
                //    field.IsPrivate && 
                //    field.FieldType == typeof(string))
                //        continue;

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
                Context = _contexMap[component],
                Name = member.Name,
                Type = memberType,
                Value = value,
                IsReadOnly = IsReadOnly(member),
                OnValueChanged = newValue => SetValue(component, member, newValue)
            };
        }

        private bool IsReadOnly(MemberInfo member)
        {
            var isReadOnlyAttribute = member.GetCustomAttribute<ReadOnlyAttribute>() != null;
            if (isReadOnlyAttribute) return true;

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
                string findingFiledGUID = member.Name + "GUID";
                string findingFiledIndexator = member.Name + "InternalIndex";
                var guidMember = _componentMap[component].FirstOrDefault(m => m.Name == findingFiledGUID);
                var indexatorMember = _componentMap[component].FirstOrDefault(m => m.Name == findingFiledIndexator);
                if (guidMember != null)
                {
                    SetValue(component, guidMember, redirection.GUID);
                }
                else
                {
                    DebLogger.Error("No GUID field");
                }
                if (guidMember != null)
                {
                    SetValue(component, indexatorMember, redirection.Indexator);
                }
                else
                {
                    DebLogger.Error("No GUID field");
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
        public string GUID { get; set; } = string.Empty;
        public string Indexator { get; set; } = string.Empty;
    }
}
