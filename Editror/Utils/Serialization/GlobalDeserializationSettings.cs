using Newtonsoft.Json;

namespace Editor
{
    internal static class GlobalDeserializationSettings
    {
        private static JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        public static JsonSerializerSettings Settings { get { return settings; } }
    }
}
