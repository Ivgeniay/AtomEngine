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
    Component for binding and displaying 3D models.

    namespace AtomEngine

    Main properties:
    - Mesh - reference to the base class of the 3D model

    Used to bind 3D models to entities for displaying them in the scene.
    Works in combination with TransformComponent to determine the position and orientation of the model.

    ",
    Author = "AtomEngine Team",
    Title = "3D Model Component"
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

        public MeshComponent(Entity owner)
        {
            Owner = owner;
            Mesh = null;
            MeshInternalIndex = string.Empty;
        }

        public MeshComponent(Entity owner, MeshBase mesh)
        {
            Owner = owner;
            Mesh = mesh;
        }

        public static MeshComponent CreateMesh(Entity owner, string guid, string meshIndex)
        {
            return new MeshComponent(owner)
            {
                MeshGUID = guid,
                MeshInternalIndex = meshIndex
            };
        }

        public static MeshComponent CreateCube(Entity owner)
        {
            return new MeshComponent(owner)
            {
                MeshGUID = "cube-model-1",
                MeshInternalIndex = "0"
            };
        }

        public static MeshComponent CreateCone(Entity owner)
        {
            return new MeshComponent(owner)
            {
                MeshGUID = "cone-model-1",
                MeshInternalIndex = "0"
            };
        }

        public static MeshComponent CreateCylinder(Entity owner)
        {
            return new MeshComponent(owner)
            {
                MeshGUID = "cylinder-model-1",
                MeshInternalIndex = "0"
            };
        }

        public static MeshComponent CreateSphere(Entity owner)
        {
            return new MeshComponent(owner)
            {
                MeshGUID = "sphere-model-1",
                MeshInternalIndex = "0"
            };
        }

        public static MeshComponent CreateTorus(Entity owner)
        {
            return new MeshComponent(owner)
            {
                MeshGUID = "torus-model-1",
                MeshInternalIndex = "0"
            };
        }
    }
}
