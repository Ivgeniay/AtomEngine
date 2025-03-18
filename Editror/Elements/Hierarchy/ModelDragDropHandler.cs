using System.Collections.Generic;
using Avalonia.Controls;
using System.Numerics;
using Avalonia.Input;
using System.Linq;
using AtomEngine;
using System.IO;
using Avalonia;
using System;
using System.Runtime.CompilerServices;

namespace Editor
{
    internal class ModelDragDropHandler
    {
        private readonly HierarchyController _hierarchyController;
        private readonly EntityHierarchyOperations _operations;
        private readonly ListBox _entitiesList;
        private readonly Canvas _indicatorCanvas;
        private readonly Border _modelDropIndicator;
        private readonly SceneManager _sceneManager;

        public ModelDragDropHandler(
            HierarchyController hierarchyController, ListBox entitiesList, Canvas indicatorCanvas, Border modelDropIndicator)
        {
            _hierarchyController = hierarchyController;
            _operations = new EntityHierarchyOperations(hierarchyController);
            _entitiesList = entitiesList;
            _indicatorCanvas = indicatorCanvas;
            _modelDropIndicator = modelDropIndicator;

            _sceneManager = ServiceHub.Get<SceneManager>();
        }

        public void Initialize()
        {
            DragDrop.SetAllowDrop(_hierarchyController, true);
            DragDrop.SetAllowDrop(_entitiesList, true);

            _hierarchyController.AddHandler(DragDrop.DragEnterEvent, OnModelDragEnter);
            _hierarchyController.AddHandler(DragDrop.DragLeaveEvent, OnModelDragLeave);
            _hierarchyController.AddHandler(DragDrop.DropEvent, OnModelDrop);

            _modelDropIndicator.AddHandler(DragDrop.DropEvent, OnModelDrop);
        }
        private void OnModelDragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = DragDropEffects.Copy;

            if (CanAcceptModelDrop(e))
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void OnModelDrop(object? sender, DragEventArgs e)
        {
            _modelDropIndicator.IsVisible = false;

            if (CanAcceptModelDrop(e))
            {
                try
                {
                    var jsonData = e.Data.Get(DataFormats.Text) as string;
                    var fileEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<FileSelectionEvent>(
                        jsonData, GlobalDeserializationSettings.Settings);

                    HandleModelDrop(fileEvent);
                }
                catch (Exception ex)
                {
                    Status.SetStatus($"Ошибка при обработке перетаскивания: {ex.Message}");
                }
            }

            e.Handled = true;
        }

        private void OnModelDragEnter(object? sender, DragEventArgs e)
        {
            if (CanAcceptModelDrop(e))
            {
                //var bounds = _entitiesList.Bounds;
                var position = _entitiesList.TranslatePoint(new Point(0, 0), _indicatorCanvas);

                if (position.HasValue)
                {
                    Canvas.SetLeft(_modelDropIndicator, position.Value.X);
                    Canvas.SetTop(_modelDropIndicator, position.Value.Y);
                    //_modelDropIndicator.Width = bounds.Width;
                    //_modelDropIndicator.Height = bounds.Height;
                    _modelDropIndicator.IsVisible = true;
                }
            }
        }

        private void OnModelDragLeave(object? sender, DragEventArgs e)
        {
            _modelDropIndicator.IsVisible = false;
        }

        private bool CanAcceptModelDrop(DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Text))
            {
                try
                {
                    var jsonData = e.Data.Get(DataFormats.Text) as string;
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        var fileEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<FileSelectionEvent>(
                            jsonData, GlobalDeserializationSettings.Settings);

                        if (fileEvent != null)
                        {
                            string extension = fileEvent.FileExtension?.ToLowerInvariant();
                            if (!string.IsNullOrEmpty(extension))
                            {
                                string[] modelExtensions = { ".obj", ".fbx", ".3ds", ".blend" };
                                return modelExtensions.Contains(extension);
                            }
                        }
                    }
                }
                catch { }
            }
            return false;
        }




        public void HandleModelDrop(FileSelectionEvent fileEvent)
        {
            if (!IsModelFile(fileEvent.FileExtension))
                return;

            
            var metadataManager = ServiceHub.Get<MetadataManager>();
            var metadata = metadataManager.GetMetadata(fileEvent.FileFullPath) as ModelMetadata;

            if (metadata == null || metadata.MeshesData.Count == 0)
            {
                Status.SetStatus("Не удалось получить данные об узлах модели");
                return;
            }

            try
            {
                CreateModelHierarchy(metadata, fileEvent.FileName);
                Status.SetStatus($"Модель '{fileEvent.FileName}' успешно добавлена в иерархию");
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при создании иерархии модели: {ex.Message}");
            }
        }

        private async void CreateModelHierarchy(ModelMetadata metadata, string modelFileName)
        {
            if (metadata.MeshesData.Count == 0)
                return;

            LoadingManager loadingManager = ServiceHub.Get<LoadingManager>();

            await loadingManager.RunWithLoading(async (progress) =>
            {
                var nodePathToEntityId = new Dictionary<string, uint>();
                var entityIdToIndex = new Dictionary<uint, int>();

                string baseModelName = Path.GetFileNameWithoutExtension(modelFileName);

                var sortedNodes = metadata.MeshesData
                    .OrderBy(node => GetNodeLevel(node.MeshPath))
                    .ToList();

                string rootName = string.IsNullOrWhiteSpace(baseModelName)
                    ? _operations.GetUniqueName("Model")
                    : _operations.GetUniqueName(baseModelName);

                progress.Report((0, "Creating root"));
                _hierarchyController.CreateNewEntity(rootName);

                var rootEntity = _hierarchyController.Entities.LastOrDefault();
                if (rootEntity == EntityHierarchyItem.Null)
                    return;

                uint rootEntityId = rootEntity.Id;
                //uint rootEntityId = uint.MaxValue;
                nodePathToEntityId[""] = rootEntityId;
                entityIdToIndex[rootEntityId] = 0;

                int full = sortedNodes.Count + 1;
                int counter = 1;

                for (int i = 0; i < sortedNodes.Count; i++)
                {
                    var nodeData = sortedNodes[i];

                    progress.Report(((int)full/counter+i, $"Creating {nodeData.MeshName}"));

                    string nodeName = string.IsNullOrWhiteSpace(nodeData.MeshName)
                        ? _operations.GetUniqueName($"Node_{i + 1}")
                        : _operations.GetUniqueName(nodeData.MeshName);

                    _hierarchyController.CreateNewEntity(nodeName);
                    //if (i == 0)
                    //{
                    //    var rootEntity = _hierarchyController.Entities.LastOrDefault();
                    //    nodePathToEntityId[""] = rootEntityId;
                    //    entityIdToIndex[rootEntityId] = 0;
                    //    continue;
                    //}

                    var createdEntity = _hierarchyController.Entities.LastOrDefault();
                    if (createdEntity == EntityHierarchyItem.Null)
                        continue;

                    uint entityId = createdEntity.Id;
                    nodePathToEntityId[nodeData.MeshPath] = entityId;
                    entityIdToIndex[entityId] = i + 1;

                    string parentPath = GetParentPath(nodeData.MeshPath);
                    if (nodePathToEntityId.TryGetValue(parentPath, out uint parentId))
                    {
                        _hierarchyController.SetParent(entityId, parentId);
                        ApplyTransformation(entityId, nodeData.Matrix);
                        AddMeshComponent(entityId, metadata, nodeData);
                    }
                }

            }, "Creating entities");
        }

        private void AddMeshComponent(uint entityId, ModelMetadata metadata, NodeModelData nodeData)
        {
            if (nodeData.Index < 0) return;

            if (!SceneManager.EntityCompProvider.HasComponent<MeshComponent>(entityId))
                _sceneManager.AddComponent(entityId, typeof(MeshComponent));

            ref MeshComponent meshComponent = ref SceneManager.EntityCompProvider.GetComponent<MeshComponent>(entityId);

            var guidField = typeof(MeshComponent)
                .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .FirstOrDefault(e => e.FieldType == typeof(string) && e.Name.EndsWith("GUID"));
            
            var indexatorField = typeof(MeshComponent)
                .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                .FirstOrDefault(f => f.FieldType == typeof(string) && f.Name.EndsWith("InternalIndex"));

            TypedReference meshComponentRef = __makeref(meshComponent);
            if (guidField != null)
            {
                guidField.SetValueDirect(meshComponentRef, metadata.Guid);
            }
            if (indexatorField != null)
            {
                indexatorField.SetValueDirect(meshComponentRef, nodeData.Index.ToString());
            }
        }

        private void ApplyTransformation(uint entityId, Matrix4x4 matrix)
        {
            if (!SceneManager.EntityCompProvider.HasComponent<TransformComponent>(entityId))
                return;

            ref var transform = ref SceneManager.EntityCompProvider.GetComponent<TransformComponent>(entityId);

            MatrixUtilities.DecomposeMatrix(matrix, out Vector3 position, out Vector3 rotation, out Vector3 scale);

            transform.Position = position;
            transform.Rotation = rotation;
            transform.Scale = scale;
        }

        private int GetNodeLevel(string nodePath)
        {
            if (string.IsNullOrEmpty(nodePath))
                return 0;

            return nodePath.Count(c => c == '/') + 1;
        }

        private string GetParentPath(string nodePath)
        {
            if (string.IsNullOrEmpty(nodePath))
                return "";

            int lastSeparatorIndex = nodePath.LastIndexOf('/');
            if (lastSeparatorIndex < 0)
                return "";

            return nodePath.Substring(0, lastSeparatorIndex);
        }

        private bool IsModelFile(string extension)
        {
            if (string.IsNullOrEmpty(extension))
                return false;

            string[] modelExtensions = { ".obj", ".fbx", ".3ds", ".blend" };
            return modelExtensions.Contains(extension.ToLowerInvariant());
        }
    }

    public static class MatrixUtilities
    {
        public static void DecomposeMatrix(Matrix4x4 matrix, out Vector3 position, out Vector3 rotation, out Vector3 scale)
        {
            if (Matrix4x4.Decompose(matrix, out scale, out Quaternion rotationQuaternion, out position))
            {
                rotation = QuaternionToEulerAngles(rotationQuaternion);
            }
            else
            {
                FallbackDecomposeMatrix(matrix, out position, out rotation, out scale);
            }
        }

        private static void FallbackDecomposeMatrix(Matrix4x4 matrix, out Vector3 position, out Vector3 rotation, out Vector3 scale)
        {
            position = new Vector3(matrix.M41, matrix.M42, matrix.M43);

            scale = new Vector3(
                new Vector3(matrix.M11, matrix.M12, matrix.M13).Length(),
                new Vector3(matrix.M21, matrix.M22, matrix.M23).Length(),
                new Vector3(matrix.M31, matrix.M32, matrix.M33).Length()
            );

            Matrix4x4 rotationMatrix = new Matrix4x4(
                matrix.M11 / scale.X, matrix.M12 / scale.X, matrix.M13 / scale.X, 0,
                matrix.M21 / scale.Y, matrix.M22 / scale.Y, matrix.M23 / scale.Y, 0,
                matrix.M31 / scale.Z, matrix.M32 / scale.Z, matrix.M33 / scale.Z, 0,
                0, 0, 0, 1
            );

            rotation = ExtractEulerAngles(rotationMatrix);
        }

        public static Vector3 QuaternionToEulerAngles(Quaternion q)
        {
            float x, y, z;

            float sinX = 2 * (q.W * q.X + q.Y * q.Z);
            float cosX = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            x = (float)Math.Atan2(sinX, cosX);

            float sinY = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinY) >= 1)
                y = (float)(Math.PI / 2 * Math.Sign(sinY)); 
            else
                y = (float)Math.Asin(sinY);

            float sinZ = 2 * (q.W * q.Z + q.X * q.Y);
            float cosZ = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            z = (float)Math.Atan2(sinZ, cosZ);

            return new Vector3(
                (float)(x * 180 / Math.PI),
                (float)(y * 180 / Math.PI),
                (float)(z * 180 / Math.PI)
            );
        }

        private static Vector3 ExtractEulerAngles(Matrix4x4 rotationMatrix)
        {
            float x, y, z;

            if (Math.Abs(rotationMatrix.M31) >= 0.99999f)
            {
                y = (float)Math.PI / 2 * Math.Sign(rotationMatrix.M31);
                z = 0;
                x = (float)Math.Atan2(rotationMatrix.M12, rotationMatrix.M22);
            }
            else
            {
                y = (float)Math.Asin(-rotationMatrix.M31);
                x = (float)Math.Atan2(rotationMatrix.M32, rotationMatrix.M33);
                z = (float)Math.Atan2(rotationMatrix.M21, rotationMatrix.M11);
            }

            return new Vector3(
                (float)(x * 180 / Math.PI),
                (float)(y * 180 / Math.PI),
                (float)(z * 180 / Math.PI)
            );
        }
    }

}
