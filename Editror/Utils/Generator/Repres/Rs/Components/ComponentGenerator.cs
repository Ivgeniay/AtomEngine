using System.Text;
using OpenglLib;

namespace Editor
{
    internal static class ComponentGenerator
    {
        public static string GenerateComponentTemplate(RSFileInfo rsFileInfo, out ComponentGeneratorInfo info)
        {
            info = new ComponentGeneratorInfo();

            string interfaceName = rsFileInfo.InterfaceName;
            string componentName = GetComponentNameFromInterface(interfaceName);
            info.ComponentName = componentName;

            var builder = new StringBuilder();
            builder.AppendLine($"{GeneratorConst.GetDefaultNamespaces()}");
            builder.AppendLine();
            builder.AppendLine($"{GeneratorConst.GetUserScriptNamespace()}");
            builder.AppendLine("{");
            builder.AppendLine("    [TooltipCategoryComponent(category: ComponentCategory.Render)]");
            builder.AppendLine($"    public partial struct {componentName} : IComponent");
            builder.AppendLine("    {");
            builder.AppendLine("        public Entity Owner { get; set; }");
            builder.AppendLine();

            foreach (var uniform in rsFileInfo.Uniforms)
            {
                ComponentGeneratorFieldInfo fieldInfo = new ComponentGeneratorFieldInfo();

                string type = uniform.type;
                string name = uniform.name;
                int? arraySize = uniform.arraySize;
                string csharpType = GlslParser.MapGlslTypeToCSharp(type);

                fieldInfo.FieldName = name;
                fieldInfo.FieldType = csharpType;
                fieldInfo.IsUniform = true;
                fieldInfo.IsArray = arraySize.HasValue;
                fieldInfo.ArraySize = arraySize.HasValue ? arraySize.Value : 0;
                fieldInfo.IsDirtySupport = true;
                fieldInfo.Attributes.Add("[ShowInInspector]");
                fieldInfo.Attributes.Add("[SupportDirty]");

                foreach(var attr in fieldInfo.Attributes)
                {
                    builder.AppendLine($"        {attr}");
                }

                if (arraySize.HasValue)
                {
                    builder.AppendLine($"        private {csharpType}[] _{name};");
                }
                else
                {
                    builder.AppendLine($"        private {csharpType} _{name};");
                }

                if (fieldInfo.IsDirtySupport)
                {
                    builder.AppendLine("        [DefaultBool(true)]");
                    builder.AppendLine($"        public bool IsDirty{name};");

                    if (arraySize.HasValue)
                    {
                        builder.AppendLine($"        public {csharpType}[] {name}");
                    }
                    else
                    {
                        builder.AppendLine($"        public {csharpType} {name}");
                    }

                    builder.AppendLine("        {");
                    builder.AppendLine($"            get => _{name};");
                    builder.AppendLine("            set");
                    builder.AppendLine("            {");
                    builder.AppendLine($"                _{name} = value;");
                    builder.AppendLine($"                IsDirty{name} = true;");
                    builder.AppendLine("            }");
                    builder.AppendLine("        }");
                }

                builder.AppendLine();

                info.AddField(fieldInfo);
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
                    fieldInfo.IsUniformBlock = true;

                    builder.AppendLine("        [ShowInInspector]");
                    builder.AppendLine($"        private {typeName} _{propertyName};");

                    builder.AppendLine("        [DefaultBool(true)]");
                    builder.AppendLine($"        public bool IsDirty{propertyName};");

                    builder.AppendLine($"        public {typeName} {propertyName}");
                    builder.AppendLine("        {");
                    builder.AppendLine($"            get => _{propertyName};");
                    builder.AppendLine("            set");
                    builder.AppendLine("            {");
                    builder.AppendLine($"                _{propertyName} = value;");
                    builder.AppendLine($"                IsDirty{propertyName} = true;");
                    builder.AppendLine("            }");
                    builder.AppendLine("        }");
                    builder.AppendLine();

                    info.AddField(fieldInfo);
                }
            }

            builder.AppendLine($"        public {componentName}(Entity owner)");
            builder.AppendLine("        {");
            builder.AppendLine("            Owner = owner;");

            foreach(var field in info.Fields)
            {
                if (field.IsUniform)
                {
                    if (field.IsArray)
                    {
                        builder.AppendLine($"            _{field.FieldName} = new {field.FieldType}[{field.ArraySize}];");
                    }
                    else
                    {
                        builder.AppendLine($"            _{field.FieldName} = default;");
                    }
                    if (field.IsDirtySupport)
                    {
                        builder.AppendLine($"            IsDirty{field.FieldName} = true;");
                    }
                }

                if (field.IsUniformBlock)
                {
                    builder.AppendLine($"            _{field.FieldName} = default;");
                    if (field.IsDirtySupport)
                    {
                        builder.AppendLine($"            IsDirty{field.FieldName} = true;");
                    }
                }
            }

            builder.AppendLine("        }");

            builder.AppendLine("        public void MakeClean()");
            builder.AppendLine("        {");

            foreach (var field in info.Fields)
            {
                if (field.IsDirtySupport)
                {
                    builder.AppendLine($"            IsDirty{field.FieldName} = false;");
                }
            }

            builder.AppendLine("        }");

            builder.AppendLine("    }");
            builder.AppendLine("}");



            return builder.ToString();
        }

        public static string GetComponentNameFromInterface(string interfaceName)
        {
            string baseName = interfaceName.StartsWith("I") ? interfaceName.Substring(1) : interfaceName;
            return $"{baseName}Component";
        }
    }
}
