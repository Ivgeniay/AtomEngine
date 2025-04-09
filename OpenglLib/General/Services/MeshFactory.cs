using AtomEngine.RenderEntity;
using Silk.NET.Assimp;
using AtomEngine;
using EngineLib;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    public class MeshFactory : IService
    {
        protected Dictionary<string, ModelData> _modelDataCache = new Dictionary<string, ModelData>();
        protected Dictionary<(string modelPath, int meshIndex, uint shaderId), Mesh> _meshCache = new Dictionary<(string, int, uint), Mesh>();
        protected Assimp _assimp;

        public virtual Task InitializeAsync()
        {
            _assimp = Assimp.GetApi();
            return Task.CompletedTask;
        }

        public MeshBase CreateMeshInstanceFromPath(GL gl, string modelPath, int meshIndex, Shader shader = null)
        {
            if (_assimp == null) _assimp = Assimp.GetApi();
            uint shaderId = shader?.Handle ?? 0;

            if (_meshCache.TryGetValue((modelPath, meshIndex, shaderId), out var cachedMesh))
            {
                return cachedMesh;
            }

            try
            {
                if (!_modelDataCache.TryGetValue(modelPath, out ModelData modelData))
                {
                    string meshText = ServiceHub.Get<ModelManager>().LoadModel(modelPath);
                    if (string.IsNullOrEmpty(meshText))
                    {
                        DebLogger.Error($"Не удалось загрузить объкт из пути: {modelPath}");
                        return null;
                    }

                    var modelResult = ModelLoader.LoadModel(modelPath, _assimp, false);
                    modelData = modelResult.Unwrap();
                    _modelDataCache[modelPath] = modelData;
                }

                if (meshIndex < 0 || meshIndex >= modelData.Meshes.Count)
                {
                    DebLogger.Error($"Некорректный индекс меша: {meshIndex}. В модели {modelPath} всего {modelData.Meshes.Count} мешей");
                    return null;
                }

                MeshData meshData = modelData.Meshes[meshIndex];

                Mesh mesh;
                if (shader != null)
                {
                    MeshBuilder builder = new MeshBuilder(gl, shader);
                    mesh = builder.BuildMesh(meshData);
                }
                else
                {
                    var format = new VertexFormat();
                    format.AddAttribute("aPosition", 0, 3);
                    mesh = new Mesh(gl, ConvertToPositionOnlyVertices(meshData.Vertices), meshData.GetIndices(), format);
                }

                _meshCache[(modelPath, meshIndex, shaderId)] = mesh;
                return mesh;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка создания экземпляра объкта из пути: {ex.Message}");
                return null;
            }
        }

        public MeshBase CreateMeshInstanceFromGuid(GL gl, string meshGuid, int meshIndex, Shader shader = null)
        {
            try
            {
                string materialPath = ServiceHub.Get<MetadataManager>().GetPathByGuid(meshGuid);
                if (string.IsNullOrEmpty(materialPath))
                {
                    DebLogger.Error($"Mesh не найден для GUID: {meshGuid}");
                    return null;
                }

                return CreateMeshInstanceFromPath(gl, materialPath, meshIndex, shader);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка создания экземпляра Mesh из GUID: {ex.Message}");
                return null;
            }
        }

        private float[] ConvertToPositionOnlyVertices(List<VertexData> vertices)
        {
            float[] result = new float[vertices.Count * 3];
            for (int i = 0; i < vertices.Count; i++)
            {
                result[i * 3] = vertices[i].Position.X;
                result[i * 3 + 1] = vertices[i].Position.Y;
                result[i * 3 + 2] = vertices[i].Position.Z;
            }
            return result;
        }

        public void ClearShaderCache(uint shaderId)
        {
            var keysToRemove = _meshCache.Keys.Where(k => k.shaderId == shaderId).ToList();
            foreach (var key in keysToRemove)
            {
                if (_meshCache.TryGetValue(key, out var mesh))
                {
                    mesh.Dispose();
                    _meshCache.Remove(key);
                }
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

            foreach (var mesh in _meshCache.Values)
            {
                mesh.Dispose();
            }
            _meshCache.Clear();
            _modelDataCache.Clear();
        }
    }


    //public class MeshFactory : IService
    //{
    //    protected Dictionary<string, Model> _modelInstanceCache = new Dictionary<string, Model>();
    //    protected Assimp? _assimp;

    //    public virtual Task InitializeAsync()
    //    {
    //        return Task.CompletedTask;
    //    }

    //    public MeshBase CreateMeshInstanceFromPath(GL gl, string modelPath, object context)
    //    {
    //        int index = (int)context;

    //        if (_assimp == null) _assimp = Assimp.GetApi();
    //        try
    //        {
    //            if (_modelInstanceCache.TryGetValue(modelPath, out Model cachedModel))
    //            {
    //                return cachedModel.Meshes[index];
    //            }
    //            else
    //            {
    //                string meshText = ServiceHub.Get<ModelManager>().LoadModel(modelPath);
    //                if (meshText == null)
    //                {
    //                    DebLogger.Error($"Не удалось загрузить объкт из пути: {modelPath}");
    //                    return null;
    //                }
    //                Result<Model, Error> mb_model = ModelLoader.LoadModel(modelPath, gl, _assimp, false);
    //                var model = mb_model.Unwrap();
    //                _modelInstanceCache[modelPath] = model;
    //                var mesh = model.Meshes[index];
    //                return mesh;
    //            }

    //            throw new NotFoundError($"Не найден {modelPath}");
    //        }
    //        catch (Exception ex)
    //        {
    //            DebLogger.Error($"Ошибка создания экземпляра объкта из пути: {ex.Message}");
    //            return null;
    //        }
    //    }
    //    public MeshBase CreateMeshInstanceFromGuid(GL gl, string meshGuid, object context)
    //    {
    //        try
    //        {
    //            string materialPath = ServiceHub.Get<MetadataManager>().GetPathByGuid(meshGuid);
    //            if (string.IsNullOrEmpty(materialPath))
    //            {
    //                DebLogger.Error($"Mesh не найден для GUID: {meshGuid}");
    //                return null;
    //            }

    //            return CreateMeshInstanceFromPath(gl, materialPath, context);
    //        }
    //        catch (Exception ex)
    //        {
    //            DebLogger.Error($"Ошибка создания экземпляра Mesh из GUID: {ex.Message}");
    //            return null;
    //        }
    //    }

    //    public void ClearCache()
    //    {
    //        Dispose();
    //    }

    //    public void Dispose()
    //    {
    //        _assimp?.Dispose();
    //        _assimp = null;

    //        foreach (var model in _modelInstanceCache.Values)
    //        {
    //            model.Dispose();
    //        }
    //        _modelInstanceCache.Clear();
    //    }
    //}

}
