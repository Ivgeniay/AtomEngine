﻿using Silk.NET.Windowing;
using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using System.Linq;
using OpenglLib;
using System;
using System.Text;
using EngineLib;

namespace Editor
{
    internal static class MeshCompiler
    {
        internal unsafe static CompilationMeshResult TryToCompile(FileEvent e)
        {
            CompilationMeshResult result = new CompilationMeshResult();
            result.FilePath = e.FileFullPath;
            GL gl = null;

            EditorModelManager meshManager = ServiceHub.Get<EditorModelManager>();
            var extensions = meshManager.GetExtensions().ToList();
            result.Log.AppendLine("Format checking");
            if (!extensions.Any(t => t == e.FileExtension))
            {
                result.Success = false;
                result.Log.AppendLine($"Not sopported format (${e.FileExtension})");
                result.Log.AppendLine("Avaliable:");
                foreach (var extension in extensions)
                {
                    result.Log.AppendLine($"{extension}");
                }
                return result;
            }

            try
            {
                WindowOptions options = WindowOptions.Default;
                options.Size = new Silk.NET.Maths.Vector2D<int>(1, 1);
                options.Title = "GLSL Compiler";
                options.VSync = false;
                options.ShouldSwapAutomatically = false;
                options.IsVisible = false;
                options.API = new GraphicsAPI(
                    ContextAPI.OpenGL,
                    ContextProfile.Core,
                    ContextFlags.Debug,
                    new APIVersion(3, 3)
                );

                using var window = Window.Create(options);
                window.Initialize();

                gl = window.CreateOpenGL();

                if (gl != null)
                {
                    var glVersion = gl.GetString(StringName.Version);
                    var shaderVersion = gl.GetString(StringName.ShadingLanguageVersion);

                    result.Log.AppendLine($"OpenGL версия: {*glVersion}");
                    result.Log.AppendLine($"GLSL версия: {*shaderVersion}");

                    Assimp assimp = Assimp.GetApi();

                    var mb_Model = ModelLoader.LoadModel(e.FileFullPath, assimp, false);
                    result.ModelData = mb_Model.Unwrap();
                    result.Success = true;
                }
            }
            catch (Exception ex)
            {

            }

            return result;
        }
    }

    public class CompilationMeshResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public StringBuilder Log { get; set; } = new StringBuilder();
        private ModelData modelData;
        public ModelData ModelData
        {
            get => modelData;
            set
            {
                modelData = value;
                MeshCounter = modelData.Meshes.Count;

                TextureCounter = 0;
                foreach (var mesh in modelData.Meshes)
                    TextureCounter += mesh.TextureInfos.Count();
            }
        }
        public int MeshCounter { get; set; } = 0;
        public int TextureCounter { get; set; } = 0;


        public override string ToString()
        {
            return $"{(Success ? "Успех" : "Ошибка")}: {Message}\n{Log.ToString()}";
        }
    }
}
