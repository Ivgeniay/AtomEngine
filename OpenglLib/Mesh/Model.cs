﻿using System.Numerics;
using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using AtomEngine;

using AssimpMesh = Silk.NET.Assimp.Mesh;
using Node = Silk.NET.Assimp.Node;

namespace OpenglLib
{
    public class Model : IDisposable
    {
        public Model(GL gl, bool gamma = false)
        {
            var assimp = Assimp.GetApi();
            _assimp = assimp;
            _gl = gl;
        }
        private readonly GL _gl;
        private Assimp _assimp;
        public List<Texture> _texturesLoaded = new List<Texture>();
        public string Directory { get; set; } = string.Empty;
        public List<Mesh> Meshes { get; set; } = new List<Mesh>();
        public MeshNode RootNode { get; set; }
        public Dictionary<string, MeshNode> NodeMap { get; set; } = new Dictionary<string, MeshNode>(StringComparer.OrdinalIgnoreCase);

        public MeshNode GetNodeByName(string nodeName)
        {
            if (NodeMap.TryGetValue(nodeName, out var node))
                return node;

            return null;
        }

        public List<MeshNode> FindNodes(string nameSubstring)
        {
            return NodeMap.Values
                .Where(node => node.Name.Contains(nameSubstring, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public string GetNodePath(MeshNode node)
        {
            if (node == null)
                return string.Empty;

            var path = new List<string>();
            var current = node;

            while (current != null)
            {
                path.Add(current.Name);
                current = current.Parent;
            }

            path.Reverse();
            return string.Join("/", path);
        }

        private unsafe void ProcessNode(Node* node, Scene* scene)
        {
            for (var i = 0; i < node->MNumMeshes; i++)
            {
                var mesh = scene->MMeshes[node->MMeshes[i]];
                Meshes.Add(ProcessMesh(mesh, scene));
            }

            for (var i = 0; i < node->MNumChildren; i++)
            {
                ProcessNode(node->MChildren[i], scene);
            }
        }

        private unsafe Mesh ProcessMesh(AssimpMesh* mesh, Scene* scene)
        {
            List<Vertex> vertices = new List<Vertex>();
            List<uint> indices = new List<uint>();
            List<Texture> textures = new List<Texture>();

            for (uint i = 0; i < mesh->MNumVertices; i++)
            {
                Vertex vertex = new Vertex();
                vertex.BoneIds = new int[Vertex.MAX_BONE_INFLUENCE];
                vertex.Weights = new float[Vertex.MAX_BONE_INFLUENCE];

                vertex.Position = mesh->MVertices[i];

                // normals
                if (mesh->MNormals != null)
                    vertex.Normal = mesh->MNormals[i];
                // tangent
                if (mesh->MTangents != null)
                    vertex.Tangent = mesh->MTangents[i];
                // bitangent
                if (mesh->MBitangents != null)
                    vertex.Bitangent = mesh->MBitangents[i];

                // texture coordinates
                if (mesh->MTextureCoords[0] != null) // does the mesh contain texture coordinates?
                {
                    // a vertex can contain up to 8 different texture coordinates. We thus make the assumption that we won't 
                    // use models where a vertex can have multiple texture coordinates so we always take the first set (0).
                    Vector3 texcoord3 = mesh->MTextureCoords[0][i];
                    vertex.TexCoords = new Vector2(texcoord3.X, texcoord3.Y);
                }

                vertices.Add(vertex);
            }

            // now wak through each of the mesh's faces (a face is a mesh its triangle) and retrieve the corresponding vertex indices.
            for (uint i = 0; i < mesh->MNumFaces; i++)
            {
                Face face = mesh->MFaces[i];
                // retrieve all indices of the face and store them in the indices vector
                for (uint j = 0; j < face.MNumIndices; j++)
                    indices.Add(face.MIndices[j]);
            }

            // process materials
            Silk.NET.Assimp.Material* material = scene->MMaterials[mesh->MMaterialIndex];
            // we assume a convention for sampler names in the shaders. Each diffuse texture should be named
            // as 'texture_diffuseN' where N is a sequential number ranging from 1 to MAX_SAMPLER_NUMBER. 
            // Same applies to other texture as the following list summarizes:
            // diffuse: texture_diffuseN
            // specular: texture_specularN
            // normal: texture_normalN

            // 1. diffuse maps
            var diffuseMaps = LoadMaterialTextures(material, TextureType.Diffuse, "texture_diffuse");
            if (diffuseMaps.Any())
                textures.AddRange(diffuseMaps);
            // 2. specular maps
            var specularMaps = LoadMaterialTextures(material, TextureType.Specular, "texture_specular");
            if (specularMaps.Any())
                textures.AddRange(specularMaps);
            // 3. normal maps
            var normalMaps = LoadMaterialTextures(material, TextureType.Height, "texture_normal");
            if (normalMaps.Any())
                textures.AddRange(normalMaps);
            // 4. height maps
            var heightMaps = LoadMaterialTextures(material, TextureType.Ambient, "texture_height");
            if (heightMaps.Any())
                textures.AddRange(heightMaps);

            // return a mesh object created from the extracted mesh data
            //var result = Mesh.CreateStandardMesh(_gl, BuildVertices(vertices), BuildIndices(indices), textures);
            //return result;
            return null;
        }

        private unsafe List<Texture> LoadMaterialTextures(Silk.NET.Assimp.Material* mat, TextureType type, string typeName)
        {
            var textureCount = _assimp.GetMaterialTextureCount(mat, type);
            List<Texture> textures = new List<Texture>();
            for (uint i = 0; i < textureCount; i++)
            {
                AssimpString path;
                _assimp.GetMaterialTexture(mat, type, i, &path, null, null, null, null, null, null);
                bool skip = false;
                for (int j = 0; j < _texturesLoaded.Count; j++)
                {
                    if (_texturesLoaded[j].Path == path)
                    {
                        textures.Add(_texturesLoaded[j]);
                        skip = true;
                        break;
                    }
                }
                if (!skip)
                {
                    var texture = new Texture(_gl, Directory, type: type);
                    texture.Path = path;
                    textures.Add(texture);
                    _texturesLoaded.Add(texture);
                }
            }
            return textures;
        }

        private float[] BuildVertices(List<Vertex> vertexCollection)
        {
            var vertices = new List<float>();

            foreach (var vertex in vertexCollection)
            {
                vertices.Add(vertex.Position.X);
                vertices.Add(vertex.Position.Y);
                vertices.Add(vertex.Position.Z);

                vertices.Add(vertex.Normal.X);
                vertices.Add(vertex.Normal.Y);
                vertices.Add(vertex.Normal.Z);

                vertices.Add(vertex.TexCoords.X);
                vertices.Add(vertex.TexCoords.Y);
            }

            return vertices.ToArray();
        }

        private uint[] BuildIndices(List<uint> indices)
        {
            return indices.ToArray();
        }

        public void Dispose()
        {
            foreach (var mesh in Meshes)
            {
                mesh.Dispose();
            }

            _texturesLoaded = null;
            NodeMap = null;
            RootNode = null;
        }
    }

    public class MeshNode
    {
        public string Name { get; set; }
        public Matrix4x4 Transformation { get; set; }
        public MeshNode Parent { get; set; }
        public List<MeshNode> Children { get; set; } = new List<MeshNode>();
        public List<int> MeshIndices { get; set; } = new List<int>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public MeshNode(string name, Matrix4x4 transformation)
        {
            Name = name;
            Transformation = transformation;
        }

        // Получение абсолютной трансформации (с учетом родительских узлов)
        public Matrix4x4 GetGlobalTransformation()
        {
            if (Parent == null)
                return Transformation;

            return Transformation * Parent.GetGlobalTransformation();
        }

        public MeshNode FindNode(string nodeName)
        {
            if (Name == nodeName)
                return this;

            foreach (var child in Children)
            {
                var found = child.FindNode(nodeName);
                if (found != null)
                    return found;
            }

            return null;
        }

        public void Traverse(Action<MeshNode> action)
        {
            action(this);

            foreach (var child in Children)
            {
                child.Traverse(action);
            }
        }
    }

}
