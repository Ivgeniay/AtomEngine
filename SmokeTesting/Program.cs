using AtomEngine;
using OpenglLib;
using Silk.NET.Assimp;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Numerics;

namespace SmokeTesting
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ILogger logger = new DefaultLogger();
            DebLogger.AddLogger(logger);
            using App app = new App(options: new AppOptions() { Width = 800, Height = 600, Debug = true });
            app.NativeWindow.Load += () => { OnLoad(app); }; 


            app.Run();
        }

        private static void OnLoad(App app)
        {
            DebLogger.Info("Window loaded");
            Game(app);
        //    TestF.Execute(app.Gl);

            //    string vertexShaderSource = ShaderLoader.LoadShader("TestVertexShader.glsl", true);
            //    string fragmentShaderSource = ShaderLoader.LoadShader("TestFragmentShader.glsl", true);

            //    var shader = new OpenglLib.Shader(app.Gl, vertexShaderSource, fragmentShaderSource);
            //    float[] vertices = {
            //     0.5f, -0.5f, 0.0f,  // Нижний правый
            //    -0.5f, -0.5f, 0.0f,  // Нижний левый
            //     0.0f,  0.5f, 0.0f   // Верхний
            //};

            //    uint[] indices = { 0, 1, 2 };
            //    var mesh = new OpenglLib.Mesh(app.Gl, vertices, indices);

            //    app.NativeWindow.Update += delta =>
            //    {
            //        float angle = (float)(DateTime.Now.Millisecond / 1000.0f * Math.PI * 2.0);
            //        var transform = Matrix4X4.CreateRotationZ(angle);

            //        shader.SetUniform("transform", transform);
            //    };

            //    app.NativeWindow.Render += delta =>
            //    {
            //        app.Gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
            //        mesh.Draw(shader);
            //    };
        }

        private static void Game(App app)
        {
            World world = new World();
            var camera = world.CreateEntity();
            var cameraTransform = new TransformComponent(camera);
            cameraTransform.Position = new Vector3(0, 0, 5);
            world.AddComponent(camera, cameraTransform);
            world.AddComponent(camera, new CameraComponent(camera));

            var cube = world.CreateEntity();
            world.AddComponent(cube, new TransformComponent(cube));

            ModelLoader modelLoader = new ModelLoader(app.Gl);
            var result = modelLoader.LoadModel("D:/Programming/CS/Engine/OpenglLib/Geometry/Standart/cube.obj");
            OpenglLib.Mesh mesh = result.Unwrap().Children[0].Meshes[0];
            var cubeMeshComponent = new MeshComponent(cube, mesh);
            world.AddComponent(cube, cubeMeshComponent);

            string vertexShaderSource = ShaderLoader.LoadShader("StandartShader/Vertex.glsl", true);
            vertexShaderSource = ShaderParser.ProcessIncludes(vertexShaderSource, "Vertex.glsl");
            vertexShaderSource = ShaderParser.ProcessConstants(vertexShaderSource);
            string fragmentShaderSource = ShaderLoader.LoadShader("StandartShader/Fragment.glsl", true);
            fragmentShaderSource = ShaderParser.ProcessIncludes(fragmentShaderSource, "Fragment.glsl");
            fragmentShaderSource = ShaderParser.ProcessConstants(fragmentShaderSource);
            DebLogger.Info(vertexShaderSource);
            DebLogger.Info(fragmentShaderSource);
            OpenglLib.Shader shader = new OpenglLib.Shader(app.Gl, vertexShaderSource, fragmentShaderSource);
            world.AddComponent(cube, new ShaderComponent(cube, shader));

            world.AddSystem(new RenderSystem(world));

            app.NativeWindow.Render += delta =>
            {
                world.Update(delta);
            };
        }
    }

    public class RenderSystem : ISystem
    {
        private IWorld _world;
        public IWorld World => _world;
        private ShaderFields fields = new ShaderFields();

        public RenderSystem(IWorld world)
        {
            _world = world;
        }

        public void Update(double deltaTime)
        {
            Entity[] cameras = this.CreateQuery()
                .With<TransformComponent>()
                .With<CameraComponent>()
                .Build();

            if (cameras.Length == 0)
            {
                DebLogger.Warn("No camera found");
                return;
            }

            var camera = cameras[0];
            ref var cameraTransform = ref this.GetComponent<TransformComponent>(camera);
            ref var cameraComponent = ref this.GetComponent<CameraComponent>(camera);

            // Вычисляем матрицу вида
            var cameraRotation = cameraTransform.GetRotationMatrix();
            var cameraPosition = cameraTransform.GetTranslationMatrix();
            Matrix4x4.Invert(cameraPosition * cameraRotation, out var resMatrix);
            cameraComponent.ViewMatrix = resMatrix;

            // Вычисляем VP матрицу один раз
            var viewProjectionMatrix = cameraComponent.ViewMatrix * cameraComponent.CreateProjectionMatrix();

            // Рендерим объекты
            Entity[] rendererEntities = this.CreateQuery()
                .With<TransformComponent>()
                .With<MeshComponent>()
                .With<ShaderComponent>()
                .Build();

            foreach (var entity in rendererEntities)
            {
                ref var transform = ref this.GetComponent<TransformComponent>(entity);
                ref var meshComponent = ref this.GetComponent<MeshComponent>(entity);
                ref var shaderComponent = ref this.GetComponent<ShaderComponent>(entity);

                // Устанавливаем uniforms
                shaderComponent.Shader.SetUniform(fields.MODEL, transform.GetModelMatrix().ToSilk());
                shaderComponent.Shader.SetUniform(fields.VIEW, cameraComponent.ViewMatrix.ToSilk());
                shaderComponent.Shader.SetUniform(fields.PROJ, cameraComponent.CreateProjectionMatrix().ToSilk());
                // Опционально: можно передавать VP матрицу одним uniform
                // shaderComponent.Shader.SetUniform(fields.VIEW_PROJECTION, viewProjectionMatrix);

                // Рендерим меш
                meshComponent.Mesh.Draw(shaderComponent.Shader);
            }
        } 
    }

    public class DefaultLogger : ILogger
    {
        private LogLevel _logLevel;
        public LogLevel LogLevel { get => _logLevel; set => _logLevel = value; }
        public void Log(string message, LogLevel logLevel)
        {
            ConsoleColor enterColor = Console.ForegroundColor;
            ConsoleColor color = Console.ForegroundColor;
            switch (logLevel)
            {
                case LogLevel.Debug: color = ConsoleColor.White; break;
                case LogLevel.Info: color = ConsoleColor.White; break;
                case LogLevel.Warn: color = ConsoleColor.Yellow; break;
                case LogLevel.Error: color = ConsoleColor.Red; break;
                case LogLevel.Fatal: color = ConsoleColor.DarkRed; break;
            }
            Console.ForegroundColor = color;
            Console.Write($"{logLevel}:");
            Console.ForegroundColor = enterColor;
            Console.Write($"{message}\n");
        }
    }
}
