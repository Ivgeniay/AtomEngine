using Avalonia.OpenGL.Controls;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Silk.NET.OpenGL;
using System;
using Avalonia;
using AtomEngine;

namespace Editor
{

    public class GLController : OpenGlControlBase
    {
        private static GL _gl;
        private bool _isInitialized = false;

        public static event Action<GL>? OnGLInitialized;
        public static event Action? OnGLDeInitialized;


        public static GL GetGL()
        {
            return _gl;
        }

        protected override void OnOpenGlInit(GlInterface gl)
        {
            _gl = GL.GetApi(gl.GetProcAddress);
            _isInitialized = true;

            OnGLInitialized?.Invoke(_gl);

            _gl.ClearColor(0.1f, 0.1f, 0.4f, 1.0f);
            _gl.Enable(EnableCap.DepthTest);
            _gl.Enable(EnableCap.CullFace);
            _gl.CullFace(TriangleFace.FrontAndBack);
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

            // Вызов рендеринга сцены
            //SceneRenderer.RenderCurrentScene();
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
    }
}