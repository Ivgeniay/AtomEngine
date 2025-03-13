using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.IO;
using AtomEngine;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Editor
{
    internal class ScriptProjectGenerator : IService
    {
        private string _assetsPath;
        private string _scriptProjectPath;
        private string _outputPath;
        private Assembly _coreAssembly;
        private Assembly _renderAssembly;
        private Assembly _silkMathAssembly;
        private Assembly _silkOpenGlAssembly;
        private Assembly _silkCoreGlAssembly;
        private Assembly _newtonsoftJsonAssembly;
        private Assembly _componentGeneratorAssembly;
        private Assembly _commonLib;
        private bool _isInitialized = false;
        private string RootNamespace = "UserScripts";




        public Task InitializeAsync()
        {
            if (_isInitialized) return Task.CompletedTask;

            return Task.Run(() =>
            {
                _assetsPath = ServiceHub.Get<DirectoryExplorer>().GetPath(DirectoryType.Assets);
                _scriptProjectPath = ServiceHub.Get<DirectoryExplorer>().GetPath(DirectoryType.CSharp_Assembly);
                _outputPath = Path.Combine(_scriptProjectPath, "bin");

                _isInitialized = true;
                GenerateProject();
            });
        }

        public bool GenerateProject()
        {
            try
            {
                var projConfig = ServiceHub.Get<Configuration>().GetConfiguration<ProjectConfigurations>(ConfigurationSource.ProjectConfigs);
                var projectFilePath = Path.Combine(_scriptProjectPath, $"{projConfig}.csproj");
                if (File.Exists(projectFilePath)) return true;

                var assemblyManager = ServiceHub.Get<EditorAssemblyManager>();

                _coreAssembly = assemblyManager.GetAssembly(TAssembly.Core);
                _renderAssembly = assemblyManager.GetAssembly(TAssembly.Render);
                _silkMathAssembly = assemblyManager.GetAssembly(TAssembly.SilkMath);
                _silkOpenGlAssembly = assemblyManager.GetAssembly(TAssembly.SilkOpenGL);
                _silkCoreGlAssembly = assemblyManager.GetAssembly(TAssembly.SilkNetCore);
                _newtonsoftJsonAssembly = assemblyManager.GetAssembly(TAssembly.NewtonsoftJson);
                _componentGeneratorAssembly = assemblyManager.GetAssembly(TAssembly.ComponentGenerator);
                _commonLib = assemblyManager.GetAssembly(TAssembly.CommonLib);

                GenerateProjectFile();

                DebLogger.Debug("Проект успешно сгенерирован.");
                return true;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при генерации проекта: {ex.Message}");
                return false;
            }
        }

        public bool BuildProject(BuildType buildType = BuildType.Debug)
        {
            try
            {
                var projConfig = ServiceHub.Get<Configuration>().GetConfiguration<ProjectConfigurations>(ConfigurationSource.ProjectConfigs);
                string builtype = buildType.ToString();
                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"build \"{Path.Combine(_scriptProjectPath, $"{projConfig.AssemblyName}.csproj")}\" -c {builtype}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                DebLogger.Debug(output);
                string error = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(error)) 
                    DebLogger.Error(error);
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

        private void GenerateProjectFile()
        {
            var projConfig = ServiceHub.Get<Configuration>().GetConfiguration<ProjectConfigurations>(ConfigurationSource.ProjectConfigs);

            string relativeAssetsPath = Path.GetRelativePath(_scriptProjectPath, _assetsPath);
            string csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <RootNamespace>{RootNamespace}</RootNamespace>
  </PropertyGroup>

  <!-- Analizator Settings -->
  <PropertyGroup>
    <GeneratedScriptsFolder>Generated</GeneratedScriptsFolder>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>$(GeneratedScriptsFolder)</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>

<ItemGroup>
    <Compile Remove=""$(GeneratedScriptsFolder)\**"" />
    <EmbeddedResource Remove=""$(GeneratedScriptsFolder)\**"" />
    <None Remove=""$(GeneratedScriptsFolder)\**"" />
  </ItemGroup>

  <ItemGroup>
    <Analyzer Include=""{_componentGeneratorAssembly.Location}"" />
  </ItemGroup>

  <ItemGroup>
    <Reference Include=""{_coreAssembly.GetName().Name}"">
      <HintPath>{_coreAssembly.Location}</HintPath>
    </Reference>
    <Reference Include=""{_renderAssembly.GetName().Name}"">
      <HintPath>{_renderAssembly.Location}</HintPath>
    </Reference>
    <Reference Include=""{_silkMathAssembly.GetName().Name}"">
      <HintPath>{_silkMathAssembly.Location}</HintPath>
    </Reference>
    <Reference Include=""{_silkOpenGlAssembly.GetName().Name}"">
      <HintPath>{_silkOpenGlAssembly.Location}</HintPath>
    </Reference>
    <Reference Include=""{_silkCoreGlAssembly.GetName().Name}"">
      <HintPath>{_silkCoreGlAssembly.Location}</HintPath>
    </Reference>
    <Reference Include=""{_newtonsoftJsonAssembly.GetName().Name}"">
      <HintPath>{_newtonsoftJsonAssembly.Location}</HintPath>
    </Reference>
    <Reference Include=""{_commonLib.GetName().Name}"">
      <HintPath>{_commonLib.Location}</HintPath>
    </Reference>
  </ItemGroup>
</Project>
";
            File.WriteAllText(Path.Combine(_scriptProjectPath, $"{projConfig.AssemblyName}.csproj"), csprojContent);

            int scriptCount = Directory.GetFiles(_assetsPath, "*.cs", SearchOption.AllDirectories).Length;
            DebLogger.Debug($"Проект создан с доступом к {scriptCount} скриптам в папке Assets");
        }

        public Assembly LoadCompiledAssembly(BuildType buildType = BuildType.Debug)
        {
            var projConfig = ServiceHub.Get<Configuration>().GetConfiguration<ProjectConfigurations>(ConfigurationSource.ProjectConfigs);
            string assemblyPath = string.Empty;
            if (_outputPath.EndsWith("bin"))
            {
                assemblyPath = Path.Combine(_outputPath, buildType.ToString(), $"{projConfig.AssemblyName}.dll");
            }
            else
            {
                assemblyPath = Path.Combine(_outputPath, $"{projConfig.AssemblyName}.dll");
            }

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
        public void OpenProjectInIDE()
        {
            var projConfig = ServiceHub.Get<Configuration>().GetConfiguration<ProjectConfigurations>(ConfigurationSource.ProjectConfigs);
            string projectPath = Path.Combine(_scriptProjectPath, $"{projConfig.AssemblyName}.csproj");

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


        /// <summary>
        /// Открывает файл скрипта в IDE в контексте проекта
        /// </summary>
        /// <param name="assetFilePath">Полный путь к файлу скрипта в папке Assets</param>
        public void OpenProjectInIDE(string assetFilePath)
        {
            if (string.IsNullOrEmpty(assetFilePath))
            {
                DebLogger.Error("Путь к файлу не указан");
                OpenProjectInIDE();
                return;
            }

            // Проверяем существование файла
            if (!File.Exists(assetFilePath))
            {
                DebLogger.Error($"Файл не существует: {assetFilePath}");
                return;
            }

            var projConfig = ServiceHub.Get<Configuration>().GetConfiguration<ProjectConfigurations>(ConfigurationSource.ProjectConfigs);
            string projectPath = Path.Combine(_scriptProjectPath, $"{projConfig.AssemblyName}.csproj");

            if (!File.Exists(projectPath))
            {
                DebLogger.Error("Проект не найден. Возможно, он не был сгенерирован.");
                return;
            }

            try
            {
                string relativeToAssets = Path.GetRelativePath(_assetsPath, assetFilePath);
                string projectFilePath = Path.Combine(_scriptProjectPath, relativeToAssets);

                if (!File.Exists(projectFilePath))
                {
                    string fileName = Path.GetFileName(assetFilePath);
                    var files = Directory.GetFiles(_scriptProjectPath, fileName, SearchOption.AllDirectories);

                    if (files.Length > 0)
                    {
                        projectFilePath = files[0];
                    }
                    else
                    {
                        projectFilePath = assetFilePath;
                    }
                }

                string ideType = DetectIDE();

                switch (ideType)
                {
                    case "vs":
                        OpenInVisualStudio(projectPath, projectFilePath);
                        break;

                    case "vscode":
                        OpenInVSCode(_scriptProjectPath, projectFilePath);
                        break;

                    case "rider":
                        OpenInRider(projectPath, projectFilePath);
                        break;

                    default:
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = projectFilePath,
                            UseShellExecute = true
                        });

                        Process.Start(new ProcessStartInfo
                        {
                            FileName = projectPath,
                            UseShellExecute = true
                        });
                        break;
                }

                DebLogger.Debug($"Открыт файл в IDE: {projectFilePath}");
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при открытии файла в IDE: {ex.Message}");
                OpenProjectInIDE();
            }
        }

        /// <summary>
        /// Определяет доступную IDE
        /// </summary>
        private string DetectIDE()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                    if (Directory.Exists(Path.Combine(programFilesX86, "Microsoft Visual Studio")))
                        return "vs";

                    if (Directory.Exists(Path.Combine(programFiles, "JetBrains")) ||
                        Process.GetProcessesByName("rider64").Length > 0)
                        return "rider";

                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    if (Directory.Exists(Path.Combine(appData, "Code")) ||
                        Process.GetProcessesByName("Code").Length > 0)
                        return "vscode";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    if (Directory.Exists("/Applications/Visual Studio.app"))
                        return "vs";

                    if (Directory.Exists("/Applications/Rider.app"))
                        return "rider";

                    if (Directory.Exists("/Applications/Visual Studio Code.app"))
                        return "vscode";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    if (CommandExists("rider"))
                        return "rider";

                    if (CommandExists("code"))
                        return "vscode";
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при определении IDE: {ex.Message}");
            }

            return "unknown";
        }

        /// <summary>
        /// Проверяет наличие команды в PATH на Linux
        /// </summary>
        private bool CommandExists(string command)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "which",
                        Arguments = command,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Открывает файл в Visual Studio
        /// </summary>
        private void OpenInVisualStudio(string projectPath, string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "devenv",
                    Arguments = $"\"{projectPath}\" /edit \"{filePath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = projectPath,
                    UseShellExecute = true
                });
            }
        }

        /// <summary>
        /// Открывает файл в Visual Studio Code
        /// </summary>
        private void OpenInVSCode(string folderPath, string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "code",
                    Arguments = $"\"{folderPath}\" --goto \"{filePath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
        }

        /// <summary>
        /// Открывает файл в JetBrains Rider
        /// </summary>
        private void OpenInRider(string projectPath, string filePath)
        {
            try
            {
                string riderExecutable = "rider";

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    riderExecutable = "rider64";
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = riderExecutable,
                    Arguments = $"\"{projectPath}\" --line 1 \"{filePath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = projectPath,
                    UseShellExecute = true
                });
            }
        }

    }
}