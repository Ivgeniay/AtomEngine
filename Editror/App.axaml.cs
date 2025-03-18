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
                // �������� ��������
                ServiceHub.RegisterService<DirectoryExplorer>();
                // �������� ������������
                ServiceHub.RegisterService<Configuration>();
                ServiceHub.RegisterService<EventHub>();
                ServiceHub.RegisterService<ModelWatcher>();
                // �������� ������
                ServiceHub.RegisterService<EditorAssemblyManager>();
                // ������������ ������ � Assets
                ServiceHub.RegisterService<MetadataManager>();
                // ������ ������� � Assets
                ServiceHub.RegisterService<FileSystemWatcher>();
                // ������ ������� ��������� ������� ���������������� �������� � ����� Assets
                ServiceHub.RegisterService<ScriptProjectGenerator>();
                // ������ ������� � ����� � �������� ���������������� ��������
                ServiceHub.RegisterService<ProjectFileWatcher>();
                ServiceHub.RegisterService<CsCompileWatcher>();

                ServiceHub.RegisterService<ScriptSyncSystem>();
                // ������ ������������� ��������� ����� ������� ���������������� �������� � ������ Assets
                ServiceHub.RegisterService<CodeFilesSynchronizer>();
                // �������� �������� � ���������� ��������� ��������
                ServiceHub.RegisterService<MaterialManager>();
                ServiceHub.RegisterService<MeshManager>();
                ServiceHub.RegisterService<BuildManager>();
                
                // �������
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
                        await loadingWindow.UpdateLoadingStatus($"������ ������������� {type}...");
                        await Task.Delay(delay);
                    },
                    async (type) =>
                    {
                        await loadingWindow.UpdateLoadingStatus($"������������� {type} ���������.");
                        await Task.Delay(delay);
                    });

                await loadingWindow.UpdateLoadingStatus("���������� �������...");
                await ServiceHub.Get<ScriptSyncSystem>().Compile();
                await Task.Delay(100);

                Dispatcher.UIThread.UnhandledException += (sender, args) =>
                {
                    DebLogger.Fatal($"�������������� ���������� � UI ������: {args.Exception.Message}\n{args.Exception.StackTrace}");
                };

                AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
                {
                    var exception = (Exception)args.ExceptionObject;
                    DebLogger.Fatal($"�������������� ���������� AppDomain: {exception.Message}\n{exception.StackTrace}");
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
                DebLogger.Error($"������ ��� �������������: {ex}");
            }
        }

    }
}