using EngineLib;

namespace AtomEngine
{
    /// <summary>
    /// Dont show this field in inspector
    /// </summary>
    /// 
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "HideInInspectorAttribute",
    SubSection = "Attribute/Inspector/Visible",
    Description = @"
    Скрывает поле в инспекторе.
    
    namespace AtomEngine
    HideInInspectorAttribute()
    
    Этот атрибут используется для исключения отдельных полей из отображения 
    в инспекторе. Это полезно для служебных полей, которые не должны изменяться 
    пользователем напрямую, но при этом должны оставаться частью компонента.
    
    Примеры использования:
    public struct PlayerComponent : IComponent
    {
        public string Name;
        
        [HideInInspector]
        public Guid UniqueIdentifier;
    }
    ",
    Author = "AtomEngine Team")]
    public class HideInInspectorAttribute : Attribute
    {
    }
}
