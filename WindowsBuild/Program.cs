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
            WindowBuildFileRouter router = new WindowBuildFileRouter(rootPath);

            //Собираем все .dll
            AssemblyManager assemblyManager = new AssemblyManager();
            assemblyManager.ScanDirectory(router.AssembliesPath);

            RuntimeResourceManager resourceManager = new RuntimeResourceManager();

            var options = new AppOptions() { Width = 800, Height = 600, Debug = false };
            using App app = new App(options);

            app.OnLoaded += (gl) =>
            {
                ResourceLoader.LoadResources(app.Gl, app.Assimp, router, assemblyManager, resourceManager);
            };

            app.Run();
        }

    }


}
