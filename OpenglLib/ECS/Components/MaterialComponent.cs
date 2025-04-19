using Newtonsoft.Json;
using AtomEngine;
using EngineLib;

namespace OpenglLib
{
    [TooltipCategoryComponent(ComponentCategory.Render)]
    [GLDependable]
    public partial struct MaterialComponent : IComponent
    {
        public Entity Owner { get; set; }

        public Material Material;
        [JsonProperty]
        private string MaterialGUID;

        public MaterialComponent(Entity owner)
        {
            Owner = owner;
        }

        public static MaterialComponent CreateMaterial(Entity owner, string guid)
        {
            return new MaterialComponent
            {
                Owner = owner,
                MaterialGUID = guid
            };
        }

        public static MaterialComponent CreateDefauldMaterial(Entity owner)
        {
            return new MaterialComponent
            {
                Owner = owner,
                MaterialGUID = "pbr-shader-material"
            };
        }
    }


}
