using System.Reflection;

namespace EngineLib
{
    public class AssemblyUploadEvent : EventHubEvent
    {
        public Assembly Assembly;
    }
}
