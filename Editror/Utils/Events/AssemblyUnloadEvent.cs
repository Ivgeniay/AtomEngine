using System.Reflection;
using EngineLib;

namespace Editor
{
    internal class AssemblyUnloadEvent : EventHubEvent
    {
        public Assembly Assembly;
    }
}
