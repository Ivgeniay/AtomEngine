using AtomEngine;
using EngineLib;
using OpenglLib;
using System.Collections.Generic;
using System.Text;
using System;
using System.Linq;
using System.IO;

namespace Editor
{
    public class GlslsUBOGenerator
    {
        public static void GenerateUniformBlockClass(UniformBlockStructure block, string outputDirectory, string sourceGuid, List<GlslStructure> structures)
        {
            List<GlslStructure> usedStructures = GetUsedStructures(block, structures);
            if (usedStructures.Count > 0)
            {
                GenerateStructsForUbo(usedStructures, outputDirectory, sourceGuid);
            }

            var builder = new StringBuilder();
            GeneratorConst.WriteGeneratedCodeHeader(builder, sourceGuid);
            builder.AppendLine("using System.Runtime.InteropServices;");
            builder.AppendLine($"{GeneratorConst.GetDefaultNamespaces()}");
            builder.AppendLine();
            builder.AppendLine($"{GeneratorConst.GetUserScriptNamespace()}");
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
                "mat2x3" => 2 * 16,
                "mat2x4" => 2 * 16,
                "mat3" or "mat3x3" => 3 * 16,
                "mat3x2" => 3 * 16,
                "mat3x4" => 3 * 16,
                "mat4" or "mat4x4" => 4 * 16,
                "mat4x2" => 4 * 16,
                "mat4x3" => 4 * 16,

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
                GeneratorConst.WriteGeneratedCodeHeader(builder, sourceGuid);

                builder.AppendLine("using System.Runtime.InteropServices;");
                builder.AppendLine($"{GeneratorConst.GetDefaultNamespaces()}");
                builder.AppendLine();
                builder.AppendLine($"{GeneratorConst.GetUserScriptNamespace()}");
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


    }
}
