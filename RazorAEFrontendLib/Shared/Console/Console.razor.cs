using AtomEngineEditor.Services.Console;
using Microsoft.AspNetCore.Components;
using AtomEngineEditor.Services;
using Microsoft.AspNetCore.Components.Web;
using AtomEngine.Diagnostic;

namespace AtomEngineEditor.Shared
{
    public partial class ConsoleBase : ComponentBase, IConsoleAgent, IDisposable
    {
        [Inject] protected IConsoleService Console { get; set; } 
        protected List<LogMessage> consoleMessages = new List<LogMessage>();
        protected LogLevelFlagWrap Level = new LogLevelFlagWrap();

        public void Log(LogMessage message)
        {
            consoleMessages.Insert(0, message);
            StateHasChanged();
        }

        protected override void OnInitialized()
        {
            Console.RegisterConsoleAgent(this);
        }

        protected void ToggleInfo() => Toggle(LogLevel.Information);
        protected void ToggleDebug() => Toggle(LogLevel.Debug);
        protected void ToggleWarning() => Toggle(LogLevel.Warning); 
        protected void ToggleTrace() => Toggle(LogLevel.Trace);
        protected void ToggleError() => Toggle(LogLevel.Error);
        protected void ToggleCritical() => Toggle(LogLevel.Critical); 

        protected void Toggle(LogLevel level)
        {
            Level.ToggleLogLevel(level);
            StateHasChanged();
        }

        public void Dispose()
        {
            Console.UnRegisterConsoleAgent(this);
        }

        protected void ClearConsole(MouseEventArgs e)
        {
            consoleMessages.Clear();
            StateHasChanged();
        }
    }


}
