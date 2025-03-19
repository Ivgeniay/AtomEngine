using EngineLib;

namespace AtomEngine
{
    /// <summary>
    /// Show private field in inspector
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "ShowInInspectorAttribute",
    SubSection = "Attribute/Inspector/Visible",
    Description = @"
    Отображает приватное поле в инспекторе.
    
    namespace AtomEngine
    ShowInInspectorAttribute()
    
    По умолчанию приватные поля не отображаются в инспекторе. Этот атрибут 
    позволяет сделать приватные поля видимыми и редактируемыми в инспекторе, 
    сохраняя при этом их инкапсуляцию в коде.
    
    Примеры использования:
    public struct ConfigComponent : IComponent
    {
        public string ConfigName;
        
        [ShowInInspector]
        private float _internalSetting;
    }
    ",
    Author = "AtomEngine Team")]
    public class ShowInInspectorAttribute : Attribute
    {
    }
}
