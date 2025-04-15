using System.Collections.Generic;
using Avalonia.OpenGL;
using Avalonia;
using System;
using AtomEngine;

namespace Editor
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                if (exception is ExecutionEngineException)
                {
                    if (!e.IsTerminating)
                    {
                        
                    }
                }
            };

            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
            catch (Exception e)
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
            }
        } 

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
