using System.Threading.Tasks;
using AtomEngine;
using System;

namespace Editor
{
    public class ScriptSyncSystem : IService
    {
        private bool _isInitialized = false;

        public Task InitializeAsync()
        {
            if (_isInitialized)
                return Task.CompletedTask;

            return Task.Run(() =>
            {
                try
                {
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    DebLogger.Error($"Ошибка при инициализации системы синхронизации скриптов: {ex.Message}");
                    _isInitialized = false;
                }
            });
        }

        internal Task Compile()
        {
            return Task.Run(async () => {
                bool success = await ServiceHub.Get<ScriptProjectGenerator>().BuildProject();
                if (success)
                {
                    var assembly = ServiceHub.Get<ScriptProjectGenerator>().LoadCompiledAssembly();
                    if (assembly != null)
                    {
                        ServiceHub.Get<EditorAssemblyManager>().UpdateScriptAssembly(assembly);
                        DebLogger.Info("Проект скриптов успешно скомпилирован и загружен");
                    }
                    else
                    {
                        DebLogger.Error("Не удалось загрузить скомпилированную сборку");
                    }
                }
                else
                {
                    DebLogger.Error("Не удалось скомпилировать проект скриптов");
                }
            });
        }

        public async Task<bool> RebuildProject(BuildType buildType = BuildType.Debug)
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

                await Task.Run(async () => {
                    success = ServiceHub.Get<ScriptProjectGenerator>().GenerateProject();
                    if (!success)
                    {
                        DebLogger.Error("Не удалось перегенерировать проект скриптов");
                        return;
                    }

                    success = await ServiceHub.Get<ScriptProjectGenerator>().BuildProject(buildType);
                    if (!success)
                    {
                        DebLogger.Error("Не удалось скомпилировать проект скриптов");
                        return;
                    }

                    var assembly = ServiceHub.Get<ScriptProjectGenerator>().LoadCompiledAssembly(buildType);
                    if (assembly == null)
                    {
                        DebLogger.Error("Не удалось загрузить скомпилированную сборку");
                        success = false;
                        return;
                    }

                    ServiceHub.Get<EditorAssemblyManager>().UpdateScriptAssembly(assembly);

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

        public void OpenProjectInIDE(string filepath = null)
        {
            if (!_isInitialized)
            {
                DebLogger.Error("Система синхронизации скриптов не инициализирована");
                return;
            }
            try
            {
                DebLogger.Info("Открытие проекта скриптов в IDE...");
                if (filepath != null) ServiceHub.Get<ScriptProjectGenerator>().OpenProjectInIDE(filepath);
                else ServiceHub.Get<ScriptProjectGenerator>().OpenProjectInIDE();
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка при открытии проекта скриптов в IDE: {ex.Message}");
            }
        }
    }
}