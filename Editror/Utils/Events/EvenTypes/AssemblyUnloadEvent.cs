using System.Reflection;

namespace Editor
{
    internal class AssemblyUnloadEvent : EventHubEvent
    {
        public Assembly Assembly;
    }
}
