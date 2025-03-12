using System.Collections.Generic;
using Avalonia.Controls;
using Silk.NET.OpenGL;
using System.Linq;
using AtomEngine;
using System;

namespace Editor
{
    public class AssetMetadataInspectable : IInspectable
    {
        protected AssetMetadata _metadata;
        protected string _filePath;

        public AssetMetadataInspectable(AssetMetadata metaData)
        {
            _metadata = metaData;
            Update();
        }

        public AssetMetadataInspectable(string filePath)
        {
            _filePath = filePath;
            _metadata = ServiceHub.Get<MetadataManager>().GetMetadata(filePath);
        }

        public virtual string Title => $"Asset Metadata";

        public virtual IEnumerable<Control> GetCustomControls(Panel parent)
        {
            return null;
        }

        public virtual IEnumerable<PropertyDescriptor> GetProperties()
        {

            yield return new PropertyDescriptor
            {
                Name = "GUID",
                Type = typeof(String),
                Value = _metadata.Guid,
                IsReadOnly = true
            };

            yield return new PropertyDescriptor
            {
                Name = "Asset Type",
                Type = typeof(String),
                Value = _metadata.AssetType.ToString(),
                IsReadOnly = true
            };

            yield return new PropertyDescriptor
            {
                Name = "Version",
                Type = typeof(Int32),
                Value = _metadata.Version,
                IsReadOnly = true
            };

            yield return new PropertyDescriptor
            {
                Name = "Last Modified",
                Type = typeof(String),
                Value = _metadata.LastModified.ToString(),
                IsReadOnly = true
            };

            yield return new PropertyDescriptor
            {
                Name = "Tags",
                Type = typeof(String),
                Value = string.Join(", ", _metadata.Tags),
                OnValueChanged = (value) => {
                    var tags = ((string)value).Split(',')
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToList();

                    _metadata.Tags.Clear();
                    _metadata.Tags.AddRange(tags);

                    Save();
                }
            };

            yield return new PropertyDescriptor
            {
                Name = "Dependencies",
                Type = typeof(IEnumerable<string>),
                Value = _metadata.Dependencies,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    _metadata.Dependencies.Clear();
                    foreach (var el in (IEnumerable<string>)e)
                    {
                        _metadata.Dependencies.Add(el.ToString());
                    }

                    Save();
                }
            };

        }
        protected void Save()
        {
            if (_filePath == null) ServiceHub.Get<MetadataManager>().SaveMetadata(_metadata);
            else ServiceHub.Get<MetadataManager>().SaveMetadata(_filePath, _metadata);
        }
        public virtual void Update() { }
    }

    public class TextureMetadataInspectable : AssetMetadataInspectable
    {
        private TextureMetadata _metadata;
        public TextureMetadataInspectable(string filePath) : base(filePath) {  }

        internal TextureMetadataInspectable(TextureMetadata metadata) : base(metadata) {
            _metadata = metadata;
        }

        public override string Title => $"Texture Metadata";

        public override IEnumerable<Control> GetCustomControls(Panel parent)
        {
            return null;
        }

        public override IEnumerable<PropertyDescriptor> GetProperties()
        {
            var _baseProps = base.GetProperties();
            foreach (var prop in _baseProps)
            {
                yield return prop;
            }

            yield return new PropertyDescriptor
            {
                Name = "Generate Mipmaps",
                Type = typeof(bool),
                Value = _metadata.GenerateMipmaps,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    _metadata.GenerateMipmaps = (bool)e;

                    Save();
                }
            };

            yield return new PropertyDescriptor
            {
                Name = "sRGB",
                Type = typeof(bool),
                Value = _metadata.sRGB,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    _metadata.sRGB = (bool)e;

                    Save();
                }
            };

            yield return new PropertyDescriptor
            {
                Name = "Max Size",
                Type = typeof(int),
                Value = _metadata.MaxSize,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    int value = Convert.ToInt32(e);
                    _metadata.MaxSize = value;

                    Save();
                }
            };

            yield return new PropertyDescriptor
            {
                Name = "Min Mode",
                Type = typeof(Silk.NET.OpenGL.TextureMinFilter),
                Value = _metadata.MinFilter,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    DebLogger.Info(e);
                    _metadata.MinFilter = Enum.Parse< Silk.NET.OpenGL.TextureMinFilter>(e.ToString());

                    Save();
                }
            };
            yield return new PropertyDescriptor
            {
                Name = "Mag Mode",
                Type = typeof(Silk.NET.OpenGL.TextureMagFilter),
                Value = _metadata.MagFilter,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    DebLogger.Info(e);
                    _metadata.MagFilter = Enum.Parse<Silk.NET.OpenGL.TextureMagFilter>(e.ToString());

                    Save();
                }
            };


            yield return new PropertyDescriptor
            {
                Name = "Aniso Level",
                Type = typeof(int),
                Value = _metadata.AnisoLevel,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    int value = Convert.ToInt32(e);
                    _metadata.AnisoLevel = value;

                    Save();
                }
            };

            yield return new PropertyDescriptor
            {
                Name = "Wrap Mode",
                Type = typeof(Silk.NET.OpenGL.TextureWrapMode),
                Value = _metadata.WrapMode,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    _metadata.WrapMode = Enum.Parse<Silk.NET.OpenGL.TextureWrapMode>(e.ToString());
                    Save();
                }
            };

            yield return new PropertyDescriptor
            {
                Name = "Compression Format",
                Type = typeof(InternalFormat),
                Value = _metadata.CompressionFormat,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    _metadata.CompressionFormat = Enum.Parse<InternalFormat>(e.ToString());
                    Save();
                }
            };

            yield return new PropertyDescriptor
            {
                Name = "Texture Target",
                Type = typeof(TextureTarget),
                Value = _metadata.TextureTarget,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    _metadata.TextureTarget = Enum.Parse<TextureTarget>(e.ToString());
                    Save();
                }
            };

            yield return new PropertyDescriptor
            {
                Name = "Texture Type",
                Type = typeof(Silk.NET.Assimp.TextureType),
                Value = _metadata.TextureType,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    _metadata.TextureType = Enum.Parse<Silk.NET.Assimp.TextureType>(e.ToString());
                    Save();
                }
            };

            yield return new PropertyDescriptor
            {
                Name = "Compress Texture",
                Type = typeof(bool),
                Value = _metadata.CompressTexture,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    _metadata.CompressTexture = (bool)e;

                    Save();
                }
            };

            yield return new PropertyDescriptor
            {
                Name = "Compression Quality",
                Type = typeof(float),
                Value = _metadata.CompressionQuality,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    float value = Convert.ToSingle(e);
                    _metadata.CompressionQuality = value;

                    Save();
                }
            };

            yield return new PropertyDescriptor
            {
                Name = "Transparency",
                Type = typeof(bool),
                Value = _metadata.AlphaIsTransparency,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    _metadata.AlphaIsTransparency = (bool)e;

                    Save();
                }
            };

            yield return new PropertyDescriptor
            {
                Name = "Is Normal Map",
                Type = typeof(bool),
                Value = _metadata.IsNormalMap,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    _metadata.IsNormalMap = (bool)e;

                    Save();
                }
            };

            yield return new PropertyDescriptor
            {
                Name = "Is Sprite Sheet",
                Type = typeof(bool),
                Value = _metadata.IsSpriteSheet,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    _metadata.IsSpriteSheet = (bool)e;

                    Save();
                }
            };

            yield return new PropertyDescriptor
            {
                Name = "Sprite Pixels Per Unit",
                Type = typeof(int),
                Value = _metadata.SpritePixelsPerUnit,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    int value = Convert.ToInt32(e);
                    _metadata.SpritePixelsPerUnit = value;

                    Save();
                }
            };

            yield return new PropertyDescriptor
            {
                Name = "Generate Sprite Mesh",
                Type = typeof(bool),
                Value = _metadata.GenerateSpriteMesh,
                IsReadOnly = false,
                OnValueChanged = (e) =>
                {
                    _metadata.GenerateSpriteMesh = (bool)e;

                    Save();
                }
            };
        }
        public override void Update()
        {
            if (!string.IsNullOrWhiteSpace(_filePath))
                _metadata = (TextureMetadata)ServiceHub.Get<MetadataManager>().GetMetadata(_filePath);
        }
    }

    public class ShaderSourceInspectable : AssetMetadataInspectable
    {
        private ShaderSourceMetadata _metadata;
        public ShaderSourceInspectable(string filePath) : base(filePath) { }
        internal ShaderSourceInspectable(ShaderSourceMetadata metadata) : base(metadata)
        {
            _metadata = metadata;
        }

        public override string Title => $"Shader Metadata";

        public override IEnumerable<Control> GetCustomControls(Panel parent)
        {
            return null;
        }

        public override IEnumerable<PropertyDescriptor> GetProperties()
        {
            var baseProps =  base.GetProperties();
            foreach (var prop in baseProps) {
                yield return prop;
            }

            if (_metadata.IsGenerator)
            {
                yield return new PropertyDescriptor
                {
                    Name = "Auto Generation",
                    Type = typeof(bool),
                    Value = _metadata.AutoGeneration,
                    IsReadOnly = false,
                    OnValueChanged = (e) =>
                    {
                        _metadata.AutoGeneration = (bool)e;

                        Save();
                    },
                };

                yield return new PropertyDescriptor
                {
                    Name = "Generated Assets",
                    Type = typeof(IEnumerable<string>),
                    Value = _metadata.GeneratedAssets,
                    IsReadOnly = true,
                };
            }
        }

        public override void Update()
        {
            if (!string.IsNullOrWhiteSpace(_filePath))
                _metadata = (ShaderSourceMetadata)ServiceHub.Get<MetadataManager>().GetMetadata(_filePath);
        }
    }

    public class ScriptInspectable : AssetMetadataInspectable
    {
        private ScriptMetadata _metadata;
        public ScriptInspectable(string filePath) : base(filePath) { }
        internal ScriptInspectable(ScriptMetadata metadata) : base(metadata)
        {
            _metadata = metadata;
        }

        public override string Title => $"Script Metadata";

        public override IEnumerable<Control> GetCustomControls(Panel parent)
        {
            return null;
        }

        public override IEnumerable<PropertyDescriptor> GetProperties()
        {
            var baseProp = base.GetProperties();
            foreach (var prop in baseProp)
            {
                yield return prop;
            }

            yield return new PropertyDescriptor
            {
                Name = "Is Generated",
                Type = typeof(bool),
                Value = _metadata.IsGenerated,
                IsReadOnly = true
            };

            if (_metadata.IsGenerated)
            {
                yield return new PropertyDescriptor
                {
                    Name = "Source Asset Guid",
                    Type = typeof(String),
                    Value = _metadata.SourceAssetGuid,
                    IsReadOnly = true
                };
            }
        }

        public override void Update()
        {
            if (!string.IsNullOrWhiteSpace(_filePath))
                _metadata = (ScriptMetadata)ServiceHub.Get<MetadataManager>().GetMetadata(_filePath);
        }
    }
}
