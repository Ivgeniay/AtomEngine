using Avalonia.OpenGL.Controls;
using Avalonia.OpenGL;
using Silk.NET.OpenGL;
using System;
using AtomEngine;

namespace Editor
{
    internal class GLController : OpenGlControlBase
    {
        private static GL _gl;
        private bool _isInitialized = false;
        public static event Action<GL>? OnGLInitialized;
        public static event Action? OnGLDeInitialized;
        public static event Action<GL>? OnRender;

        public static GL GetGL()
        {
            return _gl;
        }

        protected override void OnOpenGlInit(GlInterface gl)
        {
            _gl = GL.GetApi(gl.GetProcAddress);
            _isInitialized = true;
            //_gl.ClearColor(0.1f, 0.1f, 0.4f, 1.0f);
            _gl.Enable(EnableCap.DepthTest);
            _gl.Enable(EnableCap.CullFace);
            _gl.CullFace(TriangleFace.Back);
            OnGLInitialized?.Invoke(_gl);
        }

        protected override void OnOpenGlDeinit(GlInterface gl)
        {
            _isInitialized = false;
            OnGLDeInitialized?.Invoke();
            _gl = null;
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            if (!_isInitialized || _gl == null)
                return;


            _gl.Viewport(0, 0, (uint)Bounds.Width, (uint)Bounds.Height);
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            OnRender?.Invoke(_gl);
        }

        public void ForceRender()
        {
            if (_isInitialized)
                this.RequestNextFrameRendering();
        }

        public void Dispose()
        {
            _isInitialized = false;
            OnGLDeInitialized?.Invoke();
            _gl = null;
        }

        internal void Invalidate()
        {
            try
            {
                this.RequestNextFrameRendering();
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при запросе рендеринга: {ex.Message}");
            }
        }
    }
}