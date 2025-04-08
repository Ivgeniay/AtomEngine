using AtomEngine;
using CommonLib;

public abstract class Error: Exception
{
    public Error(string message = "") : base(message)
    {
        DebLogger.Error($"Error type {this.GetType().Name}.\n Message: {message}");
        DebLogger.Error($"StackTrace: {StackTraceHelper. GetStackTrace(10)}");
    }

}
