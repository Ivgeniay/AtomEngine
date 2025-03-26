using System.Text.RegularExpressions;

namespace OpenglLib
{
    public static class GlslParser
    {
        public static bool IsCompleteShaderFile(string source)
        {
            return source.Contains("#vertex") && source.Contains("#fragment");
        }

        public static string ProcessIncludesRecursively(string source, string sourcePath, HashSet<string> processedPaths = null)
        {
            processedPaths ??= new HashSet<string>();
            return IncludeProcessor.ProcessIncludes(source, sourcePath, processedPaths);
        }

        public static string CleanComments(string source)
        {
            string noSingleLineComments = Regex.Replace(source, @"//.*$", "", RegexOptions.Multiline);
            return Regex.Replace(noSingleLineComments, @"/\*[\s\S]*?\*/", "");
        }

        public static HashSet<int> ExtractUniformBlockBindings(string shaderSource)
        {
            var bindings = new HashSet<int>();
            var bindingRegex = new Regex(@"layout\s*\(\s*std140\s*,\s*binding\s*=\s*(\d+)\s*\)", RegexOptions.Multiline);

            foreach (Match match in bindingRegex.Matches(shaderSource))
            {
                if (match.Groups.Count > 1 && int.TryParse(match.Groups[1].Value, out int bindingPoint))
                {
                    bindings.Add(bindingPoint);
                }
            }

            return bindings;
        }

        public static (string vertex, string fragment) ExtractShaderSources(string source)
        {
            source = CleanComments(source);

            string vertexSource = "";
            string fragmentSource = "";
            try
            {
                var vertexRegex = new Regex(@"#vertex\r?\n(.*?)(?=#fragment|$)", RegexOptions.Singleline);
                var fragmentRegex = new Regex(@"#fragment\r?\n(.*?)$", RegexOptions.Singleline);

                var vertexMatch = vertexRegex.Match(source);
                var fragmentMatch = fragmentRegex.Match(source);

                if (vertexMatch.Success)
                {
                    vertexSource = vertexMatch.Groups[1].Value.Trim();
                }

                if (fragmentMatch.Success)
                {
                    fragmentSource = fragmentMatch.Groups[1].Value.Trim();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при извлечении шейдеров: {ex.Message}", ex);
            }

            return (vertexSource, fragmentSource);
        }

        public static void ValidateMainFunctions(string vertexSource, string fragmentSource)
        {
            var mainRegex = new Regex(@"void\s+main\s*\(\s*\)\s*{");

            var vertexMainCount = mainRegex.Matches(vertexSource).Count;
            var fragmentMainCount = mainRegex.Matches(fragmentSource).Count;

            if (vertexMainCount != 1)
            {
                throw new Exception($"Vertex shader must have exactly one main function. Found: {vertexMainCount}");
            }

            if (fragmentMainCount != 1)
            {
                throw new Exception($"Fragment shader must have exactly one main function. Found: {fragmentMainCount}");
            }
        }

        public static List<(string type, string name, int? arraySize)> ExtractUniforms(string source)
        {
            var uniforms = new List<(string type, string name, int? arraySize)>();
            //var uniformRegex = new Regex(@"uniform\s+(?!layout)(\w+)\s+(\w+)(?:\[(\d+)\])?\s*;");
            var uniformRegex = new Regex(@"uniform\s+(?!layout)(?:highp|mediump|lowp)?\s*(\w+)\s+(\w+)(?:\[(\d+)\])?\s*;");

            foreach (Match match in uniformRegex.Matches(source))
            {
                var type = match.Groups[1].Value;
                var name = match.Groups[2].Value;
                int? arraySize = null;

                if (match.Groups[3].Success)
                {
                    arraySize = int.Parse(match.Groups[3].Value);
                }

                uniforms.Add((type, name, arraySize));
            }

            return uniforms;
        }

        public static List<GlslStructure> ParseGlslStructures(string sourceCode)
        {
            var structures = new List<GlslStructure>();
            var structRegex = new Regex(@"struct\s+(\w+)\s*\{([^}]+)\}", RegexOptions.Multiline);
            var matches = structRegex.Matches(sourceCode);

            foreach (Match match in matches)
            {
                var structure = new GlslStructure
                {
                    Name = match.Groups[1].Value,
                    Fields = ParseFields(match.Groups[2].Value)
                };
                structures.Add(structure);
            }

            return structures;
        }

        public static List<UniformBlockStructure> ParseUniformBlocks(string source)
        {
            var blocks = new List<UniformBlockStructure>();
            var blockRegex = new Regex(
                @"(?:layout\s*\(\s*(std140|std430|packed|shared)(?:\s*,\s*binding\s*=\s*(\d+))?\)\s*)?" +
                @"uniform\s+(\w+)?\s*\{([^}]+)\}\s*(\w+)?;",
                RegexOptions.Multiline);

            foreach (Match match in blockRegex.Matches(source))
            {
                var layoutTypeStr = match.Groups[1].Success ? match.Groups[1].Value : null;
                var bindingStr = match.Groups[2].Success ? match.Groups[2].Value : null;
                int? binding = bindingStr != null ? int.Parse(bindingStr) : null;
                var blockName = match.Groups[3].Success ? match.Groups[3].Value : null;
                var fieldsText = match.Groups[4].Value;
                var instanceName = match.Groups[5].Success ? match.Groups[5].Value : null;
                var name = blockName ?? instanceName;

                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                UniformBlockType blockType = GetUniformBlockType(layoutTypeStr);

                blocks.Add(new UniformBlockStructure
                {
                    Name = name,
                    UniformBlockType = blockType,
                    Binding = binding,
                    Fields = ParseFields(fieldsText),
                    InstanceName = instanceName
                });
            }

            return blocks;
        }

        public static List<(string Type, string Name, int? ArraySize)> ParseFields(string fieldsText)
        {
            var fields = new List<(string Type, string Name, int? ArraySize)>();

            fieldsText = Regex.Replace(fieldsText, @"//.*$", "", RegexOptions.Multiline);
            fieldsText = Regex.Replace(fieldsText, @"/\*[\s\S]*?\*/", "");

            var fieldRegex = new Regex(@"(?<type>\w+)\s+(?<name>\w+)(?:\[(?<size>\d+)\])?\s*;", RegexOptions.Multiline);

            foreach (Match match in fieldRegex.Matches(fieldsText))
            {
                var type = match.Groups["type"].Value;
                var name = match.Groups["name"].Value;
                int? arraySize = null;

                if (match.Groups["size"].Success)
                {
                    arraySize = int.Parse(match.Groups["size"].Value);
                }

                fields.Add((type, name, arraySize));
            }

            return fields;
        }

        public static string MapGlslTypeToCSharp(string glslType, HashSet<string> generatedTypes = null)
        {
            if (generatedTypes != null && generatedTypes.Contains(glslType))
            {
                return glslType;
            }

            return glslType switch
            {
                "bool" => "bool",
                "int" => "int",
                "uint" => "uint",
                "float" => "float",
                "double" => "double",

                "bvec2" => "Vector2D<bool>",
                "bvec3" => "Vector3D<bool>",
                "bvec4" => "Vector4D<bool>",

                "ivec2" => "Vector2D<int>",
                "ivec3" => "Vector3D<int>",
                "ivec4" => "Vector4D<int>",

                "uvec2" => "Vector2D<uint>",
                "uvec3" => "Vector3D<uint>",
                "uvec4" => "Vector4D<uint>",

                "vec2" => "Vector2D<float>",
                "vec3" => "Vector3D<float>",
                "vec4" => "Vector4D<float>",

                "mat2" => "Matrix2X2<float>",
                "mat3" => "Matrix3X3<float>",
                "mat4" => "Matrix4X4<float>",
                "mat2x2" => "Matrix2X2<float>",
                "mat2x3" => "Matrix2X3<float>",
                "mat2x4" => "Matrix2X4<float>",
                "mat3x2" => "Matrix3X2<float>",
                "mat3x3" => "Matrix3X3<float>",
                "mat3x4" => "Matrix3X4<float>",
                "mat4x2" => "Matrix4X2<float>",
                "mat4x3" => "Matrix4X3<float>",
                "mat4x4" => "Matrix4X4<float>",

                "sampler1D" => "int",
                "sampler2D" => "int",
                "sampler3D" => "int",
                "samplerCube" => "int",
                "sampler2DRect" => "int",
                "sampler1DArray" => "int",
                "sampler2DArray" => "int",
                "samplerCubeArray" => "int",
                "samplerBuffer" => "int",
                "sampler2DMS" => "int",
                "sampler2DMSArray" => "int",

                "sampler1DShadow" => "int",
                "sampler2DShadow" => "int",
                "samplerCubeShadow" => "int",
                "sampler2DRectShadow" => "int",
                "sampler1DArrayShadow" => "int",
                "sampler2DArrayShadow" => "int",
                "samplerCubeArrayShadow" => "int",

                "isampler1D" => "int",
                "isampler2D" => "int",
                "isampler3D" => "int",
                "isamplerCube" => "int",
                "isampler2DRect" => "int",
                "isampler1DArray" => "int",
                "isampler2DArray" => "int",
                "isamplerCubeArray" => "int",
                "isamplerBuffer" => "int",
                "isampler2DMS" => "int",
                "isampler2DMSArray" => "int",

                "usampler1D" => "int",
                "usampler2D" => "int",
                "usampler3D" => "int",
                "usamplerCube" => "int",
                "usampler2DRect" => "int",
                "usampler1DArray" => "int",
                "usampler2DArray" => "int",
                "usamplerCubeArray" => "int",
                "usamplerBuffer" => "int",
                "usampler2DMS" => "int",
                "usampler2DMSArray" => "int",

                "atomic_uint" => "uint",
                "image1D" => "int",
                "image2D" => "int",
                "image3D" => "int",
                "image2DRect" => "int",
                "imageCube" => "int",
                "image1DArray" => "int",
                "image2DArray" => "int",
                "imageBuffer" => "int",
                "image2DMS" => "int",
                "image2DMSArray" => "int",

                "void" => "void",
                "struct" => "struct",
                _ => glslType
            };
        }

        public static string GetTextureTarget(string samplerType)
        {
            return samplerType switch
            {
                "sampler1D" => "Texture1D",
                "sampler2D" => "Texture2D",
                "sampler3D" => "Texture3D",
                "samplerCube" => "TextureCubeMap",
                "sampler2DRect" => "TextureRectangle",

                "sampler1DArray" => "Texture1DArray",
                "sampler2DArray" => "Texture2DArray",
                "samplerCubeArray" => "TextureCubeMapArray",
                "samplerBuffer" => "TextureBuffer",

                "sampler2DMS" => "Texture2DMultisample",
                "sampler2DMSArray" => "Texture2DMultisampleArray",

                "sampler1DShadow" => "Texture1D",
                "sampler2DShadow" => "Texture2D",
                "samplerCubeShadow" => "TextureCubeMap",
                "sampler2DRectShadow" => "TextureRectangle",
                "sampler1DArrayShadow" => "Texture1DArray",
                "sampler2DArrayShadow" => "Texture2DArray",
                "samplerCubeArrayShadow" => "TextureCubeMapArray",

                "isampler1D" => "Texture1D",
                "isampler2D" => "Texture2D",
                "isampler3D" => "Texture3D",
                "isamplerCube" => "TextureCubeMap",
                "isampler2DRect" => "TextureRectangle",
                "isampler1DArray" => "Texture1DArray",
                "isampler2DArray" => "Texture2DArray",
                "isamplerCubeArray" => "TextureCubeMapArray",
                "isamplerBuffer" => "TextureBuffer",
                "isampler2DMS" => "Texture2DMultisample",
                "isampler2DMSArray" => "Texture2DMultisampleArray",

                "usampler1D" => "Texture1D",
                "usampler2D" => "Texture2D",
                "usampler3D" => "Texture3D",
                "usamplerCube" => "TextureCubeMap",
                "usampler2DRect" => "TextureRectangle",
                "usampler1DArray" => "Texture1DArray",
                "usampler2DArray" => "Texture2DArray",
                "usamplerCubeArray" => "TextureCubeMapArray",
                "usamplerBuffer" => "TextureBuffer",
                "usampler2DMS" => "Texture2DMultisample",
                "usampler2DMSArray" => "Texture2DMultisampleArray",

                _ => throw new ArgumentException($"Unknown sampler type: {samplerType}")
            };
        }

        public static bool IsGlslBaseType(string type)
        {
            return type switch
            {
                "bool" or "int" or "uint" or "float" or "double" or

                "bvec2" or "bvec3" or "bvec4" or
                "ivec2" or "ivec3" or "ivec4" or
                "uvec2" or "uvec3" or "uvec4" or
                "vec2" or "vec3" or "vec4" or
                "dvec2" or "dvec3" or "dvec4" or

                "mat2" or "mat3" or "mat4" or
                "mat2x2" or "mat2x3" or "mat2x4" or
                "mat3x2" or "mat3x3" or "mat3x4" or
                "mat4x2" or "mat4x3" or "mat4x4" or
                "dmat2" or "dmat3" or "dmat4" or
                "dmat2x2" or "dmat2x3" or "dmat2x4" or
                "dmat3x2" or "dmat3x3" or "dmat3x4" or
                "dmat4x2" or "dmat4x3" or "dmat4x4" => true,
                _ => false
            };
        }

        public static bool IsCustomType(string csharpType, string type) =>
            csharpType == type && type != "float" && type != "bool" && type != "int" && type != "uint" && type != "float" && type != "double";

        private static UniformBlockType GetUniformBlockType(string layoutType)
        {
            return layoutType.ToLower() switch
            {
                "std140" => UniformBlockType.STD140,
                "std430" => UniformBlockType.STD430,
                "packed" => UniformBlockType.Packed,
                "shared" => UniformBlockType.Shared,
                _ => UniformBlockType.Ordinary
            };
        }
    }

    public class GlslStructure
    {
        public string Name { get; set; }
        public List<(string Type, string Name, int? ArraySize)> Fields { get; set; }
    }

    public class UniformBlockStructure : IEquatable<UniformBlockStructure>
    {
        public UniformBlockType UniformBlockType { get; set; } = UniformBlockType.Ordinary;
        public string CSharpTypeName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? Binding { get; set; } = null;
        public string? InstanceName { get; set; } = null;
        public List<(string Type, string Name, int? ArraySize)> Fields { get; set; } = new List<(string Type, string Name, int? ArraySize)>();

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals(obj as UniformBlockStructure);
        }
        public bool Equals(UniformBlockStructure? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            bool fieldsEqual = Fields.Count == other.Fields.Count &&
                              !Fields.Except(other.Fields).Any();
            return UniformBlockType == other.UniformBlockType &&
                   //CSharpTypeName == other.CSharpTypeName &&
                   Name == other.Name &&
                   Binding == other.Binding &&
                   InstanceName == other.InstanceName &&
                   fieldsEqual;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + UniformBlockType.GetHashCode();
                //hash = hash * 23 + (CSharpTypeName?.GetHashCode() ?? 0);
                hash = hash * 23 + (Name?.GetHashCode() ?? 0);
                hash = hash * 23 + (Binding?.GetHashCode() ?? 0);
                hash = hash * 23 + (InstanceName?.GetHashCode() ?? 0);
                foreach (var field in Fields)
                {
                    hash = hash * 23 + (field.Type?.GetHashCode() ?? 0);
                    hash = hash * 23 + (field.Name?.GetHashCode() ?? 0);
                    hash = hash * 23 + (field.ArraySize?.GetHashCode() ?? 0);
                }

                return hash;
            }
        }
        public static bool operator ==(UniformBlockStructure? left, UniformBlockStructure? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }
        public static bool operator !=(UniformBlockStructure? left, UniformBlockStructure? right) =>
            !(left == right);
    }

    public enum UniformBlockType
    {
        Ordinary,
        STD140,     // layout(std140)
        STD430,     // layout(std430) 
        Packed,     // layout(packed)
        Shared,     // layout(shared)
    }
}