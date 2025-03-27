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
        private const string MAIN_CONTENT_PLACEHOLDER = "/*MAIN_CONTENT*/";
        private const string CLASS_FIELDS_PLACEHOLDER = "/*CLASS_FIELDS*/";

        public static void GenerateUniformBlockClass(UniformBlockStructure block, string outputDirectory, string sourceGuid, List<GlslStructure> structures)
        {
            List<GlslStructure> usedStructures = GetUsedStructures(block, structures);
            if (usedStructures.Count > 0)
            {
                GenerateStructsForUbo(usedStructures, outputDirectory, sourceGuid);
            }

            var mainBuilder = new StringBuilder();
            var contentBuilder = new StringBuilder();
            var fieldsBuilder = new StringBuilder();

            GenerateMainStructure(mainBuilder, block, structures, sourceGuid);

            bool hasComplexTypes = HasComplexTypes(block, structures);
            int totalSize = hasComplexTypes ? CalculateTotalSize(block, structures) : 0;

            if (hasComplexTypes)
            {
                GenerateExplicitLayoutContent(contentBuilder, block, totalSize);
                GenerateExplicitLayoutFields(fieldsBuilder, block, structures);
            }
            else
            {
                GenerateSequentialLayoutContent(contentBuilder, block);
                GenerateSequentialLayoutFields(fieldsBuilder, block);
            }

            string contentText = contentBuilder.ToString().Replace(CLASS_FIELDS_PLACEHOLDER, fieldsBuilder.ToString());
            string result = mainBuilder.ToString().Replace(MAIN_CONTENT_PLACEHOLDER, contentText);

            string blockFilePath = Path.Combine(outputDirectory, $"UBO.{block.CSharpTypeName}.g.cs");
            File.WriteAllText(blockFilePath, result, Encoding.UTF8);
        }

        private static void GenerateMainStructure(StringBuilder builder, UniformBlockStructure block, List<GlslStructure> structures, string sourceGuid)
        {
            GeneratorConst.WriteGeneratedCodeHeader(builder, sourceGuid);
            builder.AppendLine("using System.Runtime.InteropServices;");
            builder.AppendLine($"{GeneratorConst.GetDefaultNamespaces()}");
            builder.AppendLine();
            builder.AppendLine($"{GeneratorConst.GetUserScriptNamespace()}");
            builder.AppendLine("{");
            builder.AppendLine(MAIN_CONTENT_PLACEHOLDER);
            builder.AppendLine("}");
        }

        private static void GenerateExplicitLayoutContent(StringBuilder builder, UniformBlockStructure block, int totalSize)
        {
            builder.AppendLine($"    [StructLayout(LayoutKind.Explicit, Size = {totalSize})]");
            builder.AppendLine($"    public unsafe struct {block.CSharpTypeName} : IDataSerializable");
            builder.AppendLine("    {");
            builder.AppendLine(CLASS_FIELDS_PLACEHOLDER);
            builder.AppendLine("    }");
        }

        private static void GenerateSequentialLayoutContent(StringBuilder builder, UniformBlockStructure block)
        {
            builder.AppendLine("    [StructLayout(LayoutKind.Sequential)]");
            builder.AppendLine($"    public unsafe struct {block.CSharpTypeName} : IDataSerializable");
            builder.AppendLine("    {");
            builder.AppendLine(CLASS_FIELDS_PLACEHOLDER);
            builder.AppendLine("    }");
        }

        private static void GenerateExplicitLayoutFields(StringBuilder builder, UniformBlockStructure block, List<GlslStructure> structures)
        {
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
                    GenerateCustomTypeField(builder, type, name, arraySize, structures, currentOffset);
                }
                else
                {
                    GenerateSimpleTypeField(builder, type, csharpType, name, arraySize, currentOffset);
                }

                currentOffset += size;
                builder.AppendLine();
            }
        }

        private static void GenerateSequentialLayoutFields(StringBuilder builder, UniformBlockStructure block)
        {
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
                builder.AppendLine();
            }
        }

        private static void GenerateCustomTypeField(StringBuilder builder, string type, string name, int? arraySize, List<GlslStructure> structures, int currentOffset)
        {
            bool isUserStructure = structures.Any(s => s.Name == type);
            string fieldType = isUserStructure ? $"{type}_Std140" : GlslParser.MapGlslTypeToCSharp(type);

            if (arraySize.HasValue)
            {
                var arrayElementsBuilder = new StringBuilder();
                var getterCasesBuilder = new StringBuilder();
                var setterCasesBuilder = new StringBuilder();

                for (int i = 0; i < arraySize.Value; i++)
                {
                    int elementSize = GetTypeSize(type, fieldType, null, structures);
                    int elementAlignedSize = AlignSize(elementSize, 16);
                    int elemOffset = currentOffset + i * elementAlignedSize;

                    arrayElementsBuilder.AppendLine($"        [FieldOffset({elemOffset})]");
                    arrayElementsBuilder.AppendLine($"        public {fieldType} {name}_{i};");

                    getterCasesBuilder.AppendLine($"                case {i}: return {name}_{i};");

                    setterCasesBuilder.AppendLine($"                case {i}: {name}_{i} = value; break;");
                }

                var getterMethodBuilder = new StringBuilder();
                getterMethodBuilder.AppendLine($"        public {fieldType} Get{name}(int index)");
                getterMethodBuilder.AppendLine("        {");
                getterMethodBuilder.AppendLine($"            switch (index)");
                getterMethodBuilder.AppendLine("            {");
                getterMethodBuilder.Append(getterCasesBuilder);
                getterMethodBuilder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
                getterMethodBuilder.AppendLine("            }");
                getterMethodBuilder.AppendLine("        }");

                var setterMethodBuilder = new StringBuilder();
                setterMethodBuilder.AppendLine($"        public void Set{name}(int index, {fieldType} value)");
                setterMethodBuilder.AppendLine("        {");
                setterMethodBuilder.AppendLine($"            switch (index)");
                setterMethodBuilder.AppendLine("            {");
                setterMethodBuilder.Append(setterCasesBuilder);
                setterMethodBuilder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
                setterMethodBuilder.AppendLine("            }");
                setterMethodBuilder.AppendLine("        }");

                builder.Append(arrayElementsBuilder);
                builder.AppendLine();
                builder.Append(getterMethodBuilder);
                builder.AppendLine();
                builder.Append(setterMethodBuilder);
            }
            else
            {
                builder.AppendLine($"        [FieldOffset({currentOffset})]");
                builder.AppendLine($"        public {fieldType} {name};");
            }
        }

        private static void GenerateSimpleTypeField(StringBuilder builder, string type, string csharpType, string name, int? arraySize, int currentOffset)
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
                    var arrayElementsBuilder = new StringBuilder();
                    var getterCasesBuilder = new StringBuilder();
                    var setterCasesBuilder = new StringBuilder();

                    for (int i = 0; i < arraySize.Value; i++)
                    {
                        int elementSize = GetTypeSize(type, csharpType, null, null);
                        int elementAlignedSize = AlignSize(elementSize, 16);
                        int elemOffset = currentOffset + i * elementAlignedSize;

                        arrayElementsBuilder.AppendLine($"        [FieldOffset({elemOffset})]");
                        arrayElementsBuilder.AppendLine($"        public {csharpType} {name}_{i};");

                        getterCasesBuilder.AppendLine($"                case {i}: return {name}_{i};");

                        setterCasesBuilder.AppendLine($"                case {i}: {name}_{i} = value; break;");
                    }

                    var getterMethodBuilder = new StringBuilder();
                    getterMethodBuilder.AppendLine($"        public {csharpType} Get{name}(int index)");
                    getterMethodBuilder.AppendLine("        {");
                    getterMethodBuilder.AppendLine($"            switch (index)");
                    getterMethodBuilder.AppendLine("            {");
                    getterMethodBuilder.Append(getterCasesBuilder);
                    getterMethodBuilder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
                    getterMethodBuilder.AppendLine("            }");
                    getterMethodBuilder.AppendLine("        }");

                    var setterMethodBuilder = new StringBuilder();
                    setterMethodBuilder.AppendLine($"        public void Set{name}(int index, {csharpType} value)");
                    setterMethodBuilder.AppendLine("        {");
                    setterMethodBuilder.AppendLine($"            switch (index)");
                    setterMethodBuilder.AppendLine("            {");
                    setterMethodBuilder.Append(setterCasesBuilder);
                    setterMethodBuilder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
                    setterMethodBuilder.AppendLine("            }");
                    setterMethodBuilder.AppendLine("        }");

                    builder.Append(arrayElementsBuilder);
                    builder.AppendLine();
                    builder.Append(getterMethodBuilder);
                    builder.AppendLine();
                    builder.Append(setterMethodBuilder);
                }
            }
            else
            {
                builder.AppendLine($"        [FieldOffset({currentOffset})]");
                builder.AppendLine($"        public {csharpType} {name};");
            }
        }

        private static void GenerateStructsForUbo(List<GlslStructure> structures, string outputDirectory, string sourceGuid)
        {
            foreach (var structure in structures)
            {
                var structName = $"{structure.Name}_Std140";

                var mainBuilder = new StringBuilder();
                var contentBuilder = new StringBuilder();
                var fieldsBuilder = new StringBuilder();

                GeneratorConst.WriteGeneratedCodeHeader(mainBuilder, sourceGuid);
                mainBuilder.AppendLine("using System.Runtime.InteropServices;");
                mainBuilder.AppendLine($"{GeneratorConst.GetDefaultNamespaces()}");
                mainBuilder.AppendLine();
                mainBuilder.AppendLine($"{GeneratorConst.GetUserScriptNamespace()}");
                mainBuilder.AppendLine("{");
                mainBuilder.AppendLine(MAIN_CONTENT_PLACEHOLDER);
                mainBuilder.AppendLine("}");

                int totalSize = CalculateStructureSize(structure.Name, structures);
                contentBuilder.AppendLine($"    [StructLayout(LayoutKind.Explicit, Size = {totalSize})]");
                contentBuilder.AppendLine($"    public struct {structName}");
                contentBuilder.AppendLine("    {");
                contentBuilder.AppendLine(CLASS_FIELDS_PLACEHOLDER);
                contentBuilder.AppendLine("    }");

                int currentOffset = 0;
                foreach (var (type, name, arraySize) in structure.Fields)
                {
                    var csharpType = GlslParser.MapGlslTypeToCSharp(type);
                    var isCustomType = GlslParser.IsCustomType(csharpType, type);

                    int alignment = GetTypeAlignment(type, csharpType, structures);
                    currentOffset = AlignOffset(currentOffset, alignment);

                    if (isCustomType && structures.Any(s => s.Name == type))
                    {
                        GenerateCustomTypeFieldForStruct(fieldsBuilder, type, name, arraySize, structures, currentOffset);
                    }
                    else if (arraySize.HasValue)
                    {
                        GenerateArrayFieldForStruct(fieldsBuilder, type, csharpType, name, arraySize.Value, structures, currentOffset);
                    }
                    else
                    {
                        fieldsBuilder.AppendLine($"        [FieldOffset({currentOffset})]");
                        fieldsBuilder.AppendLine($"        public {csharpType} {name};");
                        fieldsBuilder.AppendLine();
                    }

                    int size = GetTypeSize(type, csharpType, arraySize, structures);
                    currentOffset += size;
                }

                string contentText = contentBuilder.ToString().Replace(CLASS_FIELDS_PLACEHOLDER, fieldsBuilder.ToString());
                string result = mainBuilder.ToString().Replace(MAIN_CONTENT_PLACEHOLDER, contentText);

                File.WriteAllText(Path.Combine(outputDirectory, $"UboStruct.{structName}.g.cs"),
                    result, Encoding.UTF8);
            }
        }

        private static void GenerateCustomTypeFieldForStruct(StringBuilder builder, string type, string name, int? arraySize, List<GlslStructure> structures, int currentOffset)
        {
            if (arraySize.HasValue)
            {
                var arrayElementsBuilder = new StringBuilder();
                var getterCasesBuilder = new StringBuilder();
                var setterCasesBuilder = new StringBuilder();

                for (int i = 0; i < arraySize.Value; i++)
                {
                    int elementSize = GetTypeSize(type, $"{type}_Std140", null, structures);
                    int elementAlignedSize = AlignSize(elementSize, 16);
                    int elemOffset = currentOffset + i * elementAlignedSize;

                    arrayElementsBuilder.AppendLine($"        [FieldOffset({elemOffset})]");
                    arrayElementsBuilder.AppendLine($"        public {type}_Std140 {name}_{i};");

                    getterCasesBuilder.AppendLine($"                case {i}: return {name}_{i};");

                    setterCasesBuilder.AppendLine($"                case {i}: {name}_{i} = value; break;");
                }

                var getterMethodBuilder = new StringBuilder();
                getterMethodBuilder.AppendLine($"        public {type}_Std140 Get{name}(int index)");
                getterMethodBuilder.AppendLine("        {");
                getterMethodBuilder.AppendLine($"            switch (index)");
                getterMethodBuilder.AppendLine("            {");
                getterMethodBuilder.Append(getterCasesBuilder);
                getterMethodBuilder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
                getterMethodBuilder.AppendLine("            }");
                getterMethodBuilder.AppendLine("        }");

                var setterMethodBuilder = new StringBuilder();
                setterMethodBuilder.AppendLine($"        public void Set{name}(int index, {type}_Std140 value)");
                setterMethodBuilder.AppendLine("        {");
                setterMethodBuilder.AppendLine($"            switch (index)");
                setterMethodBuilder.AppendLine("            {");
                setterMethodBuilder.Append(setterCasesBuilder);
                setterMethodBuilder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
                setterMethodBuilder.AppendLine("            }");
                setterMethodBuilder.AppendLine("        }");

                builder.Append(arrayElementsBuilder);
                builder.AppendLine();
                builder.Append(getterMethodBuilder);
                builder.AppendLine();
                builder.Append(setterMethodBuilder);
            }
            else
            {
                builder.AppendLine($"        [FieldOffset({currentOffset})]");
                builder.AppendLine($"        public {type}_Std140 {name};");
                builder.AppendLine();
            }
        }

        private static void GenerateArrayFieldForStruct(StringBuilder builder, string type, string csharpType, string name, int arraySize, List<GlslStructure> structures, int currentOffset)
        {
            var arrayElementsBuilder = new StringBuilder();
            var getterCasesBuilder = new StringBuilder();
            var setterCasesBuilder = new StringBuilder();

            if (IsFixedArrayType(type))
            {
                for (int i = 0; i < arraySize; i++)
                {
                    int elementOffset = currentOffset + i * 16;

                    arrayElementsBuilder.AppendLine($"        [FieldOffset({elementOffset})]");
                    arrayElementsBuilder.AppendLine($"        public {csharpType} {name}_{i};");

                    getterCasesBuilder.AppendLine($"                case {i}: return {name}_{i};");
                    setterCasesBuilder.AppendLine($"                case {i}: {name}_{i} = value; break;");
                }

                var getterMethodBuilder = new StringBuilder();
                getterMethodBuilder.AppendLine($"        public {csharpType} Get{name}(int index)");
                getterMethodBuilder.AppendLine("        {");
                getterMethodBuilder.AppendLine($"            switch (index)");
                getterMethodBuilder.AppendLine("            {");
                getterMethodBuilder.Append(getterCasesBuilder);
                getterMethodBuilder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize}\");");
                getterMethodBuilder.AppendLine("            }");
                getterMethodBuilder.AppendLine("        }");

                var setterMethodBuilder = new StringBuilder();
                setterMethodBuilder.AppendLine($"        public void Set{name}(int index, {csharpType} value)");
                setterMethodBuilder.AppendLine("        {");
                setterMethodBuilder.AppendLine($"            switch (index)");
                setterMethodBuilder.AppendLine("            {");
                setterMethodBuilder.Append(setterCasesBuilder);
                setterMethodBuilder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize}\");");
                setterMethodBuilder.AppendLine("            }");
                setterMethodBuilder.AppendLine("        }");

                builder.Append(arrayElementsBuilder);
                builder.AppendLine();
                builder.Append(getterMethodBuilder);
                builder.AppendLine();
                builder.Append(setterMethodBuilder);
            }
            else
            {


                for (int i = 0; i < arraySize; i++)
                {
                    int elementSize = GetTypeSize(type, csharpType, null, structures);
                    int elementAlignedSize = AlignSize(elementSize, 16);
                    int elemOffset = currentOffset + i * elementAlignedSize;

                    arrayElementsBuilder.AppendLine($"        [FieldOffset({elemOffset})]");
                    arrayElementsBuilder.AppendLine($"        public {csharpType} {name}_{i};");

                    getterCasesBuilder.AppendLine($"                case {i}: return {name}_{i};");
                    setterCasesBuilder.AppendLine($"                case {i}: {name}_{i} = value; break;");
                }

                var getterMethodBuilder = new StringBuilder();
                getterMethodBuilder.AppendLine($"        public {csharpType} Get{name}(int index)");
                getterMethodBuilder.AppendLine("        {");
                getterMethodBuilder.AppendLine($"            switch (index)");
                getterMethodBuilder.AppendLine("            {");
                getterMethodBuilder.Append(getterCasesBuilder);
                getterMethodBuilder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize}\");");
                getterMethodBuilder.AppendLine("            }");
                getterMethodBuilder.AppendLine("        }");

                var setterMethodBuilder = new StringBuilder();
                setterMethodBuilder.AppendLine($"        public void Set{name}(int index, {csharpType} value)");
                setterMethodBuilder.AppendLine("        {");
                setterMethodBuilder.AppendLine($"            switch (index)");
                setterMethodBuilder.AppendLine("            {");
                setterMethodBuilder.Append(setterCasesBuilder);
                setterMethodBuilder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize}\");");
                setterMethodBuilder.AppendLine("            }");
                setterMethodBuilder.AppendLine("        }");

                builder.Append(arrayElementsBuilder);
                builder.AppendLine();
                builder.Append(getterMethodBuilder);
                builder.AppendLine();
                builder.Append(setterMethodBuilder);
            }
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

        private static int AlignOffset(int offset, int alignment)
        {
            return (offset + alignment - 1) / alignment * alignment;
        }

        private static int AlignSize(int size, int alignment)
        {
            return (size + alignment - 1) / alignment * alignment;
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

        private static bool IsFixedArrayType(string type)
        {
            return type is "bool" or "int" or "uint" or "float" or "double";
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

    }


    //public class GlslsUBOGenerator
    //{
    //    public static void GenerateUniformBlockClass(UniformBlockStructure block, string outputDirectory, string sourceGuid, List<GlslStructure> structures)
    //    {
    //        List<GlslStructure> usedStructures = GetUsedStructures(block, structures);
    //        if (usedStructures.Count > 0)
    //        {
    //            GenerateStructsForUbo(usedStructures, outputDirectory, sourceGuid);
    //        }

    //        var builder = new StringBuilder();
    //        GeneratorConst.WriteGeneratedCodeHeader(builder, sourceGuid);
    //        builder.AppendLine("using System.Runtime.InteropServices;");
    //        builder.AppendLine($"{GeneratorConst.GetDefaultNamespaces()}");
    //        builder.AppendLine();
    //        builder.AppendLine($"{GeneratorConst.GetUserScriptNamespace()}");
    //        builder.AppendLine("{");

    //        bool hasComplexTypes = HasComplexTypes(block, structures);

    //        if (hasComplexTypes)
    //        {
    //            int totalSize = CalculateTotalSize(block, structures);
    //            builder.AppendLine($"    [StructLayout(LayoutKind.Explicit, Size = {totalSize})]");
    //            builder.AppendLine($"    public unsafe struct {block.CSharpTypeName} : IDataSerializable");
    //            builder.AppendLine("    {");

    //            int currentOffset = 0;

    //            foreach (var (type, name, arraySize) in block.Fields)
    //            {
    //                var csharpType = GlslParser.MapGlslTypeToCSharp(type);
    //                bool isCustomType = GlslParser.IsCustomType(csharpType, type);

    //                int alignment = GetTypeAlignment(type, csharpType, structures);
    //                currentOffset = AlignOffset(currentOffset, alignment);
    //                int size = GetTypeSize(type, csharpType, arraySize, structures);

    //                if (isCustomType)
    //                {
    //                    bool isUserStructure = structures.Any(s => s.Name == type);
    //                    string fieldType = isUserStructure ? $"{type}_Std140" : csharpType;

    //                    if (arraySize.HasValue)
    //                    {
    //                        for (int i = 0; i < arraySize.Value; i++)
    //                        {
    //                            int elementSize = GetTypeSize(type, csharpType, null, structures);
    //                            int elementAlignedSize = AlignSize(elementSize, 16);
    //                            int elemOffset = currentOffset + i * elementAlignedSize;

    //                            builder.AppendLine($"        [FieldOffset({elemOffset})]");
    //                            builder.AppendLine($"        public {fieldType} {name}_{i};");
    //                        }

    //                        builder.AppendLine();
    //                        builder.AppendLine($"        public {fieldType} Get{name}(int index)");
    //                        builder.AppendLine("        {");
    //                        builder.AppendLine($"            switch (index)");
    //                        builder.AppendLine("            {");

    //                        for (int i = 0; i < arraySize.Value; i++)
    //                        {
    //                            builder.AppendLine($"                case {i}: return {name}_{i};");
    //                        }

    //                        builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
    //                        builder.AppendLine("            }");
    //                        builder.AppendLine("        }");

    //                        builder.AppendLine();
    //                        builder.AppendLine($"        public void Set{name}(int index, {fieldType} value)");
    //                        builder.AppendLine("        {");
    //                        builder.AppendLine($"            switch (index)");
    //                        builder.AppendLine("            {");

    //                        for (int i = 0; i < arraySize.Value; i++)
    //                        {
    //                            builder.AppendLine($"                case {i}: {name}_{i} = value; break;");
    //                        }

    //                        builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
    //                        builder.AppendLine("            }");
    //                        builder.AppendLine("        }");
    //                    }
    //                    else
    //                    {
    //                        builder.AppendLine($"        [FieldOffset({currentOffset})]");
    //                        builder.AppendLine($"        public {fieldType} {name};");
    //                    }
    //                }
    //                else
    //                {
    //                    if (arraySize.HasValue)
    //                    {
    //                        if (IsFixedArrayType(type))
    //                        {
    //                            builder.AppendLine($"        [FieldOffset({currentOffset})]");
    //                            builder.AppendLine($"        public fixed {csharpType} {name}[{arraySize.Value}];");
    //                        }
    //                        else
    //                        {
    //                            for (int i = 0; i < arraySize.Value; i++)
    //                            {
    //                                int elementSize = GetTypeSize(type, csharpType, null, structures);
    //                                int elementAlignedSize = AlignSize(elementSize, 16);
    //                                int elemOffset = currentOffset + i * elementAlignedSize;

    //                                builder.AppendLine($"        [FieldOffset({elemOffset})]");
    //                                builder.AppendLine($"        public {csharpType} {name}_{i};");
    //                            }

    //                            builder.AppendLine();
    //                            builder.AppendLine($"        public {csharpType} Get{name}(int index)");
    //                            builder.AppendLine("        {");
    //                            builder.AppendLine($"            switch (index)");
    //                            builder.AppendLine("            {");

    //                            for (int i = 0; i < arraySize.Value; i++)
    //                            {
    //                                builder.AppendLine($"                case {i}: return {name}_{i};");
    //                            }

    //                            builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
    //                            builder.AppendLine("            }");
    //                            builder.AppendLine("        }");

    //                            builder.AppendLine();
    //                            builder.AppendLine($"        public void Set{name}(int index, {csharpType} value)");
    //                            builder.AppendLine("        {");
    //                            builder.AppendLine($"            switch (index)");
    //                            builder.AppendLine("            {");

    //                            for (int i = 0; i < arraySize.Value; i++)
    //                            {
    //                                builder.AppendLine($"                case {i}: {name}_{i} = value; break;");
    //                            }

    //                            builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
    //                            builder.AppendLine("            }");
    //                            builder.AppendLine("        }");
    //                        }
    //                    }
    //                    else
    //                    {
    //                        builder.AppendLine($"        [FieldOffset({currentOffset})]");
    //                        builder.AppendLine($"        public {csharpType} {name};");
    //                    }
    //                }

    //                currentOffset += size;
    //                builder.AppendLine();
    //            }
    //        }
    //        else
    //        {
    //            builder.AppendLine("    [StructLayout(LayoutKind.Sequential)]");
    //            builder.AppendLine($"    public unsafe struct {block.CSharpTypeName} : IDataSerializable");
    //            builder.AppendLine("    {");

    //            foreach (var (type, name, arraySize) in block.Fields)
    //            {
    //                var csharpType = GlslParser.MapGlslTypeToCSharp(type);

    //                if (arraySize.HasValue)
    //                {
    //                    builder.AppendLine($"        [MarshalAs(UnmanagedType.ByValArray, SizeConst = {arraySize.Value})]");
    //                    builder.AppendLine($"        public {csharpType}[] {name};");
    //                }
    //                else
    //                {
    //                    builder.AppendLine($"        public {csharpType} {name};");
    //                }
    //            }
    //        }

    //        builder.AppendLine("    }");
    //        builder.AppendLine("}");

    //        string blockFilePath = Path.Combine(outputDirectory, $"UBO.{block.CSharpTypeName}.g.cs");
    //        string blockCode = builder.ToString();
    //        File.WriteAllText(blockFilePath, blockCode, Encoding.UTF8);
    //    }

    //    private static List<GlslStructure> GetUsedStructures(UniformBlockStructure block, List<GlslStructure> allStructures)
    //    {
    //        var result = new List<GlslStructure>();
    //        var processedTypes = new HashSet<string>();

    //        void CollectStructureAndDependencies(string typeName)
    //        {
    //            if (processedTypes.Contains(typeName))
    //                return;

    //            var structure = allStructures.FirstOrDefault(s => s.Name == typeName);
    //            if (structure == null)
    //                return;

    //            processedTypes.Add(typeName);
    //            result.Add(structure);

    //            foreach (var (fieldType, _, _) in structure.Fields)
    //            {
    //                if (allStructures.Any(s => s.Name == fieldType))
    //                {
    //                    CollectStructureAndDependencies(fieldType);
    //                }
    //            }
    //        }

    //        foreach (var (type, _, _) in block.Fields)
    //        {
    //            if (allStructures.Any(s => s.Name == type))
    //            {
    //                CollectStructureAndDependencies(type);
    //            }
    //        }

    //        return result;
    //    }

    //    private static bool HasComplexTypes(UniformBlockStructure block, List<GlslStructure> structures)
    //    {
    //        foreach (var (type, _, arraySize) in block.Fields)
    //        {
    //            var csharpType = GlslParser.MapGlslTypeToCSharp(type);
    //            var isCustomType = GlslParser.IsCustomType(csharpType, type);

    //            if (isCustomType || arraySize.HasValue || structures.Any(s => s.Name == type))
    //                return true;
    //        }
    //        return false;
    //    }

    //    private static int GetTypeAlignment(string type, string csharpType, List<GlslStructure> structures)
    //    {
    //        if (type is "bool" or "int" or "uint" or "float")
    //            return 4;

    //        return type switch
    //        {
    //            "double" => 8,
    //            "bvec2" or "ivec2" or "uvec2" or "vec2" => 8,
    //            "bvec3" or "ivec3" or "uvec3" or "vec3" or
    //            "bvec4" or "ivec4" or "uvec4" or "vec4" => 16,

    //            var t when t.StartsWith("mat") => 16,

    //            var t when structures.Any(s => s.Name == t) => CalculateStructureAlignment(t, structures),
    //            _ => 4
    //        };
    //    }

    //    private static int GetTypeSize(string type, string csharpType, int? arraySize, List<GlslStructure> structures)
    //    {
    //        int baseSize = type switch
    //        {
    //            "bool" or "int" or "uint" or "float" => 4,
    //            "double" => 8,

    //            "bvec2" or "ivec2" or "uvec2" or "vec2" => 8,
    //            "bvec3" or "ivec3" or "uvec3" or "vec3" => 12,
    //            "bvec4" or "ivec4" or "uvec4" or "vec4" => 16,

    //            "mat2" or "mat2x2" => 2 * 16,
    //            "mat2x3" => 2 * 16,
    //            "mat2x4" => 2 * 16,
    //            "mat3" or "mat3x3" => 3 * 16,
    //            "mat3x2" => 3 * 16,
    //            "mat3x4" => 3 * 16,
    //            "mat4" or "mat4x4" => 4 * 16,
    //            "mat4x2" => 4 * 16,
    //            "mat4x3" => 4 * 16,

    //            var t when structures.Any(s => s.Name == t) => EstimateStructureSize(t, structures),
    //            _ => 4
    //        };

    //        if (arraySize.HasValue)
    //        {
    //            int alignedElementSize = AlignSize(baseSize, 16);
    //            return alignedElementSize * arraySize.Value;
    //        }

    //        return baseSize;
    //    }

    //    private static int EstimateStructureSize(string structureName, List<GlslStructure> structures)
    //    {
    //        var structure = structures.FirstOrDefault(s => s.Name == structureName);
    //        if (structure == null)
    //        {
    //            Type type = ServiceHub.Get<AssemblyManager>().FindType(structureName, false);
    //            if (type != null)
    //            {
    //                return System.Runtime.InteropServices.Marshal.SizeOf(type);
    //            }
    //            return 64;
    //        }

    //        int totalSize = 0;
    //        int maxAlignment = 4;

    //        foreach (var (fieldType, _, fieldArraySize) in structure.Fields)
    //        {
    //            var csharpType = GlslParser.MapGlslTypeToCSharp(fieldType);

    //            int fieldAlignment = GetTypeAlignment(fieldType, csharpType, structures);
    //            maxAlignment = Math.Max(maxAlignment, fieldAlignment);
    //            totalSize = AlignOffset(totalSize, fieldAlignment);
    //            totalSize += GetTypeSize(fieldType, csharpType, fieldArraySize, structures);
    //        }
    //        return AlignSize(totalSize, maxAlignment);
    //    }

    //    private static bool IsFixedArrayType(string type)
    //    {
    //        return type is "bool" or "int" or "uint" or "float" or "double";
    //    }

    //    private static int CalculateTotalSize(UniformBlockStructure block, List<GlslStructure> structures)
    //    {
    //        int totalSize = 0;
    //        int maxAlignment = 4;

    //        foreach (var (type, _, arraySize) in block.Fields)
    //        {
    //            var csharpType = GlslParser.MapGlslTypeToCSharp(type);

    //            int alignment = GetTypeAlignment(type, csharpType, structures);
    //            maxAlignment = Math.Max(maxAlignment, alignment);
    //            totalSize = AlignOffset(totalSize, alignment);
    //            totalSize += GetTypeSize(type, csharpType, arraySize, structures);
    //        }
    //        return AlignSize(totalSize, maxAlignment);
    //    }

    //    private static int CalculateStructureAlignment(string structureName, List<GlslStructure> structures)
    //    {
    //        var structure = structures.FirstOrDefault(s => s.Name == structureName);
    //        if (structure == null)
    //            return 16;

    //        int maxAlignment = 4;

    //        foreach (var (fieldType, _, _) in structure.Fields)
    //        {
    //            var csharpType = GlslParser.MapGlslTypeToCSharp(fieldType);
    //            int fieldAlignment = GetTypeAlignment(fieldType, csharpType, structures);
    //            maxAlignment = Math.Max(maxAlignment, fieldAlignment);
    //        }

    //        return maxAlignment;
    //    }

    //    private static int AlignOffset(int offset, int alignment)
    //    {
    //        return (offset + alignment - 1) / alignment * alignment;
    //    }

    //    private static int AlignSize(int size, int alignment)
    //    {
    //        return (size + alignment - 1) / alignment * alignment;
    //    }

    //    private static void GenerateStructsForUbo(List<GlslStructure> structures, string outputDirectory, string sourceGuid)
    //    {
    //        foreach (var structure in structures)
    //        {
    //            var structName = $"{structure.Name}_Std140";
    //            var builder = new StringBuilder();
    //            GeneratorConst.WriteGeneratedCodeHeader(builder, sourceGuid);

    //            builder.AppendLine("using System.Runtime.InteropServices;");
    //            builder.AppendLine($"{GeneratorConst.GetDefaultNamespaces()}");
    //            builder.AppendLine();
    //            builder.AppendLine($"{GeneratorConst.GetUserScriptNamespace()}");
    //            builder.AppendLine("{");

    //            int totalSize = CalculateStructureSize(structure.Name, structures);
    //            builder.AppendLine($"    [StructLayout(LayoutKind.Explicit, Size = {totalSize})]");
    //            builder.AppendLine($"    public struct {structName}");
    //            builder.AppendLine("    {");

    //            int currentOffset = 0;

    //            foreach (var (type, name, arraySize) in structure.Fields)
    //            {
    //                var csharpType = GlslParser.MapGlslTypeToCSharp(type);
    //                var isCustomType = GlslParser.IsCustomType(csharpType, type);

    //                int alignment = GetTypeAlignment(type, csharpType, structures);
    //                currentOffset = AlignOffset(currentOffset, alignment);

    //                if (isCustomType && structures.Any(s => s.Name == type))
    //                {
    //                    if (arraySize.HasValue)
    //                    {
    //                        for (int i = 0; i < arraySize.Value; i++)
    //                        {
    //                            int elementSize = GetTypeSize(type, csharpType, null, structures);
    //                            int elementAlignedSize = AlignSize(elementSize, 16);
    //                            int elemOffset = currentOffset + i * elementAlignedSize;

    //                            builder.AppendLine($"        [FieldOffset({elemOffset})]");
    //                            builder.AppendLine($"        public {type}_Std140 {name}_{i};");
    //                        }

    //                        builder.AppendLine();
    //                        builder.AppendLine($"        public {type}_Std140 Get{name}(int index)");
    //                        builder.AppendLine("        {");
    //                        builder.AppendLine($"            switch (index)");
    //                        builder.AppendLine("            {");

    //                        for (int i = 0; i < arraySize.Value; i++)
    //                        {
    //                            builder.AppendLine($"                case {i}: return {name}_{i};");
    //                        }

    //                        builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
    //                        builder.AppendLine("            }");
    //                        builder.AppendLine("        }");

    //                        builder.AppendLine();
    //                        builder.AppendLine($"        public void Set{name}(int index, {type}_Std140 value)");
    //                        builder.AppendLine("        {");
    //                        builder.AppendLine($"            switch (index)");
    //                        builder.AppendLine("            {");

    //                        for (int i = 0; i < arraySize.Value; i++)
    //                        {
    //                            builder.AppendLine($"                case {i}: {name}_{i} = value; break;");
    //                        }

    //                        builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
    //                        builder.AppendLine("            }");
    //                        builder.AppendLine("        }");
    //                    }
    //                    else
    //                    {
    //                        builder.AppendLine($"        [FieldOffset({currentOffset})]");
    //                        builder.AppendLine($"        public {type}_Std140 {name};");
    //                    }
    //                }
    //                else if (arraySize.HasValue)
    //                {
    //                    if (IsFixedArrayType(type))
    //                    {
    //                        for (int i = 0; i < arraySize.Value; i++)
    //                        {
    //                            int elementOffset = currentOffset + i * 16;

    //                            builder.AppendLine($"        [FieldOffset({elementOffset})]");
    //                            builder.AppendLine($"        public {csharpType} {name}_{i};");
    //                        }
    //                        builder.AppendLine();
    //                        builder.AppendLine($"        public {csharpType} Get{name}(int index)");
    //                        builder.AppendLine("        {");
    //                        builder.AppendLine($"            switch (index)");
    //                        builder.AppendLine("            {");

    //                        for (int i = 0; i < arraySize.Value; i++)
    //                        {
    //                            builder.AppendLine($"                case {i}: return {name}_{i};");
    //                        }

    //                        builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
    //                        builder.AppendLine("            }");
    //                        builder.AppendLine("        }");

    //                        builder.AppendLine();
    //                        builder.AppendLine($"        public void Set{name}(int index, {csharpType} value)");
    //                        builder.AppendLine("        {");
    //                        builder.AppendLine($"            switch (index)");
    //                        builder.AppendLine("            {");

    //                        for (int i = 0; i < arraySize.Value; i++)
    //                        {
    //                            builder.AppendLine($"                case {i}: {name}_{i} = value; break;");
    //                        }

    //                        builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
    //                        builder.AppendLine("            }");
    //                        builder.AppendLine("        }");
    //                    }
    //                    else
    //                    {
    //                        for (int i = 0; i < arraySize.Value; i++)
    //                        {
    //                            int elementSize = GetTypeSize(type, csharpType, null, structures);
    //                            int elementAlignedSize = AlignSize(elementSize, 16);
    //                            int elemOffset = currentOffset + i * elementAlignedSize;

    //                            builder.AppendLine($"        [FieldOffset({elemOffset})]");
    //                            builder.AppendLine($"        public {csharpType} {name}_{i};");
    //                        }

    //                        builder.AppendLine();
    //                        builder.AppendLine($"        public {csharpType} Get{name}(int index)");
    //                        builder.AppendLine("        {");
    //                        builder.AppendLine($"            switch (index)");
    //                        builder.AppendLine("            {");

    //                        for (int i = 0; i < arraySize.Value; i++)
    //                        {
    //                            builder.AppendLine($"                case {i}: return {name}_{i};");
    //                        }

    //                        builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
    //                        builder.AppendLine("            }");
    //                        builder.AppendLine("        }");

    //                        builder.AppendLine();
    //                        builder.AppendLine($"        public void Set{name}(int index, {csharpType} value)");
    //                        builder.AppendLine("        {");
    //                        builder.AppendLine($"            switch (index)");
    //                        builder.AppendLine("            {");

    //                        for (int i = 0; i < arraySize.Value; i++)
    //                        {
    //                            builder.AppendLine($"                case {i}: {name}_{i} = value; break;");
    //                        }

    //                        builder.AppendLine($"                default: throw new IndexOutOfRangeException($\"Index {{index}} is out of range for array of size {arraySize.Value}\");");
    //                        builder.AppendLine("            }");
    //                        builder.AppendLine("        }");
    //                    }
    //                }
    //                else
    //                {
    //                    builder.AppendLine($"        [FieldOffset({currentOffset})]");
    //                    builder.AppendLine($"        public {csharpType} {name};");
    //                }

    //                int size = GetTypeSize(type, csharpType, arraySize, structures);
    //                currentOffset += size;
    //                builder.AppendLine();
    //            }

    //            builder.AppendLine("    }");
    //            builder.AppendLine("}");

    //            File.WriteAllText(Path.Combine(outputDirectory, $"UboStruct.{structName}.g.cs"),
    //                builder.ToString(), Encoding.UTF8);
    //        }
    //    }

    //    private static int CalculateStructureSize(string structureName, List<GlslStructure> structures)
    //    {
    //        var structure = structures.FirstOrDefault(s => s.Name == structureName);
    //        if (structure == null)
    //        {
    //            Type type = ServiceHub.Get<AssemblyManager>().FindType(structureName, false);
    //            if (type != null)
    //            {
    //                return System.Runtime.InteropServices.Marshal.SizeOf(type);
    //            }
    //            return 64;
    //        }

    //        int totalSize = 0;
    //        int maxAlignment = 4;

    //        foreach (var (fieldType, _, fieldArraySize) in structure.Fields)
    //        {
    //            var csharpType = GlslParser.MapGlslTypeToCSharp(fieldType);

    //            int fieldAlignment = GetTypeAlignment(fieldType, csharpType, structures);
    //            maxAlignment = Math.Max(maxAlignment, fieldAlignment);

    //            totalSize = AlignOffset(totalSize, fieldAlignment);
    //            totalSize += GetTypeSize(fieldType, csharpType, fieldArraySize, structures);
    //        }

    //        return AlignSize(totalSize, maxAlignment);
    //    }


    //}

}
