using AtomEngine.RenderEntity;
using Silk.NET.Assimp;
using AtomEngine;
using EngineLib;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    public class MeshFactory : IService
    {
        protected Dictionary<string, Model> _modelInstanceCache = new Dictionary<string, Model>();
        protected Assimp? _assimp;

        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public MeshBase CreateMeshInstanceFromPath(GL gl, string modelPath, object context)
        {
            int index = (int)context;

            if (_assimp == null) _assimp = Assimp.GetApi();
            try
            {
                if (_modelInstanceCache.TryGetValue(modelPath, out Model cachedModel))
                {
                    return cachedModel.Meshes[index];
                }
                else
                {
                    string meshText = ServiceHub.Get<ModelManager>().LoadModel(modelPath);
                    if (meshText == null)
                    {
                        DebLogger.Error($"Не удалось загрузить объкт из пути: {modelPath}");
                        return null;
                    }
                    Result<Model, Error> mb_model = ModelLoader.LoadModel(modelPath, gl, _assimp, false);
                    var model = mb_model.Unwrap();
                    _modelInstanceCache[modelPath] = model;
                    var mesh = model.Meshes[index];
                    return mesh;
                }

                throw new NotFoundError($"Не найден {modelPath}");
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка создания экземпляра объкта из пути: {ex.Message}");
                return null;
            }
        }
        public MeshBase CreateMeshInstanceFromGuid(GL gl, string meshGuid, object context)
        {
            try
            {
                string materialPath = ServiceHub.Get<MetadataManager>().GetPathByGuid(meshGuid);
                if (string.IsNullOrEmpty(materialPath))
                {
                    DebLogger.Error($"Mesh не найден для GUID: {meshGuid}");
                    return null;
                }

                return CreateMeshInstanceFromPath(gl, materialPath, context);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка создания экземпляра Mesh из GUID: {ex.Message}");
                return null;
            }
        }

        public void ClearCache()
        {
            Dispose();
        }

        public void Dispose()
        {
            _assimp?.Dispose();
            _assimp = null;

            foreach (var model in _modelInstanceCache.Values)
            {
                model.Dispose();
            }
            _modelInstanceCache.Clear();
        }
    }
}
