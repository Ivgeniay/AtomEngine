using Editor.Data;
using Microsoft.Extensions.DependencyInjection;

namespace WinAtomEngineFrontend
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var services = new ServiceCollection();
            services.AddWindowsFormsBlazorWebView();
            services.AddBlazorWebViewDeveloperTools();

            services.AddSingleton<WeatherForecastService>(); 

            using var serviceProvider = services.BuildServiceProvider();

            Application.Run(new Form1(serviceProvider));
        }
    }
}