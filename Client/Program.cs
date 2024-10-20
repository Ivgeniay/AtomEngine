using Microsoft.Extensions.DependencyInjection;
using AtomEngine.Diagnostic;
using AtomEngine.Services;
using AtomEngine.Scenes;
using AtomEngine.Math;
using OpenGLCore;

namespace Client
{
    internal class Program
    { 
        static void Main(string[] args)
        { 
            using(DIContainer di = new DIContainer()) {
                di.GetServiceCollection()
                    .AddSingleton<DIContainer>(di)
                    .AddSingleton<ILogger, Logger>()
                    .AddSingleton<ISceneDisposer, SceneDisposer>()
                    .AddScoped<SceneDIContainer>()
                    .AddTransient<AppConfiguration>(options => 
                        new AppConfiguration() {
                            Resolution = new Vector2D<int>(1024, 768),
                            Title = "Atom Engine",
                            UpdateFrequency = 144})
                    .AddSingleton<App>()
                    .AddSingleton<GLScene>();

                di.BuildContainer();
                
                App core = di.GetService<App>();
                using (core)
                {

                    GLScene scene = new GLScene(di, di.GetService<ILogger>());
                    //var glScene = di.GetService<GLScene>();
                    var sceneDisposer = di.GetService<ISceneDisposer>();
                    sceneDisposer.AddScene(scene);
                    sceneDisposer.LoadScene(scene);

                    core.Run();
                } 
            }
        }
    }

    public class Logger : ILogger
    {
        private bool isEnabled = true; 
        public void Enable(bool enable) => isEnabled = enable; 
        public bool IsEnabled => isEnabled;

        public void Log(LogLevel logLevel, string message)
        {
            Console.WriteLine($"{logLevel} {DateTime.Now}: {message}");
        }

        public void Log(string message, LogLevel logLevel = LogLevel.Information)
        {
            Console.WriteLine($"{logLevel} {DateTime.Now}: {message}");
        } 
    }
}
