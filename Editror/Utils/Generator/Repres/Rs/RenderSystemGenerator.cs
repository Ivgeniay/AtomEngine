using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Editor
{
    public static class RenderSystemGenerator
    {
        public static string GenerateRenderSystemTemplate(RSFileInfo rsFileInfo, List<ComponentGeneratorInfo> generatorInfos = null)
        {
            string interfaceName = rsFileInfo.InterfaceName;
            string systemName = GetSystemNameFromInterface(interfaceName);

            var requiredComponents = rsFileInfo.RequiredComponent;

            var builder = new StringBuilder();

            builder.AppendLine($"{GeneratorConst.GetDefaultNamespaces()}");
            builder.AppendLine();

            builder.AppendLine($"{GeneratorConst.GetUserScriptNamespace()}");
            builder.AppendLine("{");

            builder.AppendLine($"    public class {systemName} : IRenderSystem");
            builder.AppendLine("    {");
            builder.AppendLine("        public IWorld World { get; set; }");
            builder.AppendLine("        private QueryEntity queryRendererEntities;");
            builder.AppendLine();

            builder.AppendLine($"        public {systemName}(IWorld world)");
            builder.AppendLine("        {");
            builder.AppendLine("            World = world;");
            builder.AppendLine();
            builder.AppendLine("            queryRendererEntities = this.CreateEntityQuery()");
            builder.AppendLine("                .With<MeshComponent>()");
            builder.AppendLine("                .With<MaterialComponent>()");

            foreach (var component in requiredComponents)
            {
                builder.AppendLine($"                .With<{component}>()");
            }

            if (generatorInfos != null && generatorInfos.Count > 0)
            {
                foreach (var componentInfo in generatorInfos)
                {
                    builder.AppendLine($"                .With<{componentInfo.ComponentName}>()");
                }
            }

            builder.AppendLine("                ;");
            builder.AppendLine("        }");
            builder.AppendLine();

            builder.AppendLine("        public void Render(double deltaTime)");
            builder.AppendLine("        {");
            builder.AppendLine("            Entity[] rendererEntities = queryRendererEntities.Build();");
            builder.AppendLine("            if (rendererEntities.Length == 0)");
            builder.AppendLine("                return;");
            builder.AppendLine();
            builder.AppendLine("            foreach (var entity in rendererEntities)");
            builder.AppendLine("            {");
            builder.AppendLine("                ref var meshComponent = ref this.GetComponent<MeshComponent>(entity);");
            builder.AppendLine("                ref var materialComponent = ref this.GetComponent<MaterialComponent>(entity);");
            builder.AppendLine();
            builder.AppendLine("                if (meshComponent.Mesh == null || materialComponent.Material?.Shader == null)");
            builder.AppendLine("                    continue;");
            builder.AppendLine();

            if (generatorInfos != null && generatorInfos.Count > 0)
            {
                foreach (var componentInfo in generatorInfos)
                {
                    builder.AppendLine($"                ref var {GetComponentVariableName(componentInfo.ComponentName)} = ref this.GetComponent<{componentInfo.ComponentName}>(entity);");
                }
            }

            builder.AppendLine();
            builder.AppendLine($"                if (materialComponent.Material.Shader is {interfaceName} renderer)");
            builder.AppendLine("                {");

            if (generatorInfos != null && generatorInfos.Count > 0)
            {
                foreach (var componentInfo in generatorInfos)
                {
                    string componentVar = GetComponentVariableName(componentInfo.ComponentName);

                    foreach (var field in componentInfo.Fields)
                    {
                        if (field.IsDirtySupport)
                        {
                            builder.AppendLine($"                    if ({componentVar}.IsDirty{field.FieldName})");
                            builder.AppendLine("                    {");
                            builder.AppendLine($"                        renderer.{field.FieldName} = {componentVar}.{field.FieldName};");
                            builder.AppendLine("                    }");
                        }
                        else
                        {
                            builder.AppendLine($"                    renderer.{field.FieldName} = {componentVar}.{field.FieldName};");
                        }
                    }

                    if (componentInfo.Fields.Any(f => f.IsDirtySupport))
                    {
                        builder.AppendLine($"                    {componentVar}.MakeClean();");
                    }
                }
            }
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine("                    meshComponent.Mesh.Draw(materialComponent.Material.Shader);");
            builder.AppendLine("                }");
            builder.AppendLine("            }");
            builder.AppendLine("        }");
            builder.AppendLine();

            // Метод Resize
            builder.AppendLine("        public void Resize(Vector2 size)");
            builder.AppendLine("        {");
            builder.AppendLine("            ");
            builder.AppendLine("        }");
            builder.AppendLine();

            builder.AppendLine("        public void Initialize()");
            builder.AppendLine("        {");
            builder.AppendLine("            ");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        public static string GetSystemNameFromInterface(string interfaceName)
        {
            string baseName = interfaceName.StartsWith("I") ? interfaceName.Substring(1) : interfaceName;
            return $"{baseName}RenderSystem";
        }

        private static string GetComponentVariableName(string componentName)
        {
            if (componentName.EndsWith("Component"))
            {
                string baseName = componentName.Substring(0, componentName.Length - "Component".Length);
                return $"{char.ToLowerInvariant(baseName[0])}{baseName.Substring(1)}Component";
            }

            return $"{char.ToLowerInvariant(componentName[0])}{componentName.Substring(1)}";
        }
    }
}
