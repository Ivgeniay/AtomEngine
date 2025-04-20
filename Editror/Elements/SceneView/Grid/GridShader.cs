using System.Numerics;
using Silk.NET.OpenGL;
using System;

namespace Editor
{
    public class GridShader : OpenglLib.Mat
    {
        private readonly string _vertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec3 aColor;
        
        out vec3 outColor;
        
        uniform mat4 model;
        uniform mat4 view;
        uniform mat4 projection;
        
        void main()
        {
            gl_Position = projection * view * model * vec4(aPosition, 1.0);
            outColor = aColor;
        }
        ";
        private readonly string _fragmentShaderSource = @"
        #version 330 core
        in vec3 outColor;
        out vec4 FragColor;
        
        void main()
        {
            FragColor = vec4(outColor, 1.0);
        }
        ";

        private uint _vao;
        private uint _vbo;
        private uint _ebo;
        private int _indexCount;

        private int _modelLocation;
        private int _viewLocation;
        private int _projectionLocation;

        private bool isCreated = false;
        private bool isDisposed = false;

        public GridShader(GL gl) : base(gl)
        {
            CompileShaders();

            _modelLocation = gl.GetUniformLocation(handle, "model");
            _viewLocation = gl.GetUniformLocation(handle, "view");
            _projectionLocation = gl.GetUniformLocation(handle, "projection");

            CreateGrid(gl, 20, 20, 1.0f);
        }

        private void CompileShaders()
        {
            uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
            _gl.ShaderSource(vertexShader, _vertexShaderSource);
            _gl.CompileShader(vertexShader);
            CheckShaderCompilation(vertexShader, "вершинного");

            uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
            _gl.ShaderSource(fragmentShader, _fragmentShaderSource);
            _gl.CompileShader(fragmentShader);
            CheckShaderCompilation(fragmentShader, "фрагментного");

            handle = _gl.CreateProgram();
            _gl.AttachShader(handle, vertexShader);
            _gl.AttachShader(handle, fragmentShader);
            _gl.LinkProgram(handle);

            _gl.GetProgram(handle, ProgramPropertyARB.LinkStatus, out var status);
            if (status == 0)
            {
                string infoLog = _gl.GetProgramInfoLog(handle);
                throw new Exception($"Ошибка линковки шейдерной программы: {infoLog}");
            }

            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
        }

        private void CheckShaderCompilation(uint shader, string shaderType)
        {
            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var status);
            if (status == 0)
            {
                string infoLog = _gl.GetShaderInfoLog(shader);
                throw new Exception($"Ошибка компиляции {shaderType} шейдера: {infoLog}");
            }
        }

        private unsafe void CreateGrid(GL gl, int width, int height, float cellSize)
        {
            int numLinesX = width + 1;
            int numLinesZ = height + 1;
            int numVertices = (numLinesX + numLinesZ) * 2;

            float[] vertices = new float[numVertices * 6];

            int vertexIndex = 0;
            float halfWidth = width * cellSize / 2.0f;
            float halfHeight = height * cellSize / 2.0f;

            for (int i = 0; i < numLinesX; i++)
            {
                float x = i * cellSize - halfWidth;
                float r = (i == width / 2) ? 1.0f : 0.5f;
                float g = (i == width / 2) ? 0.0f : 0.5f;
                float b = (i == width / 2) ? 0.0f : 0.5f;

                vertices[vertexIndex++] = x;
                vertices[vertexIndex++] = 0;
                vertices[vertexIndex++] = -halfHeight;
                vertices[vertexIndex++] = r;
                vertices[vertexIndex++] = g;
                vertices[vertexIndex++] = b;

                vertices[vertexIndex++] = x;
                vertices[vertexIndex++] = 0;
                vertices[vertexIndex++] = halfHeight;
                vertices[vertexIndex++] = r;
                vertices[vertexIndex++] = g;
                vertices[vertexIndex++] = b;
            }

            for (int i = 0; i < numLinesZ; i++)
            {
                float z = i * cellSize - halfHeight;
                float r = (i == height / 2) ? 0.0f : 0.5f;
                float g = (i == height / 2) ? 0.0f : 0.5f;
                float b = (i == height / 2) ? 1.0f : 0.5f;

                vertices[vertexIndex++] = -halfWidth;
                vertices[vertexIndex++] = 0;
                vertices[vertexIndex++] = z;
                vertices[vertexIndex++] = r;
                vertices[vertexIndex++] = g;
                vertices[vertexIndex++] = b;

                vertices[vertexIndex++] = halfWidth;
                vertices[vertexIndex++] = 0;
                vertices[vertexIndex++] = z;
                vertices[vertexIndex++] = r;
                vertices[vertexIndex++] = g;
                vertices[vertexIndex++] = b;
            }

            float[] yAxis = new float[] {
                0, -1, 0, 0, 1, 0,
                0, 10, 0, 0, 1, 0
            };

            _indexCount = numVertices;
            ushort[] indices = new ushort[_indexCount];
            for (ushort i = 0; i < _indexCount; i++)
            {
                indices[i] = i;
            }

            gl.GenVertexArrays(1, out _vao);
            gl.GenBuffers(1, out _vbo);
            gl.GenBuffers(1, out _ebo);

            gl.BindVertexArray(_vao);

            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            unsafe
            {
                fixed (void* data = vertices)
                {
                    gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), data, BufferUsageARB.StaticDraw);
                }
            }

            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
            unsafe
            {
                fixed (void* data = indices)
                {
                    gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(ushort)), data, BufferUsageARB.StaticDraw);
                }
            }

            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)0);
            gl.EnableVertexAttribArray(0);

            gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
            gl.EnableVertexAttribArray(1);

            gl.BindVertexArray(0);

            isCreated = true;
        }

        public void SetMVP(Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection)
        {
            unsafe
            {
                _gl.UniformMatrix4(_modelLocation, 1, false, (float*)&model);
                _gl.UniformMatrix4(_viewLocation, 1, false, (float*)&view);
                _gl.UniformMatrix4(_projectionLocation, 1, false, (float*)&projection);
            }
        }

        public unsafe void Draw()
        {
            _gl.BindVertexArray(_vao);
            _gl.DrawElements(PrimitiveType.Lines, (uint)_indexCount, DrawElementsType.UnsignedShort, (void*)0);
            _gl.BindVertexArray(0);
        }

        public override void Dispose()
        {
            if (isDisposed || !isCreated) return;

            if (_gl != null)
            {
                _gl.DeleteVertexArray(_vao);
                _gl.DeleteBuffer(_vbo);
                _gl.DeleteBuffer(_ebo);
            }
            isDisposed = true;
        }
    }


    public class InfiniteGridShader : OpenglLib.Mat
    {
        private readonly string _vertexShaderSource = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
out vec3 nearPoint;
out vec3 farPoint;
uniform mat4 view;
uniform mat4 projection;
vec3 UnprojectPoint(float x, float y, float z) {
    vec4 unprojectedPoint = inverse(projection * view) * vec4(x, y, z, 1.0);
    return unprojectedPoint.xyz / unprojectedPoint.w;
}
void main() {
    vec3 p = aPosition;
    nearPoint = UnprojectPoint(p.x, p.y, 0.0);
    farPoint = UnprojectPoint(p.x, p.y, 1.0);
    gl_Position = vec4(p, 1.0);
}
";
        private readonly string _fragmentShaderSource = @$"
#version 330 core
out vec4 FragColor;
in vec3 nearPoint;
in vec3 farPoint;
uniform vec3 cameraPosition;
uniform float gridSize;
uniform float fadeDistance;
uniform mat4 view;
uniform mat4 projection;

vec4 grid(vec3 pos, float scale) {{
    vec2 coord = pos.xz;
    {withoutDerivative};
    float line = min(grid.x, grid.y);
    
    float alpha = 1.0 - min(line, 1.0);
    alpha = alpha > {alpha} ? 1.0 : 0.0;
    
    vec4 color = vec4({r}, {g}, {b}, alpha);
    float axisWidth = 1.0;
    
    if(abs(pos.z) < derivative.y * axisWidth) {{
        color = vec4(1.0, 0.0, 0.0, 1.0);
    }}
    
    if(abs(pos.x) < derivative.x * axisWidth) {{
        color = vec4(0.0, 0.0, 1.0, 1.0);
    }}
    
    return color;
}}

float computeDepth(vec3 pos) {{
    vec4 clip = projection * view * vec4(pos, 1.0);
    return (clip.z / clip.w + 1.0) * 0.5;
}}

void main() {{
    float t = -nearPoint.y / (farPoint.y - nearPoint.y);
    
    if (t < 0.0 || t > 1.0) discard;
    
    vec3 worldPos = nearPoint + t * (farPoint - nearPoint);
    gl_FragDepth = computeDepth(worldPos);
    
    float distance = length(worldPos - cameraPosition);
    {SmoothFade}
    vec4 gridColor = grid(worldPos, gridSize);
    gridColor.a *= fade;
    if (gridColor.a < 0.1) discard;
    
    FragColor = gridColor; 
}}
";

        private const string useDerivative = @$"
vec2 derivative = fwidth(coord);
vec2 grid = abs(fract(coord / scale - 0.5) - 0.5) /derivative * {widthMultiplier};
";
        private const string withoutDerivative = @$"
vec2 derivative = fwidth(coord);
vec2 grid = abs(fract(coord / scale - 0.5) - 0.5) / {widthMultiplier};
";
        private const string widthMultiplier = "0.03";
        private const string alpha = "0.8";
        private const string r = "0.5";
        private const string g = "0.5";
        private const string b = "0.5";

        private const string NormalFade = "float fade = max(0.0, 1.0 - distance / fadeDistance);";
        private const string SmoothFade = "float fade = max(0.0, sqrt(1.0 - distance / fadeDistance));";

        private uint _vao;
        private uint _vbo;
        private uint _ebo;
        private int _viewLocation;
        private int _projectionLocation;
        private int _cameraPositionLocation;
        private int _gridSizeLocation;
        private int _fadeDistanceLocation;

        private bool _isCreated = false;
        private bool _isDisposed = false;

        public float GridSize { get; set; } = 1.0f;
        public float FadeDistance { get; set; } = 12.0f;

        public InfiniteGridShader(GL gl) : base(gl)
        {
            SetUpShader(_vertexShaderSource, _fragmentShaderSource);

            _viewLocation = gl.GetUniformLocation(handle, "view");
            _projectionLocation = gl.GetUniformLocation(handle, "projection");
            _cameraPositionLocation = gl.GetUniformLocation(handle, "cameraPosition");
            _gridSizeLocation = gl.GetUniformLocation(handle, "gridSize");
            _fadeDistanceLocation = gl.GetUniformLocation(handle, "fadeDistance");

            CreateFullScreenQuad(gl);
        }


        private unsafe void CreateFullScreenQuad(GL gl)
        {
            float[] vertices = new float[] {

            -1.0f, -1.0f, 0.0f,  
             1.0f, -1.0f, 0.0f,  
             1.0f,  1.0f, 0.0f,  
            -1.0f,  1.0f, 0.0f   
        };

            uint[] indices = new uint[] {
            0, 1, 2, 
            0, 2, 3  
        };

            gl.GenVertexArrays(1, out _vao);
            gl.GenBuffers(1, out _vbo);
            gl.GenBuffers(1, out _ebo);

            gl.BindVertexArray(_vao);

            gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            fixed (void* data = vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), data, BufferUsageARB.StaticDraw);
            }

            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
            fixed (void* data = indices)
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), data, BufferUsageARB.StaticDraw);
            }

            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
            gl.EnableVertexAttribArray(0);

            gl.BindVertexArray(0);

            _isCreated = true;
        }

        public override void Use()
        {
            _gl.UseProgram(handle);
        }

        public void SetViewProjection(Matrix4x4 view, Matrix4x4 projection, Vector3 cameraPosition)
        {
            Use();

            unsafe
            {
                _gl.UniformMatrix4(_viewLocation, 1, false, (float*)&view);
                _gl.UniformMatrix4(_projectionLocation, 1, false, (float*)&projection);
                _gl.Uniform3(_cameraPositionLocation, cameraPosition.X, cameraPosition.Y, cameraPosition.Z);
                _gl.Uniform1(_gridSizeLocation, GridSize);
                _gl.Uniform1(_fadeDistanceLocation, FadeDistance);
            }
        }

        public unsafe void Draw()
        {
            if (!_isCreated || _isDisposed) return;

            Use();

            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _gl.BindVertexArray(_vao);
            _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);
            _gl.BindVertexArray(0);

            _gl.Disable(EnableCap.Blend);
        }

        public override void Dispose()
        {
            if (_isDisposed || !_isCreated) return;

            if (_gl != null)
            {
                _gl.DeleteVertexArray(_vao);
                _gl.DeleteBuffer(_vbo);
                _gl.DeleteBuffer(_ebo);
            }
            _isDisposed = true;

            base.Dispose();
        }
    }

}