using Microsoft.AspNetCore.Components;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.Drawing.Imaging;
using System.Drawing;
using Microsoft.JSInterop;

namespace AtomEngineEditor.Components
{
    public partial class OpenTkWindowBase : ComponentBase, IDisposable
    {
        [Inject] private IJSRuntime JSRuntime { get; set; }
        protected ElementReference imageElement;
        protected OffscreenRenderer renderer;
        protected const int Width = 800;
        protected const int Height = 600;

        protected override void OnInitialized()
        {
            renderer = new OffscreenRenderer(Width, Height);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await RenderAndUpdateImage();
            }
        }

        private async Task RenderAndUpdateImage()
        {
            using (var bitmap = renderer.Render())
            {
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    var imageBase64 = Convert.ToBase64String(ms.ToArray());
                    await JSRuntime.InvokeVoidAsync("updateImage", imageElement, $"data:image/png;base64,{imageBase64}");
                }
            }
        }

        public void Dispose()
        {
            renderer?.Dispose();
        }
    }

    public class OffscreenRenderer : IDisposable
    {
        private readonly int width;
        private readonly int height;
        private readonly NativeWindow nativeWindow;
        private readonly IGraphicsContext context;

        public OffscreenRenderer(int width, int height)
        {
            this.width = width;
            this.height = height;

            var nativeWindowSettings = new NativeWindowSettings()
            {
                Size = new Vector2i(width, height),
                WindowBorder = WindowBorder.Hidden,
                WindowState = WindowState.Minimized,
                Flags = ContextFlags.Offscreen
            };

            nativeWindow = new NativeWindow(nativeWindowSettings);
            context = nativeWindow.Context;
        }

        public Bitmap Render()
        {
            context.MakeCurrent();

            GL.Viewport(0, 0, width, height);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Ваш код рендеринга OpenGL здесь
            GL.ClearColor(Color4.Blue);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // Создание битмапа из буфера кадра
            var bmp = new Bitmap(width, height);
            var data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);

            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            context.SwapBuffers();

            return bmp;
        }

        public void Dispose()
        {
            nativeWindow.Dispose();
        }
    }
}
