using AtomEngine;
using EngineLib;
using Newtonsoft.Json;

namespace OpenglLib
{
    [TooltipCategoryComponent(ComponentCategory.Render)]
    [GLDependable]
    public partial struct SkyBoxComponent : IComponent
    {
        public Entity Owner { get ; set ; }

        public SkyBoxSourceType SkyBoxSourceType = SkyBoxSourceType.SixTexture;

        public Material Material;
        [JsonProperty] private string MaterialGUID;


        public Texture PosXTexture;
        public Texture PosYTexture;
        public Texture PosZTexture;

        public Texture NegXTexture;
        public Texture NegYTexture;
        public Texture NegZTexture;

        [JsonProperty] private string PosXTextureGUID;
        [JsonProperty] private string PosYTextureGUID;
        [JsonProperty] private string PosZTextureGUID;

        [JsonProperty] private string NegXTextureGUID;
        [JsonProperty] private string NegYTextureGUID;
        [JsonProperty] private string NegZTextureGUID;

        public SkyBoxComponent(Entity owner)
        {
            Owner = owner;
        }

        public static SkyBoxComponent CreateDefault(Entity owner, string materialGUID = "0f07d997-b39e-45c4-9e4d-5b523e338eb0", string textureGUID = "558b385e-2002-4bc6-8db5-eb0d4f2ce599")
        {
            return new SkyBoxComponent(owner)
            {
                SkyBoxSourceType = SkyBoxSourceType.SixTexture,
                PosXTextureGUID = textureGUID,
                PosYTextureGUID = textureGUID,
                PosZTextureGUID = textureGUID,
                NegXTextureGUID = textureGUID,
                NegYTextureGUID = textureGUID,
                NegZTextureGUID = textureGUID,
                MaterialGUID = materialGUID
            };
        }
    }

    public enum SkyBoxSourceType
    {
        SixTexture,
        Cubetexture
    }
}
