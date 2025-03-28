using AtomEngine;
using EngineLib;
using OpenglLib;
using System.Reflection;

namespace WindowsBuild
{
    static class Program
    {
        private static void Main(string[] args)
        {
#if DEBUG
            DefaultLogger logger = new DefaultLogger();
#endif
            Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<Assembly> embeddedFileAssembly = new List<Assembly>();
            foreach (var assembly in allAssemblies)
            {
                var name = assembly.GetName().Name;
                if (
                    name == "CommonLib" ||
                    name == "EngineLib" ||
                    name == "OpenglLib"
                    )
                    embeddedFileAssembly.Add(assembly);
            }
            IncludeProcessor.RegisterContentProvider(new FileSystemContentProvider());
            IncludeProcessor.RegisterContentProvider(new EmbeddedContentProvider(embeddedFileAssembly.ToArray()));

            string rootPath = AppDomain.CurrentDomain.BaseDirectory;
            WindowBuildFileRouter router = new WindowBuildFileRouter(rootPath);

            ServiceHub.RegisterService<AssemblyManager>();
            ServiceHub.RegisterService<RuntimeResourceManager>();
            ServiceHub.RegisterService<EventHub>();
            ServiceHub.RegisterService<DirectoryExplorer>(); //Нужно создать экземпляр для WindowsBuild, создать модели, зарегать все сраные пути из router
            ServiceHub.RegisterService<OpenGlExcludeSerializationTypeService>();
            ServiceHub.AddMapping<ExcludeSerializationTypeService, OpenGlExcludeSerializationTypeService>();
            ServiceHub.RegisterService<BindingPointService>();

            ServiceHub.Initialize().Wait();

            AssemblyManager assemblyManager = ServiceHub.Get<AssemblyManager>();
            assemblyManager.InitializeAddDomainAssemblies();
            assemblyManager.ScanDirectory(router.AssembliesPath);

            RuntimeResourceManager resourceManager = ServiceHub.Get<RuntimeResourceManager>();
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

            //Input.KeyDown += (s, e) =>
            //{
            //    DebLogger.Debug(e);
            //};

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
