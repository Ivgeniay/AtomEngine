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
        private static string _projectNameWithExt;
        private static bool _isInitialized = false;

        static ScriptProjectGenerator()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (_isInitialized) return;

            _assetsPath = DirectoryExplorer.GetPath(DirectoryType.Assets);
            _projectNameWithExt = _projectName + ".csproj";
            _scriptProjectPath = DirectoryExplorer.GetPath(DirectoryType.CSharp_Assembly);
            _outputPath = Path.Combine(_scriptProjectPath, "bin", "Debug", "net9.0");
        }

        public static bool GenerateProject()
        {
            try
            {
                var projectFilePath = Path.Combine(_scriptProjectPath, $"{_projectNameWithExt}");
                if (File.Exists(projectFilePath)) return true;

                _coreAssembly = AssemblyManager.Instance.GetCoreAssembly();

                GenerateProjectFile();
                //LinkScriptFiles();

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
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"build \"{Path.Combine(_scriptProjectPath, $"{_projectNameWithExt}")}\" -c Debug",
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

        private static void GenerateProjectFile()
        {
            string relativeAssetsPath = Path.GetRelativePath(_scriptProjectPath, _assetsPath);
            string csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <!-- Указываем корневую папку для новых элементов -->
    <RootNamespace>UserScripts</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include=""{_coreAssembly.GetName().Name}"">
      <HintPath>{_coreAssembly.Location}</HintPath>
    </Reference>
  </ItemGroup>

</Project>
";
            File.WriteAllText(Path.Combine(_scriptProjectPath, $"{_projectName}.csproj"), csprojContent);

            int scriptCount = Directory.GetFiles(_assetsPath, "*.cs", SearchOption.AllDirectories).Length;
            DebLogger.Debug($"Проект создан с доступом к {scriptCount} скриптам в папке Assets");
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

        /// <summary>
        /// Открывает проект в IDE
        /// </summary>
        public static void OpenProjectInIDE()
        {
            string projectPath = Path.Combine(_scriptProjectPath, $"{_projectName}.csproj");

            if (!File.Exists(projectPath))
            {
                DebLogger.Error("Проект не найден. Возможно, он не был сгенерирован.");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = projectPath,
                    UseShellExecute = true
                });

                DebLogger.Debug($"Проект открыт: {projectPath}");
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при открытии проекта: {ex.Message}");
            }
        }


    }
}