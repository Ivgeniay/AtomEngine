using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EngineLib.Memory;

namespace EngineLib
{
    /// <summary>
    /// Управляет хранением и организацией сущностей по архетипам.
    /// Предоставляет операции для добавления/удаления компонентов и перемещения сущностей между архетипами.
    /// </summary>
    public sealed partial class ArchetypePool : IDisposable
    {
        // Хранит все существующие архетипы и их данные
        private readonly Dictionary<Archetype, ArchetypeData> _archetypeData = new();

        // Отслеживает, к какому архетипу принадлежит каждая сущность
        private readonly Dictionary<uint, EntityLocation> _entityLocations = new();

        private bool _isDisposed;


        public void AddEntityToArchetype(uint entityId, ReadOnlySpan<IComponent> components, Type[] componentTypes)
        {
            var archetype = new Archetype(componentTypes);
            var data = GetOrCreateArchetypeData(archetype);

            // Если сущность уже где-то есть, перемещаем её
            if (_entityLocations.TryGetValue(entityId, out var location))
            {
                MoveEntityToArchetype(entityId, location, archetype);
            }

            data.AddEntity(entityId, components, archetype);
            _entityLocations[entityId] = new EntityLocation(archetype, data.Count - 1);
        }

        public void RemoveEntity(uint entityId)
        {
            if (!_entityLocations.TryGetValue(entityId, out var location))
                return;

            var data = _archetypeData[location.Archetype];
            data.RemoveEntity(entityId);
            _entityLocations.Remove(entityId);
        }

        public ref T GetComponent<T>(uint entityId, int typeIndex) where T : struct, IComponent
        {
            if (!_entityLocations.TryGetValue(entityId, out var location))
                throw new KeyNotFoundException($"Entity {entityId} not found");

            var data = _archetypeData[location.Archetype];
            return ref data.GetComponent<T>(entityId, typeIndex);
        }

        private void MoveEntityToArchetype(uint entityId, EntityLocation currentLocation, Archetype newArchetype)
        {
            // TODO: Реализовать перемещение сущности между архетипами
            // 1. Собрать все компоненты из текущего архетипа
            // 2. Добавить сущность в новый архетип
            // 3. Удалить сущность из старого архетипа
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
            private readonly List<uint> _entityIds;         // Список ID сущностей
            private readonly Dictionary<uint, int> _entityToRow;  // Маппинг ID сущности -> индекс строки
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
                _entityToRow = new Dictionary<uint, int>(initialCapacity);
            }

            // Добавляет новую сущность в архетип
            public unsafe void AddEntity(uint entityId, ReadOnlySpan<IComponent> components, Archetype archetype)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(ArchetypeData));

                // Проверяем необходимость увеличения буфера
                if (_entityIds.Count * _rowSize >= _componentData.Size)
                {
                    ResizeBuffer();
                }

                int row = _entityIds.Count;
                _entityIds.Add(entityId);
                _entityToRow[entityId] = row;

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
                if (!_entityToRow.TryGetValue(entityId, out int row))
                    return;

                int lastRow = _entityIds.Count - 1;
                if (row != lastRow)
                {
                    // Перемещаем последнюю сущность на место удаляемой
                    uint lastEntityId = _entityIds[lastRow];

                    byte* sourcePtr = (byte*)_componentData.GetPointer() + lastRow * _rowSize;
                    byte* destPtr = (byte*)_componentData.GetPointer() + row * _rowSize;
                    Buffer.MemoryCopy(sourcePtr, destPtr, _rowSize, _rowSize);

                    _entityIds[row] = lastEntityId;
                    _entityToRow[lastEntityId] = row;
                }

                _entityIds.RemoveAt(lastRow);
                _entityToRow.Remove(entityId);
            }

            // Получает компонент для сущности по индексу типа
            public unsafe ref T GetComponent<T>(uint entityId, int typeIndex) where T : struct, IComponent
            {
                if (!_entityToRow.TryGetValue(entityId, out int row))
                    throw new KeyNotFoundException($"Entity {entityId} not found");

                byte* rowPtr = (byte*)_componentData.GetPointer() + row * _rowSize;
                byte* componentPtr = rowPtr + _archetype.Metadata[typeIndex].Offset;
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
        public IEnumerable<ReadOnlyMemory<uint>> GetEntitiesHaving<T>() where T : struct, IComponent
        {
            var componentType = typeof(T);
            foreach (var (archetype, data) in _archetypeData)
            {
                if (archetype.HasComponent(componentType))
                {
                    yield return data.GetEntityMemory();
                }
            }
        }

        public IEnumerable<ReadOnlyMemory<uint>> GetEntitiesHaving<T1, T2>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            var type1 = typeof(T1);
            var type2 = typeof(T2);

            foreach (var (archetype, data) in _archetypeData)
            {
                if (archetype.HasComponent(type1) &&
                    archetype.HasComponent(type2))
                {
                    yield return data.GetEntityMemory();
                }
            }
        }

        public IEnumerable<ReadOnlyMemory<uint>> GetEntitiesHaving<T1, T2, T3>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
        {
            var type1 = typeof(T1);
            var type2 = typeof(T2);
            var type3 = typeof(T3);

            foreach (var (archetype, data) in _archetypeData)
            {
                if (archetype.HasComponent(type1) &&
                    archetype.HasComponent(type2) &&
                    archetype.HasComponent(type3))
                {
                    yield return data.GetEntityMemory();
                }
            }
        }

        public IEnumerable<ReadOnlyMemory<uint>> GetEntitiesHaving<T1, T2, T3, T4>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
        {
            var type1 = typeof(T1);
            var type2 = typeof(T2);
            var type3 = typeof(T3);
            var type4 = typeof(T4);

            foreach (var (archetype, data) in _archetypeData)
            {
                if (archetype.HasComponent(type1) &&
                    archetype.HasComponent(type2) &&
                    archetype.HasComponent(type3) &&
                    archetype.HasComponent(type4))
                {
                    yield return data.GetEntityMemory();
                }
            }
        }

        public IEnumerable<ReadOnlyMemory<uint>> GetEntitiesHaving<T1, T2, T3, T4, T5>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
        {
            var type1 = typeof(T1);
            var type2 = typeof(T2);
            var type3 = typeof(T3);
            var type4 = typeof(T4);
            var type5 = typeof(T5);

            foreach (var (archetype, data) in _archetypeData)
            {
                if (archetype.HasComponent(type1) &&
                    archetype.HasComponent(type2) &&
                    archetype.HasComponent(type3) &&
                    archetype.HasComponent(type4) &&
                    archetype.HasComponent(type5))
                {
                    yield return data.GetEntityMemory();
                }
            }
        }

        public IEnumerable<ReadOnlyMemory<uint>> GetEntitiesHaving<T1, T2, T3, T4, T5, T6>()
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
        {
            var type1 = typeof(T1);
            var type2 = typeof(T2);
            var type3 = typeof(T3);
            var type4 = typeof(T4);
            var type5 = typeof(T5);
            var type6 = typeof(T6);

            foreach (var (archetype, data) in _archetypeData)
            {
                if (archetype.HasComponent(type1) &&
                    archetype.HasComponent(type2) &&
                    archetype.HasComponent(type3) &&
                    archetype.HasComponent(type4) &&
                    archetype.HasComponent(type5) &&
                    archetype.HasComponent(type6))
                {
                    yield return data.GetEntityMemory();
                }
            }
        }
    }
}
