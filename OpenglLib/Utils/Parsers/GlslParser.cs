using AtomEngine;
using System.Text;
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

        public static List<UniformField> ExtractUniforms(string source)
        {
            List<UniformField> uniforms = new List<UniformField>();
            var processedUniformNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var uniformRegex = new Regex(@"uniform\s+(?!layout)(?:highp|mediump|lowp)?\s*(\w+)\s+(\w+)(?:\[(\w+|\d+)\])?\s*;");

            foreach (Match match in uniformRegex.Matches(source))
            {
                var type = match.Groups[1].Value;
                var name = match.Groups[2].Value;

                if (processedUniformNames.Contains(name))
                {
                    continue;
                }

                int? arraySize = null;

                if (match.Groups[3].Success)
                {
                    var sizeValue = match.Groups[3].Value;
                    if (int.TryParse(sizeValue, out int size))
                    {
                        arraySize = size;
                    }
                    else
                    {
                        arraySize = ResolveArraySizeIdentifier(source, sizeValue);
                    }
                }
                var uniformField = new UniformField
                {
                    Type = type,
                    CSharpType = MapGlslTypeToCSharp(type),
                    Name = name,
                    FullText = match.Value,
                    ArraySize = arraySize
                };

                uniformField.Attributes = ExtractAttributesAbove(source, match.Index);
                uniforms.Add(uniformField);
            }
            return uniforms;
        }

        public static List<ShaderAttribute> ExtractAttributesAbove(string source, int uniformPosition)
        {
            List<ShaderAttribute> attributes = new List<ShaderAttribute>();

            int lineStartPosition = source.LastIndexOf('\n', uniformPosition);
            if (lineStartPosition == -1)
                lineStartPosition = 0;
            else
                lineStartPosition++;

            var attributeRegex = new Regex(@"\[(\w+):([^\]]*)\]");

            int currentLineEnd = lineStartPosition;
            while (currentLineEnd > 0)
            {
                int prevLineStart = source.LastIndexOf('\n', currentLineEnd - 2);
                if (prevLineStart == -1)
                    prevLineStart = 0;
                else
                    prevLineStart++; 

                string line = source.Substring(prevLineStart, currentLineEnd - prevLineStart).Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                {
                    currentLineEnd = prevLineStart;
                    continue;
                }
                bool hasOnlyAttributes = true;
                string remainingLine = line;

                MatchCollection matches = attributeRegex.Matches(line);
                if (matches.Count == 0)
                {
                    break;
                }
                foreach (Match match in matches)
                {
                    remainingLine = remainingLine.Replace(match.Value, "");
                    attributes.Add(new ShaderAttribute
                    {
                        Name = match.Groups[1].Value.ToLower(),
                        Value = match.Groups[2].Value.Trim().ToLower()
                    });
                }

                if (!string.IsNullOrWhiteSpace(remainingLine))
                {
                    break;
                }
                currentLineEnd = prevLineStart;
            }
            return attributes.Reverse<ShaderAttribute>().ToList();
        }

        public static string RemoveAllAttributes(string source)
        {
            return Regex.Replace(source, @"\[\w+:[^\]]*\]", string.Empty);
        }

        public static List<GlslStructField> ParseStructFields(string fieldsText)
        {
            List<GlslStructField> fields = new List<GlslStructField>();

            fieldsText = Regex.Replace(fieldsText, @"//.*$", "", RegexOptions.Multiline);
            fieldsText = Regex.Replace(fieldsText, @"/\*[\s\S]*?\*/", "");
            var fieldRegex = new Regex(@"(?<type>\w+)\s+(?<name>\w+)(?:\[(?<size>\w+|\d+)\])?\s*;", RegexOptions.Multiline);

            foreach (Match match in fieldRegex.Matches(fieldsText))
            {
                var type = match.Groups["type"].Value;
                var name = match.Groups["name"].Value;
                int? arraySize = null;

                if (match.Groups["size"].Success)
                {
                    var sizeValue = match.Groups["size"].Value;
                    if (int.TryParse(sizeValue, out int size))
                    {
                        arraySize = size;
                    }
                    else
                    {
                        arraySize = ResolveArraySizeIdentifier(fieldsText, sizeValue);
                    }
                }
                var field = new GlslStructField
                {
                    Type = type,
                    Name = name,
                    ArraySize = arraySize
                };
                fields.Add(field);
            }

            return fields;
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

                var uniblock = new UniformBlockStructure
                {
                    Name = name,
                    UniformBlockType = blockType,
                    Binding = binding,
                    Fields = ParseFields(fieldsText, source),
                    InstanceName = instanceName
                };
                uniblock.Attributes = ExtractAttributesAbove(source, match.Index);
                blocks.Add(uniblock);
            }

            return blocks;
        }

        public static List<UniformBlockField> ParseFields(string fieldsText, string fullSource)
        {
            List<UniformBlockField> fields = new List<UniformBlockField>();

            fieldsText = Regex.Replace(fieldsText, @"//.*$", "", RegexOptions.Multiline);
            fieldsText = Regex.Replace(fieldsText, @"/\*[\s\S]*?\*/", "");
            var fieldRegex = new Regex(@"(?<type>\w+)\s+(?<name>\w+)(?:\[(?<size>\w+|\d+)\])?\s*;", RegexOptions.Multiline);

            foreach (Match match in fieldRegex.Matches(fieldsText))
            {
                var type = match.Groups["type"].Value;
                var name = match.Groups["name"].Value;
                int? arraySize = null;

                if (match.Groups["size"].Success)
                {
                    var sizeValue = match.Groups["size"].Value;
                    if (int.TryParse(sizeValue, out int size))
                    {
                        arraySize = size;
                    }
                    else
                    {
                        arraySize = ResolveArraySizeIdentifier(fullSource, sizeValue);
                    }
                }
                UniformBlockField field = new UniformBlockField()
                {
                    Type = type,
                    Name = name,
                    ArraySize = arraySize,
                };
                fields.Add(field);
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
                "bool"                   => "bool",
                "int"                    => "int",
                "uint"                   => "uint",
                "float"                  => "float",
                "double"                 => "double",

                "bvec2"                  => "Vector2D<bool>",
                "bvec3"                  => "Vector3D<bool>",
                "bvec4"                  => "Vector4D<bool>",

                "ivec2"                  => "Vector2D<int>",
                "ivec3"                  => "Vector3D<int>",
                "ivec4"                  => "Vector4D<int>",

                "uvec2"                  => "Vector2D<uint>",
                "uvec3"                  => "Vector3D<uint>",
                "uvec4"                  => "Vector4D<uint>",

                "vec2"                   => "Vector2D<float>",
                "vec3"                   => "Vector3D<float>",
                "vec4"                   => "Vector4D<float>",

                "mat2"                   => "Matrix2X2<float>",
                "mat3"                   => "Matrix3X3<float>",
                "mat4"                   => "Matrix4X4<float>",
                "mat2x2"                 => "Matrix2X2<float>",
                "mat2x3"                 => "Matrix2X3<float>",
                "mat2x4"                 => "Matrix2X4<float>",
                "mat3x2"                 => "Matrix3X2<float>",
                "mat3x3"                 => "Matrix3X3<float>",
                "mat3x4"                 => "Matrix3X4<float>",
                "mat4x2"                 => "Matrix4X2<float>",
                "mat4x3"                 => "Matrix4X3<float>",
                "mat4x4"                 => "Matrix4X4<float>",

                "sampler1D"              => "int",
                "sampler2D"              => "int",
                "sampler3D"              => "int",
                "samplerCube"            => "int",
                "sampler2DRect"          => "int",
                "sampler1DArray"         => "int",
                "sampler2DArray"         => "int",
                "samplerCubeArray"       => "int",
                "samplerBuffer"          => "int",
                "sampler2DMS"            => "int",
                "sampler2DMSArray"       => "int",

                "sampler1DShadow"        => "int",
                "sampler2DShadow"        => "int",
                "samplerCubeShadow"      => "int",
                "sampler2DRectShadow"    => "int",
                "sampler1DArrayShadow"   => "int",
                "sampler2DArrayShadow"   => "int",
                "samplerCubeArrayShadow" => "int",

                "isampler1D"             => "int",
                "isampler2D"             => "int",
                "isampler3D"             => "int",
                "isamplerCube"           => "int",
                "isampler2DRect"         => "int",
                "isampler1DArray"        => "int",
                "isampler2DArray"        => "int",
                "isamplerCubeArray"      => "int",
                "isamplerBuffer"         => "int",
                "isampler2DMS"           => "int",
                "isampler2DMSArray"      => "int",

                "usampler1D"             => "int",
                "usampler2D"             => "int",
                "usampler3D"             => "int",
                "usamplerCube"           => "int",
                "usampler2DRect"         => "int",
                "usampler1DArray"        => "int",
                "usampler2DArray"        => "int",
                "usamplerCubeArray"      => "int",
                "usamplerBuffer"         => "int",
                "usampler2DMS"           => "int",
                "usampler2DMSArray"      => "int",

                "atomic_uint"            => "uint",
                "image1D"                => "int",
                "image2D"                => "int",
                "image3D"                => "int",
                "image2DRect"            => "int",
                "imageCube"              => "int",
                "image1DArray"           => "int",
                "image2DArray"           => "int",
                "imageBuffer"            => "int",
                "image2DMS"              => "int",
                "image2DMSArray"         => "int",

                "void"                   => "void",
                "struct"                 => "struct",
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
            csharpType == type && type != "float" && type != "bool" && type != "int" && type != "uint" && type != "double" &&
            type != "Vector2D<bool>" && type != "Vector3D<bool>" && type != "Vector4D<bool>" && type != "Vector2D<int>" &&
            type != "Vector3D<int>" && type != "Vector4D<int>" && type != "Vector2D<uint>" && type != "Vector3D<uint>" &&
            type != "Vector4D<uint>" && type != "Vector2D<float>" && type != "Vector3D<float>" && type != "Vector4D<float>" &&
            type != "Matrix2X2<float>" && type != "Matrix3X3<float>" && type != "Matrix4X4<float>" && type != "Matrix2X2<float>" &&
            type != "Matrix2X3<float>" && type != "Matrix2X4<float>" && type != "Matrix3X2<float>" && type != "Matrix3X3<float>" &&
            type != "Matrix3X4<float>" && type != "Matrix4X2<float>" && type != "Matrix4X3<float>" && type != "Matrix4X4<float>";



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

        public static int? ResolveArraySizeIdentifier(string sourceCode, string identifier)
        {
            var defineRegex = new Regex($@"#define\s+{Regex.Escape(identifier)}\s+(\d+)");
            var defineMatch = defineRegex.Match(sourceCode);
            if (defineMatch.Success && defineMatch.Groups.Count > 1)
            {
                if (int.TryParse(defineMatch.Groups[1].Value, out int size))
                {
                    return size;
                }
            }

            var constRegex = new Regex($@"const\s+(int|uint)\s+{Regex.Escape(identifier)}\s*=\s*(\d+)\s*;");
            var constMatch = constRegex.Match(sourceCode);
            if (constMatch.Success && constMatch.Groups.Count > 2)
            {
                if (int.TryParse(constMatch.Groups[2].Value, out int size))
                {
                    return size;
                }
            }

            var constRefRegex = new Regex($@"const\s+(int|uint)\s+{Regex.Escape(identifier)}\s*=\s*(\w+)\s*;");
            var constRefMatch = constRefRegex.Match(sourceCode);
            if (constRefMatch.Success && constRefMatch.Groups.Count > 2)
            {
                var refIdentifier = constRefMatch.Groups[2].Value;
                return ResolveArraySizeIdentifier(sourceCode, refIdentifier);
            }

            DebLogger.Warn($"Unable to resolve array size identifier: {identifier}");
            return null;
        }


        // Uniforms
        public static string ResolveUniformPlacement(string sourceShader, List<UniformField> uniforms)
        {
            int vertexSectionIndex = sourceShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
            int fragmentSectionIndex = sourceShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

            if (vertexSectionIndex == -1 || fragmentSectionIndex == -1)
            {
                throw new FormatException("Shader source must contain both #vertex and #fragment sections");
            }

            string resultShader = new string(sourceShader.ToCharArray());

            List<string> uniformsToAddToVertex = new List<string>();
            List<string> uniformsToAddToFragment = new List<string>();
            List<string> uniformsToRemove = new List<string>();

            foreach (var uniform in uniforms)
            {
                var uniformTargetAttr = uniform.Attributes
                    .FirstOrDefault(a => a.Name.Equals("uniformtarget", StringComparison.OrdinalIgnoreCase));

                if (uniformTargetAttr == null)
                {
                    continue;
                }

                string uniformText = uniform.FullText;
                if (string.IsNullOrEmpty(uniformText))
                {
                    uniformText = FormatUniform(uniform);
                }

                int uniformPosition = resultShader.IndexOf(uniformText, StringComparison.OrdinalIgnoreCase);

                if (uniformPosition == -1)
                {
                    string uniformPattern = $"uniform\\s+{uniform.Type}\\s+{uniform.Name}";
                    if (uniform.ArraySize.HasValue)
                    {
                        uniformPattern += $"\\s*\\[\\s*{uniform.ArraySize}\\s*\\]";
                    }
                    uniformPattern += "\\s*;";

                    var regex = new Regex(uniformPattern, RegexOptions.Singleline);
                    var match = regex.Match(resultShader);

                    if (match.Success)
                    {
                        uniformText = match.Value;
                        uniformPosition = match.Index;
                    }
                    else
                    {
                        continue;
                    }
                }

                bool isInVertexSection = uniformPosition > vertexSectionIndex && uniformPosition < fragmentSectionIndex;
                bool isInFragmentSection = uniformPosition > fragmentSectionIndex;

                bool shouldBeInVertex = uniformTargetAttr.Value.Equals("vertex", StringComparison.OrdinalIgnoreCase) ||
                                       uniformTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

                bool shouldBeInFragment = uniformTargetAttr.Value.Equals("fragment", StringComparison.OrdinalIgnoreCase) ||
                                         uniformTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

                if (shouldBeInVertex && !isInVertexSection)
                {
                    uniformsToAddToVertex.Add(uniformText);
                }

                if (shouldBeInFragment && !isInFragmentSection)
                {
                    uniformsToAddToFragment.Add(uniformText);
                }

                if ((isInVertexSection && !shouldBeInVertex) ||
                    (isInFragmentSection && !shouldBeInFragment))
                {
                    uniformsToRemove.Add(uniformText);
                }
            }

            foreach (var uniformText in uniformsToRemove)
            {
                resultShader = resultShader.Replace(uniformText, "");
            }

            vertexSectionIndex = resultShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
            fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

            int vertexMainIndex = resultShader.IndexOf("void main()", vertexSectionIndex,
                fragmentSectionIndex - vertexSectionIndex, StringComparison.OrdinalIgnoreCase);

            int fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
                StringComparison.OrdinalIgnoreCase);

            if (vertexMainIndex == -1 || fragmentMainIndex == -1)
            {
                throw new FormatException("Both vertex and fragment sections must contain a main function");
            }

            if (uniformsToAddToVertex.Count > 0)
            {
                string vertexUniformsToAdd = string.Join("\n", uniformsToAddToVertex);
                resultShader = resultShader.Insert(vertexMainIndex, vertexUniformsToAdd + "\n\n");
            }

            fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);
            fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
                StringComparison.OrdinalIgnoreCase);

            if (uniformsToAddToFragment.Count > 0)
            {
                string fragmentUniformsToAdd = string.Join("\n", uniformsToAddToFragment);
                resultShader = resultShader.Insert(fragmentMainIndex, fragmentUniformsToAdd + "\n\n");
            }

            return resultShader;
        }

        private static string FormatUniform(UniformField uniform)
        {
            string result = $"uniform {uniform.Type} {uniform.Name}";
            if (uniform.ArraySize.HasValue)
            {
                result += $"[{uniform.ArraySize}]";
            }
            result += ";";
            return result;
        }


        // Structs
        public static List<GlslStructure> ParseGlslStructures(string sourceCode)
        {
            var structures = new List<GlslStructure>();
            var processedStructNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var structRegex = new Regex(@"struct\s+(\w+)\s*\{([^}]+)\}\s*(?:\w+(?:\s*,\s*\w+)*\s*)?;?", RegexOptions.Multiline);
            var matches = structRegex.Matches(sourceCode);

            foreach (Match match in matches)
            {
                string structName = match.Groups[1].Value;

                if (processedStructNames.Contains(structName))
                {
                    continue;
                }

                var structure = new GlslStructure
                {
                    Name = structName,
                    Fields = ParseStructFields(match.Groups[2].Value),
                    FullText = match.Value,
                    Position = match.Index
                };

                structure.Attributes = ExtractAttributesAbove(sourceCode, match.Index);

                structures.Add(structure);
                processedStructNames.Add(structName);
            }

            return structures;
        }



        //public static List<GlslStructure> ParseGlslStructures(string sourceCode)
        //{
        //    var structures = new List<GlslStructure>();
        //    var processedStructNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        //    var structRegex = new Regex(@"struct\s+(\w+)\s*\{([^}]+)\}", RegexOptions.Multiline);
        //    var matches = structRegex.Matches(sourceCode);

        //    foreach (Match match in matches)
        //    {
        //        string structName = match.Groups[1].Value;
        //        if (processedStructNames.Contains(structName))
        //        {
        //            continue;
        //        }

        //        var structure = new GlslStructure
        //        {
        //            Name = structName,
        //            Fields = ParseStructFields(match.Groups[2].Value),
        //            FullText = match.Value,
        //            Position = match.Index
        //        };

        //        structure.Attributes = ExtractAttributesAbove(sourceCode, match.Index);

        //        structures.Add(structure);
        //        processedStructNames.Add(structName);
        //    }

        //    return structures;
        //}

        //public static List<GlslStructure> ParseGlslStructures(string sourceCode)
        //{
        //    var structures = new List<GlslStructure>();
        //    var structRegex = new Regex(@"struct\s+(\w+)\s*\{([^}]+)\}", RegexOptions.Multiline);
        //    var matches = structRegex.Matches(sourceCode);

        //    foreach (Match match in matches)
        //    {
        //        var structure = new GlslStructure
        //        {
        //            Name = match.Groups[1].Value,
        //            Fields = ParseStructFields(match.Groups[2].Value),
        //            Attributes = ExtractAttributesAbove(sourceCode, match.Index),
        //        };

        //        structure.Attributes = ExtractAttributesAbove(sourceCode, match.Index);
        //        structures.Add(structure);
        //    }

        //    return structures;
        //}

        public static string ResolveStructurePlacement(string sourceShader, List<GlslStructure> structures)
        {
            int vertexSectionIndex = sourceShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
            int fragmentSectionIndex = sourceShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

            if (vertexSectionIndex == -1 || fragmentSectionIndex == -1)
            {
                throw new FormatException("Shader source must contain both #vertex and #fragment sections");
            }

            string resultShader = new string(sourceShader.ToCharArray());

            List<string> structsToAddToVertex = new List<string>();
            List<string> structsToAddToFragment = new List<string>();
            List<string> structsToRemove = new List<string>();

            foreach (var structure in structures)
            {
                var structTargetAttr = structure.Attributes
                    .FirstOrDefault(a => a.Name.Equals("structtarget", StringComparison.OrdinalIgnoreCase));

                if (structTargetAttr == null)
                {
                    continue;
                }

                string structText = structure.FullText;
                if (string.IsNullOrEmpty(structText))
                {
                    structText = FormatStructure(structure);
                }

                int structPosition = resultShader.IndexOf(structText, StringComparison.OrdinalIgnoreCase);

                if (structPosition == -1)
                {
                    string structPattern = $"struct\\s+{structure.Name}\\s*\\{{[^}}]+\\}}\\s*;?";
                    var regex = new Regex(structPattern, RegexOptions.Singleline);
                    var match = regex.Match(resultShader);

                    if (match.Success)
                    {
                        structText = match.Value;
                        if (!structText.TrimEnd().EndsWith(";"))
                        {
                            structText = structText.TrimEnd() + ";";
                        }

                        structPosition = match.Index;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    if (!structText.TrimEnd().EndsWith(";"))
                    {
                        structText = structText.TrimEnd() + ";";
                    }
                }

                bool isInVertexSection = structPosition > vertexSectionIndex && structPosition < fragmentSectionIndex;
                bool isInFragmentSection = structPosition > fragmentSectionIndex;

                bool shouldBeInVertex = structTargetAttr.Value.Equals("vertex", StringComparison.OrdinalIgnoreCase) ||
                                       structTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

                bool shouldBeInFragment = structTargetAttr.Value.Equals("fragment", StringComparison.OrdinalIgnoreCase) ||
                                         structTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

                if (shouldBeInVertex && !isInVertexSection)
                {
                    structsToAddToVertex.Add(structText);
                }

                if (shouldBeInFragment && !isInFragmentSection)
                {
                    structsToAddToFragment.Add(structText);
                }

                if ((isInVertexSection && !shouldBeInVertex) ||
                    (isInFragmentSection && !shouldBeInFragment))
                {
                    structsToRemove.Add(structText);
                }
            }

            foreach (var structText in structsToRemove)
            {
                resultShader = resultShader.Replace(structText, "");
            }

            vertexSectionIndex = resultShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
            fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

            int vertexMainIndex = resultShader.IndexOf("void main()", vertexSectionIndex,
                fragmentSectionIndex - vertexSectionIndex, StringComparison.OrdinalIgnoreCase);

            int fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
                StringComparison.OrdinalIgnoreCase);

            if (vertexMainIndex == -1 || fragmentMainIndex == -1)
            {
                throw new FormatException("Both vertex and fragment sections must contain a main function");
            }

            if (structsToAddToVertex.Count > 0)
            {
                string vertexStructsToAdd = string.Join("\n\n", structsToAddToVertex);
                resultShader = resultShader.Insert(vertexMainIndex, vertexStructsToAdd + "\n\n");
            }

            fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);
            fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
                StringComparison.OrdinalIgnoreCase);

            if (structsToAddToFragment.Count > 0)
            {
                string fragmentStructsToAdd = string.Join("\n\n", structsToAddToFragment);
                resultShader = resultShader.Insert(fragmentMainIndex, fragmentStructsToAdd + "\n\n");
            }

            return resultShader;
        }

        private static string FormatStructure(GlslStructure structure)
        {
            var sb = new StringBuilder();
            sb.Append($"struct {structure.Name} {{");

            foreach (var field in structure.Fields)
            {
                sb.Append($"\n    {field.Type} {field.Name};");
            }

            sb.Append("\n};");
            return sb.ToString();
        }

        // Methods
        public static string ResolveMethodPlacement(string sourceShader, List<GlslMethodInfo> methods)
        {
            int vertexSectionIndex = sourceShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
            int fragmentSectionIndex = sourceShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

            if (vertexSectionIndex == -1 || fragmentSectionIndex == -1)
            {
                throw new FormatException("Shader source must contain both #vertex and #fragment sections");
            }

            string resultShader = sourceShader;
            List<string> methodsToAddToVertex = new List<string>();
            List<string> methodsToAddToFragment = new List<string>();
            List<string> methodsToRemove = new List<string>();

            foreach (var method in methods)
            {
                var methodTargetAttr = method.Attributes
                    .FirstOrDefault(a => a.Name.Equals("methodtarget", StringComparison.OrdinalIgnoreCase));

                if (methodTargetAttr == null)
                {
                    continue;
                }

                // Определяем текущее положение метода в шейдере
                int methodPosition = resultShader.IndexOf(method.FullMethodText, StringComparison.OrdinalIgnoreCase);
                if (methodPosition == -1)
                {
                    continue;
                }

                bool isInVertexSection = methodPosition > vertexSectionIndex && methodPosition < fragmentSectionIndex;
                bool isInFragmentSection = methodPosition > fragmentSectionIndex;

                bool shouldBeInVertex = methodTargetAttr.Value.Equals("vertex", StringComparison.OrdinalIgnoreCase) ||
                                      methodTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

                bool shouldBeInFragment = methodTargetAttr.Value.Equals("fragment", StringComparison.OrdinalIgnoreCase) ||
                                        methodTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

                if (shouldBeInVertex && !isInVertexSection)
                {
                    methodsToAddToVertex.Add(method.FullMethodText);
                }

                if (shouldBeInFragment && !isInFragmentSection)
                {
                    methodsToAddToFragment.Add(method.FullMethodText);
                }

                if ((isInVertexSection && !shouldBeInVertex) ||
                    (isInFragmentSection && !shouldBeInFragment))
                {
                    methodsToRemove.Add(method.FullMethodText);
                }
            }

            foreach (var methodText in methodsToRemove)
            {
                resultShader = resultShader.Replace(methodText, "");
            }

            vertexSectionIndex = resultShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
            fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

            int vertexMainIndex = resultShader.IndexOf("void main()", vertexSectionIndex,
                fragmentSectionIndex - vertexSectionIndex, StringComparison.OrdinalIgnoreCase);

            int fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
                StringComparison.OrdinalIgnoreCase);

            if (vertexMainIndex == -1 || fragmentMainIndex == -1)
            {
                throw new FormatException("Both vertex and fragment sections must contain a main function");
            }

            if (methodsToAddToVertex.Count > 0)
            {
                string vertexMethodsToAdd = string.Join("\n\n", methodsToAddToVertex);
                resultShader = resultShader.Insert(vertexMainIndex, vertexMethodsToAdd + "\n\n");
            }

            fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);
            fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
                StringComparison.OrdinalIgnoreCase);

            if (methodsToAddToFragment.Count > 0)
            {
                string fragmentMethodsToAdd = string.Join("\n\n", methodsToAddToFragment);
                resultShader = resultShader.Insert(fragmentMainIndex, fragmentMethodsToAdd + "\n\n");
            }

            return resultShader;
        }

        public static List<GlslMethodInfo> ExtractMethods(string sourceCode)
        {
            var methods = new List<GlslMethodInfo>();

            string cleanedCode = RemoveComments(sourceCode);

            Dictionary<int, int> bracketPairs = MatchBrackets(cleanedCode);

            var methodHeaderPattern = @"\b(?:void|float|int|vec\d|dvec\d|ivec\d|uvec\d|mat\d|dmat\d|bool|[a-zA-Z_][a-zA-Z0-9_]*)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\([^;]*\)\s*\{";
            var methodHeaderMatches = Regex.Matches(cleanedCode, methodHeaderPattern);

            foreach (Match headerMatch in methodHeaderMatches)
            {
                int openBracePos = cleanedCode.IndexOf('{', headerMatch.Index + headerMatch.Length - 1);
                if (openBracePos < 0) continue;

                if (!bracketPairs.TryGetValue(openBracePos, out int closeBracePos))
                    continue;

                int methodStartPos = headerMatch.Index;
                string fullMethodText = cleanedCode.Substring(methodStartPos, closeBracePos - methodStartPos + 1);

                string header = fullMethodText.Substring(0, fullMethodText.IndexOf('{'));
                string body = fullMethodText.Substring(fullMethodText.IndexOf('{'));

                var methodInfo = ParseMethodHeader(header);
                if (methodInfo == null) continue;

                methodInfo.FullMethodText = fullMethodText;
                methodInfo.Content = body.Substring(1, body.Length - 2).Trim();

                methodInfo.Attributes = GlslParser.ExtractAttributesAbove(sourceCode, methodStartPos);

                methods.Add(methodInfo);
            }

            return methods;
        }

        private static string RemoveComments(string code)
        {
            code = Regex.Replace(code, @"/\*[\s\S]*?\*/", "");
            code = Regex.Replace(code, @"//.*$", "", RegexOptions.Multiline);

            return code;
        }

        private static Dictionary<int, int> MatchBrackets(string code)
        {
            var result = new Dictionary<int, int>();
            var stack = new Stack<int>();

            for (int i = 0; i < code.Length; i++)
            {
                if (code[i] == '{')
                {
                    stack.Push(i);
                }
                else if (code[i] == '}' && stack.Count > 0)
                {
                    int openPos = stack.Pop();
                    result[openPos] = i;
                }
            }

            return result;
        }

        private static GlslMethodInfo ParseMethodHeader(string header)
        {
            header = header.Trim();

            int paramStartPos = header.IndexOf('(');
            if (paramStartPos < 0) return null;

            int paramEndPos = header.LastIndexOf(')');
            if (paramEndPos < paramStartPos) return null;

            string beforeParams = header.Substring(0, paramStartPos).Trim();

            var parts = beforeParams.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return null;

            string methodName = parts[parts.Length - 1];

            StringBuilder returnType = new StringBuilder();
            for (int i = 0; i < parts.Length - 1; i++)
            {
                returnType.Append(parts[i]);
                if (i < parts.Length - 2) returnType.Append(" ");
            }
            string paramsText = header.Substring(paramStartPos + 1, paramEndPos - paramStartPos - 1).Trim();
            var parameters = ParseParameters(paramsText);

            var methodInfo = new GlslMethodInfo
            {
                Name = methodName,
                ReturnType = returnType.ToString(),
                Params = parameters,
                Attributes = new List<ShaderAttribute>()
            };

            return methodInfo;
        }

        private static List<RsMethodParamInfo> ParseParameters(string paramsText)
        {
            var parameters = new List<RsMethodParamInfo>();

            if (string.IsNullOrWhiteSpace(paramsText))
                return parameters;

            var paramList = SplitByComma(paramsText);

            foreach (var param in paramList)
            {
                var trimmedParam = param.Trim();
                if (string.IsNullOrEmpty(trimmedParam)) continue;
                var parts = trimmedParam.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                string paramName = parts[parts.Length - 1];
                if (paramName.Contains("["))
                {
                    paramName = paramName.Substring(0, paramName.IndexOf('['));
                }

                StringBuilder paramType = new StringBuilder();
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    paramType.Append(parts[i]);
                    if (i < parts.Length - 2) paramType.Append(" ");
                }

                parameters.Add(new RsMethodParamInfo
                {
                    Type = paramType.ToString(),
                    Name = paramName
                });
            }

            return parameters;
        }

        private static List<string> SplitByComma(string text)
        {
            var result = new List<string>();
            int start = 0;
            int depth = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '(' || text[i] == '[' || text[i] == '<')
                {
                    depth++;
                }
                else if (text[i] == ')' || text[i] == ']' || text[i] == '>')
                {
                    depth--;
                }
                else if (text[i] == ',' && depth == 0)
                {
                    result.Add(text.Substring(start, i - start));
                    start = i + 1;
                }
            }

            if (start < text.Length)
            {
                result.Add(text.Substring(start));
            }

            return result;
        }

    }

    public class GlslStructure
    {
        public string Name { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
        public int Position { get; set; }
        public List<GlslStructField> Fields { get; set; } = new List<GlslStructField>();
        public List<ShaderAttribute> Attributes { get; set; } = new List<ShaderAttribute>();
    }

    public class GlslStructField
    {
        public List<ShaderAttribute> Attributes { get; set; } = new List<ShaderAttribute>();
        public string Type { get; set; } = string.Empty;
        public string Name {  set; get; } = string.Empty;
        public int? ArraySize { get; set; }
    }

    public class UniformField
    {
        public List<ShaderAttribute> Attributes { get; set; } = new List<ShaderAttribute>();
        public string Type { get; set; } = string.Empty;
        public string CSharpType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
        public int? ArraySize { get; set; }
    }

    public class ShaderAttribute
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class GlslMethodInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public string FullMethodText { get; set; } = string.Empty;
        public List<RsMethodParamInfo> Params { get; set; } = new List<RsMethodParamInfo>();
        public List<ShaderAttribute> Attributes { get; set; } = new List<ShaderAttribute>();
    }

    public class RsMethodParamInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string[] Qualifiers { get; set; }
    }

    public class UniformBlockStructure
    {
        public List<ShaderAttribute> Attributes { get; set; } = new List<ShaderAttribute>();
        public UniformBlockType UniformBlockType { get; set; } = UniformBlockType.Ordinary;
        public string CSharpTypeName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? Binding { get; set; } = null;
        public string? InstanceName { get; set; } = null;
        public List<UniformBlockField> Fields { get; set; } = new List<UniformBlockField>();
    }

    public class UniformBlockField
    {
        public List<ShaderAttribute> Attributes { get; set; } = new List<ShaderAttribute>();
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? ArraySize { get; set; }
    }

    public enum UniformBlockType
    {
        Ordinary,
        STD140, 
        STD430, 
        Packed, 
        Shared, 
    }
}