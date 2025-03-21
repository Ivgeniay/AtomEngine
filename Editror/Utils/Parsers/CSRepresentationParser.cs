using System.Collections.Generic;
using AtomEngine;
using Silk.NET.Maths;

namespace Editor
{
    public static class CSRepresentationParser
    {
        public static object GetDefaultValueForType(string typeName)
        {
            switch (typeName)
            {
                case "bool":
                    return false;
                case "int":
                    return 0;
                case "uint":
                    return 0u;
                case "float":
                    return 0.0f;
                case "Vector2D<float>":
                    return Vector2D<float>.Zero;
                case "Vector3D<float>":
                    return Vector3D<float>.Zero;
                case "Vector4D<float>":
                    return Vector4D<float>.Zero;

                default:
                    return null;
            }
        }

        public static void ExtractUniformProperties(string code, in Dictionary<string, object> properties, in List<string> samplers)
        {
            var propertyRegex = new System.Text.RegularExpressions.Regex(
                @"public\s+(?:unsafe\s+)?(\w+(?:<\w+>)?)\s+(\w+)\s*\{",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );

            var samplerRegex = new System.Text.RegularExpressions.Regex(
                @"public\s+void\s+(\w+)_SetTexture\s*\(OpenglLib\.Texture\s+\w+\)",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );

            var propertyMatches = propertyRegex.Matches(code);
            foreach (System.Text.RegularExpressions.Match match in propertyMatches)
            {
                if (match.Groups.Count > 2)
                {
                    string typeName = match.Groups[1].Value;
                    string propertyName = match.Groups[2].Value;

                    if (typeName.Contains("Array") || propertyName.EndsWith("Location") ||
                        typeName.Contains("Struct") || IsSamplerType(typeName))
                        continue;

                    properties.Add(propertyName, GetDefaultValueForType(typeName));
                }
            }

            var samplerMatches = samplerRegex.Matches(code);
            foreach (System.Text.RegularExpressions.Match match in samplerMatches)
            {
                if (match.Groups.Count > 1)
                {
                    string samplerName = match.Groups[1].Value;
                    samplers.Add(samplerName);
                }
            }

        }

        public static bool IsSamplerType(string typeName)
        {
            return typeName.Equals("int") && (typeName.Contains("sampler") || typeName.Contains("Sampler"));
        }

        public static string ExtractNamespace(string code)
        {
            var namespaceMatch = System.Text.RegularExpressions.Regex.Match(
                code,
                @"namespace\s+([^\s{]+)"
            );

            if (namespaceMatch.Success && namespaceMatch.Groups.Count > 1)
            {
                return namespaceMatch.Groups[1].Value;
            }

            return string.Empty;
        }
        public static string ExtractClassName(string code)
        {
            var classMatch = System.Text.RegularExpressions.Regex.Match(
                code,
                @"public(?:\s+partial)?\s+class\s+([^\s:]+)"
            );

            if (classMatch.Success && classMatch.Groups.Count > 1)
            {
                return classMatch.Groups[1].Value;
            }

            return string.Empty;
        }

    }
}
