using AtomEngine.Diagnostic;
using AtomEngine.Math; 
using OpenGLCore;

namespace Client
{
    internal class Program
    { 
        static void Main(string[] args)
        { 
            ILogger logger = new Logger();

            using (App core = new App(logger)) {
                core.CreateWindow(new Vector2D<int>(1024, 768), updateFrequency: 144);
                core.SceneDisposer.AddScene(new SceneOne("SceneOne", logger));
                core.SceneDisposer.AddScene(new SceneTwo("SceneTwo", logger));
                core.SceneDisposer.LoadScene("SceneOne");
                core.Run();
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
