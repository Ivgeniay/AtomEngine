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
                mainWindow = new MainWindow();
                loadingWindow.Show();
                InitializeAppAsync(desktop);
            }

            base.OnFrameworkInitializationCompleted();
        }

        private async void InitializeAppAsync(IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                await Task.Delay(100);
                await loadingWindow.UpdateLoadingStatus("������������� �������� �������...");
                AssetFileSystem.Instance.Initialize();
                await Task.Delay(100);

                await loadingWindow.UpdateLoadingStatus("������������� ������...");
                AssemblyManager.Instance.Initialize(AppDomain.CurrentDomain.GetAssemblies());
                await Task.Delay(100);

                await loadingWindow.UpdateLoadingStatus("��������� ������� ���������������� ��������...");
                ScriptProjectGenerator.GenerateProject();
                ProjectFileWatcher.Initialize();
                await Task.Delay(100);

                await loadingWindow.UpdateLoadingStatus("������������� ������ � �������� �������� �������...");
                await ScriptSyncSystem.Initialize();
                await Task.Delay(100);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    desktop.MainWindow = mainWindow;
                    mainWindow.Show();
                    loadingWindow.Close();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"������ ��� �������������: {ex}");
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