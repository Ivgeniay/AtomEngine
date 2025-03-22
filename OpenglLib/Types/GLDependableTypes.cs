using AtomEngine.RenderEntity;

namespace OpenglLib
{
    public static class GLDependableTypes
    {
        private static readonly Type[] _glDependableTypes = { typeof(Texture), typeof(ShaderBase), typeof(MeshBase), typeof(Material) };
        public static bool IsDependableType(Type type) => _glDependableTypes.Any(dt => dt.IsAssignableFrom(type));

        public static IEnumerable<Type> GetGLDependableTypes()
        {
            foreach (var type in _glDependableTypes)
                yield return type;
        }
    }
}
