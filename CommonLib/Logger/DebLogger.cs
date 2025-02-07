namespace AtomEngine
{
    public static class DebLogger
    {
        private static List<ILogger> loggers = new List<ILogger>();

        public static void AddLogger(ILogger logger) => loggers.Add(logger);
        public static void RemoveLogger(ILogger logger) => loggers.Remove(logger);

        public static void Debug(params object[] args)
        {
            foreach (var logger in loggers)
                logger.Debug(args);
        }
        public static void Info(params object[] args)
        {
            foreach (var logger in loggers)
                logger.Info(args);
        }
        public static void Warn(params object[] args)
        {
            foreach (var logger in loggers)
                logger.Warn(args); 
        }
        public static void Error(params object[] args)
        {
            foreach (var logger in loggers)
                logger.Error(args); 
        }
        public static void Fatal(params object[] args)
        {
            foreach (var logger in loggers)
                logger.Fatal(args); 
        }
    }
}
