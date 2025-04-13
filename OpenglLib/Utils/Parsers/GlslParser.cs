using System.Text.RegularExpressions;
using System.Text;
using AtomEngine;
using EngineLib;
using Silk.NET.OpenGL;
using Silk.NET.Maths;

namespace OpenglLib
{
    public static class GlslExtractor
    {
        public static GlslShaderModel ExtractShaderModel(string sourcePath)
        {
            GlslShaderModel shaderModel = new GlslShaderModel();
            string shaderSource = FileLoader.LoadFile(sourcePath);

            bool isComplete = GlslParser.IsCompleteShaderFile(shaderSource);

            List<RSFileInfo> rsFiles = RSParser.ProcessIncludes(shaderSource, sourcePath);

            shaderSource = GlslParser.ProcessIncludesRecursively(shaderSource, sourcePath);
            shaderSource = RSParser.RemoveServiceMarkers(shaderSource);
            shaderSource = GlslParser.RemoveAllAttributes(shaderSource);

            shaderSource = GlslPlacementResolver.ResolveUniformBlockPlacement(shaderSource, RSParser.GetUniformsBlocksFromRsFileInfos(rsFiles));
            shaderSource = GlslPlacementResolver.ResolveUniformPlacement(shaderSource, RSParser.GetUniformsFromRsFileInfos(rsFiles));
            shaderSource = GlslPlacementResolver.ResolveStructInstancePlacement(shaderSource, RSParser.GetStructuresInstanceFromFileInfos(rsFiles));
            shaderSource = GlslPlacementResolver.ResolveStructurePlacement(shaderSource, RSParser.GetStructuresFromFileInfos(rsFiles));
            shaderSource = GlslPlacementResolver.ResolveConstantPlacement(shaderSource, RSParser.GetConstFromFileInfos(rsFiles));
            shaderSource = GlslPlacementResolver.ResolveMethodPlacement(shaderSource, RSParser.GetMethodsFromRsFileInfos(rsFiles));

            var (sourceVertex, sourceFragment) = GlslParser.ExtractShaderSources(shaderSource);
            var (hasVertexMain, hasFragmentMain) = GlslParser.ValidateMainFunctions(sourceVertex, sourceFragment);

            shaderModel.RSFiles = rsFiles;

            shaderModel.Vertex = new VertexShaderModel
            {
                FullText = GlslParser.NormalizeLineBreaks(GlslParser.RemoveAllAttributes(sourceVertex)),
                VertexAttributes = GlslParser.ParseVertexAttributes(sourceVertex),
                VertexOutParams = GlslParser.ExtractVertexOutputs(sourceVertex),
                Version = GlslParser.ExtractGlslVersion(sourceVertex),
                Constants = GlslParser.ExtractGlslConstants(sourceVertex),
                Uniforms = GlslParser.ExtractUniforms(sourceVertex),
                Structures = GlslParser.ExtractGlslStructures(sourceVertex),
                UniformsBlocks = GlslParser.ExtractUniformBlocks(sourceVertex),
                Methods = GlslParser.ExtractMethods(sourceVertex)
            };
            shaderModel.Fragment = new FragmentShaderModel
            {
                FullText = GlslParser.NormalizeLineBreaks(GlslParser.RemoveAllAttributes(sourceFragment)),
                InnerParams = GlslParser.ExtractFragmentInputs(sourceFragment),
                Version = GlslParser.ExtractGlslVersion(sourceFragment),
                Constants = GlslParser.ExtractGlslConstants(sourceFragment),
                Uniforms = GlslParser.ExtractUniforms(sourceFragment),
                Structures = GlslParser.ExtractGlslStructures(sourceFragment),
                UniformsBlocks = GlslParser.ExtractUniformBlocks(sourceFragment),
                Methods = GlslParser.ExtractMethods(sourceFragment)
            };

            shaderModel.Vertex.StructureInstances = GlslParser.ExtractStructInstances(sourceVertex, shaderModel.Vertex.Structures);
            shaderModel.Fragment.StructureInstances = GlslParser.ExtractStructInstances(sourceFragment, shaderModel.Fragment.Structures);
            
            shaderModel.FullText = GlslParser.NormalizeLineBreaks(new string(shaderSource));

            PropagateAttributesFromRsFiles(shaderModel);

            return shaderModel;
        }


        public static void PropagateAttributesFromRsFiles(GlslShaderModel shaderModel)
        {
            if (shaderModel.RSFiles == null || shaderModel.RSFiles.Count == 0 ||
                (shaderModel.Vertex == null && shaderModel.Fragment == null))
                return;

            var rsConstantsDict = new Dictionary<string, List<GlslConstantModel>>();
            var rsUniformsDict = new Dictionary<string, List<UniformModel>>();
            var rsUniformBlocksDict = new Dictionary<string, List<UniformBlockModel>>();
            var rsStructuresDict = new Dictionary<string, List<GlslStructModel>>();
            var rsMethodsDict = new Dictionary<string, List<GlslMethodInfo>>();

            foreach (var rsFile in shaderModel.RSFiles)
            {
                foreach (var constant in rsFile.Constants)
                {
                    if (!rsConstantsDict.ContainsKey(constant.Name))
                        rsConstantsDict[constant.Name] = new List<GlslConstantModel>();
                    rsConstantsDict[constant.Name].Add(constant);
                }

                foreach (var uniform in rsFile.Uniforms)
                {
                    if (!rsUniformsDict.ContainsKey(uniform.Name))
                        rsUniformsDict[uniform.Name] = new List<UniformModel>();
                    rsUniformsDict[uniform.Name].Add(uniform);
                }

                foreach (var block in rsFile.UniformBlocks)
                {
                    var blockName = !string.IsNullOrEmpty(block.Name) ? block.Name :
                                 (!string.IsNullOrEmpty(block.InstanceName) ? block.InstanceName : "");

                    if (!string.IsNullOrEmpty(blockName))
                    {
                        if (!rsUniformBlocksDict.ContainsKey(blockName))
                            rsUniformBlocksDict[blockName] = new List<UniformBlockModel>();
                        rsUniformBlocksDict[blockName].Add(block);
                    }
                }

                foreach (var structure in rsFile.Structures)
                {
                    if (!rsStructuresDict.ContainsKey(structure.Name))
                        rsStructuresDict[structure.Name] = new List<GlslStructModel>();
                    rsStructuresDict[structure.Name].Add(structure);
                }

                foreach (var method in rsFile.Methods)
                {
                    if (!rsMethodsDict.ContainsKey(method.Name))
                        rsMethodsDict[method.Name] = new List<GlslMethodInfo>();
                    rsMethodsDict[method.Name].Add(method);
                }
            }

            if (shaderModel.Vertex != null)
            {
                PropagateAttributesToShaderModel(shaderModel.Vertex, rsConstantsDict, rsUniformsDict,
                    rsUniformBlocksDict, rsStructuresDict, rsMethodsDict);
            }
            if (shaderModel.Fragment != null)
            {
                PropagateAttributesToShaderModel(shaderModel.Fragment, rsConstantsDict, rsUniformsDict,
                    rsUniformBlocksDict, rsStructuresDict, rsMethodsDict);
            }
        }

        private static void PropagateAttributesToShaderModel(
            ShaderChankModel shaderModel,
            Dictionary<string, List<GlslConstantModel>> rsConstantsDict,
            Dictionary<string, List<UniformModel>> rsUniformsDict,
            Dictionary<string, List<UniformBlockModel>> rsUniformBlocksDict,
            Dictionary<string, List<GlslStructModel>> rsStructuresDict,
            Dictionary<string, List<GlslMethodInfo>> rsMethodsDict)
        {
            foreach (var constant in shaderModel.Constants)
            {
                if (rsConstantsDict.TryGetValue(constant.Name, out var rsConstants))
                {
                    foreach (var rsConstant in rsConstants)
                    {
                        MergeAttributes(constant.Attributes, rsConstant.Attributes);
                    }
                }
            }

            foreach (var uniform in shaderModel.Uniforms)
            {
                if (rsUniformsDict.TryGetValue(uniform.Name, out var rsUniforms))
                {
                    foreach (var rsUniform in rsUniforms)
                    {
                        MergeAttributes(uniform.Attributes, rsUniform.Attributes);
                    }
                }
            }

            foreach (var block in shaderModel.UniformsBlocks)
            {
                // Проверяем сначала по имени блока
                var blockName = !string.IsNullOrEmpty(block.Name) ? block.Name :
                             (!string.IsNullOrEmpty(block.InstanceName) ? block.InstanceName : "");

                if (!string.IsNullOrEmpty(blockName) && rsUniformBlocksDict.TryGetValue(blockName, out var rsBlocks))
                {
                    foreach (var rsBlock in rsBlocks)
                    {
                        MergeAttributes(block.Attributes, rsBlock.Attributes);

                        // Также обрабатываем поля блоков
                        foreach (var field in block.Fields)
                        {
                            var rsField = rsBlock.Fields.FirstOrDefault(f => f.Name == field.Name);
                            if (rsField != null && rsField.Attributes != null)
                            {
                                MergeAttributes(field.Attributes, rsField.Attributes);
                            }
                        }
                    }
                }
            }

            // Структуры
            foreach (var structure in shaderModel.Structures)
            {
                if (rsStructuresDict.TryGetValue(structure.Name, out var rsStructures))
                {
                    foreach (var rsStructure in rsStructures)
                    {
                        MergeAttributes(structure.Attributes, rsStructure.Attributes);

                        // Можно добавить обработку полей структур, если это необходимо
                        // Однако в текущей модели структуры это не предусмотрено
                    }
                }
            }

            // Методы
            foreach (var method in shaderModel.Methods)
            {
                if (rsMethodsDict.TryGetValue(method.Name, out var rsMethods))
                {
                    foreach (var rsMethod in rsMethods)
                    {
                        MergeAttributes(method.Attributes, rsMethod.Attributes);
                    }
                }
            }
        }

        private static void MergeAttributes(List<ShaderAttribute> targetAttributes, List<ShaderAttribute> sourceAttributes)
        {
            if (sourceAttributes == null || sourceAttributes.Count == 0)
                return;

            // Если целевая коллекция не инициализирована, создаем её
            if (targetAttributes == null)
                throw new ArgumentNullException(nameof(targetAttributes));

            // Добавляем отсутствующие атрибуты
            foreach (var srcAttr in sourceAttributes)
            {
                // Проверяем есть ли такой атрибут уже
                var existingAttr = targetAttributes.FirstOrDefault(
                    a => a.Name.Equals(srcAttr.Name, StringComparison.OrdinalIgnoreCase) &&
                         a.Value.Equals(srcAttr.Value, StringComparison.OrdinalIgnoreCase));

                // Если нет, добавляем
                if (existingAttr == null)
                {
                    targetAttributes.Add(new ShaderAttribute
                    {
                        Name = srcAttr.Name,
                        Value = srcAttr.Value,
                        FullText = srcAttr.FullText
                    });
                }
            }
        }
    }

    public static class GlslParser
    {
        public const string FRAGMENT_SPACE = "fragment";
        public const string VERTEX_SPACE = "vertex";

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

        public static string NormalizeLineBreaks(string code)
        {
            string normalized = code.Replace("\r\n", "\n").Replace("\r", "\n");
            return Regex.Replace(normalized, @"\n{2,}", "\n");
        }

        public static (string vertex, string fragment) ExtractShaderSources(string source)
        {
            source = CleanComments(source);

            string vertexSource = "";
            string fragmentSource = "";
            try
            {
                var vertexRegex = new Regex(@$"#{VERTEX_SPACE}\r?\n(.*?)(?=#{FRAGMENT_SPACE}|$)", RegexOptions.Singleline);
                var fragmentRegex = new Regex(@$"#{FRAGMENT_SPACE}\r?\n(.*?)$", RegexOptions.Singleline);

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

        public static (bool, bool) ValidateMainFunctions(string vertexSource, string fragmentSource)
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

            return (vertexMainCount == 1, fragmentMainCount == 1);
        }

        public static string RemoveAllAttributes(string source)
        {
            return Regex.Replace(source, @"\[\w+:[^\]]*\]", string.Empty);
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

        public static bool IsSamplerType(string type)
        {
            return type.StartsWith("sampler") ||
                   type.StartsWith("isampler") ||
                   type.StartsWith("usampler") ||
                   type.StartsWith("image");
        }

        public static bool IsCustomType(string csharpType, string glslType) =>
            (csharpType == glslType || csharpType.Contains(glslType)) && glslType != "float" && glslType != "bool" && glslType != "int" && glslType != "uint" && glslType != "double" &&
            glslType != "Vector2D<bool>" && glslType != "Vector3D<bool>" && glslType != "Vector4D<bool>" && glslType != "Vector2D<int>" &&
            glslType != "Vector3D<int>" && glslType != "Vector4D<int>" && glslType != "Vector2D<uint>" && glslType != "Vector3D<uint>" &&
            glslType != "Vector4D<uint>" && glslType != "Vector2D<float>" && glslType != "Vector3D<float>" && glslType != "Vector4D<float>" &&
            glslType != "Matrix2X2<float>" && glslType != "Matrix3X3<float>" && glslType != "Matrix4X4<float>" && glslType != "Matrix2X2<float>" &&
            glslType != "Matrix2X3<float>" && glslType != "Matrix2X4<float>" && glslType != "Matrix3X2<float>" && glslType != "Matrix3X3<float>" &&
            glslType != "Matrix3X4<float>" && glslType != "Matrix4X2<float>" && glslType != "Matrix4X3<float>" && glslType != "Matrix4X4<float>";

        public static bool IsSamplerType(UniformType type)
        {
            switch (type)
            {
                case UniformType.Sampler1D:
                case UniformType.Sampler2D:
                case UniformType.Sampler3D:
                case UniformType.SamplerCube:
                case UniformType.Sampler1DShadow:
                case UniformType.Sampler2DShadow:
                case UniformType.Sampler2DRect:
                case UniformType.Sampler2DRectShadow:
                case UniformType.Sampler1DArray:
                case UniformType.Sampler2DArray:
                case UniformType.SamplerBuffer:
                case UniformType.Sampler1DArrayShadow:
                case UniformType.Sampler2DArrayShadow:
                case UniformType.SamplerCubeShadow:
                case UniformType.IntSampler1D:
                case UniformType.IntSampler2D:
                case UniformType.IntSampler3D:
                case UniformType.IntSamplerCube:
                case UniformType.IntSampler2DRect:
                case UniformType.IntSampler1DArray:
                case UniformType.IntSampler2DArray:
                case UniformType.IntSamplerBuffer:
                case UniformType.UnsignedIntSampler1D:
                case UniformType.UnsignedIntSampler2D:
                case UniformType.UnsignedIntSampler3D:
                case UniformType.UnsignedIntSamplerCube:
                case UniformType.UnsignedIntSampler2DRect:
                case UniformType.UnsignedIntSampler1DArray:
                case UniformType.UnsignedIntSampler2DArray:
                case UniformType.UnsignedIntSamplerBuffer:
                case UniformType.SamplerCubeMapArray:
                case UniformType.SamplerCubeMapArrayShadow:
                case UniformType.IntSamplerCubeMapArray:
                case UniformType.UnsignedIntSamplerCubeMapArray:
                case UniformType.Sampler2DMultisample:
                case UniformType.IntSampler2DMultisample:
                case UniformType.UnsignedIntSampler2DMultisample:
                case UniformType.Sampler2DMultisampleArray:
                case UniformType.IntSampler2DMultisampleArray:
                case UniformType.UnsignedIntSampler2DMultisampleArray:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsMatrixType(UniformType type)
        {
            switch (type) {
                case UniformType.FloatMat2:
                case UniformType.FloatMat3:
                case UniformType.FloatMat4:
                case UniformType.FloatMat2x3:
                case UniformType.FloatMat2x4:
                case UniformType.FloatMat3x2:
                case UniformType.FloatMat3x4:
                case UniformType.FloatMat4x2:
                case UniformType.FloatMat4x3:
                case UniformType.DoubleMat2:
                case UniformType.DoubleMat3:
                case UniformType.DoubleMat4:
                case UniformType.DoubleMat2x3:
                case UniformType.DoubleMat2x4:
                case UniformType.DoubleMat3x2:
                case UniformType.DoubleMat3x4:
                case UniformType.DoubleMat4x2:
                case UniformType.DoubleMat4x3:
                    return true;

                default:
                    return false;
            }
        }
        public static Type MapUniformTypeToSystemType(UniformType uniformType)
        {
            switch (uniformType)
            {
                case UniformType.Int:
                    return typeof(int);
                case UniformType.UnsignedInt:
                    return typeof(uint);
                case UniformType.Float:
                    return typeof(float);
                case UniformType.Double:
                    return typeof(double);
                case UniformType.Bool:
                    return typeof(bool);

                case UniformType.FloatVec2:
                    return typeof(Vector2D<float>);
                case UniformType.FloatVec3:
                    return typeof(Vector3D<float>);
                case UniformType.FloatVec4:
                    return typeof(Vector4D<float>);

                case UniformType.IntVec2:
                    return typeof(Vector2D<int>);
                case UniformType.IntVec3:
                    return typeof(Vector3D<int>);
                case UniformType.IntVec4:
                    return typeof(Vector4D<int>);

                case UniformType.UnsignedIntVec2:
                    return typeof(Vector2D<uint>);
                case UniformType.UnsignedIntVec3:
                    return typeof(Vector3D<uint>);
                case UniformType.UnsignedIntVec4:
                    return typeof(Vector4D<uint>);

                case UniformType.DoubleVec2:
                    return typeof(Vector2D<double>);
                case UniformType.DoubleVec3:
                    return typeof(Vector3D<double>);
                case UniformType.DoubleVec4:
                    return typeof(Vector4D<double>);

                case UniformType.FloatMat2:
                    return typeof(Matrix2X2<float>);
                case UniformType.FloatMat3:
                    return typeof(Matrix3X3<float>);
                case UniformType.FloatMat4:
                    return typeof(Matrix4X4<float>);
                case UniformType.FloatMat2x3:
                    return typeof(Matrix2X3<float>);
                case UniformType.FloatMat2x4:
                    return typeof(Matrix2X4<float>);
                case UniformType.FloatMat3x2:
                    return typeof(Matrix3X2<float>);
                case UniformType.FloatMat3x4:
                    return typeof(Matrix3X4<float>);
                case UniformType.FloatMat4x2:
                    return typeof(Matrix4X2<float>);
                case UniformType.FloatMat4x3:
                    return typeof(Matrix4X3<float>);

                case UniformType.DoubleMat2:
                    return typeof(Matrix2X2<double>);
                case UniformType.DoubleMat3:
                    return typeof(Matrix3X3<double>);
                case UniformType.DoubleMat4:
                    return typeof(Matrix4X4<double>);
                case UniformType.DoubleMat2x3:
                    return typeof(Matrix2X3<double>);
                case UniformType.DoubleMat2x4:
                    return typeof(Matrix2X4<double>);
                case UniformType.DoubleMat3x2:
                    return typeof(Matrix3X2<double>);
                case UniformType.DoubleMat3x4:
                    return typeof(Matrix3X4<double>);
                case UniformType.DoubleMat4x2:
                    return typeof(Matrix4X2<double>);
                case UniformType.DoubleMat4x3:
                    return typeof(Matrix4X3<double>);

                case UniformType.Sampler1D:
                case UniformType.Sampler2D:
                case UniformType.Sampler3D:
                case UniformType.SamplerCube:
                case UniformType.Sampler1DShadow:
                case UniformType.Sampler2DShadow:
                case UniformType.Sampler2DRect:
                case UniformType.Sampler2DRectShadow:
                case UniformType.Sampler1DArray:
                case UniformType.Sampler2DArray:
                case UniformType.SamplerBuffer:
                case UniformType.Sampler1DArrayShadow:
                case UniformType.Sampler2DArrayShadow:
                case UniformType.SamplerCubeShadow:
                case UniformType.IntSampler1D:
                case UniformType.IntSampler2D:
                case UniformType.IntSampler3D:
                case UniformType.IntSamplerCube:
                case UniformType.IntSampler2DRect:
                case UniformType.IntSampler1DArray:
                case UniformType.IntSampler2DArray:
                case UniformType.IntSamplerBuffer:
                case UniformType.UnsignedIntSampler1D:
                case UniformType.UnsignedIntSampler2D:
                case UniformType.UnsignedIntSampler3D:
                case UniformType.UnsignedIntSamplerCube:
                case UniformType.UnsignedIntSampler2DRect:
                case UniformType.UnsignedIntSampler1DArray:
                case UniformType.UnsignedIntSampler2DArray:
                case UniformType.UnsignedIntSamplerBuffer:
                case UniformType.SamplerCubeMapArray:
                case UniformType.SamplerCubeMapArrayShadow:
                case UniformType.IntSamplerCubeMapArray:
                case UniformType.UnsignedIntSamplerCubeMapArray:
                case UniformType.Sampler2DMultisample:
                case UniformType.IntSampler2DMultisample:
                case UniformType.UnsignedIntSampler2DMultisample:
                case UniformType.Sampler2DMultisampleArray:
                case UniformType.IntSampler2DMultisampleArray:
                case UniformType.UnsignedIntSampler2DMultisampleArray:
                    return typeof(string);

                default:
                    return typeof(float);
            }
        }

        public static object GetDefaultValueForType(UniformType uniformType)
        {
            switch (uniformType)
            {
                case UniformType.Int:
                    return 0;
                case UniformType.UnsignedInt:
                    return 0u;
                case UniformType.Float:
                    return 0.0f;
                case UniformType.Double:
                    return 0.0;
                case UniformType.Bool:
                    return false;

                case UniformType.FloatVec2:
                    return new Vector2D<float>(0, 0);
                case UniformType.FloatVec3:
                    return new Vector3D<float>(0, 0, 0);
                case UniformType.FloatVec4:
                    return new Vector4D<float>(0, 0, 0, 0);

                case UniformType.IntVec2:
                    return new Vector2D<int>(0, 0);
                case UniformType.IntVec3:
                    return new Vector3D<int>(0, 0, 0);
                case UniformType.IntVec4:
                    return new Vector4D<int>(0, 0, 0, 0);

                case UniformType.UnsignedIntVec2:
                    return new Vector2D<uint>(0, 0);
                case UniformType.UnsignedIntVec3:
                    return new Vector3D<uint>(0, 0, 0);
                case UniformType.UnsignedIntVec4:
                    return new Vector4D<uint>(0, 0, 0, 0);


                case UniformType.DoubleVec2:
                    return new Vector2D<double>(0, 0);
                case UniformType.DoubleVec3:
                    return new Vector3D<double>(0, 0, 0);
                case UniformType.DoubleVec4:
                    return new Vector4D<double>(0, 0, 0, 0);

                case UniformType.FloatMat2:
                    return Matrix2X2<float>.Identity;
                case UniformType.FloatMat3:
                    return Matrix3X3<float>.Identity;
                case UniformType.FloatMat4:
                    return Matrix4X4<float>.Identity;
                case UniformType.FloatMat2x3:
                    return Matrix2X3<float>.Identity;
                case UniformType.FloatMat2x4:
                    return Matrix2X4<float>.Identity;
                case UniformType.FloatMat3x2:
                    return Matrix3X2<float>.Identity;
                case UniformType.FloatMat3x4:
                    return Matrix3X4<float>.Identity;
                case UniformType.FloatMat4x2:
                    return Matrix4X2<float>.Identity;
                case UniformType.FloatMat4x3:
                    return Matrix4X3<float>.Identity;

                case UniformType.DoubleMat2:
                    return Matrix2X2<double>.Identity;
                case UniformType.DoubleMat3:
                    return Matrix3X3<double>.Identity;
                case UniformType.DoubleMat4:
                    return Matrix4X4<double>.Identity;
                case UniformType.DoubleMat2x3:
                    return Matrix2X3<double>.Identity;
                case UniformType.DoubleMat2x4:
                    return Matrix2X4<double>.Identity;
                case UniformType.DoubleMat3x2:
                    return Matrix3X2<double>.Identity;
                case UniformType.DoubleMat3x4:
                    return Matrix3X4<double>.Identity;
                case UniformType.DoubleMat4x2:
                    return Matrix4X2<double>.Identity;
                case UniformType.DoubleMat4x3:
                    return Matrix4X3<double>.Identity;

                case UniformType.Sampler1D:
                case UniformType.Sampler2D:
                case UniformType.Sampler3D:
                case UniformType.SamplerCube:
                case UniformType.Sampler1DShadow:
                case UniformType.Sampler2DShadow:
                case UniformType.Sampler2DRect:
                case UniformType.Sampler2DRectShadow:
                case UniformType.Sampler1DArray:
                case UniformType.Sampler2DArray:
                case UniformType.SamplerBuffer:
                case UniformType.Sampler1DArrayShadow:
                case UniformType.Sampler2DArrayShadow:
                case UniformType.SamplerCubeShadow:
                case UniformType.IntSampler1D:
                case UniformType.IntSampler2D:
                case UniformType.IntSampler3D:
                case UniformType.IntSamplerCube:
                case UniformType.IntSampler2DRect:
                case UniformType.IntSampler1DArray:
                case UniformType.IntSampler2DArray:
                case UniformType.IntSamplerBuffer:
                case UniformType.UnsignedIntSampler1D:
                case UniformType.UnsignedIntSampler2D:
                case UniformType.UnsignedIntSampler3D:
                case UniformType.UnsignedIntSamplerCube:
                case UniformType.UnsignedIntSampler2DRect:
                case UniformType.UnsignedIntSampler1DArray:
                case UniformType.UnsignedIntSampler2DArray:
                case UniformType.UnsignedIntSamplerBuffer:
                case UniformType.SamplerCubeMapArray:
                case UniformType.SamplerCubeMapArrayShadow:
                case UniformType.IntSamplerCubeMapArray:
                case UniformType.UnsignedIntSamplerCubeMapArray:
                case UniformType.Sampler2DMultisample:
                case UniformType.IntSampler2DMultisample:
                case UniformType.UnsignedIntSampler2DMultisample:
                case UniformType.Sampler2DMultisampleArray:
                case UniformType.IntSampler2DMultisampleArray:
                case UniformType.UnsignedIntSampler2DMultisampleArray:
                    return string.Empty;

                default:
                    return 0.0f;
            }
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

        public static GlslVersionModel ExtractGlslVersion(string shaderSource)
        {
            var versionRegex = new Regex(@"#version\s+(\d+)(?:\s+(\w+))?", RegexOptions.Multiline);
            var match = versionRegex.Match(shaderSource);

            if (!match.Success)
            {
                return new GlslVersionModel { Version = 110 };
            }

            int version = int.Parse(match.Groups[1].Value);
            string profile = match.Groups[2].Success ? match.Groups[2].Value : string.Empty;

            return new GlslVersionModel
            {
                Version = version,
                Profile = profile
            };
        }


        #region InOutParams
        public static List<VertexOutParams> ExtractVertexOutputs(string source)
        {
            var outputs = new List<VertexOutParams>();
            var processedOutNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ExtractSimpleOutputs(source, outputs, processedOutNames);
            ExtractBlockOutputs(source, outputs, processedOutNames);

            return outputs;
        }

        private static void ExtractSimpleOutputs(string source, List<VertexOutParams> outputs, HashSet<string> processedOutNames)
        {
            var simpleOutRegex = new Regex(@"(?:layout\s*\(\s*location\s*=\s*(\d+)\s*\))?\s*out\s+(?!layout)(?:highp|mediump|lowp)?\s*(\w+)\s+(\w+)(?:\[(\w+|\d+)\])?\s*;");

            foreach (Match match in simpleOutRegex.Matches(source))
            {
                int? location = null;
                if (!string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    location = int.Parse(match.Groups[1].Value);
                }

                var type = match.Groups[2].Value;
                var name = match.Groups[3].Value;

                if (processedOutNames.Contains(name))
                {
                    continue;
                }

                int? arraySize = null;
                if (match.Groups[4].Success)
                {
                    var sizeValue = match.Groups[4].Value;
                    if (int.TryParse(sizeValue, out int size))
                    {
                        arraySize = size;
                    }
                    else
                    {
                        arraySize = ResolveArraySizeIdentifier(source, sizeValue);
                    }
                }

                var field = new BlockFieldModel
                {
                    Type = type,
                    CSharpTypeName = MapGlslTypeToCSharp(type),
                    Name = name,
                    ArraySize = arraySize,
                    Fulltext = match.Value,
                    Attributes = ExtractAttributesAbove(source, match.Index)
                };

                var outParam = new VertexOutParams
                {
                    Location = location,
                    Type = type,
                    CSharpTypeName = MapGlslTypeToCSharp(type),
                    FillText = match.Value
                };

                outParam.Fields.Add(field);
                outputs.Add(outParam);
                processedOutNames.Add(name);
            }
        }

        private static void ExtractBlockOutputs(string source, List<VertexOutParams> outputs, HashSet<string> processedOutNames)
        {
            var blockStartRegex = new Regex(@"(?:layout\s*\(\s*location\s*=\s*(\d+)\s*\))?\s*out\s+(\w+)\s*{");

            foreach (Match blockMatch in blockStartRegex.Matches(source))
            {
                int? location = null;
                if (!string.IsNullOrEmpty(blockMatch.Groups[1].Value))
                {
                    location = int.Parse(blockMatch.Groups[1].Value);
                }

                var blockTypeName = blockMatch.Groups[2].Value;

                int blockStart = blockMatch.Index + blockMatch.Length - 1;
                int blockEnd = FindMatchingBrace(source, blockStart);

                if (blockEnd == -1)
                {
                    continue;
                }

                string blockContent = source.Substring(blockStart + 1, blockEnd - blockStart - 1);

                var instanceNameMatch = Regex.Match(source.Substring(blockEnd + 1), @"^\s*(\w+)\s*;");
                string instanceName = instanceNameMatch.Success ? instanceNameMatch.Groups[1].Value : string.Empty;

                if (!string.IsNullOrEmpty(instanceName) && processedOutNames.Contains(instanceName))
                {
                    continue;
                }

                var outParam = new VertexOutParams
                {
                    Location = location,
                    InstanceName = instanceName,
                    Type = blockTypeName,
                    CSharpTypeName = GlslParser.MapGlslTypeToCSharp(blockTypeName), 
                    FillText = source.Substring(blockMatch.Index, blockEnd - blockMatch.Index + instanceNameMatch.Length + 1)
                };

                ExtractBlockFields(blockContent, outParam);

                if (outParam.Fields.Count > 0)
                {
                    outputs.Add(outParam);
                    if (!string.IsNullOrEmpty(instanceName))
                    {
                        processedOutNames.Add(instanceName);
                    }
                }
            }
        }

        private static void ExtractBlockFields(string blockContent, VertexOutParams outParam)
        {
            var fieldRegex = new Regex(@"(?:highp|mediump|lowp)?\s*(\w+)\s+(\w+)(?:\[(\w+|\d+)\])?\s*;");

            foreach (Match fieldMatch in fieldRegex.Matches(blockContent))
            {
                var type = fieldMatch.Groups[1].Value;
                var name = fieldMatch.Groups[2].Value;

                int? arraySize = null;
                if (fieldMatch.Groups[3].Success)
                {
                    var sizeValue = fieldMatch.Groups[3].Value;
                    if (int.TryParse(sizeValue, out int size))
                    {
                        arraySize = size;
                    }
                }

                var field = new BlockFieldModel
                {
                    Type = type,
                    CSharpTypeName = MapGlslTypeToCSharp(type),
                    Name = name,
                    ArraySize = arraySize,
                    Fulltext = fieldMatch.Value
                };

                outParam.Fields.Add(field);
            }
        }

        private static int FindMatchingBrace(string text, int openBracePosition)
        {
            int depth = 1;
            for (int i = openBracePosition + 1; i < text.Length; i++)
            {
                if (text[i] == '{')
                {
                    depth++;
                }
                else if (text[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }



        public static List<FragmentInnerParams> ExtractFragmentInputs(string source)
        {
            var inputs = new List<FragmentInnerParams>();
            var processedInNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            ExtractSimpleInputs(source, inputs, processedInNames);
            ExtractBlockInputs(source, inputs, processedInNames);

            return inputs;
        }

        private static void ExtractSimpleInputs(string source, List<FragmentInnerParams> inputs, HashSet<string> processedInNames)
        {
            var simpleInRegex = new Regex(@"(?:layout\s*\(\s*location\s*=\s*(\d+)\s*\))?\s*in\s+(?!layout)(?:highp|mediump|lowp)?\s*(\w+)\s+(\w+)(?:\[(\w+|\d+)\])?\s*;");

            foreach (Match match in simpleInRegex.Matches(source))
            {
                int? location = null;
                if (!string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    location = int.Parse(match.Groups[1].Value);
                }

                var type = match.Groups[2].Value;
                var name = match.Groups[3].Value;

                if (processedInNames.Contains(name))
                {
                    continue;
                }

                int? arraySize = null;
                if (match.Groups[4].Success)
                {
                    var sizeValue = match.Groups[4].Value;
                    if (int.TryParse(sizeValue, out int size))
                    {
                        arraySize = size;
                    }
                    else
                    {
                        arraySize = ResolveArraySizeIdentifier(source, sizeValue);
                    }
                }

                var field = new BlockFieldModel
                {
                    Type = type,
                    CSharpTypeName = MapGlslTypeToCSharp(type),
                    Name = name,
                    ArraySize = arraySize,
                    Fulltext = match.Value,
                    Attributes = ExtractAttributesAbove(source, match.Index)
                };

                var inParam = new FragmentInnerParams
                {
                    Location = location,
                    Type = type,
                    CSharpTypeName = MapGlslTypeToCSharp(type),
                    FillText = match.Value
                };

                inParam.Fields.Add(field);
                inputs.Add(inParam);
                processedInNames.Add(name);
            }
        }

        private static void ExtractBlockInputs(string source, List<FragmentInnerParams> inputs, HashSet<string> processedInNames)
        {
            var blockStartRegex = new Regex(@"(?:layout\s*\(\s*location\s*=\s*(\d+)\s*\))?\s*in\s+(\w+)\s*{");

            foreach (Match blockMatch in blockStartRegex.Matches(source))
            {
                int? location = null;
                if (!string.IsNullOrEmpty(blockMatch.Groups[1].Value))
                {
                    location = int.Parse(blockMatch.Groups[1].Value);
                }

                var blockTypeName = blockMatch.Groups[2].Value;

                int blockStart = blockMatch.Index + blockMatch.Length - 1;
                int blockEnd = FindMatchingBrace(source, blockStart);

                if (blockEnd == -1)
                {
                    continue;
                }

                string blockContent = source.Substring(blockStart + 1, blockEnd - blockStart - 1);

                var instanceNameMatch = Regex.Match(source.Substring(blockEnd + 1), @"^\s*(\w+)\s*;");
                string instanceName = instanceNameMatch.Success ? instanceNameMatch.Groups[1].Value : string.Empty;

                if (!string.IsNullOrEmpty(instanceName) && processedInNames.Contains(instanceName))
                {
                    continue;
                }

                var inParam = new FragmentInnerParams
                {
                    Location = location,
                    InstanceName = instanceName,
                    Type = blockTypeName,
                    CSharpTypeName = GlslParser.MapGlslTypeToCSharp(blockTypeName),
                    FillText = source.Substring(blockMatch.Index, blockEnd - blockMatch.Index + instanceNameMatch.Length + 1)
                };

                ExtractBlockFields(blockContent, inParam);

                if (inParam.Fields.Count > 0)
                {
                    inputs.Add(inParam);
                    if (!string.IsNullOrEmpty(instanceName))
                    {
                        processedInNames.Add(instanceName);
                    }
                }
            }
        }

        private static void ExtractBlockFields(string blockContent, FragmentInnerParams inParam)
        {
            var fieldRegex = new Regex(@"(?:highp|mediump|lowp)?\s*(\w+)\s+(\w+)(?:\[(\w+|\d+)\])?\s*;");

            foreach (Match fieldMatch in fieldRegex.Matches(blockContent))
            {
                var type = fieldMatch.Groups[1].Value;
                var name = fieldMatch.Groups[2].Value;

                int? arraySize = null;
                if (fieldMatch.Groups[3].Success)
                {
                    var sizeValue = fieldMatch.Groups[3].Value;
                    if (int.TryParse(sizeValue, out int size))
                    {
                        arraySize = size;
                    }
                }

                var field = new BlockFieldModel
                {
                    Type = type,
                    CSharpTypeName = MapGlslTypeToCSharp(type),
                    Name = name,
                    ArraySize = arraySize,
                    Fulltext = fieldMatch.Value
                };

                inParam.Fields.Add(field);
            }
        }

        #endregion

        #region Attributes
        public static List<ShaderAttribute> ExtractAttributesAbove(string source, int startPosition)
        {
            List<ShaderAttribute> attributes = new List<ShaderAttribute>();

            if (startPosition < 0 || startPosition >= source.Length)
                return attributes;

            int currentLineStart = startPosition;

            while (currentLineStart < source.Length && (source[currentLineStart] == '\r' || source[currentLineStart] == '\n'))
            {
                currentLineStart++;
            }
            if (currentLineStart >= source.Length)
                return attributes;
            if (currentLineStart > 0)
            {
                int tempStart = currentLineStart;
                while (tempStart > 0 && source[tempStart - 1] != '\n')
                {
                    tempStart--;
                }
                currentLineStart = tempStart;
            }
            if (currentLineStart == 0)
                return attributes;

            var attributeRegex = new Regex(@"\[(\w+)(?::([^\]]*))?\]");

            int checkPosition = currentLineStart - 1;
            bool foundNonAttributeLine = false;

            while (checkPosition > 0 && !foundNonAttributeLine)
            {
                int lineEnd = checkPosition;

                if (lineEnd > 0 && source[lineEnd - 1] == '\r')
                {
                    lineEnd--;
                }
                int lineStart = lineEnd;
                while (lineStart > 0 && source[lineStart - 1] != '\n')
                {
                    lineStart--;
                }

                if (lineStart <= lineEnd)
                {
                    string line = source.Substring(lineStart, lineEnd - lineStart).Trim();

                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                    {
                        checkPosition = lineStart - 1;
                        continue;
                    }
                    var matches = attributeRegex.Matches(line);

                    if (matches.Count > 0)
                    {
                        string lineWithoutAttributes = line;

                        foreach (Match match in matches)
                        {
                            lineWithoutAttributes = lineWithoutAttributes.Replace(match.Value, "");
                            attributes.Add(new ShaderAttribute
                            {
                                Name = match.Groups[1].Value,
                                Value = match.Groups[2].Success ? match.Groups[2].Value : string.Empty,
                                FullText = match.Value
                            });
                        }
                        if (!string.IsNullOrWhiteSpace(lineWithoutAttributes))
                        {
                            foundNonAttributeLine = true;
                        }
                    }
                    else
                    {
                        foundNonAttributeLine = true;
                    }
                    checkPosition = lineStart - 1;
                }
                else
                {
                    break;
                }
            }
            return attributes.Reverse<ShaderAttribute>().ToList();
        }

        public static List<VertexAttribute> ParseVertexAttributes(string shaderSource)
        {
            var attributes = new List<VertexAttribute>();

            var lines = shaderSource.Split('\n');
            int? currentLocation = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                var layoutMatch = Regex.Match(line, @"layout\s*\(\s*location\s*=\s*(\d+)\s*\)");
                if (layoutMatch.Success)
                {
                    currentLocation = int.Parse(layoutMatch.Groups[1].Value);

                    var attrMatch = Regex.Match(line, @"in\s+(\w+)\s+(\w+)\s*;");
                    if (attrMatch.Success)
                    {
                        var type = attrMatch.Groups[1].Value;
                        var name = attrMatch.Groups[2].Value;

                        attributes.Add(new VertexAttribute
                        {
                            Name = name,
                            Location = currentLocation,
                            Type = type,
                            CSharpTypeName = GlslParser.MapGlslTypeToCSharp(type),
                            FullText = line,
                            Attributes = GlslParser.ExtractAttributesAbove(shaderSource, attrMatch.Index)
                        });

                        currentLocation = null;
                    }
                }
                else if (line.StartsWith("in "))
                {
                    var attrMatch = Regex.Match(line, @"in\s+(\w+)\s+(\w+)\s*;");
                    if (attrMatch.Success)
                    {
                        var type = attrMatch.Groups[1].Value;
                        var name = attrMatch.Groups[2].Value;

                        attributes.Add(new VertexAttribute
                        {
                            Name = name,
                            Location = currentLocation,
                            Type = type,
                            CSharpTypeName = GlslParser.MapGlslTypeToCSharp(type),
                            FullText = line,
                            Attributes = GlslParser.ExtractAttributesAbove(shaderSource, attrMatch.Index)
                        });

                        currentLocation = null;
                    }
                }
            }

            return attributes;
        }

        #endregion

        #region const
        public static List<GlslConstantModel> ExtractGlslConstants(string source)
        {
            var constants = new List<GlslConstantModel>();
            var processedConstantNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var constantRegex = new Regex(@"const\s+(\w+)\s+(\w+)\s*=\s*([^;]+);", RegexOptions.Multiline);

            foreach (Match match in constantRegex.Matches(source))
            {
                var type = match.Groups[1].Value;
                var name = match.Groups[2].Value;
                var value = match.Groups[3].Value.Trim();

                if (processedConstantNames.Contains(name))
                {
                    continue;
                }

                var constant = new GlslConstantModel
                {
                    Type = type,
                    Name = name,
                    Value = value,
                    CSharpTypeName = GlslParser.MapGlslTypeToCSharp(type),
                    FullText = match.Value,
                };

                constant.Attributes = ExtractAttributesAbove(source, match.Index);
                constants.Add(constant);
                processedConstantNames.Add(name);
            }

            return constants;
        }

        #endregion

        #region Uniform-блоки
        public static List<UniformBlockModel> ExtractUniformBlocks(string source)
        {
            var blocks = new List<UniformBlockModel>();
            var blockRegex = new Regex(
                @"(?:layout\s*\(\s*(std140|std430|packed|shared)(?:\s*,\s*binding\s*=\s*(\d+))?\)\s*)?" +
                @"uniform\s+(\w+)?\s*\{([^}]+)\}\s*(\w+)?;",
                RegexOptions.Multiline);
            var processedBlockNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

                if (processedBlockNames.Contains(name))
                {
                    continue;
                }

                LayoutType blockType = GetUniformBlockType(layoutTypeStr);

                var uniblock = new UniformBlockModel
                {
                    Name = name,
                    UniformBlockType = blockType,
                    Binding = binding,
                    FullText = match.Value,
                    Fields = ParseUniformBlockFields(fieldsText, source),
                    Attributes = ExtractAttributesAbove(source, match.Index),
                    InstanceName = instanceName
                };
                blocks.Add(uniblock);
            }

            return blocks;
        }

        public static List<BlockFieldModel> ParseUniformBlockFields(string fieldsText, string fullSource)
        {
            List<BlockFieldModel> fields = new List<BlockFieldModel>();

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
                BlockFieldModel field = new BlockFieldModel()
                {
                    Type = type,
                    Name = name,
                    ArraySize = arraySize,
                    Attributes = GlslParser.ExtractAttributesAbove(fullSource, match.Index),
                    CSharpTypeName = GlslParser.MapGlslTypeToCSharp(type),
                    Fulltext = match.Value,
                };
                fields.Add(field);
            }
            return fields;
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
        
        private static LayoutType GetUniformBlockType(string layoutType)
        {
            return layoutType.ToLower() switch
            {
                "std140" => LayoutType.STD140,
                "std430" => LayoutType.STD430,
                "packed" => LayoutType.Packed,
                "shared" => LayoutType.Shared,
                _ => LayoutType.Ordinary
            };
        }

        #endregion

        #region Uniforms
        public static List<UniformModel> ExtractUniforms(string source)
        {
            List<UniformModel> uniforms = new List<UniformModel>();
            var processedUniformNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var uniformRegex = new Regex(@"(?:layout\s*\(\s*binding\s*=\s*(\d+)\s*\))?\s*uniform\s+(?!layout)(?:highp|mediump|lowp)?\s*(\w+)\s+(\w+)(?:\[(\w+|\d+)\])?\s*;");

            foreach (Match match in uniformRegex.Matches(source))
            {
                int? binding = null;
                if (!string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    binding = int.Parse(match.Groups[1].Value);
                }

                var type = match.Groups[2].Value;
                var name = match.Groups[3].Value;

                if (processedUniformNames.Contains(name))
                {
                    continue;
                }

                int? arraySize = null;
                if (match.Groups[4].Success)
                {
                    var sizeValue = match.Groups[4].Value;
                    if (int.TryParse(sizeValue, out int size))
                    {
                        arraySize = size;
                    }
                    else
                    {
                        arraySize = ResolveArraySizeIdentifier(source, sizeValue);
                    }
                }

                var uniformField = new UniformModel
                {
                    Type = type,
                    CSharpTypeName = MapGlslTypeToCSharp(type),
                    Name = name,
                    FullText = match.Value,
                    ArraySize = arraySize,
                    Attributes = ExtractAttributesAbove(source, match.Index),
                    Binding = binding
                };

                uniforms.Add(uniformField);
                processedUniformNames.Add(name);
            }

            return uniforms;
        }

        #endregion

        #region Structs
        public static List<GlslStructModel> ExtractGlslStructures(string sourceCode)
        {
            var structures = new List<GlslStructModel>();
            //var processedStructNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var structRegex = new Regex(@"struct\s+(\w+)\s*\{([^}]+)\}\s*(?:\w+(?:\s*,\s*\w+)*\s*)?;?", RegexOptions.Multiline);
            var matches = structRegex.Matches(sourceCode);

            foreach (Match match in matches)
            {
                string structName = match.Groups[1].Value;

                //if (processedStructNames.Contains(structName))
                //{
                //    continue;
                //}

                var structure = new GlslStructModel
                {
                    Name = structName,
                    Fields = ParseStructFields(match.Groups[2].Value),
                    FullText = match.Value,
                    Attributes = ExtractAttributesAbove(sourceCode, match.Index)
                };

                structures.Add(structure);
                //processedStructNames.Add(structName);
            }

            return structures;
        }

        public static List<GlslStructFieldModel> ParseStructFields(string fieldsText)
        {
            List<GlslStructFieldModel> fields = new List<GlslStructFieldModel>();

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
                var field = new GlslStructFieldModel
                {
                    Type = type,
                    CSharpTypeName = MapGlslTypeToCSharp(type),
                    Name = name,
                    ArraySize = arraySize
                };
                fields.Add(field);
            }

            return fields;
        }

        public static List<GlslStructInstance> ExtractStructInstances(string sourceCode, List<GlslStructModel> structures)
        {
            var instances = new List<GlslStructInstance>();
            var processedInstanceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (structures == null || structures.Count == 0 || string.IsNullOrEmpty(sourceCode))
                return instances;

            string cleanedCode = CleanComments(sourceCode);
            Dictionary<int, int> topLevelBlocks = FindTopLevelBlocks(cleanedCode);
            foreach (var structure in structures)
            {
                var uniformRegex = new Regex($@"uniform\s+(?:highp|mediump|lowp)?\s*{Regex.Escape(structure.Name)}\s+(\w+)(?:\[(\w+|\d+)\])?\s*;");
                foreach (Match match in uniformRegex.Matches(cleanedCode))
                {
                    if (IsAtTopLevel(match.Index, match.Length, topLevelBlocks))
                    {
                        var instanceName = match.Groups[1].Value;
                        if (processedInstanceNames.Contains(instanceName))
                            continue;

                        int? arraySize = null;

                        if (match.Groups[2].Success)
                        {
                            var sizeValue = match.Groups[2].Value;
                            if (int.TryParse(sizeValue, out int size))
                            {
                                arraySize = size;
                            }
                            else
                            {
                                arraySize = ResolveArraySizeIdentifier(cleanedCode, sizeValue);
                            }
                        }

                        instances.Add(new GlslStructInstance
                        {
                            Structure = structure,
                            InstanceName = instanceName,
                            IsUniform = true,
                            FullText = match.Value,
                            ArraySize = arraySize,
                            Attributes = GlslParser.ExtractAttributesAbove(sourceCode, match.Index)
                        });

                        processedInstanceNames.Add(instanceName);
                    }
                }
            }

            foreach (var structure in structures)
            {
                var structDefinitionRegex = new Regex($@"struct\s+{Regex.Escape(structure.Name)}\s*{{[^}}]*}}\s*([\w,\s]+)\s*;");
                foreach (Match match in structDefinitionRegex.Matches(cleanedCode))
                {
                    if (match.Groups[1].Success)
                    {
                        var instanceNames = match.Groups[1].Value.Split(',');
                        foreach (var instanceNameWithSpaces in instanceNames)
                        {
                            var instanceName = instanceNameWithSpaces.Trim();
                            if (!string.IsNullOrEmpty(instanceName))
                            {
                                var arrayMatch = Regex.Match(instanceName, @"(\w+)(?:\[(\w+|\d+)\])?");
                                if (arrayMatch.Success)
                                {
                                    var name = arrayMatch.Groups[1].Value;
                                    if (processedInstanceNames.Contains(name))
                                        continue;

                                    int? arraySize = null;

                                    if (arrayMatch.Groups[2].Success)
                                    {
                                        var sizeValue = arrayMatch.Groups[2].Value;
                                        if (int.TryParse(sizeValue, out int size))
                                        {
                                            arraySize = size;
                                        }
                                        else
                                        {
                                            arraySize = ResolveArraySizeIdentifier(cleanedCode, sizeValue);
                                        }
                                    }

                                    instances.Add(new GlslStructInstance
                                    {
                                        Structure = structure,
                                        InstanceName = name,
                                        IsUniform = false,
                                        FullText = match.Value,
                                        ArraySize = arraySize,
                                        Attributes = GlslParser.ExtractAttributesAbove(sourceCode, match.Index)
                                    });

                                    processedInstanceNames.Add(name);
                                }
                            }
                        }
                    }
                }
            }

            foreach (var structure in structures)
            {
                var regex1 = new Regex($@"(?<!struct\s+){Regex.Escape(structure.Name)}\s+(\w+)(?:\[(\w+|\d+)\])?(?:\s*=\s*[^;]+)?\s*;");
                foreach (Match match in regex1.Matches(cleanedCode))
                {
                    if (IsAtTopLevel(match.Index, match.Length, topLevelBlocks))
                    {
                        var instanceName = match.Groups[1].Value;
                        if (processedInstanceNames.Contains(instanceName))
                            continue;

                        int? arraySize = null;

                        if (match.Groups[2].Success)
                        {
                            var sizeValue = match.Groups[2].Value;
                            if (int.TryParse(sizeValue, out int size))
                            {
                                arraySize = size;
                            }
                            else
                            {
                                arraySize = ResolveArraySizeIdentifier(cleanedCode, sizeValue);
                            }
                        }

                        instances.Add(new GlslStructInstance
                        {
                            Structure = structure,
                            InstanceName = instanceName,
                            IsUniform = false,
                            FullText = match.Value,
                            ArraySize = arraySize,
                            Attributes = GlslParser.ExtractAttributesAbove(sourceCode, match.Index)
                        });

                        processedInstanceNames.Add(instanceName);
                    }
                }
            }

            return instances;
        }

        private static Dictionary<int, int> FindTopLevelBlocks(string code)
        {
            var blocks = new Dictionary<int, int>();
            var braceStack = new Stack<int>();

            for (int i = 0; i < code.Length; i++)
            {
                if (code[i] == '{')
                {
                    braceStack.Push(i);
                }
                else if (code[i] == '}' && braceStack.Count > 0)
                {
                    int openBracePos = braceStack.Pop();
                    blocks[openBracePos] = i;
                }
            }

            return blocks;
        }

        private static bool IsAtTopLevel(int position, int length, Dictionary<int, int> blocks)
        {
            foreach (var block in blocks)
            {
                int blockStart = block.Key;
                int blockEnd = block.Value;

                if (position > blockStart && position + length < blockEnd)
                    return false;
            }

            return true;
        }

        #endregion

        #region Methods
        public static List<GlslMethodInfo> ExtractMethods(string sourceCode)
        {
            var methods = new List<GlslMethodInfo>();

            string cleanedCode = CleanComments(sourceCode);

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

        private static List<GlslMethodParamInfo> ParseParameters(string paramsText)
        {
            var parameters = new List<GlslMethodParamInfo>();

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

                parameters.Add(new GlslMethodParamInfo
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
        
        #endregion
    }


    public class GlslVersionModel
    {
        public string Profile { get; set; } = string.Empty;
        public int Version { get; set; }
    }
    
    public class GlslShaderModel
    {
        public string FullText { get; set; } = string.Empty;
        public VertexShaderModel Vertex { get; set; } = new VertexShaderModel();
        public FragmentShaderModel Fragment { get; set; } = new FragmentShaderModel();
        public List<RSFileInfo> RSFiles { get; set; } = new List<RSFileInfo>();
    }

    public class VertexShaderModel : ShaderChankModel
    {
        public override string PREFIX { get; set; } = "vertex";
        public List<VertexAttribute> VertexAttributes { get; set; } = new List<VertexAttribute>();
        public List<VertexOutParams> VertexOutParams { get; set; } = new List<VertexOutParams>();
    }

    public class FragmentShaderModel : ShaderChankModel
    {
        public override string PREFIX { get; set; } = "fragment";
        public List<FragmentInnerParams> InnerParams { get; set; } = new List<FragmentInnerParams>();
    }

    public class ShaderChankModel
    {
        public virtual string PREFIX { get; set; } = string.Empty;
        public GlslVersionModel? Version { get; set; }
        public List<GlslStructInstance> StructureInstances { get; set; } = new List<GlslStructInstance>();
        public List<GlslConstantModel> Constants { get; set; } = new List<GlslConstantModel>();
        public List<UniformModel> Uniforms { get; set; } = new List<UniformModel>();
        public List<UniformBlockModel> UniformsBlocks { get; set; } = new List<UniformBlockModel>();
        public List<GlslStructModel> Structures { get; set; } = new List<GlslStructModel>();
        public List<GlslMethodInfo> Methods { get; set; } = new List<GlslMethodInfo>();
        public string FullText { get; set; } = string.Empty; 
    }

    public class VertexAttribute
    {
        public List<ShaderAttribute> Attributes { get; set; } = new List<ShaderAttribute>();
        public string Name { get; set; } = string.Empty;
        public int? Location { get; set; }
        public string Type { get; set; } = string.Empty;
        public string CSharpTypeName { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
    }

    public class VertexOutParams
    {
        public int? Location { get; set; }
        public string? InstanceName { get; set; } = null;
        public string Type { get; set; } = string.Empty;
        public string CSharpTypeName { get; set; } = string.Empty;
        public string FillText { get; set; } = string.Empty;
        public List<BlockFieldModel> Fields { get; set; } = new List<BlockFieldModel>();
    }

    public class FragmentInnerParams
    {
        public int? Location { get; set; }
        public string? InstanceName { get; set; } = null;
        public string Type { get; set; } = string.Empty;
        public string CSharpTypeName { get; set; } = string.Empty;
        public string FillText { get; set; } = string.Empty;
        public List<BlockFieldModel> Fields { get; set; } = new List<BlockFieldModel>();
    }

    public class GlslConstantModel
    {
        public string Type { get; set; } = string.Empty;
        public string CSharpTypeName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
        public List<ShaderAttribute> Attributes { get; set; } = new List<ShaderAttribute>();
    }
    
    public class GlslStructModel
    {
        public string Name { get; set; } = string.Empty;
        public string CSharpTypeName { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
        public List<GlslStructFieldModel> Fields { get; set; } = new List<GlslStructFieldModel>();
        public List<ShaderAttribute> Attributes { get; set; } = new List<ShaderAttribute>();
        public string TypeMappingId { get; set; } = string.Empty;
    }

    public class GlslStructInstance
    {
        public GlslStructModel Structure { get; set; }
        public List<ShaderAttribute> Attributes { get; set; } = new List<ShaderAttribute>();
        public string OriginalInstanceName { get; set; } = string.Empty;
        public string InstanceName { get; set; } = string.Empty;
        public bool IsUniform { get; set; } = false;
        public string FullText { get; set; } = string.Empty;
        public int? ArraySize { get; set; }
        public bool AdditionPair { get; set; } = false;
    }

    public class GlslStructFieldModel
    {
        public List<ShaderAttribute> Attributes { get; set; } = new List<ShaderAttribute>();
        public string Type { get; set; } = string.Empty;
        public string CSharpTypeName { get; set; } = string.Empty;
        public string Name {  set; get; } = string.Empty;
        public int? ArraySize { get; set; }
        public string TypeMappingId { get; set; } = string.Empty;
    }

    public class UniformModel
    {
        public List<ShaderAttribute> Attributes { get; set; } = new List<ShaderAttribute>();
        public string Type { get; set; } = string.Empty;
        public string CSharpTypeName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
        public int? ArraySize { get; set; }
        public int? Binding { get; set; }
    }

    public class ShaderAttribute
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
    }

    public class GlslMethodInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public string FullMethodText { get; set; } = string.Empty;
        public List<GlslMethodParamInfo> Params { get; set; } = new List<GlslMethodParamInfo>();
        public List<ShaderAttribute> Attributes { get; set; } = new List<ShaderAttribute>();
    }

    public class GlslMethodParamInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string[] Qualifiers { get; set; }
    }

    public class UniformBlockModel
    {
        public List<ShaderAttribute> Attributes { get; set; } = new List<ShaderAttribute>();
        public LayoutType UniformBlockType { get; set; } = LayoutType.Ordinary;
        public string CSharpTypeName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
        public int? Binding { get; set; } = null;
        public string? InstanceName { get; set; } = null;
        public List<BlockFieldModel> Fields { get; set; } = new List<BlockFieldModel>();
    }

    public class BlockFieldModel
    {
        public List<ShaderAttribute> Attributes { get; set; } = new List<ShaderAttribute>();
        public string Type { get; set; } = string.Empty;
        public string CSharpTypeName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? ArraySize { get; set; }
        public string Fulltext { get; set; } = string.Empty;
    }

    public enum LayoutType
    {
        Ordinary,
        STD140, 
        STD430, 
        Packed, 
        Shared, 
    }

}


/*
 
Тип layout	                Вершинный шейдер	Фрагментный шейдер	            Пример
location (входной in)	        ✅ Да	            ❌ Нет	            layout(location=0) in vec3 pos;
location (выходной out)	        ❌ Нет*	            ✅ Да	            layout(location=0) out vec4 color;
binding (uniform)	            ✅ Да	            ✅ Да	            layout(binding=0) uniform sampler2D tex;
set + binding (Vulkan)	        ✅ Да	            ✅ Да	            layout(set=0, binding=1) uniform…
std140/std430 (UBO/SSBO)	    ✅ Да	            ✅ Да	            layout(std140, binding=2) uniform Camera {…}
flat/noperspective	            ✅ Только out	    ✅ Только in	    layout(flat) out int id;
row_major/column_major	        ✅ Да	            ✅ Да	            layout(row_major) uniform Transform {…}

 */