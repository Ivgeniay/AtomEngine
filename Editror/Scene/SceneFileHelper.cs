using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using System.Numerics;
using Newtonsoft.Json;
using AtomEngine;
using System;
using OpenglLib;
using OpenglLib.ECS.Components;

namespace Editor
{
    internal static class SceneFileHelper
    {
        public static readonly FileDialogService.FileFilter[] SceneFileFilters = new[]
        {
            new FileDialogService.FileFilter("Scene Files", "scene"),
            new FileDialogService.FileFilter("JSON Files", "json"),
            new FileDialogService.FileFilter("All Files", "*")
        };

        public static async Task<ProjectScene?> OpenSceneAsync(Window window)
        {
            try
            {
                var filePath = await FileDialogService.OpenFileAsync(
                    window,
                    "Open Scene",
                    SceneFileFilters);

                if (string.IsNullOrEmpty(filePath))
                {
                    return null;
                }

                var fileContent = await FileDialogService.ReadTextFileAsync(filePath);
                if (string.IsNullOrEmpty(fileContent))
                {
                    DebLogger.Error("Не удалось прочитать содержимое файла");
                    return null;
                }

                ProjectScene? sceneData = SceneSerializer.DeserializeScene(fileContent);

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

                var jsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.Auto,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize
                };

                string jsonContent = SceneSerializer.SerializeScene(scene);
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
        public static WorldData CreateDefauldWorldData()
        {
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
                        Components = new Dictionary<string, IComponent>
                        {
                            {
                                typeof(TransformComponent).FullName,
                                new TransformComponent
                                {
                                    Position = new Vector3(0, 0, -5),
                                    Scale = new Vector3(1, 1, 1),
                                    Rotation = new Vector3(180, 0, 0)
                                }
                            },
                            {
                                typeof(CameraComponent).FullName,
                                new CameraComponent(new Entity(0,0))
                            }
                        }
                    },
                    new EntityData
                    {
                        Id = 1,
                        Name = "Directional Light",
                        Version = 0,
                        Components = new Dictionary<string, IComponent>
                        {
                            {
                                typeof(TransformComponent).FullName,
                                new TransformComponent
                                {
                                    Position = Vector3.UnitX,
                                    Rotation = new Vector3(0, -90, 0),
                                    Scale = new Vector3(1, 1, 1),
                                }
                            },
                            {
                                typeof(LightComponent).FullName,
                                new LightComponent(new Entity(1,0))
                            },
                            {
                                typeof(ShadowMaterialComponent).FullName,
                                ShadowMaterialComponent.CreateShadowMaterial(new Entity(1,0), "shadow-shader-material")
                            }
                        }
                    },
                    new EntityData
                    {
                        Id = 2,
                        Name = "GlobalLightSettings",
                        Version = 0,
                        Components = new Dictionary<string, IComponent>
                        {
                            {
                                typeof(GlobalLightSettingsComponent).FullName,
                                new GlobalLightSettingsComponent(new Entity(2,0))
                            },
                            {
                                typeof(ShadowMapComponent).FullName,
                                new ShadowMapComponent(new Entity(2,0))
                            }
                        }
                    },
                    new EntityData
                    {
                        Id = 3,
                        Name = "PBRMaterial",
                        Version = 0,
                        Components = new Dictionary<string, IComponent>
                        {
                            {
                                typeof(PBRMaterialComponent).FullName,
                                new PBRMaterialComponent(new Entity(10,0))
                            }
                        }
                    },
                    new EntityData
                    {
                        Id = 10,
                        Name = "Cube",
                        Version = 0,
                        Components = new Dictionary<string, IComponent>
                        {
                            {
                                typeof(TransformComponent).FullName,
                                new TransformComponent(new Entity(10,0))
                            },
                            {
                                typeof(MaterialComponent).FullName,
                                MaterialComponent.CreateMaterial(new Entity(10,0), "pbr-shader-material")
                            },
                            {
                                typeof(MeshComponent).FullName,
                                MeshComponent.CreateCube(new Entity(10,0))
                            },
                            {
                                typeof(ColliderComponent).FullName,
                                new ColliderComponent()
                            },
                        }
                    },
                    new EntityData
                    {
                        Id = 11,
                        Name = "Platrform",
                        Version = 0,
                        Components = new Dictionary<string, IComponent>
                        { 
                            {
                                typeof(TransformComponent).FullName,
                                new TransformComponent(new Entity(11,0))
                                {
                                    Position = new Vector3(0, -3, 0),
                                    Rotation = new Vector3(0, 0, 0),
                                    Scale = new Vector3(10, 0.1f, 10)
                                }
                            },
                            {
                                typeof(MaterialComponent).FullName,
                                MaterialComponent.CreateMaterial(new Entity(11,0), "pbr-shader-material")
                            },
                            {
                                typeof(MeshComponent).FullName,
                                MeshComponent.CreateCube(new Entity(11,0))
                            },
                            {
                                typeof(ColliderComponent).FullName,
                                new ColliderComponent()
                            },
                        }
                    }
                },

            };
            return newScene;
        }

        public static List<SystemData> CreateDefaultSystems()
        {
            return new List<SystemData>
            {
                new SystemData
                {
                    SystemFullTypeName = "OpenglLib.ShadowMapSystem",
                    ExecutionOrder = 0,
                    IncludInWorld = new List<uint>{ 0 },
                    Dependencies = new List<SystemData> { },
                    Category = SystemCategory.Render,
                },
                new SystemData
                {
                    SystemFullTypeName = "OpenglLib.ShadowMapBindingSystem",
                    ExecutionOrder = 1,
                    IncludInWorld = new List<uint>{ 0 },
                    Dependencies = new List<SystemData> { },
                    Category = SystemCategory.Render,
                },

                new SystemData
                {
                    SystemFullTypeName = "OpenglLib.LightUboRenderSystem",
                    ExecutionOrder = 2,
                    IncludInWorld = new List<uint>{ 0 },
                    Dependencies = new List<SystemData> { },
                    Category = SystemCategory.Render,
                },

                new SystemData
                {
                    SystemFullTypeName = "OpenglLib.CameraUboRenderSystem",
                    ExecutionOrder = 3,
                    IncludInWorld = new List<uint>{ 0 },
                    Dependencies = new List<SystemData> { },
                    Category = SystemCategory.Render,
                },

                new SystemData
                {
                    SystemFullTypeName = "OpenglLib.PBRMaterialUboRenderSystem",
                    ExecutionOrder = 4,
                    IncludInWorld = new List<uint>{ 0 },
                    Dependencies = new List<SystemData> { },
                    Category = SystemCategory.Render,
                },

                new SystemData
                {
                    SystemFullTypeName = "OpenglLib.ViewRenderSystem",
                    ExecutionOrder = 5,
                    IncludInWorld = new List<uint>{ 0 },
                    Dependencies = new List<SystemData> { },
                    Category = SystemCategory.Render,
                },
            };
        }
    
    }
}
