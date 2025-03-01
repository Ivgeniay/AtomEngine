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
                // Проверка директор
                ServiceHub.RegisterService<DirectoryExplorer>();
                // Загрузка конфигураций
                ServiceHub.RegisterService<Configuration>();
                // Загрузка сборок
                ServiceHub.RegisterService<AssemblyManager>();
                // Сканирование файлов в Assets
                ServiceHub.RegisterService<MetadataManager>();
                // Вотчер событий в Assets
                ServiceHub.RegisterService<AssetFileSystem>();
                // Вотчер событий отношений проекта пользовательских скриптов и папки Assets
                ServiceHub.RegisterService<ScriptProjectGenerator>();
                // Вотчер событий в папке с проектом пользовательских скриптов
                ServiceHub.RegisterService<ProjectFileWatcher>();

                ServiceHub.RegisterService<ScriptSyncSystem>();
                // Сервис синхронизации состояния между папками пользовательских скриптов и папкой Assets
                ServiceHub.RegisterService<CodeFilesSynchronizer>();
                // Менеджер загрузки и сохранения состояний материалов
                ServiceHub.RegisterService<MaterialManager>();
                
                // Фабрики
                ServiceHub.RegisterService<TextureFactory>();
                ServiceHub.RegisterService<MaterialFactory>();
                ServiceHub.RegisterService<ResourceManager>();

                await ServiceHub.Initialize(
                    async (type) =>
                    {
                        await Task.Delay(200);
                        await loadingWindow.UpdateLoadingStatus($"Начало инициализации {type}...");
                        await Task.Delay(200);
                    },
                    async (type) =>
                    {
                        await Task.Delay(200);
                        await loadingWindow.UpdateLoadingStatus($"Инициализации {type} завершена.");
                        await Task.Delay(200);
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