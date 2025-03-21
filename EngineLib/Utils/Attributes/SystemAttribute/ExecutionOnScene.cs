using EngineLib;

namespace AtomEngine
{
    [AttributeUsage(AttributeTargets.Class)]
    [Documentation(
    DocumentationSection = "Core",
    Name = "ExecutionOnSceneAttribute",
    SubSection = "Attribute/Systems/Editor",
    Description = @"
    Execution system on scene view.

    namespace AtomEngine
    ExecutionOnSceneAttribute()

    This attribute is used to enable the operation of systems not related to IRenderSystem. The system will be executed in the editor at each iteration.

    Usage examples:
    [ExecutionOnScene]
    public class MovingSystem : ISystem
    {
        public World { get; set; }
        public MovingSystem(IWorld world) => World = world;
    }
    ",
    Author = "AtomEngine Team")]
    public class ExecutionOnScene : Attribute
    {
    }
}
