using System.IO;
using System;

namespace Editor.Utils.Generator
{
    internal static class ShaderCodeGenerationManager
    {
        public static void GenerateAllShadersAndComponents(string shaderDirectory, string outputDirectory)
        {
            // Очищаем зарегистрированные включаемые файлы
            GlslCodeGenerator.ClearIncludeFiles();

            // Добавляем включаемые файлы из директории шейдеров
            var includesDir = Path.Combine(shaderDirectory, "Includes");
            if (Directory.Exists(includesDir))
            {
                GlslCodeGenerator.AddIncludeFilesFromDirectory(includesDir);
            }

            // Создаём директории для вывода генерируемых файлов
            var representationsDir = Path.Combine(outputDirectory, "Representations");
            var componentsDir = Path.Combine(outputDirectory, "Components");
            var systemsDir = Path.Combine(outputDirectory, "Systems");

            Directory.CreateDirectory(representationsDir);
            Directory.CreateDirectory(componentsDir);
            Directory.CreateDirectory(systemsDir);

            // Генерируем представления шейдеров (.cs файлы из .glsl)
            var generatedMaterials = GlslCodeGenerator.GenerateCodeFromDirectory(
                shaderDirectory, representationsDir, "*.glsl", true);

            Console.WriteLine($"Generated {generatedMaterials.Count} shader representations.");

            // Генерируем компоненты из представлений шейдеров
            ShaderComponentGenerator.GenerateComponentsFromDirectory(
                representationsDir, componentsDir, "*Representation.g.cs");

            // Генерируем системы рендеринга из представлений шейдеров
            ShaderRenderSystemGenerator.GenerateRenderSystemsFromDirectory(
                representationsDir, systemsDir, "*Representation.g.cs");

            Console.WriteLine("Components and systems generation completed.");
        }
    }
}
