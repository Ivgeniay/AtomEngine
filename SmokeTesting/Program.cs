using System.Numerics;
using AtomEngine;
using OpenglLib;
using Silk.NET.Input;
using Silk.NET.Maths;
using Texture = OpenglLib.Texture;

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
            var cameraTransform = new TransformComponent(camera, world);
            cameraTransform.Position = new Vector3(0, 0, 5);
            world.AddComponent(camera, cameraTransform);
            world.AddComponent(camera, new CameraComponent(camera));

            Entity cubeEntity = world.CreateEntity();
            world.AddComponent(cubeEntity, new TransformComponent(cubeEntity, world));
            world.AddComponent(cubeEntity, new RotateComponent(cubeEntity));
            Result<Model, Error> mb_model = ModelLoader.LoadModel(PathStorage.CUBE_OBJ, app.Gl, app.Assimp);
            var model = mb_model.Unwrap();
            var mesh = model.Meshes[0];
            var cubeMeshComponent = new MeshComponent(cubeEntity, mesh);
            world.AddComponent(cubeEntity, cubeMeshComponent);
            var boudingComponent = new BoundingComponent(cubeEntity).FromBox(mesh);
            world.AddComponent(cubeEntity, boudingComponent);
            BoudingMovedComponent boudingMovedComponent = new BoudingMovedComponent(cubeEntity);
            world.AddComponent(cubeEntity, boudingMovedComponent);

            TestMaterialMaterial shader = new TestMaterialMaterial(app.Gl);
            
            Texture texture = new Texture(app.Gl, PathStorage.WOOD_JPG);
            Texture texture2 = new Texture(app.Gl, PathStorage.ICON_LIGHT_BULB_PNG);
            shader.tex_SetTexture(texture);

            world.AddComponent(cubeEntity, new ShaderComponent(cubeEntity, shader));

            Entity cube2Entity = world.CreateEntity();
            TransformComponent cube2Transform = new TransformComponent(cube2Entity, world);
            cube2Transform.Position = new Vector3(3, 0, 0);
            world.AddComponent(cube2Entity, cube2Transform);
            world.AddComponent(cube2Entity, new RotateComponent(cube2Entity));
            mb_model = ModelLoader.LoadModel(PathStorage.CUBE_OBJ, app.Gl, app.Assimp);
            model = mb_model.Unwrap();
            mesh = model.Meshes[0];
            cubeMeshComponent = new MeshComponent(cube2Entity, mesh);
            world.AddComponent(cube2Entity, cubeMeshComponent);
            var boudingComponentCube2 = new BoundingComponent(cube2Entity).FromBox(mesh);
            world.AddComponent(cube2Entity, boudingComponentCube2);
            world.AddComponent(cube2Entity, new ShaderComponent(cube2Entity, shader));


            world.AddSystem(new RenderSystem(world));
            //world.AddSystem(new RotateSystem(world));
            world.AddSystem(new CameraMoveSystem(world, app));
            world.AddSystem(new BoundingInputSystem(world, app));
            world.AddSystem(new BoundingMoveSystem(world));
            //world.AddSystem(new TestSystem(world));

            bool isUpdate = true;

            app.Input.Keyboards[0].KeyDown += (keyboard, key, num) =>
            {
                if (key == Key.Space)
                {
                    shader.tex_SetTexture(texture2);
                    //isUpdate = !isUpdate;
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
    public struct TestComponent<T> : IComponent where T : IBoundingVolume 
    {
        public Entity Owner => _owner;
        Entity _owner;
        public TestComponent(Entity owner)
        {
            _owner = owner;
        }
    }
    public struct BoudingMovedComponent : IComponent
    {
        public Entity Owner => _owner;
        Entity _owner;
        public Vector3 Direction;
        public BoudingMovedComponent(Entity owner)
        {
            _owner = owner;
        }
    }
    public class TestSystem : ISystem
    {
        public IWorld World => _world;
        private IWorld _world;
        private QueryEntity _query;
        public TestSystem(IWorld world)
        {
            _world = world;
            _query = _world.CreateEntityQuery().With<TestComponent<IBoundingVolume>>();
        }


        public void Initialize()
        { }

        public void Update(double deltaTime)
        {
            var entity = _query.Build();
            foreach (var item in entity)
            {
                ref var bounding = ref _world.GetComponent<TestComponent<BoundingBox>>(item);
                DebLogger.Debug($"Bounding: {bounding.Owner}");
            }
        }
    }
    public class BoundingInputSystem : ISystem
    {
        public IWorld World => _world;
        private IWorld _world; 
        private QueryEntity movedComp;
        private QueryEntity sphereEntity;
        private App app;
        public BoundingInputSystem(IWorld world, App app)
        {
            this.app = app;
            _world = world;
            movedComp = this.CreateEntityQuery()
                .With<BoundingComponent>()
                .With<BoudingMovedComponent>();

            sphereEntity = this.CreateEntityQuery()
                .With<BoundingComponent>()
                .Without<BoudingMovedComponent>();

            app.Input.Keyboards[0].KeyDown += (keyboard, key, num) =>
            {
                if (key == Key.Right) IsRight = true;
                if (key == Key.Left) IsLeft = true;
                if (key == Key.Up) IsUp = true;
                if (key == Key.Down) IsDown = true;
            };
        }
        private bool IsRight = false;
        private bool IsLeft = false;
        private bool IsUp = false;
        private bool IsDown = false;
        public void Initialize() { }

        public void Update(double deltaTime)
        {
            if (!IsDown && !IsUp && !IsLeft && !IsRight) return;

            Entity[] moves = movedComp.Build();
            Entity[] spheres = sphereEntity.Build();
            foreach (var entity in moves)
            {
                //ref var bounding = ref this.GetComponent<BoxBoudingComponent>(entity);
                ref var bounding = ref this.GetComponent<BoundingComponent>(entity);
                ref var moved = ref this.GetComponent<BoudingMovedComponent>(entity);

                if (IsRight) moved.Direction += new Vector3(1, 0, 0) * (float)deltaTime * 30;
                if (IsLeft) moved.Direction += new Vector3(-1, 0, 0) * (float)deltaTime * 30;
                if (IsUp) moved.Direction += new Vector3(0, 1, 0) * (float)deltaTime * 30;
                if (IsDown) moved.Direction += new Vector3(0, -1, 0) * (float)deltaTime * 30;
            }


            IsRight = false;
            IsLeft = false;
            IsUp = false;
            IsDown = false;

            if (_world is World world)
            {
                var r = world.GetPotentialCollisions();
                foreach (var collision in r)
                {
                    DebLogger.Debug($"Collision: {collision.Item1} - {collision.Item2}");
                }
            }
        }
    }
    public class BoundingMoveSystem : ISystem
    {
        private IWorld _world;
        public IWorld World => _world;
        private QueryEntity queryEntity;

        public BoundingMoveSystem(IWorld world)
        {
            _world = world;
            queryEntity = this.CreateEntityQuery()
                .With<BoudingMovedComponent>();
        }

        public void Update(double deltaTime)
        {
            Entity[] entities = queryEntity.Build();
            foreach (var entity in entities)
            {
                ref var moved = ref this.GetComponent<BoudingMovedComponent>(entity);
                ref var transform = ref this.GetComponent<TransformComponent>(entity);
                transform.Position += moved.Direction;
                moved.Direction = Vector3.Zero;
            }
        }


        public void Initialize()
        {
        }

    }

    public class RotateSystem : ISystem
    {
        private IWorld _world;
        public IWorld World => _world;
        private QueryEntity queryEntity;

        public RotateSystem(IWorld world)
        {
            _world = world;
            queryEntity = this.CreateEntityQuery()
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

                transform.Rotation += new Vector3(
                    deltaRotation,
                    deltaRotation,
                    deltaRotation 
                );

                transform.Rotation = new Vector3(
                    transform.Rotation.X % (2 * MathF.PI),
                    transform.Rotation.Y % (2 * MathF.PI),
                    transform.Rotation.Z % (2 * MathF.PI)
                );
            }
        }

        public void Initialize() { }
    }
    public class RenderSystem : IRenderSystem
    {
        private IWorld _world;
        public IWorld World => _world;
        private QueryEntity queryCameraEntity;
        private QueryEntity queryRenderersEntity;

        public RenderSystem(IWorld world)
        { 
            _world = world;
            queryCameraEntity = this.CreateEntityQuery()
                .With<TransformComponent>()
                .With<CameraComponent>();

            queryRenderersEntity = this.CreateEntityQuery()
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
            ref var cameraTransform = ref this.GetComponent<TransformComponent>(camera);
            ref var cameraComponent = ref this.GetComponent<CameraComponent>(camera);

            var cameraRotation = cameraTransform.GetRotationMatrix();
            var cameraPosition = cameraTransform.GetTranslationMatrix();
            //Matrix4x4.Invert(cameraPosition * cameraRotation, out var resMatrix);
            cameraComponent.ViewMatrix = Matrix4x4.CreateLookAt(cameraTransform.Position, cameraTransform.Position + cameraComponent.CameraFront, cameraComponent.CameraUp);

            var viewProjectionMatrix = cameraComponent.ViewMatrix * cameraComponent.CreateProjectionMatrix();

            Entity[] rendererEntities = queryRenderersEntity.Build();

            foreach (var entity in rendererEntities)
            {
                ref var transform = ref this.GetComponent<TransformComponent>(entity);
                ref var meshComponent = ref this.GetComponent<MeshComponent>(entity);
                ref var shaderComponent = ref this.GetComponent<ShaderComponent>(entity);

                TestMaterialMaterial shader = (TestMaterialMaterial)shaderComponent.Shader;

                shader.MODEL = transform.GetModelMatrix().ToSilk();
                shader.VIEW = cameraComponent.ViewMatrix.ToSilk();
                shader.PROJ = cameraComponent.CreateProjectionMatrix().ToSilk();

                shader.col = new Vector3D<float>(1.0f, 1.0f, 1.0f);
                shader.kek = new CameraData_TestMaterial()
                {
                    view = Matrix4X4<float>.Identity,
                    projection = Matrix4X4<float>.Identity,
                    cameraPos = Vector3D<float>.Zero,
                    padding = 1.0f
                };

                meshComponent.Mesh.Draw(shaderComponent.Shader);
            }
        }

        public void Resize(Vector2 size) { }

        public void Initialize() { }
    }
    public class CameraMoveSystem : ISystem
    {
        private IWorld _world;
        public IWorld World => _world;
        private QueryEntity queryEntity;
        private App app;
        private float speed = 5f;
        public CameraMoveSystem(IWorld world, App app)
        {
            this.app = app;
            _world = world;
            queryEntity = this.CreateEntityQuery()
                .With<TransformComponent>()
                .With<CameraComponent>();

            app.Input.Mice[0].MouseMove += (mouse, point) =>
            {
                position = point;
            };
        }

        private Vector2 LastMousePosition = Vector2.Zero;
        private Vector2 position = Vector2.Zero;
        private Vector3 CameraDirection = Vector3.Zero;
        private Vector3 CameraFront = Vector3.Zero;
        private float CameraYaw = -90.0f;
        private float CameraPitch = 0.0f;
        private Vector3 Direction = Vector3.Zero;
        public void Update(double deltaTime)
        {
            float moveSpeed = 1.0f;
            float deltaMove = (float)deltaTime * moveSpeed;
            Entity[] entities = queryEntity.Build();
            foreach (var entity in entities)
            {
                var lookSensitivity = 0.1f;
                if (LastMousePosition == default)
                {
                    LastMousePosition = position;
                }
                else
                {
                    var xOffset = (position.X - LastMousePosition.X) * lookSensitivity;
                    var yOffset = (position.Y - LastMousePosition.Y) * lookSensitivity;
                    LastMousePosition = position;

                    CameraYaw += xOffset;
                    CameraPitch -= yOffset;

                    CameraPitch = Math.Clamp(CameraPitch, -89.0f, 89.0f);

                    CameraDirection.X = MathF.Cos(AtomMath.DegreesToRadians(CameraYaw)) * MathF.Cos(AtomMath.DegreesToRadians(CameraPitch));
                    CameraDirection.Y = MathF.Sin(AtomMath.DegreesToRadians(CameraPitch));
                    CameraDirection.Z = MathF.Sin(AtomMath.DegreesToRadians(CameraYaw)) * MathF.Cos(AtomMath.DegreesToRadians(CameraPitch));
                    CameraFront = Vector3.Normalize(CameraDirection);
                    ref var transform = ref this.GetComponent<TransformComponent>(entity);
                    ref var cameraComponent = ref this.GetComponent<CameraComponent>(entity);

                    if (app.Input.Keyboards[0].IsKeyPressed(Key.W)) Direction += new Vector3(0, 0, -1);
                    if (app.Input.Keyboards[0].IsKeyPressed(Key.S)) Direction += new Vector3(0, 0, 1);
                    if (app.Input.Keyboards[0].IsKeyPressed(Key.A)) Direction += new Vector3(-1, 0, 0);
                    if (app.Input.Keyboards[0].IsKeyPressed(Key.D)) Direction += new Vector3(1, 0, 0);
                    transform.Rotation = new Vector3(CameraPitch, CameraYaw, 0);
                    transform.Position += Direction * deltaMove * speed;
                    cameraComponent.CameraFront = CameraFront;
                    Direction = Vector3.Zero;
                }
            }
        }
        public void Initialize() { }
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

public static class ProjectResources2
{
    public static string CONE_OBJ = "Geometry.Standart.cone.obj";
}