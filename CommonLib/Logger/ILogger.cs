public interface ILogger
{
    public LogLevel LogLevel { get; set; }
    public void Log(string message, LogLevel logLevel);
    public void Debug(params object[] args) => Log(string.Join(" ", args), LogLevel.Debug);
    public void Info(params object[] args) => Log(string.Join(" ", args), LogLevel.Info);
    public void Warm(params object[] args) => Log(string.Join(" ", args), LogLevel.Warn);
    public void Error(params object[] args) => Log(string.Join(" ", args), LogLevel.Error);
    public void Fatal(params object[] args) => Log(string.Join(" ", args), LogLevel.Fatal);
    public void SetMaxLevelFilter(LogLevel logLevel)
    {
        LogLevel = logLevel == LogLevel.None ? LogLevel.None : (LogLevel)((int)logLevel * 2 - 1);
    }
    public void EnableLevel(LogLevel logLevel)
    {
        LogLevel |= logLevel;
    }
    public void DisableLevel(LogLevel logLevel)
    {
        LogLevel &= ~logLevel;
    }
}
