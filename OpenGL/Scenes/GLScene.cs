using AtomEngine;
using AtomEngine.Diagnostic;
using AtomEngine.Geometry;
using AtomEngine.Input;
using AtomEngine.Math;
using AtomEngine.Scenes;
using AtomEngine.Services;

namespace Client
{
    public class GLScene : Scene
    {
        public GLScene(DIContainer DIContainer, ILogger logger = null) : base(DIContainer, logger) { }

        protected override void PrepareScene()
        {
            AtomObject cameraOb = Instantiate<AtomObject>();
            CameraComponent camera = cameraOb.AddComponent<CameraComponent>();
            camera.Initialize(new Vector2D<int>(1024, 768));

            AtomObject go = Instantiate<AtomObject>();

            MeshFilterComponent filter = go.AddComponent<MeshFilterComponent>();
            MeshGLRendererComponent renderer = go.AddComponent<MeshGLRendererComponent>();
            filter.Mesh = new Mesh();

            var vert0 = new Vertice(positions: new Vector3D(-0.5f, -0.5f, 0.0f), texCoord: new Vector2D(0.0f, 0.0f), index: 0);
            var vert1 = new Vertice(positions: new Vector3D(0.5f, -0.5f, 0.0f), texCoord: new Vector2D(1.0f, 0.0f), index: 1);
            var vert2 = new Vertice(positions: new Vector3D(0.5f, 0.5f, 0.0f), texCoord: new Vector2D(1.0f, 1.0f), index: 2);
            var vert3 = new Vertice(positions: new Vector3D(-0.5f, 0.5f, 0.0f), texCoord: new Vector2D(0.0f, 1.0f), index: 3);

            Triangle triangle0 = new Triangle(vert0, vert2, vert1);
            Triangle triangle1 = new Triangle(vert0, vert3, vert2);

            filter.Mesh.SetTriangles(new[] { triangle0, triangle1 });

            _logger?.LogInformation($"GO transform: {go.Transform.AbsolutePositon}");

            var result = OnSerialize();
        }

        protected override void OnLoadHandler() { } 
        protected override void OnRenderHandler(double value) { } 
        protected override void OnResizeHandler(Vector2D<int> value) { } 
        protected override void OnUnloadHandler() { }

        protected override void OnUpdateHandler(double value)
        {
            if (InputManager.IsKeyPressed(key: OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space))
            {
                _logger?.LogInformation("Space key pressed from scene one");
            }
        }
    }
}
