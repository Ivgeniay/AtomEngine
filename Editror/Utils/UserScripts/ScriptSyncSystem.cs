using System.Threading.Tasks;
using AtomEngine;
using System;

namespace Editor
{
    /// <summary>
    /// Класс для управления системой синхронизации файлов кода между 
    /// папкой проекта и папкой Assets
    /// </summary>
    public static class ScriptSyncSystem
    {
        private static bool _isInitialized = false;

        /// <summary>
        /// Инициализирует всю систему синхронизации скриптов
        /// </summary>
        public static async Task Initialize()
        {
            if (_isInitialized)
                return;

            try
            {
                DebLogger.Info("Инициализация системы синхронизации скриптов...");

                if (AssetFileSystem.Instance != null)
                {
                    AssetFileSystem.Instance.Initialize();
                }

                await Task.Run(() => {
                    bool success = ScriptProjectGenerator.GenerateProject();
                    if (!success)
                    {
                        DebLogger.Error("Не удалось сгенерировать проект скриптов");
                    }
                });
                ScriptProjectGenerator.Initialize();
                CodeFilesSynchronizer.Initialize();
                ProjectFileWatcher.Initialize();

                _isInitialized = true;

                DebLogger.Info("Система синхронизации скриптов успешно инициализирована");

                // 5. Выполняем компиляцию проекта после инициализации
                await Task.Run(() => {
                    //bool success = ScriptProjectGenerator.BuildProject();
                    //if (success)
                    //{
                    //    var assembly = ScriptProjectGenerator.LoadCompiledAssembly();
                    //    if (assembly != null)
                    //    {
                    //        //AssemblyManager.Instance.UpdateScriptAssembly(assembly);
                    //        DebLogger.Info("Проект скриптов успешно скомпилирован и загружен");
                    //    }
                    //    else
                    //    {
                    //        DebLogger.Error("Не удалось загрузить скомпилированную сборку");
                    //    }
                    //}
                    //else
                    //{
                    //    DebLogger.Error("Не удалось скомпилировать проект скриптов");
                    //}
                });
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при инициализации системы синхронизации скриптов: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Освобождает ресурсы системы синхронизации скриптов
        /// </summary>
        public static void Shutdown()
        {
            if (!_isInitialized)
                return;

            try
            {
                DebLogger.Info("Остановка системы синхронизации скриптов...");

                // Останавливаем все компоненты в обратном порядке
                ProjectFileWatcher.Dispose();
                CodeFilesSynchronizer.Dispose();

                _isInitialized = false;

                DebLogger.Info("Система синхронизации скриптов успешно остановлена");
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при остановке системы синхронизации скриптов: {ex.Message}");
            }
        }

        /// <summary>
        /// Перекомпилирует проект скриптов
        /// </summary>
        public static async Task<bool> RebuildProject(BuildType buildType = BuildType.Debug)
        {
            if (!_isInitialized)
            {
                DebLogger.Error("Система синхронизации скриптов не инициализирована");
                return false;
            }

            bool success = false;

            try
            {
                DebLogger.Info("Перегенерация проекта скриптов...");

                // Выполняем в отдельном потоке, чтобы не блокировать UI
                await Task.Run(() => {
                    // Обновляем файл проекта
                    success = ScriptProjectGenerator.GenerateProject();
                    if (!success)
                    {
                        DebLogger.Error("Не удалось перегенерировать проект скриптов");
                        return;
                    }

                    // Компилируем проект
                    success = ScriptProjectGenerator.BuildProject(buildType);
                    if (!success)
                    {
                        DebLogger.Error("Не удалось скомпилировать проект скриптов");
                        return;
                    }

                    // Загружаем скомпилированную сборку
                    var assembly = ScriptProjectGenerator.LoadCompiledAssembly(buildType);
                    if (assembly == null)
                    {
                        DebLogger.Error("Не удалось загрузить скомпилированную сборку");
                        success = false;
                        return;
                    }

                    // Обновляем ссылку на сборку в AssemblyManager
                    AssemblyManager.Instance.UpdateUserScriptAssembly(assembly);

                    DebLogger.Info("Проект скриптов успешно перекомпилирован и загружен");
                });
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при перекомпиляции проекта скриптов: {ex.Message}");
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Открывает проект скриптов в IDE
        /// </summary>
        public static void OpenProjectInIDE(string filepath = null)
        {
            if (!_isInitialized)
            {
                DebLogger.Error("Система синхронизации скриптов не инициализирована");
                return;
            }

            try
            {
                DebLogger.Info("Открытие проекта скриптов в IDE...");
                if (filepath != null) ScriptProjectGenerator.OpenProjectInIDE(filepath);
                else ScriptProjectGenerator.OpenProjectInIDE();
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при открытии проекта скриптов в IDE: {ex.Message}");
            }
        }
    }
}