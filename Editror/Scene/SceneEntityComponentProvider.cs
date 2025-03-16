using AtomEngine;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Editor
{
    internal class SceneEntityComponentProvider : IEntityComponentInfoProvider
    {
        private SceneManager _sceneManager;

        public SceneEntityComponentProvider(SceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }

        public unsafe ref T GetComponent<T>(uint entityId) where T : struct, IComponent
        {
            Type type = typeof(T);
            var component = _sceneManager
                    .CurrentScene
                    .CurrentWorldData
                    .Entities
                    .First(e => e.Id == entityId)
                    .Components
                    .FirstOrDefault(e => e.Value.GetType() == type).Value;
            return ref Unsafe.Unbox<T>(component);
        }
        public bool HasComponent<T>(uint entityId) where T : struct, IComponent
        {
            Type type = typeof(T);
            var entityData = _sceneManager
                    .CurrentScene
                    .CurrentWorldData
                    .Entities
                    .FirstOrDefault(e => e.Id == entityId);
            if (entityData != null)
                return entityData.Components.Any(e => e.Value.GetType() == type);

            return false;
        }
    }
}
