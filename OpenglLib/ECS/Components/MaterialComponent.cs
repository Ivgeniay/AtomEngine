using AtomEngine.RenderEntity;
using AtomEngine;
using Newtonsoft.Json;
using EngineLib;

namespace OpenglLib
{
    [TooltipCategoryComponent(ComponentCategory.Render)]
    [GLDependable]
    public partial struct MaterialComponent : IComponent
    {
        public Entity Owner { get; set; }

        public readonly Material Material;
        [JsonProperty]
        private string MaterialGUID;

        public MaterialComponent(Entity owner, Material material)
        {
            Owner = owner;
            Material = material;
        }
    }
}
