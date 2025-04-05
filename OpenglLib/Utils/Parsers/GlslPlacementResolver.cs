using System.Text.RegularExpressions;
using System.Text;

namespace OpenglLib
{
    public static class GlslPlacementResolver
    {
        private static string ResolveElementsPlacement<T>(
            string sourceShader,
            IEnumerable<T> elements,
            Func<T, string> getFullText,
            Func<T, string> formatElement,
            Func<T, string, Regex> createSearchRegex,
            Func<T, List<ShaderAttribute>> getAttributes,
            bool useNewLinesBetweenElements = false,
            bool insertAtTop = false)
        {
            int vertexSectionIndex = sourceShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
            int fragmentSectionIndex = sourceShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

            if (vertexSectionIndex == -1 || fragmentSectionIndex == -1)
            {
                throw new FormatException("Shader source must contain both #vertex and #fragment sections");
            }

            string resultShader = new string(sourceShader.ToCharArray());

            List<string> elementsToAddToVertex = new List<string>();
            List<string> elementsToAddToFragment = new List<string>();
            List<string> elementsToRemove = new List<string>();

            foreach (var element in elements)
            {
                var attributes = getAttributes(element);
                var placeTargetAttr = attributes?.FirstOrDefault(a => a.Name.Equals("placetarget", StringComparison.OrdinalIgnoreCase));

                if (placeTargetAttr == null)
                {
                    continue;
                }

                string elementText = getFullText(element);
                if (string.IsNullOrEmpty(elementText))
                {
                    elementText = formatElement(element);
                }

                int elementPosition = resultShader.IndexOf(elementText, StringComparison.OrdinalIgnoreCase);

                if (elementPosition == -1)
                {
                    var regex = createSearchRegex(element, resultShader);
                    if (regex != null)
                    {
                        var match = regex.Match(resultShader);
                        if (match.Success)
                        {
                            elementText = match.Value;
                            elementPosition = match.Index;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                bool isInVertexSection = elementPosition > vertexSectionIndex && elementPosition < fragmentSectionIndex;
                bool isInFragmentSection = elementPosition > fragmentSectionIndex;

                bool shouldBeInVertex = placeTargetAttr.Value.Equals("vertex", StringComparison.OrdinalIgnoreCase) ||
                                       placeTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

                bool shouldBeInFragment = placeTargetAttr.Value.Equals("fragment", StringComparison.OrdinalIgnoreCase) ||
                                         placeTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

                if (shouldBeInVertex && !isInVertexSection)
                {
                    elementsToAddToVertex.Add(elementText);
                }

                if (shouldBeInFragment && !isInFragmentSection)
                {
                    elementsToAddToFragment.Add(elementText);
                }

                if ((isInVertexSection && !shouldBeInVertex) ||
                    (isInFragmentSection && !shouldBeInFragment))
                {
                    elementsToRemove.Add(elementText);
                }
            }

            foreach (var elementText in elementsToRemove)
            {
                resultShader = resultShader.Replace(elementText, "");
            }

            vertexSectionIndex = resultShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
            fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

            int vertexMainIndex = resultShader.IndexOf("void main()", vertexSectionIndex,
                fragmentSectionIndex - vertexSectionIndex, StringComparison.OrdinalIgnoreCase);

            int fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
                StringComparison.OrdinalIgnoreCase);

            if (vertexMainIndex == -1 || fragmentMainIndex == -1)
            {
                throw new FormatException("Both vertex and fragment sections must contain a main function");
            }

            if (elementsToAddToVertex.Count > 0)
            {
                string separator = useNewLinesBetweenElements ? "\n\n" : "\n";
                string vertexElementsToAdd = string.Join(separator, elementsToAddToVertex);

                if (insertAtTop)
                {
                    int versionIndex = resultShader.IndexOf("#version", vertexSectionIndex, fragmentSectionIndex - vertexSectionIndex);
                    if (versionIndex != -1)
                    {
                        int versionEndIndex = resultShader.IndexOf('\n', versionIndex);
                        if (versionEndIndex != -1)
                        {
                            resultShader = resultShader.Insert(versionEndIndex + 1, vertexElementsToAdd + "\n\n");
                        }
                    }
                    else
                    {
                        int vertexEndIndex = resultShader.IndexOf('\n', vertexSectionIndex);
                        if (vertexEndIndex != -1)
                        {
                            resultShader = resultShader.Insert(vertexEndIndex + 1, vertexElementsToAdd + "\n\n");
                        }
                    }
                }
                else
                {
                    int vertexMainIndex_ = resultShader.IndexOf("void main()", vertexSectionIndex,
                        fragmentSectionIndex - vertexSectionIndex, StringComparison.OrdinalIgnoreCase);

                    if (vertexMainIndex_ != -1)
                    {
                        resultShader = resultShader.Insert(vertexMainIndex_, vertexElementsToAdd + "\n\n");
                    }
                }
            }

            fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);
            fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
                StringComparison.OrdinalIgnoreCase);

            if (elementsToAddToFragment.Count > 0)
            {
                string separator = useNewLinesBetweenElements ? "\n\n" : "\n";
                string fragmentElementsToAdd = string.Join(separator, elementsToAddToFragment);

                if (insertAtTop)
                {
                    int versionIndex = resultShader.IndexOf("#version", fragmentSectionIndex);
                    if (versionIndex != -1)
                    {
                        int versionEndIndex = resultShader.IndexOf('\n', versionIndex);
                        if (versionEndIndex != -1)
                        {
                            resultShader = resultShader.Insert(versionEndIndex + 1, fragmentElementsToAdd + "\n\n");
                        }
                    }
                    else
                    {
                        int fragmentEndIndex = resultShader.IndexOf('\n', fragmentSectionIndex);
                        if (fragmentEndIndex != -1)
                        {
                            resultShader = resultShader.Insert(fragmentEndIndex + 1, fragmentElementsToAdd + "\n\n");
                        }
                    }
                }
                else
                {
                    int fragmentMainIndex_ = resultShader.IndexOf("void main()", fragmentSectionIndex,
                        StringComparison.OrdinalIgnoreCase);

                    if (fragmentMainIndex_ != -1)
                    {
                        resultShader = resultShader.Insert(fragmentMainIndex_, fragmentElementsToAdd + "\n\n");
                    }
                }
            }

            return resultShader;
        }

        public static string ResolveConstantPlacement(string sourceShader, List<GlslConstantModel> constants)
        {
            return ResolveElementsPlacement(
                sourceShader,
                constants,
                c => c.FullText,
                FormatConstant,
                (constant, shader) => new Regex($"const\\s+{constant.Type}\\s+{constant.Name}\\s*=\\s*[^;]+;", RegexOptions.Singleline),
                c => c.Attributes,
                false,
                true
            );
        }

        private static string FormatConstant(GlslConstantModel constant)
        {
            return $"const {constant.Type} {constant.Name} = {constant.Value};";
        }

        public static string ResolveUniformBlockPlacement(string sourceShader, List<UniformBlockModel> uniformBlocks)
        {
            return ResolveElementsPlacement(
                sourceShader,
                uniformBlocks,
                b => b.FullText,
                FormatUniformBlock,
                (block, shader) => {
                    string blockPattern = $"(?:layout\\s*\\(\\s*(?:std140|std430|packed|shared)(?:\\s*,\\s*binding\\s*=\\s*\\d+)?\\)\\s*)?" +
                                         $"uniform\\s+{Regex.Escape(block.Name)}?\\s*\\{{[^}}]+\\}}\\s*{Regex.Escape(block.InstanceName ?? "")}?\\s*;";
                    return new Regex(blockPattern, RegexOptions.Singleline);
                },
                b => b.Attributes,
                true,
                true
            );
        }

        private static string FormatUniformBlock(UniformBlockModel block)
        {
            var sb = new StringBuilder();

            if (block.UniformBlockType != LayoutType.Ordinary || block.Binding.HasValue)
            {
                sb.Append("layout(");

                if (block.UniformBlockType != LayoutType.Ordinary)
                {
                    sb.Append(block.UniformBlockType.ToString().ToLower());
                }

                if (block.Binding.HasValue)
                {
                    if (block.UniformBlockType != LayoutType.Ordinary)
                    {
                        sb.Append(", ");
                    }
                    sb.Append($"binding = {block.Binding.Value}");
                }

                sb.Append(") ");
            }

            sb.Append("uniform ");
            if (!string.IsNullOrEmpty(block.Name) && block.Name != block.InstanceName)
            {
                sb.Append(block.Name + " ");
            }

            sb.Append("{\n");
            foreach (var field in block.Fields)
            {
                sb.Append($"    {field.Type} {field.Name}");
                if (field.ArraySize.HasValue)
                {
                    sb.Append($"[{field.ArraySize.Value}]");
                }
                sb.Append(";\n");
            }
            sb.Append("}");

            if (!string.IsNullOrEmpty(block.InstanceName))
            {
                sb.Append(" " + block.InstanceName);
            }

            sb.Append(";");

            return sb.ToString();
        }

        public static string ResolveUniformPlacement(string sourceShader, List<UniformModel> uniforms)
        {
            return ResolveElementsPlacement(
                sourceShader,
                uniforms,
                u => u.FullText,
                FormatUniform,
                (uniform, shader) => {
                    string uniformPattern = $"uniform\\s+{uniform.Type}\\s+{uniform.Name}";
                    if (uniform.ArraySize.HasValue)
                    {
                        uniformPattern += $"\\s*\\[\\s*(?:{uniform.ArraySize}|\\w+)\\s*\\]";
                    }
                    uniformPattern += "\\s*;";

                    var regex = new Regex(uniformPattern, RegexOptions.Singleline);
                    var match = regex.Match(shader);

                    if (!match.Success)
                    {
                        uniformPattern = $"uniform\\s+{uniform.Type}\\s+{uniform.Name}\\s*(?:\\[[^\\]]*\\])?\\s*;";
                        return new Regex(uniformPattern, RegexOptions.Singleline);
                    }

                    return regex;
                },
                u => u.Attributes,
                false,
                true
            );
        }

        private static string FormatUniform(UniformModel uniform)
        {
            string result = $"uniform {uniform.Type} {uniform.Name}";
            if (uniform.ArraySize.HasValue)
            {
                result += $"[{uniform.ArraySize}]";
            }
            result += ";";
            return result;
        }

        public static string ResolveStructurePlacement(string sourceShader, List<GlslStructModel> structures)
        {
            return ResolveElementsPlacement(
                sourceShader,
                structures,
                s => s.FullText,
                FormatStructure,
                (structure, shader) => {
                    string structPattern = $"struct\\s+{structure.Name}\\s*\\{{[^}}]+\\}}\\s*;?";
                    return new Regex(structPattern, RegexOptions.Singleline);
                },
                s => s.Attributes,
                true,
                true
            );
        }

        private static string FormatStructure(GlslStructModel structure)
        {
            var sb = new StringBuilder();
            sb.Append($"struct {structure.Name} {{");

            foreach (var field in structure.Fields)
            {
                sb.Append($"\n    {field.Type} {field.Name}");
                if (field.ArraySize.HasValue)
                {
                    sb.Append($"[{field.ArraySize}]");
                }
                sb.Append(";");
            }

            sb.Append("\n};");
            return sb.ToString();
        }

        public static string ResolveMethodPlacement(string sourceShader, List<GlslMethodInfo> methods)
        {
            return ResolveElementsPlacement(
                sourceShader,
                methods,
                m => m.FullMethodText,
                m => m.FullMethodText,
                (method, shader) => null,
                m => m.Attributes,
                true,
                false
            );
        }

        public static string ResolveStructInstancePlacement(string sourceShader, List<GlslStructInstance> structInstances)
        {
            var nonUniformInstances = structInstances.Where(i => !i.IsUniform).ToList();

            return ResolveElementsPlacement(
                sourceShader,
                nonUniformInstances,
                si => si.FullText,
                FormatStructInstance,
                (instance, shader) => {
                    string pattern = $"{Regex.Escape(instance.Structure.Name)}\\s+{Regex.Escape(instance.InstanceName)}";
                    if (instance.ArraySize.HasValue)
                    {
                        pattern += $"\\s*\\[(?:{instance.ArraySize}|\\w+)\\]";
                    }
                    pattern += "(?:\\s*=\\s*[^;]+)?\\s*;";

                    return new Regex(pattern, RegexOptions.Singleline);
                },
                si => si.Attributes,
                false
            );
        }

        private static string FormatStructInstance(GlslStructInstance instance)
        {
            string result = $"{instance.Structure.Name} {instance.InstanceName}";
            if (instance.ArraySize.HasValue)
            {
                result += $"[{instance.ArraySize.Value}]";
            }
            result += ";";
            return result;
        }


        //#region const
        //public static string ResolveConstantPlacement(string sourceShader, List<GlslConstantModel> constants)
        //{
        //    int vertexSectionIndex = sourceShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
        //    int fragmentSectionIndex = sourceShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

        //    if (vertexSectionIndex == -1 || fragmentSectionIndex == -1)
        //    {
        //        throw new FormatException("Shader source must contain both #vertex and #fragment sections");
        //    }

        //    string resultShader = new string(sourceShader.ToCharArray());

        //    List<string> constantsToAddToVertex = new List<string>();
        //    List<string> constantsToAddToFragment = new List<string>();
        //    List<string> constantsToRemove = new List<string>();

        //    foreach (var constant in constants)
        //    {
        //        var placeTargetAttr = constant.Attributes
        //            .FirstOrDefault(a => a.Name.Equals("placetarget", StringComparison.OrdinalIgnoreCase));

        //        if (placeTargetAttr == null)
        //        {
        //            continue;
        //        }

        //        string constantText = constant.FullText;
        //        if (string.IsNullOrEmpty(constantText))
        //        {
        //            constantText = FormatConstant(constant);
        //        }

        //        int constantPosition = resultShader.IndexOf(constantText, StringComparison.OrdinalIgnoreCase);

        //        if (constantPosition == -1)
        //        {
        //            string constantPattern = $"const\\s+{constant.Type}\\s+{constant.Name}\\s*=\\s*[^;]+;";
        //            var regex = new Regex(constantPattern, RegexOptions.Singleline);
        //            var match = regex.Match(resultShader);

        //            if (match.Success)
        //            {
        //                constantText = match.Value;
        //                constantPosition = match.Index;
        //            }
        //            else
        //            {
        //                continue;
        //            }
        //        }

        //        bool isInVertexSection = constantPosition > vertexSectionIndex && constantPosition < fragmentSectionIndex;
        //        bool isInFragmentSection = constantPosition > fragmentSectionIndex;

        //        bool shouldBeInVertex = placeTargetAttr.Value.Equals("vertex", StringComparison.OrdinalIgnoreCase) ||
        //                               placeTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

        //        bool shouldBeInFragment = placeTargetAttr.Value.Equals("fragment", StringComparison.OrdinalIgnoreCase) ||
        //                                 placeTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

        //        if (shouldBeInVertex && !isInVertexSection)
        //        {
        //            constantsToAddToVertex.Add(constantText);
        //        }

        //        if (shouldBeInFragment && !isInFragmentSection)
        //        {
        //            constantsToAddToFragment.Add(constantText);
        //        }

        //        if ((isInVertexSection && !shouldBeInVertex) ||
        //            (isInFragmentSection && !shouldBeInFragment))
        //        {
        //            constantsToRemove.Add(constantText);
        //        }
        //    }

        //    foreach (var constantText in constantsToRemove)
        //    {
        //        resultShader = resultShader.Replace(constantText, "");
        //    }

        //    vertexSectionIndex = resultShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
        //    fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

        //    int vertexMainIndex = resultShader.IndexOf("void main()", vertexSectionIndex,
        //        fragmentSectionIndex - vertexSectionIndex, StringComparison.OrdinalIgnoreCase);

        //    int fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
        //        StringComparison.OrdinalIgnoreCase);

        //    if (vertexMainIndex == -1 || fragmentMainIndex == -1)
        //    {
        //        throw new FormatException("Both vertex and fragment sections must contain a main function");
        //    }

        //    if (constantsToAddToVertex.Count > 0)
        //    {
        //        string vertexConstantsToAdd = string.Join("\n", constantsToAddToVertex);
        //        resultShader = resultShader.Insert(vertexMainIndex, vertexConstantsToAdd + "\n\n");
        //    }

        //    fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);
        //    fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
        //        StringComparison.OrdinalIgnoreCase);

        //    if (constantsToAddToFragment.Count > 0)
        //    {
        //        string fragmentConstantsToAdd = string.Join("\n", constantsToAddToFragment);
        //        resultShader = resultShader.Insert(fragmentMainIndex, fragmentConstantsToAdd + "\n\n");
        //    }

        //    return resultShader;
        //}

        //private static string FormatConstant(GlslConstantModel constant)
        //{
        //    return $"const {constant.Type} {constant.Name} = {constant.Value};";
        //}

        //#endregion

        //#region Uniform-блоки
        //public static string ResolveUniformBlockPlacement(string sourceShader, List<UniformBlockModel> uniformBlocks)
        //{
        //    int vertexSectionIndex = sourceShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
        //    int fragmentSectionIndex = sourceShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

        //    if (vertexSectionIndex == -1 || fragmentSectionIndex == -1)
        //    {
        //        throw new FormatException("Shader source must contain both #vertex and #fragment sections");
        //    }

        //    string resultShader = new string(sourceShader.ToCharArray());

        //    List<string> blocksToAddToVertex = new List<string>();
        //    List<string> blocksToAddToFragment = new List<string>();
        //    List<string> blocksToRemove = new List<string>();

        //    foreach (var block in uniformBlocks)
        //    {
        //        var placeTargetAttr = block.Attributes
        //            .FirstOrDefault(a => a.Name.Equals("placetarget", StringComparison.OrdinalIgnoreCase));

        //        if (placeTargetAttr == null)
        //        {
        //            continue;
        //        }

        //        string blockText = block.FullText;
        //        if (string.IsNullOrEmpty(blockText))
        //        {
        //            blockText = FormatUniformBlock(block);
        //        }

        //        int blockPosition = resultShader.IndexOf(blockText, StringComparison.OrdinalIgnoreCase);

        //        if (blockPosition == -1)
        //        {
        //            string blockPattern = $"(?:layout\\s*\\(\\s*(?:std140|std430|packed|shared)(?:\\s*,\\s*binding\\s*=\\s*\\d+)?\\)\\s*)?" +
        //                                 $"uniform\\s+{Regex.Escape(block.Name)}?\\s*\\{{[^}}]+\\}}\\s*{Regex.Escape(block.InstanceName ?? "")}?\\s*;";

        //            var regex = new Regex(blockPattern, RegexOptions.Singleline);
        //            var match = regex.Match(resultShader);

        //            if (match.Success)
        //            {
        //                blockText = match.Value;
        //                blockPosition = match.Index;
        //            }
        //            else
        //            {
        //                continue;
        //            }
        //        }

        //        bool isInVertexSection = blockPosition > vertexSectionIndex && blockPosition < fragmentSectionIndex;
        //        bool isInFragmentSection = blockPosition > fragmentSectionIndex;

        //        bool shouldBeInVertex = placeTargetAttr.Value.Equals("vertex", StringComparison.OrdinalIgnoreCase) ||
        //                               placeTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

        //        bool shouldBeInFragment = placeTargetAttr.Value.Equals("fragment", StringComparison.OrdinalIgnoreCase) ||
        //                                 placeTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

        //        if (shouldBeInVertex && !isInVertexSection)
        //        {
        //            blocksToAddToVertex.Add(blockText);
        //        }

        //        if (shouldBeInFragment && !isInFragmentSection)
        //        {
        //            blocksToAddToFragment.Add(blockText);
        //        }

        //        if ((isInVertexSection && !shouldBeInVertex) ||
        //            (isInFragmentSection && !shouldBeInFragment))
        //        {
        //            blocksToRemove.Add(blockText);
        //        }
        //    }

        //    foreach (var blockText in blocksToRemove)
        //    {
        //        resultShader = resultShader.Replace(blockText, "");
        //    }

        //    vertexSectionIndex = resultShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
        //    fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

        //    int vertexMainIndex = resultShader.IndexOf("void main()", vertexSectionIndex,
        //        fragmentSectionIndex - vertexSectionIndex, StringComparison.OrdinalIgnoreCase);

        //    int fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
        //        StringComparison.OrdinalIgnoreCase);

        //    if (vertexMainIndex == -1 || fragmentMainIndex == -1)
        //    {
        //        throw new FormatException("Both vertex and fragment sections must contain a main function");
        //    }

        //    if (blocksToAddToVertex.Count > 0)
        //    {
        //        string vertexBlocksToAdd = string.Join("\n\n", blocksToAddToVertex);
        //        resultShader = resultShader.Insert(vertexMainIndex, vertexBlocksToAdd + "\n\n");
        //    }

        //    fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);
        //    fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
        //        StringComparison.OrdinalIgnoreCase);

        //    if (blocksToAddToFragment.Count > 0)
        //    {
        //        string fragmentBlocksToAdd = string.Join("\n\n", blocksToAddToFragment);
        //        resultShader = resultShader.Insert(fragmentMainIndex, fragmentBlocksToAdd + "\n\n");
        //    }

        //    return resultShader;
        //}

        //private static string FormatUniformBlock(UniformBlockModel block)
        //{
        //    var sb = new StringBuilder();

        //    if (block.UniformBlockType != LayoutType.Ordinary || block.Binding.HasValue)
        //    {
        //        sb.Append("layout(");

        //        if (block.UniformBlockType != LayoutType.Ordinary)
        //        {
        //            sb.Append(block.UniformBlockType.ToString().ToLower());
        //        }

        //        if (block.Binding.HasValue)
        //        {
        //            if (block.UniformBlockType != LayoutType.Ordinary)
        //            {
        //                sb.Append(", ");
        //            }
        //            sb.Append($"binding = {block.Binding.Value}");
        //        }

        //        sb.Append(") ");
        //    }

        //    sb.Append("uniform ");
        //    if (!string.IsNullOrEmpty(block.Name) && block.Name != block.InstanceName)
        //    {
        //        sb.Append(block.Name + " ");
        //    }

        //    sb.Append("{\n");
        //    foreach (var field in block.Fields)
        //    {
        //        sb.Append($"    {field.Type} {field.Name}");
        //        if (field.ArraySize.HasValue)
        //        {
        //            sb.Append($"[{field.ArraySize.Value}]");
        //        }
        //        sb.Append(";\n");
        //    }
        //    sb.Append("}");

        //    if (!string.IsNullOrEmpty(block.InstanceName))
        //    {
        //        sb.Append(" " + block.InstanceName);
        //    }

        //    sb.Append(";");

        //    return sb.ToString();
        //}

        //#endregion

        //#region Uniforms
        //public static string ResolveUniformPlacement(string sourceShader, List<UniformModel> uniforms)
        //{
        //    int vertexSectionIndex = sourceShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
        //    int fragmentSectionIndex = sourceShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

        //    if (vertexSectionIndex == -1 || fragmentSectionIndex == -1)
        //    {
        //        throw new FormatException("Shader source must contain both #vertex and #fragment sections");
        //    }

        //    string resultShader = new string(sourceShader.ToCharArray());

        //    List<string> uniformsToAddToVertex = new List<string>();
        //    List<string> uniformsToAddToFragment = new List<string>();
        //    List<string> uniformsToRemove = new List<string>();

        //    foreach (var uniform in uniforms)
        //    {
        //        var placeTargetAttr = uniform.Attributes
        //            .FirstOrDefault(a => a.Name.Equals("placetarget", StringComparison.OrdinalIgnoreCase));

        //        if (placeTargetAttr == null)
        //        {
        //            continue;
        //        }

        //        string uniformText = uniform.FullText;
        //        if (string.IsNullOrEmpty(uniformText))
        //        {
        //            uniformText = FormatUniform(uniform);
        //        }

        //        int uniformPosition = resultShader.IndexOf(uniformText, StringComparison.OrdinalIgnoreCase);

        //        if (uniformPosition == -1)
        //        {
        //            string uniformPattern = $"uniform\\s+{uniform.Type}\\s+{uniform.Name}";
        //            if (uniform.ArraySize.HasValue)
        //            {
        //                uniformPattern += $"\\s*\\[\\s*(?:{uniform.ArraySize}|\\w+)\\s*\\]";
        //            }
        //            uniformPattern += "\\s*;";

        //            var regex = new Regex(uniformPattern, RegexOptions.Singleline);
        //            var match = regex.Match(resultShader);

        //            if (match.Success)
        //            {
        //                uniformText = match.Value;
        //                uniformPosition = match.Index;
        //            }
        //            else
        //            {
        //                uniformPattern = $"uniform\\s+{uniform.Type}\\s+{uniform.Name}\\s*(?:\\[[^\\]]*\\])?\\s*;";
        //                regex = new Regex(uniformPattern, RegexOptions.Singleline);
        //                match = regex.Match(resultShader);

        //                if (match.Success)
        //                {
        //                    uniformText = match.Value;
        //                    uniformPosition = match.Index;
        //                }
        //                else
        //                {
        //                    continue;
        //                }
        //            }
        //        }

        //        bool isInVertexSection = uniformPosition > vertexSectionIndex && uniformPosition < fragmentSectionIndex;
        //        bool isInFragmentSection = uniformPosition > fragmentSectionIndex;

        //        bool shouldBeInVertex = placeTargetAttr.Value.Equals("vertex", StringComparison.OrdinalIgnoreCase) ||
        //                               placeTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

        //        bool shouldBeInFragment = placeTargetAttr.Value.Equals("fragment", StringComparison.OrdinalIgnoreCase) ||
        //                                 placeTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

        //        if (shouldBeInVertex && !isInVertexSection)
        //        {
        //            uniformsToAddToVertex.Add(uniformText);
        //        }

        //        if (shouldBeInFragment && !isInFragmentSection)
        //        {
        //            uniformsToAddToFragment.Add(uniformText);
        //        }

        //        if ((isInVertexSection && !shouldBeInVertex) ||
        //            (isInFragmentSection && !shouldBeInFragment))
        //        {
        //            uniformsToRemove.Add(uniformText);
        //        }
        //    }

        //    foreach (var uniformText in uniformsToRemove)
        //    {
        //        resultShader = resultShader.Replace(uniformText, "");
        //    }

        //    vertexSectionIndex = resultShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
        //    fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

        //    int vertexMainIndex = resultShader.IndexOf("void main()", vertexSectionIndex,
        //        fragmentSectionIndex - vertexSectionIndex, StringComparison.OrdinalIgnoreCase);

        //    int fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
        //        StringComparison.OrdinalIgnoreCase);

        //    if (vertexMainIndex == -1 || fragmentMainIndex == -1)
        //    {
        //        throw new FormatException("Both vertex and fragment sections must contain a main function");
        //    }

        //    if (uniformsToAddToVertex.Count > 0)
        //    {
        //        string vertexUniformsToAdd = string.Join("\n", uniformsToAddToVertex);
        //        resultShader = resultShader.Insert(vertexMainIndex, vertexUniformsToAdd + "\n\n");
        //    }

        //    fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);
        //    fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
        //        StringComparison.OrdinalIgnoreCase);

        //    if (uniformsToAddToFragment.Count > 0)
        //    {
        //        string fragmentUniformsToAdd = string.Join("\n", uniformsToAddToFragment);
        //        resultShader = resultShader.Insert(fragmentMainIndex, fragmentUniformsToAdd + "\n\n");
        //    }

        //    return resultShader;
        //}

        //private static string FormatUniform(UniformModel uniform)
        //{
        //    string result = $"uniform {uniform.Type} {uniform.Name}";
        //    if (uniform.ArraySize.HasValue)
        //    {
        //        result += $"[{uniform.ArraySize}]";
        //    }
        //    result += ";";
        //    return result;
        //}

        //#endregion

        //#region Structs
        //public static string ResolveStructurePlacement(string sourceShader, List<GlslStructureModel> structures)
        //{
        //    int vertexSectionIndex = sourceShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
        //    int fragmentSectionIndex = sourceShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

        //    if (vertexSectionIndex == -1 || fragmentSectionIndex == -1)
        //    {
        //        throw new FormatException("Shader source must contain both #vertex and #fragment sections");
        //    }

        //    string resultShader = new string(sourceShader.ToCharArray());

        //    List<string> structsToAddToVertex = new List<string>();
        //    List<string> structsToAddToFragment = new List<string>();
        //    List<string> structsToRemove = new List<string>();

        //    foreach (var structure in structures)
        //    {
        //        var placeTargetAttr = structure.Attributes
        //            .FirstOrDefault(a => a.Name.Equals("placetarget", StringComparison.OrdinalIgnoreCase));

        //        if (placeTargetAttr == null)
        //        {
        //            continue;
        //        }

        //        string structText = structure.FullText;
        //        if (string.IsNullOrEmpty(structText))
        //        {
        //            structText = FormatStructure(structure);
        //        }

        //        int structPosition = resultShader.IndexOf(structText, StringComparison.OrdinalIgnoreCase);

        //        if (structPosition == -1)
        //        {
        //            string structPattern = $"struct\\s+{structure.Name}\\s*\\{{[^}}]+\\}}\\s*;?";
        //            var regex = new Regex(structPattern, RegexOptions.Singleline);
        //            var match = regex.Match(resultShader);

        //            if (match.Success)
        //            {
        //                structText = match.Value;
        //                structPosition = match.Index;

        //                if (!structText.TrimEnd().EndsWith(";"))
        //                {
        //                    structText = structText.TrimEnd() + ";";
        //                }
        //            }
        //            else
        //            {
        //                continue;
        //            }
        //        }
        //        else
        //        {
        //            if (!structText.TrimEnd().EndsWith(";"))
        //            {
        //                structText = structText.TrimEnd() + ";";
        //            }
        //        }

        //        bool isInVertexSection = structPosition > vertexSectionIndex && structPosition < fragmentSectionIndex;
        //        bool isInFragmentSection = structPosition > fragmentSectionIndex;

        //        bool shouldBeInVertex = placeTargetAttr.Value.Equals("vertex", StringComparison.OrdinalIgnoreCase) ||
        //                               placeTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

        //        bool shouldBeInFragment = placeTargetAttr.Value.Equals("fragment", StringComparison.OrdinalIgnoreCase) ||
        //                                 placeTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

        //        if (shouldBeInVertex && !isInVertexSection)
        //        {
        //            structsToAddToVertex.Add(structText);
        //        }

        //        if (shouldBeInFragment && !isInFragmentSection)
        //        {
        //            structsToAddToFragment.Add(structText);
        //        }

        //        if ((isInVertexSection && !shouldBeInVertex) ||
        //            (isInFragmentSection && !shouldBeInFragment))
        //        {
        //            structsToRemove.Add(structText);
        //        }
        //    }

        //    foreach (var structText in structsToRemove)
        //    {
        //        resultShader = resultShader.Replace(structText, "");
        //    }

        //    vertexSectionIndex = resultShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
        //    fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

        //    int vertexMainIndex = resultShader.IndexOf("void main()", vertexSectionIndex,
        //        fragmentSectionIndex - vertexSectionIndex, StringComparison.OrdinalIgnoreCase);

        //    int fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
        //        StringComparison.OrdinalIgnoreCase);

        //    if (vertexMainIndex == -1 || fragmentMainIndex == -1)
        //    {
        //        throw new FormatException("Both vertex and fragment sections must contain a main function");
        //    }

        //    if (structsToAddToVertex.Count > 0)
        //    {
        //        string vertexStructsToAdd = string.Join("\n\n", structsToAddToVertex);
        //        resultShader = resultShader.Insert(vertexMainIndex, vertexStructsToAdd + "\n\n");
        //    }

        //    fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);
        //    fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
        //        StringComparison.OrdinalIgnoreCase);

        //    if (structsToAddToFragment.Count > 0)
        //    {
        //        string fragmentStructsToAdd = string.Join("\n\n", structsToAddToFragment);
        //        resultShader = resultShader.Insert(fragmentMainIndex, fragmentStructsToAdd + "\n\n");
        //    }

        //    return resultShader;
        //}

        //private static string FormatStructure(GlslStructureModel structure)
        //{
        //    var sb = new StringBuilder();
        //    sb.Append($"struct {structure.Name} {{");

        //    foreach (var field in structure.Fields)
        //    {
        //        sb.Append($"\n    {field.Type} {field.Name}");
        //        if (field.ArraySize.HasValue)
        //        {
        //            sb.Append($"[{field.ArraySize}]");
        //        }
        //        sb.Append(";");
        //    }

        //    sb.Append("\n};");
        //    return sb.ToString();
        //}

        //#endregion

        //#region Methods
        //public static string ResolveMethodPlacement(string sourceShader, List<GlslMethodInfo> methods)
        //{
        //    int vertexSectionIndex = sourceShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
        //    int fragmentSectionIndex = sourceShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

        //    if (vertexSectionIndex == -1 || fragmentSectionIndex == -1)
        //    {
        //        throw new FormatException("Shader source must contain both #vertex and #fragment sections");
        //    }

        //    string resultShader = new string(sourceShader.ToCharArray());

        //    List<string> methodsToAddToVertex = new List<string>();
        //    List<string> methodsToAddToFragment = new List<string>();
        //    List<string> methodsToRemove = new List<string>();

        //    foreach (var method in methods)
        //    {
        //        var placeTargetAttr = method.Attributes
        //            .FirstOrDefault(a => a.Name.Equals("placetarget", StringComparison.OrdinalIgnoreCase));

        //        if (placeTargetAttr == null)
        //        {
        //            continue;
        //        }

        //        int methodPosition = resultShader.IndexOf(method.FullMethodText, StringComparison.OrdinalIgnoreCase);
        //        if (methodPosition == -1)
        //        {
        //            continue;
        //        }

        //        bool isInVertexSection = methodPosition > vertexSectionIndex && methodPosition < fragmentSectionIndex;
        //        bool isInFragmentSection = methodPosition > fragmentSectionIndex;

        //        bool shouldBeInVertex = placeTargetAttr.Value.Equals("vertex", StringComparison.OrdinalIgnoreCase) ||
        //                              placeTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

        //        bool shouldBeInFragment = placeTargetAttr.Value.Equals("fragment", StringComparison.OrdinalIgnoreCase) ||
        //                                placeTargetAttr.Value.Equals("both", StringComparison.OrdinalIgnoreCase);

        //        if (shouldBeInVertex && !isInVertexSection)
        //        {
        //            methodsToAddToVertex.Add(method.FullMethodText);
        //        }

        //        if (shouldBeInFragment && !isInFragmentSection)
        //        {
        //            methodsToAddToFragment.Add(method.FullMethodText);
        //        }

        //        if ((isInVertexSection && !shouldBeInVertex) ||
        //            (isInFragmentSection && !shouldBeInFragment))
        //        {
        //            methodsToRemove.Add(method.FullMethodText);
        //        }
        //    }

        //    foreach (var methodText in methodsToRemove)
        //    {
        //        resultShader = resultShader.Replace(methodText, "");
        //    }

        //    vertexSectionIndex = resultShader.IndexOf("#vertex", StringComparison.OrdinalIgnoreCase);
        //    fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);

        //    int vertexMainIndex = resultShader.IndexOf("void main()", vertexSectionIndex,
        //        fragmentSectionIndex - vertexSectionIndex, StringComparison.OrdinalIgnoreCase);

        //    int fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
        //        StringComparison.OrdinalIgnoreCase);

        //    if (vertexMainIndex == -1 || fragmentMainIndex == -1)
        //    {
        //        throw new FormatException("Both vertex and fragment sections must contain a main function");
        //    }

        //    if (methodsToAddToVertex.Count > 0)
        //    {
        //        string vertexMethodsToAdd = string.Join("\n\n", methodsToAddToVertex);
        //        resultShader = resultShader.Insert(vertexMainIndex, vertexMethodsToAdd + "\n\n");
        //    }

        //    fragmentSectionIndex = resultShader.IndexOf("#fragment", StringComparison.OrdinalIgnoreCase);
        //    fragmentMainIndex = resultShader.IndexOf("void main()", fragmentSectionIndex,
        //        StringComparison.OrdinalIgnoreCase);

        //    if (methodsToAddToFragment.Count > 0)
        //    {
        //        string fragmentMethodsToAdd = string.Join("\n\n", methodsToAddToFragment);
        //        resultShader = resultShader.Insert(fragmentMainIndex, fragmentMethodsToAdd + "\n\n");
        //    }

        //    return resultShader;
        //}

        //#endregion
    }

}


/*
 
Тип layout	                Вершинный шейдер	Фрагментный шейдер	            Пример
location (входной in)	        ✅ Да	            ❌ Нет	            layout(location=0) in vec3 pos;
location (выходной out)	        ❌ Нет*	            ✅ Да	            layout(location=0) out vec4 color;
binding (uniform)	            ✅ Да	            ✅ Да	            layout(binding=0) uniform sampler2D tex;
set + binding (Vulkan)	        ✅ Да	            ✅ Да	            layout(set=0, binding=1) uniform…
std140/std430 (UBO/SSBO)	    ✅ Да	            ✅ Да	            layout(std140, binding=2) uniform Camera {…}
flat/noperspective	            ✅ Только out	    ✅ Только in	    layout(flat) out int id;
row_major/column_major	        ✅ Да	            ✅ Да	            layout(row_major) uniform Transform {…}

 */