using System.Reflection;

namespace Editor
{
    internal class AssemblyUploadEvent : EventHubEvent
    {
        public Assembly Assembly;
    }
}
