using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AtomEngine;
using Avalonia;
using System;
using Application = Avalonia.Application;
using System.Threading;

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
                ServiceHub.RegisterService<EventHub>();
                ServiceHub.RegisterService<ModelWatcher>();
                // Загрузка сборок
                ServiceHub.RegisterService<EditorAssemblyManager>();
                // Сканирование файлов в Assets
                ServiceHub.RegisterService<MetadataManager>();
                // Вотчер событий в Assets
                ServiceHub.RegisterService<FileSystemWatcher>();
                // Вотчер событий отношений проекта пользовательских скриптов и папки Assets
                ServiceHub.RegisterService<ScriptProjectGenerator>();
                // Вотчер событий в папке с проектом пользовательских скриптов
                ServiceHub.RegisterService<ProjectFileWatcher>();
                ServiceHub.RegisterService<CsCompileWatcher>();

                ServiceHub.RegisterService<ScriptSyncSystem>();
                // Сервис синхронизации состояния между папками пользовательских скриптов и папкой Assets
                ServiceHub.RegisterService<CodeFilesSynchronizer>();
                // Менеджер загрузки и сохранения состояний ресурсов
                ServiceHub.RegisterService<MaterialManager>();
                ServiceHub.RegisterService<MeshManager>();
                ServiceHub.RegisterService<BuildManager>();
                
                // Фабрики
                ServiceHub.RegisterService<TextureFactory>();
                ServiceHub.RegisterService<MaterialFactory>();
                ServiceHub.RegisterService<MeshFactory>();

                ServiceHub.RegisterService<InspectorViewFactory>();
                ServiceHub.RegisterService<EditorRuntimeResourceManager>();
                ServiceHub.RegisterService<ToolbarService>();
                ServiceHub.RegisterService<DraggableWindowManagerService>();
                ServiceHub.RegisterService<SceneManager>();
                ServiceHub.RegisterService<InspectorDistributor>();
                ServiceHub.RegisterService<ComponentService>();
                ServiceHub.RegisterService<LoadingManager>();

                int delay = 1000;

                await ServiceHub.Initialize(
                    async (type) =>
                    {
                        await loadingWindow.UpdateLoadingStatus($"Начало инициализации {type}...");
                        await Task.Delay(delay);
                    },
                    async (type) =>
                    {
                        await loadingWindow.UpdateLoadingStatus($"Инициализации {type} завершена.");
                        await Task.Delay(delay);
                    });

                await loadingWindow.UpdateLoadingStatus("Компиляция проекта...");
                await ServiceHub.Get<ScriptSyncSystem>().Compile();
                await Task.Delay(100);

                Dispatcher.UIThread.UnhandledException += (sender, args) =>
                {
                    DebLogger.Fatal($"Необработанное исключение в UI потоке: {args.Exception.Message}\n{args.Exception.StackTrace}");
                };

                AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                {
                    var exception = (Exception)args.ExceptionObject;
                    DebLogger.Fatal($"Необработанное исключение AppDomain: {exception.Message}\n{exception.StackTrace}");
                };

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