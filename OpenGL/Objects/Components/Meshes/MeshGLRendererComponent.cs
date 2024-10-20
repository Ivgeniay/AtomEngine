using OpenTK.Graphics.OpenGL4;
using System.Drawing.Imaging;
using System.Text.Json.Nodes; 
using AtomEngine.Utilits;
using System.Drawing;
using OpenTK.Mathematics;

namespace AtomEngine
{
    public sealed class MeshGLRendererComponent : MeshRendererComponent
    { 
        float[] vertices;

        private int textureId;
        private int shaderProgram;
        private int _vertexArrayObject;
        private int _vertexBufferObject;
        private int _shaderProgram;
        private int _texture;

        int vertexShader;
        int fragmentShader; 

        public override void Awake()
        {
            meshFilter = AtomObject.GetComponent<MeshFilterComponent>();
        } 

        public override void OnEnable()
        {
            if (meshFilter == null) return;
            vertices = meshFilter?.Mesh?.ToVerticeInfoFloat();

            CreateVAO();
            CreateVBO();
            SetUpVertexAttributes();
            CreateAndCompileShaders();
            LoadAndCompileTexture();
        }

        /// <summary>
        /// /// -------------- Создание Vertex Array Object (VAO) -------------------
        /// VAO хранит всю информацию о конфигурации вершинных атрибутов. Это позволяет 
        /// нам переключаться между различными конфигурациями вершин, просто привязывая разные VAO.
        /// </summary>
        private void CreateVAO()
        {
            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);
        }

        /// -------------- Создание Vertex Buffer Object (VBO) -------------------
        /// VBO хранит фактические данные вершин в памяти GPU. Мы создаем буфер и загружаем в него наши вершины.
        private void CreateVBO()
        {
            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
        }

        /// -------------- Настройка вершинных атрибутов -------------------
        /// Здесь мы указываем OpenGL, как интерпретировать данные вершин. Первый вызов настраивает атрибут позиции, 
        /// второй - атрибут текстурных координат.
        private void SetUpVertexAttributes()
        {
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
        }

        /// -------------- Создание и компиляция шейдеров -------------------
        /// Шейдеры - это программы, выполняемые на GPU. Вершинный шейдер обрабатывает каждую вершину, а фрагментный 
        /// шейдер определяет цвет каждого пикселя.
        private void CreateAndCompileShaders()
        {
            string vertexShaderSource = ShaderFinder.Find("Default", "vert");
            string fragmentShaderSource = ShaderFinder.Find("Default", "frag");

            //string vertexShaderSource = @"
            //#version 330 core
            //layout (location = 0) in vec3 aPosition;
            //layout (location = 1) in vec2 aTexCoord;
            //out vec2 texCoord;
            //void main()
            //{
            //    gl_Position = vec4(aPosition, 1.0);
            //    texCoord = aTexCoord;
            //}";

            //string fragmentShaderSource = @"
            //#version 330 core
            //in vec2 texCoord;
            //out vec4 FragColor;
            //uniform sampler2D texture0;
            //void main()
            //{
            //    FragColor = texture(texture0, texCoord);
            //}";

            vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);

            fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);

            _shaderProgram = GL.CreateProgram();
            GL.AttachShader(_shaderProgram, vertexShader);
            GL.AttachShader(_shaderProgram, fragmentShader);
            GL.LinkProgram(_shaderProgram);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        private void LoadAndCompileTexture()
        {
            string path = FileFinder.FindFile("Bricks_29_JE5_BE3", "jpg");
            _texture = LoadTexture(path);
        }

        public override void OnUnload()
        { 
            GL.DeleteVertexArray(_vertexArrayObject);
            GL.DeleteBuffer(_vertexBufferObject);
            GL.DeleteProgram(_shaderProgram);
            GL.DeleteTexture(_texture);
        }

        public override void Render()
        {
            if (meshFilter == null) return;

            GL.UseProgram(_shaderProgram);
            GL.BindVertexArray(_vertexArrayObject);
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);

            Matrix4 model = Matrix4.Identity;
            Matrix4 view = Matrix4.Identity;

            var rad = AtomEngine.Math.MathF.DegToRad(60f);
            //float res = (float)Resolution.X / (float)Resolution.Y;
            float res = (float)CameraComponent.Main.Resolution.X / (float)CameraComponent.Main.Resolution.Y;
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(rad, res, 0.1f, 100f);

            int modelLocation = GL.GetUniformLocation(_shaderProgram, "model");
            int viewLocation = GL.GetUniformLocation(_shaderProgram, "view");
            int projectionLocation = GL.GetUniformLocation(_shaderProgram, "projection");

            GL.UniformMatrix4(modelLocation, true, ref model);
            GL.UniformMatrix4(viewLocation, true, ref view);
            GL.UniformMatrix4(projectionLocation, true, ref projection);
        }

        public override JsonObject OnSerialize()
        { 
            return new JsonObject();
        }

        public override void OnDeserialize(JsonObject json) { }

        private int LoadTexture(string path)
        {
            int textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            using (Bitmap image = new Bitmap(path))
            {
                var data = image.LockBits(
                    new Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra,
                          PixelType.UnsignedByte, data.Scan0);

                image.UnlockBits(data);
            }

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            return textureId;
        }
    }
}
