[Flags]
public enum LogLevel
{
    None = 0,
    Debug = 1 << 0,    
    Info = 1 << 1,     
    Warn = 1 << 2,     
    Error = 1 << 3,    
    Fatal = 1 << 4,    
    All = Debug | Info | Warn | Error | Fatal 
}