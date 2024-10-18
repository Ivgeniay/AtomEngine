namespace AtomEngine.Diagnostic
{
    public enum LogLevel
    { 
        Information,
        Trace,
        Debug,
        Warning,
        Error,
        Critical,
    }

    [Flags]
    public enum LogLevelFlag
    {
        None = 0,
        Trace = 1 << 0,         // 1
        Debug = 1 << 1,         // 2
        Information = 1 << 2,   // 4
        Warning = 1 << 3,       // 8
        Error = 1 << 4,         // 16
        Critical = 1 << 5,      // 32
        All = ~None  
    }

    public class LogLevelFlagWrap
    {
        private LogLevelFlag logLevel = LogLevelFlag.All;

        public void AddLogLevel(LogLevel level) => logLevel |= ConvertToFlag(level);
        public void RemoveLogLevel(LogLevel level) => logLevel &= ~ConvertToFlag(level);
        public bool IsLogLevelEnabled(LogLevel level)
        {
            LogLevelFlag flag = ConvertToFlag(level);
            return (logLevel & flag) == flag;
        }
        public LogLevelFlag GetCurrentLogLevels() => logLevel;
        public void ToggleLogLevel(LogLevel level)
        {
            LogLevelFlag flag = ConvertToFlag(level);
            if ((logLevel & flag) != 0) logLevel &= ~flag; 
            else logLevel |= flag;
        }

        public void SetLogLevelAndBelow(LogLevel level)
        {
            logLevel = LogLevelFlag.None;
            for (int i = (int)LogLevel.Trace; i <= (int)level; i++) AddLogLevel((LogLevel)i);
        }
        private LogLevelFlag ConvertToFlag(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => LogLevelFlag.Trace,
                LogLevel.Debug => LogLevelFlag.Debug,
                LogLevel.Information => LogLevelFlag.Information,
                LogLevel.Warning => LogLevelFlag.Warning,
                LogLevel.Error => LogLevelFlag.Error,
                LogLevel.Critical => LogLevelFlag.Critical,
                _ => LogLevelFlag.None,
            };
        }

    }
}
