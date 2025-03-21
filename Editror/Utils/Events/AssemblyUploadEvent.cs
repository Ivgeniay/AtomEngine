using System.Reflection;
using EngineLib;

namespace Editor
{
    internal class AssemblyUploadEvent : EventHubEvent
    {
        public Assembly Assembly;
    }
}
