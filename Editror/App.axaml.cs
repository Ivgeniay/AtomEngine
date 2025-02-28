using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AtomEngine;
using Avalonia;
using System;
using Application = Avalonia.Application;

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
                ServiceHub.RegisterService<Configuration>();
                ServiceHub.RegisterService<AssemblyManager>();
                ServiceHub.RegisterService<AssetFileSystem>();
                ServiceHub.RegisterService<ScriptProjectGenerator>();
                ServiceHub.RegisterService<ProjectFileWatcher>();
                ServiceHub.RegisterService<ScriptSyncSystem>();
                ServiceHub.RegisterService<CodeFilesSynchronizer>();

                await ServiceHub.Initialize(
                    async (type) =>
                    {
                        await Task.Delay(100);
                        await loadingWindow.UpdateLoadingStatus($"Начало инициализации {type}...");
                        await Task.Delay(100);
                    },
                    async (type) =>
                    {
                        await Task.Delay(100);
                        await loadingWindow.UpdateLoadingStatus($"Инициализации {type} завершена.");
                        await Task.Delay(100);
                    });

                await loadingWindow.UpdateLoadingStatus("Компиляция проекта...");
                await ServiceHub.Get<ScriptSyncSystem>().Compile();
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
                DebLogger.Error($"Ошибка при инициализации: {ex}");
            }
        }
    }
}