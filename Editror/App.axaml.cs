using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AtomEngine;
using Avalonia;
using System;
using Application = Avalonia.Application;
using System.Threading;
using EngineLib;
using OpenglLib;

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
            //AvaloniaXamlLoader.Load(new AvaloniaEdit.AvaloniaEditTheme());

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
                ServiceHub.RegisterService<EditorDirectoryExplorer>();
                ServiceHub.AddMapping<DirectoryExplorer, EditorDirectoryExplorer>();
                // Загрузка конфигураций
                ServiceHub.RegisterService<Configuration>();
                ServiceHub.RegisterService<EventHub>();
                ServiceHub.RegisterService<ModelWatcher>();
                // Загрузка сборок
                ServiceHub.RegisterService<EditorAssemblyManager>();
                ServiceHub.AddMapping<AssemblyManager, EditorAssemblyManager>();
                // Сканирование файлов в Assets
                ServiceHub.RegisterService<EditorMetadataManager>();
                ServiceHub.AddMapping<MetadataManager, EditorMetadataManager>();
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
                ServiceHub.RegisterService<EditorMaterialAssetManager>();
                ServiceHub.AddMapping<MaterialAssetManager, EditorMaterialAssetManager>();
                ServiceHub.RegisterService<EditorModelManager>();
                ServiceHub.AddMapping<ModelManager, EditorModelManager>();
                ServiceHub.RegisterService<BuildManager>();
                
                // Фабрики
                ServiceHub.RegisterService<TextureFactory>();
                ServiceHub.RegisterService<EditorMaterialFactory>();
                ServiceHub.AddMapping<MaterialFactory, EditorMaterialFactory>();
                ServiceHub.RegisterService<MeshFactory>();
                ServiceHub.RegisterService<ShaderFactory>();

                ServiceHub.RegisterService<InspectorViewFactory>();
                ServiceHub.RegisterService<EditorRuntimeResourceManager>();
                ServiceHub.AddMapping<RuntimeResourceManager, EditorRuntimeResourceManager>();
                ServiceHub.AddMapping<OpenGLRuntimeResourceManager, EditorRuntimeResourceManager>();

                ServiceHub.RegisterService<ToolbarService>();
                ServiceHub.RegisterService<DraggableWindowManagerService>();
                ServiceHub.RegisterService<SceneManager>();
                ServiceHub.RegisterService<InspectorDistributor>();
                ServiceHub.RegisterService<ComponentService>();
                ServiceHub.RegisterService<LoadingManager>();
                ServiceHub.RegisterService<BindingPointService>();

                ServiceHub.RegisterService<OpenGlExcludeSerializationTypeService>();
                ServiceHub.AddMapping<ExcludeSerializationTypeService, OpenGlExcludeSerializationTypeService>();

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

                EventHub eventHub = ServiceHub.Get<EventHub>();
                eventHub.RegisterErrorHandler<Exception>((ex, subscriber, evt) =>
                {
                    if (ex.Message == "Call from invalid thread")
                    {
                        var eventType = evt.GetType();
                        var delegateType = typeof(Action<>).MakeGenericType(eventType);
                        var invokeMethod = delegateType.GetMethod("Invoke");

                        Dispatcher.UIThread.Invoke(new Action(() =>
                        {
                            invokeMethod.Invoke(subscriber, new object[] { evt });
                        }));
                    }
                });


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