[Flags]
public enum LogLevel
{
    None = 0,
    Debug = 1 << 0,
    Info = 2 << 0,
    Warn = 3 << 0,
    Error = 4 << 0,
    Fatal = 5 << 0,
    All = Debug | Info | Warn | Error | Fatal,
}
