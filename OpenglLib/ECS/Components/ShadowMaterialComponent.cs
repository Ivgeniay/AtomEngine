using Newtonsoft.Json;
using AtomEngine;
using EngineLib;

namespace OpenglLib.ECS.Components
{

    [TooltipCategoryComponent(ComponentCategory.Render)]
    [GLDependable]
    public partial struct ShadowMaterialComponent : IComponent
    {
        public Entity Owner { get; set; }
        public Material Material;
        [JsonProperty] private string MaterialGUID;

        public ShadowMaterialComponent(Entity owner)
        {
            Owner = owner;
        }

        public static ShadowMaterialComponent CreateShadowMaterial(Entity owner, string guid)
        {
            return new ShadowMaterialComponent
            {
                Owner = owner,
                MaterialGUID = guid
            };
        }
        public static ShadowMaterialComponent CreateDefaultShadowMaterial(Entity owner)
        {
            return new ShadowMaterialComponent
            {
                Owner = owner,
                MaterialGUID = "shadow-shader-material"
            };
        }
    }
}
