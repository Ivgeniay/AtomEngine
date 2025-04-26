using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using System.Reflection;
using SixLabors.ImageSharp.Processing;

namespace OpenglLib
{
    public sealed class Texture : IDisposable
    {
        private uint _handle;
        private GL _gl;
        private bool _isBound = false;
        private Image<Rgba32> _image;
        private bool _generateMipmaps = true;
        public string Path { get; set; }
        public uint Handle { get { return _handle; } }
        public TextureType Type { get; }

        public PixelFormat PixelDataFormat { get; private set; } = PixelFormat.Rgba;
        public PixelType PixelDataType { get; private set; } = PixelType.UnsignedByte;

        public TextureUnit TextureUnit { get; private set; } = TextureUnit.Texture0;
        public TextureTarget Target { get; set; } = TextureTarget.Texture2D;
        public Silk.NET.OpenGL.TextureWrapMode WrapS { get; set; } = Silk.NET.OpenGL.TextureWrapMode.Repeat;
        public Silk.NET.OpenGL.TextureWrapMode WrapT { get; set; } = Silk.NET.OpenGL.TextureWrapMode.Repeat;
        public TextureMinFilter MinFilter { get; set; } = TextureMinFilter.LinearMipmapLinear;
        public TextureMagFilter MagFilter { get; set; } = TextureMagFilter.Linear;
        public InternalFormat CompressionFormat { get; set; } = InternalFormat.Rgba8;
        public int AnisoLevel { get; set; } = 1;
        public bool IsCompressed { get; set; } = false;
        public uint MaxSize { get; set; } = 2048;

        public int Width { get; set; } = 0;
        public int Height { get; set; } = 0;

        public unsafe Texture(GL gl, string path, TextureType type = TextureType.None)
        {
            _gl = gl;
            Path = path;
            Type = type;
            _handle = _gl.GenTexture();
            bool useEmbeddedResources = !(path.Contains("\\") || path.Contains("/"));

            if (!useEmbeddedResources)
            {
                _image = Image.Load<Rgba32>(path);
            }
            else
            {
                var assembly = Assembly.GetExecutingAssembly();
                var normalizedShaderName = path.Replace('/', '.').Replace('\\', '.');
                var resources = assembly.GetManifestResourceNames();
                var resourceName = resources.FirstOrDefault(r =>
                    r.EndsWith(normalizedShaderName, StringComparison.OrdinalIgnoreCase));

                if (resourceName == null)
                    throw new FileNotFoundError($"Resource not found: {path}");

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                        throw new FileNotFoundException($"Resource not found: {resourceName}");

                    _image = Image.Load<Rgba32>(stream);
                }
            }
            Width = _image.Width;
            Height = _image.Height;
        }

        public unsafe Texture(GL gl, byte[] textureData, int width, int height,
                     PixelFormat pixelFormat = PixelFormat.Rgba,
                     PixelType pixelType = PixelType.UnsignedByte,
                     TextureType type = TextureType.None)
        {
        }

        private void LoadImage(string path)
        {
            bool useEmbeddedResources = !(path.Contains("\\") || path.Contains("/"));

            if (!useEmbeddedResources)
            {
                if (System.IO.File.Exists(path))
                {
                    _image = Image.Load<Rgba32>(path);
                }
                else
                {
                    throw new FileNotFoundException($"Texture file not found: {path}");
                }
            }
            else
            {
                var assembly = Assembly.GetExecutingAssembly();
                var normalizedResourceName = path.Replace('/', '.').Replace('\\', '.');
                var resources = assembly.GetManifestResourceNames();
                var resourceName = resources.FirstOrDefault(r =>
                    r.EndsWith(normalizedResourceName, StringComparison.OrdinalIgnoreCase));

                if (resourceName == null)
                    throw new FileNotFoundException($"Resource not found: {path}");

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                        throw new FileNotFoundException($"Resource not found: {resourceName}");

                    _image = Image.Load<Rgba32>(stream);
                }
            }

            // Apply size limits if needed
            if (MaxSize > 0 && (_image.Width > MaxSize || _image.Height > MaxSize))
            {
                ResizeImage();
            }
        }

        private void ResizeImage()
        {
            float aspectRatio = (float)_image.Width / _image.Height;
            int newWidth, newHeight;

            if (_image.Width > _image.Height)
            {
                newWidth = (int)MaxSize;
                newHeight = (int)(MaxSize / aspectRatio);
            }
            else
            {
                newHeight = (int)MaxSize;
                newWidth = (int)(MaxSize * aspectRatio);
            }

            _image.Mutate(x => x.Resize(newWidth, newHeight));
        }
        

        public void ConfigureFromParameters(
            Silk.NET.OpenGL.TextureWrapMode wrapMode = Silk.NET.OpenGL.TextureWrapMode.Repeat,
            int anisoLevel = 1,
            bool generateMipmaps = true,
            bool compressed = false,
            InternalFormat compressionFormat = InternalFormat.Rgba8,
            uint maxSize = 2048,
            TextureMinFilter minFilter = TextureMinFilter.Nearest,
            TextureMagFilter magFilter = TextureMagFilter.Linear
            )
        {
            WrapS = wrapMode;
            WrapT = wrapMode;
            AnisoLevel = Math.Clamp(anisoLevel, 1, 16);
            _generateMipmaps = generateMipmaps;
            IsCompressed = compressed;
            CompressionFormat = compressionFormat;
            MaxSize = maxSize;
            MinFilter = minFilter;
            MagFilter = magFilter;
        }

        public void ApplyParameters()
        {
            _gl.TexParameter(Target, TextureParameterName.TextureWrapS, (int)WrapS);
            _gl.TexParameter(Target, TextureParameterName.TextureWrapT, (int)WrapT);
            _gl.TexParameter(Target, TextureParameterName.TextureMinFilter, (int)MinFilter);
            _gl.TexParameter(Target, TextureParameterName.TextureMagFilter, (int)MagFilter);
            _gl.TexParameter(Target, TextureParameterName.TextureBaseLevel, 0);


            if (AnisoLevel > 1)
            {
                if (_gl.IsExtensionPresent("GL_EXT_texture_filter_anisotropic"))
                {
                    _gl.TexParameter(Target, (TextureParameterName)0x84FE, AnisoLevel);
                }
            }

            if (_generateMipmaps)
            {
                _gl.TexParameter(Target, TextureParameterName.TextureMaxLevel, 8);
                _gl.GenerateMipmap(Target);
            }
            else
            {
                _gl.TexParameter(Target, TextureParameterName.TextureMaxLevel, 0);
            }
        }

        public unsafe void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            this.TextureUnit = textureSlot;
            _gl.ActiveTexture(textureSlot);
            _gl.BindTexture(Target, _handle);
            if (!_isBound && _image != null)
            {
                InternalFormat format = IsCompressed ? CompressionFormat : InternalFormat.Rgba8;
                _gl.TexImage2D(Target, 0, (int)format, (uint)_image.Width, (uint)_image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);

                _image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        fixed (void* data = accessor.GetRowSpan(y))
                        {
                            _gl.TexSubImage2D(Target, 0, 0, y, (uint)accessor.Width, 1, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                        }
                    }
                });

                ApplyParameters();
                _image?.Dispose();
                _isBound = true;
            }
        }
        
        public void Dispose()
        {
            _gl.DeleteTexture(_handle);
            _image?.Dispose();
        }
    }
}
