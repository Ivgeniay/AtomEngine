using Silk.NET.OpenGL;

namespace OpenglLib
{
    public class MeshBuilder
    {
        private readonly GL _gl;
        private readonly Shader _shader;

        private static readonly Dictionary<string, (int size, VertexAttribPointerType type)> StandardAttributes =
            new Dictionary<string, (int, VertexAttribPointerType)>(StringComparer.OrdinalIgnoreCase)
        {
            { "aPosition", (3, VertexAttribPointerType.Float) },
            { "aNormal", (3, VertexAttribPointerType.Float) },
            { "aTexCoord", (2, VertexAttribPointerType.Float) },
            { "aTangent", (3, VertexAttribPointerType.Float) },
            { "aBitangent", (3, VertexAttribPointerType.Float) },
            { "aColor", (4, VertexAttribPointerType.Float) },
            { "aBoneIds", (4, VertexAttribPointerType.Float) },
            { "aWeights", (4, VertexAttribPointerType.Float) },
            
            { "position", (3, VertexAttribPointerType.Float) },
            { "normal", (3, VertexAttribPointerType.Float) },
            { "texCoord", (2, VertexAttribPointerType.Float) },
            { "tangent", (3, VertexAttribPointerType.Float) },
            { "bitangent", (3, VertexAttribPointerType.Float) },
            { "color", (4, VertexAttribPointerType.Float) },
            { "boneIds", (4, VertexAttribPointerType.Float) },
            { "weights", (4, VertexAttribPointerType.Float) }
        };

        public MeshBuilder(GL gl, Shader shader)
        {
            _gl = gl;
            _shader = shader;
        }

        public Mesh BuildMesh(MeshData meshData)
        {
            var (vertexArray, format) = CreateVertexArrayAndFormat(meshData);
            var indices = meshData.GetIndices();

            return new Mesh(_gl, vertexArray, indices, format, _shader);
        }

        private (float[] vertexArray, VertexFormat format) CreateVertexArrayAndFormat(MeshData meshData)
        {
            var shaderAttributes = _shader.GetAllAttributeLocations();
            var format = new VertexFormat();

            Dictionary<string, int> offsets = new Dictionary<string, int>();
            int stride = 0;

            foreach (var attr in shaderAttributes)
            {
                if (StandardAttributes.TryGetValue(attr.Key, out var attrInfo))
                {
                    offsets[attr.Key] = stride;
                    format.AddAttribute(attr.Key, attr.Value, attrInfo.size, attrInfo.type);
                    stride += attrInfo.size * sizeof(float);
                }
            }

            if (offsets.Count == 0)
            {
                return (Array.Empty<float>(), format);
            }

            int componentsPerVertex = stride / sizeof(float);
            float[] vertexArray = new float[meshData.Vertices.Count * componentsPerVertex];

            for (int i = 0; i < meshData.Vertices.Count; i++)
            {
                int baseIndex = i * componentsPerVertex;
                var vertex = meshData.Vertices[i];

                foreach (var attr in shaderAttributes)
                {
                    if (offsets.TryGetValue(attr.Key, out int offset))
                    {
                        int vertexOffset = baseIndex + (offset / sizeof(float));
                        string attrLower = attr.Key.ToLowerInvariant();

                        if (attrLower == "aposition" || attrLower == "position")
                        {
                            vertexArray[vertexOffset] = vertex.Position.X;
                            vertexArray[vertexOffset + 1] = vertex.Position.Y;
                            vertexArray[vertexOffset + 2] = vertex.Position.Z;
                        }
                        else if (attrLower == "anormal" || attrLower == "normal")
                        {
                            vertexArray[vertexOffset] = vertex.Normal.X;
                            vertexArray[vertexOffset + 1] = vertex.Normal.Y;
                            vertexArray[vertexOffset + 2] = vertex.Normal.Z;
                        }
                        else if (attrLower == "atexcoord" || attrLower == "texcoord")
                        {
                            vertexArray[vertexOffset] = vertex.TexCoords.X;
                            vertexArray[vertexOffset + 1] = vertex.TexCoords.Y;
                        }
                        else if (attrLower == "atangent" || attrLower == "tangent")
                        {
                            vertexArray[vertexOffset] = vertex.Tangent.X;
                            vertexArray[vertexOffset + 1] = vertex.Tangent.Y;
                            vertexArray[vertexOffset + 2] = vertex.Tangent.Z;
                        }
                        else if (attrLower == "abitangent" || attrLower == "bitangent")
                        {
                            vertexArray[vertexOffset] = vertex.Bitangent.X;
                            vertexArray[vertexOffset + 1] = vertex.Bitangent.Y;
                            vertexArray[vertexOffset + 2] = vertex.Bitangent.Z;
                        }
                        else if (attrLower == "aboneids" || attrLower == "boneids")
                        {
                            for (int j = 0; j < Math.Min(4, vertex.BoneIds.Length); j++)
                            {
                                vertexArray[vertexOffset + j] = vertex.BoneIds[j];
                            }
                        }
                        else if (attrLower == "aweights" || attrLower == "weights")
                        {
                            for (int j = 0; j < Math.Min(4, vertex.Weights.Length); j++)
                            {
                                vertexArray[vertexOffset + j] = vertex.Weights[j];
                            }
                        }
                        else if (attrLower == "acolor" || attrLower == "color")
                        {
                            vertexArray[vertexOffset] = vertex.Color.X;
                            vertexArray[vertexOffset + 1] = vertex.Color.Y;
                            vertexArray[vertexOffset + 2] = vertex.Color.Z;
                            vertexArray[vertexOffset + 3] = vertex.Color.W;
                        }
                    }
                }
            }

            return (vertexArray, format);
        }
    }

}
