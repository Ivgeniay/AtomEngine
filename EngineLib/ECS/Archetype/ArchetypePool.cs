using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AtomEngine.Memory;

namespace AtomEngine
{
    /// <summary>
    /// Управляет хранением и организацией сущностей по архетипам.
    /// Предоставляет операции для добавления/удаления компонентов и перемещения сущностей между архетипами.
    /// </summary>
    public sealed partial class ArchetypePool : IDisposable
    {
        private readonly Dictionary<Archetype, ArchetypeData> _archetypeData = new();
        private readonly Dictionary<uint, EntityLocation> _entityLocations = new();

        private bool _isDisposed;


        public void AddEntityToArchetype(uint entityId, ReadOnlySpan<IComponent> components, Type[] componentTypes)
        {
            var newArchetype = new Archetype(componentTypes);

            if (_entityLocations.TryGetValue(entityId, out var oldLocation))
            {
                var oldData = _archetypeData[oldLocation.Archetype];
                oldData.RemoveEntity(entityId);
                if (oldData.Count == 0)
                {
                    oldData.Dispose();
                    _archetypeData.Remove(oldLocation.Archetype);
                }
            }

            var data = GetOrCreateArchetypeData(newArchetype);
            data.AddEntity(entityId, components, newArchetype);
            _entityLocations[entityId] = new EntityLocation(newArchetype, data.Count - 1);
        }

        public void RemoveEntity(uint entityId)
        {
            if (!_entityLocations.TryGetValue(entityId, out var location))
                return;

            var data = _archetypeData[location.Archetype];
            data.RemoveEntity(entityId);
            _entityLocations.Remove(entityId); 
            if (data.Count == 0)
            {
                data.Dispose();
                _archetypeData.Remove(location.Archetype);
            }
        }

        public ref T GetComponent<T>(uint entityId, int typeIndex) where T : struct, IComponent
        {
            if (!_entityLocations.TryGetValue(entityId, out var location))
                throw new KeyNotFoundException($"Entity {entityId} not found");

            var data = _archetypeData[location.Archetype];
            return ref data.GetComponent<T>(entityId, typeIndex);
        }

        private ArchetypeData GetOrCreateArchetypeData(Archetype archetype)
        {
            if (!_archetypeData.TryGetValue(archetype, out var data))
            {
                data = new ArchetypeData(archetype);
                _archetypeData[archetype] = data;
            }
            return data;
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            foreach (var data in _archetypeData.Values)
            {
                data.Dispose();
            }

            _archetypeData.Clear();
            _entityLocations.Clear();
            _isDisposed = true;
        }

        /// <summary>
        /// Описывает расположение сущности в конкретном архетипе
        /// </summary>
        private readonly struct EntityLocation
        {
            public readonly Archetype Archetype;    // Архетип, которому принадлежит сущность
            public readonly int Row;                // Индекс строки в данных архетипа

            public EntityLocation(Archetype archetype, int row)
            {
                Archetype = archetype;
                Row = row;
            }
        }


        /// <summary>
        /// Хранит данные всех сущностей, принадлежащих одному архетипу
        /// </summary>
        private sealed class ArchetypeData : IDisposable
        {
            private readonly Archetype _archetype;          // Добавляем архетип
            private MemoryBlock _componentData;             // Блок памяти для хранения компонентов
            private readonly List<uint> _entityIds;         // Список ID сущностей в архетипе
            private readonly int _rowSize;                  // Размер одной строки (всех компонентов сущности)
            private bool _isDisposed;

            public int Count => _entityIds.Count;
            public ReadOnlySpan<uint> GetEntitySpan() => CollectionsMarshal.AsSpan(_entityIds);
            public ReadOnlyMemory<uint> GetEntityMemory() => _entityIds.ToArray();
            public List<uint> EntityIds => _entityIds;

            public ArchetypeData(Archetype archetype, int initialCapacity = 64)
            {
                _archetype = archetype;
                _rowSize = archetype.GetTotalSize();
                _componentData = new MemoryBlock(_rowSize * initialCapacity);
                _entityIds = new List<uint>(initialCapacity);
            }

            // Добавляет новую сущность в архетип
            public unsafe void AddEntity(uint entityId, ReadOnlySpan<IComponent> components, Archetype archetype)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(ArchetypeData));

                if (components.Length != archetype.Metadata.Count)
                    throw new ArgumentException("Components count doesn't match archetype metadata count");

                // Проверяем необходимость увеличения буфера
                if (_entityIds.Count * _rowSize >= _componentData.Size)
                {
                    ResizeBuffer();
                }

                int row = _entityIds.Count;
                _entityIds.Add(entityId);

                // Копируем компоненты в память
                byte* rowPtr = (byte*)_componentData.GetPointer() + row * _rowSize;

                for (int i = 0; i < components.Length; i++)
                {
                    var metadata = archetype.Metadata[i];
                    var componentPtr = rowPtr + metadata.Offset;
                    Marshal.StructureToPtr(components[i], (IntPtr)componentPtr, false);
                }
            }

            // Удаляет сущность из архетипа
            public unsafe void RemoveEntity(uint entityId)
            {
                // Находим индекс сущности в списке
                int index = _entityIds.IndexOf(entityId);
                if (index == -1) return;

                int lastIndex = _entityIds.Count - 1;
                if (index != lastIndex)
                {
                    // Получаем указатели на память
                    byte* data = (byte*)_componentData.GetPointer();
                    byte* srcRow = data + lastIndex * _rowSize;
                    byte* dstRow = data + index * _rowSize;

                    // Копируем последнюю строку на место удаляемой
                    Buffer.MemoryCopy(srcRow, dstRow, _rowSize, _rowSize);

                    // Перемещаем последний ID на место удаляемого
                    _entityIds[index] = _entityIds[lastIndex];
                }

                // Удаляем последний элемент
                _entityIds.RemoveAt(lastIndex);
            }

            // Получает компонент для сущности по индексу типа
            public unsafe ref T GetComponent<T>(uint entityId, int typeIndex) where T : struct, IComponent
            {
                // Получаем индекс сущности в списке
                int entityIndex = _entityIds.IndexOf(entityId);
                if (entityIndex == -1)
                    throw new KeyNotFoundException($"Entity {entityId} not found");

                // Проверяем корректность индекса типа
                if (typeIndex < 0 || typeIndex >= _archetype.Metadata.Count)
                    throw new ArgumentOutOfRangeException(nameof(typeIndex));

                // Получаем метаданные компонента
                var metadata = _archetype.Metadata[typeIndex];
                if (metadata.Type != typeof(T))
                    throw new ArgumentException($"Type mismatch: expected {typeof(T)}, got {metadata.Type}");

                // Вычисляем указатель на компонент
                byte* rowPtr = (byte*)_componentData.GetPointer() + entityIndex * _rowSize;
                byte* componentPtr = rowPtr + metadata.Offset;

                return ref Unsafe.AsRef<T>(componentPtr);
            }

            private void ResizeBuffer()
            {
                var newSize = _componentData.Size * 2;
                var newBuffer = new MemoryBlock(newSize);

                _componentData.AsSpan().CopyTo(newBuffer.AsSpan());
                _componentData.Dispose();
                _componentData = newBuffer;
            }

            public void Dispose()
            {
                if (_isDisposed) return;
                _componentData.Dispose();
                _isDisposed = true;
            }
        }
    }

    public sealed partial class ArchetypePool : IDisposable
    {
        // Добавление компонента к сущности
        public void AddComponent<T>(uint entityId, T component) where T : struct, IComponent
        {
            if (!_entityLocations.TryGetValue(entityId, out var location))
                throw new KeyNotFoundException($"Entity {entityId} not found");

            // Получаем текущий архетип и его компоненты
            var currentArchetype = location.Archetype;
            var currentTypes = currentArchetype.Metadata.Select(m => m.Type).ToList();

            // Если компонент уже существует, обновляем его
            var componentType = typeof(T);
            var existingIndex = currentTypes.IndexOf(componentType);
            if (existingIndex != -1)
            {
                var data = _archetypeData[currentArchetype];
                ref var existingComponent = ref data.GetComponent<T>(entityId, existingIndex);
                existingComponent = component;
                return;
            }

            // Создаем новый список типов с добавленным компонентом
            currentTypes.Add(componentType);
            var newArchetype = new Archetype(currentTypes.ToArray());

            // Перемещаем сущность в новый архетип
            var components = GatherEntityComponents(entityId, location);
            var newComponents = components.Concat(new IComponent[] { component }).ToArray();

            MoveEntityToArchetype(entityId, location, newArchetype, newComponents);
        }

        // Удаление компонента у сущности
        public void RemoveComponent<T>(uint entityId) where T : struct, IComponent
        {
            if (!_entityLocations.TryGetValue(entityId, out var location))
                return;

            var currentArchetype = location.Archetype;
            var componentType = typeof(T);

            // Проверяем, есть ли компонент у сущности
            var currentTypes = currentArchetype.Metadata.Select(m => m.Type).ToList();
            var index = currentTypes.IndexOf(componentType);
            if (index == -1) return;

            // Если это последний компонент, удаляем всю сущность
            if (currentTypes.Count == 1)
            {
                RemoveEntity(entityId);
                return;
            }

            // Создаем новый архетип без удаляемого компонента
            currentTypes.RemoveAt(index);
            var newArchetype = new Archetype(currentTypes.ToArray());

            // Собираем все компоненты кроме удаляемого
            var components = GatherEntityComponents(entityId, location)
                .Where((_, i) => i != index)
                .ToArray();

            MoveEntityToArchetype(entityId, location, newArchetype, components);
        }

        // Перемещение сущности между архетипами
        private void MoveEntityToArchetype(uint entityId, EntityLocation currentLocation,
            Archetype newArchetype, IComponent[] components)
        {
            // Получаем или создаем данные для нового архетипа
            var newData = GetOrCreateArchetypeData(newArchetype);

            // Добавляем сущность в новый архетип
            newData.AddEntity(entityId, components, newArchetype);

            // Удаляем из старого архетипа
            var oldData = _archetypeData[currentLocation.Archetype];
            oldData.RemoveEntity(entityId);

            // Обновляем локацию сущности
            _entityLocations[entityId] = new EntityLocation(newArchetype, newData.Count - 1);
        }

        // Собирает все компоненты сущности из текущего архетипа
        private IComponent[] GatherEntityComponents(uint entityId, EntityLocation location)
        {
            var archetype = location.Archetype;
            var data = _archetypeData[archetype];
            var components = new IComponent[archetype.Metadata.Count];

            for (int i = 0; i < archetype.Metadata.Count; i++)
            {
                var metadata = archetype.Metadata[i];
                var method = typeof(ArchetypeData)
                    .GetMethod(nameof(ArchetypeData.GetComponent))!
                    .MakeGenericMethod(metadata.Type);

                components[i] = (IComponent)method.Invoke(data, new object[] { entityId, i })!;
            }

            return components;
        }

        // Методы для итерации по сущностям архетипа
        public ReadOnlySpan<uint> GetEntities(Archetype archetype)
        {
            if (_archetypeData.TryGetValue(archetype, out var data))
            {
                return data.GetEntitySpan();
            }
            return ReadOnlySpan<uint>.Empty;
        }

        public IEnumerable<(uint EntityId, Archetype Archetype)> GetEntitiesWithComponents(Type[] componentTypes)
        {
            var targetArchetype = new Archetype(componentTypes);

            foreach (var (archetype, data) in _archetypeData)
            {
                bool hasAllComponents = true;
                foreach (var type in componentTypes)
                {
                    if (archetype.GetComponentIndex(type) == -1)
                    {
                        hasAllComponents = false;
                        break;
                    }
                }

                if (hasAllComponents)
                {
                    foreach (var entityId in data.EntityIds)
                    {
                        yield return (entityId, archetype);
                    }
                }
            }
        }

    }

    public sealed partial class ArchetypePool : IDisposable
    {
        /// Методы для поиска Entities с точным совпадением архетипов 
        public ReadOnlySpan<uint> GetEntitiesWith<T>() where T : struct, IComponent
        {
            var archetype = new Archetype(new[] { typeof(T) });
            return GetEntitiesForArchetype(archetype);
        }

        public ReadOnlySpan<uint> GetEntitiesWith<T1, T2>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            var archetype = new Archetype(new[] { typeof(T1), typeof(T2) });
            return GetEntitiesForArchetype(archetype);
        }

        public ReadOnlySpan<uint> GetEntitiesWith<T1, T2, T3>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
        {
            var archetype = new Archetype(new[] { typeof(T1), typeof(T2), typeof(T3) });
            return GetEntitiesForArchetype(archetype);
        }

        public ReadOnlySpan<uint> GetEntitiesWith<T1, T2, T3, T4>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
        {
            var archetype = new Archetype(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4) });
            return GetEntitiesForArchetype(archetype);
        }

        public ReadOnlySpan<uint> GetEntitiesWith<T1, T2, T3, T4, T5>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
        {
            var archetype = new Archetype(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) });
            return GetEntitiesForArchetype(archetype);
        }

        public ReadOnlySpan<uint> GetEntitiesWith<T1, T2, T3, T4, T5, T6>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
        {
            var archetype = new Archetype(new[] { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6) });
            return GetEntitiesForArchetype(archetype);
        }

        private ReadOnlySpan<uint> GetEntitiesForArchetype(Archetype archetype)
        {
            if (_archetypeData.TryGetValue(archetype, out var data))
            {
                return data.GetEntitySpan();
            }
            return ReadOnlySpan<uint>.Empty;
        }
    }

    public sealed partial class ArchetypePool : IDisposable
    {
        /// Поиск Entities, которые имеют указанные компоненты
        public IEnumerable<uint> GetEntitiesHaving<T>() where T : struct, IComponent
        {
            var componentType = typeof(T);
            foreach (var (archetype, data) in _archetypeData)
            {
                if (archetype.HasComponent(componentType))
                {
                    foreach (var entityId in data.EntityIds)
                    {
                        yield return entityId;
                    }
                }
            }
        }

        public IEnumerable<uint> GetEntitiesHaving<T1, T2>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            return GetEntitiesHaving<T1>()
                .Intersect(GetEntitiesHaving<T2>());
        }

        public IEnumerable<uint> GetEntitiesHaving<T1, T2, T3>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
        {
            return GetEntitiesHaving<T1>()
                .Intersect(GetEntitiesHaving<T2>())
                .Intersect(GetEntitiesHaving<T3>());
        }

        public IEnumerable<uint> GetEntitiesHaving<T1, T2, T3, T4>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
        {
            return GetEntitiesHaving<T1>()
                .Intersect(GetEntitiesHaving<T2>())
                .Intersect(GetEntitiesHaving<T3>())
                .Intersect(GetEntitiesHaving<T4>());
        }

        public IEnumerable<uint> GetEntitiesHaving<T1, T2, T3, T4, T5>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
        {
            return GetEntitiesHaving<T1>()
                .Intersect(GetEntitiesHaving<T2>())
                .Intersect(GetEntitiesHaving<T3>())
                .Intersect(GetEntitiesHaving<T4>())
                .Intersect(GetEntitiesHaving<T5>());
        }

        public IEnumerable<uint> GetEntitiesHaving<T1, T2, T3, T4, T5, T6>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
        {
            return GetEntitiesHaving<T1>()
                .Intersect(GetEntitiesHaving<T2>())
                .Intersect(GetEntitiesHaving<T3>())
                .Intersect(GetEntitiesHaving<T4>())
                .Intersect(GetEntitiesHaving<T5>())
                .Intersect(GetEntitiesHaving<T6>());
        }
    }
}
