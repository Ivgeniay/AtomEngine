using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using System.IO;
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

        public string Title => $"Material for {_materialAsset.ShaderRepresentationTypeName}";

        public IEnumerable<Control> GetCustomControls(Panel parent)
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical };
            var addComponentButton = new Button
            {
                Content = "Select Shader",
                Classes = { "inspectorActionButton" },
            };

            Command command = new Command(() =>
            {
                List<SearchPopupItem> popUpItems = new List<SearchPopupItem>();
                var metaService = ServiceHub.Get<EditorMetadataManager>();

                var shaders = metaService.FindAssetsByType(MetadataType.Shader);

                foreach (var shader in shaders)
                {
                    var name = Path.GetFileName(shader);
                    popUpItems.Add(new SearchPopupItem(name, shader));
                }

                ComponentSearchDialog searchDialog = new ComponentSearchDialog(popUpItems);

                searchDialog.ItemSelected += (selectedValue) =>
                {
                    var metadata = metaService.GetMetadata(selectedValue as string);
                    if (metadata != null && metadata is ShaderMetadata shaderMeta)
                    {
                        _materialAssetManager.AssignShaderToMaterial(_materialAsset, metadata.Guid);
                    }
                };

                searchDialog.Closed += (s, e) =>
                {
                    var rootCanvas = MainWindow.MainCanvas_;
                    if (rootCanvas != null && rootCanvas.Children.Contains(searchDialog))
                    {
                        rootCanvas.Children.Remove(searchDialog);
                    }
                };

                searchDialog.Show(addComponentButton);
            });

            addComponentButton.Command = command;
            panel.Children.Add(addComponentButton);

            yield return panel;
        }

        public IEnumerable<PropertyDescriptor> GetProperties()
        {
            yield return new PropertyDescriptor
            {
                Name = "Shader Representation",
                Type = typeof(string),
                Value = GetShaderDisplayName(),
                IsReadOnly = true
            };

            // Получаем все контейнеры и обрабатываем их
            var containers = _materialAsset.GetAllContainers();
            foreach (var container in containers)
            {
                foreach (var descriptor in CreatePropertyDescriptorsForContainer(container, ""))
                {
                    yield return descriptor;
                }
            }
        }

        private IEnumerable<PropertyDescriptor> CreatePropertyDescriptorsForContainer(
            MaterialDataContainer container, string parentPath)
        {
            string currentPath = string.IsNullOrEmpty(parentPath)
                ? container.Name
                : $"{parentPath}.{container.Name}";

            string displayName = string.IsNullOrEmpty(parentPath)
                ? container.Name
                : currentPath;

            if (container is MaterialUniformDataContainer uniformContainer)
            {
                yield return CreatePropertyDescriptorForUniform(uniformContainer, currentPath, displayName);
            }
            else if (container is MaterialSamplerDataContainer samplerContainer)
            {
                yield return CreatePropertyDescriptorForTexture(samplerContainer, currentPath, displayName);
            }
            else if (container is MaterialSamplerArrayDataContainer samplerArrayContainer)
            {
                for (int i = 0; i < samplerArrayContainer.TextureGuids.Count; i++)
                {
                    int capturedIndex = i;
                    string arrayIndexName = $"{container.Name}[{i}]";
                    string indexPath = string.IsNullOrEmpty(parentPath)
                        ? arrayIndexName
                        : $"{parentPath}.{arrayIndexName}";

                    yield return new PropertyDescriptor
                    {
                        Name = $"{indexPath} (Texture)",
                        Type = typeof(OpenglLib.Texture),
                        Value = samplerArrayContainer.TextureGuids[i],
                        OnValueChanged = newValue =>
                        {
                            string textureGuid = (string)newValue;
                            if (capturedIndex < samplerArrayContainer.TextureGuids.Count)
                            {
                                samplerArrayContainer.TextureGuids[capturedIndex] = textureGuid;
                                _materialFactory.SetTexture(_materialAsset, indexPath, textureGuid);
                                _materialAssetManager.SaveMaterialAsset(_materialAsset);
                            }
                        }
                    };
                }
            }
            else if (container is MaterialArrayDataContainer arrayContainer)
            {
                for (int i = 0; i < arrayContainer.Values.Count; i++)
                {
                    int capturedIndex = i;
                    string arrayIndexName = $"{container.Name}[{i}]";
                    string indexPath = string.IsNullOrEmpty(parentPath)
                        ? arrayIndexName
                        : $"{parentPath}.{arrayIndexName}";

                    yield return new PropertyDescriptor
                    {
                        Name = indexPath,
                        Type = arrayContainer.ElementType,
                        Value = arrayContainer.Values[i],
                        OnValueChanged = newValue =>
                        {
                            if (capturedIndex < arrayContainer.Values.Count)
                            {
                                arrayContainer.Values[capturedIndex] = newValue;
                                _materialFactory.SetUniformValue(_materialAsset, indexPath, newValue);
                                _materialAssetManager.SaveMaterialAsset(_materialAsset);
                            }
                        }
                    };
                }
            }
            else if (container is MaterialStructDataContainer structContainer)
            {
                foreach (var field in structContainer.Fields)
                {
                    foreach (var descriptor in CreatePropertyDescriptorsForContainer(field, currentPath))
                    {
                        yield return descriptor;
                    }
                }
            }
            else if (container is MaterialStructArrayDataContainer structArrayContainer)
            {
                for (int i = 0; i < structArrayContainer.Elements.Count; i++)
                {
                    string arrayIndexName = $"{container.Name}[{i}]";
                    string elementPath = string.IsNullOrEmpty(parentPath)
                        ? arrayIndexName
                        : $"{parentPath}.{arrayIndexName}";

                    var element = structArrayContainer.Elements[i];
                    foreach (var field in element.Fields)
                    {
                        foreach (var descriptor in CreatePropertyDescriptorsForContainer(field, elementPath))
                        {
                            yield return descriptor;
                        }
                    }
                }
            }
        }

        private PropertyDescriptor CreatePropertyDescriptorForUniform(
            MaterialUniformDataContainer container, string path, string displayName)
        {
            return new PropertyDescriptor
            {
                Name = displayName,
                Type = container.Type,
                Value = container.Value,
                IsReadOnly = false,
                OnValueChanged = newValue =>
                {
                    container.Value = newValue;
                    _materialFactory.SetUniformValue(_materialAsset, path, newValue);
                    _materialAssetManager.SaveMaterialAsset(_materialAsset);
                }
            };
        }

        private PropertyDescriptor CreatePropertyDescriptorForTexture(
            MaterialSamplerDataContainer container, string path, string displayName)
        {
            return new PropertyDescriptor
            {
                Name = $"{displayName} (Texture)",
                Type = typeof(OpenglLib.Texture),
                Value = container.TextureGuid,
                OnValueChanged = newValue =>
                {
                    string textureGuid = (string)newValue;
                    container.TextureGuid = textureGuid;
                    _materialFactory.SetTexture(_materialAsset, path, textureGuid);
                    _materialAssetManager.SaveMaterialAsset(_materialAsset);
                }
            };
        }

        private string GetShaderDisplayName()
        {
            if (string.IsNullOrEmpty(_materialAsset.ShaderGuid))
                return "None";

            var metadata = ServiceHub.Get<EditorMetadataManager>().GetMetadataByGuid(_materialAsset.ShaderGuid);
            if (metadata == null)
                return _materialAsset.ShaderRepresentationTypeName;

            string path = ServiceHub.Get<EditorMetadataManager>().GetPathByGuid(_materialAsset.ShaderGuid);
            return Path.GetFileNameWithoutExtension(path);
        }

        public void Update() { }
    }

}
