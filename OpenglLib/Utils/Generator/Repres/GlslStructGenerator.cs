using System.Text;

namespace OpenglLib
{
    internal static class GlslStructGenerator
    {
        private const string CONTENT_PLACEHOLDER = "/*CONTENT*/";
        private const string FIELDS_PLACEHOLDER = "/*FIELDS*/";
        private const string CONSTRUCTOR_PLACEHOLDER = "/*CONSTRUCTOR*/";
        private const string CONSTRUCTOR_BODY_PLACEHOLDER = "/*CONSTRUCTOR_BODY*/";
        private const string ISDIRTY_PLACEHOLDER = "/*ISDIRTY_OVERRIDE*/";
        private const string SETCLEAN_PLACEHOLDER = "/*SETCLEAN_OVERRIDE*/";

        private static HashSet<string> _generatedTypes = new HashSet<string>();

        public static string GenerateModelClass(GlslStructModel structure, string sourceStructGuid = null)
        {
            if (string.IsNullOrEmpty(structure.TypeMappingId))
            {
                structure.TypeMappingId = GlslTypeMapper.TryFindStructureGuid(structure.Name, sourceStructGuid);
            }
            string generatedTypeName = structure.CSharpTypeName;
            if (string.IsNullOrEmpty(generatedTypeName))
            {
                generatedTypeName = structure.Name;
            }
            GlslTypeMapper.RegisterTypeMapping(structure.TypeMappingId, structure.Name, generatedTypeName);
            foreach (var field in structure.Fields)
            {
                if (GlslParser.IsCustomType(field.Type, field.Type))
                {
                    if (string.IsNullOrEmpty(field.TypeMappingId))
                    {
                        field.TypeMappingId = GlslTypeMapper.TryFindStructureGuid(field.Type, sourceStructGuid);
                    }
                    if (!string.IsNullOrEmpty(field.TypeMappingId))
                    {
                        string mappedTypeName = GlslTypeMapper.GetTypeNameByGuid(field.TypeMappingId);
                        if (!string.IsNullOrEmpty(mappedTypeName))
                        {
                            field.CSharpTypeName = mappedTypeName;
                        }
                    }
                }
                else
                {
                    field.CSharpTypeName = GlslParser.MapGlslTypeToCSharp(field.Type, _generatedTypes);
                }
            }

            var mainBuilder = new StringBuilder();
            var contentBuilder = new StringBuilder();
            var fieldsBuilder = new StringBuilder();
            var constructorBuilder = new StringBuilder();
            var constructorBodyBuilder = new StringBuilder();
            var isDirtyGetterBuilder = new StringBuilder();
            var isDirtySetterBuilder = new StringBuilder();
            var setCleanBuilder = new StringBuilder();

            GenerateClassStructure(mainBuilder, structure.Name, sourceStructGuid);
            GenerateContentStructure(contentBuilder);
            GenerateConstructor(constructorBuilder, structure.Name);

            foreach (var field in structure.Fields)
            {
                var type = field.Type;
                var name = field.Name;
                var arraySize = field.ArraySize;

                string csharpType = !string.IsNullOrEmpty(field.CSharpTypeName)
                    ? field.CSharpTypeName
                    : GlslParser.MapGlslTypeToCSharp(field.Type, _generatedTypes);

                bool isCustomType = GlslParser.IsCustomType(csharpType, type);

                if (arraySize.HasValue)
                {
                    if (isCustomType)
                    {
                        CustomTypeArrayCase(fieldsBuilder, constructorBodyBuilder, isDirtyGetterBuilder, isDirtySetterBuilder, setCleanBuilder, csharpType, name, arraySize.Value);
                    }
                    else
                    {
                        SimpleTypeArrayCase(fieldsBuilder, constructorBodyBuilder, isDirtyGetterBuilder, isDirtySetterBuilder, setCleanBuilder, csharpType, type, name, arraySize.Value);
                    }
                }
                else
                {
                    if (isCustomType)
                    {
                        CustomTypeCase(fieldsBuilder, constructorBodyBuilder, isDirtyGetterBuilder, isDirtySetterBuilder, setCleanBuilder, csharpType, name);
                    }
                    else
                    {
                        SimpleTypeCase(fieldsBuilder, constructorBodyBuilder, csharpType, type, name);
                    }
                }
            }

            bool needsOverride = isDirtyGetterBuilder.Length > 0 || isDirtySetterBuilder.Length > 0;

            string isDirtyOverride = "";
            if (needsOverride)
            {
                var isDirtyBuilder = new StringBuilder();
                isDirtyBuilder.AppendLine("        public override bool IsDirty");
                isDirtyBuilder.AppendLine("        {");
                isDirtyBuilder.AppendLine("            get");
                isDirtyBuilder.AppendLine("            {");
                isDirtyBuilder.AppendLine("                if (_isDirty) return true;");
                isDirtyBuilder.Append(isDirtyGetterBuilder.ToString());
                isDirtyBuilder.AppendLine("                return false;");
                isDirtyBuilder.AppendLine("            }");
                isDirtyBuilder.AppendLine("            set");
                isDirtyBuilder.AppendLine("            {");
                isDirtyBuilder.AppendLine("                _isDirty = value;");
                isDirtyBuilder.Append(isDirtySetterBuilder.ToString());
                isDirtyBuilder.AppendLine("            }");
                isDirtyBuilder.AppendLine("        }");

                isDirtyOverride = isDirtyBuilder.ToString();
            }

            string setCleanOverride = "";
            if (setCleanBuilder.Length > 0)
            {
                var cleanBuilder = new StringBuilder();
                cleanBuilder.AppendLine("        public override void SetClean()");
                cleanBuilder.AppendLine("        {");
                cleanBuilder.AppendLine("            _isDirty = false;");
                cleanBuilder.Append(setCleanBuilder.ToString());
                cleanBuilder.AppendLine("        }");

                setCleanOverride = cleanBuilder.ToString();
            }

            //string constructorText = constructorBuilder.ToString()
            //    .Replace(CONSTRUCTOR_BODY_PLACEHOLDER, constructorBodyBuilder.ToString().TrimEnd());

            //string contentText = contentBuilder.ToString()
            //    .Replace(FIELDS_PLACEHOLDER, fieldsBuilder.ToString())
            //    .Replace(CONSTRUCTOR_PLACEHOLDER, constructorText);

            //string result = mainBuilder.ToString().Replace(CONTENT_PLACEHOLDER, contentText);

            //return result;

            string contentText = contentBuilder.ToString()
                .Replace(FIELDS_PLACEHOLDER, fieldsBuilder.ToString())
                .Replace(CONSTRUCTOR_PLACEHOLDER, constructorBuilder.ToString().Replace(CONSTRUCTOR_BODY_PLACEHOLDER, constructorBodyBuilder.ToString().TrimEnd()));

            string result = mainBuilder.ToString()
                .Replace(CONTENT_PLACEHOLDER, contentText)
                .Replace(ISDIRTY_PLACEHOLDER, isDirtyOverride)
                .Replace(SETCLEAN_PLACEHOLDER, setCleanOverride);

            return result;
        }

        private static void GenerateClassStructure(StringBuilder builder, string className, string sourceGuid = null)
        {
            if (sourceGuid != null) GeneratorConst.WriteGeneratedCodeHeader(builder, sourceGuid);

            builder.AppendLine($"{GeneratorConst.GetDefaultNamespaces()}");
            builder.AppendLine();
            builder.AppendLine($"{GeneratorConst.GetUserScriptNamespace()}");
            builder.AppendLine("{");
            builder.AppendLine($"    public class {className} : CustomStruct, IDataSerializable");
            builder.AppendLine("    {");
            builder.AppendLine(CONTENT_PLACEHOLDER);
            builder.AppendLine(ISDIRTY_PLACEHOLDER);
            builder.AppendLine(SETCLEAN_PLACEHOLDER);
            builder.AppendLine("    }");
            builder.AppendLine("}");
        }

        private static void GenerateContentStructure(StringBuilder builder)
        {
            builder.AppendLine(FIELDS_PLACEHOLDER);
            builder.AppendLine(CONSTRUCTOR_PLACEHOLDER);
        }

        private static void GenerateConstructor(StringBuilder builder, string className)
        {
            builder.AppendLine($"        public {className}(Silk.NET.OpenGL.GL gl, OpenglLib.Mat shader) : base(gl, shader)");
            builder.AppendLine("        {");
            builder.AppendLine(CONSTRUCTOR_BODY_PLACEHOLDER);
            builder.AppendLine("        }");
        }

        private static void SimpleTypeCase(StringBuilder fieldsBuilder, StringBuilder constructorBodyBuilder, string csharpType, string glslType, string name)
        {
            string cashFieldName = $"_{name}";
            string locationName = $"{name}Location";
            fieldsBuilder.AppendLine($"        public int {locationName} " + "{" + " get ; set; } = -1;");

            if (GlslParser.IsSamplerType(glslType))
            {
                fieldsBuilder.AppendLine($"        private OpenglLib.Texture _{name};");
                fieldsBuilder.AppendLine($"        public OpenglLib.Texture {name}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.AppendLine($"            get => _{name};");
                fieldsBuilder.AppendLine("            set");
                fieldsBuilder.AppendLine("            {");
                fieldsBuilder.AppendLine($"                if (value != null && {locationName} != -1)");
                fieldsBuilder.AppendLine($"                {{");
                fieldsBuilder.AppendLine($"                    _{name} = value;");
                fieldsBuilder.AppendLine($"                    _shader?.SetTexture({locationName}, value);");
                fieldsBuilder.AppendLine($"                }}");
                fieldsBuilder.AppendLine("            }");
                fieldsBuilder.AppendLine("        }");
            }
            else
            {
                var _unsafe = glslType.StartsWith("mat") ? "unsafe " : "";

                fieldsBuilder.AppendLine($"        private {csharpType} {cashFieldName};");

                fieldsBuilder.AppendLine($"        public {_unsafe}{csharpType} {name}");
                fieldsBuilder.AppendLine("        {");

                fieldsBuilder.Append(GetSetter(glslType, locationName, cashFieldName));
                fieldsBuilder.AppendLine("        }");
                fieldsBuilder.AppendLine();
                fieldsBuilder.AppendLine();
            }

        }

        private static void SimpleTypeArrayCase(StringBuilder fieldsBuilder, StringBuilder constructorBodyBuilder, StringBuilder isDirtyGetterBuilder, StringBuilder isDirtySetterBuilder, StringBuilder setCleanBuilder, string csharpType, string glslType, string name, int arraySize)
        {
            string cashFieldName = $"_{name}";
            string locationName = $"{name}Location";
            if (GlslParser.IsSamplerType(glslType))
            {
                int baseSamplerIndex = 0;

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

                constructorBodyBuilder.AppendLine($"            {cashFieldName} = new SamplerArray(_gl, {arraySize}, _shader);");
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

                constructorBodyBuilder.AppendLine($"            {cashFieldName}  = new LocaleArray<{csharpType}>({arraySize}, _gl);");

                isDirtyGetterBuilder.AppendLine($"                if ({cashFieldName} != null && {cashFieldName}.IsDirty) return true;");
                isDirtySetterBuilder.AppendLine($"                if ({cashFieldName} != null) {cashFieldName}.IsDirty = value;");
                setCleanBuilder.AppendLine($"            if ({cashFieldName} != null) {cashFieldName}.SetClean();");
            }
        }

        private static void CustomTypeCase(StringBuilder fieldsBuilder, StringBuilder constructorBodyBuilder, StringBuilder isDirtyGetterBuilder, StringBuilder isDirtySetterBuilder, StringBuilder setCleanBuilder, string csharpType, string name)
        {
            string cashFieldName = $"_{name}";
            string _unsafe = "";

            fieldsBuilder.AppendLine($"        private {csharpType} {cashFieldName};");

            fieldsBuilder.AppendLine($"        public {_unsafe}{csharpType} {name}");
            fieldsBuilder.AppendLine("        {");
            fieldsBuilder.Append(GetSimpleGetter(cashFieldName));
            fieldsBuilder.AppendLine("        }");
            fieldsBuilder.AppendLine();

            constructorBodyBuilder.AppendLine($"            {cashFieldName} = new {csharpType}(_gl, _shader);");

            isDirtyGetterBuilder.AppendLine($"                if ({cashFieldName} != null && {cashFieldName}.IsDirty) return true;");
            isDirtySetterBuilder.AppendLine($"                if ({cashFieldName} != null) {cashFieldName}.IsDirty = value;");
            setCleanBuilder.AppendLine($"            if ({cashFieldName} != null) {cashFieldName}.SetClean();");
        }

        private static void CustomTypeArrayCase(StringBuilder fieldsBuilder, StringBuilder constructorBodyBuilder, StringBuilder isDirtyGetterBuilder, StringBuilder isDirtySetterBuilder, StringBuilder setCleanBuilder, string csharpType, string name, int arraySize)
        {
            string cashFieldName = $"_{name}";

            fieldsBuilder.AppendLine($"        private StructArray<{csharpType}> {cashFieldName};");

            fieldsBuilder.AppendLine($"        public StructArray<{csharpType}> {name}");
            fieldsBuilder.AppendLine("        {");
            fieldsBuilder.Append(GetSimpleGetter(cashFieldName));
            fieldsBuilder.AppendLine("        }");
            fieldsBuilder.AppendLine();

            constructorBodyBuilder.AppendLine($"            {cashFieldName}  = new StructArray<{csharpType}>({arraySize}, _gl, _shader);");

            isDirtyGetterBuilder.AppendLine($"                if ({cashFieldName} != null && {cashFieldName}.IsDirty) return true;");
            isDirtySetterBuilder.AppendLine($"                if ({cashFieldName} != null) {cashFieldName}.IsDirty = value;");
            setCleanBuilder.AppendLine($"            if ({cashFieldName} != null) {cashFieldName}.SetClean();");
        }

        private static string GetSimpleGetter(string cashFieldName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("            get");
            builder.AppendLine("            {");
            builder.AppendLine($"                return {cashFieldName};");
            builder.AppendLine("            }");
            return builder.ToString();
        }

        private static string GetPropertyForLocaleArray(string type, string fieldName, string locationFieldName)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"        public int {locationFieldName}");
            builder.AppendLine($"        {{");
            builder.AppendLine($"             get => {fieldName}.Location;");
            builder.AppendLine($"             set => {fieldName}.Location = value;");
            builder.AppendLine($"        }}");

            return builder.ToString();
        }

        private static string GetSetter(string type, string locationFieldName, string cashFieldName)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("            get");
            builder.AppendLine("            {");
            builder.AppendLine($"                return {cashFieldName};");
            builder.AppendLine("            }");
            builder.AppendLine("            set");
            builder.AppendLine("            {");
            builder.AppendLine($"                if ({locationFieldName} == -1 && _gl != null)");
            builder.AppendLine($"                {{");
            //builder.AppendLine($"                   DebLogger.Warn(\"You try to set value to -1 lcation field\");");
            builder.AppendLine($"                   return;");
            builder.AppendLine($"                }}");
            builder.AppendLine($"                {cashFieldName} = value;");
            builder.AppendLine($"                _isDirty = true;");

            switch (type)
            {
                case "bool":
                    builder.AppendLine($"                _gl?.Uniform1({locationFieldName}, value ? 1 : 0);");
                    break;
                case "int":
                    builder.AppendLine($"                _gl?.Uniform1({locationFieldName}, value);");
                    break;
                case "uint":
                    builder.AppendLine($"                _gl?.Uniform1({locationFieldName}, value);");
                    break;
                case "float":
                    builder.AppendLine($"                _gl?.Uniform1({locationFieldName}, value);");
                    break;
                case "double":
                    builder.AppendLine($"                _gl?.Uniform1({locationFieldName}, value);");
                    break;

                case "bvec2":
                    builder.AppendLine($"                _gl?.Uniform2({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0);");
                    break;
                case "bvec3":
                    builder.AppendLine($"                _gl?.Uniform3({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0, value.Z ? 1 : 0);");
                    break;
                case "bvec4":
                    builder.AppendLine($"                _gl?.Uniform4({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0, value.Z ? 1 : 0, value.W ? 1 : 0);");
                    break;

                case "ivec2":
                    builder.AppendLine($"                _gl?.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;
                case "ivec3":
                    builder.AppendLine($"                _gl?.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;
                case "ivec4":
                    builder.AppendLine($"                _gl?.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                case "uvec2":
                    builder.AppendLine($"                _gl?.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;                                  
                case "uvec3":                               
                    builder.AppendLine($"                _gl?.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;                                  
                case "uvec4":                               
                    builder.AppendLine($"                _gl?.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                case "vec2":
                    builder.AppendLine($"                _gl?.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;
                case "vec3":
                    builder.AppendLine($"                _gl?.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;
                case "vec4":
                    builder.AppendLine($"                _gl?.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                case "mat2":
                case "mat2x2":
                    builder.AppendLine($"                var mat2 = (Matrix2X2<float>)value;");
                    builder.AppendLine($"                _gl?.UniformMatrix2({locationFieldName}, 1, false, (float*)&mat2);");
                    break;

                case "mat2x3":
                    builder.AppendLine($"                var mat2x3 = (Matrix2X3<float>)value;");
                    builder.AppendLine($"                _gl?.UniformMatrix2x3({locationFieldName}, 1, false, (float*)&mat2x3);");
                    break;

                case "mat2x4":
                    builder.AppendLine($"                var mat2x4 = (Matrix2X4<float>)value;");
                    builder.AppendLine($"                _gl?.UniformMatrix2x4({locationFieldName}, 1, false, (float*)&mat2x4);");
                    break;

                case "mat3":
                case "mat3x3":
                    builder.AppendLine($"                var mat3 = (Matrix3X3<float>)value;");
                    builder.AppendLine($"                _gl?.UniformMatrix3({locationFieldName}, 1, false, (float*)&mat3);");
                    break;

                case "mat3x2":
                    builder.AppendLine($"                var mat3x2 = (Matrix3X2<float>)value;");
                    builder.AppendLine($"                _gl?.UniformMatrix3x2({locationFieldName}, 1, false, (float*)&mat3x2);");
                    break;

                case "mat3x4":
                    builder.AppendLine($"                var mat3x4 = (Matrix3X4<float>)value;");
                    builder.AppendLine($"                _gl?.UniformMatrix3x4({locationFieldName}, 1, false, (float*)&mat3x4);");
                    break;

                case "mat4":
                case "mat4x4":
                    builder.AppendLine($"                var mat4 = (Matrix4X4<float>)value;");
                    builder.AppendLine($"                _gl?.UniformMatrix4({locationFieldName}, 1, false, (float*)&mat4);");
                    break;

                case "mat4x2":
                    builder.AppendLine($"                var mat4x2 = (Matrix4X2<float>)value;");
                    builder.AppendLine($"                _gl?.UniformMatrix4x2({locationFieldName}, 1, false, (float*)&mat4x2);");
                    break;

                case "mat4x3":
                    builder.AppendLine($"                var mat4x3 = (Matrix4X3<float>)value;");
                    builder.AppendLine($"                _gl?.UniformMatrix4x3({locationFieldName}, 1, false, (float*)&mat4x3);");
                    break;

                case string s when s.StartsWith("sampler"):
                    builder.AppendLine($"                _gl?.Uniform1({locationFieldName}, value);");
                    break;
                case string s when s.StartsWith("isampler"):
                    builder.AppendLine($"                _gl?.Uniform1i({locationFieldName}, value);");
                    break;
                case string s when s.StartsWith("usampler"):
                    builder.AppendLine($"                _gl?.Uniform1ui({locationFieldName}, value);");
                    break;

                default:
                    builder.AppendLine($"                throw new NotSupportedException(\"Unsupported uniform type: {type}\");");
                    break;
            }

            builder.AppendLine("            }");
            return builder.ToString();
        }

    }

}