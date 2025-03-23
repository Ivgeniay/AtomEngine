using System.Text.RegularExpressions;
using System.Text;

namespace OpenglLib
{
    public static class GlslParser
    {
        private static readonly IReadOnlyList<char> AllowedCharacters = new[] {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
            'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
            'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '_' };


        public static bool IsCompleteShaderFile(string source)
        {
            return source.Contains("#vertex") && source.Contains("#fragment");
        }

        public static string ProcessIncludesRecursively(string source, string sourcePath, HashSet<string> processedPaths = null)
        {
            processedPaths ??= new HashSet<string>();

            if (processedPaths.Contains(sourcePath))
                throw new CircularDependencyError($"Циклическая зависимость обнаружена: {sourcePath}");

            processedPaths.Add(sourcePath);

            var sourceDir = Path.GetDirectoryName(sourcePath);
            var includeRegex = new Regex(@"#include\s+""([^""]+)""");

            return includeRegex.Replace(source, match => {
                var includePath = match.Groups[1].Value;
                var fullPath = Path.GetFullPath(Path.Combine(sourceDir, includePath));

                try
                {
                    if (!File.Exists(fullPath))
                        throw new FileNotFoundException($"Включаемый файл не найден: {includePath}");

                    var includeContent = File.ReadAllText(fullPath);
                    return ProcessIncludesRecursively(includeContent, fullPath, new HashSet<string>(processedPaths));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Ошибка при обработке включения '{includePath}': {ex.Message}", ex);
                }
            });
        }
        public static string CleanComments(string source)
        {
            string noSingleLineComments = Regex.Replace(source, @"//.*$", "", RegexOptions.Multiline);
            return Regex.Replace(noSingleLineComments, @"/\*[\s\S]*?\*/", "");
        }


        public static (string vertex, string fragment) ExtractShaderSources(string source, string falepath = null)
        {
            if (falepath != null) source = ProcessIncludesRecursively(source, falepath);
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

        private static string ProcessIncludes(string source, Dictionary<string, string> includedFiles)
        {
            var includeRegex = new Regex(@"#include\s+""([^""]+)""");

            return includeRegex.Replace(source, match =>
            {
                var includePath = match.Groups[1].Value;

                if (includedFiles.TryGetValue(includePath, out string content))
                {
                    return content;
                }

                throw new Exception($"Include file not found: {includePath}");
            });
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
            var blockRegex = new Regex(@"layout\s*\(std140(?:\s*,\s*binding\s*=\s*(\d+))?\)\s*uniform\s+(\w+)?\s*\{([^}]+)\}\s*(\w+)?;", RegexOptions.Multiline);

            foreach (Match match in blockRegex.Matches(source))
            {
                var bindingStr = match.Groups[1].Success ? match.Groups[1].Value : null;
                int? binding = bindingStr != null ? int.Parse(bindingStr) : null;

                var blockName = match.Groups[2].Success ? match.Groups[2].Value : null;

                var instanceName = match.Groups[4].Success ? match.Groups[4].Value : null;
                var fieldsText = match.Groups[3].Value;

                var name = blockName ?? instanceName;
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                blocks.Add(new UniformBlockStructure
                {
                    Name = name,
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

        public static string ConvertToValidCSharpIdentifier(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "_";

            var result = new StringBuilder();
            if (char.IsDigit(input[0]))
                result.Append('_');

            foreach (char c in input)
            {
                if (AllowedCharacters.Contains(c))
                {
                    result.Append(c);
                }
                else
                {
                    result.Append('_');
                }
            }

            return result.ToString();
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
    }

    public class GlslStructure
    {
        public string Name { get; set; }
        public List<(string Type, string Name, int? ArraySize)> Fields { get; set; }
    }

    public class UniformBlockStructure
    {
        public string Name { get; set; } = string.Empty;
        public int? Binding { get; set; } = null;
        public string? InstanceName { get; set; } = null;
        public List<(string Type, string Name, int? ArraySize)> Fields { get; set; } = new List<(string Type, string Name, int? ArraySize)>();
    }
}