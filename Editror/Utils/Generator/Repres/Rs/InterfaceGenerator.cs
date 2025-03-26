using System.Text;
using OpenglLib;

namespace Editor
{
    public static class InterfaceGenerator
    {
        public static string GenerateInterface(RSFileInfo fileInfo)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"{GeneratorConst.GetDefaultNamespaces()}");
            builder.AppendLine();

            builder.AppendLine($"{GeneratorConst.GetUserScriptNamespace()}");
            builder.AppendLine("{");
            builder.AppendLine($"    public interface {fileInfo.InterfaceName}");
            builder.AppendLine("    {");

            foreach (var block in fileInfo.UniformBlocks)
            {
                if (block.InstanceName != null)
                {
                    string propertyName = block.InstanceName ?? block.Name;
                    string typeName = $"{block.CSharpTypeName}";
                    builder.AppendLine($"        public {typeName} {propertyName} {{ set; }}");
                }
            }

            foreach (var (type, name, arraySize) in fileInfo.Uniforms)
            {
                string csharpType = GlslParser.MapGlslTypeToCSharp(type);

                if (arraySize.HasValue)
                {
                    builder.AppendLine($"        public {csharpType} {name} {{ set; }}");
                }
                else
                {
                    builder.AppendLine($"        public {csharpType} {name} {{ set; }}");
                }
            }
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }
    }
}
