using Newtonsoft.Json;
using OpenglLib.Utils;

namespace OpenglLib
{
    internal class GLSLTypeManager
    {
        private Dictionary<string, GlslTypeModel> _typesByMark= new Dictionary<string, GlslTypeModel>();
        private Dictionary<int, GlslTypeModel> _typesByCode = new Dictionary<int, GlslTypeModel>();
        private static GLSLTypeManager? _instance;

        public static GLSLTypeManager Instance
        {
            get
            {
                _instance ??= new GLSLTypeManager();
                return _instance;
            }
        }

        public void LazyInitializer()
        { 
            LoadTypes();
        }

        private void LoadTypes()
        {
            try
            {
                Result<string, Error> mb_jsonContent = Loader.LoadConfigurationFileAsText("GlslTypes.json");
                string jsonContent = mb_jsonContent.Unwrap();
                GLSLTypesConfigurationModel types = JsonConvert.DeserializeObject<GLSLTypesConfigurationModel>(jsonContent);

                if (types == null)
                {
                    throw new DeserializeError("Failed to deserialize GLSL types");
                }

                foreach (KeyValuePair<string, GlslTypeModel> type in types)
                {
                    _typesByCode[type.Value.GlCode] = type.Value;
                    _typesByMark[type.Value.GlslMark] = type.Value;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load GLSL types", ex);

            }
        }

        public GlslTypeModel? GetTypeByCode(int code)
        {
            return _typesByCode.TryGetValue(code, out var type) ? type : null;
        }

        public GlslTypeModel? GetTypeByMark(string mark)
        {
            return _typesByMark.TryGetValue(mark, out var type) ? type : null;
        }

        public IEnumerable<GlslTypeModel> GetTypesByVersion(double version)
        {
            return _typesByCode.Values.Where(t => t.Version <= version);
        }

        public bool IsTypeSupported(int code, double version)
        {
            return GetTypeByCode(code)?.Version <= version;
        }

        public bool IsTypeSupported(string mark, double version)
        {
            return GetTypeByMark(mark)?.Version <= version;
        }
    }
}
