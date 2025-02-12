using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;
using System.Reflection;
using AtomEngine.RenderEntity;
using System.Resources;

namespace OpenglLib
{
    public class Texture : IDisposable
    {
        private uint _handle;
        private GL _gl;
        private bool isBinded = false;
        public string Path { get; set; }
        public TextureType Type { get; }
        public TextureTarget Target = TextureTarget.Texture2D;
        private Image<Rgba32> _image;

        public unsafe Texture(GL gl, string path, TextureType type = TextureType.None)
        {
            _gl = gl;
            Path = path;
            Type = type;
            _handle = _gl.GenTexture();
            bool useEmbeddedResources = !(path.Contains("\\") || path.Contains("/"));

            if (!useEmbeddedResources) _image = Image.Load<Rgba32>(path);
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
        }

        public unsafe Texture(GL gl, Span<byte> data, uint width, uint height)
        {
            _gl = gl;

            _handle = _gl.GenTexture();
            Bind();

            fixed (void* d = &data[0])
            {
                _gl.TexImage2D(Target, 0, (int)InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, d);
                SetParameters();
            }
        }

        public void SetParameters(
                Silk.NET.OpenGL.TextureWrapMode wrapS = Silk.NET.OpenGL.TextureWrapMode.ClampToEdge,
                Silk.NET.OpenGL.TextureWrapMode wrapT = Silk.NET.OpenGL.TextureWrapMode.ClampToEdge,
                TextureMinFilter minFilter = TextureMinFilter.LinearMipmapLinear,
                TextureMagFilter magFilter = TextureMagFilter.Linear)
        {
            _gl.TexParameter(Target, TextureParameterName.TextureWrapS, (int)wrapS);
            _gl.TexParameter(Target, TextureParameterName.TextureWrapT, (int)wrapT);
            _gl.TexParameter(Target, TextureParameterName.TextureMinFilter, (int)minFilter);
            _gl.TexParameter(Target, TextureParameterName.TextureMagFilter, (int)magFilter);
            _gl.TexParameter(Target, TextureParameterName.TextureBaseLevel, 0);
            _gl.TexParameter(Target, TextureParameterName.TextureMaxLevel, 8);
            _gl.GenerateMipmap(Target);
        }

        public unsafe void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            _gl.ActiveTexture(textureSlot);
            _gl.BindTexture(Target, _handle);
            if (!isBinded)
            {
                _gl.TexImage2D(Target, 0, InternalFormat.Rgba8, (uint)_image.Width, (uint)_image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);

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
                SetParameters();
                _image?.Dispose();
                isBinded = true;
            }
        }
        
        public void Dispose()
        {
            _gl.DeleteTexture(_handle);
            _image?.Dispose();
        }
    }
}
