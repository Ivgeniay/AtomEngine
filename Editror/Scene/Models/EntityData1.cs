using System.Collections.Generic;
using Newtonsoft.Json;
using AtomEngine;

namespace Editor
{
    internal class EntityData1
    {
        public uint Id { get; set; }
        public uint Version { get; set; }
        public string Name { get; set; } = string.Empty;

        [JsonConverter(typeof(ComponentDictionaryConverter))]
        public Dictionary<string, IComponent> Components { get; set; } = new();

        public static Entity CreateEntity(EntityData1 entityData) => new Entity(entityData.Id, entityData.Version);
        public static EntityData1 CreateFromEntity(Entity entity) => new EntityData1() { Id = entity.Id, Version = entity.Version };
        public static EntityData1 CreateFromEntity(Entity entity, Dictionary<string, IComponent> components) => new EntityData1() { Id = entity.Id, Version = entity.Version, Components = components };
    }
}
