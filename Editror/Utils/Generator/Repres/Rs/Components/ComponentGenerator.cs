using System.Linq;
using System.Text;
using OpenglLib;

namespace Editor
{
    internal static class ComponentGenerator
    {
        private const string CONTENT_PLACEHOLDER = "/*CONTENT*/";
        private const string FIELDS_PLACEHOLDER = "/*FIELDS*/";
        private const string CONSTRUCTOR_PLACEHOLDER = "/*CONSTRUCTOR*/";
        private const string MAKE_CLEAN_PLACEHOLDER = "/*MAKE_CLEAN*/";
        private const string DISPOSE_PLACEHOLDER = "/*DISPOSE*/";
        private const string CONSTRUCTOR_BODY_PLACEHOLDER = "/*CONSTRUCTOR_BODY*/";
        private const string MAKE_CLEAN_BODY_PLACEHOLDER = "/*MAKE_CLEAN_BODY*/";

        public static string GenerateComponentTemplate(RSFileInfo rsFileInfo, out ComponentGeneratorInfo info)
        {
            info = new ComponentGeneratorInfo();

            string interfaceName = rsFileInfo.InterfaceName;
            string componentName = rsFileInfo.ComponentName;
            info.ComponentName = componentName;

            var mainBuilder = new StringBuilder();
            var contentBuilder = new StringBuilder();
            var fieldsBuilder = new StringBuilder();
            var constructorBuilder = new StringBuilder();
            var makeCleanBuilder = new StringBuilder();
            var disposeBuilder = new StringBuilder();

            var constructorBodyBuilder = new StringBuilder();
            var makeCleanBodyBuilder = new StringBuilder();

            GenerateComponentStructure(mainBuilder, componentName);
            GenerateComponentContentStructure(contentBuilder);
            GenerateConstructor(constructorBuilder, componentName);
            GenerateMakeClean(makeCleanBuilder);

            foreach (var uniform in rsFileInfo.Uniforms)
            {
                ComponentGeneratorFieldInfo fieldInfo = new ComponentGeneratorFieldInfo();

                string type = uniform.Type;
                string name = uniform.Name;
                int? arraySize = uniform.ArraySize;
                string csharpType = GlslParser.MapGlslTypeToCSharp(type);

                fieldInfo.FieldName = name;
                fieldInfo.FieldType = csharpType;
                fieldInfo.IsUniform = true;
                fieldInfo.IsArray = arraySize.HasValue;
                fieldInfo.ArraySize = arraySize.HasValue ? arraySize.Value : 0;
                fieldInfo.IsDirtySupport = uniform.Attributes.Any(e => e.Name == "IsDirty".ToLower() && e.Value == "True".ToLower());
                //foreach(var attribute in uniform.Attributes)
                //{
                //    fieldInfo.IsDirtySupport = true;
                //}
                fieldInfo.Attributes.Add("[ShowInInspector]");
                fieldInfo.IsCustomStruct =  GlslParser.IsCustomType(csharpType, type);

                info.AddField(fieldInfo);

                if (fieldInfo.IsArray)
                {
                    if (fieldInfo.IsCustomStruct)
                        UniformArrayCustomStructCase(fieldsBuilder, constructorBodyBuilder, makeCleanBodyBuilder, fieldInfo);
                    else
                        UniformArrayCase(fieldsBuilder, constructorBodyBuilder, makeCleanBodyBuilder, fieldInfo);
                }
                else
                {
                    if (fieldInfo.IsCustomStruct)
                        UniformCustomStructCase(fieldsBuilder, constructorBodyBuilder, makeCleanBodyBuilder, fieldInfo);
                    else
                        UniformCase(fieldsBuilder, constructorBodyBuilder, makeCleanBodyBuilder, fieldInfo);
                }
            }

            foreach (var block in rsFileInfo.UniformBlocks)
            {
                if (block.InstanceName != null)
                {
                    ComponentGeneratorFieldInfo fieldInfo = new ComponentGeneratorFieldInfo();

                    string typeName = $"{block.CSharpTypeName}";
                    string propertyName = block.InstanceName;

                    fieldInfo.FieldName = propertyName;
                    fieldInfo.FieldType = typeName;

                    fieldInfo.Attributes.Add("[ShowInInspector]");
                    fieldInfo.IsUniformBlock = true;
                    fieldInfo.IsDirtySupport = block.Attributes.Any(e => e.Name == "IsDirty".ToLower() && e.Value == "True".ToLower()); ;

                    info.AddField(fieldInfo);

                    UniformBlockCase(fieldsBuilder, constructorBodyBuilder, makeCleanBodyBuilder, fieldInfo);
                }
            }


            string constructorText = constructorBuilder.ToString()
                .Replace(CONSTRUCTOR_BODY_PLACEHOLDER, constructorBodyBuilder.ToString().TrimEnd());

            string makeCleanText = makeCleanBuilder.ToString()
                .Replace(MAKE_CLEAN_BODY_PLACEHOLDER, makeCleanBodyBuilder.ToString().TrimEnd());

            string contentText = contentBuilder.ToString()
                .Replace(FIELDS_PLACEHOLDER, fieldsBuilder.ToString())
                .Replace(CONSTRUCTOR_PLACEHOLDER, constructorText)
                .Replace(MAKE_CLEAN_PLACEHOLDER, makeCleanText);

            if (disposeBuilder.Length > 0)
            {
                contentText = contentText.Replace(DISPOSE_PLACEHOLDER, disposeBuilder.ToString());
            }
            else
            {
                contentText = contentText.Replace(DISPOSE_PLACEHOLDER, "");
            }

            string result = mainBuilder.ToString().Replace(CONTENT_PLACEHOLDER, contentText);

            return result;
        }

        private static void GenerateComponentStructure(StringBuilder builder, string componentName)
        {
            builder.AppendLine($"{GeneratorConst.GetDefaultNamespaces()}");
            builder.AppendLine();
            builder.AppendLine($"{GeneratorConst.GetUserScriptNamespace()}");
            builder.AppendLine("{");
            builder.AppendLine("    [TooltipCategoryComponent(category: ComponentCategory.Render)]");
            builder.AppendLine($"    public partial struct {componentName} : IComponent");
            builder.AppendLine("    {");
            builder.AppendLine("        public Entity Owner { get; set; }");
            builder.AppendLine();
            builder.AppendLine(CONTENT_PLACEHOLDER);
            builder.AppendLine("    }");
            builder.AppendLine("}");
        }

        private static void GenerateComponentContentStructure(StringBuilder builder)
        {
            builder.AppendLine(FIELDS_PLACEHOLDER);
            builder.AppendLine(CONSTRUCTOR_PLACEHOLDER);
            builder.AppendLine(DISPOSE_PLACEHOLDER);
            builder.AppendLine(MAKE_CLEAN_PLACEHOLDER);
        }

        private static void GenerateConstructor(StringBuilder builder, string componentName)
        {
            builder.AppendLine($"        public {componentName}(Entity owner)");
            builder.AppendLine("        {");
            builder.AppendLine("            Owner = owner;");
            builder.AppendLine(CONSTRUCTOR_BODY_PLACEHOLDER);
            builder.AppendLine("        }");
        }

        private static void GenerateMakeClean(StringBuilder builder)
        {
            builder.AppendLine("        public void MakeClean()");
            builder.AppendLine("        {");
            builder.AppendLine(MAKE_CLEAN_BODY_PLACEHOLDER);
            builder.AppendLine("        }");
        }

        private static void UniformCustomStructCase(StringBuilder fieldsBuilder, StringBuilder constructorBuilder, StringBuilder makeCleanBuilder, ComponentGeneratorFieldInfo fieldInfo)
        {
            //public DirectionalLight Lights  = new DirectionalLight();
            foreach (var attr in fieldInfo.Attributes)
            {
                fieldsBuilder.AppendLine($"        {attr}");
            }

            if (!fieldInfo.IsDirtySupport)
            {
                fieldsBuilder.AppendLine($"        public {fieldInfo.FieldType} {fieldInfo.FieldName};");
            }

            constructorBuilder.AppendLine($"            {fieldInfo.FieldName} = new {fieldInfo.FieldType}();");

            if (fieldInfo.IsDirtySupport)
            {
                fieldsBuilder.AppendLine($"        [SupportDirty]");
                fieldsBuilder.AppendLine($"        private {fieldInfo.FieldType} _{fieldInfo.FieldName};");

                constructorBuilder.AppendLine($"            IsDirty{fieldInfo.FieldName} = true;");

                fieldsBuilder.AppendLine($"        public bool IsDirty{fieldInfo.FieldName}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.AppendLine($"            get => {fieldInfo.FieldName}.IsDirty;");
                fieldsBuilder.AppendLine($"            set => {fieldInfo.FieldName}.IsDirty = value;");
                fieldsBuilder.AppendLine("        }");

                makeCleanBuilder.AppendLine($"            IsDirty{fieldInfo.FieldName} = false;");
            }
        }

        private static void UniformCase(StringBuilder fieldsBuilder, StringBuilder constructorBuilder, StringBuilder makeCleanBuilder, ComponentGeneratorFieldInfo fieldInfo)
        {
            foreach (var attr in fieldInfo.Attributes)
            {
                fieldsBuilder.AppendLine($"        {attr}");
            }

            if (!fieldInfo.IsDirtySupport)
            {
                fieldsBuilder.AppendLine($"        public {fieldInfo.FieldType} {fieldInfo.FieldName};");
            }

            if (fieldInfo.IsDirtySupport)
            {
                fieldsBuilder.AppendLine($"        [SupportDirty]");
                fieldsBuilder.AppendLine($"        private {fieldInfo.FieldType} _{fieldInfo.FieldName};");

                constructorBuilder.AppendLine($"            IsDirty{fieldInfo.FieldName} = true;");

                fieldsBuilder.AppendLine("        [DefaultBool(true)]");
                fieldsBuilder.AppendLine($"        public bool IsDirty{fieldInfo.FieldName};");

                fieldsBuilder.AppendLine($"        public {fieldInfo.FieldType} {fieldInfo.FieldName}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.AppendLine($"            get => _{fieldInfo.FieldName};");
                fieldsBuilder.AppendLine("            set");
                fieldsBuilder.AppendLine("            {");
                fieldsBuilder.AppendLine($"                _{fieldInfo.FieldName} = value;");
                fieldsBuilder.AppendLine($"                IsDirty{fieldInfo.FieldName} = true;");
                fieldsBuilder.AppendLine("            }");
                fieldsBuilder.AppendLine("        }");

                makeCleanBuilder.AppendLine($"            IsDirty{fieldInfo.FieldName} = false;");
            }

            fieldsBuilder.AppendLine();
        }

        private static void UniformArrayCustomStructCase(StringBuilder fieldsBuilder, StringBuilder constructorBuilder, StringBuilder makeCleanBuilder, ComponentGeneratorFieldInfo fieldInfo)
        {
            foreach (var attr in fieldInfo.Attributes)
            {
                fieldsBuilder.AppendLine($"        {attr}");
            }

            if (fieldInfo.IsDirtySupport)
            {
                fieldsBuilder.AppendLine($"        [SupportDirty]");
            }
            fieldsBuilder.AppendLine($"        public StructArray<{fieldInfo.FieldType}> {fieldInfo.FieldName};");

            constructorBuilder.AppendLine($"            {fieldInfo.FieldName} = new StructArray<{fieldInfo.FieldType}>({fieldInfo.ArraySize});");

            if (fieldInfo.IsDirtySupport)
            {
                fieldsBuilder.AppendLine($"        public bool IsDirty{fieldInfo.FieldName}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.AppendLine($"            get => {fieldInfo.FieldName}.IsDirty;");
                fieldsBuilder.AppendLine($"            set => {fieldInfo.FieldName}.IsDirty = value;");
                fieldsBuilder.AppendLine("        }");

                makeCleanBuilder.AppendLine($"            IsDirty{fieldInfo.FieldName} = false;");
            }
            fieldsBuilder.AppendLine();
        }

        private static void UniformArrayCase(StringBuilder fieldsBuilder, StringBuilder constructorBuilder, StringBuilder makeCleanBuilder, ComponentGeneratorFieldInfo fieldInfo)
        {
            foreach (var attr in fieldInfo.Attributes)
            {
                fieldsBuilder.AppendLine($"        {attr}");
            }
            if (fieldInfo.IsDirtySupport)
            {
                fieldsBuilder.AppendLine($"        [SupportDirty]");
            }
            fieldsBuilder.AppendLine($"        public LocaleArray<{fieldInfo.FieldType}> {fieldInfo.FieldName};");
            
            constructorBuilder.AppendLine($"            {fieldInfo.FieldName} = new LocaleArray<{fieldInfo.FieldType}>({fieldInfo.ArraySize});");

            if (fieldInfo.IsDirtySupport)
            {
                constructorBuilder.AppendLine($"            IsDirty{fieldInfo.FieldName} = true;");

                fieldsBuilder.AppendLine($"        public bool IsDirty{fieldInfo.FieldName}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.AppendLine($"            get => {fieldInfo.FieldName}.IsDirty;");
                fieldsBuilder.AppendLine($"            set => {fieldInfo.FieldName}.IsDirty = value;");
                fieldsBuilder.AppendLine("        }");

                makeCleanBuilder.AppendLine($"            IsDirty{fieldInfo.FieldName} = false;");
            }

            fieldsBuilder.AppendLine();
        }

        private static void UniformBlockCase(StringBuilder fieldsBuilder, StringBuilder constructorBuilder, StringBuilder makeCleanBuilder, ComponentGeneratorFieldInfo fieldInfo)
        {
            foreach (var attr in fieldInfo.Attributes)
            {
                fieldsBuilder.AppendLine($"        {attr}");
            }

            if (!fieldInfo.IsDirtySupport)
            {
                fieldsBuilder.AppendLine($"        public {fieldInfo.FieldType} {fieldInfo.FieldName};");
            }

            if (fieldInfo.IsDirtySupport)
            {
                fieldsBuilder.AppendLine($"        [SupportDirty]");
                fieldsBuilder.AppendLine($"        private {fieldInfo.FieldType} _{fieldInfo.FieldName};");

                fieldsBuilder.AppendLine("        [DefaultBool(true)]");
                fieldsBuilder.AppendLine($"        public bool IsDirty{fieldInfo.FieldName};");

                fieldsBuilder.AppendLine($"        public {fieldInfo.FieldType} {fieldInfo.FieldName}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.AppendLine($"            get => _{fieldInfo.FieldName};");
                fieldsBuilder.AppendLine("            set");
                fieldsBuilder.AppendLine("            {");
                fieldsBuilder.AppendLine($"                _{fieldInfo.FieldName} = value;");
                fieldsBuilder.AppendLine($"                IsDirty{fieldInfo.FieldName} = true;");
                fieldsBuilder.AppendLine("            }");
                fieldsBuilder.AppendLine("        }");
                fieldsBuilder.AppendLine();

                makeCleanBuilder.AppendLine($"            IsDirty{fieldInfo.FieldName} = false;");
            }
        }

        public static string GetComponentNameFromInterface(string interfaceName)
        {
            string baseName = interfaceName.StartsWith("I") ? interfaceName.Substring(1) : interfaceName;
            return $"{baseName}Component";
        }
    }

}
