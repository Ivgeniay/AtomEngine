using AtomEngine.RenderEntity;
using AtomEngine;
using EngineLib;
using OpenglLib.Buffers;
using Silk.NET.OpenGL;
using System.Numerics;

namespace OpenglLib
{
    public class Mesh : MeshBase
    {
        public IReadOnlyList<Texture> Textures { get; private set; }
        public VertexArrayObject<float, uint> VAO { get; set; }
        public BufferObject<float> VBO { get; set; }
        public BufferObject<uint> EBO { get; set; }
        public GL GL { get; }
        private readonly PrimitiveType _primitiveType;
        private VertexFormat _format;

        public Mesh(GL gl, float[] vertices, uint[] indices, VertexFormat format, List<Texture> textures = null, PrimitiveType primitiveType = PrimitiveType.Triangles)
        {
            GL = gl;
            _format = format;
            _primitiveType = primitiveType;
            Vertices = vertices;
            Indices = indices;
            Textures = textures ?? new List<Texture>();
            FillingVertices(vertices, indices);

            ValidateVertexData();
            SetupMesh();
        }
        public Mesh(GL gl, float[] vertices, uint[] indices, List<Texture> textures, PrimitiveType primitiveType = PrimitiveType.Triangles)
        {
            if (vertices.Length % 8 != 0)
                throw new ArgumentError("Vertices array length must be multiple of 8");
            if (indices.Length % 3 != 0)
                throw new ArgumentError("Indices array length must be multiple of 3");

            GL = gl;
            Vertices = vertices;
            Indices = indices;
            Textures = textures;
            _primitiveType = primitiveType;
            FillingVertices(vertices, indices);

            SetupMesh();
        }

        private void ValidateVertexData()
        {
            //if (Vertices.Length % (_format.Stride / sizeof(float)) != 0)
            //    throw new ArgumentError($"Vertices array length must be multiple of {_format.Stride / sizeof(float)}");

            //if (_primitiveType == PrimitiveType.Triangles && Indices.Length % 3 != 0)
            //    throw new ArgumentError("Indices array length must be multiple of 3 for triangle primitives");
        }

        private Dictionary<string, int> _attributeOffsets;

        private void InitializeAttributeOffsets()
        {
            _attributeOffsets = new Dictionary<string, int>();
            foreach (var attr in _format.Attributes)
            {
                _attributeOffsets[attr.Name] = attr.Offset / sizeof(float);
            }
        }

        private void FillingVertices(float[] vertices, uint[] indices)
        {
            if (_format == null)
            {
                // Для случая с Vector3, у нас только позиции
                _format = new VertexFormat();
                _format.AddAttribute("position", 0, 3);
            }

            // Для Vector3 у нас 3 компонента на вершину
            int componentsPerVertex = 3;
            int vertexCount = vertices.Length / componentsPerVertex;

            // Создаем массив правильного размера
            Vertices_ = new Vertex[vertexCount];

            for (int i = 0; i < vertexCount; i++)
            {
                int baseIndex = i * componentsPerVertex;

                Vertices_[i] = new Vertex
                {
                    Index = i,
                    Position = new Vector3(
                        vertices[baseIndex],
                        vertices[baseIndex + 1],
                        vertices[baseIndex + 2]
                    )
                };
            }

            //if (_primitiveType == PrimitiveType.Triangles)
            //{
            //    // Проверяем, что количество индексов кратно 3
            //    if (indices.Length % 3 != 0)
            //        throw new ArgumentException("For triangles, the number of indices must be a multiple of 3");

            //    Triangles = new Triangle[indices.Length / 3];

            //    for (int i = 0; i < indices.Length; i += 3)
            //    {
            //        // Проверяем, что индексы не выходят за границы массива
            //        if (indices[i] >= vertexCount ||
            //            indices[i + 1] >= vertexCount ||
            //            indices[i + 2] >= vertexCount)
            //        {
            //            throw new ArgumentException($"Index {indices[i]}, {indices[i + 1]}, or {indices[i + 2]} is out of range for vertex count {vertexCount}");
            //        }

            //        Triangles[i / 3] = new Triangle(
            //            Vertices_[indices[i]].Position,
            //            Vertices_[indices[i + 1]].Position,
            //            Vertices_[indices[i + 2]].Position
            //        );
            //    }
            //}
            //else
            //{
            //    Triangles = Array.Empty<Triangle>();
            //}
        }

        public static Mesh CreateWireframeMesh(GL gl, Vector3[] vertices, uint[] indices, List<Texture> textures = null)
        {
            List<float> verticesList = new List<float>();
            foreach (var vertex in vertices)
            {
                verticesList.Add(vertex.X);
                verticesList.Add(vertex.Y);
                verticesList.Add(vertex.Z);
            }

            var format = new VertexFormat();
            format.AddAttribute("position", 0, 3);
            return Mesh.CreateWireframeMesh(gl, verticesList.ToArray(), indices, textures);
        }
        public static Mesh CreateWireframeMesh(GL gl, float[] vertices, uint[] indices, List<Texture> textures = null)
        {
            var format = new VertexFormat();
            format.AddAttribute("position", 0, 3);

            return new Mesh(gl, vertices, indices, format, textures, PrimitiveType.Triangles);
        }
        public static Mesh CreateStandardMesh(GL gl, float[] vertices, uint[] indices, List<Texture> textures = null)
        {
            var format = new VertexFormat();
            format.AddAttribute("position", 0, 3);
            format.AddAttribute("normal", 1, 3);  
            format.AddAttribute("texCoord", 2, 2);

            return new Mesh(gl, vertices, indices, format, textures);
        }

        private unsafe void SetupMesh()
        {
            EBO = new BufferObject<uint>(GL, Indices, BufferTargetARB.ElementArrayBuffer);
            VBO = new BufferObject<float>(GL, Vertices, BufferTargetARB.ArrayBuffer);
            VAO = new VertexArrayObject<float, uint>(GL, VBO, EBO);

            foreach (var attr in _format.Attributes)
            {
                VAO.VertexAttributePointer(
                    attr.Location,
                    attr.Size,
                    attr.Type,
                    (uint)_format.Stride / sizeof(float),
                    attr.Offset / sizeof(float)
                );
            }

            VAO.Unbind();
            VBO.Unbind();
            EBO.Unbind();
        }

        public void Bind()
        {
            VAO.Bind();
        }

        public override void Dispose()
        {
            if (Textures != null && Textures.Count > 0)
            {
                foreach (var texture in Textures)
                {
                    texture.Dispose();
                }
            }
            Textures = null;
            VAO.Dispose();
            VBO.Dispose();
            EBO.Dispose();
        }

        public unsafe override void Draw(ShaderBase shader)
        {
            shader.Use();
            VAO.Bind();
            GL.DrawElements(_primitiveType, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
            VAO.Unbind();
        }

        public unsafe void DrawAs(ShaderBase shader, PrimitiveType primitiveType)
        {
            shader.Use();
            VAO.Bind();
            GL.DrawElements(primitiveType, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
            VAO.Unbind();
        }
    }

    public struct VertexAttributeDescriptor
    {
        public string Name;                  
        public uint Location;                
        public int Size;                     
        public int Offset;                   
        public VertexAttribPointerType Type; 

        public VertexAttributeDescriptor(string name, uint location, int size, int offset,
            VertexAttribPointerType type = VertexAttribPointerType.Float)
        {
            Name = name;
            Location = location;
            Size = size;
            Offset = offset;
            Type = type;
        }
    }

    public class VertexFormat
    {
        public List<VertexAttributeDescriptor> Attributes { get; } = new();
        public int Stride { get; private set; }

        public void AddAttribute(string name, uint location, int size, VertexAttribPointerType type = VertexAttribPointerType.Float)
        {
            var attribute = new VertexAttributeDescriptor(name, location, size, Stride, type);
            Attributes.Add(attribute);
            Stride += size * GetSizeForType(type);
        }

        private int GetSizeForType(VertexAttribPointerType type)
        {
            return type switch
            {
                VertexAttribPointerType.Byte => sizeof(byte),
                VertexAttribPointerType.UnsignedByte => sizeof(byte),
                VertexAttribPointerType.Short => sizeof(short),
                VertexAttribPointerType.UnsignedShort => sizeof(short),
                VertexAttribPointerType.Int => sizeof(int),
                VertexAttribPointerType.UnsignedInt => sizeof(int),
                VertexAttribPointerType.Float => sizeof(float),
                VertexAttribPointerType.Double => sizeof(double),

                _ => throw new ArgumentException($"Unsupported vertex attribute type: {type}")
            };
        }
    }
}
