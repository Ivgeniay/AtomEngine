using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace Editor
{
    public partial class App : Application
    {
        private LoadingWindow loadingWindow;
        private MainWindow mainWindow;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            Environment.SetEnvironmentVariable("AVALONIA_DISABLE_ANGLE", "1");
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                loadingWindow = new LoadingWindow();
                desktop.MainWindow = loadingWindow;
                loadingWindow.Show();
                InitializeAppAsync(desktop);
            }

            base.OnFrameworkInitializationCompleted();
        }

        private async Task InitializeAppAsync(IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                await Task.Delay(100);
                await loadingWindow.UpdateLoadingStatus("Инициализация конфигураций...");
                Configuration.Initialize();
                await Task.Delay(100);

                await loadingWindow.UpdateLoadingStatus("Инициализация сборок...");
                AssemblyManager.Instance.Initialize(AppDomain.CurrentDomain.GetAssemblies());
                await Task.Delay(100);

                await loadingWindow.UpdateLoadingStatus("Инициализации файловой системы...");
                AssetFileSystem.Instance.Initialize();
                await Task.Delay(100);

                await loadingWindow.UpdateLoadingStatus("Генерация проекта пользовательских скриптов...");
                ScriptProjectGenerator.GenerateProject();
                ProjectFileWatcher.Initialize();
                await Task.Delay(100);

                await loadingWindow.UpdateLoadingStatus("Инициализация работы с файловой системой проекта...");
                await ScriptSyncSystem.Initialize();
                await Task.Delay(100);

                await loadingWindow.UpdateLoadingStatus("Компиляция проекта...");
                await ScriptSyncSystem.Compile();
                await Task.Delay(100);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    mainWindow = new MainWindow();
                    desktop.MainWindow = mainWindow;
                    mainWindow.Show();
                    loadingWindow.Close();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при инициализации: {ex}");
            }
        }

        //public override void Initialize()
        //{
        //    AvaloniaXamlLoader.Load(this);
        //    Environment.SetEnvironmentVariable("AVALONIA_DISABLE_ANGLE", "1");
        //}

        //public override void OnFrameworkInitializationCompleted()
        //{
        //    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        //    {
        //        desktop.MainWindow = new MainWindow();
        //    }


        //    base.OnFrameworkInitializationCompleted();
        //}
    }
}