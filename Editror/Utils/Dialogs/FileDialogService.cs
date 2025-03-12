using System.Collections.Generic;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using Avalonia.Controls;
using System.Linq;
using AtomEngine;
using System.IO;
using System;

namespace Editor
{
    public static class FileDialogService
    {

        public class FileFilter
        {
            public string Name { get; set; }
            public List<string> Extensions { get; set; }

            public FileFilter(string name, params string[] extensions)
            {
                Name = name;
                Extensions = extensions.ToList();
            }
        }
        private static FilePickerFileType[] ConvertFilters(IEnumerable<FileFilter> filters)
        {
            return filters.Select(f => new FilePickerFileType(f.Name)
            {
                Patterns = f.Extensions.Select(e => $"*.{e}").ToArray(),
                MimeTypes = f.Extensions.Select(e => GetMimeType(e)).ToArray()
            }).ToArray();
        }
        private static string GetMimeType(string extension)
        {
            return extension.ToLower() switch
            {
                "json" => "application/json",
                "xml" => "application/xml",
                "txt" => "text/plain",
                "cs" => "text/plain",
                "png" => "image/png",
                "jpg" or "jpeg" => "image/jpeg",
                "bmp" => "image/bmp",
                "scene" => "application/x-scene",
                "prefab" => "application/x-prefab",
                "mat" => "application/x-material",
                "shader" => "application/x-shader",
                _ => "application/octet-stream"
            };
        }

        public static async Task<string> OpenFileAsync(Window window, string title, params FileFilter[] filters)
        {
            try
            {
                var fileTypes = filters.Length > 0
                    ? ConvertFilters(filters)
                    : new[] { new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } } };

                var options = new FilePickerOpenOptions
                {
                    Title = title,
                    FileTypeFilter = fileTypes,
                    AllowMultiple = false
                };

                var result = await window.StorageProvider.OpenFilePickerAsync(options);
                if (result.Count == 0)
                {
                    return null;
                }

                return result[0].Path.LocalPath;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при открытии файла: {ex.Message}");
                return null;
            }
        }
        public static async Task<string> SaveFileAsync(Window window, string title, string defaultFileName, params FileFilter[] filters)
        {
            try
            {
                var fileTypes = filters.Length > 0
                    ? ConvertFilters(filters)
                    : new[] { new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } } };

                var options = new FilePickerSaveOptions
                {
                    Title = title,
                    FileTypeChoices = fileTypes,
                    SuggestedFileName = defaultFileName
                };

                var result = await window.StorageProvider.SaveFilePickerAsync(options);
                if (result == null)
                {
                    return null;
                }

                return result.Path.LocalPath;
            }
            catch (FileError ex)
            {
                return null;
            }
        }
        public static async Task<string> ReadTextFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    DebLogger.Error($"Файл не существует: {filePath}");
                    return null;
                }

                return await File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при чтении файла: {ex.Message}");
                return null;
            }
        }
        public static async Task<bool> WriteTextFileAsync(string filePath, string content)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    DebLogger.Error("Путь к файлу не указан");
                    return false;
                }

                // Создаем директорию, если она не существует
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(filePath, content);
                return true;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при записи файла: {ex.Message}");
                return false;
            }
        }
        public static async Task<string> ChooseFolderAsync(Window window, string v)
        {
            FolderPickerOpenOptions options = new FolderPickerOpenOptions()
            {
                Title = v,
            };

            var result = await window.StorageProvider.OpenFolderPickerAsync(options);
            if (result == null || result.Count == 0)
            {
                return null;
            }

            return result[0].Path.LocalPath;
        }
    }
}

