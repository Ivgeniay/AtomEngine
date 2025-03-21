using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using EngineLib;
using System;

namespace Editor
{
    internal class ComponentService : IService, ICacheble
    {
        private EditorAssemblyManager _assemblyManager;
        public List<Type> _componentTypes = new List<Type>();

        public Task InitializeAsync()
        {
            _assemblyManager = ServiceHub.Get<EditorAssemblyManager>();
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

        internal void RebuildUserScrAssembly()
        {
            _componentTypes = _assemblyManager.FindTypesByInterface<AtomEngine.IComponent>().ToList();
        }

        public void FreeCache()
        {
            _componentTypes.Clear();
            _componentTypes = null;
            _componentTypes = new List<Type>();
        }

    }
}
