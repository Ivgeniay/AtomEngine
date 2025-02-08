using System.Runtime.InteropServices;

namespace AtomEngine
{
    /// <summary>
    /// Организованное хранение архетипов сущностей.
    /// 
    /// var entity = world.CreateEntity();
    /// world.AddComponent(entity, new TransformComponent());
    /// world.AddComponent(entity, new MeshComponent());
    /// После второго добавления компонента сущность перемещается в новый архетип:
    /// Archetype([typeof(TransformComponent)]): { },
    /// Сущность перемещена в новый архетип
    /// Archetype([typeof(TransformComponent), typeof(MeshComponent)]): { entityId: entityData }
    /// </summary>
    public readonly struct Archetype : IEquatable<Archetype>
    {
        private readonly ComponentMetadata[] _metadata;
        private readonly int _hash;

        public IReadOnlyList<ComponentMetadata> Metadata => _metadata;

        public Archetype(Type[] componentTypes)
        {
            if (componentTypes == null)
                throw new NullValueError(nameof(componentTypes));

            // Сортируем типы для обеспечения уникальности порядка
            Type[] sortedTypes = componentTypes.OrderBy(t => t.FullName).ToArray();

            // Проверяем корректность типов
            ValidateComponentTypes(sortedTypes);

            // Создаем метаданные для каждого компонента
            _metadata = new ComponentMetadata[sortedTypes.Length];

            int offset = 0;
            for (int i = 0; i < sortedTypes.Length; i++)
            {
                var type = sortedTypes[i];
                var size = Marshal.SizeOf(type);
                var alignment = GetRequiredAlignment(size);

                offset = AlignOffset(offset, alignment);

                _metadata[i] = new ComponentMetadata(type, offset, size, alignment);
                offset += size;
            }

            // Вычисляем хэш архетипа
            _hash = ComputeHash(sortedTypes);
        }

        public readonly struct ComponentMetadata
        {
            public readonly Type Type;       // Тип компонента
            public readonly int Offset;      // Смещение в памяти
            public readonly int Size;        // Размер в байтах
            public readonly int Alignment;   // Требуемое выравнивание

            public ComponentMetadata(Type type, int offset, int size, int alignment)
            {
                Type = type;
                Offset = offset;
                Size = size;
                Alignment = alignment;
            }
        }

        public int GetTotalSize()
        {
            if (_metadata.Length == 0) return 0;
            var lastMeta = _metadata[^1];
            return AlignOffset(lastMeta.Offset + lastMeta.Size, 8); // Выравниваем по 8 байт
        }

        public bool HasComponent(Type componentType)
        {
            return _metadata.Any(m => m.Type == componentType);
        }

        public int GetComponentIndex(Type componentType)
        {
            for (int i = 0; i < _metadata.Length; i++)
            {
                if (_metadata[i].Type == componentType)
                    return i;
            }
            return -1;
        }

        private static void ValidateComponentTypes(Type[] types)
        {
            foreach (var type in types)
            {
                if (!type.IsValueType || !typeof(IComponent).IsAssignableFrom(type))
                    throw new ArgumentException($"Type {type} must be a value type implementing IComponent");
            }

            if (types.Length != types.Distinct().Count())
                throw new ArgumentException("Duplicate component types are not allowed");
        }

        private static int GetRequiredAlignment(int size)
        {
            if (size <= 1) return 1;
            if (size <= 2) return 2;
            if (size <= 4) return 4;
            return 8;
        }

        private static int AlignOffset(int offset, int alignment)
        {
            return (offset + (alignment - 1)) & ~(alignment - 1);
        }

        private static int ComputeHash(Type[] types)
        {
            var hash = 17;
            foreach (var type in types)
            {
                hash = hash * 31 + type.GetHashCode();
            }
            return hash;
        }

        public override int GetHashCode() => _hash;

        public bool Equals(Archetype other)
        {
            if (_metadata.Length != other._metadata.Length)
                return false;

            for (int i = 0; i < _metadata.Length; i++)
            {
                if (_metadata[i].Type != other._metadata[i].Type)
                    return false;
            }

            return true;
        }

        public override bool Equals(object? obj)
            => obj is Archetype other && Equals(other);

        public static bool operator ==(Archetype left, Archetype right)
            => left.Equals(right);

        public static bool operator !=(Archetype left, Archetype right)
            => !left.Equals(right);
    }
}