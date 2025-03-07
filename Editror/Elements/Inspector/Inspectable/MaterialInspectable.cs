using System.Collections.Generic;
using Avalonia.Controls;
using System.IO;
using System;

namespace Editor
{
    internal class MaterialInspectable : IInspectable
    {
        private MaterialAsset _material;

        public MaterialInspectable(MaterialAsset material)
        {
            _material = material;
        }

        public string Title => $"Material: {_material.Name}";

        public IEnumerable<Control> GetCustomControls(Panel parent)
        {
            return null;
        }

        public IEnumerable<PropertyDescriptor> GetProperties()
        {
            yield return new PropertyDescriptor
            {
                Name = "Name",
                Type = typeof(string),
                Value = _material.Name,
                OnValueChanged = value => _material.Name = (string)value
            };

            yield return new PropertyDescriptor
            {
                Name = "Shader Representation",
                Type = typeof(string),
                Value = GetShaderDisplayName(),
                IsReadOnly = true
            };

            if (_material.UniformValues.Count > 0)
            {
                // Разделитель
                //yield return new PropertyDescriptor
                //{
                //    Name = "Uniforms",
                //    Type = typeof(SectionHeader),
                //    Value = null,
                //    IsReadOnly = true
                //};

                foreach (var pair in _material.UniformValues)
                {
                    yield return CreatePropertyDescriptorForUniform(pair.Key, pair.Value);
                }
            }

            if (_material.TextureReferences.Count > 0)
            {
                // Разделитель
                //yield return new PropertyDescriptor
                //{
                //    Name = "Textures",
                //    Type = typeof(SectionHeader),
                //    Value = null,
                //    IsReadOnly = true
                //};

                foreach (var pair in _material.TextureReferences)
                {
                    yield return CreatePropertyDescriptorForTexture(pair.Key, pair.Value);
                }
            }
        }

        private string GetShaderDisplayName()
        {
            if (string.IsNullOrEmpty(_material.ShaderRepresentationGuid))
                return "None";

            var metadata = ServiceHub.Get<MetadataManager>().GetMetadataByGuid(_material.ShaderRepresentationGuid);
            if (metadata == null)
                return _material.ShaderRepresentationTypeName;

            string path = ServiceHub.Get<MetadataManager>().GetPathByGuid(_material.ShaderRepresentationGuid);
            return Path.GetFileNameWithoutExtension(path);
        }

        private PropertyDescriptor CreatePropertyDescriptorForUniform(string name, object value)
        {
            Type type = value?.GetType() ?? typeof(object);

            return new PropertyDescriptor
            {
                Name = name,
                Type = type,
                Value = value,
                IsReadOnly = false,
                OnValueChanged = newValue => _material.SetUniformValue(name, newValue)
            };
        }

        private PropertyDescriptor CreatePropertyDescriptorForTexture(string name, string textureGuid)
        {
            return new PropertyDescriptor
            {
                Name = $"{name} (Texture)",
                Type = typeof(OpenglLib.Texture), 
                Value = textureGuid,
                OnValueChanged = newValue => _material.SetTexture(name, (string)newValue)
            };
        }
    
    }

}
