using AtomEngine;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    public static class TestF
    {
        public static void Execute(GL gl)
        {
            //ModelLoader modelLoader = new ModelLoader(gl);
            //var result = modelLoader.LoadModel("D:/Programming/CS/Engine/OpenglLib/Geometry/Standart/cone.obj");
            //DebLogger.Info(result);

            var vertexResult = ShaderLoader.LoadShader("StandartShader/Vertex.glsl", true);
            //DebLogger.Info("\n", vertexResult);
            //DebLogger.Info("\n=============================\n");
            vertexResult = ShaderParser.ProcessIncludes(vertexResult, "Vertex.glsl");
            //DebLogger.Info("\n", vertexResult);
            //DebLogger.Info("\n=============================\n");
            vertexResult = ShaderParser.ProcessConstants(vertexResult);
            DebLogger.Info("\n", vertexResult);
            DebLogger.Fatal("\n=============================\n\n\n");


            var fragmentResult = ShaderLoader.LoadShader("StandartShader/Fragment.glsl", true);
            //DebLogger.Info("\n", fragmentResult);
            //DebLogger.Info("\n=============================\n");
            fragmentResult = ShaderParser.ProcessIncludes(fragmentResult, "Fragment.glsl");
            //DebLogger.Info("\n", fragmentResult);
            //DebLogger.Info("\n=============================\n");
            fragmentResult = ShaderParser.ProcessConstants(fragmentResult);
            DebLogger.Info("\n", fragmentResult);
            DebLogger.Fatal("\n=============================\n\n\n");


            //string shaderSource = @"
            //        struct DirectionalLight {
            //            float intensity;
            //            float ambientStrength;
            //            vec3 direction;
            //        };

            //        struct Wrapper {
            //            DirectionalLight lighting[2];
            //            float someValue;
            //        };
            //    ";

            //var t = ShaderParser.ParseStructs(shaderSource);
            //foreach (var item in t)
            //{
            //    DebLogger.Info(item);
            //}
        }
    }
}
