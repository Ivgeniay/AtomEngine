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
    public class OpenGlController : OpenGlControlBase, IWindowed
    {
        public Action<object> OnClose { get; set; }
        private Stopwatch _stopwatch;

        public OpenGlController()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            PropertyChanged += OnPropertyChangedHandler;
        }

        private void OnPropertyChangedHandler(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == BoundsProperty)
            {
                //RequestNextFrameRendering();
            }
        }

        protected override void OnOpenGlInit(GlInterface gl)
        {
            
        }

        protected override void OnOpenGlDeinit(GlInterface gl)
        {
            
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            
        }


        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            DebLogger.Info($"OnSizeChanged: {e.PreviousSize} -> {e.NewSize}");
            this.RequestNextFrameRendering();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            PropertyChanged -= OnPropertyChangedHandler;
        }

        public void Dispose()
        {
            OnClose?.Invoke(this);
        }
    }
}