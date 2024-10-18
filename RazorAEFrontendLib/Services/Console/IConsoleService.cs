using AtomEngine.Diagnostic;
using AtomEngineEditor.Services.Console;

namespace AtomEngineEditor.Services
{
    public interface IConsoleService: ILogger
    {
        public void RegisterConsoleAgent(IConsoleAgent agent);
        public void UnRegisterConsoleAgent(IConsoleAgent agent);
    }
}
