using CommonLib;

namespace AtomEngine
{
    public static class DebLogger
    {
        public static int MaxCacheEntry = 100;
        private static List<LogEntry> logEntries = new List<LogEntry>();

        private static List<ILogger> loggers = new List<ILogger>();
        public static void AddLogger(ILogger logger) => loggers.Add(logger);
        public static void RemoveLogger(ILogger logger) => loggers.Remove(logger);

        public static IEnumerable<LogEntry> GetLogs()
        {
            foreach (LogEntry entry in logEntries)
            {
                yield return entry;
            }
        }

        public static void Debug(params object[] args)
        {
            AddLogEntry(LogLevel.Debug, args);
            foreach (var logger in loggers)
                logger.Debug(args);
        }
        public static void Info(params object[] args)
        {
            AddLogEntry(LogLevel.Info, args);
            foreach (var logger in loggers)
                logger.Info(args);
        }
        public static void Warn(params object[] args)
        {
            AddLogEntry(LogLevel.Warn, args);
            foreach (var logger in loggers)
                logger.Warn(args); 
        }
        public static void Error(params object[] args)
        {
            AddLogEntry(LogLevel.Error, args);
            foreach (var logger in loggers)
                logger.Error(args); 
        }
        public static void Fatal(params object[] args)
        {
            AddLogEntry(LogLevel.Fatal, args);
            foreach (var logger in loggers)
                logger.Fatal(args); 
        }

        private static void AddLogEntry(LogLevel level, params object[] args)
        {
            string message = string.Join(" ", args);

            var callerInfo = StackTraceHelper.GetCallerInfo(3);
            var logEntry = new LogEntry(message, level, callerInfo);

            //var logEntry = new LogEntry(message, level);

            if (logEntries.Count > MaxCacheEntry)
            {
                logEntries.RemoveAt(0);
            }
            logEntries.Add(logEntry);
        }

        public class LogEntry
        {
            public string Message { get; set; }
            public LogLevel Level { get; set; }
            public DateTime Timestamp { get; set; }

            public CallerInfo CallerInfo { get; set; }

            public LogEntry(string message, LogLevel level)
            {
                Message = message;
                Level = level;
                Timestamp = DateTime.Now;
            }

            public LogEntry(string message, LogLevel level, CallerInfo callerInfo) : this(message, level) =>
                CallerInfo = callerInfo;

            public string GetTimestampString() => Timestamp.ToString("HH:mm:ss.fff");
            public string GetLevelString() => Level.ToString().ToUpper();
            public bool HasSourceInfo() => CallerInfo.IsValid;
        }
    }
}
