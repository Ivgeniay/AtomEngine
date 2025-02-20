using Avalonia;
using Avalonia.OpenGL;
using System;
using System.Collections.Generic;

namespace Editor
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new Win32PlatformOptions
                {
                    RenderingMode = new[] {
                        Win32RenderingMode.Wgl,
                        Win32RenderingMode.Software
                    },
                    WglProfiles = new List<GlVersion>
                    {
                        new GlVersion(GlProfileType.OpenGL, 4, 0),
                        new GlVersion(GlProfileType.OpenGL, 3, 3),
                        new GlVersion(GlProfileType.OpenGL, 3, 0)
                    }
                })
                .LogToTrace();

    }
}
