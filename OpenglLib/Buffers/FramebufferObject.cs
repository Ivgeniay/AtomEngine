using EngineLib;
using Silk.NET.OpenGL;

namespace OpenglLib.Buffers
{
    public class FramebufferObject : IDisposable
    {
        private uint _handle;
        private GL _gl;
        private uint _depthTexture;
        private int _x;
        private int _y;
        private int _width;
        private int _height;
        private bool _isDisposed;

        public uint Handle => _handle;
        public uint DepthTexture => _depthTexture;
        public int X => _x;
        public int Y => _y;
        public int Width => _width;
        public int Height => _height;

        public FramebufferObject(GL gl, int width, int height, int x = 0, int y = 0)
        {
            _gl = gl;
            _x = x;
            _y = y;
            _width = width;
            _height = height;
            _isDisposed = false;

            CreateFramebuffer();
        }

        private unsafe void CreateFramebuffer()
        {
            _handle = _gl.GenFramebuffer();
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _handle);

            _depthTexture = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, _depthTexture);
            _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.DepthComponent,
                          (uint)_width, (uint)_height, 0,
                          PixelFormat.DepthComponent, PixelType.Float, null);

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);

            float[] borderColor = { 1.0f, 1.0f, 1.0f, 1.0f };
            unsafe
            {
                fixed (float* borderColorPtr = borderColor)
                {
                    _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, borderColorPtr);
                }
            }

            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                                   FramebufferAttachment.DepthAttachment,
                                   TextureTarget.Texture2D,
                                   _depthTexture, 0);

            _gl.DrawBuffer(DrawBufferMode.None);
            _gl.ReadBuffer(ReadBufferMode.None);

            if (_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            {
                throw new Exception("Framebuffer is not complete!");
            }

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void Bind()
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _handle);
            _gl.Viewport(_x, _y, (uint)_width, (uint)_height);
        }

        public void Unbind(int screenWidth, int screenHeight)
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _gl.Viewport(0, 0, (uint)screenWidth, (uint)screenHeight);
        }

        public unsafe void Resize(int width, int height, int x = 0, int y = 0)
        {
            if (_width == width && _height == height && _x == x && _y == y)
                return;

            _x = x;
            _y = y;
            _width = width;
            _height = height;

            _gl.BindTexture(TextureTarget.Texture2D, _depthTexture);
            _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.DepthComponent,
                          (uint)_width, (uint)_height, 0,
                          PixelFormat.DepthComponent, PixelType.Float, null);

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _handle);
            if (_gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            {
                throw new Exception("Framebuffer is not complete after resize!");
            }
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public unsafe byte[] GetDepthTextureData()
        {
            int width = _width;
            int height = _height;

            Bind();

            int pixelSize = sizeof(float);
            byte[] data = new byte[width * height * pixelSize];

            fixed (byte* dataPtr = data)
            {
                _gl.ReadPixels(0, 0, (uint)width, (uint)height, PixelFormat.DepthComponent, PixelType.Float, dataPtr);
            }

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            return data;
        }

        public unsafe byte[] GetDepthTextureDataFromTexture()
        {
            int width = _width;
            int height = _height;

            _gl.BindTexture(TextureTarget.Texture2D, _depthTexture);

            int pixelSize = sizeof(float);
            byte[] data = new byte[width * height * pixelSize];

            fixed (byte* dataPtr = data)
            {
                _gl.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.DepthComponent, PixelType.Float, dataPtr);
            }

            _gl.BindTexture(TextureTarget.Texture2D, 0);

            return data;
        }

        public Texture CreateDepthTexture()
        {
            byte[] textureData = GetDepthTextureDataFromTexture();
            Texture depthTexture = new Texture(_gl, textureData, _width, _height, PixelFormat.DepthComponent, PixelType.Float);
            return depthTexture;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _gl.DeleteTexture(_depthTexture);
            _gl.DeleteFramebuffer(_handle);
            _isDisposed = true;
        }
    }

    public class FBOService : IService
    {
        private readonly Dictionary<string, FramebufferObject> _fboByName = new Dictionary<string, FramebufferObject>();
        private readonly object _lock = new object();
        private GL _gl;

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public void SetGL(GL gl)
        {
            _gl = gl;
        }

        public FramebufferObject GetOrCreateFBO(string name, int width, int height, int x = 0, int y = 0)
        {
            lock (_lock)
            {
                if (_fboByName.TryGetValue(name, out var existingFBO))
                {
                    existingFBO.Resize(width, height, x, y);
                    return existingFBO;
                }

                var fbo = new FramebufferObject(_gl, width, height, x, y);
                _fboByName[name] = fbo;
                return fbo;
            }
        }

        public FramebufferObject GetExistingFBO(string name)
        {
            lock (_lock)
            {
                if (_fboByName.TryGetValue(name, out var fbo))
                {
                    return fbo;
                }
                return null;
            }
        }

        public bool HasFBO(string name)
        {
            lock (_lock)
            {
                return _fboByName.ContainsKey(name);
            }
        }

        public void RemoveFBO(string name)
        {
            lock (_lock)
            {
                if (_fboByName.TryGetValue(name, out var fbo))
                {
                    fbo.Dispose();
                    _fboByName.Remove(name);
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var fbo in _fboByName.Values)
                {
                    fbo.Dispose();
                }
                _fboByName.Clear();
            }
        }
    }

}
