using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;


namespace Editor
{
    public class ModelExpandableHandler
    {
        private readonly List<string> _supportedExtensions = new List<string>();
        private readonly ExpandableFileManager _fileManager;

        public ModelExpandableHandler(ExpandableFileManager fileManager)
        {
            _fileManager = fileManager;

            _supportedExtensions.Add(".obj");
            _supportedExtensions.Add(".fbx");
            _supportedExtensions.Add(".3ds");
            _supportedExtensions.Add(".blend");

            RegisterHandler();
        }

        private void RegisterHandler()
        {
            _fileManager.RegisterHandlerByExtension(
                "3D Model Viewer",
                "Позволяет просматривать и использовать внутреннюю структуру 3D-моделей",
                _supportedExtensions,
                GetModelChildItems,
                HandleModelChildItemDrag
            );
        }

        private IEnumerable<ExpandableFileItemChild> GetModelChildItems(string filePath)
        {
            var metadataManager = ServiceHub.Get<MetadataManager>();
            var metadata = metadataManager.GetMetadata(filePath) as ModelMetadata;

            if (metadata == null || metadata.MeshesData.Count == 0)
                yield break;

            // Строим иерархию дочерних элементов
            var pathToItem = new Dictionary<string, ExpandableFileItemChild>();
            var rootItems = new List<ExpandableFileItemChild>();

            // Сначала создаем все элементы
            foreach (var meshData in metadata.MeshesData)
            {
                var path = meshData.MeshPath;
                var name = string.IsNullOrEmpty(meshData.MeshName) ?
                    $"Mesh_{meshData.Index}" : meshData.MeshName;

                var item = ExpandableFileManager.CreateChildItem(
                    filePath,
                    name,
                    meshData,
                    GetPathLevel(meshData.MeshPath),
                    child => GetDisplayNameForMesh(child)
                );

                pathToItem[path] = item;

                if (string.IsNullOrEmpty(path))
                {
                    rootItems.Add(item);
                }
            }

            // Затем строим иерархию
            foreach (var meshData in metadata.MeshesData)
            {
                if (string.IsNullOrEmpty(meshData.MeshPath))
                    continue;

                var parentPath = GetParentPath(meshData.MeshPath);
                if (!string.IsNullOrEmpty(parentPath) && pathToItem.TryGetValue(parentPath, out var parentItem))
                {
                    var item = pathToItem[meshData.MeshPath];
                    parentItem.Children.Add(item);
                }
                else if (!rootItems.Contains(pathToItem[meshData.MeshPath]))
                {
                    rootItems.Add(pathToItem[meshData.MeshPath]);
                }
            }

            // Возвращаем только корневые элементы
            foreach (var item in rootItems)
            {
                yield return item;
            }
        }
        private string GetDisplayNameForMesh(ExpandableFileItemChild child)
        {
            if (child.Data is NodeModelData meshData)
            {
                string name = !string.IsNullOrEmpty(meshData.MeshName) ?
                    meshData.MeshName : $"Mesh_{meshData.Index}";

                return name;
            }

            return child.Name;
        }
        private void HandleModelChildItemDrag(ExpandableFileItemChild child, DragDropEventArgs args)
        {
            if (child.Data is NodeModelData meshData)
            {
                // Обработка перетаскивания элемента модели
                Status.SetStatus($"Перетаскивание меша {meshData.MeshName} из модели {Path.GetFileName(args.FileFullPath)}");

                // Дополнительная логика для обработки перетаскивания
                // Например, создание сущности в сцене с этим мешем
            }
        }
        public void HandleModelDropData(string jsonData)
        {
            try
            {
                var settings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    Error = (sender, errorArgs) => { errorArgs.ErrorContext.Handled = true; }
                };

                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonData, settings);

                string fileName = data.FileName;
                string fileFullPath = data.FileFullPath;
                string childName = data.ChildItem.Name;

                var metadataManager = ServiceHub.Get<MetadataManager>();
                var metadata = metadataManager.GetMetadata(fileFullPath) as ModelMetadata;

                if (metadata != null)
                {
                    var meshData = metadata.MeshesData.FirstOrDefault(m =>
                        !string.IsNullOrEmpty(m.MeshName) && m.MeshName == childName);

                    if (meshData != null)
                    {
                        Status.SetStatus($"Обработка перетаскивания меша {meshData.MeshName} из модели {fileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при обработке перетаскивания: {ex.Message}");
            }
        }
        private int GetPathLevel(string path)
        {
            if (string.IsNullOrEmpty(path))
                return 0;

            return path.Count(c => c == '/');
        }
        private string GetParentPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            int lastSlashIndex = path.LastIndexOf('/');
            if (lastSlashIndex < 0)
                return string.Empty;

            return path.Substring(0, lastSlashIndex);
        }
    }
}