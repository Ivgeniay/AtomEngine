using EngineLib;
using System.Numerics;

namespace AtomEngine
{
    public enum BodyType
    {
        Static,     
        Dynamic,    
        Kinematic   
    }

    [Documentation(
    DocumentationSection = "Core",
    SubSection = "Components/Physics",
    Name = "RigidbodyComponent",
    Description = @"
    Компонент для симуляции физического поведения сущности.
    
    namespace AtomEngine
    
    Основные свойства:
    - Mass - масса физического тела
    - InverseMass - обратная масса (1/mass)
    - Inertia - инерция по трем осям
    - InverseInertia - обратная инерция
    - BodyType - тип физического тела (Dynamic, Static, Kinematic)
    
    Rigidbody автоматически рассчитывает инерцию на основе массы тела. 
    Для динамических объектов масса должна быть больше нуля.
    
    Типы тел:
    - Dynamic: полностью подвержены физике
    - Static: неподвижны и не реагируют на столкновения
    - Kinematic: движутся программно, но влияют на динамические тела
    
    Пример использования:
    var ball = world.CreateEntity();
    world.AddComponent<RigidbodyComponent>(ball);

    ",
    Author = "AtomEngine Team",
    Title = "Компонент твердого тела"
)]
    [TooltipCategoryComponent(ComponentCategory.Physic)]
    public struct RigidbodyComponent : IComponent
    {
        public Entity Owner { get; set; }

        public float Mass;                
        public float InverseMass;         
        public Vector3 Inertia;           
        public Vector3 InverseInertia;    
        public BodyType BodyType;

        public RigidbodyComponent(Entity owner, float mass, BodyType bodyType = BodyType.Dynamic)
        {
            Owner = owner;
            Mass = mass;
            InverseMass = mass > 0 ? 1.0f / mass : 0.0f;

            float i = 2.0f * mass / 5.0f;
            Inertia = new Vector3(i, i, i);
            InverseInertia = mass > 0 ? new Vector3(1.0f / i, 1.0f / i, 1.0f / i) : Vector3.Zero;
        }

        public override string ToString() => $"RIGIDBODY {Owner}: \n Mass: {Mass}, Inertia: {Inertia}, BodyType: {BodyType}";
    }


}
