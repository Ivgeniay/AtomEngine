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
        [JsonProperty]
        private string MeshGUID;

        public MeshComponent(Entity owner, MeshBase mesh)
        {
            Owner = owner;
            Mesh = mesh;
        }
    }
}
