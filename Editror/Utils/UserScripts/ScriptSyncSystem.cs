using System.Threading.Tasks;
using AtomEngine;
using EngineLib;
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

        public async Task<bool> RebuildProject(BuildType buildType)
        {
            bool success = false;
            var loadingManager = ServiceHub.Get<LoadingManager>();

            try
            {
                await loadingManager.RunWithLoading(async (progress) =>
                {
                    await Task.Delay(100);
                    progress.Report((0, "Start compiling..."));
                    await Task.Delay(100);

                    success = await Task.Run(() => ServiceHub.Get<ScriptProjectGenerator>().GenerateProject());
                    if (!success)
                    { 
                        progress.Report((100, "Failed"));
                        await Task.Delay(1000);
                        return;
                    }

                    progress.Report((30, "Compile..."));

                    success = await ServiceHub.Get<ScriptProjectGenerator>().BuildProject(buildType);
                    if (!success)
                    { 
                        progress.Report((100, "Failed"));
                        await Task.Delay(1000);
                        return;
                    }

                    progress.Report((70, "Loading assembly..."));

                    var assembly = await Task.Run(() =>
                        ServiceHub.Get<ScriptProjectGenerator>().LoadCompiledAssembly(buildType));

                    if (assembly == null)
                    {
                        progress.Report((100, "Failed"));
                        await Task.Delay(1000);
                        success = false;
                        return;
                    }

                    progress.Report((90, "Refresh links..."));

                    await Task.Run(() => ServiceHub.Get<EditorAssemblyManager>().UpdateScriptAssembly(assembly));

                    progress.Report((100, "Done."));
                    await Task.Delay(500);

                }, "Preparing to compile...");
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Faited to compile project: {ex.Message}");
                success = false;
            }

            return success;
        }

        public void OpenProjectInIDE(string filepath = null)
        {
            if (!_isInitialized)
            {
                return;
            }
            try
            {
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