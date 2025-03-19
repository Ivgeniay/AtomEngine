using EngineLib;

namespace AtomEngine
{
    [Documentation(
    DocumentationSection = "Core",
    Name = "GLDependableAttribute",
    SubSection = "Attribute/Rendering",
    Description = @"
    Указывает, что компонент использует GL-зависимые поля и требует генерации служебного кода.
    
    namespace AtomEngine
    GLDependableAttribute()
    
    Данный атрибут применяется к публичным partial-компонентам, реализующим интерфейс IComponent,
    чтобы указать генератору кода на необходимость создания дополнительных служебных полей для
    правильного рендеринга в графическом контексте.
    
    Важно:
    - Компонент должен быть объявлен как partial
    - Компонент должен реализовывать интерфейс IComponent
    - Компонент должен быть публичным
    
    Примеры использования:
    [GLDependable]
    public partial struct RenderComponent : IComponent
    {
        public Vector3 Position;
        public Vector4 Color;
        
        // Генератор кода автоматически добавит необходимые GL-зависимые поля
        // и методы в дополнительный partial-класс
    }
    ",
    Author = "AtomEngine Team")]
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class GLDependableAttribute : Attribute
    {
    }
}
