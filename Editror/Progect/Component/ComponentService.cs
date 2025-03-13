using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Editor
{
    internal class ComponentService : IService
    {
        private EditorAssemblyManager _assemblyManager;
        public List<Type> _componentTypes = new List<Type>();

        public Task InitializeAsync()
        {
            _assemblyManager = ServiceHub.Get<EditorAssemblyManager>();
            _assemblyManager.OnUserScriptAsseblyRebuild += RebuildUserScrAssembly;
            return Task.CompletedTask;
        }

        public IEnumerable<Type> GetComponentTypes()
        {
            if (_componentTypes.Count == 0) _componentTypes = _assemblyManager.FindTypesByInterface<AtomEngine.IComponent>().ToList();
            foreach (var type in _componentTypes)
            {
                yield return type;
            }
        }

        private void RebuildUserScrAssembly()
        {
            _componentTypes = _assemblyManager.FindTypesByInterface<AtomEngine.IComponent>().ToList();
        }
    }
}
