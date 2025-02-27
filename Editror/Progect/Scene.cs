using System.Collections.Generic;
using AtomEngine.RenderEntity;
using Newtonsoft.Json;
using AtomEngine;
using System.IO;
using System;

namespace Editor
{
    public class Scene : IDisposable
    {
        // Уникальный идентификатор сцены
        public Guid Id { get; private set; }

        // Имя сцены
        public string Name { get; set; }

        // Путь к файлу сцены (для сохранения/загрузки)
        public string FilePath { get; set; }

        // Коллекция сущностей (Entity -> List компонентов)
        private Dictionary<Entity, Dictionary<Type, IComponent>> _entities;

        // Метки для быстрого поиска сущностей
        private Dictionary<string, HashSet<Entity>> _entityTags;

        // Система для управления ресурсами
        private ResourceManager _resourceManager;

        // События
        public event Action<Entity> EntityAdded;
        public event Action<Entity> EntityRemoved;
        public event Action<Entity, Type> ComponentAdded;
        public event Action<Entity, Type> ComponentRemoved;
        public event Action SceneChanged;

        public Scene(string name = "New Scene")
        {
            Id = Guid.NewGuid();
            Name = name;
            _entities = new Dictionary<Entity, Dictionary<Type, IComponent>>();
            _entityTags = new Dictionary<string, HashSet<Entity>>();
            _resourceManager = new ResourceManager();
        }

        public Entity CreateEntity(string name = "Entity")
        {
            var entity = new Entity(); // Предполагаем, что Entity создаётся с уникальным ID
            var components = new Dictionary<Type, IComponent>();
            _entities[entity] = components;

            // Добавляем базовый компонент с именем
            var nameComponent = new NameComponent(entity, name);
            components[typeof(NameComponent)] = nameComponent;

            EntityAdded?.Invoke(entity);
            SceneChanged?.Invoke();

            return entity;
        }

        public void DestroyEntity(Entity entity)
        {
            if (!_entities.TryGetValue(entity, out var components))
                return;

            // Освобождаем ресурсы компонентов
            foreach (var component in components.Values)
            {
                if (component is IDisposable disposable)
                    disposable.Dispose();
            }

            _entities.Remove(entity);

            // Удаляем из тегов
            foreach (var taggedEntities in _entityTags.Values)
            {
                taggedEntities.Remove(entity);
            }

            EntityRemoved?.Invoke(entity);
            SceneChanged?.Invoke();
        }

        public bool HasEntity(Entity entity)
        {
            return _entities.ContainsKey(entity);
        }

        public IEnumerable<Entity> GetAllEntities()
        {
            return _entities.Keys;
        }

        public T AddComponent<T>(Entity entity) where T : IComponent, new()
        {
            if (!_entities.TryGetValue(entity, out var components))
                throw new ArgumentException($"Entity {entity} does not exist in this scene");

            if (components.ContainsKey(typeof(T)))
                throw new InvalidOperationException($"Entity already has component of type {typeof(T).Name}");

            var component = new T();
            // Устанавливаем Owner для компонента, если требуется
            SetComponentOwner(component, entity);

            components[typeof(T)] = component;

            ComponentAdded?.Invoke(entity, typeof(T));
            SceneChanged?.Invoke();

            return component;
        }

        public void RemoveComponent<T>(Entity entity) where T : IComponent
        {
            if (!_entities.TryGetValue(entity, out var components))
                return;

            if (components.TryGetValue(typeof(T), out var component))
            {
                if (component is IDisposable disposable)
                    disposable.Dispose();

                components.Remove(typeof(T));

                ComponentRemoved?.Invoke(entity, typeof(T));
                SceneChanged?.Invoke();
            }
        }

        public bool HasComponent<T>(Entity entity) where T : IComponent
        {
            return _entities.TryGetValue(entity, out var components) &&
                   components.ContainsKey(typeof(T));
        }

        public T GetComponent<T>(Entity entity) where T : IComponent
        {
            if (_entities.TryGetValue(entity, out var components) &&
                components.TryGetValue(typeof(T), out var component))
            {
                return (T)component;
            }

            return default;
        }

        private void SetComponentOwner(IComponent component, Entity entity)
        {
            // Здесь можно использовать рефлексию для установки Owner,
            // если компонент имеет свойство Owner
            var ownerProperty = component.GetType().GetProperty("Owner");
            if (ownerProperty != null && ownerProperty.CanWrite)
            {
                ownerProperty.SetValue(component, entity);
            }
        }

        public void Save(string path = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                if (string.IsNullOrEmpty(FilePath))
                    throw new InvalidOperationException("File path not specified for saving");

                path = FilePath;
            }
            else
            {
                FilePath = path;
            }

            // Создаем сериализуемую структуру данных
            var sceneData = new SceneData
            {
                Id = Id,
                Name = Name,
                Entities = SerializeEntities()
            };

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
                Converters = new List<JsonConverter> { new ResourceReferenceConverter() }
            };

            string json = JsonConvert.SerializeObject(sceneData, settings);
            File.WriteAllText(path, json);
        }

        private List<EntityData> SerializeEntities()
        {
            var entitiesData = new List<EntityData>();

            foreach (var kvp in _entities)
            {
                var entity = kvp.Key;
                var components = kvp.Value;

                var entityData = new EntityData
                {
                    Id = entity.Id,
                    Components = new List<ComponentData>()
                };

                foreach (var componentKvp in components)
                {
                    var componentType = componentKvp.Key;
                    var component = componentKvp.Value;

                    var componentData = new ComponentData
                    {
                        TypeName = componentType.AssemblyQualifiedName,
                        Data = JsonConvert.SerializeObject(component, new JsonSerializerSettings
                        {
                            Converters = new List<JsonConverter> { new ResourceReferenceConverter() }
                        })
                    };

                    entityData.Components.Add(componentData);
                }

                entitiesData.Add(entityData);
            }

            return entitiesData;
        }

        public static Scene Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Scene file not found: {path}");

            string json = File.ReadAllText(path);

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };

            var sceneData = JsonConvert.DeserializeObject<SceneData>(json, settings);

            var scene = new Scene(sceneData.Name)
            {
                Id = sceneData.Id,
                FilePath = path
            };

            // Загружаем сущности и компоненты
            scene.DeserializeEntities(sceneData.Entities);

            return scene;
        }

        private void DeserializeEntities(List<EntityData> entitiesData)
        {
            foreach (var entityData in entitiesData)
            {
                var entity = new Entity(entityData.Id);
                var components = new Dictionary<Type, IComponent>();
                _entities[entity] = components;

                foreach (var componentData in entityData.Components)
                {
                    Type componentType = Type.GetType(componentData.TypeName);
                    if (componentType == null)
                        continue;

                    // Десериализуем компонент с GUID вместо реальных объектов
                    var component = (IComponent)JsonConvert.DeserializeObject(componentData.Data, componentType);

                    // Устанавливаем Owner
                    SetComponentOwner(component, entity);

                    components[componentType] = component;
                }

                EntityAdded?.Invoke(entity);
            }
        }

        public void Dispose()
        {
            // Освобождаем ресурсы компонентов
            foreach (var entityComponents in _entities.Values)
            {
                foreach (var component in entityComponents.Values)
                {
                    if (component is IDisposable disposable)
                        disposable.Dispose();
                }
            }

            _resourceManager.Dispose();
        }
    }

    public class ResourceManager : IDisposable
    {
        // Кэш загруженных ресурсов (GUID -> ресурс)
        private Dictionary<string, object> _resourceCache = new Dictionary<string, object>();

        // Метаданные ассет-менеджера
        private MetadataManager _metadataManager;

        public ResourceManager()
        {
            _metadataManager = MetadataManager.Instance;
        }

        // Получение ресурса по GUID
        public T GetResource<T>(string guid) where T : class
        {
            // Пробуем найти в кэше
            if (_resourceCache.TryGetValue(guid, out var cachedResource) && cachedResource is T typedResource)
                return typedResource;

            // Получаем путь к файлу по GUID
            string filePath = _metadataManager.GetPathByGuid(guid);
            if (string.IsNullOrEmpty(filePath))
                return null;

            // Загружаем ресурс (здесь нужно реализовать логику загрузки конкретных типов)
            T resource = LoadResource<T>(filePath);

            if (resource != null)
                _resourceCache[guid] = resource;

            return resource;
        }

        // Метод загрузки ресурса по типу
        private T LoadResource<T>(string filePath) where T : class
        {
            // Здесь реализуем загрузку разных типов ресурсов
            Type resourceType = typeof(T);

            // Пример для ShaderBase
            if (typeof(ShaderBase).IsAssignableFrom(resourceType))
            {
                // Логика загрузки шейдера
                // return LoadShader(filePath) as T;
            }

            // Для других типов...

            return null;
        }

        public void Dispose()
        {
            // Освобождаем все загруженные ресурсы
            foreach (var resource in _resourceCache.Values)
            {
                if (resource is IDisposable disposable)
                    disposable.Dispose();
            }

            _resourceCache.Clear();
        }
    }

    public class ResourceReference
    {
        public string Guid { get; set; }

        public ResourceReference(string guid)
        {
            Guid = guid;
        }
    }

    // Конвертер JSON для ссылочных типов
    public class ResourceReferenceConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // Проверяем, что это не примитивный тип и не строка
            return !objectType.IsPrimitive &&
                   objectType != typeof(string) &&
                   !objectType.IsValueType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Десериализуем как ResourceReference
            var reference = serializer.Deserialize<ResourceReference>(reader);

            if (reference == null)
                return null;

            // Здесь просто возвращаем GUID, настоящий ресурс будет загружен позже
            return reference.Guid;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Получаем GUID ресурса (предполагаем, что у ресурса есть свойство Guid)
            string guid = null;

            if (value is IResource resource)
            {
                guid = resource.Guid;
            }
            else
            {
                // Попробуем получить через рефлексию
                var guidProperty = value.GetType().GetProperty("Guid");
                if (guidProperty != null)
                {
                    guid = guidProperty.GetValue(value) as string;
                }
            }

            if (guid != null)
            {
                // Записываем как ResourceReference
                serializer.Serialize(writer, new ResourceReference(guid));
            }
            else
            {
                // Если не можем получить GUID, записываем null
                writer.WriteNull();
            }
        }
    }

    [Serializable]
    public class SceneData
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<EntityData> Entities { get; set; } = new List<EntityData>();
    }

    [Serializable]
    public class EntityData
    {
        public uint Id { get; set; }
        public List<ComponentData> Components { get; set; } = new List<ComponentData>();
    }

    [Serializable]
    public class ComponentData
    {
        public string TypeName { get; set; }
        public string Data { get; set; }
    }

    public interface IResource
    {
        string Guid { get; }
    }
}