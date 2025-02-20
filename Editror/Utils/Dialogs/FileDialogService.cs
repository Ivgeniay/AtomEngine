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
    /// <summary>
    /// Статический класс для работы с диалоговыми окнами открытия/сохранения файлов
    /// </summary>
    public static class FileDialogService
    {
        /// <summary>
        /// Описание фильтра файлов
        /// </summary>
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

        /// <summary>
        /// Преобразует фильтры в формат для Avalonia
        /// </summary>
        private static FilePickerFileType[] ConvertFilters(IEnumerable<FileFilter> filters)
        {
            return filters.Select(f => new FilePickerFileType(f.Name)
            {
                Patterns = f.Extensions.Select(e => $"*.{e}").ToArray(),
                MimeTypes = f.Extensions.Select(e => GetMimeType(e)).ToArray()
            }).ToArray();
        }

        /// <summary>
        /// Получает MIME-тип по расширению файла
        /// </summary>
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

        /// <summary>
        /// Открывает диалог выбора файла для загрузки
        /// </summary>
        /// <param name="window">Родительское окно</param>
        /// <param name="title">Заголовок диалога</param>
        /// <param name="filters">Фильтры файлов</param>
        /// <returns>Путь к выбранному файлу или null</returns>
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

        /// <summary>
        /// Открывает диалог выбора файла для сохранения
        /// </summary>
        /// <param name="window">Родительское окно</param>
        /// <param name="title">Заголовок диалога</param>
        /// <param name="defaultFileName">Имя файла по умолчанию</param>
        /// <param name="filters">Фильтры файлов</param>
        /// <returns>Путь к выбранному файлу или null</returns>
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
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при сохранении файла: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Асинхронно читает содержимое текстового файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>Содержимое файла</returns>
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

        /// <summary>
        /// Асинхронно записывает текст в файл
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <param name="content">Содержимое для записи</param>
        /// <returns>true в случае успеха, false в случае ошибки</returns>
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
    }
}

