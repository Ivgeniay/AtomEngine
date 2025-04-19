using AtomEngine;
using AtomEngine.RenderEntity;
using EngineLib;
using Newtonsoft.Json;

namespace OpenglLib
{
    [TooltipCategoryComponent(ComponentCategory.Render)]
    [GLDependable]
    public partial struct IconComponent : IComponent
    {
        public Entity Owner { get; set; }

        public Material Material;
        [ShowInInspector]
        [JsonProperty]
        private string MaterialGUID;

        public MeshBase Mesh;
        [ShowInInspector]
        [JsonProperty]
        private string MeshGUID;
        [ShowInInspector]
        [JsonProperty]
        private string MeshInternalIndex;

        [DefaultFloat(1.0f)]
        [Range(0.1f, 10.0f)]
        public float IconSize;

        public IconComponent(Entity owner)
        {
            Owner = owner;
            Material = null;
            MaterialGUID = string.Empty;
            IconSize = 10.0f;
        }

        public static IconComponent CreateIconComponent(Entity owner, string materialGuid, string meshGuid, string meshId)
        {
            return new IconComponent(owner)
            {
                MaterialGUID = materialGuid,
                MeshGUID = meshGuid,
                MeshInternalIndex = meshId
            };
        }
    }
}
