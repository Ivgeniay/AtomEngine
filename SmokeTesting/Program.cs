using AtomEngine.RenderEntity;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;
using AtomEngine;
using OpenglLib;
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

            TestComponent testComponent = new TestComponent();

            app.Run();
        }

        private static void OnLoad(App app)
        {
            Game(app);
        }
        private static void Game(App app)
        {
            World world = new World();
            var camera = world.CreateEntity();
            var cameraTransform = new TransformComponent(camera);
            cameraTransform.Position = new Vector3(0, -25, 30);
            world.AddComponent(camera, cameraTransform);
            world.AddComponent(camera, new CameraComponent(camera));

            TestMaterialMaterial shader = new TestMaterialMaterial(app.Gl);
            
            Texture texture = new Texture(app.Gl, PathStorage.WOOD_JPG);
            Texture texture2 = new Texture(app.Gl, PathStorage.ICON_LIGHT_BULB_PNG);
            shader.tex_SetTexture(texture);

            PhysicsSystem physicsSystem = new PhysicsSystem(world);

            for (int i = 0; i < 2; i++)
            {
                Entity cubeEntity = world.CreateEntity();

                Vector3 position = new Vector3(0, 0 + 5 * i, 0);
                var t = new TransformComponent(cubeEntity);
                t.Position = position;

                Result<Model, Error> _mb_model1 = ModelLoader.LoadModel(PathStorage.CUBE_OBJ, app.Gl, app.Assimp);
                var _model1 = _mb_model1.Unwrap();
                var _mesh1 = _model1.Meshes[0];
                var _cubeMeshComponent1 = new MeshComponent(cubeEntity, _mesh1);
                var boudingComponent = new BoundingComponent(cubeEntity).FromBox(_mesh1);
                physicsSystem.CreateDynamicBox(ref t, new Vector3(2, 2, 2), 1);
                BoudingMovedComponent boudingMovedComponent = new BoudingMovedComponent(cubeEntity);
                PhysicsMaterialComponent _physicsMaterialComponent = new PhysicsMaterialComponent(cubeEntity, PhysicsMaterial.Metal);

                world.AddComponent(cubeEntity, t);
                world.AddComponent(cubeEntity, _cubeMeshComponent1);
                world.AddComponent(cubeEntity, boudingComponent);
                world.AddComponent(cubeEntity, boudingMovedComponent);
                world.AddComponent(cubeEntity, new ShaderComponent(cubeEntity, shader));
                world.AddComponent(cubeEntity, new CollisionComponent(cubeEntity));
                world.AddComponent(cubeEntity, _physicsMaterialComponent);
            }

            Vector3 pos = new Vector3(0, -30, 0);
            Entity platformE = world.CreateEntity();
            TransformComponent platformTransform = new TransformComponent(platformE);
            platformTransform.Position = pos;
            platformTransform.Scale = new Vector3(10, 1, 10);
            platformTransform.Rotation = new Vector3(5, 0, 0);
            var _mb_model = ModelLoader.LoadModel(PathStorage.CUBE_OBJ, app.Gl, app.Assimp);
            var platformModel = _mb_model.Unwrap();
            var pltfarmMesh = platformModel.Meshes[0];
            var boudingComponentCube = new BoundingComponent(platformE).FromBox(pltfarmMesh);
            var platformMeshComponent = new MeshComponent(platformE, pltfarmMesh);
            world.AddComponent(platformE, platformMeshComponent);
            world.AddComponent(platformE, platformTransform);
            world.AddComponent(platformE, boudingComponentCube);
            world.AddComponent(platformE, new StaticComponent(platformE));
            world.AddComponent(platformE, new ShaderComponent(platformE, shader));
            physicsSystem.CreateStaticBox(ref platformTransform, new Vector3(20, 2, 20));

            BoundingShaderMaterial boundingShader = new BoundingShaderMaterial(app.Gl);
            Mesh mesh1 = Mesh.CreateWireframeMesh(app.Gl, boudingComponentCube.GetVertices(), boudingComponentCube.GetIndices());
            world.AddComponent(platformE, new BoundingRenderComponent(platformE, boundingShader, mesh1));
            world.AddComponent(platformE, new CollisionComponent(platformE));
            PhysicsMaterialComponent platformPhysicsMaterialComponent = new PhysicsMaterialComponent(platformE, PhysicsMaterial.Rubber);
            world.AddComponent(platformE, platformPhysicsMaterialComponent);


            world.AddSystem(new RenderSystem(world));
            world.AddSystem(new RotateSystem(world));
            world.AddSystem(new CameraMoveSystem(world, app));
            world.AddSystem(new BoundingInputSystem(world, app));
            world.AddSystem(new BoundingMoveSystem(world));
            world.AddSystem(new BoundingRenderSystem(world));
            world.AddSystem(physicsSystem);
            world.AddSystem(new CollisionSystem(world));

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
            app.OnFixedUpdate += () => { world.FixedUpdate(); };
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
    public struct BoundingRenderComponent : IComponent
    {
        private Entity _owner;
        public Entity Owner => _owner;
        public BoundingRenderComponent(Entity entity, ShaderBase shader, Mesh mesh)
        {
            _owner = entity;
            Shader = shader;
            Mesh = mesh;
        }

        public Vector3 Color { get; set; }
        public bool IsRender { get; set; } = true;
        public ShaderBase Shader { get; set; }
        public Mesh Mesh { get; set; }
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
                //var currentCol = world.GetCurrentCollisions();
                //foreach (var con in currentCol)
                //{
                //    DebLogger.Debug(con);
                //}

                //var potCollisions = world.GetPotentialCollisions();
                //foreach (var collision in potCollisions)
                //{
                //    ref var boundingA = ref world.GetComponent<BoundingComponent>(collision.Item1);
                //    ref var boundingB = ref world.GetComponent<BoundingComponent>(collision.Item2);
                //    ref var transformA = ref world.GetComponent<TransformComponent>(collision.Item1);
                //    ref var transformB = ref world.GetComponent<TransformComponent>(collision.Item2);

                //    var worldBoundsA = boundingA.BoundingVolume.Transform(transformA.GetModelMatrix());
                //    var worldBoundsB = boundingB.BoundingVolume.Transform(transformB.GetModelMatrix());

                //    DebLogger.Debug($"Entity {collision.Item1} bounds: Min={worldBoundsA.Min}, Max={worldBoundsA.Max}");
                //    DebLogger.Debug($"Entity {collision.Item2} bounds: Min={worldBoundsB.Min}, Max={worldBoundsB.Max}");
                //    DebLogger.Debug($"Real intersection: {worldBoundsA.Intersects(worldBoundsB)}");
                //}
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
            }
        }

        public void Initialize() { }
    }
    public class CollisionSystem : ISystem
    {
        public IWorld World => _world;
        private readonly World _world;
        private QueryEntity queryEntity;

        public CollisionSystem(World world)
        {
            _world = world;
            queryEntity = this.CreateEntityQuery()
                .With<CollisionComponent>();
        }
        public void Initialize() {}


        public void Update(double deltaTime)
        {
            var entities = queryEntity.Build();
            foreach (var entity in entities)
            {
                ref var collisionComponent = ref _world.GetComponent<CollisionComponent>(entity);

                foreach (var collision in collisionComponent.Collisions)
                {
                    //DebLogger.Debug($"Collision detected between {entity} and {collision.OtherEntity} at {collision.ContactPoint} with normal {collision.Normal} and impulse {collision.Depth}");
                }

                collisionComponent.Collisions.Clear();
            }
        }
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
                shader.Use();

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
        private QueryEntity queryEntity;
        private App app;
        private float moveSpeed = 5.0f;
        private float mouseSensitivity = 0.1f;

        // Состояние мыши
        private Vector2 lastMousePosition;
        private Vector2 currentMousePosition;
        private bool isFirstMouseMove = true;

        // Состояние камеры
        private float yaw = -90.0f;
        private float pitch = 0.0f;
        private Vector3 cameraFront = new Vector3(0, 0, -1);
        private Vector3 cameraRight = Vector3.Zero;
        private Vector3 cameraUp = Vector3.UnitY;
        private float run = 3f;

        public IWorld World => _world;

        public CameraMoveSystem(IWorld world, App app)
        {
            _world = world;
            this.app = app;

            queryEntity = this.CreateEntityQuery()
                .With<TransformComponent>()
                .With<CameraComponent>();

            app.Input.Mice[0].MouseMove += (mouse, point) =>
            {
                currentMousePosition = point;
            };
        }

        public void Update(double deltaTime)
        {
            Entity[] entities = queryEntity.Build();
            foreach (var entity in entities)
            {
                ref var transform = ref this.GetComponent<TransformComponent>(entity);
                ref var camera = ref this.GetComponent<CameraComponent>(entity);

                UpdateCameraRotation();
                UpdateCameraPosition(deltaTime, ref transform);
                UpdateCameraVectors(ref camera);
            }
        }

        private void UpdateCameraRotation()
        {
            if (isFirstMouseMove)
            {
                lastMousePosition = currentMousePosition;
                isFirstMouseMove = false;
                return;
            }

            float deltaX = currentMousePosition.X - lastMousePosition.X;
            float deltaY = lastMousePosition.Y - currentMousePosition.Y;

            deltaX *= mouseSensitivity;
            deltaY *= mouseSensitivity;

            yaw += deltaX;
            pitch += deltaY;

            pitch = Math.Clamp(pitch, -89.0f, 89.0f);
            lastMousePosition = currentMousePosition;
        }

        private void UpdateCameraPosition(double deltaTime, ref TransformComponent transform)
        {
            float velocity = moveSpeed * (float)deltaTime;
            Vector3 movement = Vector3.Zero;

            if (app.Input.Keyboards[0].IsKeyPressed(Key.ShiftLeft))
                velocity *= run;
            if (app.Input.Keyboards[0].IsKeyPressed(Key.W))
                movement += cameraFront ;
            if (app.Input.Keyboards[0].IsKeyPressed(Key.S))
                movement -= cameraFront;
            if (app.Input.Keyboards[0].IsKeyPressed(Key.A))
                movement -= cameraRight;
            if (app.Input.Keyboards[0].IsKeyPressed(Key.D))
                movement += cameraRight;

            if (movement != Vector3.Zero)
            {
                movement = Vector3.Normalize(movement);
                transform.Position += movement * velocity;
            }
        }

        private void UpdateCameraVectors(ref CameraComponent camera)
        {
            Vector3 direction;
            direction.X = MathF.Cos(AtomMath.DegreesToRadians(yaw)) * MathF.Cos(AtomMath.DegreesToRadians(pitch));
            direction.Y = MathF.Sin(AtomMath.DegreesToRadians(pitch));
            direction.Z = MathF.Sin(AtomMath.DegreesToRadians(yaw)) * MathF.Cos(AtomMath.DegreesToRadians(pitch));

            cameraFront = Vector3.Normalize(direction);
            cameraRight = Vector3.Normalize(Vector3.Cross(cameraFront, Vector3.UnitY));
            cameraUp = Vector3.Cross(cameraRight, cameraFront);

            camera.CameraFront = cameraFront;
        }

        public void Initialize() { }
    }
    public class BoundingRenderSystem : IRenderSystem
    {
        private IWorld _world;
        public IWorld World => _world;
        private QueryEntity boundingRenderQuery;
        private QueryEntity queryCameraEntity;
        public BoundingRenderSystem(IWorld world)
        {
            _world = world;
            queryCameraEntity = this.CreateEntityQuery()
                .With<TransformComponent>()
                .With<CameraComponent>();

            boundingRenderQuery = this.CreateEntityQuery()
                .With<TransformComponent>()
                .With<BoundingComponent>()
                .With<BoundingRenderComponent>();
        }

        public void Initialize() { }

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
            cameraComponent.ViewMatrix = Matrix4x4.CreateLookAt(cameraTransform.Position, cameraTransform.Position + cameraComponent.CameraFront, cameraComponent.CameraUp);

            var viewProjectionMatrix = cameraComponent.ViewMatrix * cameraComponent.CreateProjectionMatrix();

            Entity[] entities = boundingRenderQuery.Build();
            foreach (var entity in entities)
            {
                ref var transform = ref this.GetComponent<TransformComponent>(entity);
                ref var bounding = ref this.GetComponent<BoundingComponent>(entity);
                ref var render = ref this.GetComponent<BoundingRenderComponent>(entity);
                if (render.IsRender)
                {
                    var s = (BoundingShaderMaterial)render.Shader;
                    s.Use();

                    s.MODEL = transform.GetModelMatrix().ToSilk();
                    s.VIEW = cameraComponent.ViewMatrix.ToSilk();
                    s.PROJ = cameraComponent.CreateProjectionMatrix().ToSilk();

                    s.col = Vector3D<float>.UnitY;

                    render.Mesh.DrawAs(s, Silk.NET.OpenGL.PrimitiveType.LineLoop);
                }
            }
        }

        public void Resize(Vector2 size)
        { }
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
            Console.Write($"{logLevel} ({DateTime.Now}):");
            Console.ForegroundColor = enterColor;
            Console.Write($"{message}\n");
        }
    }
}