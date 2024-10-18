using AtomEngine.Diagnostic;

namespace AtomEngineEditor.Services.Console
{
    public class ConsoleService : IConsoleService
    {
        public bool IsEnabled { get => isEnable; set => isEnable = value; }
        private bool isEnable = true;
        private readonly List<IConsoleAgent> _agents = new List<IConsoleAgent>();

        public void RegisterConsoleAgent(IConsoleAgent agent)
        {
            if (!_agents.Contains(agent))
            {
                _agents.Add(agent);
            }
            else
            {
                throw new InvalidOperationException("Agent already registered.");
            }
        }
        public void UnRegisterConsoleAgent(IConsoleAgent agent)
        {
            if (_agents.Contains(agent))
            {
                _agents.Remove(agent);
            }
            else
            {
                throw new InvalidOperationException("Agent not registered.");
            }
        }
        public void Enable(bool enable) => IsEnabled = enable; 
        public void Log(string message, LogLevel logLevel = LogLevel.Information) => Log(logLevel, message); 
        public void Log(LogLevel logLevel, string message) => Post(logLevel, message);
        private void Post(LogLevel logLevel, string message)
        {
            if (!IsEnabled) return;
            foreach (var agent in _agents)
            { 
                agent.Log(new LogMessage() { LogLevel = logLevel, Message = message }); 
                //agent.Log($"{DateTime.Now} [{logLevel}]: {message}"); 
            }
        }
    }

    public class LogMessage
    {
        public DateTime Time { get; set; } = DateTime.Now;
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
        public string Message { get; set; } = string.Empty;
    }
    
}
