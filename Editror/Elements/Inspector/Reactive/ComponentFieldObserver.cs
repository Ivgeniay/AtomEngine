using System.Reactive.Linq;
using System.Reflection;
using System.Linq;
using AtomEngine;
using EngineLib;
using System;

namespace Editor
{
    internal class ComponentFieldObserver<T> : IDisposable
    {
        private readonly SceneManager _sceneManager;
        private readonly EntityData? _entityData;
        private readonly FieldInfo? _fieldInfo;

        private readonly string? _componentKey;
        private readonly uint _entityId;

        private readonly Action<T> _uiUpdateAction;

        public ComponentFieldObserver(uint entityId, IComponent component, string fieldName, Action<T> uiUpdateAction)
        {
            _entityId = entityId;
            _uiUpdateAction = uiUpdateAction;
            _sceneManager = ServiceHub.Get<SceneManager>();

            var componentType = component.GetType();
            _componentKey = componentType.FullName;
            _fieldInfo = componentType.GetField(fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            _entityData = _sceneManager
                .CurrentScene
                .CurrentWorldData
                .Entities
                .FirstOrDefault(e => e.Id == _entityId);

            EditorUpdateSystem.DataUpdateRequested += ActionMethod;
        }

        private void ActionMethod(object? sender, EventArgs e)
        {
            _uiUpdateAction?.Invoke(GetFieldValue());
        }

        private T GetFieldValue()
        {
            if (_entityData.Components.TryGetValue(_componentKey, out var currentComponent))
                return default;

            return (T)_fieldInfo.GetValue(currentComponent);
        }

        public void SetValue(T value)
        {
            if (_entityData?.Components == null || !_entityData.Components.TryGetValue(_componentKey, out var currentComponent))
                return;

            _fieldInfo.SetValue(currentComponent, value);
            _entityData.Components[_componentKey] = currentComponent;
        }

        public void Dispose()
        {
            EditorUpdateSystem.DataUpdateRequested -= ActionMethod;
        }
    }
}
