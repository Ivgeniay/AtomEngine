﻿using System.Text.RegularExpressions;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    public class Mat : Shader
    {
        public Mat(GL gl) : base(gl) { }

        protected void SetTexture(string textureUnit, string texTarget, int location, int index, Texture texture)
        {
            TextureUnit unit = Enum.Parse<TextureUnit>(textureUnit);
            TextureTarget target = Enum.Parse<TextureTarget>(texTarget);

            texture.Target = target;
            texture.Bind(unit);
            int textureIndex = (int)(unit - TextureUnit.Texture0);
            _gl.Uniform1(location, textureIndex);
        }

        protected void SetupUniformLocations()
        {
            foreach (var uniform in _uniformLocations)
            {
                if (uniform.Value > -1) ProcessUniformLocation(uniform.Key, uniform.Value); 
            }
        }

        private void ProcessUniformLocation(string path, int location)
        {
            var parts = path.Split('.');
            object currentInstance = this;

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var arrayMatch = Regex.Match(part, @"(.*?)\[(\d+)\]");
                if (arrayMatch.Success)
                {
                    var propertyName = arrayMatch.Groups[1].Value;
                    var property = currentInstance.GetType().GetProperty(propertyName);
                    if (property == null) return;

                    var propertyType = property.PropertyType;
                    var value = property.GetValue(currentInstance);

                    if (propertyType.IsGenericType)
                    {
                        var genericType = propertyType.GetGenericTypeDefinition();
                        if (genericType == typeof(LocaleArray<>))
                        {
                            if (i == parts.Length - 1)
                            {
                                var locationProperty = currentInstance.GetType().GetProperty($"{propertyName}Location");
                                if (locationProperty != null)
                                {
                                    locationProperty.SetValue(currentInstance, location);
                                }
                            }
                        }
                        else if (genericType == typeof(StructArray<>))
                        {
                            var index = int.Parse(arrayMatch.Groups[2].Value);
                            var indexer = propertyType.GetProperty("Item");
                            currentInstance = indexer.GetValue(value, new object[] { index });
                        }
                    }
                }
                else
                {
                    var property = currentInstance.GetType().GetProperty(part);
                    if (property == null) return;

                    var propertyType = property.PropertyType;
                    if (typeof(CustomStruct).IsAssignableFrom(propertyType))
                    {
                        currentInstance = property.GetValue(currentInstance);
                    }
                    else
                    {
                        property = currentInstance.GetType().GetProperty(part + "Location");
                        if (property != null)
                        {
                            property.SetValue(currentInstance, location);
                        }
                    }
                }
            }
        }

    }
}
