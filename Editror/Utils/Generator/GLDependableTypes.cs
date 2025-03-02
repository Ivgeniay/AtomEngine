using AtomEngine.RenderEntity;
using OpenglLib;
using System;
using System.Linq;

namespace Editor
{
    internal static class GLDependableTypes
    {
        private static readonly Type[] _glDependableTypes = { typeof(Texture), typeof(ShaderBase), typeof(MeshBase) };
        public static bool IsDependableType(Type type) => _glDependableTypes.Any(dt => dt.IsAssignableFrom(type));
    }
}
