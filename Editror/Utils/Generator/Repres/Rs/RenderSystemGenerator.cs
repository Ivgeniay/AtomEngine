using System.Collections.Generic;
using System.Text;
using System.Linq;
using OpenglLib;

namespace Editor
{
    public static class RenderSystemGenerator
    {
        private const string CONTENT_PLACEHOLDER = "/*CONTENT*/";
        private const string CLASS_FIELDS_PLACEHOLDER = "/*CLASS_FIELDS*/";
        private const string CONSTRUCTOR_PLACEHOLDER = "/*CONSTRUCTOR*/";
        private const string CONSTRUCTOR_BODY_PLACEHOLDER = "/*CONSTRUCTOR_BODY*/";
        private const string RENDER_METHOD_PLACEHOLDER = "/*RENDER_METHOD*/";
        private const string RENDER_BODY_PLACEHOLDER = "/*RENDER_BODY*/";
        private const string RENDERER_BODY_PLACEHOLDER = "/*RENDERER_BODY*/";
        private const string COMPONENT_FIELDS_PLACEHOLDER = "/*COMPONENT_FIELDS*/";
        private const string OTHER_METHODS_PLACEHOLDER = "/*OTHER_METHODS*/";
        private const string QUERY_COMPONENTS_PLACEHOLDER = "/*QUERY_COMPONENTS*/";

        public static string GenerateRenderSystemTemplate(RSFileInfo rsFileInfo, ComponentGeneratorInfo componentInfo = null)
        {
            string interfaceName = rsFileInfo.InterfaceName;
            string systemName = GetSystemNameFromInterface(interfaceName);
            var requiredComponents = rsFileInfo.RequiredComponent;

            var mainBuilder = new StringBuilder();
            var contentBuilder = new StringBuilder();
            var classFieldsBuilder = new StringBuilder();
            var constructorBuilder = new StringBuilder();
            var constructorBodyBuilder = new StringBuilder();
            var renderMethodBuilder = new StringBuilder();
            var renderBodyBuilder = new StringBuilder();
            var rendererBodyBuilder = new StringBuilder();
            var componentFieldsBuilder = new StringBuilder();
            var queryComponentsBuilder = new StringBuilder();
            var otherMethodsBuilder = new StringBuilder();

            GenerateClassStructure(mainBuilder, systemName);
            GenerateContentStructure(contentBuilder);
            GenerateClassFields(classFieldsBuilder);
            GenerateConstructorStructure(constructorBuilder, systemName);
            GenerateConstructorBody(constructorBodyBuilder, requiredComponents, componentInfo);
            GenerateRenderMethodStructure(renderMethodBuilder, componentInfo);
            GenerateRenderBody(renderBodyBuilder, interfaceName, componentInfo);
            GenerateOtherMethods(otherMethodsBuilder);
            GenerateQueryComponents(queryComponentsBuilder, requiredComponents, componentInfo);

            string constructorText = constructorBuilder.ToString()
                .Replace(CONSTRUCTOR_BODY_PLACEHOLDER, constructorBodyBuilder.ToString().TrimEnd())
                .Replace(QUERY_COMPONENTS_PLACEHOLDER, queryComponentsBuilder.ToString().TrimEnd());

            string renderMethodText = renderMethodBuilder.ToString()
                .Replace(RENDER_BODY_PLACEHOLDER, renderBodyBuilder.ToString().TrimEnd());

            string contentText = contentBuilder.ToString()
                .Replace(CLASS_FIELDS_PLACEHOLDER, classFieldsBuilder.ToString())
                .Replace(CONSTRUCTOR_PLACEHOLDER, constructorText)
                .Replace(RENDER_METHOD_PLACEHOLDER, renderMethodText)
                .Replace(OTHER_METHODS_PLACEHOLDER, otherMethodsBuilder.ToString());

            string result = mainBuilder.ToString()
                .Replace(CONTENT_PLACEHOLDER, contentText);

            return result;
        }

        private static void GenerateClassStructure(StringBuilder builder, string systemName)
        {
            builder.AppendLine($"{GeneratorConst.GetDefaultNamespaces()}");
            builder.AppendLine();
            builder.AppendLine($"{GeneratorConst.GetUserScriptNamespace()}");
            builder.AppendLine("{");
            builder.AppendLine($"    public class {systemName} : IRenderSystem");
            builder.AppendLine("    {");
            builder.AppendLine(CONTENT_PLACEHOLDER);
            builder.AppendLine("    }");
            builder.AppendLine("}");
        }

        private static void GenerateContentStructure(StringBuilder builder)
        {
            builder.AppendLine(CLASS_FIELDS_PLACEHOLDER);
            builder.AppendLine(CONSTRUCTOR_PLACEHOLDER);
            builder.AppendLine(RENDER_METHOD_PLACEHOLDER);
            builder.AppendLine(OTHER_METHODS_PLACEHOLDER);
        }

        private static void GenerateClassFields(StringBuilder builder)
        {
            builder.AppendLine("        public IWorld World { get; set; }");
            builder.AppendLine("        private QueryEntity queryRendererEntities;");
            builder.AppendLine();
        }

        private static void GenerateConstructorStructure(StringBuilder builder, string systemName)
        {
            builder.AppendLine($"        public {systemName}(IWorld world)");
            builder.AppendLine("        {");
            builder.AppendLine("            World = world;");
            builder.AppendLine();
            builder.AppendLine("            queryRendererEntities = this.CreateEntityQuery()");
            builder.AppendLine("                .With<MeshComponent>()");
            builder.AppendLine("                .With<MaterialComponent>()");
            builder.AppendLine(QUERY_COMPONENTS_PLACEHOLDER);
            builder.AppendLine("                ;");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        private static void GenerateConstructorBody(StringBuilder builder, List<string> requiredComponents, ComponentGeneratorInfo componentInfo)
        {
            
        }

        private static void GenerateQueryComponents(StringBuilder builder, List<string> requiredComponents, ComponentGeneratorInfo componentInfo)
        {
            foreach (var component in requiredComponents)
            {
                builder.AppendLine($"                .With<{component}>()");
            }

            if (componentInfo != null)
            {
                builder.AppendLine($"                .With<{componentInfo.ComponentName}>()");
            }
        }

        private static void GenerateRenderMethodStructure(StringBuilder builder, ComponentGeneratorInfo componentInfo)
        {
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
            if (componentInfo != null)
            {
                string componentVar = GetComponentVariableName(componentInfo.ComponentName);
                builder.AppendLine($"                ref var {componentVar} = ref this.GetComponent<{componentInfo.ComponentName}>(entity);");
            }
            builder.AppendLine();
            builder.AppendLine(RENDER_BODY_PLACEHOLDER);
            builder.AppendLine("            }");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        private static void GenerateRenderBody(StringBuilder builder, string interfaceName, ComponentGeneratorInfo componentInfo)
        {
            builder.AppendLine($"                if (materialComponent.Material.Shader is {interfaceName} renderer)");
            builder.AppendLine("                {");

            if (componentInfo != null)
            {
                string componentVar = GetComponentVariableName(componentInfo.ComponentName);

                foreach (var field in componentInfo.Fields)
                {
                    if (field.IsArray)
                    {
                        for (var i = 0; i < field.ArraySize; i++)
                        {
                            builder.AppendLine($"                    renderer.{field.FieldName}[{i}] = {componentVar}.{field.FieldName}[{i}];");
                        }
                    }
                    else if (field.IsDirtySupport)
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

            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine("                    meshComponent.Mesh.Draw(materialComponent.Material.Shader);");
            builder.AppendLine("                }");
        }

        private static void GenerateComponentFields(StringBuilder builder, ComponentGeneratorInfo componentInfo, string componentVar)
        {
            foreach (var field in componentInfo.Fields)
            {
                if (field.IsArray)
                {
                    ArrayFieldCase(builder, field, componentVar);
                }
                else if (field.IsDirtySupport)
                {
                    DirtyFieldCase(builder, field, componentVar);
                }
                else
                {
                    SimpleFieldCase(builder, field, componentVar);
                }
            }

            if (componentInfo.Fields.Any(f => f.IsDirtySupport))
            {
                builder.AppendLine($"                    {componentVar}.MakeClean();");
            }
        }

        private static void SimpleFieldCase(StringBuilder builder, ComponentGeneratorFieldInfo field, string componentVar)
        {
            builder.AppendLine($"                    renderer.{field.FieldName} = {componentVar}.{field.FieldName};");
        }

        private static void DirtyFieldCase(StringBuilder builder, ComponentGeneratorFieldInfo field, string componentVar)
        {
            builder.AppendLine($"                    if ({componentVar}.IsDirty{field.FieldName})");
            builder.AppendLine("                    {");
            builder.AppendLine($"                        renderer.{field.FieldName} = {componentVar}.{field.FieldName};");
            builder.AppendLine("                    }");
        }

        private static void ArrayFieldCase(StringBuilder builder, ComponentGeneratorFieldInfo field, string componentVar)
        {
            for (var i = 0; i < field.ArraySize; i++)
            {
                builder.AppendLine($"                    renderer.{field.FieldName}[{i}] = {componentVar}.{field.FieldName}[{i}];");
            }
        }

        private static void GenerateOtherMethods(StringBuilder builder)
        {
            builder.AppendLine("        public void Resize(Vector2 size)");
            builder.AppendLine("        {");
            builder.AppendLine("            ");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        public void Initialize()");
            builder.AppendLine("        {");
            builder.AppendLine("            ");
            builder.AppendLine("        }");
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
