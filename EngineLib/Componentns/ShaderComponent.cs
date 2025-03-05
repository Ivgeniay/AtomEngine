using AtomEngine.RenderEntity;
using EngineLib;
using Newtonsoft.Json;

namespace AtomEngine
{
    [TooltipCategoryComponent(ComponentCategory.Render)]
    [GLDependable]
    public partial struct ShaderComponent : IComponent
    {
        public Entity Owner { get; }

        public readonly ShaderBase Shader;
        [JsonProperty]
        private string ShaderGUID;

        public ShaderComponent(Entity owner, ShaderBase shader)
        {
            Owner = owner;
            Shader = shader;
        }

    }
}
