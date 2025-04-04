using System.Text;

namespace OpenglLib
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

        internal static string GenerateComponentTemplate(RSFileInfo rsFileInfo)
        {
            string componentName = rsFileInfo.ComponentName;

            var mainBuilder = new StringBuilder();
            var contentBuilder = new StringBuilder();
            var fieldsBuilder = new StringBuilder();
            var constructorBuilder = new StringBuilder();
            var constructorBodyBuilder = new StringBuilder();
            var makeCleanBuilder = new StringBuilder();
            var makeCleanBodyBuilder = new StringBuilder();

            GenerateComponentStructure(mainBuilder, componentName);
            GenerateComponentContentStructure(contentBuilder);
            GenerateConstructor(constructorBuilder, componentName);
            GenerateMakeClean(makeCleanBuilder);

            foreach (var uniform in rsFileInfo.Uniforms)
            {
                string typeName = uniform.CSharpTypeName;
                string fieldName = uniform.Name;
                bool isArray = uniform.ArraySize.HasValue;
                int arraySize = isArray ? uniform.ArraySize.Value : 0;
                bool isCustomStruct = GlslParser.IsCustomType(typeName, uniform.Type);
                bool isDirtySupport = uniform.Attributes.Any(e => e.Name.Equals("isdirty", StringComparison.OrdinalIgnoreCase) &&
                                                                 e.Value.Equals("true", StringComparison.OrdinalIgnoreCase));

                if (isArray)
                {
                    if (isCustomStruct)
                    {
                        GenerateCustomStructArrayField(fieldsBuilder, constructorBodyBuilder, makeCleanBodyBuilder, typeName, fieldName, arraySize, isDirtySupport);
                    }
                    else
                    {
                        GenerateSimpleTypeArrayField(fieldsBuilder, constructorBodyBuilder, makeCleanBodyBuilder, typeName, fieldName, arraySize, isDirtySupport);
                    }
                }
                else
                {
                    if (isCustomStruct)
                    {
                        GenerateCustomStructField(fieldsBuilder, constructorBodyBuilder, makeCleanBodyBuilder, typeName, fieldName, isDirtySupport);
                    }
                    else
                    {
                        GenerateSimpleTypeField(fieldsBuilder, constructorBodyBuilder, makeCleanBodyBuilder, typeName, fieldName, isDirtySupport);
                    }
                }
            }

            foreach (var block in rsFileInfo.UniformBlocks)
            {
                if (block.InstanceName != null)
                {
                    string typeName = block.CSharpTypeName;
                    string fieldName = block.InstanceName;
                    bool isDirtySupport = block.Attributes.Any(e => e.Name.Equals("isdirty", StringComparison.OrdinalIgnoreCase) &&
                                                                  e.Value.Equals("true", StringComparison.OrdinalIgnoreCase));

                    GenerateUniformBlockField(fieldsBuilder, constructorBodyBuilder, makeCleanBodyBuilder, typeName, fieldName, isDirtySupport);
                }
            }

            foreach (var structInstance in rsFileInfo.StructureInstances)
            {
                if (!structInstance.IsUniform)
                {
                    string instanceName = !string.IsNullOrEmpty(structInstance.InstanceName)
                        ? structInstance.InstanceName
                        : structInstance.OriginalInstanceName;

                    string structType = string.IsNullOrWhiteSpace(structInstance.Structure.CSharpTypeName)
                        ? structInstance.Structure.Name
                        : structInstance.Structure.CSharpTypeName;

                    bool isDirtySupport = structInstance.Attributes.Any(e => e.Name.Equals("isdirty", StringComparison.OrdinalIgnoreCase) &&
                                                                  e.Value.Equals("true", StringComparison.OrdinalIgnoreCase));

                    if (structInstance.ArraySize.HasValue)
                    {
                        GenerateStructArrayField(fieldsBuilder, constructorBodyBuilder, makeCleanBodyBuilder, structType, instanceName, structInstance.ArraySize.Value, isDirtySupport);
                    }
                    else
                    {
                        GenerateStructField(fieldsBuilder, constructorBodyBuilder, makeCleanBodyBuilder, structType, instanceName, isDirtySupport);
                    }
                }
            }

            string constructorText = constructorBuilder.ToString()
                .Replace(CONSTRUCTOR_BODY_PLACEHOLDER, constructorBodyBuilder.ToString().TrimEnd());

            string makeCleanText = makeCleanBuilder.ToString()
                .Replace(MAKE_CLEAN_BODY_PLACEHOLDER, makeCleanBodyBuilder.ToString().TrimEnd());

            string contentText = contentBuilder.ToString()
                .Replace(FIELDS_PLACEHOLDER, fieldsBuilder.ToString())
                .Replace(CONSTRUCTOR_PLACEHOLDER, constructorText)
                .Replace(MAKE_CLEAN_PLACEHOLDER, makeCleanText)
                .Replace(DISPOSE_PLACEHOLDER, "");

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


        private static void GenerateStructField(StringBuilder fieldsBuilder, StringBuilder constructorBodyBuilder, StringBuilder makeCleanBuilder, string typeName, string fieldName, bool isDirtySupport)
        {
            fieldsBuilder.AppendLine($"        [ShowInInspector]");

            if (isDirtySupport)
            {
                fieldsBuilder.AppendLine($"        [SupportDirty]");
            }

            fieldsBuilder.AppendLine($"        public {typeName} {fieldName};");
            constructorBodyBuilder.AppendLine($"            {fieldName} = new {typeName}(null);");

            if (isDirtySupport)
            {
                fieldsBuilder.AppendLine($"        public bool IsDirty{fieldName}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.AppendLine($"            get => {fieldName}.IsDirty;");
                fieldsBuilder.AppendLine($"            set => {fieldName}.IsDirty = value;");
                fieldsBuilder.AppendLine("        }");

                makeCleanBuilder.AppendLine($"            IsDirty{fieldName} = false;");
            }
        }

        private static void GenerateStructArrayField(StringBuilder fieldsBuilder, StringBuilder constructorBodyBuilder, StringBuilder makeCleanBuilder, string typeName, string fieldName, int arraySize, bool isDirtySupport)
        {
            fieldsBuilder.AppendLine($"        [ShowInInspector]");

            if (isDirtySupport)
            {
                fieldsBuilder.AppendLine($"        [SupportDirty]");
            }

            fieldsBuilder.AppendLine($"        public StructArray<{typeName}> {fieldName};");
            constructorBodyBuilder.AppendLine($"            {fieldName} = new StructArray<{typeName}>({arraySize});");

            if (isDirtySupport)
            {
                fieldsBuilder.AppendLine($"        public bool IsDirty{fieldName}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.AppendLine($"            get => {fieldName}.IsDirty;");
                fieldsBuilder.AppendLine($"            set => {fieldName}.IsDirty = value;");
                fieldsBuilder.AppendLine("        }");

                makeCleanBuilder.AppendLine($"            IsDirty{fieldName} = false;");
            }
        }
        private static void GenerateCustomStructArrayField(StringBuilder fieldsBuilder, StringBuilder constructorBuilder, StringBuilder makeCleanBuilder, string typeName, string fieldName, int arraySize, bool isDirtySupport)
        {
            fieldsBuilder.AppendLine($"        [ShowInInspector]");

            if (isDirtySupport)
            {
                fieldsBuilder.AppendLine($"        [SupportDirty]");
            }

            fieldsBuilder.AppendLine($"        public StructArray<{typeName}> {fieldName};");
            constructorBuilder.AppendLine($"            {fieldName} = new StructArray<{typeName}>({arraySize});");

            if (isDirtySupport)
            {
                fieldsBuilder.AppendLine($"        public bool IsDirty{fieldName}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.AppendLine($"            get => {fieldName}.IsDirty;");
                fieldsBuilder.AppendLine($"            set => {fieldName}.IsDirty = value;");
                fieldsBuilder.AppendLine("        }");

                makeCleanBuilder.AppendLine($"            IsDirty{fieldName} = false;");
            }

            fieldsBuilder.AppendLine();
        }

        private static void GenerateSimpleTypeArrayField(StringBuilder fieldsBuilder, StringBuilder constructorBuilder, StringBuilder makeCleanBuilder, string typeName, string fieldName, int arraySize, bool isDirtySupport)
        {
            fieldsBuilder.AppendLine($"        [ShowInInspector]");

            if (isDirtySupport)
            {
                fieldsBuilder.AppendLine($"        [SupportDirty]");
            }

            fieldsBuilder.AppendLine($"        public LocaleArray<{typeName}> {fieldName};");
            constructorBuilder.AppendLine($"            {fieldName} = new LocaleArray<{typeName}>({arraySize});");

            if (isDirtySupport)
            {
                fieldsBuilder.AppendLine($"        public bool IsDirty{fieldName}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.AppendLine($"            get => {fieldName}.IsDirty;");
                fieldsBuilder.AppendLine($"            set => {fieldName}.IsDirty = value;");
                fieldsBuilder.AppendLine("        }");

                makeCleanBuilder.AppendLine($"            IsDirty{fieldName} = false;");
            }

            fieldsBuilder.AppendLine();
        }

        private static void GenerateCustomStructField(StringBuilder fieldsBuilder, StringBuilder constructorBuilder, StringBuilder makeCleanBuilder, string typeName, string fieldName, bool isDirtySupport)
        {
            fieldsBuilder.AppendLine($"        [ShowInInspector]");

            if (!isDirtySupport)
            {
                fieldsBuilder.AppendLine($"        public {typeName} {fieldName};");
            }

            constructorBuilder.AppendLine($"            {fieldName} = new {typeName}(null);");

            if (isDirtySupport)
            {
                fieldsBuilder.AppendLine($"        [SupportDirty]");
                fieldsBuilder.AppendLine($"        private {typeName} _{fieldName};");

                constructorBuilder.AppendLine($"            IsDirty{fieldName} = true;");

                fieldsBuilder.AppendLine($"        public bool IsDirty{fieldName}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.AppendLine($"            get => {fieldName}.IsDirty;");
                fieldsBuilder.AppendLine($"            set => {fieldName}.IsDirty = value;");
                fieldsBuilder.AppendLine("        }");

                makeCleanBuilder.AppendLine($"            IsDirty{fieldName} = false;");
            }

            fieldsBuilder.AppendLine();
        }

        private static void GenerateSimpleTypeField(StringBuilder fieldsBuilder, StringBuilder constructorBuilder, StringBuilder makeCleanBuilder, string typeName, string fieldName, bool isDirtySupport)
        {
            fieldsBuilder.AppendLine($"        [ShowInInspector]");

            if (!isDirtySupport)
            {
                fieldsBuilder.AppendLine($"        public {typeName} {fieldName};");
            }

            if (isDirtySupport)
            {
                fieldsBuilder.AppendLine($"        [SupportDirty]");
                fieldsBuilder.AppendLine($"        private {typeName} _{fieldName};");

                constructorBuilder.AppendLine($"            IsDirty{fieldName} = true;");

                fieldsBuilder.AppendLine("        [DefaultBool(true)]");
                fieldsBuilder.AppendLine($"        public bool IsDirty{fieldName};");

                fieldsBuilder.AppendLine($"        public {typeName} {fieldName}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.AppendLine($"            get => _{fieldName};");
                fieldsBuilder.AppendLine("            set");
                fieldsBuilder.AppendLine("            {");
                fieldsBuilder.AppendLine($"                _{fieldName} = value;");
                fieldsBuilder.AppendLine($"                IsDirty{fieldName} = true;");
                fieldsBuilder.AppendLine("            }");
                fieldsBuilder.AppendLine("        }");

                makeCleanBuilder.AppendLine($"            IsDirty{fieldName} = false;");
            }

            fieldsBuilder.AppendLine();
        }

        private static void GenerateUniformBlockField(StringBuilder fieldsBuilder, StringBuilder constructorBuilder, StringBuilder makeCleanBuilder, string typeName, string fieldName, bool isDirtySupport)
        {
            fieldsBuilder.AppendLine($"        [ShowInInspector]");

            if (!isDirtySupport)
            {
                fieldsBuilder.AppendLine($"        public {typeName} {fieldName};");
            }

            if (isDirtySupport)
            {
                fieldsBuilder.AppendLine($"        [SupportDirty]");
                fieldsBuilder.AppendLine($"        private {typeName} _{fieldName};");

                fieldsBuilder.AppendLine("        [DefaultBool(true)]");
                fieldsBuilder.AppendLine($"        public bool IsDirty{fieldName};");

                fieldsBuilder.AppendLine($"        public {typeName} {fieldName}");
                fieldsBuilder.AppendLine("        {");
                fieldsBuilder.AppendLine($"            get => _{fieldName};");
                fieldsBuilder.AppendLine("            set");
                fieldsBuilder.AppendLine("            {");
                fieldsBuilder.AppendLine($"                _{fieldName} = value;");
                fieldsBuilder.AppendLine($"                IsDirty{fieldName} = true;");
                fieldsBuilder.AppendLine("            }");
                fieldsBuilder.AppendLine("        }");

                constructorBuilder.AppendLine($"            IsDirty{fieldName} = true;");
                makeCleanBuilder.AppendLine($"            IsDirty{fieldName} = false;");
            }

            fieldsBuilder.AppendLine();
        }



    }

}
