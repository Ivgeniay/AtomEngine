using System.Collections.Generic;
using Avalonia.Controls;
using System.IO;
using System;
using Avalonia.Layout;
using AtomEngine;
using EngineLib;
using OpenglLib;

namespace Editor
{
    internal class MaterialInspectable : IInspectable
    {
        private MaterialAsset _materialAsset;
        private MaterialFactory _materialFactory;
        private EditorMaterialAssetManager _materialAssetManager;

        public MaterialInspectable(MaterialAsset material)
        {
            _materialAsset = material;

            _materialFactory = ServiceHub.Get<MaterialFactory>();
            _materialAssetManager = ServiceHub.Get<EditorMaterialAssetManager>();
        }

        public string Title => $"Material: {_materialAsset.Name}";

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
                Value = _materialAsset.Name,
                OnValueChanged = value => _materialAsset.Name = (string)value
            };

            yield return new PropertyDescriptor
            {
                Name = "Shader Representation",
                Type = typeof(string),
                Value = GetShaderDisplayName(),
                IsReadOnly = true
            };

            if (_materialAsset.UniformValues.Count > 0)
            {
                // Разделитель
                //yield return new PropertyDescriptor
                //{
                //    Name = "Uniforms",
                //    Type = typeof(SectionHeader),
                //    Value = null,
                //    IsReadOnly = true
                //};

                foreach (var pair in _materialAsset.UniformValues)
                {
                    yield return CreatePropertyDescriptorForUniform(pair.Key, pair.Value);
                }
            }

            if (_materialAsset.TextureReferences.Count > 0)
            {
                // Разделитель
                //yield return new PropertyDescriptor
                //{
                //    Name = "Textures",
                //    Type = typeof(SectionHeader),
                //    Value = null,
                //    IsReadOnly = true
                //};

                foreach (var pair in _materialAsset.TextureReferences)
                {
                    yield return CreatePropertyDescriptorForTexture(pair.Key, pair.Value);
                }
            }
        }

        private string GetShaderDisplayName()
        {
            if (string.IsNullOrEmpty(_materialAsset.ShaderRepresentationGuid))
                return "None";

            var metadata = ServiceHub.Get<EditorMetadataManager>().GetMetadataByGuid(_materialAsset.ShaderRepresentationGuid);
            if (metadata == null)
                return _materialAsset.ShaderRepresentationTypeName;

            string path = ServiceHub.Get<EditorMetadataManager>().GetPathByGuid(_materialAsset.ShaderRepresentationGuid);
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
                OnValueChanged = newValue =>
                {
                    _materialFactory.SetUniformValue(_materialAsset, name, newValue);
                    _materialAssetManager.SaveMaterialAsset(_materialAsset);
                }
            };
        }

        private PropertyDescriptor CreatePropertyDescriptorForTexture(string name, string textureGuid)
        {
            return new PropertyDescriptor
            {
                Name = $"{name} (Texture)",
                Type = typeof(OpenglLib.Texture), 
                Value = textureGuid,
                OnValueChanged = newValue =>
                {
                    _materialFactory.SetTexture(_materialAsset, name, (string)newValue);
                    _materialAssetManager.SaveMaterialAsset(_materialAsset);
                }
            };
        }
    
        public void Update() { }

    }

}
