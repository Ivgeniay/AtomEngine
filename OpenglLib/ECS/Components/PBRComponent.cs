using Newtonsoft.Json;
using AtomEngine;
using EngineLib;
using AtomEngine.RenderEntity;

namespace OpenglLib
{
    [TooltipCategoryComponent(ComponentCategory.Render)]
    [GLDependable]
    public partial struct PBRComponent : IComponent
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

        public PBRComponent(Entity owner)
        {
            Owner = owner;
        }

        public static PBRComponent CreateMaterial(Entity owner, string shaderGuid, string meshGuid, string meshIndex)
        {
            return new PBRComponent
            {
                Owner = owner,
                MaterialGUID = shaderGuid,
                MeshGUID = meshGuid,
                MeshInternalIndex = meshIndex
            };
        }

    }


}
