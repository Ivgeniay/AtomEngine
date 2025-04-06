using System.Text;
using EngineLib;

namespace OpenglLib
{
    public static class ShaderCodeRepresentationGenerator
    {
        private const string CONTENT_PLACEHOLDER = "/*CONTENT*/";
        private const string INTERFACES_PLACEHOLDER = "/*INTERFACES*/";
        private const string FIELDS_PLACEHOLDER = "/*FIELDS*/";
        private const string CONSTRUCTOR_PLACEHOLDER = "/*CONSTRUCTOR*/";
        private const string CONSTRUCTOR_BODY_PLACEHOLDER = "/*CONSTRUCTOR_BODY*/";
        private const string DISPOSE_PLACEHOLDER = "/*DISPOSE*/";
        private const string DISPOSE_BODY_PLACEHOLDER = "/*DISPOSE_BODY*/";

        public static string GenerateRepresentationFromSource(string representationName, string vertexSource, string fragmentSource,
             string outputDirectory, List<UniformModel> uniforms, List<UniformBlockModel> uniformBlocks, List<RSFileInfo> rsFiles,
             GlslShaderModel shaderModel, string sourceGuid = null, string sourcePath = null)
        {
            UpdateUniformTypesFromRSFiles(uniforms, rsFiles);

            var representationCode = GenerateRepresentationClass(representationName, vertexSource, fragmentSource, uniforms, uniformBlocks, sourceGuid, rsFiles, shaderModel);
            var representationFilePath = Path.Combine(outputDirectory, $"{representationName}{GeneratorConst.LABLE}.cs");
            File.WriteAllText(representationFilePath, representationCode, Encoding.UTF8);

            return $"{representationName}Representation";
        }

        private static string GenerateRepresentationClass(string materialName, string vertexSource,
            string fragmentSource, List<UniformModel> uniforms, List<UniformBlockModel> uniformBlocks, 
            string sourceGuid, List<RSFileInfo> rsFiles, GlslShaderModel shaderModel)
        {
            var mainBuilder = new StringBuilder();
            var contentBuilder = new StringBuilder();
            var interfacesBuilder = new StringBuilder();
            var fieldsBuilder = new StringBuilder();
            var constructorBuilder = new StringBuilder();
            var constructorBodyBuilder = new StringBuilder();
            var disposeBuilder = new StringBuilder();
            var disposeBodyBuilder = new StringBuilder();

            bool isUseDispose = false;

            #region SamplerUniform
            var samplerBindings = new Dictionary<int, string>();
            int samplerCounter = 0;
            foreach (var uniform in uniforms)
            {
                if (GlslParser.IsSamplerType(uniform.Type) && uniform.Binding.HasValue)
                {
                    samplerBindings[uniform.Binding.Value] = uniform.Name;
                }
            }
            Func<UniformModel, int> getSamplerIndex = (uniform) => {
                if (uniform.Binding.HasValue)
                {
                    return uniform.Binding.Value;
                }
                while (samplerBindings.ContainsKey(samplerCounter))
                {
                    samplerCounter++;
                }
                samplerBindings[samplerCounter] = uniform.Name;
                return samplerCounter++;
            };
            #endregion

            GenerateClassStructure(mainBuilder, materialName, vertexSource, fragmentSource, sourceGuid);
            GenerateContentStructure(contentBuilder);
            GenerateConstructorStructure(constructorBuilder, materialName);
            GenerateDisposeStructure(disposeBuilder);

            foreach (var uniform in uniforms)
            {
                var type = uniform.Type;
                var name = uniform.Name;
                var arraySize = uniform.ArraySize;

                var csharpType = uniform.CSharpTypeName;
                bool isCustomType = GlslParser.IsCustomType(csharpType, type);
                string cashFieldName = $"_{name}";
                string locationName = $"{name}{ShaderConst.LOCATION}";
                int? binding = uniform.Binding;

                if (isCustomType)
                {
                    if (arraySize.HasValue)
                    {
                        CustomTypeArrayCase(fieldsBuilder, constructorBodyBuilder, csharpType, name, arraySize.Value);
                    }
                    else
                    {
                        CustomTypeCase(fieldsBuilder, constructorBodyBuilder, csharpType, name);
                    }
                }
                else
                {
                    if (arraySize.HasValue)
                    {
                        SimpleTypeArrayCase(fieldsBuilder, constructorBodyBuilder, csharpType, type, name, locationName, arraySize.Value, getSamplerIndex, uniform);
                    }
                    else
                    {
                        SimpleTypeCase(fieldsBuilder, constructorBodyBuilder, type, csharpType, name, locationName, getSamplerIndex, uniform);
                    }
                }
            }

            constructorBodyBuilder.AppendLine("            SetUpShader(VertexSource, FragmentSource);");
            constructorBodyBuilder.AppendLine("            SetupUniformLocations();");

            foreach (var block in uniformBlocks)
            {
                if (block.InstanceName != null && block.Binding != null)
                {
                    isUseDispose = true;
                    UniformBlockWithBindingCase(fieldsBuilder, constructorBodyBuilder, disposeBodyBuilder, block);
                }
            }

            foreach (var block in uniformBlocks)
            {
                if (block.InstanceName != null && block.Binding == null)
                {
                    isUseDispose = true;
                    UniformBlockWithoutBindingCase(fieldsBuilder, constructorBodyBuilder, disposeBodyBuilder, block);
                }
            }

            if (shaderModel.Vertex != null)
            {
                foreach (var structInstance in shaderModel.Vertex.StructureInstances)
                {
                    if (!structInstance.IsUniform)
                    {
                        string cashFieldName = $"_{structInstance.InstanceName}";
                        string csharpType = string.IsNullOrWhiteSpace(structInstance.Structure.CSharpTypeName)
                            ? GlslParser.MapGlslTypeToCSharp(structInstance.Structure.Name)
                            : structInstance.Structure.CSharpTypeName;

                        if (structInstance.ArraySize.HasValue)
                        {
                            int arraySize = structInstance.ArraySize.Value;

                            fieldsBuilder.AppendLine($"        private StructArray<{csharpType}> {cashFieldName};");
                            fieldsBuilder.AppendLine($"        public StructArray<{csharpType}> {structInstance.InstanceName}");
                            fieldsBuilder.AppendLine("        {");
                            fieldsBuilder.Append(GetSimpleGetter(cashFieldName));
                            fieldsBuilder.AppendLine("        }");
                            fieldsBuilder.AppendLine();
                            fieldsBuilder.AppendLine();

                            constructorBodyBuilder.AppendLine($"            {cashFieldName} = new StructArray<{csharpType}>({arraySize}, _gl);");
                        }
                        else
                        {
                            fieldsBuilder.AppendLine($"        private {csharpType} {cashFieldName};");
                            fieldsBuilder.AppendLine($"        public {csharpType} {structInstance.InstanceName}");
                            fieldsBuilder.AppendLine("        {");
                            fieldsBuilder.Append(GetSimpleGetter(cashFieldName));
                            fieldsBuilder.AppendLine("        }");
                            fieldsBuilder.AppendLine();
                            fieldsBuilder.AppendLine();

                            constructorBodyBuilder.AppendLine($"            {cashFieldName} = new {csharpType}(_gl);");
                        }
                    }
                }
            }

            if (shaderModel.Fragment != null)
            {
                foreach (var structInstance in shaderModel.Fragment.StructureInstances)
                {
                    if (!structInstance.IsUniform)
                    {
                        string cashFieldName = $"_{structInstance.InstanceName}";
                        if (fieldsBuilder.ToString().Contains($"public {structInstance.InstanceName}"))
                            continue;

                        string csharpType = string.IsNullOrWhiteSpace(structInstance.Structure.CSharpTypeName)
                            ? GlslParser.MapGlslTypeToCSharp(structInstance.Structure.Name)
                            : structInstance.Structure.CSharpTypeName;

                        if (structInstance.ArraySize.HasValue)
                        {
                            int arraySize = structInstance.ArraySize.Value;

                            fieldsBuilder.AppendLine($"        private StructArray<{csharpType}> {cashFieldName};");
                            fieldsBuilder.AppendLine($"        public StructArray<{csharpType}> {structInstance.InstanceName}");
                            fieldsBuilder.AppendLine("        {");
                            fieldsBuilder.Append(GetSimpleGetter(cashFieldName));
                            fieldsBuilder.AppendLine("        }");
                            fieldsBuilder.AppendLine();
                            fieldsBuilder.AppendLine();

                            constructorBodyBuilder.AppendLine($"            {cashFieldName} = new StructArray<{csharpType}>({arraySize}, _gl);");
                        }
                        else
                        {
                            fieldsBuilder.AppendLine($"        private {csharpType} {cashFieldName};");
                            fieldsBuilder.AppendLine($"        public {csharpType} {structInstance.InstanceName}");
                            fieldsBuilder.AppendLine("        {");
                            fieldsBuilder.Append(GetSimpleGetter(cashFieldName));
                            fieldsBuilder.AppendLine("        }");
                            fieldsBuilder.AppendLine();
                            fieldsBuilder.AppendLine();

                            constructorBodyBuilder.AppendLine($"            {cashFieldName} = new {csharpType}(_gl);");
                        }
                    }
                }
            }

            foreach (var rs in rsFiles)
            {
                interfacesBuilder.Append(", " + rs.InterfaceName);
            }

            string constructorText = constructorBuilder.ToString()
                .Replace(CONSTRUCTOR_BODY_PLACEHOLDER, constructorBodyBuilder.ToString().TrimEnd());

            string disposeText = "";
            if (isUseDispose)
            {
                disposeText = disposeBuilder.ToString()
                    .Replace(DISPOSE_BODY_PLACEHOLDER, disposeBodyBuilder.ToString().TrimEnd());
            }

            string contentText = contentBuilder.ToString()
                .Replace(FIELDS_PLACEHOLDER, fieldsBuilder.ToString())
                .Replace(CONSTRUCTOR_PLACEHOLDER, constructorText)
                .Replace(DISPOSE_PLACEHOLDER, disposeText);

            string result = mainBuilder.ToString()
                .Replace(CONTENT_PLACEHOLDER, contentText)
                .Replace(INTERFACES_PLACEHOLDER, interfacesBuilder.ToString());

            return result;
        }

        private static void GenerateClassStructure(StringBuilder builder, string materialName, string vertexSource, string fragmentSource, string sourceGuid)
        {
            GeneratorConst.WriteGeneratedCodeHeader(builder, sourceGuid);

            builder.AppendLine("using OpenglLib.Buffers;");
            builder.AppendLine("using Silk.NET.OpenGL;");
            builder.AppendLine($"{GeneratorConst.GetDefaultNamespaces()}");
            builder.AppendLine();
            builder.AppendLine($"{GeneratorConst.GetUserScriptNamespace()}");
            builder.AppendLine("{");
            builder.AppendLine($"    public partial class {materialName}Representation : Mat{INTERFACES_PLACEHOLDER}");
            builder.AppendLine("    {");
            builder.AppendLine($"        protected new string VertexSource = @\"{vertexSource.Replace("\"", "\"\"")}\";");
            builder.AppendLine($"        protected new string FragmentSource = @\"{fragmentSource.Replace("\"", "\"\"")}\";");
            builder.AppendLine();
            builder.AppendLine(CONTENT_PLACEHOLDER);
            builder.AppendLine("    }");
            builder.AppendLine("}");
        }

        private static void GenerateContentStructure(StringBuilder builder)
        {
            builder.AppendLine(FIELDS_PLACEHOLDER);
            builder.AppendLine(CONSTRUCTOR_PLACEHOLDER);
            builder.AppendLine(DISPOSE_PLACEHOLDER);
        }

        private static void GenerateConstructorStructure(StringBuilder builder, string materialName)
        {
            builder.AppendLine($"        public {materialName}Representation(GL gl) : base(gl)");
            builder.AppendLine("        {");
            builder.AppendLine(CONSTRUCTOR_BODY_PLACEHOLDER);
            builder.AppendLine("        }");
        }

        private static void GenerateDisposeStructure(StringBuilder builder)
        {
            builder.AppendLine("        public override void Dispose()");
            builder.AppendLine("        {");
            builder.AppendLine("            base.Dispose();");
            builder.AppendLine(DISPOSE_BODY_PLACEHOLDER);
            builder.AppendLine("        }");
        }

        private static void CustomTypeCase(StringBuilder fieldsBuilder, StringBuilder constructorBodyBuilder, string csharpType, string name)
        {
            string cashFieldName = $"_{name}";

            fieldsBuilder.AppendLine($"        private {csharpType} {cashFieldName};");
            fieldsBuilder.AppendLine($"        public {csharpType} {name}");
            fieldsBuilder.AppendLine("        {");
            fieldsBuilder.Append(GetSimpleGetter(cashFieldName));
            fieldsBuilder.AppendLine("        }");
            fieldsBuilder.AppendLine();
            fieldsBuilder.AppendLine();

            constructorBodyBuilder.AppendLine($"            {cashFieldName} = new {csharpType}(_gl);");
        }

        private static void CustomTypeArrayCase(StringBuilder fieldsBuilder, StringBuilder constructorBodyBuilder, string csharpType, string name, int arraySize)
        {
            string cashFieldName = $"_{name}";

            fieldsBuilder.AppendLine($"        private StructArray<{csharpType}> {cashFieldName};");
            fieldsBuilder.AppendLine($"        public StructArray<{csharpType}> {name}");
            fieldsBuilder.AppendLine("        {");
            fieldsBuilder.Append(GetSimpleGetter(cashFieldName));
            fieldsBuilder.AppendLine("        }");
            fieldsBuilder.AppendLine();
            fieldsBuilder.AppendLine();

            constructorBodyBuilder.AppendLine($"            {cashFieldName} = new StructArray<{csharpType}>({arraySize}, _gl);");
        }

        private static void SimpleTypeCase(StringBuilder fieldsBuilder, StringBuilder constructorBodyBuilder, string type, string csharpType, string name, string locationName, Func<UniformModel, int> getSamplerIndex, UniformModel uniform)
        {
            //if (GlslParser.IsSamplerType(type))
            //{
            //    int sampler = getSamplerIndex(uniform);
            //    fieldsBuilder.AppendLine($"        public void {name}_SetTexture(OpenglLib.Texture texture) => SetTexture(\"Texture{sampler}\", \"{GlslParser.GetTextureTarget(type)}\", {locationName}, {sampler}, texture);");
            //}

            string cashFieldName = $"_{name}";
            var _unsafe = type.StartsWith("mat") ? "unsafe " : "";

            fieldsBuilder.AppendLine($"        public int {locationName} " + "{" + " get ; protected set; } = -1;");

            if (GlslParser.IsSamplerType(type))
            {
                int samplerIndex = getSamplerIndex(uniform);

                fieldsBuilder.AppendLine($"        private OpenglLib.Texture _{name};");
                fieldsBuilder.AppendLine($"        public OpenglLib.Texture {name}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.AppendLine($"            get => _{name};");
                fieldsBuilder.AppendLine("            set");
                fieldsBuilder.AppendLine("            {");
                fieldsBuilder.AppendLine($"                if (value != null && {locationName} != -1)");
                fieldsBuilder.AppendLine($"                {{");
                fieldsBuilder.AppendLine($"                    _{name} = value;");
                fieldsBuilder.AppendLine($"                    SetTexture(\"Texture{samplerIndex}\", \"{GlslParser.GetTextureTarget(type)}\", {locationName}, {samplerIndex}, value);");
                fieldsBuilder.AppendLine($"                }}");
                fieldsBuilder.AppendLine("            }");
                fieldsBuilder.AppendLine("        }");
            }
            else
            {
                fieldsBuilder.AppendLine($"        private {csharpType} {cashFieldName};");
                fieldsBuilder.AppendLine($"        public {_unsafe}{csharpType} {name}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.Append(GetSetter(type, locationName, cashFieldName));
                fieldsBuilder.AppendLine("        }");
            }

            fieldsBuilder.AppendLine();
        }

        private static void SimpleTypeArrayCase(StringBuilder fieldsBuilder, StringBuilder constructorBodyBuilder, string csharpType, string type, string name, string locationName, int arraySize, Func<UniformModel, int> getSamplerIndex, UniformModel uniform)
        {
            string cashFieldName = $"_{name}";

            if (GlslParser.IsSamplerType(type))
            {
                int baseSamplerIndex = 0;

                for(int i = 0; i < arraySize; i++)
                {
                    if (i == 0) baseSamplerIndex = getSamplerIndex(uniform);
                    getSamplerIndex(uniform);
                }

                fieldsBuilder.AppendLine($"        private SamplerArray {cashFieldName};");
                fieldsBuilder.AppendLine($"        public SamplerArray {name}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.AppendLine($"            get => {cashFieldName};");
                fieldsBuilder.AppendLine("        }");
                fieldsBuilder.AppendLine();

                fieldsBuilder.AppendLine($"        public int {locationName}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.AppendLine($"            get => {name}.Location;");
                fieldsBuilder.AppendLine($"            set => {name}.Location = value;");
                fieldsBuilder.AppendLine("        }");

                constructorBodyBuilder.AppendLine($"            {cashFieldName} = new SamplerArray(_gl, {arraySize}, \"{GlslParser.GetTextureTarget(type)}\", {baseSamplerIndex});");
            }
            else
            {
                var localeProperty = GetPropertyForLocaleArray(csharpType, name, locationName);
                fieldsBuilder.Append(localeProperty);
                fieldsBuilder.AppendLine($"        private LocaleArray<{csharpType}> {cashFieldName};");
                fieldsBuilder.AppendLine($"        public LocaleArray<{csharpType}> {name}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.Append(GetSimpleGetter(cashFieldName));
                fieldsBuilder.AppendLine("        }");
                fieldsBuilder.AppendLine();
                fieldsBuilder.AppendLine();

                constructorBodyBuilder.AppendLine($"            {cashFieldName} = new LocaleArray<{csharpType}>({arraySize}, _gl);");
            }
        }

        private static void UniformBlockWithBindingCase(StringBuilder fieldsBuilder, StringBuilder constructorBodyBuilder, StringBuilder disposeBodyBuilder, UniformBlockModel block)
        {
            string refStruct = $"_{block.InstanceName}";

            fieldsBuilder.AppendLine($"        private UniformBufferObject<{block.CSharpTypeName}> {block.InstanceName}Ubo;");
            fieldsBuilder.AppendLine($"        private {block.CSharpTypeName} {refStruct} = new {block.CSharpTypeName}();");
            fieldsBuilder.AppendLine($"        public {block.CSharpTypeName} {block.InstanceName}");
            fieldsBuilder.AppendLine("        {");
            fieldsBuilder.AppendLine("            set");
            fieldsBuilder.AppendLine("            {");
            fieldsBuilder.AppendLine($"                {refStruct} = value;");
            fieldsBuilder.AppendLine($"                {block.InstanceName}Ubo.UpdateData(ref {refStruct});");
            fieldsBuilder.AppendLine("            }");
            fieldsBuilder.AppendLine("        }");
            fieldsBuilder.AppendLine();

            constructorBodyBuilder.AppendLine($"            {block.InstanceName}Ubo = new UniformBufferObject<{block.CSharpTypeName}>(_gl, ref {refStruct}, {ShaderConst.SHADER_PROGRAM}, {block.Binding.Value});");
            //constructorBodyBuilder.AppendLine($"            bindingService.AllocateBindingPoint(handle, {block.Binding.Value});");

            disposeBodyBuilder.AppendLine($"            {block.InstanceName}Ubo.Dispose();");
        }

        private static void UniformBlockWithoutBindingCase(StringBuilder fieldsBuilder, StringBuilder constructorBodyBuilder, StringBuilder disposeBodyBuilder, UniformBlockModel block)
        {
            string refStruct = $"_{block.InstanceName}";

            fieldsBuilder.AppendLine($"        private UniformBufferObject<{block.CSharpTypeName}> {block.InstanceName}Ubo;");
            fieldsBuilder.AppendLine($"        private {block.CSharpTypeName} {refStruct} = new {block.CSharpTypeName}();");
            fieldsBuilder.AppendLine($"        public {block.CSharpTypeName} {block.InstanceName}");
            fieldsBuilder.AppendLine("        {");
            fieldsBuilder.AppendLine("            set");
            fieldsBuilder.AppendLine("            {");
            fieldsBuilder.AppendLine($"                {refStruct} = value;");
            fieldsBuilder.AppendLine($"                if ({block.InstanceName}Ubo != null)");
            fieldsBuilder.AppendLine($"                    {block.InstanceName}Ubo.UpdateData(ref {refStruct});");
            fieldsBuilder.AppendLine("            }");
            fieldsBuilder.AppendLine("        }");
            fieldsBuilder.AppendLine();

            constructorBodyBuilder.AppendLine($"            {block.InstanceName}Ubo = new UniformBufferObject<{block.CSharpTypeName}>(_gl, ref {refStruct}, handle, GetBlockIndex(\"{block.Name}\"), \"{block.Name}\");");

            disposeBodyBuilder.AppendLine($"            {block.InstanceName}Ubo.Dispose();");
        }

        public static string GetSimpleGetter(string cashFieldName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("            get");
            builder.AppendLine("            {");
            builder.AppendLine($"                return {cashFieldName};");
            builder.AppendLine("            }");
            return builder.ToString();
        }

        public static string GetPropertyForLocaleArray(string type, string fieldName, string locationFieldName)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"        public int {locationFieldName}");
            builder.AppendLine($"        {{");
            builder.AppendLine($"             get => {fieldName}.{ShaderConst.LOCATION};");
            builder.AppendLine($"             set => {fieldName}.{ShaderConst.LOCATION} = value;");
            builder.AppendLine($"        }}");

            return builder.ToString();
        }

        public static string GetSetter(string type, string locationFieldName, string cashFieldName)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("            get");
            builder.AppendLine("            {");
            builder.AppendLine($"                return {cashFieldName};");
            builder.AppendLine("            }");
            builder.AppendLine("            set");
            builder.AppendLine("            {");
            builder.AppendLine($"                if ({locationFieldName} == -1)");
            builder.AppendLine($"                {{");
            builder.AppendLine($"                   return;");
            builder.AppendLine($"                }}");
            builder.AppendLine($"                {cashFieldName} = value;");

            switch (type)
            {
                case "bool":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value ? 1 : 0);");
                    break;
                case "int":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;
                case "uint":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;
                case "float":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;
                case "double":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;

                case "bvec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0);");
                    break;
                case "bvec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0, value.Z ? 1 : 0);");
                    break;
                case "bvec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0, value.Z ? 1 : 0, value.W ? 1 : 0);");
                    break;

                case "ivec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;
                case "ivec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;
                case "ivec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                case "uvec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;
                case "uvec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;
                case "uvec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                case "vec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, (float)value.X, (float)value.Y);");
                    break;
                case "vec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, (float)value.X, (float)value.Y, (float)value.Z);");
                    break;
                case "vec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, (float)value.X, (float)value.Y, (float)value.Z, (float)value.W);");
                    break;

                case "mat2":
                case "mat2x2":
                    builder.AppendLine($"                var mat2 = (Matrix2X2<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix2({locationFieldName}, 1, false, (float*)&mat2);");
                    break;

                case "mat2x3":
                    builder.AppendLine($"                var mat2x3 = (Matrix2X3<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix2x3({locationFieldName}, 1, false, (float*)&mat2x3);");
                    break;

                case "mat2x4":
                    builder.AppendLine($"                var mat2x4 = (Matrix2X4<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix2x4({locationFieldName}, 1, false, (float*)&mat2x4);");
                    break;

                case "mat3":
                case "mat3x3":
                    builder.AppendLine($"                var mat3 = (Matrix3X3<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix3({locationFieldName}, 1, false, (float*)&mat3);");
                    break;

                case "mat3x2":
                    builder.AppendLine($"                var mat3x2 = (Matrix3X2<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix3x2({locationFieldName}, 1, false, (float*)&mat3x2);");
                    break;

                case "mat3x4":
                    builder.AppendLine($"                var mat3x4 = (Matrix3X4<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix3x4({locationFieldName}, 1, false, (float*)&mat3x4);");
                    break;

                case "mat4":
                case "mat4x4":
                    builder.AppendLine($"                var mat4 = (Matrix4X4<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix4({locationFieldName}, 1, false, (float*)&mat4);");
                    break;

                case "mat4x2":
                    builder.AppendLine($"                var mat4x2 = (Matrix4X2<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix4x2({locationFieldName}, 1, false, (float*)&mat4x2);");
                    break;

                case "mat4x3":
                    builder.AppendLine($"                var mat4x3 = (Matrix4X3<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix4x3({locationFieldName}, 1, false, (float*)&mat4x3);");
                    break;

                case string s when s.StartsWith("sampler"):
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;
                case string s when s.StartsWith("isampler"):
                    builder.AppendLine($"                _gl.Uniform1i({locationFieldName}, value);");
                    break;
                case string s when s.StartsWith("usampler"):
                    builder.AppendLine($"                _gl.Uniform1ui({locationFieldName}, value);");
                    break;

                default:
                    builder.AppendLine($"                throw new NotSupportedException(\"Unsupported uniform type: {type}\");");
                    break;
            }

            builder.AppendLine("            }");
            return builder.ToString();
        }

        private static void UpdateUniformTypesFromRSFiles(List<UniformModel> uniforms, List<RSFileInfo> rsFiles)
        {
            var rsManager = ServiceHub.Get<RSManager>();
            foreach (var uniform in uniforms)
            {
                foreach (var rsFile in rsFiles)
                {
                    var matchingStruct = rsFile.Structures.FirstOrDefault(s => s.Name == uniform.Type);
                    if (matchingStruct != null)
                    {
                        var structInfo = rsManager.GetStructTypeInfo(matchingStruct.Name, rsFile.SourcePath);
                        if (structInfo != null)
                        {
                            uniform.CSharpTypeName = structInfo.GeneratedTypeName;
                            break;
                        }
                    }
                }
            }
        }

    }


    //public static class ShaderCodeRepresentationGenerator
    //{
    //    public static string GenerateRepresentationFromSource(string representationName, string vertexSource, string fragmentSource, 
    //        string outputDirectory, List<UniformBlockStructure> uniformBlocks, List<RSFileInfo> rsFiles, string sourceGuid = null, 
    //        string sourcePath = null)
    //    {
    //        if (string.IsNullOrEmpty(sourceGuid) && !string.IsNullOrEmpty(sourcePath))
    //            sourceGuid = ServiceHub.Get<EditorMetadataManager>().GetMetadata(sourcePath)?.Guid;

    //        GlslParser.ValidateMainFunctions(vertexSource, fragmentSource);
    //        var uniforms = GlslParser.ExtractUniforms(vertexSource + "\n" + fragmentSource);
    //        var representationCode = GenerateRepresentationClass(representationName, vertexSource, fragmentSource, uniforms, uniformBlocks, sourceGuid, rsFiles);
    //        var representationFilePath = Path.Combine(outputDirectory, $"{representationName}{GlslCodeGenerator.LABLE}.cs");
    //        File.WriteAllText(representationFilePath, representationCode, Encoding.UTF8);

    //        return $"{representationName}Representation";
    //    }

    //    private static string GenerateRepresentationClass(string materialName, string vertexSource,
    //        string fragmentSource, List<(string type, string name, int? arraySize)> uniforms,
    //        List<UniformBlockStructure> uniformBlocks, string sourceGuid, List<RSFileInfo> rsFiles)
    //    {
    //        StringBuilder builder = new StringBuilder();
    //        StringBuilder interfaces = new StringBuilder();
    //        StringBuilder construcBuilder = new StringBuilder();
    //        StringBuilder disposeBuilder = new StringBuilder();
    //        List<string> constructor_lines = new List<string>();
    //        int samplers = 0;

    //        bool isUseDispose = false;

    //        disposeBuilder.AppendLine("        public override void Dispose()");
    //        disposeBuilder.AppendLine("        {");
    //        disposeBuilder.AppendLine("            base.Dispose();");

    //        GeneratorConst.WriteGeneratedCodeHeader(builder, sourceGuid);

    //        builder.AppendLine("using OpenglLib.Buffers;");
    //        builder.AppendLine("using Silk.NET.OpenGL;");
    //        builder.AppendLine($"{GeneratorConst.GetDefaultNamespaces()}");
    //        builder.AppendLine();
    //        builder.AppendLine($"{GeneratorConst.GetUserScriptNamespace()}");
    //        builder.AppendLine("{");
    //        builder.AppendLine($"    public partial class {materialName}Representation : Mat*interfaces*");
    //        builder.AppendLine("    {");
    //        builder.AppendLine($"        protected new string VertexSource = @\"{vertexSource.Replace("\"", "\"\"")}\";");
    //        builder.AppendLine($"        protected new string FragmentSource = @\"{fragmentSource.Replace("\"", "\"\"")}\";");

    //        builder.AppendLine("*construct*");
    //        construcBuilder.AppendLine($"        public {materialName}Representation(GL gl) : base(gl)");
    //        construcBuilder.AppendLine("        {");

    //        builder.AppendLine("");

    //        foreach (var (type, name, arraySize) in uniforms)
    //        {
    //            string csharpType = GlslParser.MapGlslTypeToCSharp(type);
    //            bool isCustomType = GlslParser.IsCustomType(csharpType, type);

    //            string cashFieldName = $"_{name}";
    //            string locationName = $"{name}{ShaderConst.LOCATION}";
    //            var _unsafe = type.StartsWith("mat") ? "unsafe " : "";

    //            if (isCustomType)
    //            {
    //                if (arraySize.HasValue)
    //                {
    //                    builder.AppendLine($"        private StructArray<{csharpType}> {cashFieldName};");
    //                    builder.AppendLine($"        public StructArray<{csharpType}> {name}");
    //                    builder.AppendLine("        {");
    //                    builder.Append(GetSimpleGetter(cashFieldName));
    //                    builder.AppendLine("        }");
    //                    constructor_lines.Add($"            {cashFieldName}  = new StructArray<{csharpType}>({arraySize.Value}, _gl);");
    //                }
    //                else
    //                {
    //                    builder.AppendLine($"        private {csharpType} {cashFieldName};");
    //                    builder.AppendLine($"        public {csharpType} {name}");
    //                    builder.AppendLine("        {");
    //                    builder.Append(GetSimpleGetter(cashFieldName));
    //                    builder.AppendLine("        }");
    //                    constructor_lines.Add($"            {cashFieldName} = new {csharpType}(_gl);");
    //                }
    //            }
    //            else
    //            {
    //                if (arraySize.HasValue)
    //                {
    //                    var localeProperty = GetPropertyForLocaleArray(csharpType, name, locationName);
    //                    builder.Append(localeProperty);
    //                    builder.AppendLine($"        private LocaleArray<{csharpType}> {cashFieldName};");
    //                    builder.AppendLine($"        public LocaleArray<{csharpType}> {name}");
    //                    builder.AppendLine("        {");
    //                    builder.Append(GetSimpleGetter(cashFieldName));
    //                    builder.AppendLine("        }");
    //                    constructor_lines.Add($"            {cashFieldName}  = new LocaleArray<{csharpType}>({arraySize.Value}, _gl);");
    //                }
    //                else
    //                {
    //                    if (type.IndexOf("sampler") != -1)
    //                    {
    //                        builder.AppendLine($"        public void {name}_SetTexture(OpenglLib.Texture texture) => SetTexture(\"Texture{samplers}\", \"{GlslParser.GetTextureTarget(type)}\", {locationName}, {samplers++}, texture);");
    //                    }
    //                    builder.AppendLine($"        public int {locationName} " + "{" + " get ; protected set; } = -1;");
    //                    builder.AppendLine($"        private {csharpType} {cashFieldName};");
    //                    builder.AppendLine($"        public {_unsafe}{csharpType} {name}");
    //                    builder.AppendLine("        {");
    //                    builder.Append(GetSetter(type, locationName, cashFieldName));
    //                    builder.AppendLine("        }");
    //                }
    //            }
    //            builder.AppendLine("");
    //            builder.AppendLine("");
    //        }

    //        if (constructor_lines.Count > 0)
    //        {
    //            foreach (var line in constructor_lines)
    //            {
    //                construcBuilder.AppendLine(line);
    //            }
    //        }
    //        construcBuilder.AppendLine("            SetUpShader(VertexSource, FragmentSource);");
    //        construcBuilder.AppendLine("            SetupUniformLocations();");

    //        foreach (var block in uniformBlocks)
    //        {
    //            if (block.InstanceName != null && block.Binding != null)
    //            {
    //                isUseDispose = true;

    //                string refStruct = $"_{block.InstanceName}";
    //                builder.AppendLine($"        private UniformBufferObject<{block.CSharpTypeName}> {block.InstanceName}Ubo;");
    //                construcBuilder.AppendLine($"            {block.InstanceName}Ubo = new UniformBufferObject<{block.CSharpTypeName}>(_gl, ref {refStruct}, {ShaderConst.SHADER_PROGRAM}, {block.Binding.Value});");

    //                builder.AppendLine($"        private {block.CSharpTypeName} {refStruct} = new {block.CSharpTypeName}();");
    //                builder.AppendLine($"        public {block.CSharpTypeName} {block.InstanceName}");
    //                builder.AppendLine("        {");
    //                builder.AppendLine("            set");
    //                builder.AppendLine("            {");
    //                builder.AppendLine($"                {refStruct} = value;");
    //                builder.AppendLine($"                {block.InstanceName}Ubo.UpdateData(ref {refStruct});");
    //                builder.AppendLine("            }");
    //                builder.AppendLine("        }");

    //                builder.AppendLine("");

    //                disposeBuilder.AppendLine($"            {block.InstanceName}Ubo.Dispose();");
    //            }
    //        }

    //        foreach (var block in uniformBlocks)
    //        {
    //            if (block.InstanceName != null && block.Binding == null)
    //            {
    //                isUseDispose = true;

    //                string refStruct = $"_{block.InstanceName}";
    //                construcBuilder.AppendLine($"            {block.InstanceName}Ubo = new UniformBufferObject<{block.CSharpTypeName}>(_gl, ref {refStruct}, GetBlockIndex(\"{block.Name}\"));");

    //                builder.AppendLine($"        private UniformBufferObject<{block.CSharpTypeName}> {block.InstanceName}Ubo;");
    //                builder.AppendLine($"        private {block.CSharpTypeName} {refStruct} = new {block.CSharpTypeName}();");
    //                builder.AppendLine($"        public {block.CSharpTypeName} {block.InstanceName}");
    //                builder.AppendLine("        {");
    //                builder.AppendLine("            set");
    //                builder.AppendLine("            {");
    //                builder.AppendLine($"                {refStruct} = value;");
    //                builder.AppendLine($"                if ({block.InstanceName}Ubo != null)");
    //                builder.AppendLine($"                    {block.InstanceName}Ubo.UpdateData(ref {refStruct});");
    //                builder.AppendLine("            }");
    //                builder.AppendLine("        }");

    //                disposeBuilder.AppendLine($"            {block.InstanceName}Ubo.Dispose();");
    //            }
    //        }


    //        if (isUseDispose)
    //        {
    //            disposeBuilder.AppendLine("        }");
    //            builder.AppendLine(disposeBuilder.ToString());
    //        }

    //        builder.AppendLine("    }");
    //        builder.AppendLine("}");


    //        construcBuilder.AppendLine("        }");
    //        builder.Replace("*construct*", construcBuilder.ToString());

    //        foreach(var rs in rsFiles)
    //            interfaces.Append(", " + rs.InterfaceName);
    //        builder.Replace("*interfaces*", interfaces.ToString());

    //        return builder.ToString();
    //    }


    //    public static string GetSimpleGetter(string cashFieldName)
    //    {
    //        StringBuilder builder = new StringBuilder();
    //        builder.AppendLine("            get");
    //        builder.AppendLine("            {");
    //        builder.AppendLine($"                return {cashFieldName};");
    //        builder.AppendLine("            }");
    //        return builder.ToString();
    //    }
    //    public static string GetPropertyForLocaleArray(string type, string fieldName, string locationFieldName)
    //    {
    //        StringBuilder builder = new StringBuilder();

    //        builder.AppendLine($"        public int {locationFieldName}");
    //        builder.AppendLine($"        {{");
    //        builder.AppendLine($"             get => {fieldName}.{ShaderConst.LOCATION};");
    //        builder.AppendLine($"             set => {fieldName}.{ShaderConst.LOCATION} = value;");
    //        builder.AppendLine($"        }}");

    //        return builder.ToString();
    //    }
    //    public static string GetSetter(string type, string locationFieldName, string cashFieldName)
    //    {
    //        StringBuilder builder = new StringBuilder();

    //        builder.AppendLine("            get");
    //        builder.AppendLine("            {");
    //        builder.AppendLine($"                return {cashFieldName};");
    //        builder.AppendLine("            }");
    //        builder.AppendLine("            set");
    //        builder.AppendLine("            {");
    //        builder.AppendLine($"                if ({locationFieldName} == -1)");
    //        builder.AppendLine($"                {{");
    //        builder.AppendLine($"                   return;");
    //        builder.AppendLine($"                }}");
    //        builder.AppendLine($"                {cashFieldName} = value;");

    //        switch (type)
    //        {
    //            case "bool":
    //                builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value ? 1 : 0);");
    //                break;
    //            case "int":
    //                builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
    //                break;
    //            case "uint":
    //                builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
    //                break;
    //            case "float":
    //                builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
    //                break;
    //            case "double":
    //                builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
    //                break;

    //            case "bvec2":
    //                builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0);");
    //                break;
    //            case "bvec3":
    //                builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0, value.Z ? 1 : 0);");
    //                break;
    //            case "bvec4":
    //                builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0, value.Z ? 1 : 0, value.W ? 1 : 0);");
    //                break;

    //            case "ivec2":
    //                builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X, value.Y);");
    //                break;
    //            case "ivec3":
    //                builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
    //                break;
    //            case "ivec4":
    //                builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
    //                break;

    //            case "uvec2":
    //                builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X, value.Y);");
    //                break;
    //            case "uvec3":
    //                builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
    //                break;
    //            case "uvec4":
    //                builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
    //                break;

    //            case "vec2":
    //                builder.AppendLine($"                _gl.Uniform2({locationFieldName}, (float)value.X, (float)value.Y);");
    //                break;
    //            case "vec3":
    //                builder.AppendLine($"                _gl.Uniform3({locationFieldName}, (float)value.X, (float)value.Y, (float)value.Z);");
    //                break;
    //            case "vec4":
    //                builder.AppendLine($"                _gl.Uniform4({locationFieldName}, (float)value.X, (float)value.Y, (float)value.Z, (float)value.W);");
    //                break;

    //            case "mat2":
    //            case "mat2x2":
    //                builder.AppendLine($"                var mat2 = (Matrix2X2<float>)value;");
    //                builder.AppendLine($"                _gl.UniformMatrix2({locationFieldName}, 1, false, (float*)&mat2);");
    //                break;

    //            case "mat2x3":
    //                builder.AppendLine($"                var mat2x3 = (Matrix2X3<float>)value;");
    //                builder.AppendLine($"                _gl.UniformMatrix2x3({locationFieldName}, 1, false, (float*)&mat2x3);");
    //                break;

    //            case "mat2x4":
    //                builder.AppendLine($"                var mat2x4 = (Matrix2X4<float>)value;");
    //                builder.AppendLine($"                _gl.UniformMatrix2x4({locationFieldName}, 1, false, (float*)&mat2x4);");
    //                break;

    //            case "mat3":
    //            case "mat3x3":
    //                builder.AppendLine($"                var mat3 = (Matrix3X3<float>)value;");
    //                builder.AppendLine($"                _gl.UniformMatrix3({locationFieldName}, 1, false, (float*)&mat3);");
    //                break;

    //            case "mat3x2":
    //                builder.AppendLine($"                var mat3x2 = (Matrix3X2<float>)value;");
    //                builder.AppendLine($"                _gl.UniformMatrix3x2({locationFieldName}, 1, false, (float*)&mat3x2);");
    //                break;

    //            case "mat3x4":
    //                builder.AppendLine($"                var mat3x4 = (Matrix3X4<float>)value;");
    //                builder.AppendLine($"                _gl.UniformMatrix3x4({locationFieldName}, 1, false, (float*)&mat3x4);");
    //                break;

    //            case "mat4":
    //            case "mat4x4":
    //                builder.AppendLine($"                var mat4 = (Matrix4X4<float>)value;");
    //                builder.AppendLine($"                _gl.UniformMatrix4({locationFieldName}, 1, false, (float*)&mat4);");
    //                break;

    //            case "mat4x2":
    //                builder.AppendLine($"                var mat4x2 = (Matrix4X2<float>)value;");
    //                builder.AppendLine($"                _gl.UniformMatrix4x2({locationFieldName}, 1, false, (float*)&mat4x2);");
    //                break;

    //            case "mat4x3":
    //                builder.AppendLine($"                var mat4x3 = (Matrix4X3<float>)value;");
    //                builder.AppendLine($"                _gl.UniformMatrix4x3({locationFieldName}, 1, false, (float*)&mat4x3);");
    //                break;

    //            case string s when s.StartsWith("sampler"):
    //                builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
    //                break;
    //            case string s when s.StartsWith("isampler"):
    //                builder.AppendLine($"                _gl.Uniform1i({locationFieldName}, value);");
    //                break;
    //            case string s when s.StartsWith("usampler"):
    //                builder.AppendLine($"                _gl.Uniform1ui({locationFieldName}, value);");
    //                break;

    //            default:
    //                builder.AppendLine($"                throw new NotSupportedException(\"Unsupported uniform type: {type}\");");
    //                break;
    //        }

    //        builder.AppendLine("            }");
    //        return builder.ToString();
    //    }

    //}

}