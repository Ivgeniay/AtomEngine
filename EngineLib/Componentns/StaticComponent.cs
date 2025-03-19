using EngineLib;

namespace AtomEngine
{
    [Documentation(
    DocumentationSection = "Core",
    SubSection = "Components/Physics",
    Name = "StaticComponent",
    Description = @"
    Компонент, обозначающий сущность как статичную.
    
    namespace AtomEngine
    
    Статичные сущности не подвержены физическим взаимодействиям и не перемещаются 
    в результате столкновений. Они используются для создания неподвижных объектов 
    сцены, таких как земля, стены и другие элементы окружения.
    
    Пример использования:
    var wall = world.CreateEntity();
    world.AddComponent<StaticComponent>(wall);


    ",
    Author = "AtomEngine Team",
    Title = "Компонент статичного объекта"
)]
    [TooltipCategoryComponent(ComponentCategory.Physic)]
    public struct StaticComponent : IComponent
    {
        public Entity Owner { get; set; }
        public StaticComponent(Entity owner) => Owner = owner;
    }
}
