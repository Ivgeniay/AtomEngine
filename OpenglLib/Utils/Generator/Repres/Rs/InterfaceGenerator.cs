using System.Text;

namespace OpenglLib
{
    public static class InterfaceGenerator
    {
        private const string CONTENT_PLACEHOLDER = "/*CONTENT*/";
        private const string PROPERTIES_PLACEHOLDER = "/*PROPERTIES*/";

        public static string GenerateInterface(RSFileInfo fileInfo)
        {
            var mainBuilder = new StringBuilder();
            var contentBuilder = new StringBuilder();
            var propertiesBuilder = new StringBuilder();

            GenerateInterfaceStructure(mainBuilder, fileInfo.InterfaceName);
            GenerateInterfaceContentStructure(contentBuilder);

            foreach (var block in fileInfo.UniformBlocks)
            {
                if (block.InstanceName != null)
                {
                    UniformBlockCase(propertiesBuilder, block);
                }
            }

            foreach (var uniform in fileInfo.Uniforms)
            {
                string type = uniform.Type;
                string name = uniform.Name;
                int? arraySize = uniform.ArraySize;
                string csharpType = GlslParser.MapGlslTypeToCSharp(type);
                bool isCustomStruct = GlslParser.IsCustomType(csharpType, type);

                if (arraySize.HasValue)
                {
                    if (isCustomStruct)
                    {
                        UniformArrayCustomStructCase(propertiesBuilder, type, name, arraySize.Value, csharpType);
                    }
                    else
                    {
                        UniformArrayCase(propertiesBuilder, type, name, arraySize.Value, csharpType);
                    }
                }
                else
                {
                    if (isCustomStruct)
                    {
                        UniformCustomStructCase(propertiesBuilder, type, name, csharpType);
                    }
                    else
                    {
                        UniformCase(propertiesBuilder, type, name, csharpType);
                    }
                }
            }

            string contentText = contentBuilder.ToString()
                .Replace(PROPERTIES_PLACEHOLDER, propertiesBuilder.ToString());

            string result = mainBuilder.ToString().Replace(CONTENT_PLACEHOLDER, contentText);

            return result;
        }

        private static void GenerateInterfaceStructure(StringBuilder builder, string interfaceName)
        {
            builder.AppendLine($"{GeneratorConst.GetDefaultNamespaces()}");
            builder.AppendLine();
            builder.AppendLine($"{GeneratorConst.GetUserScriptNamespace()}");
            builder.AppendLine("{");
            builder.AppendLine($"    public interface {interfaceName}");
            builder.AppendLine("    {");
            builder.AppendLine(CONTENT_PLACEHOLDER);
            builder.AppendLine("    }");
            builder.AppendLine("}");
        }

        private static void GenerateInterfaceContentStructure(StringBuilder builder)
        {
            builder.AppendLine(PROPERTIES_PLACEHOLDER);
        }

        private static void UniformBlockCase(StringBuilder builder, dynamic block)
        {
            string propertyName = block.InstanceName ?? block.Name;
            string typeName = $"{block.CSharpTypeName}";
            builder.AppendLine($"        {typeName} {propertyName} {{ set; }}");
        }

        private static void UniformCase(StringBuilder builder, string type, string name, string csharpType)
        {
            builder.AppendLine($"        {csharpType} {name} {{ set; }}");
        }

        private static void UniformCustomStructCase(StringBuilder builder, string type, string name, string csharpType)
        {
            builder.AppendLine($"        {csharpType} {name} {{ get; }}");
        }

        private static void UniformArrayCase(StringBuilder builder, string type, string name, int arraySize, string csharpType)
        {
            builder.AppendLine($"        LocaleArray<{csharpType}> {name} {{ get; }}");
        }

        private static void UniformArrayCustomStructCase(StringBuilder builder, string type, string name, int arraySize, string csharpType)
        {
            builder.AppendLine($"        StructArray<{csharpType}> {name} {{ get; }}");
        }
    }

}
