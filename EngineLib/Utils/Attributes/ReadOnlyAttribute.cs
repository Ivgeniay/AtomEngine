using EngineLib;

namespace AtomEngine
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field, AllowMultiple = false)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "ReadOnlyAttribute",
    Title = "Атрибут только для чтения",
    SubSection = "Attribute/Inspector",
    Description = @"
    Делает поле или компонент доступным только для чтения в инспекторе.
    
    namespace AtomEngine
    ReadOnlyAttribute()
    
    При применении этого атрибута к полю или структуре, они будут отображаться 
    в инспекторе, но пользователь не сможет изменить их значения. Это полезно 
    для отображения информации, которая не должна быть изменена через интерфейс.
    
    Атрибут может применяться как к отдельным полям, так и к целым компонентам.
    
    Примеры использования:
    public struct StatsComponent : IComponent
    {
        public string PlayerName;
        
        [ReadOnly]
        public int GamesPlayed;
    }
    
    [ReadOnly]
    public struct SystemInfoComponent : IComponent
    {
        public string Version;
        public DateTime BuildDate;
    }
    ",
    Author = "AtomEngine Team")]
    public class ReadOnlyAttribute : Attribute
    {
    }
}
