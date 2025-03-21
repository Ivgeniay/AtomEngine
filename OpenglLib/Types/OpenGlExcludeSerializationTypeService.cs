using EngineLib;

namespace OpenglLib
{
    public class OpenGlExcludeSerializationTypeService : ExcludeSerializationTypeService
    {
        public override Task InitializeAsync()
        {
            foreach(Type type in GLDependableTypes.GetGLDependableTypes())
            {
                AddExcludeType(type);
            }

            return base.InitializeAsync();
        }
    }
}
