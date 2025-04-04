using System.IO;
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

        public static string GenerateRenderSystemTemplate(RSFileInfo rsFileInfo)
        {
            string interfaceName = rsFileInfo.InterfaceName;
            string systemName = rsFileInfo.SystemName;
            var requiredComponents = rsFileInfo.RequiredComponent;
            string componentName = rsFileInfo.ComponentName;

            var mainBuilder = new StringBuilder();
            var contentBuilder = new StringBuilder();
            var classFieldsBuilder = new StringBuilder();
            var constructorBuilder = new StringBuilder();
            var renderMethodBuilder = new StringBuilder();
            var renderBodyBuilder = new StringBuilder();
            var otherMethodsBuilder = new StringBuilder();
            var queryComponentsBuilder = new StringBuilder();

            GenerateClassStructure(mainBuilder, systemName);
            GenerateContentStructure(contentBuilder);
            GenerateClassFields(classFieldsBuilder);
            GenerateConstructorStructure(constructorBuilder, systemName);
            GenerateRenderMethodStructure(renderMethodBuilder, componentName);
            GenerateRenderBody(renderBodyBuilder, interfaceName, rsFileInfo);
            GenerateOtherMethods(otherMethodsBuilder);
            GenerateQueryComponents(queryComponentsBuilder, requiredComponents, componentName);

            string constructorText = constructorBuilder.ToString()
                .Replace("/*QUERY_COMPONENTS*/", queryComponentsBuilder.ToString().TrimEnd());

            string renderMethodText = renderMethodBuilder.ToString()
                .Replace("/*RENDER_BODY*/", renderBodyBuilder.ToString().TrimEnd());

            string contentText = contentBuilder.ToString()
                .Replace("/*CLASS_FIELDS*/", classFieldsBuilder.ToString())
                .Replace("/*CONSTRUCTOR*/", constructorText)
                .Replace("/*RENDER_METHOD*/", renderMethodText)
                .Replace("/*OTHER_METHODS*/", otherMethodsBuilder.ToString());

            string result = mainBuilder.ToString()
                .Replace("/*CONTENT*/", contentText);

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

        private static void GenerateRenderMethodStructure(StringBuilder builder, string componentName)
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
            if (componentName != null)
            {
                string componentVar = GetComponentVariableName(componentName);
                builder.AppendLine($"                ref var {componentVar} = ref this.GetComponent<{componentName}>(entity);");
            }
            builder.AppendLine();
            builder.AppendLine(RENDER_BODY_PLACEHOLDER);
            builder.AppendLine("            }");
            builder.AppendLine("        }");
            builder.AppendLine();
        }

        private static void GenerateRenderBody(StringBuilder builder, string interfaceName, RSFileInfo rsFileInfo)
        {
            builder.AppendLine($"                if (materialComponent.Material.Shader is {interfaceName} renderer)");
            builder.AppendLine("                {");
            builder.AppendLine("                    materialComponent.Material.Shader.Use();");

            string componentVar = GetComponentVariableName(rsFileInfo.ComponentName);

            foreach (var uniform in rsFileInfo.Uniforms)
            {
                string fieldName = uniform.Name;
                bool isArray = uniform.ArraySize.HasValue;
                bool isCustomStruct = GlslParser.IsCustomType(uniform.CSharpTypeName, uniform.Type);
                bool isDirtySupport = uniform.Attributes.Any(a => a.Name.Equals("isdirty", StringComparison.OrdinalIgnoreCase) &&
                                                                 a.Value.Equals("true", StringComparison.OrdinalIgnoreCase));

                if (isArray)
                {
                    if (isCustomStruct)
                    {
                        GenerateCustomStructArrayRendering(builder, fieldName, componentVar, isDirtySupport, uniform.ArraySize.Value, rsFileInfo);
                    }
                    else
                    {
                        GenerateSimpleTypeArrayRendering(builder, fieldName, componentVar, isDirtySupport, uniform.ArraySize.Value);
                    }
                }
                else
                {
                    if (isCustomStruct)
                    {
                        GenerateCustomStructRendering(builder, fieldName, componentVar, isDirtySupport, rsFileInfo);
                    }
                    else
                    {
                        GenerateSimpleTypeRendering(builder, fieldName, componentVar, isDirtySupport);
                    }
                }
            }

            foreach (var block in rsFileInfo.UniformBlocks)
            {
                if (block.InstanceName != null)
                {
                    string fieldName = block.InstanceName;
                    bool isDirtySupport = block.Attributes.Any(a => a.Name.Equals("isdirty", StringComparison.OrdinalIgnoreCase) &&
                                                                  a.Value.Equals("true", StringComparison.OrdinalIgnoreCase));

                    GenerateUniformBlockRendering(builder, fieldName, componentVar, isDirtySupport);
                }
            }

            foreach (var structInstance in rsFileInfo.StructureInstances)
            {
                if (!structInstance.IsUniform)
                {
                    string instanceName = !string.IsNullOrEmpty(structInstance.InstanceName)
                        ? structInstance.InstanceName
                        : structInstance.OriginalInstanceName;
                    bool isDirtySupport = structInstance.Attributes.Any(a => a.Name.Equals("isdirty", StringComparison.OrdinalIgnoreCase) &&
                                                                  a.Value.Equals("true", StringComparison.OrdinalIgnoreCase));

                    if (structInstance.ArraySize.HasValue)
                    {
                        GenerateStructArrayInstanceRendering(builder, instanceName, componentVar, rsFileInfo, isDirtySupport);
                    }
                    else
                    {
                        GenerateStructInstanceRendering(builder, instanceName, componentVar, rsFileInfo, isDirtySupport);
                    }
                }
            }

            if (rsFileInfo.Uniforms.Any(e => e.Attributes.Any(a => 
                                                                a.Name.Equals("isdirty", StringComparison.OrdinalIgnoreCase) &&
                                                                a.Value.Equals("true", StringComparison.OrdinalIgnoreCase))) ||
                rsFileInfo.UniformBlocks.Any(e => e.Attributes.Any(a => 
                                                                a.Name.Equals("isdirty", StringComparison.OrdinalIgnoreCase) &&
                                                                a.Value.Equals("true", StringComparison.OrdinalIgnoreCase))) ||
                rsFileInfo.StructureInstances.Any(e => e.Attributes.Any(a => 
                                                                a.Name.Equals("isdirty", StringComparison.OrdinalIgnoreCase) &&
                                                                a.Value.Equals("true", StringComparison.OrdinalIgnoreCase))))
            {
                builder.AppendLine($"                    {componentVar}.MakeClean();");
                //{componentVar}.MakeClean();");
            }

            if (rsFileInfo.RequiredComponent.Contains("MeshComponent") ||
                rsFileInfo.RequiredComponent.Contains("AtomEngine.MeshComponent"))
            {
                builder.AppendLine();
                builder.AppendLine("                    meshComponent.Mesh.Draw(materialComponent.Material.Shader);");
            }
            builder.AppendLine("                }");
        }


        private static void GenerateStructInstanceRendering(StringBuilder builder, string fieldName, string componentVar, RSFileInfo rsFileInfo, bool isDirtySupport)
        {
            var structInstance = rsFileInfo.StructureInstances.FirstOrDefault(si =>
                si.InstanceName == fieldName || si.OriginalInstanceName == fieldName);

            if (structInstance != null && structInstance.Structure != null)
            {
                if (isDirtySupport)
                {
                    builder.AppendLine($"                    if ({componentVar}.IsDirty{fieldName})");
                    builder.AppendLine("                    {");
                    ProcessStructFields(builder, structInstance.Structure, rsFileInfo, componentVar, fieldName, 6);
                    builder.AppendLine("                    }");
                }
                else
                {
                    ProcessStructFields(builder, structInstance.Structure, rsFileInfo, componentVar, fieldName, 5);
                }
            }
            else
            {
                builder.AppendLine($"                    renderer.{fieldName} = {componentVar}.{fieldName};");
            }
        }

        private static void GenerateStructArrayInstanceRendering(StringBuilder builder, string fieldName, string componentVar, RSFileInfo rsFileInfo, bool isDirtySupport)
        {
            var structInstance = rsFileInfo.StructureInstances.FirstOrDefault(si =>
                si.InstanceName == fieldName || si.OriginalInstanceName == fieldName);

            if (structInstance != null && structInstance.Structure != null && structInstance.ArraySize.HasValue)
            {
                int arraySize = structInstance.ArraySize.Value;

                if (isDirtySupport)
                {
                    builder.AppendLine($"                    if ({componentVar}.IsDirty{fieldName})");
                    builder.AppendLine("                    {");
                    for (int i = 0; i < arraySize; i++)
                    {
                        if (GlslParser.IsCustomType(structInstance.Structure.Name, structInstance.Structure.Name))
                        {
                            ProcessStructFields(builder, structInstance.Structure, rsFileInfo, componentVar, $"{fieldName}[{i}]", 5);
                        }
                        else
                        {
                            builder.AppendLine($"                    renderer.{fieldName}[{i}] = {componentVar}.{fieldName}[{i}];");
                        }
                    }
                    builder.AppendLine("                    }");
                }
                else
                {
                    for (int i = 0; i < arraySize; i++)
                    {
                        if (GlslParser.IsCustomType(structInstance.Structure.Name, structInstance.Structure.Name))
                        {
                            ProcessStructFields(builder, structInstance.Structure, rsFileInfo, componentVar, $"{fieldName}[{i}]", 5);
                        }
                        else
                        {
                            builder.AppendLine($"                    renderer.{fieldName}[{i}] = {componentVar}.{fieldName}[{i}];");
                        }
                    }
                }
            }

        }

        private static void GenerateCustomStructArrayRendering(StringBuilder builder, string fieldName, string componentVar, bool isDirtySupport, int arraySize, RSFileInfo rsFileInfo)
        {
            var structModel = rsFileInfo.Structures.FirstOrDefault(s => s.Name == fieldName);

            if (isDirtySupport)
            {
                builder.AppendLine($"                    if ({componentVar}.IsDirty{fieldName})");
                builder.AppendLine("                    {");
                for (var i = 0; i < arraySize; i++)
                {
                    if (structModel != null)
                    {
                        ProcessStructFields(builder, structModel, rsFileInfo, componentVar, $"{fieldName}[{i}]", 6);
                    }
                    else
                    {
                        builder.AppendLine($"                        renderer.{fieldName}[{i}] = {componentVar}.{fieldName}[{i}];");
                    }
                }
                //builder.AppendLine($"                        {componentVar}.MakeClean();");
                builder.AppendLine("                    }");
            }
            else
            {
                for (var i = 0; i < arraySize; i++)
                {
                    if (structModel != null)
                    {
                        ProcessStructFields(builder, structModel, rsFileInfo, componentVar, $"{fieldName}[{i}]", 5);
                    }
                    else
                    {
                        builder.AppendLine($"                    renderer.{fieldName}[{i}] = {componentVar}.{fieldName}[{i}];");
                    }
                }
            }
        }

        private static void GenerateSimpleTypeArrayRendering(StringBuilder builder, string fieldName, string componentVar, bool isDirtySupport, int arraySize)
        {
            if (isDirtySupport)
            {
                builder.AppendLine($"                    if ({componentVar}.IsDirty{fieldName})");
                builder.AppendLine("                    {");
                for (var i = 0; i < arraySize; i++)
                {
                    builder.AppendLine($"                        renderer.{fieldName}[{i}] = {componentVar}.{fieldName}[{i}];");
                }
                //builder.AppendLine($"                        {componentVar}.MakeClean();");
                builder.AppendLine("                    }");
            }
            else
            {
                for (var i = 0; i < arraySize; i++)
                {
                    builder.AppendLine($"                    renderer.{fieldName}[{i}] = {componentVar}.{fieldName}[{i}];");
                }
            }
        }

        private static void GenerateCustomStructRendering(StringBuilder builder, string fieldName, string componentVar, bool isDirtySupport, RSFileInfo rsFileInfo)
        {
            var structModel = rsFileInfo.Structures.FirstOrDefault(s => s.Name == fieldName);
            if (structModel != null)
            {
                if (isDirtySupport)
                {
                    builder.AppendLine($"                    if ({componentVar}.IsDirty{fieldName})");
                    builder.AppendLine("                    {");
                    ProcessStructFields(builder, structModel, rsFileInfo, componentVar, fieldName, 5);
                    //builder.AppendLine($"                        {componentVar}.IsDirty{fieldName} = false;");
                    builder.AppendLine("                    }");
                }
                else
                {
                    ProcessStructFields(builder, structModel, rsFileInfo, componentVar, fieldName, 5);
                }
            }
            else
            {
                if (isDirtySupport)
                {
                    builder.AppendLine($"                    if ({componentVar}.IsDirty{fieldName})");
                    builder.AppendLine("                    {");
                    builder.AppendLine($"                        renderer.{fieldName} = {componentVar}.{fieldName};");
                    builder.AppendLine($"                        {componentVar}.MakeClean();");
                    builder.AppendLine("                    }");
                }
                else
                {
                    builder.AppendLine($"                    renderer.{fieldName} = {componentVar}.{fieldName};");
                }
            }
        }

        private static void GenerateSimpleTypeRendering(StringBuilder builder, string fieldName, string componentVar, bool isDirtySupport)
        {
            if (isDirtySupport)
            {
                builder.AppendLine($"                    if ({componentVar}.IsDirty{fieldName})");
                builder.AppendLine("                    {");
                builder.AppendLine($"                        renderer.{fieldName} = {componentVar}.{fieldName};");
                //builder.AppendLine($"                        {componentVar}.MakeClean();");
                builder.AppendLine("                    }");
            }
            else
            {
                builder.AppendLine($"                    renderer.{fieldName} = {componentVar}.{fieldName};");
            }
        }

        private static void GenerateUniformBlockRendering(StringBuilder builder, string fieldName, string componentVar, bool isDirtySupport)
        {
            if (isDirtySupport)
            {
                builder.AppendLine($"                    if ({componentVar}.IsDirty{fieldName})");
                builder.AppendLine("                    {");
                builder.AppendLine($"                        renderer.{fieldName} = {componentVar}.{fieldName};");
                //builder.AppendLine($"                        {componentVar}.MakeClean();");
                builder.AppendLine("                    }");
            }
            else
            {
                builder.AppendLine($"                    renderer.{fieldName} = {componentVar}.{fieldName};");
            }
        }


        private static void ProcessStructFields(StringBuilder builder, GlslStructModel structure, RSFileInfo rsFileInfo, string componentVar, string prefix, int indentLevel)
        {
            string indent = new string(' ', indentLevel * 4);

            foreach (var field in structure.Fields)
            {
                string fieldPrefix = prefix;
                if (prefix.IndexOf('.') == -1)
                {
                    var structInstance = rsFileInfo.StructureInstances
                        .FirstOrDefault(si => si.OriginalInstanceName == prefix);

                    if (structInstance != null && !string.IsNullOrEmpty(structInstance.InstanceName))
                    {
                        fieldPrefix = structInstance.InstanceName;
                    }
                }

                string rendererFieldPath = $"renderer.{fieldPrefix}.{field.Name}";
                string componentFieldPath = $"{componentVar}.{fieldPrefix}.{field.Name}";

                if (field.ArraySize.HasValue)
                {
                    ProcessArrayField(builder, field, rsFileInfo, componentVar, fieldPrefix, indentLevel);
                }
                else if (GlslParser.IsCustomType(field.Type, field.Type))
                {
                    var nestedStruct = rsFileInfo.Structures.FirstOrDefault(s => s.Name == field.Type);
                    if (nestedStruct != null)
                    {
                        ProcessStructFields(builder, nestedStruct, rsFileInfo, componentVar, $"{fieldPrefix}.{field.Name}", indentLevel);
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

            string fieldPrefix = prefix;
            if (prefix.IndexOf('.') == -1)
            {
                var structInstance = rsFileInfo.StructureInstances
                    .FirstOrDefault(si => si.OriginalInstanceName == prefix);

                if (structInstance != null && !string.IsNullOrEmpty(structInstance.InstanceName))
                {
                    fieldPrefix = structInstance.InstanceName;
                }
            }

            if (GlslParser.IsCustomType(field.Type, field.Type))
            {
                var itemStruct = rsFileInfo.Structures.FirstOrDefault(s => s.Name == field.Type);
                if (itemStruct != null)
                {
                    for (int i = 0; i < arraySize; i++)
                    {
                        ProcessStructFields(builder, itemStruct, rsFileInfo, componentVar, $"{fieldPrefix}.{field.Name}[{i}]", indentLevel);
                    }
                }
            }
            else
            {
                for (int i = 0; i < arraySize; i++)
                {
                    string rendererFieldPath = $"renderer.{fieldPrefix}.{field.Name}[{i}]";
                    string componentFieldPath = $"{componentVar}.{fieldPrefix}.{field.Name}[{i}]";
                    builder.AppendLine($"{indent}{rendererFieldPath} = {componentFieldPath};");
                }
            }
        }



        private static void GenerateQueryComponents(StringBuilder builder, List<string> requiredComponents, string componentName)
        {
            foreach (var component in requiredComponents)
            {
                builder.AppendLine($"                .With<{component}>()");
            }

            if (!string.IsNullOrEmpty(componentName))
            {
                builder.AppendLine($"                .With<{componentName}>()");
            }
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

    }

}
