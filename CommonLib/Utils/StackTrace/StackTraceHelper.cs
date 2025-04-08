using System.Diagnostics;
using System.Text;

namespace CommonLib
{
    public class StackTraceHelper
    {
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

        public static CallerInfo GetCallerInfo(int skipFrames = 2)
        {
            var stackFrame = new StackFrame(skipFrames, true);
            var method = stackFrame.GetMethod();

            return new CallerInfo
            {
                FilePath = stackFrame.GetFileName(),
                LineNumber = stackFrame.GetFileLineNumber(),
                MethodName = method?.Name,
                TypeName = method?.DeclaringType?.FullName
            };
        }

    }
    public struct CallerInfo
    {
        public string FilePath;
        public int LineNumber;
        public string MethodName;
        public string TypeName;

        public string FullMethodName => string.IsNullOrEmpty(TypeName)
            ? MethodName
            : $"{TypeName}.{MethodName}";

        public bool IsValid => !string.IsNullOrEmpty(FilePath) && LineNumber > 0;
    }
    
}
