using System.Numerics;
using AtomEngine;
using OpenglLib;
using OpenglLib.Buffers;
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

            TestMaterialMaterial shader = new TestMaterialMaterial(app.Gl);
            world.AddComponent(cubeEntity, new ShaderComponent(cubeEntity, shader));

            world.AddSystem(new RenderSystem(world));
            world.AddSystem(new RotateSystem(world));

            bool isUpdate = true;

            app.Input.Keyboards[0].KeyDown += (keyboard, key, num) =>
            {
                if (key == Silk.NET.Input.Key.Space)
                {
                    isUpdate = !isUpdate;
                }
            };

            app.NativeWindow.Render += delta => { if (isUpdate) world.Render(delta); }; 
            app.NativeWindow.Render += delta => { if (isUpdate) world.Update(delta); };
            app.NativeWindow.Resize += size => { world.Resize(size.ToNumetrix()); };
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
        private QueryEntity queryEntity;

        public RotateSystem(IWorld world)
        {
            _world = world;
            queryEntity = _world.CreateEntityQuery()
                .With<TransformComponent>()
                .With<RotateComponent>();
        }

        public void Update(double deltaTime)
        {
            float rotationSpeed = 1.0f;
            float deltaRotation = (float)deltaTime * rotationSpeed;

            Entity[] entities = queryEntity.Build();

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

    public class RenderSystem : IRenderSystem
    {
        private IWorld _world;
        public IWorld World => _world;
        QueryEntity queryCameraEntity;
        QueryEntity queryRenderersEntity;


        public RenderSystem(IWorld world)
        {
            _world = world;
            queryCameraEntity = _world.CreateEntityQuery()
                .With<TransformComponent>()
                .With<CameraComponent>();

            queryRenderersEntity = _world.CreateEntityQuery()
                .With<TransformComponent>()
                .With<MeshComponent>()
                .With<ShaderComponent>();
        }


        public void Render(double deltaTime)
        {
            Entity[] cameras = queryCameraEntity.Build();

            if (cameras.Length == 0)
            {
                DebLogger.Warn("No camera found");
                return;
            }

            var camera = cameras[0];
            ref var cameraTransform = ref _world.GetComponent<TransformComponent>(camera);
            ref var cameraComponent = ref _world.GetComponent<CameraComponent>(camera);

            var cameraRotation = cameraTransform.GetRotationMatrix();
            var cameraPosition = cameraTransform.GetTranslationMatrix();
            Matrix4x4.Invert(cameraPosition * cameraRotation, out var resMatrix);
            cameraComponent.ViewMatrix = resMatrix;

            var viewProjectionMatrix = cameraComponent.ViewMatrix * cameraComponent.CreateProjectionMatrix();

            Entity[] rendererEntities = queryRenderersEntity.Build();

            foreach (var entity in rendererEntities)
            {
                ref var transform = ref _world.GetComponent<TransformComponent>(entity);
                ref var meshComponent = ref _world.GetComponent<MeshComponent>(entity);
                ref var shaderComponent = ref _world.GetComponent<ShaderComponent>(entity);

                TestMaterialMaterial shader = (TestMaterialMaterial)shaderComponent.Shader;

                shader.MODEL = transform.GetModelMatrix().ToSilk();
                shader.VIEW = cameraComponent.ViewMatrix.ToSilk();
                shader.PROJ = cameraComponent.CreateProjectionMatrix().ToSilk();

                shader.col = new Vector3D<float>(1.0f, 1.0f, 0.0f);

                meshComponent.Mesh.Draw(shaderComponent.Shader);
            }
        }

        public void Resize(Vector2 size)
        {
            
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
