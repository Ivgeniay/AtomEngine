using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;
using System;

namespace Editor
{
    internal class Scene
    {
        // Идентификатор сцены
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();

        // Версия формата сцены
        public string Version { get; set; } = "1.0";

        // Имя сцены
        public string Name { get; set; } = "Untitled Scene";

        // Путь к файлу сцены
        public string ScenePath { get; set; } = string.Empty;

        // Время последнего изменения
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        // Список миров в сцене
        public List<WorldData> Worlds { get; set; } = new List<WorldData>();

        // GUID текущего выбранного мира
        public string CurrentWorldGuid { get; set; }

        // Список зависимостей (ресурсы, используемые в сцене)
        public List<AssetDependency> Dependencies { get; set; } = new List<AssetDependency>();

        // Настройки хранения сцены
        public SceneConfiguration Configuration { get; set; } = new SceneConfiguration();

        // Кэш экземпляров миров - не сериализуется
        [JsonIgnore]
        private Dictionary<string, World> _worldInstances = new Dictionary<string, World>();

        // Блокировка для многопоточного доступа
        [JsonIgnore]
        private readonly object _worldLock = new object();

        // Свойство "Грязный" (есть несохраненные изменения)
        [JsonIgnore]
        public bool IsDirty { get; private set; }

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public Scene()
        {
            // Создание мира по умолчанию
            var defaultWorld = new WorldData
            {
                Name = "World_0"
            };

            Worlds.Add(defaultWorld);
            CurrentWorldGuid = defaultWorld.Guid;
        }

        /// <summary>
        /// Конструктор с конфигурацией
        /// </summary>
        public Scene(SceneConfiguration configuration) : this()
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Получить текущий мир
        /// </summary>
        [JsonIgnore]
        public WorldData CurrentWorld
        {
            get
            {
                lock (_worldLock)
                {
                    if (string.IsNullOrEmpty(CurrentWorldGuid) && Worlds.Count > 0)
                    {
                        CurrentWorldGuid = Worlds.First().Guid;
                    }

                    return Worlds.FirstOrDefault(w => w.Guid == CurrentWorldGuid);
                }
            }
        }

        /// <summary>
        /// Получить или создать экземпляр мира для ECS
        /// </summary>
        public World GetWorldInstance(string worldGuid)
        {
            lock (_worldLock)
            {
                if (_worldInstances.TryGetValue(worldGuid, out var instance))
                {
                    return instance;
                }

                var worldData = Worlds.FirstOrDefault(w => w.Guid == worldGuid);
                if (worldData == null)
                {
                    return null;
                }

                // Создаем новый экземпляр мира
                var world = new World();

                // Восстанавливаем сущности в мире
                foreach (var entity in worldData.Entities)
                {
                    // Создаем сущность с тем же ID и версией
                    var ecsEntity = world.CreateEntityWithId(entity.Id, entity.Version);

                    // Здесь можно было бы восстановить компоненты сущности
                    // ...
                }

                _worldInstances[worldGuid] = world;
                return world;
            }
        }

        /// <summary>
        /// Создать новый мир
        /// </summary>
        public WorldData CreateWorld(string worldName)
        {
            var worldData = new WorldData
            {
                Name = worldName
            };

            Worlds.Add(worldData);

            MakeDirty();
            return worldData;
        }

        /// <summary>
        /// Удалить мир
        /// </summary>
        public void RemoveWorld(string worldGuid)
        {
            var world = Worlds.FirstOrDefault(w => w.Guid == worldGuid);
            if (world != null)
            {
                Worlds.Remove(world);
                _worldInstances.Remove(worldGuid);

                // Если удаляем текущий мир, переключаемся на другой
                if (CurrentWorldGuid == worldGuid && Worlds.Count > 0)
                {
                    CurrentWorldGuid = Worlds.First().Guid;
                }

                MakeDirty();
            }
        }

        /// <summary>
        /// Выбрать мир
        /// </summary>
        public void SelectWorld(string worldGuid)
        {
            if (Worlds.Any(w => w.Guid == worldGuid))
            {
                CurrentWorldGuid = worldGuid;
            }
        }

        /// <summary>
        /// Переименовать мир
        /// </summary>
        public void RenameWorld(string worldGuid, string newName)
        {
            var world = Worlds.FirstOrDefault(w => w.Guid == worldGuid);
            if (world != null)
            {
                world.Name = newName;
                MakeDirty();
            }
        }

        /// <summary>
        /// Пометить сцену как измененную
        /// </summary>
        public void MakeDirty()
        {
            IsDirty = true;
            LastModified = DateTime.UtcNow;
        }

        /// <summary>
        /// Пометить сцену как сохраненную
        /// </summary>
        public void MakeClean()
        {
            IsDirty = false;
        }

        /// <summary>
        /// Добавить зависимость от ресурса
        /// </summary>
        public void AddDependency(string assetGuid, string assetType)
        {
            if (!Dependencies.Any(d => d.AssetGuid == assetGuid))
            {
                Dependencies.Add(new AssetDependency
                {
                    AssetGuid = assetGuid,
                    AssetType = assetType
                });

                MakeDirty();
            }
        }

        /// <summary>
        /// Удалить зависимость от ресурса
        /// </summary>
        public void RemoveDependency(string assetGuid)
        {
            var dependency = Dependencies.FirstOrDefault(d => d.AssetGuid == assetGuid);
            if (dependency != null)
            {
                Dependencies.Remove(dependency);
                MakeDirty();
            }
        }
    }

    // Тип хранения сцены
    internal enum SceneStorageMode
    {
        SingleFile,     // Всё в одном файле
        SplitWorlds,    // Разбивать на файлы мирами
        SplitEntities   // Разбивать до уровня сущностей
    }
    internal class SceneConfiguration
    {
        // Режим хранения данных сцены
        public SceneStorageMode Storage { get; set; } = SceneStorageMode.SingleFile;

        // Автоматическая перезагрузка сцены при изменении файла
        public bool AutoReload { get; set; } = true;

        // Сжатие файла сцены
        public bool CompressSceneFile { get; set; } = false;

        // Уровень сжатия (если включено)
        public int CompressionLevel { get; set; } = 5;

        // Включить кэширование данных для рендеринга
        public bool EnableRenderingCache { get; set; } = true;

        // Максимальный размер кэша рендеринга в МБ
        public int MaxRenderingCacheMB { get; set; } = 128;
    }

    /// <summary>
    /// Данные мира
    /// </summary>
    internal class WorldData
    {
        // Уникальный идентификатор мира
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();

        // Имя мира
        public string Name { get; set; } = "World_0";

        // Сущности в мире
        public List<EntityData> Entities { get; set; } = new List<EntityData>();

        // Системы в мире
        public List<SystemData> Systems { get; set; } = new List<SystemData>();

        // Флаг активности мира
        public bool IsActive { get; set; } = true;

        // Статус "грязный" (есть несохраненные изменения)
        [JsonIgnore]
        public bool IsDirty { get; set; }

        /// <summary>
        /// Добавить новую сущность
        /// </summary>
        public EntityData AddEntity(string name, World worldInstance)
        {
            // Создаем сущность в ECS
            var ecsEntity = worldInstance.CreateEntity();

            // Создаем данные сущности
            var entityData = new EntityData
            {
                Id = ecsEntity.Id,
                Version = ecsEntity.Version,
                Name = name
            };

            // Добавляем сущность
            Entities.Add(entityData);

            // Помечаем мир как измененный
            IsDirty = true;

            return entityData;
        }

        /// <summary>
        /// Удалить сущность
        /// </summary>
        public void RemoveEntity(uint entityId, uint entityVersion, World worldInstance)
        {
            var entity = Entities.FirstOrDefault(e => e.Id == entityId && e.Version == entityVersion);
            if (entity != null)
            {
                // Удаляем сущность из ECS
                worldInstance.DestroyEntity(entityId, entityVersion);

                // Удаляем данные сущности
                Entities.Remove(entity);

                // Помечаем мир как измененный
                IsDirty = true;
            }
        }

        /// <summary>
        /// Переименовать сущность
        /// </summary>
        public void RenameEntity(uint entityId, uint entityVersion, string newName)
        {
            var entity = Entities.FirstOrDefault(e => e.Id == entityId && e.Version == entityVersion);
            if (entity != null)
            {
                entity.Name = newName;
                IsDirty = true;
            }
        }

        /// <summary>
        /// Добавить систему
        /// </summary>
        public void AddSystem(string systemType)
        {
            var systemData = new SystemData
            {
                SystemType = systemType
            };

            Systems.Add(systemData);
            IsDirty = true;
        }

        /// <summary>
        /// Удалить систему
        /// </summary>
        public void RemoveSystem(string systemGuid)
        {
            var system = Systems.FirstOrDefault(s => s.Guid == systemGuid);
            if (system != null)
            {
                Systems.Remove(system);
                IsDirty = true;
            }
        }
    }

    /// <summary>
    /// Данные сущности
    /// </summary>
    internal class EntityData
    {
        // Уникальный идентификатор сущности
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();

        // Идентификатор сущности в ECS
        public uint Id { get; set; }

        // Версия сущности в ECS
        public uint Version { get; set; }

        // Имя сущности
        public string Name { get; set; } = string.Empty;

        // Компоненты сущности
        [JsonConverter(typeof(ComponentDictionaryConverter))]
        public Dictionary<string, IComponent> Components { get; set; } = new Dictionary<string, IComponent>();

        // Флаг активности сущности
        public bool IsActive { get; set; } = true;

        // Ссылка на родительскую сущность (для иерархии)
        public string ParentEntityGuid { get; set; }

        // Получить компонент по типу
        public T GetComponent<T>() where T : class, IComponent
        {
            var componentType = typeof(T).FullName;
            if (Components.TryGetValue(componentType, out var component) && component is T typedComponent)
            {
                return typedComponent;
            }
            return null;
        }

        // Добавить компонент
        public void AddComponent(IComponent component)
        {
            var componentType = component.GetType().FullName;
            Components[componentType] = component;
        }

        // Удалить компонент
        public void RemoveComponent<T>() where T : class, IComponent
        {
            var componentType = typeof(T).FullName;
            Components.Remove(componentType);
        }
    }

    /// <summary>
    /// Данные системы
    /// </summary>
    public class SystemData
    {
        // Уникальный идентификатор системы
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();

        // Тип системы
        public string SystemType { get; set; }

        // Порядок выполнения системы
        public int ExecutionOrder { get; set; } = 0;

        // Флаг активности системы
        public bool IsActive { get; set; } = true;

        // Настройки системы
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
    }
}
