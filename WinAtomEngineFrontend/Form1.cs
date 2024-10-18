using Microsoft.AspNetCore.Components.WebView.WindowsForms; 
using OpenTK.Graphics.OpenGL4;
using System.Drawing.Imaging;
using AtomEngine.Utilits;
using AtomEngine.Math;
using AtomEngine;
using AtomEngineEditor;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;
using Editor;

namespace WinAtomEngineFrontend
{
    public partial class Form1 : Form
    {
        //private OpenTK.GLControl.GLControl glControl1;
        public Form1(IServiceProvider serviceProvider)
        { 
            InitializeComponent();

            workSpace.HostPage = "wwwroot/index.html";
            workSpace.Services = serviceProvider;
            workSpace.RootComponents.Add<App>("#app");

            //glControl1.Dock = DockStyle.Fill;
            //glControl1.Load += GLControl_Load;
            //glControl1.Paint += GLControl_Paint;
        }

        //private int textureId;
        //private int shaderProgram;
        //private int _vertexArrayObject;
        //private int _vertexBufferObject;
        //private int _shaderProgram;
        //private int _texture;

        //private void GLControl_Paint(object? sender, PaintEventArgs e)
        //{
        //    GL.ClearColor((Color)Color4.DarkKhaki);
        //    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        //    GL.UseProgram(_shaderProgram);
        //    GL.BindVertexArray(_vertexArrayObject);
        //    GL.BindTexture(TextureTarget.Texture2D, _texture);
        //    GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

        //    //glControl1?.Context?.SwapBuffers();

        //    //Core.Instance.SceneDisposer.RenderCurrentScene(e.Time);
        //}


        //private void GLControl_Load(object? sender, EventArgs e)
        //{
        //    Time.TimeDisposer time = new Time.TimeDisposer();

        //    GL.Enable(EnableCap.DepthTest);
        //    GL.Enable(EnableCap.CullFace);
        //    GL.Enable(EnableCap.Multisample);
        //    GL.Enable(EnableCap.Blend);

        //    GL.CullFace(CullFaceMode.Back);
        //    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        //    GL.ClearColor((Color)Color4.Black);

        //    //App.Instance.SceneDisposer.O .LoadCurrentScene();

        //    float[] vertices = {
        //            // Position         Texture coordinates
        //            -0.5f, -0.5f, 0.0f, 0.0f, 0.0f,
        //             0.5f, -0.5f, 0.0f, 1.0f, 0.0f,
        //             0.5f,  0.5f, 0.0f, 1.0f, 1.0f,
        //            -0.5f,  0.5f, 0.0f, 0.0f, 1.0f
        //        };

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

        //    string vertexShaderSource = @"
        //            #version 330 core
        //            layout (location = 0) in vec3 aPosition;
        //            layout (location = 1) in vec2 aTexCoord;
        //            out vec2 texCoord;
        //            void main()
        //            {
        //                gl_Position = vec4(aPosition, 1.0);
        //                texCoord = aTexCoord;
        //            }";

        //    string fragmentShaderSource = @"
        //            #version 330 core
        //            in vec2 texCoord;
        //            out vec4 FragColor;
        //            uniform sampler2D texture0;
        //            void main()
        //            {
        //                FragColor = texture(texture0, texCoord);
        //            }";

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

        //    /// -------------- Настройка цвета очистки -------------------
        //    /// Устанавливаем цвет фона, которым будет заполняться экран перед каждым кадром.
        //    /// 
        //    GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        //}

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

        private void blazorWebView1_Click(object sender, EventArgs e)
        {

        }

        private void glControl1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
