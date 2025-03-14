using AtomEngine;

namespace WindowsBuild
{
    public class DefaultLogger : ILogger, IDisposable
    {
        public DefaultLogger() { 
            DebLogger.AddLogger(this);
        }


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
            Console.Write($"{logLevel} ({DateTime.Now}):");
            Console.ForegroundColor = enterColor;
            Console.Write($"{message}\n");
        }

        public void Dispose()
        {
            DebLogger.RemoveLogger(this);
        }
    }
}
