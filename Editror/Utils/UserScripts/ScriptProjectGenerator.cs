using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.IO;
using AtomEngine;
using System;

namespace Editor
{
    internal static class ScriptProjectGenerator
    {
        private static string _assetsPath;
        private static string _scriptProjectPath;
        private static string _outputPath;
        private static Assembly _coreAssembly;
        private static string _projectName = "CSharp_Assembly";

        static ScriptProjectGenerator()
        {
            _assetsPath = DirectoryExplorer.GetPath(DirectoryType.Assets);
            _scriptProjectPath = DirectoryExplorer.GetPath(DirectoryType.CSharp_Assembly);
            _outputPath = Path.Combine(_scriptProjectPath, "bin", "Debug", "net9.0");
        }

        public static bool GenerateProject()
        {
            try
            {
                var projectFilePath = Path.Combine(_scriptProjectPath, $"{_projectName}.csproj");
                if (File.Exists(projectFilePath)) return true;

                _coreAssembly = AssemblyManager.Instance.GetCoreAssembly();

                GenerateProjectFile();
                //LinkScriptFiles();
                CreateIDEFileLinks();

                DebLogger.Debug("Проект успешно сгенерирован.");
                return true;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при генерации проекта: {ex.Message}");
                return false;
            }
        }

        public static bool BuildProject()
        {
            try
            {
                // Используем dotnet CLI для сборки проекта
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"build \"{Path.Combine(_scriptProjectPath, $"{_projectName}.csproj")}\" -c Debug",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                DebLogger.Debug(output);
                string error = process.StandardError.ReadToEnd();
                if (error != null) DebLogger.Error(error);
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    DebLogger.Error($"Ошибка сборки:\n{error}");
                    return false;
                }

                DebLogger.Debug($"Сборка завершена: {output}");
                return true;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при сборке проекта: {ex.Message}");
                return false;
            }
        }

        private static void CreateIDEFileLinks()
        {
            // Создаем файл Directory.Build.props в папке проекта
            // Этот файл будет влиять на все .csproj файлы в этой папке и подпапках
            string buildPropsContent = @"<Project>
  <PropertyGroup>
    <!-- Установка корневой директории для файлов проекта -->
    <ProjectDir>..\Assets</ProjectDir>
  </PropertyGroup>
</Project>";

            File.WriteAllText(Path.Combine(_scriptProjectPath, "Directory.Build.props"), buildPropsContent);

            // Можно также создать файл .editorconfig, который поможет настроить
            // поведение различных IDE (особенно VS и VS Code)
//            string editorConfigContent = @"# EditorConfig file
//root = true

//[*.cs]
//# Устанавливаем основную директорию для новых файлов
//file_header_template = // Этот файл должен быть сохранен в папке Assets
//";
//            File.WriteAllText(Path.Combine(_scriptProjectPath, ".editorconfig"), editorConfigContent);
        }

        private static void GenerateProjectFile()
        {
            // Создаем относительный путь от папки проекта к папке Assets
            string relativeAssetsPath = Path.GetRelativePath(_scriptProjectPath, _assetsPath);

            string csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <!-- Отключаем автоматическое включение файлов проекта -->
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <!-- Указываем корневую папку для новых элементов -->
    <RootNamespace>UserScripts</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include=""{_coreAssembly.GetName().Name}"">
      <HintPath>{_coreAssembly.Location}</HintPath>
    </Reference>
  </ItemGroup>

  <ItemGroup>
    <Compile Include=""{relativeAssetsPath}\**\*.cs"" />
  </ItemGroup>
  

</Project>
";
            File.WriteAllText(Path.Combine(_scriptProjectPath, $"{_projectName}.csproj"), csprojContent);

            // Счетчик файлов для информации
            int scriptCount = Directory.GetFiles(_assetsPath, "*.cs", SearchOption.AllDirectories).Length;
            DebLogger.Debug($"Проект создан с доступом к {scriptCount} скриптам в папке Assets");
        }


        private static void LinkScriptFiles()
        {
            // Находим все .cs файлы в папке Assets
            string[] scriptFiles = Directory.GetFiles(_assetsPath, "*.cs", SearchOption.AllDirectories);

            if (scriptFiles.Length == 0)
            {
                DebLogger.Debug("Не найдено скриптов в папке Assets");
                return;
            }

            // Обновляем проектный файл, чтобы включить ссылки на все скрипты
            var csprojPath = Path.Combine(_scriptProjectPath, $"{_projectName}.csproj");
            var csproj = File.ReadAllText(csprojPath);

            StringBuilder itemGroup = new StringBuilder();
            itemGroup.AppendLine("  <ItemGroup>");

            foreach (var file in scriptFiles)
            {
                // Создаем относительный путь от проекта до файла скрипта
                string relativePath = Path.GetRelativePath(_scriptProjectPath, file);
                itemGroup.AppendLine($"    <Compile Include=\"..\\{relativePath}\" Link=\"{Path.GetFileName(file)}\" />");
            }

            itemGroup.AppendLine("  </ItemGroup>");

            csproj = csproj.Replace("</Project>", $"{itemGroup}\n</Project>");
            File.WriteAllText(csprojPath, csproj);

            Console.WriteLine($"Добавлено скриптов: {scriptFiles.Length}");
        }

        public static Assembly LoadCompiledAssembly()
        {
            string assemblyPath = Path.Combine(_outputPath, $"{_projectName}.dll");

            if (!File.Exists(assemblyPath))
            {
                Console.WriteLine($"Не найдена скомпилированная сборка по пути: {assemblyPath}");
                return null;
            }

            try
            {
                return Assembly.LoadFrom(assemblyPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке сборки: {ex.Message}");
                return null;
            }
        }
    }
}