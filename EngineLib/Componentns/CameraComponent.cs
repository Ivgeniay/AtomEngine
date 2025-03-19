using EngineLib;
using System.Numerics; 

namespace AtomEngine
{
    [Documentation(
    DocumentationSection = "Core",
    SubSection = "Components/Rendering",
    Name = "CameraComponent",
    Description = @"
    Компонент камеры для просмотра и отображения сцены.
    
    namespace AtomEngine
    
    Основные свойства:
    - FieldOfView - угол обзора в градусах (мин. 0.1)
    - AspectRatio - соотношение сторон (ширина/высота) (мин. 0.1)
    - NearPlane - ближняя плоскость отсечения (мин. 0.001)
    - FarPlane - дальняя плоскость отсечения (мин. 0.01)
    - ViewMatrix - текущая матрица вида
    - CameraUp - вектор, указывающий вверх для камеры (по умолчанию (0,1,0))
    - CameraFront - вектор направления камеры (по умолчанию (0,0,1))
    
    Методы:
    - CreateProjectionMatrix() - создает матрицу проекции для камеры
    
    Пример использования:
    var cameraEntity = world.CreateEntity();
    var camera = world.AddComponent<CameraComponent>(cameraEntity);
    var transform = cameraEntity.GetComponent<TransformComponent>();
    transform.Position = new Vector3(0, 5, -10);
    
    // Получение матриц для рендеринга
    var viewMatrix = camera.ViewMatrix;
    var projectionMatrix = camera.CreateProjectionMatrix();
    ",
    Author = "AtomEngine Team",
    Title = "Компонент камеры"
)]
    [TooltipCategoryComponent(ComponentCategory.Render)]
    public struct CameraComponent : IComponent
    {
        public Entity Owner { get; set; }
        [Min(0.1)]
        [DefaultFloat(45f)]
        public float FieldOfView;
        [Min(0.1)]
        [DefaultFloat(16f/9f)]
        public float AspectRatio;
        [Min(0.001)]
        [DefaultFloat(0.1f)]
        public float NearPlane;
        [Min(0.01)]
        [DefaultFloat(200f)]
        public float FarPlane;

        public Matrix4x4 ViewMatrix { get; set; } = Matrix4x4.Identity;
        [DefaultVector3(0f, 1f, 0f)]
        public Vector3 CameraUp; 
        [DefaultVector3(0f, 0f, 1f)]
        public Vector3 CameraFront;

        public CameraComponent(Entity owner, float fieldOfView = 45.0f, float aspectRatio = 16f / 9f,
                             float nearPlane = 0.1f, float farPlane = 200.0f)
        {
            Owner = owner;
            FieldOfView = fieldOfView;
            AspectRatio = aspectRatio;
            NearPlane = nearPlane;
            FarPlane = farPlane; 
        }
        

        public Matrix4x4 CreateProjectionMatrix()
        {
            return Matrix4x4.CreatePerspectiveFieldOfView(
                FieldOfView * (MathF.PI / 180f),
                AspectRatio,
                NearPlane,
                FarPlane
            );
        }
    }
}
