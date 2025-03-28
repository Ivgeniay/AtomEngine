using System.Text.RegularExpressions;
using EngineLib;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    public class Mat : Shader
    {
        public Mat(GL gl) : base(gl) {
        }

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
                var isLastPart = i == parts.Length - 1;

                if (IsArrayAccess(part))
                {
                    ProcessArrayPart(ref currentInstance, part, path, location, isLastPart);
                }
                else
                {
                    ProcessSimplePart(ref currentInstance, part, path, location, isLastPart);
                }

                if (currentInstance == null)
                    return;
            }
        }

        private bool IsArrayAccess(string part)
        {
            return Regex.IsMatch(part, @"(.*?)\[(\d+)\]");
        }

        private void ProcessArrayPart(ref object currentInstance, string part, string path, int location, bool isLastPart)
        {
            var match = Regex.Match(part, @"(.*?)\[(\d+)\]");
            var propertyName = match.Groups[1].Value;
            var index = int.Parse(match.Groups[2].Value);

            var property = currentInstance.GetType().GetProperty(propertyName);
            if (property == null)
            {
                currentInstance = null;
                return;
            }

            var value = property.GetValue(currentInstance);
            if (value == null)
            {
                currentInstance = null;
                return;
            }

            var propertyType = property.PropertyType;

            if (!propertyType.IsGenericType)
            {
                currentInstance = null;
                return;
            }

            var genericType = propertyType.GetGenericTypeDefinition();

            if (genericType == typeof(LocaleArray<>))
            {
                ProcessLocaleArray(ref currentInstance, propertyName, location, isLastPart);
            }
            else if (genericType == typeof(StructArray<>))
            {
                ProcessStructArray(ref currentInstance, value, propertyType, index);
            }
            else
            {
                currentInstance = null;
            }
        }

        private void ProcessLocaleArray(ref object currentInstance, string propertyName, int location, bool isLastPart)
        {
            if (isLastPart)
            {
                var locationProperty = currentInstance.GetType().GetProperty($"{propertyName}Location");
                if (locationProperty != null)
                {
                    locationProperty.SetValue(currentInstance, location);
                }
            }
            else
            {
                currentInstance = null;
            }
        }

        private void ProcessStructArray(ref object currentInstance, object arrayInstance, Type arrayType, int index)
        {
            var indexer = arrayType.GetProperty("Item");
            if (indexer != null)
            {
                try
                {
                    currentInstance = indexer.GetValue(arrayInstance, new object[] { index });
                }
                catch
                {
                    currentInstance = null;
                }
            }
            else
            {
                currentInstance = null;
            }
        }

        private void ProcessSimplePart(ref object currentInstance, string part, string path, int location, bool isLastPart)
        {
            var property = currentInstance.GetType().GetProperty(part);

            if (property == null)
            {
                if (isLastPart)
                {
                    var locationProperty = currentInstance.GetType().GetProperty(part + "Location");
                    if (locationProperty != null)
                    {
                        locationProperty.SetValue(currentInstance, location);
                    }
                }

                currentInstance = null;
                return;
            }

            var propertyType = property.PropertyType;

            if (typeof(CustomStruct).IsAssignableFrom(propertyType))
            {
                currentInstance = property.GetValue(currentInstance);
            }
            else if (isLastPart)
            {
                var locationProperty = currentInstance.GetType().GetProperty(part + "Location");
                if (locationProperty != null)
                {
                    locationProperty.SetValue(currentInstance, location);
                }
            }
            else
            {
                currentInstance = null;
            }
        }

        //private void ProcessUniformLocation(string path, int location)
        //{
        //    var parts = path.Split('.');
        //    object currentInstance = this;

        //    for (int i = 0; i < parts.Length; i++)
        //    {
        //        var part = parts[i];
        //        var arrayMatch = Regex.Match(part, @"(.*?)\[(\d+)\]");
        //        if (arrayMatch.Success)
        //        {
        //            var propertyName = arrayMatch.Groups[1].Value;
        //            var property = currentInstance.GetType().GetProperty(propertyName);
        //            if (property == null) return;

        //            var propertyType = property.PropertyType;
        //            var value = property.GetValue(currentInstance);

        //            if (propertyType.IsGenericType)
        //            {
        //                var genericType = propertyType.GetGenericTypeDefinition();
        //                if (genericType == typeof(LocaleArray<>))
        //                {
        //                    if (i == parts.Length - 1)
        //                    {
        //                        var locationProperty = currentInstance.GetType().GetProperty($"{propertyName}Location");
        //                        if (locationProperty != null)
        //                        {
        //                            locationProperty.SetValue(currentInstance, location);
        //                        }
        //                    }
        //                }
        //                else if (genericType == typeof(StructArray<>))
        //                {
        //                    var index = int.Parse(arrayMatch.Groups[2].Value);
        //                    var indexer = propertyType.GetProperty("Item");
        //                    currentInstance = indexer.GetValue(value, new object[] { index });
        //                }
        //            }
        //        }
        //        else
        //        {
        //            var property = currentInstance.GetType().GetProperty(part);
        //            if (property == null) return;

        //            var propertyType = property.PropertyType;
        //            if (typeof(CustomStruct).IsAssignableFrom(propertyType))
        //            {
        //                currentInstance = property.GetValue(currentInstance);
        //            }
        //            else
        //            {
        //                property = currentInstance.GetType().GetProperty(part + "Location");
        //                if (property != null)
        //                {
        //                    property.SetValue(currentInstance, location);
        //                }
        //            }
        //        }
        //    }
        //}

    }

}
