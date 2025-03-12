using AtomEngine;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using System.Collections.Generic;

namespace Editor
{
    internal class BuildManager : IService
    {
        private SceneManager _sceneManager;
        private Window _mainWindow;

        public Task InitializeAsync()
        {
            _sceneManager = ServiceHub.Get<SceneManager>();

            return Task.CompletedTask;
        }

        public void SetMainWindow(Window window)
        {
            _mainWindow = window;
        }

        public async Task<bool> BuildProject(BuildConfig config)
        {
            try
            {
                Status.SetStatus("Начало сборки проекта...");

                if (_sceneManager.CurrentScene.IsDirty)
                {
                    var result = await ConfirmationDialog.Show(
                        _mainWindow,
                        "Предупреждение",
                        "Сцена содержит несохраненные изменения. Сохранить перед сборкой?",
                        true);

                    if (result == ConfirmationDialog.DialogResult.Cancel)
                        return false;

                    if (result == ConfirmationDialog.DialogResult.Yes)
                        await _sceneManager.HandleSaveScene();
                }

                if (string.IsNullOrEmpty(config.OutputPath))
                {
                    config.OutputPath = await FileDialogService.ChooseFolderAsync(
                        _mainWindow,
                        "Выберите папку для сборки проекта");

                    if (string.IsNullOrEmpty(config.OutputPath))
                        return false;
                }

                string buildDir = Path.Combine(config.OutputPath, config.ProjectName);
                Directory.CreateDirectory(buildDir);

                await ExportSceneData(_sceneManager.CurrentScene, buildDir);

                CopyEngineLibraries(buildDir, config.TargetPlatform);

                // 5. Создание исполняемого файла
                await CreateExecutable(buildDir, config);

                Status.SetStatus($"Сборка завершена: {buildDir}");
                return true;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при сборке проекта: {ex.Message}");
                Status.SetStatus("Ошибка при сборке проекта");
                return false;
            }
        }

        private async Task ExportSceneData(ProjectScene scene, string buildDir)
        {
            Status.SetStatus("Экспорт данных сцены...");

            string sceneDir = Path.Combine(buildDir, "Data", "Scenes");
            Directory.CreateDirectory(sceneDir);

            foreach (var world in scene.Worlds)
            {
                string worldFileName = $"{world.WorldName}.scene";
                string worldPath = Path.Combine(sceneDir, worldFileName);

                string worldData = JsonConvert.SerializeObject(world, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Formatting = Formatting.None
                });

                await File.WriteAllTextAsync(worldPath, worldData);
            }

            var worldsIndex = scene.Worlds.Select(w => new { Name = w.WorldName, Path = $"Scenes/{w.WorldName}.scene" }).ToList();
            string indexData = JsonConvert.SerializeObject(worldsIndex);
            await File.WriteAllTextAsync(Path.Combine(buildDir, "Data", "worlds.index"), indexData);
        }

        private void CopyEngineLibraries(string buildDir, BuildPlatform platform)
        {
            Status.SetStatus("Копирование библиотек движка...");
            List<string> assemblyPaths = new List<string>();

            var assemblyManager = ServiceHub.Get<AssemblyManager>();
            var assamblyTypes = Enum.GetValues<TAssembly>();
            foreach(var assemblyType in assamblyTypes)
            {
                var assembly = assemblyManager.GetAssembly(assemblyType);
                assemblyPaths.Add(assembly.Location);
            }

            string targetLibsPath = Path.Combine(buildDir, "Engine");

            Directory.CreateDirectory(targetLibsPath);

            foreach(string paths in assemblyPaths)
            {
                string fileName = Path.GetFileName(paths);
                File.Copy(paths, Path.Combine(targetLibsPath, fileName), true);
            }
        }

        private async Task CreateExecutable(string buildDir, BuildConfig config)
        {
            Status.SetStatus("Создание исполняемого файла...");

            string exeName = $"{config.ProjectName}.exe";
            string exePath = Path.Combine(buildDir, exeName);


            string templateExePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Templates",
                config.TargetPlatform.ToString(),
                "GameTemplate.exe");

            // Копируем шаблонный exe
            File.Copy(templateExePath, exePath, true);

            // Создаем конфигурационный файл для исполняемого файла
            var runtimeConfig = new
            {
                ProjectName = config.ProjectName,
                CompanyName = config.CompanyName,
                Version = config.Version,
                StartScene = _sceneManager.CurrentScene.CurrentWorldData.WorldName,
                DebugMode = config.IncludeDebugInfo
            };

            string configJson = JsonConvert.SerializeObject(runtimeConfig);
            await File.WriteAllTextAsync(Path.Combine(buildDir, "game.config"), configJson);
        }

        public async Task ShowBuildDialog()
        {
            // Метод для отображения диалога настроек сборки
            // Здесь можно реализовать UI для BuildConfig

            var config = new BuildConfig
            {
                ProjectName = _sceneManager.CurrentScene.CurrentWorldData.WorldName
            };

            // В реальном коде здесь будет открытие диалогового окна с настройками

            await BuildProject(config);
        }
    }
}
