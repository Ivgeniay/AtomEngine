using AtomEngine;
using Silk.NET.Assimp;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;

namespace OpenglLib
{ 
    public class ModelLoader
    {
        private readonly Assimp _assimp;
        private readonly GL _gl;

        public ModelLoader(GL gl)
        {
            _assimp = Assimp.GetApi();
            _gl = gl;
        }

        public unsafe Result<Node, Error> LoadModel(string path)
        {
            try
            {
                if (!System.IO.File.Exists(path))
                {
                    return new Result<Node, Error>(
                        new MeshError($"Model file does not exist: {path}"));
                }

                Scene* scene = _assimp.ImportFile(path,
                    (uint)(PostProcessSteps.Triangulate |
                           PostProcessSteps.GenerateNormals |
                           PostProcessSteps.FlipUVs));

                if (scene == null)
                {
                    var errorMessage = Marshal.PtrToStringAnsi((IntPtr)_assimp.GetErrorString());
                    return new Result<Node, Error>(
                        new MeshError($"Failed to load model: {path}. Assimp error: {errorMessage}"));
                }

                Node rootNode = ProcessNode(scene->MRootNode, scene);
                _assimp.ReleaseImport(scene);
                return new Result<Node, Error>(rootNode);
            }
            catch (Exception ex)
            {
                return new Result<Node, Error>(
                    new MeshError($"Error loading model {path}: {ex.Message}"));
            }
        }

        private unsafe Node ProcessNode(Silk.NET.Assimp.Node* assimpNode, Scene* scene)
        {
            // Создаем новый узел нашего типа
            var transform = *(Matrix4X4<float>*)&assimpNode->MTransformation;
            var newNode = new Node(
                Marshal.PtrToStringAnsi((IntPtr)assimpNode->MName.Data) ?? "unnamed",
                transform);

            // Добавляем все меши, принадлежащие этому узлу
            for (int i = 0; i < assimpNode->MNumMeshes; i++)
            {
                uint meshIndex = assimpNode->MMeshes[i];
                newNode.Meshes.Add(MakeMesh(scene, (int)meshIndex));
            }

            // Рекурсивно обрабатываем все дочерние узлы
            if (assimpNode->MChildren != null)
            {
                for (int i = 0; i < assimpNode->MNumChildren; i++)
                {
                    var childNode = assimpNode->MChildren[i];
                    if (childNode != null)
                    {
                        try
                        {
                            var processedNode = ProcessNode(childNode, scene);
                            newNode.Children.Add(processedNode);
                        }
                        catch (MeshError ex)
                        {
                            DebLogger.Error($"{ex}");
                            DebLogger.Error($"Processing node: {Marshal.PtrToStringAnsi((IntPtr)assimpNode->MName.Data)}");
                            DebLogger.Error($"Number of children: {assimpNode->MNumChildren}");
                        }
                    }
                }
            }

            return newNode;
        }

        private unsafe Mesh MakeMesh(Scene* scene, int meshIndex)
        {
            var mesh = scene->MMeshes[meshIndex];
            var vertices = new List<float>();

            // Собираем вершины для текущего меша
            for (int i = 0; i < mesh->MNumVertices; i++)
            {
                // Позиция
                vertices.Add(mesh->MVertices[i].X);
                vertices.Add(mesh->MVertices[i].Y);
                vertices.Add(mesh->MVertices[i].Z);

                // Нормали (если есть)
                if (mesh->MNormals != null)
                {
                    vertices.Add(mesh->MNormals[i].X);
                    vertices.Add(mesh->MNormals[i].Y);
                    vertices.Add(mesh->MNormals[i].Z);
                }

                // UV координаты (если есть)
                if (mesh->MTextureCoords[0] != null)
                {
                    vertices.Add(mesh->MTextureCoords[0][i].X);
                    vertices.Add(mesh->MTextureCoords[0][i].Y);
                }
            }

            // Собираем индексы
            var indices = new List<uint>();
            for (int i = 0; i < mesh->MNumFaces; i++)
            {
                var face = mesh->MFaces[i];
                for (int j = 0; j < face.MNumIndices; j++)
                {
                    indices.Add(face.MIndices[j]);
                }
            }

            var _mesh = new Mesh(_gl, vertices.ToArray(), indices.ToArray());
            return _mesh;
        }
    }


}
