using System.Reflection;

namespace EngineLib
{
    public class AssemblyUnloadEvent : EventHubEvent
    {
        public Assembly Assembly;
    }
}
