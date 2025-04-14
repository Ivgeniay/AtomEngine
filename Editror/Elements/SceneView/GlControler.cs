using Avalonia.OpenGL.Controls;
using Avalonia.OpenGL;
using Silk.NET.OpenGL;
using AtomEngine;
using System;
using Avalonia.VisualTree;

namespace Editor
{
    internal class GLController : OpenGlControlBase
    {
        private static GL _gl;
        private bool _isInitialized = false;
        public static event Action<GL>? OnGLInitialized;
        public static event Action? OnGLDeInitialized;
        public static event Action<GL>? OnRender;

        public uint ScaledWidth { get; private set; } = 1;
        public uint ScaledHeight { get; private set; } = 1;

        protected override void OnOpenGlInit(GlInterface gl)
        {
            base.OnOpenGlInit(gl);

            _gl = GL.GetApi(gl.GetProcAddress);
            _isInitialized = true;

            _gl.Enable(EnableCap.DepthTest);
            _gl.Enable(EnableCap.Blend);
            _gl.CullFace(TriangleFace.Front);
            _gl.DepthFunc(DepthFunction.Lequal);

            // Расчет фактического расширения окна
            var scalingFactor = VisualRoot?.RenderScaling ?? 1.0;
            ScaledWidth = (uint)(Bounds.Width * scalingFactor);
            ScaledHeight = (uint)(Bounds.Height * scalingFactor);

            OnGLInitialized?.Invoke(_gl);
        }

        protected override void OnOpenGlDeinit(GlInterface gl)
        {
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            if (!_isInitialized || _gl == null)
                return;

            try
            {
                var error = _gl.GetError();
                if (error != GLEnum.NoError)
                {
                    DebLogger.Warn($"GL error before rendering: {error}");
                    _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                    return;
                }

                OnRender?.Invoke(_gl);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка во время GL-рендеринга: {ex.Message}");
                try
                {
                    _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                }
                catch
                {
                }
            }
        }

        public void ForceRender()
        {
            OnRender?.Invoke(_gl);
        }

        public void Dispose()
        {
            _isInitialized = false;
            OnGLDeInitialized?.Invoke();
            _gl?.Dispose();
            _gl = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }


        internal void Invalidate()
        {
            try
            {
                if (this.GetVisualRoot() != null)
                {
                    this.RequestNextFrameRendering();
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при запросе рендеринга: {ex.Message}");
            }
        }


    }

    public class OpenGLCommand
    {
        public Action<GL> Execute { get; set; }
    }

}