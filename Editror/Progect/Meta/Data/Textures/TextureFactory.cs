using System.Collections.Generic;
using Silk.NET.OpenGL;
using AtomEngine;
using System.IO;
using System;

using Texture = OpenglLib.Texture;
using System.Threading.Tasks;

namespace Editor
{
    internal class TextureFactory : IService, IDisposable
    {
        private Dictionary<string, Texture> _cacheTexture = new Dictionary<string, Texture>();

        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates a texture from a GUID with configuration from its metadata
        /// </summary>
        internal Texture CreateTextureFromGuid(GL gl, string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                throw new ArgumentException("Texture GUID cannot be null or empty", nameof(guid));
            }

            string texturePath = ServiceHub.Get<MetadataManager>().GetPathByGuid(guid);
            if (string.IsNullOrEmpty(texturePath) || !File.Exists(texturePath))
            {
                DebLogger.Error($"Texture file not found for GUID: {guid}");
                return null;
            }

            var metadata = ServiceHub.Get<MetadataManager>().GetMetadata(texturePath) as TextureMetadata;
            return CreateTextureFromPath(gl, texturePath, metadata);
        }

        /// <summary>
        /// Creates a texture from a file path with optional metadata configuration
        /// </summary>
        internal Texture CreateTextureFromPath(GL gl, string texturePath, TextureMetadata metadata = null)
        {
            if (_cacheTexture.TryGetValue(texturePath, out Texture cacheTexture)) { return cacheTexture; }

            try
            {
                var texture = new Texture(gl, texturePath, Silk.NET.Assimp.TextureType.Diffuse);

                if (metadata != null)
                {
                    var minFilter = metadata.GenerateMipmaps ? TextureMinFilter.NearestMipmapNearest : metadata.MinFilter;

                    if (metadata.GenerateMipmaps)
                    {
                        switch (minFilter)
                        {
                            case TextureMinFilter.Nearest:
                                minFilter = TextureMinFilter.NearestMipmapNearest;
                                break;
                            case TextureMinFilter.Linear:
                                minFilter = TextureMinFilter.LinearMipmapNearest;
                                break;
                        }
                    }
                    else
                    {
                        switch (minFilter)
                        {
                            case TextureMinFilter.NearestMipmapNearest:
                            case TextureMinFilter.NearestMipmapLinear:
                                minFilter = TextureMinFilter.Nearest;
                                break;
                            case TextureMinFilter.LinearMipmapNearest:
                            case TextureMinFilter.LinearMipmapLinear:
                                minFilter = TextureMinFilter.Linear;
                                break;
                        }
                    }


                    texture.Target = metadata.TextureTarget;
                    texture.ConfigureFromParameters(
                        wrapMode: metadata.WrapMode,
                        anisoLevel: metadata.AnisoLevel,
                        generateMipmaps: metadata.GenerateMipmaps,
                        compressed: metadata.CompressTexture,
                        compressionFormat: metadata.CompressionFormat,
                        maxSize: (uint)metadata.MaxSize,
                        minFilter: minFilter,
                        magFilter: metadata.MagFilter
                    );
                }
                _cacheTexture[texturePath] = texture;
                return texture;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Failed to create texture from {texturePath}: {ex.Message}");
                return null;
            }
        }

        public void ClearCache()
        {
            Dispose();
        }

        public void Dispose()
        {
            foreach ( var texturePairPathtexture in _cacheTexture )
            {
                texturePairPathtexture.Value.Dispose();
            }
            _cacheTexture.Clear();
        }
    }
}