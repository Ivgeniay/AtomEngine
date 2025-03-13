using AtomEngine;
using OpenglLib;

namespace WindowsBuild
{
    static class Program
    {
        private static void Main(string[] args)
        {
            //Формируем пути к файлам
            string rootPath = AppDomain.CurrentDomain.BaseDirectory;
            WindowBuildFileConfiguration configuration = new WindowBuildFileConfiguration(rootPath);

            //Собираем все .dll
            AssemblyManager assemblyManager = new AssemblyManager();
            assemblyManager.ScanDirectory(configuration.AssembliesPath);

            var options = new AppOptions() { Width = 800, Height = 600, Debug = false };
            using App app = new App(options);

            app.Run();
        }
    }
}
