namespace OpenglLib
{
    /*
    Файлы с расширением RS используются в glsl коде шейдеров с дерективой #include.

    Текст файла импортируется в текст шейдера.
    На основе RS файла создаются компонент, система и интерфейс к которому в системе приводится шейдерное C# представление для установки значений 
    в шейдер. 
    RS файлы могут содержать атрибуты.
    [InterfaceName:IViewRender] - используется для того чтобы задать имя генерируемого интерфейса
    [ComponentName:ViewComponent] - используется для того чтобы задать имя генерируемого компонента
    [SystemName:ViewRenderSystem] - используется для того чтобы задать имя системе 
    [RequiredComponent:MeshComponent] - используется для того чтобы указать какие дополнительные компоненты необходимы системе для работы
    [PlaceTarget:Vertex] - используется для того чтобы объяснить в какой части шейдера будет использоваться тот или иной член rs файла.
    
    RS файлы могут располагаться в директории в файловой системе или, как включеные в сборку.

    #include "RC/light.rs"
    #include "embedded:Resources/Graphics/RS/view.rs"

    Включение файлов происходит рекурсивно и относительно файла в котором указывается #include.
    Если нет о время рекурсивного обхода включаемых файлов в glsl коде не найдем файл (К примеру в #include "RC/light.rs" не будет найден файл,
    то система инклуда попробует найти этот файл в embedded ресурсах.
    Если указать embedded явно, то система сразу приступит к поиску включаемого кода сразу во включенных файлах проекта.

    Для работы с RS файлами соществует RSManager, который занимается генерацией файлов, и способен отдавать информацию об уже сгенерированных объектах.


    Пример RS файла:


    [InterfaceName:IViewRender]
    [ComponentName:ViewComponent]
    [SystemName:ViewRenderSystem]
    [RequiredComponent:MeshComponent]
    [PlaceTarget:Vertex]
    uniform mat4 model;
    [PlaceTarget:Vertex]
    uniform mat4 view;
    [PlaceTarget:Vertex]
    uniform mat4 projection;

    На основании этого RS файла будет создан Компонент, Система и интерфейс:
    public interface IViewRender
    {
        Matrix4X4<float> model { set; }
        Matrix4X4<float> view { set; }
        Matrix4X4<float> projection { set; }
    }
    [TooltipCategoryComponent(category: ComponentCategory.Render)]
    public partial struct ViewComponent : IComponent
    {
        public Entity Owner { get; set; }

        [ShowInInspector]
        public Matrix4X4<float> model;

        [ShowInInspector]
        public Matrix4X4<float> view;

        [ShowInInspector]
        public Matrix4X4<float> projection;

        public ViewComponent(Entity owner) { Owner = owner; }
        public void MakeClean() { }
    }
    public class ViewRenderSystem : IRenderSystem
    {
        public IWorld World { get; set; }
        private QueryEntity queryRendererEntities;


        public ViewRenderSystem(IWorld world)
        {
            World = world;

            queryRendererEntities = this.CreateEntityQuery()
                .With<MaterialComponent>()
                .With<MeshComponent>()
                .With<ViewComponent>()
                ;
        }


        public void Render(double deltaTime)
        {
            Entity[] rendererEntities = queryRendererEntities.Build();
            if (rendererEntities.Length == 0)
                return;

            foreach (var entity in rendererEntities)
            {
                ref var meshComponent = ref this.GetComponent<MeshComponent>(entity);
                ref var materialComponent = ref this.GetComponent<MaterialComponent>(entity);

                if (meshComponent.Mesh == null || materialComponent.Material?.Shader == null)
                    continue;

                ref var viewComponent = ref this.GetComponent<ViewComponent>(entity);

                if (materialComponent.Material.Shader is IViewRender renderer)
                {
                    materialComponent.Material.Shader.Use();
                    renderer.model = viewComponent.model;
                    renderer.view = viewComponent.view;
                    renderer.projection = viewComponent.projection;

                    meshComponent.Mesh.Draw(materialComponent.Material.Shader);
                }
            }
        }
        public void Resize(Vector2 size) { }
        public void Initialize() { }
    }
     */
    public class RSFileInfo
    {
        public string SourcePath { get; set; } = string.Empty;
        public string SourceFolder { get; set; } = string.Empty;
        public string InterfaceName { get; set; } = string.Empty;
        public string ComponentName { get; set; } = string.Empty;
        public string SystemName { get; set; } = string.Empty;
        public string ProcessedCode { get; set; } = string.Empty;
        public List<GlslStructInstance> StructureInstances { get; set; } = new List<GlslStructInstance>();
        public List<GlslConstantModel> Constants { get; set; } = new List<GlslConstantModel>();
        public List<UniformBlockModel> UniformBlocks { get; set; } = new List<UniformBlockModel>();
        public List<UniformModel> Uniforms { get; set; } = new List<UniformModel>();
        public List<GlslStructModel> Structures { get; set; } = new List<GlslStructModel>();
        public List<GlslMethodInfo> Methods { get; set; } = new List<GlslMethodInfo>();
        public List<string> RequiredComponent { get; set; } = new List<string>();
    }
}
