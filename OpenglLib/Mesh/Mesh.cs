using AtomEngine.RenderEntity;
using OpenglLib.Buffers;
using System.Numerics;
using Silk.NET.OpenGL;
using AtomEngine;

namespace OpenglLib
{
    public class Mesh : MeshBase
    {
        public VertexArrayObject<float, uint> VAO { get; set; }
        public BufferObject<float> VBO { get; set; }
        public BufferObject<uint> EBO { get; set; }
        public GL GL { get; }
        public Shader OptimizedShader { get; private set; }

        private readonly PrimitiveType _primitiveType;
        private VertexFormat _format;

        public Mesh(GL gl, float[] vertices, uint[] indices, VertexFormat format, Shader optimizedShader = null, PrimitiveType primitiveType = PrimitiveType.Triangles)
        {
            GL = gl;
            _format = format;
            _primitiveType = primitiveType;
            OptimizedShader = optimizedShader;
            Vertices = vertices;
            Indices = indices;

            SetupMesh();
            BoundingVolume = new BoundingBox(this);
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
        public void Bind() => VAO.Bind();
        public unsafe override void Draw() => DrawAs(OptimizedShader, _primitiveType);
        public unsafe override void Draw(ShaderBase shader) => DrawAs(shader, _primitiveType);
        public unsafe void DrawAs(ShaderBase shader, PrimitiveType primitiveType)
        {
            if (shader != null)
            {
                shader?.Use();
                VAO.Bind();
                GL.DrawElements(primitiveType, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
                VAO.Unbind();
            }
            else
            {
#if DEBUG
                DebLogger.Error("There is no shader for drawing mesh");
#endif
            }
        }
        public override void Dispose()
        {
            VAO.Dispose();
            VBO.Dispose();
            EBO.Dispose();
        }
    }

    public class VertexAttributeDescriptor
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
        public List<VertexAttributeDescriptor> Attributes { get; } = new List<VertexAttributeDescriptor>();
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
