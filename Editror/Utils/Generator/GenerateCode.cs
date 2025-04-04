using System.IO;
using System.Threading.Tasks;
using AtomEngine;
using EngineLib;
using OpenglLib;

namespace Editor
{
    internal static class GenerateCode
    {
        public static async Task Generate(string sourcePath, string outputDirectory, string sourceGuid = null)
        {
            string assetpath = ServiceHub.Get<DirectoryExplorer>().GetPath<AssetsDirectory>();
            FileEvent fileEvent = new FileEvent();
            fileEvent.FileFullPath = sourcePath;
            fileEvent.FileName = Path.GetFileNameWithoutExtension(sourcePath);
            fileEvent.FileExtension = Path.GetExtension(sourcePath);
            fileEvent.FilePath = sourcePath.Substring(assetpath.Length);

            var result = GlslCompiler.TryToCompile(fileEvent);
            if (result.Success)
            {
                DebLogger.Info(result.Log);
                await GlslCodeGenerator.GenerateCode(sourcePath, outputDirectory, sourceGuid);
            }
            else
            {
                DebLogger.Error(result.Log);
            }
        }
    }
}
