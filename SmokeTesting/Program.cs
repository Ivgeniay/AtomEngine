using System.Numerics;
using AtomEngine;
using OpenglLib;
using Silk.NET.Maths;

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
        }

        private static void Game(App app)
        {
            //SampleMaterial sampleMaterial = new SampleMaterial(app.Gl);
            //sampleMaterial.color.Value = new Vector4D<float>(1f,1f,1f,1f);
            //sampleMaterial.light.Value.direction = new Vector3D<float>(2f, 2f, 2f);

            //ObservableVariable<Matrix4X4<float>> projection = new ObservableVariable<Matrix4X4<float>>((e) => DebLogger.Info(e));


            World world = new World();
            var camera = world.CreateEntity();
            var cameraTransform = new TransformComponent(camera);
            cameraTransform.Position = new Vector3(0, 0, 5);
            world.AddComponent(camera, cameraTransform);
            world.AddComponent(camera, new CameraComponent(camera));

            Entity cubeEntity = world.CreateEntity();
            world.AddComponent(cubeEntity, new TransformComponent(cubeEntity));
            world.AddComponent(cubeEntity, new RotateComponent(cubeEntity));
            Result<Model, Error> mb_model = ModelLoader.LoadModel("Standart/torus.obj", app.Gl, app.Assimp);
            var model = mb_model.Unwrap();
            var mesh = model.Meshes[0];
            var cubeMeshComponent = new MeshComponent(cubeEntity, mesh);
            world.AddComponent(cubeEntity, cubeMeshComponent);

            //string vertexShaderSource = ShaderLoader.LoadShader("StandartShader/Vertex.glsl", true);
            //vertexShaderSource = ShaderParser.ProcessIncludes(vertexShaderSource, "Vertex.glsl");
            //vertexShaderSource = ShaderParser.ProcessConstants(vertexShaderSource);
            //string fragmentShaderSource = ShaderLoader.LoadShader("StandartShader/Fragment.glsl", true);
            //fragmentShaderSource = ShaderParser.ProcessIncludes(fragmentShaderSource, "Fragment.glsl");
            //fragmentShaderSource = ShaderParser.ProcessConstants(fragmentShaderSource);
            //DebLogger.Info(vertexShaderSource);
            //DebLogger.Info(fragmentShaderSource);
            //Shader shader = new Shader(app.Gl, vertexShaderSource, fragmentShaderSource);

            TestMaterialMaterial shader = new TestMaterialMaterial(app.Gl);
            //shader.SetUpShader(vertexShaderSource, fragmentShaderSource);
            world.AddComponent(cubeEntity, new ShaderComponent(cubeEntity, shader));

            world.AddSystem(new RenderSystem(world));
            world.AddSystem(new RotateSystem(world));

            app.NativeWindow.Render += delta =>
            {
                world.Update(delta);
            };
        }
    }
    public struct RotateComponent : IComponent
    {
        public RotateComponent(Entity owner)
        {
            _owner = owner;
        }

        public Entity Owner => _owner;
        Entity _owner;
    }
    public class RotateSystem : ISystem
    {
        private IWorld _world;
        public IWorld World => _world;

        public RotateSystem(IWorld world)
        {
            _world = world;
        }

        public void Update(double deltaTime)
        {
            float rotationSpeed = 1.0f;
            float deltaRotation = (float)deltaTime * rotationSpeed;

            Entity[] entities = this.CreateQuery()
                .With<TransformComponent>()
                .With<RotateComponent>()
                .Build();

            foreach (var entity in entities)
            {
                ref var transform = ref this.GetComponent<TransformComponent>(entity);

                // Увеличиваем углы поворота
                transform.Rotation += new Vector3(
                    deltaRotation,  // вращение вокруг X
                    deltaRotation,  // вращение вокруг Y
                    deltaRotation   // вращение вокруг Z
                );

                // Нормализуем углы, чтобы они оставались в пределах [0, 2π]
                transform.Rotation = new Vector3(
                    transform.Rotation.X % (2 * MathF.PI),
                    transform.Rotation.Y % (2 * MathF.PI),
                    transform.Rotation.Z % (2 * MathF.PI)
                );
            }
        }
    }
    public class RenderSystem : ISystem
    {
        private IWorld _world;
        public IWorld World => _world;
        private Random Random = new Random();

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
                TestMaterialMaterial shader = (TestMaterialMaterial)shaderComponent.Shader;
                
                shader.MODEL = transform.GetModelMatrix().ToSilk();
                shader.VIEW = cameraComponent.ViewMatrix.ToSilk();
                shader.PROJ = cameraComponent.CreateProjectionMatrix().ToSilk();

                shader.coloring.mat.c[0] = 1.0f;
                shader.coloring.mat.c[1] = 1.0f;
                shader.coloring.mat.c[2] = 0.0f;
                shader.coloring.mat.ambient = 0.0f;
                //shader.coloring.mat 

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
