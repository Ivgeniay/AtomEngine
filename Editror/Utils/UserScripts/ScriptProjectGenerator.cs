using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.IO;
using AtomEngine;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Runtime.Loader;
using System.Collections.Generic;
using System.Linq;
using EngineLib;

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

        private Dictionary<string, ScriptAssemblyLoadContext> _loadContexts = new Dictionary<string, ScriptAssemblyLoadContext>();
        private int cacheCounter = 0;
        private const int MaxAssembliesToKeep = 10;

        private class ScriptAssemblyLoadContext : AssemblyLoadContext
        {
            public ScriptAssemblyLoadContext(string name) : base(name, isCollectible: true)
            {}

            protected override Assembly Load(AssemblyName assemblyName)
            {
                return null;
            }
        }


        public Task InitializeAsync()
        {
            if (_isInitialized) return Task.CompletedTask;

            return Task.Run(() =>
            {
                _assetsPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<AssetsDirectory>();
                _scriptProjectPath = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<CSharp_AssemblyDirectory>();
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

        public async Task<bool> BuildProject(BuildType buildType = BuildType.Debug)
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
                        Arguments = $"build \"{Path.Combine(_scriptProjectPath, $"{projConfig.AssemblyName}.csproj")}\" -c {builtype} --no-incremental /p:ShadowCopy=true",
                        //Arguments = $"build \"{Path.Combine(_scriptProjectPath, $"{projConfig.AssemblyName}.csproj")}\" -c {builtype} --no-incremental /p:DebugType=none /p:DebugSymbols=false",
                        //Arguments = $"build \"{Path.Combine(_scriptProjectPath, $"{projConfig.AssemblyName}.csproj")}\" -c {builtype} --no-incremental",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                DebLogger.Debug(output);
                string error = await process.StandardError.ReadToEndAsync();
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
    <RootNamespace>{projConfig.RootNamespace}</RootNamespace>
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
            try
            {
                var projConfig = ServiceHub.Get<Configuration>().GetConfiguration<ProjectConfigurations>(ConfigurationSource.ProjectConfigs);

                string assemblyPath = GetAssemblyPath(buildType, projConfig);
                if (!File.Exists(assemblyPath))
                {
                    DebLogger.Error($"Не найдена скомпилированная сборка по пути: {assemblyPath}");
                    return null;
                }

                string cacheDir = ServiceHub.Get<EditorDirectoryExplorer>().GetPath<CacheDirectory>();
                string assemblyCachePath = Path.Combine(cacheDir, "AssemblyCache");
                EnsureCacheDirectory(assemblyCachePath);

                var eventHub = ServiceHub.Get<EventHub>();
                var unloadEvent = new AssemblyUnloadEvent();

                string contextKey = $"{projConfig.AssemblyName}_{buildType}";
                if (_loadContexts.TryGetValue(contextKey, out var prevContext))
                {
                    unloadEvent.Assembly = prevContext.Assemblies.FirstOrDefault();
                }
                DebLogger.Debug("Отправка события выгрузки сборки");
                eventHub.SendEvent(unloadEvent);

                string cachedAssemblyFileName = $"{projConfig.AssemblyName}_{buildType}_{DateTime.Now.Ticks}.dll";
                string cachedAssemblyPath = Path.Combine(assemblyCachePath, cachedAssemblyFileName);

                CopyAssemblyFiles(assemblyPath, cachedAssemblyPath);
                
                if (_loadContexts.TryGetValue(contextKey, out var previousContext))
                {
                    try
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                        }

                        previousContext.Unload();
                        DebLogger.Warn($"Предыдущий контекст сборки {contextKey} выгружен");
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Error($"Ошибка при выгрузке контекста: {ex.Message}");
                    }

                    _loadContexts.Remove(contextKey);
                }
                
                var loadContext = new ScriptAssemblyLoadContext(contextKey);
                _loadContexts[contextKey] = loadContext;


                var assembly = loadContext.LoadFromAssemblyPath(cachedAssemblyPath);
                var uploadEvent = new AssemblyUploadEvent
                {
                    Assembly = assembly
                };
                DebLogger.Debug("Отправка события загрузки сборки");
                ServiceHub.Get<EditorAssemblyManager>().UpdateScriptAssembly(assembly);
                eventHub.SendEvent(uploadEvent);

                CleanupOldAssemblies(assemblyCachePath, MaxAssembliesToKeep);

                DebLogger.Debug($"Загружена сборка: {cachedAssemblyPath}");
                return assembly;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при загрузке сборки: {ex.Message}");
                if (ex.InnerException != null)
                {
                    DebLogger.Error($"Внутреннее исключение: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        private string GetAssemblyPath(BuildType buildType, ProjectConfigurations projConfig)
        {
            if (_outputPath.EndsWith("bin"))
            {
                return Path.Combine(_outputPath, buildType.ToString(), $"{projConfig.AssemblyName}.dll");
            }
            return Path.Combine(_outputPath, $"{projConfig.AssemblyName}.dll");
        }

        private void EnsureCacheDirectory(string cachePath)
        {
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }
            else if (cacheCounter == 0)
            {
                try
                {
                    var di = new DirectoryInfo(cachePath);
                    foreach (var file in di.GetFiles())
                    {
                        try { file.Delete(); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    DebLogger.Debug($"Не удалось очистить кэш: {ex.Message}");
                }
            }

            cacheCounter++;
        }

        private void CopyAssemblyFiles(string sourceAssemblyPath, string destinationAssemblyPath)
        {
            File.Copy(sourceAssemblyPath, destinationAssemblyPath, true);

            string sourcePdbPath = Path.ChangeExtension(sourceAssemblyPath, ".pdb");
            if (File.Exists(sourcePdbPath))
            {
                string destPdbPath = Path.ChangeExtension(destinationAssemblyPath, ".pdb");
                File.Copy(sourcePdbPath, destPdbPath, true);
            }
        }

        private void CleanupOldAssemblies(string cacheDir, int maxToKeep)
        {
            try
            {
                var assemblyFiles = Directory.GetFiles(cacheDir, "*.dll")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .Skip(maxToKeep)
                    .ToList();

                foreach (var file in assemblyFiles)
                {
                    try
                    {
                        File.Delete(file);
                        string pdbPath = Path.ChangeExtension(file, ".pdb");
                        if (File.Exists(pdbPath))
                        {
                            File.Delete(pdbPath);
                        }

                        DebLogger.Debug($"Удален устаревший файл: {Path.GetFileName(file)}");
                    }
                    catch (Exception ex)
                    {
                        DebLogger.Debug($"Не удалось удалить {Path.GetFileName(file)}: {ex.Message}");
                    }
                }
                if (assemblyFiles.Count > 0)
                {
                    DebLogger.Debug($"Очищено {assemblyFiles.Count} устаревших файлов сборок");
                }
            }
            catch (Exception ex)
            {
                DebLogger.Debug($"Ошибка при очистке старых файлов: {ex.Message}");
            }
        }

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

        public void OpenProjectInIDE(string assetFilePath)
        {
            if (string.IsNullOrEmpty(assetFilePath))
            {
                DebLogger.Error("Путь к файлу не указан");
                OpenProjectInIDE();
                return;
            }

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