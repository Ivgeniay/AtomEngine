using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Newtonsoft.Json;
using System.Linq;
using AtomEngine;
using System;

namespace Editor
{
    internal static class SceneFileHelper
    {
        /// <summary>
        /// Стандартные фильтры для файлов сцен
        /// </summary>
        public static readonly FileDialogService.FileFilter[] SceneFileFilters = new[]
        {
            new FileDialogService.FileFilter("Scene Files", "scene"),
            new FileDialogService.FileFilter("JSON Files", "json"),
            new FileDialogService.FileFilter("All Files", "*")
        };

        /// <summary>
        /// Открывает диалог и загружает файл сцены
        /// </summary>
        /// <param name="window">Родительское окно</param>
        /// <returns>Загруженные данные сцены или null</returns>
        public static async Task<ProjectScene?> OpenSceneAsync(Window window)
        {
            try
            {
                var filePath = await FileDialogService.OpenFileAsync(
                    window,
                    "Open Scene File",
                    SceneFileFilters);

                if (string.IsNullOrEmpty(filePath))
                {
                    return null;
                }

                // Читаем содержимое файла
                var fileContent = await FileDialogService.ReadTextFileAsync(filePath);
                if (string.IsNullOrEmpty(fileContent))
                {
                    DebLogger.Error("Не удалось прочитать содержимое файла");
                    return null;
                }

                // Десериализуем JSON
                ProjectScene? sceneData = JsonConvert.DeserializeObject<ProjectScene>(fileContent);

                if (sceneData != null)
                {
                    DebLogger.Info($"Сцена загружена: {sceneData.WorldName} ({sceneData.Worlds?.Count ?? 0} миров)");
                }

                return sceneData;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при загрузке сцены: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Открывает диалог и сохраняет данные сцены в файл
        /// </summary>
        /// <param name="window">Родительское окно</param>
        /// <param name="scene">Данные сцены для сохранения</param>
        /// <returns>true если сохранение успешно, иначе false</returns>
        public static async Task<(bool, string)> SaveSceneAsync(Window window, ProjectScene scene)
        {
            if (scene == null)
            {
                DebLogger.Error("Невозможно сохранить пустую сцену");
                return (false, string.Empty);
            }

            try
            {
                var fileName = string.IsNullOrEmpty(scene.WorldName)
                    ? "NewScene.scene"
                    : $"{scene.WorldName}.scene";

                string? filePath = await FileDialogService.SaveFileAsync(
                    window,
                    "Save Scene File",
                    fileName,
                    SceneFileFilters);

                if (string.IsNullOrEmpty(filePath))
                {
                    return (false, string.Empty);
                }
                scene.ScenePath = filePath;

                string jsonContent = JsonConvert.SerializeObject(scene);
                bool result = await FileDialogService.WriteTextFileAsync(filePath, jsonContent);

                if (result)
                {
                    DebLogger.Info($"Сцена сохранена в {filePath}");
                    Status.SetStatus($"Scene saved {filePath}");
                    return (true, filePath);
                }

                return (false, string.Empty);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при сохранении сцены: {ex.Message}");
                Status.SetStatus($"Scene not saved");
                return (false, string.Empty);
            }
        }

        /// <summary>
        /// Открывает диалоговое окно для создания новой сцены
        /// </summary>
        /// <param name="window">Родительское окно</param>
        /// <returns>Данные новой сцены или null</returns>
        public static WorldData CreateNewScene()
        {
            // Создаем новую пустую сцену с базовыми объектами
            var newScene = new WorldData
            {
                WorldName = "New Scene",
                IsDirty = false,
                Entities = new List<EntityData>
                {
                    new EntityData
                    {
                        Id = 0,
                        Name = "Main Camera",
                        Version = 0,
                        //Components = new List<ComponentData>
                        //{
                        //    new ComponentData
                        //    {
                        //        Type = "Camera",
                        //        Properties = new Dictionary<string, object>
                        //        {
                        //            ["Position"] = new float[] { 0, 1, -10 },
                        //            ["Rotation"] = new float[] { 0, 0, 0 },
                        //            ["FieldOfView"] = 60.0f
                        //        }
                        //    },
                        //    new ComponentData
                        //    {
                        //        Type = "Transform",
                        //        Properties = new Dictionary<string, object>
                        //        {
                        //            ["Position"] = new float[] { 0, 1, -10 },
                        //            ["Rotation"] = new float[] { 0, 0, 0 },
                        //            ["Scale"] = new float[] { 1, 1, 1 }
                        //        }
                        //    }
                        //}
                    },
                    new EntityData
                    {
                        Id = 1,
                        Name = "Directional Light",
                        Version = 0,
                        //Components = new List<ComponentData>
                        //{
                        //    new ComponentData
                        //    {
                        //        Type = "Light",
                        //        Properties = new Dictionary<string, object>
                        //        {
                        //            ["Type"] = "Directional",
                        //            ["Color"] = new float[] { 1, 0.95f, 0.84f },
                        //            ["Intensity"] = 1.0f
                        //        }
                        //    },
                        //    new ComponentData
                        //    {
                        //        Type = "Transform",
                        //        Properties = new Dictionary<string, object>
                        //        {
                        //            ["Position"] = new float[] { 0, 3, 0 },
                        //            ["Rotation"] = new float[] { 50, 30, 0 },
                        //            ["Scale"] = new float[] { 1, 1, 1 }
                        //        }
                        //    }
                        //}
                    }
                }
            };

            DebLogger.Info("Создана новая сцена");
            return newScene;
        }
    }
}
