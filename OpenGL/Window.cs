using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using AtomEngine.Math;
using AtomEngine;
using AtomEngine.Utilits;
using Color4 = OpenTK.Mathematics.Color4;

namespace OpenGLCore
{
    internal class Window : GameWindow
    {
        private Vector2D<int> Resolution { get; set; }
        private Time.TimeDisposer Time { get; } = new Time.TimeDisposer();
        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
            Resolution = new Vector2D<int>(nativeWindowSettings.ClientSize.X, nativeWindowSettings.ClientSize.Y);
        }

        //Render Pipline 
        float[] vertices = new float[]
        {
            0f, 0.5f, 0f,   //top
            -0.5f, -0.5f, 0f, //bottom left
            0.5f, -0.5f, 0f // bottom right
        };

        int vao;
        int shaderProgram;


        protected override void OnLoad()
        {
            Time.TimeDisposer time = new Time.TimeDisposer();

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.Multisample);
            GL.Enable(EnableCap.Blend);

            GL.CullFace(CullFaceMode.Back);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.ClearColor(Color4.Black);

            vao = GL.GenVertexArray();
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            GL.BindVertexArray(vao);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexArrayAttrib(vao, 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);  //unbinding the buffer
            GL.BindVertexArray(0); //unbinding the vertex array

            shaderProgram = GL.CreateProgram();

            string vertSha = ShaderFinder.Find("Default", "vert");
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertSha);
            GL.CompileShader(vertexShader);

            string fragSha = ShaderFinder.Find("Default", "frag");
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragSha);
            GL.CompileShader(fragmentShader);

            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);

            GL.LinkProgram(shaderProgram);

            //cleaning up
            GL.DeleteProgram(vertexShader);
            GL.DeleteProgram(fragmentShader);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            GL.Viewport(0, 0, e.Width, e.Height);
            Resolution = new Vector2D<int>(e.Width, e.Height);

            App.Instance.SceneDisposer.ResizeCurrentScene(new Vector2D<int>(e.Width, e.Height));
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            Time.Update(e.Time);
            App.Instance.SceneDisposer.UpdateCurrentScene(e.Time);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.ClearColor(Color4.DarkKhaki);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(shaderProgram);
            GL.BindVertexArray(vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);


            Context.SwapBuffers();

            App.Instance.SceneDisposer.RenderCurrentScene(e.Time);
        }


        protected override void OnUnload()
        {
            App.Instance.SceneDisposer.UnloadCurrentScene();

            //GL.DeleteBuffer(vao);
            GL.DeleteVertexArray(vao);
            GL.DeleteProgram(shaderProgram);
        }

        public override void Dispose()
        {
            OnUnload();
            Time.Dispose();
            base.Dispose();
        }
    }
}
