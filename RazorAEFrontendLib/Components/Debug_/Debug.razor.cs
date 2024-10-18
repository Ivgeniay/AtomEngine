using AtomEngine.Diagnostic;
using AtomEngineEditor.Services;
using Microsoft.AspNetCore.Components;

namespace AtomEngineEditor.Components
{
    public partial class DebugComponentBase : ComponentBase
    {
        [Inject] public IConsoleService Console { get; set; }
        protected int RequestId { get; set; } = 0;
        protected void OnClick()
        {
            RequestId += 1;
            LogLevel level = LogLevel.Information;
            if (RequestId % 3 == 0)
            {
                level = LogLevel.Warning;
            }
            else if (RequestId % 5 == 0)
            {
                level = LogLevel.Error;
            }

            Console.Log($"Request {RequestId} sent", level);
        }
    }
}