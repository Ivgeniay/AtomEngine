using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using AtomEngine;

namespace OpenglLib
{
    public class Texture : IDisposable
    {
        private uint _handle;
        private GL _gl;

        public string Path { get; set; }
        public TextureType Type { get; }
        public TextureTarget Target = TextureTarget.Texture2D;

        public unsafe Texture(GL gl, string path, TextureType type = TextureType.None)
        {
            _gl = gl;
            Path = path;
            Type = type;
            _handle = _gl.GenTexture();
            Bind();

            using (var img = Image.Load<Rgba32>(path))
            {
                DebLogger.Info($"Loading texture: {path}");
                DebLogger.Info($"Size: {img.Width}x{img.Height}");
                gl.TexImage2D(Target, 0, InternalFormat.Rgba8, (uint)img.Width, (uint)img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);

                img.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        fixed (void* data = accessor.GetRowSpan(y))
                        {
                            gl.TexSubImage2D(Target, 0, 0, y, (uint)accessor.Width, 1, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                        }
                    }
                });
            }

            SetParameters();
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
            //_gl.TexParameter(Target, TextureParameterName.TextureBaseLevel, 0);
            //_gl.TexParameter(Target, TextureParameterName.TextureMaxLevel, 8);
            _gl.GenerateMipmap(Target);
        }

        public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
        {
            _gl.ActiveTexture(textureSlot);
            _gl.BindTexture(Target, _handle);
        }
        
        public void Dispose()
        {
            _gl.DeleteTexture(_handle);
        }
    }
}
