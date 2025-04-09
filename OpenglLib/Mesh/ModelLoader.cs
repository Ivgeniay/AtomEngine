using AtomEngine;
using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using AssimpMesh = Silk.NET.Assimp.Mesh;

namespace OpenglLib
{
    public static class ModelLoader
    {
        private const string BaseNamespace = "OpenglLib";
        public static string _customBasePath = AppContext.BaseDirectory;

        public static Result<ModelData, Error> LoadModel(string modelPath, Assimp _assimp, bool useEmbeddedResources = true)
        {
            if (useEmbeddedResources)
            {
                return LoadFromResources(modelPath, _assimp);
            }
            return LoadFromFile(modelPath, _assimp);
        }

        private unsafe static Result<ModelData, Error> LoadFromResources(string modelPath, Assimp _assimp)
        {
            Scene* scene = GetSceneFromFile(modelPath, _assimp);

            ModelData modelData = new ModelData();
            modelData.Directory = modelPath;
            modelData.NodeMap = new Dictionary<string, NodeData>(StringComparer.OrdinalIgnoreCase);

            modelData.RootNode = BuildNodeHierarchy(scene->MRootNode, null, modelData.NodeMap);

            ProcessNode(scene->MRootNode, scene, _assimp, modelData);

            return new Result<ModelData, Error>(modelData);
        }

        private static unsafe Scene* GetSceneFromFile(string modelPath, Assimp _assimp)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames();
            var normalizedModelName = modelPath.Replace('/', '.').Replace('\\', '.');
            var resourceName = resources.FirstOrDefault(r =>
                r.EndsWith(normalizedModelName, StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                var availableModels = string.Join("\n", resources
                    .Where(r => r.StartsWith(BaseNamespace))
                    .Select(r => r.Substring(BaseNamespace.Length + 1)));

                throw new ShaderError(
                    $"Model resource not found: {modelPath}\n" +
                    $"Available models:\n{availableModels}");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new ShaderError($"Failed to load shader stream: {resourceName}");

            string TrimFilename(string path)
            {
                var splitedStr = path.Split(".");
                if (splitedStr.Length <= 2)
                    return path;

                var span = splitedStr.AsSpan().Slice(1, splitedStr.Length - 1);
                var res = string.Join(".", span);

                return TrimFilename(res);
            }

            string modelName = TrimFilename(normalizedModelName);

            byte[] modelData = new byte[stream.Length];
            stream.Read(modelData, 0, modelData.Length);

            fixed (byte* buffer = modelData)
            {
                Scene* scene = _assimp.ImportFileFromMemory(
                buffer,
                (uint)modelData.Length,
                (uint)(PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals | PostProcessSteps.FlipUVs),
                (byte*)Marshal.StringToHGlobalAnsi(modelName));

                if (scene == null)
                {
                    var errorMessage = Marshal.PtrToStringAnsi((IntPtr)_assimp.GetErrorString());
                    throw new MeshError($"Failed to load model from resources: {modelName}. Assimp error: {errorMessage}");
                }
                return scene;
            }
        }

        private unsafe static NodeData BuildNodeHierarchy(Node* node, NodeData parent, Dictionary<string, NodeData> nodeMap)
        {
            if (node == null)
                return null;

            string nodeName = Marshal.PtrToStringAnsi((IntPtr)node->MName.Data)?.Trim() ?? "UnnamedNode";

            var transform = new Matrix4x4(
                node->MTransformation.M11, node->MTransformation.M12, node->MTransformation.M13, node->MTransformation.M14,
                node->MTransformation.M21, node->MTransformation.M22, node->MTransformation.M23, node->MTransformation.M24,
                node->MTransformation.M31, node->MTransformation.M32, node->MTransformation.M33, node->MTransformation.M34,
                node->MTransformation.M41, node->MTransformation.M42, node->MTransformation.M43, node->MTransformation.M44
            );

            var nodeData = new NodeData(nodeName, transform);
            nodeData.Parent = parent;
            nodeMap[nodeName] = nodeData;

            for (uint i = 0; i < node->MNumMeshes; i++)
            {
                nodeData.MeshIndices.Add((int)node->MMeshes[i]);
            }

            for (uint i = 0; i < node->MNumChildren; i++)
            {
                var childNode = BuildNodeHierarchy(node->MChildren[i], nodeData, nodeMap);
                nodeData.Children.Add(childNode);
            }

            return nodeData;
        }

        private static unsafe void ProcessNode(Node* node, Scene* scene, Assimp _assimp, ModelData modelData)
        {
            for (var i = 0; i < node->MNumMeshes; i++)
            {
                var mesh = scene->MMeshes[node->MMeshes[i]];
                var meshData = ProcessMesh(mesh, scene, _assimp, modelData);
                modelData.Meshes.Add(meshData);
            }

            for (var i = 0; i < node->MNumChildren; i++)
            {
                ProcessNode(node->MChildren[i], scene, _assimp, modelData);
            }
        }

        private static unsafe MeshData ProcessMesh(AssimpMesh* mesh, Scene* scene, Assimp _assimp, ModelData modelData)
        {
            MeshData meshData = new MeshData();

            string meshName = Marshal.PtrToStringAnsi((IntPtr)mesh->MName.Data)?.Trim() ?? "UnnamedMesh";
            meshData.Name = meshName;

            // Обработка вершин
            for (uint i = 0; i < mesh->MNumVertices; i++)
            {
                VertexData vertex = new VertexData();

                // Позиция (обязательно должна быть)
                vertex.Position = mesh->MVertices[i];

                // Нормаль (опционально)
                if (mesh->MNormals != null)
                    vertex.Normal = mesh->MNormals[i];

                // Тангент (опционально)
                if (mesh->MTangents != null)
                    vertex.Tangent = mesh->MTangents[i];

                // Битангент (опционально)
                if (mesh->MBitangents != null)
                    vertex.Bitangent = mesh->MBitangents[i];

                // Текстурные координаты (опционально)
                if (mesh->MTextureCoords[0] != null)
                {
                    Vector3 texcoord3 = mesh->MTextureCoords[0][i];
                    vertex.TexCoords = new Vector2(texcoord3.X, texcoord3.Y);
                }

                meshData.Vertices.Add(vertex);
            }

            // Обработка граней и индексов
            for (uint i = 0; i < mesh->MNumFaces; i++)
            {
                Face face = mesh->MFaces[i];
                for (uint j = 0; j < face.MNumIndices; j++)
                    meshData.Indices.Add(face.MIndices[j]);
            }

            // Обработка материалов
            Silk.NET.Assimp.Material* material = scene->MMaterials[mesh->MMaterialIndex];

            // Диффузные текстуры
            var diffuseMaps = LoadMaterialTextureInfo(material, TextureType.Diffuse, "texture_diffuse", _assimp);
            if (diffuseMaps.Any())
                meshData.TextureInfos.AddRange(diffuseMaps);

            // Спекулярные текстуры
            var specularMaps = LoadMaterialTextureInfo(material, TextureType.Specular, "texture_specular", _assimp);
            if (specularMaps.Any())
                meshData.TextureInfos.AddRange(specularMaps);

            // Карты нормалей
            var normalMaps = LoadMaterialTextureInfo(material, TextureType.Height, "texture_normal", _assimp);
            if (normalMaps.Any())
                meshData.TextureInfos.AddRange(normalMaps);

            // Карты высот
            var heightMaps = LoadMaterialTextureInfo(material, TextureType.Ambient, "texture_height", _assimp);
            if (heightMaps.Any())
                meshData.TextureInfos.AddRange(heightMaps);

            return meshData;
        }

        private static unsafe List<TextureInfo> LoadMaterialTextureInfo(Silk.NET.Assimp.Material* mat, TextureType type, string typeName, Assimp _assimp)
        {
            var textureCount = _assimp.GetMaterialTextureCount(mat, type);
            List<TextureInfo> textureInfos = new List<TextureInfo>();

            for (uint i = 0; i < textureCount; i++)
            {
                AssimpString path;
                _assimp.GetMaterialTexture(mat, type, i, &path, null, null, null, null, null, null);

                textureInfos.Add(new TextureInfo
                {
                    Path = path,
                    Type = type,
                    TypeName = typeName
                });
            }

            return textureInfos;
        }

        private static unsafe Result<ModelData, Error> LoadFromFile(string modelFullPath, Assimp assimp)
        {
            try
            {
                unsafe
                {
                    uint postProcessFlags = (uint)(PostProcessSteps.Triangulate |
                                                  PostProcessSteps.GenerateNormals |
                                                  PostProcessSteps.FlipUVs);

                    Scene* scene = assimp.ImportFile((string)modelFullPath, postProcessFlags);

                    if (scene == null)
                    {
                        string errorMsg = Marshal.PtrToStringAnsi((IntPtr)assimp.GetErrorString());
                        throw new MeshError($"Failed to load model from file: {modelFullPath}. Assimp error: {errorMsg}");
                    }

                    ModelData modelData = new ModelData();
                    modelData.Directory = Path.GetDirectoryName((string)modelFullPath);
                    modelData.NodeMap = new Dictionary<string, NodeData>(StringComparer.OrdinalIgnoreCase);

                    modelData.RootNode = BuildNodeHierarchy(scene->MRootNode, null, modelData.NodeMap);

                    ProcessNode(scene->MRootNode, scene, assimp, modelData);

                    return new Result<ModelData, Error>(modelData);
                }
            }
            catch (Exception ex)
            {
                if (ex is MeshError)
                    return new Result<ModelData, Error>(ex as MeshError);

                return new Result<ModelData, Error>(new MeshError($"Error loading model from file: {ex.Message}"));
            }
        }

        private static string NormalizePath(string path)
        {
            string extension = Path.GetExtension(path);
            string pathWithoutExtension = path.Substring(0, path.Length - extension.Length);

            string normalizedPath = pathWithoutExtension
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('.', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            return normalizedPath + extension;
        }

    }

    public class ModelData
    {
        public string Directory { get; set; } = string.Empty;
        public List<MeshData> Meshes { get; set; } = new List<MeshData>();
        public NodeData RootNode { get; set; }
        public Dictionary<string, NodeData> NodeMap { get; set; } = new Dictionary<string, NodeData>(StringComparer.OrdinalIgnoreCase);

        public NodeData GetNodeByName(string nodeName)
        {
            if (NodeMap.TryGetValue(nodeName, out var node))
                return node;

            return null;
        }

        public List<NodeData> FindNodes(string nameSubstring)
        {
            return NodeMap.Values
                .Where(node => node.Name.Contains(nameSubstring, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public string GetNodePath(NodeData node)
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
    }

    public class NodeData
    {
        public string Name { get; set; }
        public Matrix4x4 Transformation { get; set; }
        public NodeData Parent { get; set; }
        public List<NodeData> Children { get; set; } = new List<NodeData>();
        public List<int> MeshIndices { get; set; } = new List<int>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public NodeData(string name, Matrix4x4 transformation)
        {
            Name = name;
            Transformation = transformation;
        }

        public Matrix4x4 GetGlobalTransformation()
        {
            if (Parent == null)
                return Transformation;

            return Transformation * Parent.GetGlobalTransformation();
        }

        public NodeData FindNode(string nodeName)
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

        public void Traverse(Action<NodeData> action)
        {
            action(this);

            foreach (var child in Children)
            {
                child.Traverse(action);
            }
        }
    }






    //public static class ModelLoader
    //{
    //    private const string BaseNamespace = "OpenglLib";
    //    public static string _customBasePath = AppContext.BaseDirectory;

    //    public static Result<Model, Error> LoadModel(string modelPath, GL gl, Assimp _assimp, bool useEmbeddedResources = true)
    //    {
    //        if (useEmbeddedResources)
    //        {
    //            return LoadFromResources(modelPath, gl, _assimp);
    //        }
    //        return LoadFromFile(modelPath, gl, _assimp);
    //    }

    //    private unsafe static Result<Model, Error> LoadFromResources(string modelPath, GL gl, Assimp _assimp)
    //    {
    //        Scene* scene = GetSceneFromFile(modelPath, _assimp);

    //        Model model = new Model(gl);
    //        model.Directory = modelPath;
    //        model.RootNode = BuildNodeHierarchy(scene->MRootNode, null, model);
    //        ProcessNode(scene->MRootNode, scene, gl, _assimp, model);

    //        return new Result<Model, Error>(model);
    //    }

    //    private static unsafe Scene* GetSceneFromFile(string modelPath, Assimp _assimp)
    //    {
    //        var assembly = Assembly.GetExecutingAssembly();
    //        var resources = assembly.GetManifestResourceNames();
    //        var normalizedModelName = modelPath.Replace('/', '.').Replace('\\', '.');
    //        var resourceName = resources.FirstOrDefault(r =>
    //            r.EndsWith(normalizedModelName, StringComparison.OrdinalIgnoreCase));

    //        if (resourceName == null)
    //        {
    //            var availableModels = string.Join("\n", resources
    //                .Where(r => r.StartsWith(BaseNamespace))
    //                .Select(r => r.Substring(BaseNamespace.Length + 1)));

    //            throw new ShaderError(
    //                $"Model resource not found: {modelPath}\n" +
    //                $"Available models:\n{availableModels}");
    //        }

    //        using var stream = assembly.GetManifestResourceStream(resourceName);
    //        if (stream == null)
    //            throw new ShaderError($"Failed to load shader stream: {resourceName}");

    //        string TrimFilename(string path)
    //        {
    //            var splitedStr = path.Split(".");
    //            if (splitedStr.Length <= 2)
    //                return path;

    //            var span = splitedStr.AsSpan().Slice(1, splitedStr.Length - 1);
    //            var res = string.Join(".", span);

    //            return TrimFilename(res);
    //        }

    //        string modelName = TrimFilename(normalizedModelName);

    //        byte[] modelData = new byte[stream.Length];
    //        stream.Read(modelData, 0, modelData.Length);

    //        fixed (byte* buffer = modelData)
    //        {
    //            Scene* scene = _assimp.ImportFileFromMemory(
    //            buffer,
    //            (uint)modelData.Length,
    //            (uint)(PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals | PostProcessSteps.FlipUVs),
    //            (byte*)Marshal.StringToHGlobalAnsi(modelName));

    //            if (scene == null)
    //            {
    //                var errorMessage = Marshal.PtrToStringAnsi((IntPtr)_assimp.GetErrorString());
    //                throw new MeshError($"Failed to load model from resources: {modelName}. Assimp error: {errorMessage}");
    //            }
    //            return scene;
    //        }
    //    }

    //    private unsafe static MeshNode BuildNodeHierarchy(Node* node, MeshNode parent, Model model)
    //    {
    //        if (node == null)
    //            return null;

    //        // Извлечение имени узла
    //        string nodeName = Marshal.PtrToStringAnsi((IntPtr)node->MName.Data)?.Trim() ?? "UnnamedNode";

    //        // Преобразование матрицы трансформации из Assimp в System.Numerics
    //        var transform = new Matrix4x4(
    //            node->MTransformation.M11, node->MTransformation.M12, node->MTransformation.M13, node->MTransformation.M14,
    //            node->MTransformation.M21, node->MTransformation.M22, node->MTransformation.M23, node->MTransformation.M24,
    //            node->MTransformation.M31, node->MTransformation.M32, node->MTransformation.M33, node->MTransformation.M34,
    //            node->MTransformation.M41, node->MTransformation.M42, node->MTransformation.M43, node->MTransformation.M44
    //        );

    //        // Создание узла
    //        var meshNode = new MeshNode(nodeName, transform);
    //        meshNode.Parent = parent;

    //        // Добавление индексов мешей
    //        for (uint i = 0; i < node->MNumMeshes; i++)
    //        {
    //            meshNode.MeshIndices.Add((int)node->MMeshes[i]);
    //        }

    //        // Добавление узла в словарь для быстрого доступа
    //        model.NodeMap[nodeName] = meshNode;

    //        // Рекурсивное построение дочерних узлов
    //        for (uint i = 0; i < node->MNumChildren; i++)
    //        {
    //            var childNode = BuildNodeHierarchy(node->MChildren[i], meshNode, model);
    //            meshNode.Children.Add(childNode);
    //        }

    //        return meshNode;
    //    }

    //    private static unsafe void ProcessNode(Node* node, Scene* scene, GL gl, Assimp _assimp, Model model)
    //    {
    //        for (var i = 0; i < node->MNumMeshes; i++)
    //        {
    //            var mesh = scene->MMeshes[node->MMeshes[i]];
    //            model.Meshes.Add(ProcessMesh(mesh, scene, gl, _assimp, model));
    //        }

    //        for (var i = 0; i < node->MNumChildren; i++)
    //        {
    //            ProcessNode(node->MChildren[i], scene, gl, _assimp, model);
    //        }
    //    }

    //    private static unsafe Mesh ProcessMesh(AssimpMesh* mesh, Scene* scene, GL gl, Assimp _assimp, Model model)
    //    {
    //        // data to fill
    //        List<Vertex> vertices = new List<Vertex>();
    //        List<uint> indices = new List<uint>();
    //        List<Texture> textures = new List<Texture>();

    //        // walk through each of the mesh's vertices
    //        for (uint i = 0; i < mesh->MNumVertices; i++)
    //        {
    //            Vertex vertex = new Vertex();
    //            vertex.BoneIds = new int[Vertex.MAX_BONE_INFLUENCE];
    //            vertex.Weights = new float[Vertex.MAX_BONE_INFLUENCE];

    //            vertex.Position = mesh->MVertices[i];

    //            // normals
    //            if (mesh->MNormals != null)
    //                vertex.Normal = mesh->MNormals[i];
    //            // tangent
    //            if (mesh->MTangents != null)
    //                vertex.Tangent = mesh->MTangents[i];
    //            // bitangent
    //            if (mesh->MBitangents != null)
    //                vertex.Bitangent = mesh->MBitangents[i];

    //            // texture coordinates
    //            if (mesh->MTextureCoords[0] != null) // does the mesh contain texture coordinates?
    //            {
    //                // a vertex can contain up to 8 different texture coordinates. We thus make the assumption that we won't 
    //                // use models where a vertex can have multiple texture coordinates so we always take the first set (0).
    //                Vector3 texcoord3 = mesh->MTextureCoords[0][i];
    //                vertex.TexCoords = new Vector2(texcoord3.X, texcoord3.Y);
    //            }

    //            vertices.Add(vertex);
    //        }

    //        // now wak through each of the mesh's faces (a face is a mesh its triangle) and retrieve the corresponding vertex indices.
    //        for (uint i = 0; i < mesh->MNumFaces; i++)
    //        {
    //            Face face = mesh->MFaces[i];
    //            // retrieve all indices of the face and store them in the indices vector
    //            for (uint j = 0; j < face.MNumIndices; j++)
    //                indices.Add(face.MIndices[j]);
    //        }

    //        // process materials
    //        Silk.NET.Assimp.Material* material = scene->MMaterials[mesh->MMaterialIndex];
    //        // we assume a convention for sampler names in the shaders. Each diffuse texture should be named
    //        // as 'texture_diffuseN' where N is a sequential number ranging from 1 to MAX_SAMPLER_NUMBER. 
    //        // Same applies to other texture as the following list summarizes:
    //        // diffuse: texture_diffuseN
    //        // specular: texture_specularN
    //        // normal: texture_normalN

    //        // 1. diffuse maps
    //        var diffuseMaps = LoadMaterialTextures(material, TextureType.Diffuse, "texture_diffuse", gl, _assimp, model);
    //        if (diffuseMaps.Any())
    //            textures.AddRange(diffuseMaps);
    //        // 2. specular maps
    //        var specularMaps = LoadMaterialTextures(material, TextureType.Specular, "texture_specular", gl, _assimp, model);
    //        if (specularMaps.Any())
    //            textures.AddRange(specularMaps);
    //        // 3. normal maps
    //        var normalMaps = LoadMaterialTextures(material, TextureType.Height, "texture_normal", gl, _assimp, model);
    //        if (normalMaps.Any())
    //            textures.AddRange(normalMaps);
    //        // 4. height maps
    //        var heightMaps = LoadMaterialTextures(material, TextureType.Ambient, "texture_height", gl, _assimp, model);
    //        if (heightMaps.Any())
    //            textures.AddRange(heightMaps);

    //        // return a mesh object created from the extracted mesh data
    //        var result = Mesh.CreateStandardMesh(gl, BuildVertices(vertices), BuildIndices(indices), textures);
    //        return result;
    //    }

    //    private static unsafe List<Texture> LoadMaterialTextures(Silk.NET.Assimp.Material* mat, TextureType type, string typeName, GL gl, Assimp _assimp, Model model)
    //    {
    //        var textureCount = _assimp.GetMaterialTextureCount(mat, type);
    //        List<Texture> textures = new List<Texture>();
    //        for (uint i = 0; i < textureCount; i++)
    //        {
    //            AssimpString path;
    //            _assimp.GetMaterialTexture(mat, type, i, &path, null, null, null, null, null, null);
    //            bool skip = false;
    //            for (int j = 0; j < model._texturesLoaded.Count; j++)
    //            {
    //                if (model._texturesLoaded[j].Path == path)
    //                {
    //                    textures.Add(model._texturesLoaded[j]);
    //                    skip = true;
    //                    break;
    //                }
    //            }
    //            if (!skip)
    //            {
    //                var texture = new Texture(gl, model.Directory, type: type);
    //                texture.Path = path;
    //                textures.Add(texture);
    //                model._texturesLoaded.Add(texture);
    //            }
    //        }
    //        return textures;
    //    }

    //    private static float[] BuildVertices(List<Vertex> vertexCollection)
    //    {
    //        var vertices = new List<float>();

    //        foreach (var vertex in vertexCollection)
    //        {
    //            vertices.Add(vertex.Position.X);
    //            vertices.Add(vertex.Position.Y);
    //            vertices.Add(vertex.Position.Z);

    //            vertices.Add(vertex.Normal.X);
    //            vertices.Add(vertex.Normal.Y);
    //            vertices.Add(vertex.Normal.Z);

    //            vertices.Add(vertex.TexCoords.X);
    //            vertices.Add(vertex.TexCoords.Y);
    //        }

    //        return vertices.ToArray();
    //    }

    //    private static uint[] BuildIndices(List<uint> indices)
    //    {
    //        return indices.ToArray();
    //    }

    //    private static unsafe Result<Model, Error> LoadFromFile(string modelFullPath, GL gl, Assimp assimp)
    //    {
    //        try
    //        {
    //            unsafe
    //            {
    //                uint postProcessFlags = (uint)(PostProcessSteps.Triangulate |
    //                                              PostProcessSteps.GenerateNormals |
    //                                              PostProcessSteps.FlipUVs);

    //                Scene* scene = assimp.ImportFile((string)modelFullPath, postProcessFlags);

    //                if (scene == null)
    //                {
    //                    string errorMsg = Marshal.PtrToStringAnsi((IntPtr)assimp.GetErrorString());
    //                    throw new MeshError($"Failed to load model from file: {modelFullPath}. Assimp error: {errorMsg}");
    //                }

    //                Model model = new Model(gl);
    //                model.Directory = Path.GetDirectoryName((string)modelFullPath);

    //                // Сначала строим иерархию узлов
    //                model.RootNode = BuildNodeHierarchy(scene->MRootNode, null, model);

    //                // Затем обрабатываем меши
    //                ProcessNode(scene->MRootNode, scene, gl, assimp, model);

    //                return new Result<Model, Error>(model);
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            if (ex is MeshError)
    //                return new Result<Model, Error>(ex as MeshError);

    //            return new Result<Model, Error>(new MeshError($"Error loading model from file: {ex.Message}"));
    //        }
    //    }

    //    private static string NormalizePath(string path)
    //    {
    //        string extension = Path.GetExtension(path);
    //        string pathWithoutExtension = path.Substring(0, path.Length - extension.Length);

    //        string normalizedPath = pathWithoutExtension
    //            .Replace('/', Path.DirectorySeparatorChar)
    //            .Replace('\\', Path.DirectorySeparatorChar)
    //            .Replace('.', Path.DirectorySeparatorChar)
    //            .TrimStart(Path.DirectorySeparatorChar);

    //        return normalizedPath + extension;
    //    }
    //}

}
