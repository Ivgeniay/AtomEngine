using AtomEngine;
using OpenglLib;

namespace WindowsBuild
{
    static class Program
    {
        private static void Main(string[] args)
        {
#if DEBUG
            DefaultLogger logger = new DefaultLogger();
#endif

            string rootPath = AppDomain.CurrentDomain.BaseDirectory;
            WindowBuildFileRouter router = new WindowBuildFileRouter(rootPath);

            AssemblyManager assemblyManager = new AssemblyManager();
            assemblyManager.ScanDirectory(router.AssembliesPath);

            RuntimeResourceManager resourceManager = new RuntimeResourceManager();
            WorldManager worldManager = new WorldManager();
            SceneLoader sceneLoader = new(router, assemblyManager, worldManager);
            var scene = sceneLoader.LoadDefaultScene();

            var options = new AppOptions() { Width = 800, Height = 600, Debug = false };
            using App app = new App(options);

            app.OnLoaded += (gl) =>
            {
                ResourceLoader.LoadResources(app.Gl, app.Assimp, router, assemblyManager, resourceManager);
                sceneLoader.InitializeScene(scene, resourceManager);
            };

            Input.KeyDown += (s, e) =>
            {
                DebLogger.Debug(e);
            };

            app.OnUpdated += (deltaTime) =>
            {
                worldManager.Update(deltaTime);
            };

            app.OnRendered += (deltaTime) =>
            {
                worldManager.Render(deltaTime);
            };

            app.OnFixedUpdate += () =>
            {
                worldManager.FixedUpdate();
            };

            app.Run();
#if DEBUG
            logger.Dispose();
#endif
        }

    }
}
