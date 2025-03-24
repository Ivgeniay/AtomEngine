using AtomEngine;
using System.Diagnostics;
using System.Text;

public abstract class Error: Exception
{
    public Error(string message = "") : base(message)
    {
        DebLogger.Error($"Error type {this.GetType().Name}.\n Message: {message}");
        DebLogger.Error($"StackTrace: {GetStackTrace(10)}");
    }

    public static string GetStackTrace(int frames = 8)
    {
        var stackTrace = new StackTrace(true);
        var count = Math.Min(frames, stackTrace.FrameCount);
        var sb = new StringBuilder();

        for (int i = 2; i < count; i++)
        {
            var frame = stackTrace.GetFrame(i);
            var method = frame.GetMethod();
            var fileName = frame.GetFileName();
            var lineNumber = frame.GetFileLineNumber();

            sb.AppendLine($"at {method.DeclaringType?.FullName}.{method.Name}");
            if (!string.IsNullOrEmpty(fileName))
            {
                sb.AppendLine($"    in {fileName}:line {lineNumber}");
            }
        }

        return sb.ToString();
    }
}
