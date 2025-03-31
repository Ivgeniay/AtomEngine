using System.Text;

namespace OpenglLib
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
            string systemName = rsFileInfo.SystemName;
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
            GenerateRenderBody(renderBodyBuilder, interfaceName, rsFileInfo, componentInfo);
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

        private static void GenerateRenderBody(StringBuilder builder, string interfaceName, RSFileInfo rsFileInfo, ComponentGeneratorInfo componentInfo)
        {
            builder.AppendLine($"                if (materialComponent.Material.Shader is {interfaceName} renderer)");
            builder.AppendLine("                {");
            builder.AppendLine("                    materialComponent.Material.Shader.Use();");

            if (componentInfo != null)
            {
                string componentVar = GetComponentVariableName(componentInfo.ComponentName);

                foreach (var field in componentInfo.Fields)
                {
                    if (field.IsUniform)
                    {
                        if (field.IsArray)
                        {
                            UniformArrayCase(builder, field, rsFileInfo, componentVar);
                        }
                        else
                        {
                            UniformCase(builder, field, rsFileInfo, componentVar);
                        }

                    }

                    if (field.IsCustomStruct) 
                    {
                        if (field.IsArray)
                        {
                            CustomStructArrayCase(builder, field, rsFileInfo, componentVar);
                        }
                        else
                        {
                            CustomStructCase(builder, field, rsFileInfo, componentVar);
                        }
                    }

                    if (field.IsUniformBlock)
                    {
                        UniformBlockCase(builder, field, rsFileInfo, componentVar);
                    }
                }
            }
            if (rsFileInfo.RequiredComponent.Contains("MeshComponent") ||
                rsFileInfo.RequiredComponent.Contains("AtomEngine.MeshComponent"))
            {
                builder.AppendLine();
                builder.AppendLine("                    meshComponent.Mesh.Draw(materialComponent.Material.Shader);");
            }
            builder.AppendLine("                }");
        }

        private static void UniformArrayCase(StringBuilder builder, ComponentGeneratorFieldInfo fieldInfo, RSFileInfo rsFileInfo, string componentVar)
        {
            if (fieldInfo.IsDirtySupport)
            {
                builder.AppendLine($"                    if ({componentVar}.IsDirty{fieldInfo.FieldName})");
                builder.AppendLine("                    {");
                for (var i = 0; i < fieldInfo.ArraySize; i++)
                {
                    builder.AppendLine($"                        renderer.{fieldInfo.FieldName}[{i}] = {componentVar}.{fieldInfo.FieldName}[{i}];");
                }
                builder.AppendLine($"                        {componentVar}.MakeClean();");
                builder.AppendLine("                    }");
            }
            else
            {
                for (var i = 0; i < fieldInfo.ArraySize; i++)
                {
                    builder.AppendLine($"                    renderer.{fieldInfo.FieldName}[{i}] = {componentVar}.{fieldInfo.FieldName}[{i}];");
                }
            }
        }
        private static void UniformCase(StringBuilder builder, ComponentGeneratorFieldInfo fieldInfo, RSFileInfo rsFileInfo, string componentVar)
        {
            if (fieldInfo.IsDirtySupport)
            {
                builder.AppendLine($"                    if ({componentVar}.IsDirty{fieldInfo.FieldName})");
                builder.AppendLine("                    {");
                builder.AppendLine($"                        renderer.{fieldInfo.FieldName} = {componentVar}.{fieldInfo.FieldName};");
                builder.AppendLine($"                        {componentVar}.MakeClean();");
                builder.AppendLine("                    }");
            }
            else
            {
                builder.AppendLine($"                    renderer.{fieldInfo.FieldName} = {componentVar}.{fieldInfo.FieldName};");
            }
        }
        private static void CustomStructArrayCase(StringBuilder builder, ComponentGeneratorFieldInfo fieldInfo, RSFileInfo rsFileInfo, string componentVar, int indentLevel = 5)
        {
            string indent = new string(' ', indentLevel * 4);

            GlslStructModel? currentStruct = rsFileInfo.Structures.FirstOrDefault(e => e.Name == fieldInfo.FieldType);
            if (currentStruct == null) return;

            int arraySize = fieldInfo.ArraySize;

            for (int i = 0; i < arraySize; i++)
            {
                if (fieldInfo.IsDirtySupport)
                {
                    builder.AppendLine($"{indent}if ({componentVar}.IsDirty{fieldInfo.FieldName})");
                    builder.AppendLine($"{indent}{{");
                    indentLevel++;
                    indent = new string(' ', indentLevel * 4);
                }

                string arrayElementPrefix = $"{fieldInfo.FieldName}[{i}]";
                ProcessStructFields(builder, currentStruct, rsFileInfo, componentVar, arrayElementPrefix, indentLevel);

                if (fieldInfo.IsDirtySupport)
                {
                    indentLevel--;
                    indent = new string(' ', indentLevel * 4);
                    if (i == arraySize - 1)
                    {
                        builder.AppendLine($"{indent}    {componentVar}.IsDirty{fieldInfo.FieldName} = false;");
                    }
                    builder.AppendLine($"{indent}}}");
                }
            }
        }
        private static void CustomStructCase(StringBuilder builder, ComponentGeneratorFieldInfo fieldInfo, RSFileInfo rsFileInfo, string componentVar, int indentLevel = 5)
        {
            string indent = new string(' ', indentLevel * 4);

            GlslStructModel? currentStruct = rsFileInfo.Structures.FirstOrDefault(e => e.Name == fieldInfo.FieldType);
            if (currentStruct == null) return;

            if (fieldInfo.IsDirtySupport)
            {
                builder.AppendLine($"{indent}if ({componentVar}.IsDirty{fieldInfo.FieldName})");
                builder.AppendLine($"{indent}{{");
                indentLevel++;
                indent = new string(' ', indentLevel * 4);
            }

            ProcessStructFields(builder, currentStruct, rsFileInfo, componentVar, fieldInfo.FieldName, indentLevel);

            if (fieldInfo.IsDirtySupport)
            {
                indentLevel--;
                indent = new string(' ', indentLevel * 4);
                builder.AppendLine($"{indent}    {componentVar}.IsDirty{fieldInfo.FieldName} = false;");
                builder.AppendLine($"{indent}}}");
            }
        }
        private static void UniformBlockCase(StringBuilder builder, ComponentGeneratorFieldInfo fieldInfo, RSFileInfo rsFileInfo, string componentVar)
        {
            if (fieldInfo.IsDirtySupport)
            {
                builder.AppendLine($"                    if ({componentVar}.IsDirty{fieldInfo.FieldName})");
                builder.AppendLine("                    {");
                builder.AppendLine($"                        renderer.{fieldInfo.FieldName} = {componentVar}.{fieldInfo.FieldName};");
                builder.AppendLine($"                        {componentVar}.MakeClean();");
                builder.AppendLine("                    }");
            }
            else
            {
                builder.AppendLine($"                    renderer.{fieldInfo.FieldName} = {componentVar}.{fieldInfo.FieldName};");
            }
        }
        private static void ProcessStructFields(StringBuilder builder, GlslStructModel structure, RSFileInfo rsFileInfo, string componentVar, string prefix, int indentLevel)
        {
            string indent = new string(' ', indentLevel * 4);

            foreach (var field in structure.Fields)
            {
                string rendererFieldPath = $"renderer.{prefix}.{field.Name}";
                string componentFieldPath = $"{componentVar}.{prefix}.{field.Name}";

                if (field.ArraySize.HasValue)
                {
                    ProcessArrayField(builder, field, rsFileInfo, componentVar, prefix, indentLevel);
                }
                else if (GlslParser.IsCustomType(field.Type, field.Type))
                {
                    GlslStructModel? nestedStruct = rsFileInfo.Structures.FirstOrDefault(e => e.Name == field.Type);
                    if (nestedStruct != null)
                    {
                        ProcessStructFields(builder, nestedStruct, rsFileInfo, componentVar, $"{prefix}.{field.Name}", indentLevel);
                    }
                }
                else
                {
                    builder.AppendLine($"{indent}{rendererFieldPath} = {componentFieldPath};");
                }
            }
        }
        private static void ProcessArrayField(StringBuilder builder, GlslStructFieldModel field, RSFileInfo rsFileInfo, string componentVar, string prefix, int indentLevel)
        {
            string indent = new string(' ', indentLevel * 4);
            int arraySize = field.ArraySize.Value;

            if (GlslParser.IsCustomType(field.Type, field.Type))
            {
                GlslStructModel? itemStruct = rsFileInfo.Structures.FirstOrDefault(e => e.Name == field.Type);
                if (itemStruct != null)
                {
                    for (int i = 0; i < arraySize; i++)
                    {
                        ProcessStructFields(builder, itemStruct, rsFileInfo, componentVar, $"{prefix}.{field.Name}[{i}]", indentLevel);
                    }
                }
            }
            else
            {
                for (int i = 0; i < arraySize; i++)
                {
                    string rendererFieldPath = $"renderer.{prefix}.{field.Name}[{i}]";
                    string componentFieldPath = $"{componentVar}.{prefix}.{field.Name}[{i}]";
                    builder.AppendLine($"{indent}{rendererFieldPath} = {componentFieldPath};");
                }
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
