using System.Reflection;
using Newtonsoft.Json;
using Silk.NET.OpenGL;
using AtomEngine;
using Silk.NET.Maths;
using System.Text.RegularExpressions;

namespace OpenglLib
{
    public class Mat : Shader
    {

        public Mat(GL gl) : base(gl)
        {
        }

        protected void SetLocation()
        {

            var type = GetType();
            ProcessSimpleUniforms(type);
            ProcessArrayUniforms(type);
            ProcessStructureUniforms(type);

            //var filteredCustomClasses = fiels.Where(e => e.FieldType.ins)


            DebLogger.Info(JsonConvert.SerializeObject(this));
        }
        
        private void ProcessSimpleUniforms(Type type)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Default | BindingFlags.SetField);
            var filteredProps = props.Where(e => _uniformLocations.ContainsKey(e.Name));

            foreach (var prop in filteredProps)
            {
                if (_uniformLocations.TryGetValue(prop.Name, out int location))
                {
                    var locationProp = type.GetProperty(prop.Name + "Location", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.Public | BindingFlags.GetField | BindingFlags.SetField);
                    if (locationProp != null) locationProp.SetValue(this, location);
                }
            }
        }
        private void ProcessArrayUniforms(Type type)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Default | BindingFlags.SetField);
            var filteredFields = fields.Where(e => _uniformLocations.ContainsKey(e.Name + "[0]"));

            foreach (var field in filteredFields)
            {
                if (_uniformLocations.TryGetValue(field.Name + "[0]", out int location))
                {
                    var locationProp = type.GetProperty(field.Name + "Location", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.Public | BindingFlags.GetField | BindingFlags.SetField);
                    if (locationProp != null) locationProp.SetValue(this, location);
                }
            }
        }

        private void ProcessStructureUniforms(Type type)
        {
            var properties = type.GetProperties(AllBindings);
            var structureFields = _uniformLocations
                .Where(kvp => kvp.Key.Contains('.'))
                .GroupBy(kvp => kvp.Key.Split('.')[0])
                .ToList();

            foreach (var structGroup in structureFields)
            {
                var structName = structGroup.Key;
                var structProperty = properties.FirstOrDefault(f => f.Name == structName);

                if (structProperty != null)
                {
                    ProcessStructureProperty(structProperty, structGroup, "", this);
                }
            }
        }

        private void ProcessStructureProperty(PropertyInfo property, IEnumerable<KeyValuePair<string, int>> uniformFields, string parentPath, object instance)
        {
            var structType = property.PropertyType;
            var structInstance = property.GetValue(instance);  // Используем переданный instance
            var prefix = parentPath == "" ? property.Name : parentPath;

            var currentFields = uniformFields
                .Where(kv => kv.Key.StartsWith(prefix + "."))
                .ToList();

            var groupedFields = currentFields
                .GroupBy(kv =>
                {
                    var path = kv.Key.Substring(prefix.Length + 1);
                    var nextDot = path.IndexOf('.');
                    return nextDot == -1 ? path : path.Substring(0, nextDot);
                })
                .ToArray();

            foreach (var group in groupedFields)
            {
                var fieldName = group.Key;
                var arrayMatch = Regex.Match(fieldName, @"(\w+)\[(\d+)\]");

                if (arrayMatch.Success)
                {
                    var arrayPropName = arrayMatch.Groups[1].Value;
                    // Ищем свойство массива в структуре
                    var arrayLocation = structType.GetProperty(arrayPropName + "Location");
                    if (arrayLocation != null)
                    {
                        var location = group.First().Value;
                        arrayLocation.SetValue(structInstance, location);
                    }
                }
                else
                {
                    var _property = structType.GetProperty(fieldName);
                    if (_property != null)
                    {
                        if (IsCustomStructType(_property.PropertyType))
                        {
                            // Передаем текущий structInstance для вложенной структуры
                            ProcessStructureProperty(_property, uniformFields, $"{prefix}.{fieldName}", structInstance);
                        }
                        else
                        {
                            var locationProp = structType.GetProperty(fieldName + "Location", AllBindings);
                            if (locationProp != null)
                            {
                                var location = group.First().Value;
                                locationProp.SetValue(structInstance, location);
                            }
                        }
                    }
                }
            }
        }




        private static readonly BindingFlags AllBindings = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default |
                                                            BindingFlags.Public | BindingFlags.GetField | BindingFlags.SetField;
        private bool IsCustomStructType(Type type) => typeof(CustomStruct).IsAssignableFrom(type);
    }
}
