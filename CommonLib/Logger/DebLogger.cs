
public static class DebLogger
{
    public static ILogger logger { get; set; }

    public static void Debug(params object[] args) => logger?.Debug(args);
    public static void Info(params object[] args) => logger?.Info(args);
    public static void Warm(params object[] args) => logger?.Warm(args);
    public static void Error(params object[] args) => logger?.Error(args);
    public static void Fatal(params object[] args) => logger?.Fatal(args);
}
