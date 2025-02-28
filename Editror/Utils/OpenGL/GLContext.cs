using Silk.NET.Core.Contexts;
using Silk.NET.Windowing;
using Avalonia.Controls;
using Silk.NET.OpenGL;
using System;

namespace Editor
{
    internal static class GLContext
    {
        private static GL _gl;
        private static IWindow _hiddenWindow;
        private static IGLContext _glContext;
        private static bool _isInitialized;

        /// <summary>
        /// Получить экземпляр GL, инициализируя его при необходимости
        /// </summary>
        public static GL GetGL()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("GL контекст не инициализирован. Сначала вызовите Initialize().");
            }
            return _gl;
        }

        /// <summary>
        /// Инициализирует GL контекст Silk.NET без видимого окна
        /// </summary>
        //public static void Initialize()
        //{
        //    if (_isInitialized)
        //    {
        //        return;
        //    }

        //    // Создаем невидимое окно для контекста GL
        //    var options = WindowOptions.Default;
        //    options.Size = new Silk.NET.Maths.Vector2D<int>(1, 1);
        //    options.Title = "Hidden GL Context";
        //    options.IsVisible = false;
        //    options.ShouldSwapAutomatically = false;
        //    options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Debug);

        //    _hiddenWindow = Window.Create(options);
        //    _hiddenWindow.Initialize();

        //    // Получаем контекст GL из невидимого окна
        //    _glContext = _hiddenWindow.GLContext;
        //    if (_glContext == null)
        //    {
        //        throw new InvalidOperationException("Не удалось создать контекст OpenGL.");
        //    }

        //    // Делаем контекст текущим
        //    _glContext.MakeCurrent();

        //    // Создаем экземпляр GL
        //    _gl = GL.GetApi(_glContext);

        //    _isInitialized = true;
        //}

        ///// <summary>
        ///// Делает контекст GL активным для текущего потока
        ///// </summary>
        //public static void MakeCurrent()
        //{
        //    if (!_isInitialized)
        //    {
        //        throw new InvalidOperationException("GL контекст не инициализирован.");
        //    }
        //    _glContext.MakeCurrent();
        //}

        ///// <summary>
        ///// Освобождает контекст GL из текущего потока
        ///// </summary>
        //public static void ClearCurrent()
        //{
        //    if (!_isInitialized)
        //    {
        //        return;
        //    }
        //    _glContext.Clear();
        //}

        ///// <summary>
        ///// Освобождает ресурсы GL контекста
        ///// </summary>
        //public static void Cleanup()
        //{
        //    if (!_isInitialized)
        //    {
        //        return;
        //    }

        //    ClearCurrent();
        //    _hiddenWindow.Dispose();

        //    _gl = null;
        //    _glContext = null;
        //    _hiddenWindow = null;
        //    _isInitialized = false;
        //}
    }
}
