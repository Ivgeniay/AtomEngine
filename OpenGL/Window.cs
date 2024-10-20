using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;
using Color4 = OpenTK.Mathematics.Color4; 
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;
using System.Drawing.Imaging;
using AtomEngine.Math;
using System.Drawing; 
using AtomEngine;

namespace OpenGLCore
{
    internal class Window : GameWindow
    {
        public Vector2D<int> Resolution { get; private set; }
        private Time.TimeDisposer Time { get; } = new Time.TimeDisposer();
        public Window(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
        {
            Resolution = new Vector2D<int>(nativeWindowSettings.ClientSize.X, nativeWindowSettings.ClientSize.Y);
        }

        //private int textureId;
        //private int shaderProgram;
        //private int _vertexArrayObject;
        //private int _vertexBufferObject;
        //private int _shaderProgram;
        //private int _texture;
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

            //App.Instance.SceneDisposer.O .LoadCurrentScene();

        //    float[] vertices = {
        //    // Position         Texture coordinates
        //    -0.5f, -0.5f, 0.0f, 0.0f, 0.0f,
        //     0.5f, -0.5f, 0.0f, 1.0f, 0.0f,
        //     0.5f,  0.5f, 0.0f, 1.0f, 1.0f,
        //    -0.5f,  0.5f, 0.0f, 0.0f, 1.0f
        //};

        //    /// -------------- Создание Vertex Array Object (VAO) -------------------
        //    /// VAO хранит всю информацию о конфигурации вершинных атрибутов. Это позволяет 
        //    /// нам переключаться между различными конфигурациями вершин, просто привязывая разные VAO.
            
        //    _vertexArrayObject = GL.GenVertexArray();
        //    GL.BindVertexArray(_vertexArrayObject);


        //    /// -------------- Создание Vertex Buffer Object (VBO) -------------------
        //    /// VBO хранит фактические данные вершин в памяти GPU. Мы создаем буфер и загружаем в него наши вершины.
            
        //    _vertexBufferObject = GL.GenBuffer();
        //    GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        //    GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);


        //    /// -------------- Настройка вершинных атрибутов -------------------
        //    /// Здесь мы указываем OpenGL, как интерпретировать данные вершин. Первый вызов настраивает атрибут позиции, 
        //    /// второй - атрибут текстурных координат.

        //    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        //    GL.EnableVertexAttribArray(0);

        //    GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        //    GL.EnableVertexAttribArray(1);

        //    /// -------------- Создание и компиляция шейдеров -------------------
        //    /// Шейдеры - это программы, выполняемые на GPU. Вершинный шейдер обрабатывает каждую вершину, а фрагментный 
        //    /// шейдер определяет цвет каждого пикселя.

        //    string vertexShaderSource = ShaderFinder.Find("Default", "vert");
        //    string fragmentShaderSource = ShaderFinder.Find("Default", "frag");
        //    //string vertexShaderSource = @"
        //    //#version 330 core
        //    //layout (location = 0) in vec3 aPosition;
        //    //layout (location = 1) in vec2 aTexCoord;
        //    //out vec2 texCoord;
        //    //void main()
        //    //{
        //    //    gl_Position = vec4(aPosition, 1.0);
        //    //    texCoord = aTexCoord;
        //    //}";

        //    //string fragmentShaderSource = @"
        //    //#version 330 core
        //    //in vec2 texCoord;
        //    //out vec4 FragColor;
        //    //uniform sampler2D texture0;
        //    //void main()
        //    //{
        //    //    FragColor = texture(texture0, texCoord);
        //    //}";

        //    int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        //    GL.ShaderSource(vertexShader, vertexShaderSource);
        //    GL.CompileShader(vertexShader);

        //    int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        //    GL.ShaderSource(fragmentShader, fragmentShaderSource);
        //    GL.CompileShader(fragmentShader);

        //    /// -------------- Создание шейдерной программы -------------------
        //    /// Шейдерная программа объединяет вершинный и фрагментный шейдеры. После линковки отдельные 
        //    /// шейдеры можно удалить.
            
        //    _shaderProgram = GL.CreateProgram();
        //    GL.AttachShader(_shaderProgram, vertexShader);
        //    GL.AttachShader(_shaderProgram, fragmentShader);
        //    GL.LinkProgram(_shaderProgram);

        //    GL.DeleteShader(vertexShader);
        //    GL.DeleteShader(fragmentShader);

        //    /// -------------- Загрузка текстуры -------------------
        //    /// Здесь мы загружаем текстуру из файла и создаем для нее объект текстуры OpenGL.

        //    string path = FileFinder.FindFile("Bricks_29_JE5_BE3", "jpg");
        //    _texture = LoadTexture(path);

            /// -------------- Настройка цвета очистки -------------------
            /// Устанавливаем цвет фона, которым будет заполняться экран перед каждым кадром.
            /// 
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
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

            //GL.UseProgram(_shaderProgram);
            //GL.BindVertexArray(_vertexArrayObject);
            //GL.BindTexture(TextureTarget.Texture2D, _texture);
            //GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

            //Matrix4 model       = Matrix4.Identity;
            //Matrix4 view        = Matrix4.Identity;

            //var rad = AtomEngine.Math.MathF.DegToRad(60f);
            //float res = (float)Resolution.X / (float)Resolution.Y;
            //Matrix4 projection  = Matrix4.CreatePerspectiveFieldOfView(rad, res, 0.1f, 100f);

            //int modelLocation = GL.GetUniformLocation(_shaderProgram, "model");
            //int viewLocation = GL.GetUniformLocation(_shaderProgram, "view");
            //int projectionLocation = GL.GetUniformLocation(_shaderProgram, "projection");

            //GL.UniformMatrix4(modelLocation, true, ref model);
            //GL.UniformMatrix4(viewLocation, true, ref view);
            //GL.UniformMatrix4(projectionLocation, true, ref projection);

            App.Instance.SceneDisposer.RenderCurrentScene(e.Time);

            Context.SwapBuffers();
        }


        protected override void OnUnload()
        {
            //GL.DeleteVertexArray(_vertexArrayObject);
            //GL.DeleteBuffer(_vertexBufferObject);
            //GL.DeleteProgram(_shaderProgram);
            //GL.DeleteTexture(_texture);

            App.Instance.SceneDisposer.UnloadCurrentScene(); 
        }

        public override void Dispose()
        {
            OnUnload();
            Time.Dispose();
            base.Dispose();
        }

        //private int LoadTexture(string path)
        //{
        //    int textureId = GL.GenTexture();
        //    GL.BindTexture(TextureTarget.Texture2D, textureId);

        //    using (Bitmap image = new Bitmap(path))
        //    {
        //        var data = image.LockBits(
        //            new Rectangle(0, 0, image.Width, image.Height),
        //            ImageLockMode.ReadOnly,
        //            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        //        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
        //                  data.Width, data.Height, 0, PixelFormat.Bgra,
        //                  PixelType.UnsignedByte, data.Scan0);

        //        image.UnlockBits(data);
        //    }

        //    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        //    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        //    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        //    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        //    return textureId;
        //}
    }
}
