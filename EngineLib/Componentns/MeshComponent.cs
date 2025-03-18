using AtomEngine.RenderEntity;
using EngineLib;
using Newtonsoft.Json;

namespace AtomEngine
{
    [TooltipCategoryComponent(ComponentCategory.Render)]
    [GLDependable]
    public partial struct MeshComponent : IComponent
    {
        public Entity Owner { get; set; }
        public MeshBase Mesh;
        [ShowInInspector]
        [JsonProperty]
        private string MeshGUID;
        [ShowInInspector]
        [JsonProperty]
        private string MeshInternalIndex;

        public MeshComponent(Entity owner, MeshBase mesh)
        {
            Owner = owner;
            Mesh = mesh;
        }
    }
}
