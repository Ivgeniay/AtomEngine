using System.Collections.Generic;
using System.Threading.Tasks;
using AtomEngine.RenderEntity;
using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using AtomEngine;
using OpenglLib;
using System;

namespace Editor
{
    internal class MeshFactory : IService, IDisposable
    {
        private Dictionary<string, MeshBase> _meshInstanceCache = new Dictionary<string, MeshBase>();
        private Assimp? _assimp;

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public MeshBase CreateMeshInstanceFromPath(GL gl, string meshPath)
        {
            if (_assimp == null) _assimp = Assimp.GetApi();
            try
            {
                string meshText = ServiceHub.Get<MeshManager>().LoadMesh(meshPath);
                if (meshText == null)
                {
                    DebLogger.Error($"Не удалось загрузить материал из пути: {meshPath}");
                    return null;
                }
                Result<Model, Error> mb_model = ModelLoader.LoadModel(meshPath, gl, _assimp, false);
                var model = mb_model.Unwrap();
                var mesh = model.Meshes[0];
                return mesh;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка создания экземпляра материала из пути: {ex.Message}");
                return null;
            }
        }
        public MeshBase CreateMeshInstanceFromGuid(GL gl, string meshGuid)
        {
            try
            {
                string materialPath = ServiceHub.Get<MetadataManager>().GetPathByGuid(meshGuid);
                if (string.IsNullOrEmpty(materialPath))
                {
                    DebLogger.Error($"Mesh не найден для GUID: {meshGuid}");
                    return null;
                }

                return CreateMeshInstanceFromPath(gl, materialPath);
            }
            catch(Exception ex) 
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
            foreach(var mesh in _meshInstanceCache.Values) 
            { 
                mesh.Dispose(); 
            }
            _meshInstanceCache.Clear();
        }
    }
}
