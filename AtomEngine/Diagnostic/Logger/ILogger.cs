namespace AtomEngine.Diagnostic
{
    public interface ILogger
    {
        public bool IsEnabled { get; }
        public void Enable(bool enable);
        public void Log(LogLevel logLevel, string message);
        public void Log(string message, LogLevel logLevel = LogLevel.Information);
    }
}
