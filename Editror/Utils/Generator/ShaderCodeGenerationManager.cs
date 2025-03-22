using System.IO;
using System;

namespace Editor.Utils.Generator
{
    internal static class ShaderCodeGenerationManager
    {
        public static void GenerateShadersAndComponents(string shaderDirectory, string outputDirectory)
        {
            GlslCodeGenerator.ClearIncludeFiles();

            var getPath = Path.Combine(outputDirectory, "Generated");

            Directory.CreateDirectory(getPath);

            GlslCodeGenerator.GenerateCode(shaderDirectory, getPath, true);
            ShaderComponentGenerator.GenerateComponentsFromDirectory(getPath, getPath, $"*{GlslCodeGenerator.LABLE}.cs");
            ShaderRenderSystemGenerator.GenerateRenderSystemsFromDirectory(getPath, getPath, $"*{GlslCodeGenerator.LABLE}.cs");
        }
    }
}
