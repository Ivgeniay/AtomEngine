using OpenglLib;

namespace SmokeTesting
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ILogger logger = new DefaultLogger();
            using App app = new App(options: new AppOptions() { Width = 800, Height = 600, Debug = true, Logger = logger });

            app.Run();
        }
    }

    public class DefaultLogger : ILogger
    {
        private LogLevel _logLevel;
        public LogLevel LogLevel { get => _logLevel; set => _logLevel = value; }
        public void Log(string message, LogLevel logLevel)
        {
            ConsoleColor enterColor = Console.ForegroundColor;
            ConsoleColor color = Console.ForegroundColor;
            switch (logLevel)
            {
                case LogLevel.Debug: color = ConsoleColor.White; break;
                case LogLevel.Info: color = ConsoleColor.White; break;
                case LogLevel.Warn: color = ConsoleColor.Yellow; break;
                case LogLevel.Error: color = ConsoleColor.Red; break;
                case LogLevel.Fatal: color = ConsoleColor.DarkRed; break;
            }
            Console.ForegroundColor = color;
            Console.Write($"{logLevel}:");
            Console.ForegroundColor = enterColor;
            Console.Write($"{message}\n");
        }
    }
}
