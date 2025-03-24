using System.Collections.Generic;
using Editor.Utils.Generator;
using System.Linq;
using System.Text;
using AtomEngine;
using System.IO;
using EngineLib;
using OpenglLib;
using System;

namespace Editor
{
    public static class ShaderCodeRepresentationGenerator
    {
        public static string GenerateRepresentationFromSource(string representationName, string vertexSource, string fragmentSource, 
            string outputDirectory, List<UniformBlockStructure> uniformBlocks, List<RSFileInfo> rsFiles, string sourceGuid = null, 
            string sourcePath = null)
        {
            if (string.IsNullOrEmpty(sourceGuid) && !string.IsNullOrEmpty(sourcePath))
                sourceGuid = ServiceHub.Get<EditorMetadataManager>().GetMetadata(sourcePath)?.Guid;

            GlslParser.ValidateMainFunctions(vertexSource, fragmentSource);
            var uniforms = GlslParser.ExtractUniforms(vertexSource + "\n" + fragmentSource);
            var representationCode = GenerateRepresentationClass(representationName, vertexSource, fragmentSource, uniforms, uniformBlocks, sourceGuid, rsFiles);
            var representationFilePath = Path.Combine(outputDirectory, $"{representationName}{GlslCodeGenerator.LABLE}.cs");
            File.WriteAllText(representationFilePath, representationCode, Encoding.UTF8);

            return $"{representationName}Representation";
        }

        public static void GenerateUniformBlockClass(UniformBlockStructure block, string outputDirectory, string sourceGuid, List<GlslStructure> structures)
        {
            List<GlslStructure> usedStructures = GetUsedStructures(block, structures);
            if (usedStructures.Count > 0)
            {
                GenerateStructsForUbo(usedStructures, outputDirectory, sourceGuid);
            }

            var builder = new StringBuilder();
            WriteGeneratedCodeHeader(builder, sourceGuid);
            builder.AppendLine("using System.Runtime.InteropServices;");
            builder.AppendLine("using Silk.NET.Maths;");
            builder.AppendLine("using AtomEngine;");
            builder.AppendLine();
            builder.AppendLine("namespace OpenglLib");
            builder.AppendLine("{");

            bool hasComplexTypes = HasComplexTypes(block, structures);

            if (hasComplexTypes)
            {
                int totalSize = CalculateTotalSize(block, structures);
                builder.AppendLine($"    [StructLayout(LayoutKind.Explicit, Size = {totalSize})]");
                builder.AppendLine($"    public unsafe struct {block.CSharpTypeName} : IDataSerializable");
                builder.AppendLine("    {");

                int currentOffset = 0;

                foreach (var (type, name, arraySize) in block.Fields)
                {
                    var csharpType = GlslParser.MapGlslTypeToCSharp(type);
                    bool isCustomType = GlslParser.IsCustomType(csharpType, type);

                    int alignment = GetTypeAlignment(type, csharpType, structures);
                    currentOffset = AlignOffset(currentOffset, alignment);
                    int size = GetTypeSize(type, csharpType, arraySize, structures);

                    if (isCustomType)
                    {
                        bool isUserStructure = structures.Any(s => s.Name == type);
                        string fieldType = isUserStructure ? $"{type}_Std140" : csharpType;

                        if (arraySize.HasValue)
                        {
                            for (int i = 0; i < arraySize.Value; i++)
                            {
                                int elementSize = GetTypeSize(type, csharpType, null, structures);
                                int elementAlignedSize = AlignSize(elementSize, 16);
                                int elemOffset = currentOffset + i * elementAlignedSize;

                                builder.AppendLine($"        [FieldOffset({elemOffset})]");
                                builder.AppendLine($"        public {fieldType} {name}_{i};");
                            }

                            builder.AppendLine();
                            builder.AppendLine($"        public {fieldType} Get{name}(int index)");
                            builder.AppendLine("        {");
                            builder.AppendLine($"            switch (index)");
                            builder.AppendLine("            {");

                            for (int i = 0; i < arraySize.Value; i++)
                            {
                                builder.AppendLine($"                case {i}: return {name}_{i};");
                            }

                            builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
                            builder.AppendLine("            }");
                            builder.AppendLine("        }");

                            builder.AppendLine();
                            builder.AppendLine($"        public void Set{name}(int index, {fieldType} value)");
                            builder.AppendLine("        {");
                            builder.AppendLine($"            switch (index)");
                            builder.AppendLine("            {");

                            for (int i = 0; i < arraySize.Value; i++)
                            {
                                builder.AppendLine($"                case {i}: {name}_{i} = value; break;");
                            }

                            builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
                            builder.AppendLine("            }");
                            builder.AppendLine("        }");
                        }
                        else
                        {
                            builder.AppendLine($"        [FieldOffset({currentOffset})]");
                            builder.AppendLine($"        public {fieldType} {name};");
                        }
                    }
                    else
                    {
                        if (arraySize.HasValue)
                        {
                            if (IsFixedArrayType(type))
                            {
                                builder.AppendLine($"        [FieldOffset({currentOffset})]");
                                builder.AppendLine($"        public fixed {csharpType} {name}[{arraySize.Value}];");
                            }
                            else
                            {
                                for (int i = 0; i < arraySize.Value; i++)
                                {
                                    int elementSize = GetTypeSize(type, csharpType, null, structures);
                                    int elementAlignedSize = AlignSize(elementSize, 16);
                                    int elemOffset = currentOffset + i * elementAlignedSize;

                                    builder.AppendLine($"        [FieldOffset({elemOffset})]");
                                    builder.AppendLine($"        public {csharpType} {name}_{i};");
                                }

                                builder.AppendLine();
                                builder.AppendLine($"        public {csharpType} Get{name}(int index)");
                                builder.AppendLine("        {");
                                builder.AppendLine($"            switch (index)");
                                builder.AppendLine("            {");

                                for (int i = 0; i < arraySize.Value; i++)
                                {
                                    builder.AppendLine($"                case {i}: return {name}_{i};");
                                }

                                builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
                                builder.AppendLine("            }");
                                builder.AppendLine("        }");

                                builder.AppendLine();
                                builder.AppendLine($"        public void Set{name}(int index, {csharpType} value)");
                                builder.AppendLine("        {");
                                builder.AppendLine($"            switch (index)");
                                builder.AppendLine("            {");

                                for (int i = 0; i < arraySize.Value; i++)
                                {
                                    builder.AppendLine($"                case {i}: {name}_{i} = value; break;");
                                }

                                builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
                                builder.AppendLine("            }");
                                builder.AppendLine("        }");
                            }
                        }
                        else
                        {
                            builder.AppendLine($"        [FieldOffset({currentOffset})]");
                            builder.AppendLine($"        public {csharpType} {name};");
                        }
                    }

                    currentOffset += size;
                    builder.AppendLine();
                }
            }
            else
            {
                builder.AppendLine("    [StructLayout(LayoutKind.Sequential)]");
                builder.AppendLine($"    public unsafe struct {block.CSharpTypeName} : IDataSerializable");
                builder.AppendLine("    {");

                foreach (var (type, name, arraySize) in block.Fields)
                {
                    var csharpType = GlslParser.MapGlslTypeToCSharp(type);

                    if (arraySize.HasValue)
                    {
                        builder.AppendLine($"        [MarshalAs(UnmanagedType.ByValArray, SizeConst = {arraySize.Value})]");
                        builder.AppendLine($"        public {csharpType}[] {name};");
                    }
                    else
                    {
                        builder.AppendLine($"        public {csharpType} {name};");
                    }
                }
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");

            string blockFilePath = Path.Combine(outputDirectory, $"UBO.{block.CSharpTypeName}.g.cs");
            string blockCode = builder.ToString();
            File.WriteAllText(blockFilePath, blockCode, Encoding.UTF8);
        }

        private static List<GlslStructure> GetUsedStructures(UniformBlockStructure block, List<GlslStructure> allStructures)
        {
            var result = new List<GlslStructure>();
            var processedTypes = new HashSet<string>();

            void CollectStructureAndDependencies(string typeName)
            {
                if (processedTypes.Contains(typeName))
                    return;

                var structure = allStructures.FirstOrDefault(s => s.Name == typeName);
                if (structure == null)
                    return;

                processedTypes.Add(typeName);
                result.Add(structure);

                foreach (var (fieldType, _, _) in structure.Fields)
                {
                    if (allStructures.Any(s => s.Name == fieldType))
                    {
                        CollectStructureAndDependencies(fieldType);
                    }
                }
            }

            foreach (var (type, _, _) in block.Fields)
            {
                if (allStructures.Any(s => s.Name == type))
                {
                    CollectStructureAndDependencies(type);
                }
            }

            return result;
        }

        private static bool HasComplexTypes(UniformBlockStructure block, List<GlslStructure> structures)
        {
            foreach (var (type, _, arraySize) in block.Fields)
            {
                var csharpType = GlslParser.MapGlslTypeToCSharp(type);
                var isCustomType = GlslParser.IsCustomType(csharpType, type);

                if (isCustomType || arraySize.HasValue || structures.Any(s => s.Name == type))
                    return true;
            }
            return false;
        }

        private static int GetTypeAlignment(string type, string csharpType, List<GlslStructure> structures)
        {
            if (type is "bool" or "int" or "uint" or "float")
                return 4;

            return type switch
            {
                "double" => 8,
                "bvec2" or "ivec2" or "uvec2" or "vec2" => 8,
                "bvec3" or "ivec3" or "uvec3" or "vec3" or
                "bvec4" or "ivec4" or "uvec4" or "vec4" => 16,

                var t when t.StartsWith("mat") => 16,

                var t when structures.Any(s => s.Name == t) => CalculateStructureAlignment(t, structures),
                _ => 4
            };
        }

        private static int GetTypeSize(string type, string csharpType, int? arraySize, List<GlslStructure> structures)
        {
            int baseSize = type switch
            {
                "bool" or "int" or "uint" or "float" => 4,
                "double" => 8,

                "bvec2" or "ivec2" or "uvec2" or "vec2" => 8,
                "bvec3" or "ivec3" or "uvec3" or "vec3" => 12,
                "bvec4" or "ivec4" or "uvec4" or "vec4" => 16,

                "mat2" or "mat2x2" => 2 * 16, 
                "mat2x3"           => 2 * 16, 
                "mat2x4"           => 2 * 16, 
                "mat3" or "mat3x3" => 3 * 16, 
                "mat3x2"           => 3 * 16, 
                "mat3x4"           => 3 * 16, 
                "mat4" or "mat4x4" => 4 * 16, 
                "mat4x2"           => 4 * 16, 
                "mat4x3"           => 4 * 16, 

                var t when structures.Any(s => s.Name == t) => EstimateStructureSize(t, structures),
                _ => 4
            };

            if (arraySize.HasValue)
            {
                int alignedElementSize = AlignSize(baseSize, 16);
                return alignedElementSize * arraySize.Value;
            }

            return baseSize;
        }

        private static int EstimateStructureSize(string structureName, List<GlslStructure> structures)
        {
            var structure = structures.FirstOrDefault(s => s.Name == structureName);
            if (structure == null)
            {
                Type type = ServiceHub.Get<AssemblyManager>().FindType(structureName, false);
                if (type != null)
                {
                    return System.Runtime.InteropServices.Marshal.SizeOf(type);
                }
                return 64;
            }

            int totalSize = 0;
            int maxAlignment = 4;

            foreach (var (fieldType, _, fieldArraySize) in structure.Fields)
            {
                var csharpType = GlslParser.MapGlslTypeToCSharp(fieldType);

                int fieldAlignment = GetTypeAlignment(fieldType, csharpType, structures);
                maxAlignment = Math.Max(maxAlignment, fieldAlignment);
                totalSize = AlignOffset(totalSize, fieldAlignment);
                totalSize += GetTypeSize(fieldType, csharpType, fieldArraySize, structures);
            }
            return AlignSize(totalSize, maxAlignment);
        }

        private static bool IsFixedArrayType(string type)
        {
            return type is "bool" or "int" or "uint" or "float" or "double";
        }

        private static int CalculateTotalSize(UniformBlockStructure block, List<GlslStructure> structures)
        {
            int totalSize = 0;
            int maxAlignment = 4;

            foreach (var (type, _, arraySize) in block.Fields)
            {
                var csharpType = GlslParser.MapGlslTypeToCSharp(type);

                int alignment = GetTypeAlignment(type, csharpType, structures);
                maxAlignment = Math.Max(maxAlignment, alignment);
                totalSize = AlignOffset(totalSize, alignment);
                totalSize += GetTypeSize(type, csharpType, arraySize, structures);
            }
            return AlignSize(totalSize, maxAlignment);
        }

        private static int CalculateStructureAlignment(string structureName, List<GlslStructure> structures)
        {
            var structure = structures.FirstOrDefault(s => s.Name == structureName);
            if (structure == null)
                return 16;

            int maxAlignment = 4;

            foreach (var (fieldType, _, _) in structure.Fields)
            {
                var csharpType = GlslParser.MapGlslTypeToCSharp(fieldType);
                int fieldAlignment = GetTypeAlignment(fieldType, csharpType, structures);
                maxAlignment = Math.Max(maxAlignment, fieldAlignment);
            }

            return maxAlignment;
        }

        private static int AlignOffset(int offset, int alignment)
        {
            return (offset + alignment - 1) / alignment * alignment;
        }

        private static int AlignSize(int size, int alignment)
        {
            return (size + alignment - 1) / alignment * alignment;
        }

        private static void GenerateStructsForUbo(List<GlslStructure> structures, string outputDirectory, string sourceGuid)
        {
            foreach (var structure in structures)
            {
                var structName = $"{structure.Name}_Std140";
                var builder = new StringBuilder();
                WriteGeneratedCodeHeader(builder, sourceGuid);

                builder.AppendLine("using System.Runtime.InteropServices;");
                builder.AppendLine("using Silk.NET.Maths;");
                builder.AppendLine();
                builder.AppendLine("namespace OpenglLib");
                builder.AppendLine("{");

                int totalSize = CalculateStructureSize(structure.Name, structures);
                builder.AppendLine($"    [StructLayout(LayoutKind.Explicit, Size = {totalSize})]");
                builder.AppendLine($"    public struct {structName}");
                builder.AppendLine("    {");

                int currentOffset = 0;

                foreach (var (type, name, arraySize) in structure.Fields)
                {
                    var csharpType = GlslParser.MapGlslTypeToCSharp(type);
                    var isCustomType = GlslParser.IsCustomType(csharpType, type);

                    int alignment = GetTypeAlignment(type, csharpType, structures);
                    currentOffset = AlignOffset(currentOffset, alignment);

                    if (isCustomType && structures.Any(s => s.Name == type))
                    {
                        if (arraySize.HasValue)
                        {
                            for (int i = 0; i < arraySize.Value; i++)
                            {
                                int elementSize = GetTypeSize(type, csharpType, null, structures);
                                int elementAlignedSize = AlignSize(elementSize, 16);
                                int elemOffset = currentOffset + i * elementAlignedSize;

                                builder.AppendLine($"        [FieldOffset({elemOffset})]");
                                builder.AppendLine($"        public {type}_Std140 {name}_{i};");
                            }

                            builder.AppendLine();
                            builder.AppendLine($"        public {type}_Std140 Get{name}(int index)");
                            builder.AppendLine("        {");
                            builder.AppendLine($"            switch (index)");
                            builder.AppendLine("            {");

                            for (int i = 0; i < arraySize.Value; i++)
                            {
                                builder.AppendLine($"                case {i}: return {name}_{i};");
                            }

                            builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
                            builder.AppendLine("            }");
                            builder.AppendLine("        }");

                            builder.AppendLine();
                            builder.AppendLine($"        public void Set{name}(int index, {type}_Std140 value)");
                            builder.AppendLine("        {");
                            builder.AppendLine($"            switch (index)");
                            builder.AppendLine("            {");

                            for (int i = 0; i < arraySize.Value; i++)
                            {
                                builder.AppendLine($"                case {i}: {name}_{i} = value; break;");
                            }

                            builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
                            builder.AppendLine("            }");
                            builder.AppendLine("        }");
                        }
                        else
                        {
                            builder.AppendLine($"        [FieldOffset({currentOffset})]");
                            builder.AppendLine($"        public {type}_Std140 {name};");
                        }
                    }
                    else if (arraySize.HasValue)
                    {
                        if (IsFixedArrayType(type))
                        {
                            for (int i = 0; i < arraySize.Value; i++)
                            {
                                int elementOffset = currentOffset + i * 16;

                                builder.AppendLine($"        [FieldOffset({elementOffset})]");
                                builder.AppendLine($"        public {csharpType} {name}_{i};");
                            }
                            builder.AppendLine();
                            builder.AppendLine($"        public {csharpType} Get{name}(int index)");
                            builder.AppendLine("        {");
                            builder.AppendLine($"            switch (index)");
                            builder.AppendLine("            {");

                            for (int i = 0; i < arraySize.Value; i++)
                            {
                                builder.AppendLine($"                case {i}: return {name}_{i};");
                            }

                            builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
                            builder.AppendLine("            }");
                            builder.AppendLine("        }");

                            builder.AppendLine();
                            builder.AppendLine($"        public void Set{name}(int index, {csharpType} value)");
                            builder.AppendLine("        {");
                            builder.AppendLine($"            switch (index)");
                            builder.AppendLine("            {");

                            for (int i = 0; i < arraySize.Value; i++)
                            {
                                builder.AppendLine($"                case {i}: {name}_{i} = value; break;");
                            }

                            builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
                            builder.AppendLine("            }");
                            builder.AppendLine("        }");
                        }
                        else
                        {
                            for (int i = 0; i < arraySize.Value; i++)
                            {
                                int elementSize = GetTypeSize(type, csharpType, null, structures);
                                int elementAlignedSize = AlignSize(elementSize, 16);
                                int elemOffset = currentOffset + i * elementAlignedSize;

                                builder.AppendLine($"        [FieldOffset({elemOffset})]");
                                builder.AppendLine($"        public {csharpType} {name}_{i};");
                            }

                            builder.AppendLine();
                            builder.AppendLine($"        public {csharpType} Get{name}(int index)");
                            builder.AppendLine("        {");
                            builder.AppendLine($"            switch (index)");
                            builder.AppendLine("            {");

                            for (int i = 0; i < arraySize.Value; i++)
                            {
                                builder.AppendLine($"                case {i}: return {name}_{i};");
                            }

                            builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
                            builder.AppendLine("            }");
                            builder.AppendLine("        }");

                            builder.AppendLine();
                            builder.AppendLine($"        public void Set{name}(int index, {csharpType} value)");
                            builder.AppendLine("        {");
                            builder.AppendLine($"            switch (index)");
                            builder.AppendLine("            {");

                            for (int i = 0; i < arraySize.Value; i++)
                            {
                                builder.AppendLine($"                case {i}: {name}_{i} = value; break;");
                            }

                            builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
                            builder.AppendLine("            }");
                            builder.AppendLine("        }");
                        }
                    }
                    else
                    {
                        builder.AppendLine($"        [FieldOffset({currentOffset})]");
                        builder.AppendLine($"        public {csharpType} {name};");
                    }

                    int size = GetTypeSize(type, csharpType, arraySize, structures);
                    currentOffset += size;
                    builder.AppendLine();
                }

                builder.AppendLine("    }");
                builder.AppendLine("}");

                File.WriteAllText(Path.Combine(outputDirectory, $"UboStruct.{structName}.g.cs"),
                    builder.ToString(), Encoding.UTF8);
            }
        }

        private static int CalculateStructureSize(string structureName, List<GlslStructure> structures)
        {
            var structure = structures.FirstOrDefault(s => s.Name == structureName);
            if (structure == null)
            {
                Type type = ServiceHub.Get<AssemblyManager>().FindType(structureName, false);
                if (type != null)
                {
                    return System.Runtime.InteropServices.Marshal.SizeOf(type);
                }
                return 64;
            }

            int totalSize = 0;
            int maxAlignment = 4;

            foreach (var (fieldType, _, fieldArraySize) in structure.Fields)
            {
                var csharpType = GlslParser.MapGlslTypeToCSharp(fieldType);

                int fieldAlignment = GetTypeAlignment(fieldType, csharpType, structures);
                maxAlignment = Math.Max(maxAlignment, fieldAlignment);

                totalSize = AlignOffset(totalSize, fieldAlignment);
                totalSize += GetTypeSize(fieldType, csharpType, fieldArraySize, structures);
            }

            return AlignSize(totalSize, maxAlignment);
        }



        private static string GenerateRepresentationClass(string materialName, string vertexSource,
            string fragmentSource, List<(string type, string name, int? arraySize)> uniforms,
            List<UniformBlockStructure> uniformBlocks, string sourceGuid, List<RSFileInfo> rsFiles)
        {
            StringBuilder builder = new StringBuilder();
            StringBuilder interfaces = new StringBuilder();
            StringBuilder construcBuilder = new StringBuilder();
            StringBuilder disposeBuilder = new StringBuilder();
            List<string> constructor_lines = new List<string>();
            int samplers = 0;

            bool isUseDispose = false;

            disposeBuilder.AppendLine("        public override void Dispose()");
            disposeBuilder.AppendLine("        {");
            disposeBuilder.AppendLine("            base.Dispose();");

            WriteGeneratedCodeHeader(builder, sourceGuid);

            builder.AppendLine("using OpenglLib.Buffers;");
            builder.AppendLine("using Silk.NET.OpenGL;");
            builder.AppendLine("using Silk.NET.Maths;");
            builder.AppendLine("using AtomEngine;");
            builder.AppendLine();
            builder.AppendLine("namespace OpenglLib");
            builder.AppendLine("{");
            builder.AppendLine($"    public partial class {materialName}Representation : Mat *interfaces*");
            builder.AppendLine("    {");
            builder.AppendLine($"        protected new string VertexSource = @\"{vertexSource.Replace("\"", "\"\"")}\";");
            builder.AppendLine($"        protected new string FragmentSource = @\"{fragmentSource.Replace("\"", "\"\"")}\";");

            builder.AppendLine("*construct*");
            construcBuilder.AppendLine($"        public {materialName}Representation(GL gl) : base(gl)");
            construcBuilder.AppendLine("        {");

            builder.AppendLine("");

            foreach (var (type, name, arraySize) in uniforms)
            {
                string csharpType = GlslParser.MapGlslTypeToCSharp(type);
                bool isCustomType = GlslParser.IsCustomType(csharpType, type);

                string cashFieldName = $"_{name}";
                string locationName = $"{name}{ShaderConst.LOCATION}";
                var _unsafe = type.StartsWith("mat") ? "unsafe " : "";

                if (isCustomType)
                {
                    if (arraySize.HasValue)
                    {
                        builder.AppendLine($"        private StructArray<{csharpType}> {cashFieldName};");
                        builder.AppendLine($"        public StructArray<{csharpType}> {name}");
                        builder.AppendLine("        {");
                        builder.Append(GetSimpleGetter(cashFieldName));
                        builder.AppendLine("        }");
                        constructor_lines.Add($"            {cashFieldName}  = new StructArray<{csharpType}>({arraySize.Value}, _gl);");
                    }
                    else
                    {
                        builder.AppendLine($"        private {csharpType} {cashFieldName};");
                        builder.AppendLine($"        public {csharpType} {name}");
                        builder.AppendLine("        {");
                        builder.Append(GetSimpleGetter(cashFieldName));
                        builder.AppendLine("        }");
                        constructor_lines.Add($"            {cashFieldName} = new {csharpType}(_gl);");
                    }
                }
                else
                {
                    if (arraySize.HasValue)
                    {
                        var localeProperty = GetPropertyForLocaleArray(csharpType, name, locationName);
                        builder.Append(localeProperty);
                        builder.AppendLine($"        private LocaleArray<{csharpType}> {cashFieldName};");
                        builder.AppendLine($"        public LocaleArray<{csharpType}> {name}");
                        builder.AppendLine("        {");
                        builder.Append(GetSimpleGetter(cashFieldName));
                        builder.AppendLine("        }");
                        constructor_lines.Add($"            {cashFieldName}  = new LocaleArray<{csharpType}>({arraySize.Value}, _gl);");
                    }
                    else
                    {
                        if (type.IndexOf("sampler") != -1)
                        {
                            builder.AppendLine($"        public void {name}_SetTexture(OpenglLib.Texture texture) => SetTexture(\"Texture{samplers}\", \"{GlslParser.GetTextureTarget(type)}\", {locationName}, {samplers++}, texture);");
                        }
                        builder.AppendLine($"        public int {locationName} " + "{" + " get ; protected set; } = -1;");
                        builder.AppendLine($"        private {csharpType} {cashFieldName};");
                        builder.AppendLine($"        public {_unsafe}{csharpType} {name}");
                        builder.AppendLine("        {");
                        builder.Append(GetSetter(type, locationName, cashFieldName));
                        builder.AppendLine("        }");
                    }
                }
                builder.AppendLine("");
                builder.AppendLine("");
            }

            if (constructor_lines.Count > 0)
            {
                foreach (var line in constructor_lines)
                {
                    construcBuilder.AppendLine(line);
                }
            }
            construcBuilder.AppendLine("            SetUpShader(VertexSource, FragmentSource);");
            construcBuilder.AppendLine("            SetupUniformLocations();");

            foreach (var block in uniformBlocks)
            {
                if (block.InstanceName != null && block.Binding != null)
                {
                    isUseDispose = true;

                    string refStruct = $"_{block.InstanceName}";
                    builder.AppendLine($"        private UniformBufferObject<{block.CSharpTypeName}> {block.InstanceName}Ubo;");
                    construcBuilder.AppendLine($"            {block.InstanceName}Ubo = new UniformBufferObject<{block.CSharpTypeName}>(_gl, ref {refStruct}, {ShaderConst.SHADER_PROGRAM}, {block.Binding.Value});");

                    builder.AppendLine($"        private {block.CSharpTypeName} {refStruct} = new {block.CSharpTypeName}();");
                    builder.AppendLine($"        public {block.CSharpTypeName} {block.InstanceName}");
                    builder.AppendLine("        {");
                    builder.AppendLine("            set");
                    builder.AppendLine("            {");
                    builder.AppendLine($"                {refStruct} = value;");
                    builder.AppendLine($"                {block.InstanceName}Ubo.UpdateData(ref {refStruct});");
                    builder.AppendLine("            }");
                    builder.AppendLine("        }");

                    builder.AppendLine("");

                    disposeBuilder.AppendLine($"            {block.InstanceName}Ubo.Dispose();");
                }
            }

            foreach (var block in uniformBlocks)
            {
                if (block.InstanceName != null && block.Binding == null)
                {
                    isUseDispose = true;

                    string refStruct = $"_{block.InstanceName}";
                    construcBuilder.AppendLine($"            {block.InstanceName}Ubo = new UniformBufferObject<{block.CSharpTypeName}>(_gl, ref {refStruct}, GetBlockIndex(\"{block.Name}\"));");

                    builder.AppendLine($"        private UniformBufferObject<{block.CSharpTypeName}> {block.InstanceName}Ubo;");
                    builder.AppendLine($"        private {block.CSharpTypeName} {refStruct} = new {block.CSharpTypeName}();");
                    builder.AppendLine($"        public {block.CSharpTypeName} {block.InstanceName}");
                    builder.AppendLine("        {");
                    builder.AppendLine("            set");
                    builder.AppendLine("            {");
                    builder.AppendLine($"                {refStruct} = value;");
                    builder.AppendLine($"                if ({block.InstanceName}Ubo != null)");
                    builder.AppendLine($"                    {block.InstanceName}Ubo.UpdateData(ref {refStruct});");
                    builder.AppendLine("            }");
                    builder.AppendLine("        }");

                    disposeBuilder.AppendLine($"            {block.InstanceName}Ubo.Dispose();");
                }
            }

            
            if (isUseDispose)
            {
                disposeBuilder.AppendLine("        }");
                builder.AppendLine(disposeBuilder.ToString());
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");

            
            construcBuilder.AppendLine("        }");
            builder.Replace("*construct*", construcBuilder.ToString());

            foreach(var rs in rsFiles)
                interfaces.Append(", " + rs.InterfaceName);
            builder.Replace("*interfaces*", interfaces.ToString());

            return builder.ToString();
        }

        private static void WriteGeneratedCodeHeader(StringBuilder builder, string sourceGuid)
        {
            builder.AppendLine("// <auto-generated>");
            builder.AppendLine("// This code was generated. Dont change this code.");
            builder.AppendLine($"// SourceGuid: {sourceGuid ?? "Unknown"}");
            builder.AppendLine($"// GeneratedAt: {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")} UTC");
            builder.AppendLine("// </auto-generated>");
            builder.AppendLine();
        }

        public static string GetSimpleGetter(string cashFieldName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("            get");
            builder.AppendLine("            {");
            builder.AppendLine($"                return {cashFieldName};");
            builder.AppendLine("            }");
            return builder.ToString();
        }
        public static string GetPropertyForLocaleArray(string type, string fieldName, string locationFieldName)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"        public int {locationFieldName}");
            builder.AppendLine($"        {{");
            builder.AppendLine($"             get => {fieldName}.{ShaderConst.LOCATION};");
            builder.AppendLine($"             set => {fieldName}.{ShaderConst.LOCATION} = value;");
            builder.AppendLine($"        }}");

            return builder.ToString();
        }
        public static string GetSetter(string type, string locationFieldName, string cashFieldName)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("            get");
            builder.AppendLine("            {");
            builder.AppendLine($"                return {cashFieldName};");
            builder.AppendLine("            }");
            builder.AppendLine("            set");
            builder.AppendLine("            {");
            builder.AppendLine($"                if ({locationFieldName} == -1)");
            builder.AppendLine($"                {{");
            builder.AppendLine($"                   return;");
            builder.AppendLine($"                }}");
            builder.AppendLine($"                {cashFieldName} = value;");

            switch (type)
            {
                case "bool":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value ? 1 : 0);");
                    break;
                case "int":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;
                case "uint":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;
                case "float":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;
                case "double":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;

                case "bvec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0);");
                    break;
                case "bvec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0, value.Z ? 1 : 0);");
                    break;
                case "bvec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0, value.Z ? 1 : 0, value.W ? 1 : 0);");
                    break;

                case "ivec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;
                case "ivec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;
                case "ivec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                case "uvec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;
                case "uvec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;
                case "uvec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                case "vec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, (float)value.X, (float)value.Y);");
                    break;
                case "vec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, (float)value.X, (float)value.Y, (float)value.Z);");
                    break;
                case "vec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, (float)value.X, (float)value.Y, (float)value.Z, (float)value.W);");
                    break;

                case "mat2":
                case "mat2x2":
                    builder.AppendLine($"                var mat2 = (Matrix2X2<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix2({locationFieldName}, 1, false, (float*)&mat2);");
                    break;

                case "mat2x3":
                    builder.AppendLine($"                var mat2x3 = (Matrix2X3<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix2x3({locationFieldName}, 1, false, (float*)&mat2x3);");
                    break;

                case "mat2x4":
                    builder.AppendLine($"                var mat2x4 = (Matrix2X4<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix2x4({locationFieldName}, 1, false, (float*)&mat2x4);");
                    break;

                case "mat3":
                case "mat3x3":
                    builder.AppendLine($"                var mat3 = (Matrix3X3<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix3({locationFieldName}, 1, false, (float*)&mat3);");
                    break;

                case "mat3x2":
                    builder.AppendLine($"                var mat3x2 = (Matrix3X2<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix3x2({locationFieldName}, 1, false, (float*)&mat3x2);");
                    break;

                case "mat3x4":
                    builder.AppendLine($"                var mat3x4 = (Matrix3X4<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix3x4({locationFieldName}, 1, false, (float*)&mat3x4);");
                    break;

                case "mat4":
                case "mat4x4":
                    builder.AppendLine($"                var mat4 = (Matrix4X4<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix4({locationFieldName}, 1, false, (float*)&mat4);");
                    break;

                case "mat4x2":
                    builder.AppendLine($"                var mat4x2 = (Matrix4X2<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix4x2({locationFieldName}, 1, false, (float*)&mat4x2);");
                    break;

                case "mat4x3":
                    builder.AppendLine($"                var mat4x3 = (Matrix4X3<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix4x3({locationFieldName}, 1, false, (float*)&mat4x3);");
                    break;

                case string s when s.StartsWith("sampler"):
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;
                case string s when s.StartsWith("isampler"):
                    builder.AppendLine($"                _gl.Uniform1i({locationFieldName}, value);");
                    break;
                case string s when s.StartsWith("usampler"):
                    builder.AppendLine($"                _gl.Uniform1ui({locationFieldName}, value);");
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