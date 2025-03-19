using EngineLib;

namespace AtomEngine
{
    [Documentation(
    DocumentationSection = "Core",
    SubSection = "Components/Transform",
    Name = "HierarchyComponent",
    Description = @"
    Компонент для организации сущностей в иерархическую древовидную структуру.
    
    namespace AtomEngine
    
    Основные свойства:
    - Level - уровень вложенности в иерархии
    - LocalIndex - локальный индекс среди дочерних элементов родителя
    - Parent - идентификатор родительской сущности (uint.MaxValue для корневых объектов)
    - Children - список идентификаторов дочерних сущностей
    
    Атрибуты:
    - [HideClose] - предотвращает случайное удаление компонента
    - [ReadOnly] - поля только для чтения в инспекторе
    
    Особенности:
    - Корневые объекты имеют значение Parent = uint.MaxValue
    - При добавлении автоматически создается TransformComponent, если он отсутствует
    - Используется для трансформации в локальном/мировом пространстве
    
    ",
    Author = "AtomEngine Team",
    Title = "Компонент иерархии"
)]
    [HideClose]
    public struct HierarchyComponent : IComponent
    {
        public Entity Owner { get ; set ;}
        [ReadOnly]
        public uint Level;
        [ReadOnly]
        public uint LocalIndex;
        [ReadOnly]
        public uint Parent;
        [ReadOnly]
        public List<uint> Children;
    }
}
