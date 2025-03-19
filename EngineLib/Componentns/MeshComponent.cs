using AtomEngine.RenderEntity;
using EngineLib;
using Newtonsoft.Json;

namespace AtomEngine
{
    [Documentation(
    DocumentationSection = "Core",
    SubSection = "Components/Rendering",
    Name = "MeshComponent",
    Description = @"
    Компонент для привязки и отображения 3D-моделей.
    
    namespace AtomEngine
    
    Основные свойства:
    - Mesh - ссылка на базовый класс 3D-модели
    
    Используется для привязки 3D-моделей к сущностям для их отображения в сцене.
    Работает в сочетании с TransformComponent для определения положения и ориентации модели.

    ",
    Author = "AtomEngine Team",
    Title = "Компонент 3D-модели"
)]
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
