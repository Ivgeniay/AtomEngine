using System.Collections.Generic;
using System.Numerics;
using AtomEngine;
using EngineLib;

namespace Editor
{
    public class ModelMetadata : FileMetadata
    {
        public ModelMetadata()
        {
            AssetType = MetadataType.Model;
        }

        public List<NodeModelData> MeshesData = new List<NodeModelData>();
        public List<string> Textures = new List<string>();
    }

    public class NodeModelData : IDataSerializable
    {
        public Matrix4x4 Matrix;
        public string MeshName = string.Empty;
        public string MeshPath = string.Empty;
        public int Index = -1;
    }
}
