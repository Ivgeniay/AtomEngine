using Silk.NET.OpenGL;
using Silk.NET.Maths;
using AtomEngine;

namespace OpenglLib
{
    public class SampleMaterial : Mat
    {
        protected string VertexSource = @"#version 330 core 

layout(location = 0) in vec3 vertex_position;
layout(location = 1) in vec3 vertex_color;
layout(location = 2) in vec2 vertex_texcoord;
layout(location = 3) in vec3 vertex_normal;
uniform mat4 model_position_m4;
uniform mat4 model_rotation_m4;
uniform mat4 model_scale_m4;

uniform vec3 model_position;
uniform vec3 model_rotation;
uniform vec3 model_scale;

uniform mat4 view;
uniform mat4 projection;

uniform float TIME;
uniform float delta_time;
uniform vec2 screen_resolution;

uniform bool isBool;
uniform int num1
uniform uint unum;
uniform float fnum;
uniform double dnum;
uniform ivec2 ivector2;
uniform ivec3 ivector3;
uniform ivec4 ivector4;
uniform uvec2 uvector2;
uniform uvec3 uvector3;
uniform uvec4 uvector4;
uniform vec2 vector2;
uniform vec3 vector3;
uniform vec4 vector4;
uniform dvec2 doubleVector2;
uniform dvec3 doubleVector3;
uniform dvec4 doubleVector4;
uniform mat2 matrix2;
uniform mat3 matrix3;
uniform mat4 matrix4;
uniform mat2x2 matrix2x2;
uniform mat2x3 matrix2x3;
uniform mat2x4 matrix2x4;
uniform mat3x2 matrix3x2;
uniform mat3x3 matrix3x3;
uniform mat3x4 matrix3x4;
uniform mat4x2 matrix4x2;
uniform mat4x3 matrix4x3;
uniform mat4x4 matrix4x4;
uniform dmat2 doubleMatrix2;
uniform dmat3 doubleMatrix3;
uniform dmat4 doubleMatrix4;
uniform dmat2x2 doubleMatrix2x2;
uniform dmat2x3 doubleMatrix2x3;
uniform dmat2x4 doubleMatrix2x4;
uniform dmat3x2 doubleMatrix3x2;
uniform dmat3x3 doubleMatrix3x3;
uniform dmat3x4 doubleMatrix3x4;
uniform dmat4x2 doubleMatrix4x2;
uniform dmat4x3 doubleMatrix4x3;
uniform dmat4x4 doubleMatrix4x4;

uniform DirectionalLight light;
uniform DirectionalLight light2;
uniform DirectionalLight light33;

vec3 fragmentNormal(mat4 modelMatrix, vec3 vertexNormal) {
    return mat3(modelMatrix) * vertexNormal;
}

vec3 fragmentPosition(mat4 modelMatrix, vec3 vertexPosition) {
    return (modelMatrix * vec4(vertexPosition, 1.0)).xyz;
}

out VT_OUT{
    vec2 uv;
    vec3 col;
    vec3 norm;
    vec3 frag_pos;
    vec3 frag_norm;
} vt_out;

uniform mat4 model;

void main()
{
    gl_Position = projection * view * model * vec4(vertex_position.xyz, 1.0);
    vt_out.col = vec3(1.0, 1.0, 1.0);
    vt_out.norm = vec3(1.0, 1.0, 1.0);
    vt_out.frag_pos = fragmentPosition(model, vertex_position);
    vt_out.frag_norm = fragmentNormal(model, vertex_normal);
}";
        protected string FragmentSource = @"#version 330 core

struct DirectionalLight {
    vec3 direction;
    vec3 color;
    float ambient_strength;
    float intensity;
};

struct PointLight {
    vec3 position;
    vec3 color;
    float ambient_strength;
    float intensity;
    float constant;
    float linear;
    float quadratic;
};


layout(std140) uniform LightingData {
    DirectionalLight directionalLights[4];
    PointLight pointLights[8];
    int dirLightCount;
    int pointLightCount;
};
vec3 calculateDirectionalLight(DirectionalLight light, vec3 baseColor, vec3 normal) {
    vec3 normalizedNormal = normalize(normal);
    vec3 normalizedLightDir = normalize(-light.direction);

    vec3 ambient = light.ambient_strength * baseColor;
    float diff = max(dot(normalizedNormal, normalizedLightDir), 0.0);
    vec3 diffuse = diff * baseColor * light.color * light.intensity;

    return ambient + diffuse;
}

vec3 calculatePointLight(PointLight light, vec3 baseColor, vec3 normal, vec3 fragPos) {
    vec3 normalizedNormal = normalize(normal);
    vec3 lightDir = light.position - fragPos;
    float distance = length(lightDir);
    lightDir = normalize(lightDir);

    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * distance * distance);
    attenuation = max(attenuation, 0.0001);

    vec3 ambient = light.ambient_strength * baseColor;
    float diff = max(dot(normalizedNormal, lightDir), 0.0);
    vec3 diffuse = diff * baseColor * light.color * light.intensity;

    return (ambient + diffuse) * attenuation;
}

vec3 calculateLighting(vec3 baseColor, vec3 normal, vec3 fragPos) {
    vec3 result = vec3(0.0);

    for (int i = 0; i < 4; i++) {
        if (length(directionalLights[i].color) < 0.0001 || directionalLights[i].intensity < 0.0001) {
            continue;
        }
        float isActive = float(i < dirLightCount);
        result += calculateDirectionalLight(directionalLights[i], baseColor, normal) * isActive;
    }

    for (int i = 0; i < 8; i++) {
        if (length(pointLights[i].color) < 0.0001 || pointLights[i].intensity < 0.0001) {
            continue;
        }
        float isActive = float(i < pointLightCount);
        result += calculatePointLight(pointLights[i], baseColor, normal, fragPos) * isActive;
    }

    if (length(result) < 0.0001) return baseColor;
    return result;
}





in VT_OUT{
    vec2 uv;
    vec3 col;
    vec3 norm;
    vec3 frag_pos;
    vec3 frag_norm;
} f_in;

out vec4 FragColor;

void main()
{
    //vec3 bColor = texture(SAM_TEXTURE00, f_in.uv).rgb;
    vec3 bColor = f_in.col;
    vec3 temp = calculateLighting(bColor, f_in.norm, f_in.frag_pos);
    // vec3 temp = calculateDirectionalLight(DL_NAME[0], bColor, f_in.norm);
    temp = temp * f_in.col;
    FragColor = vec4(temp, 1.0);
}";
        public SampleMaterial(GL gl) : base(gl)
        {
            DebLogger.Info("Material constructor");
            SetUpShader(VertexSource, FragmentSource);
            SetLocation();
        }

        private Matrix4X4<float> _model_position_m4;
        public Matrix4X4<float> model_position_m4
        {
            set
            {
                if (model_position_m4Location == -1) return;
                if (_model_position_m4 == value) return;
                _model_position_m4 = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12, value.M13, value.M14,
                    value.M21, value.M22, value.M23, value.M24,
                    value.M31, value.M32, value.M33, value.M34,
                    value.M41, value.M42, value.M43, value.M44
                });
                _gl.UniformMatrix4(model_position_m4Location, 1, false, span);
            }
        }
        public int model_position_m4Location { get ; protected set; } = -1;

        private Matrix4X4<float> _model_rotation_m4;
        public Matrix4X4<float> model_rotation_m4
        {
            set
            {
                if (model_rotation_m4Location == -1) return;
                if (_model_rotation_m4 == value) return;
                _model_rotation_m4 = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12, value.M13, value.M14,
                    value.M21, value.M22, value.M23, value.M24,
                    value.M31, value.M32, value.M33, value.M34,
                    value.M41, value.M42, value.M43, value.M44
                });
                _gl.UniformMatrix4(model_rotation_m4Location, 1, false, span);
            }
        }
        public int model_rotation_m4Location { get ; protected set; } = -1;

        private Matrix4X4<float> _model_scale_m4;
        public Matrix4X4<float> model_scale_m4
        {
            set
            {
                if (model_scale_m4Location == -1) return;
                if (_model_scale_m4 == value) return;
                _model_scale_m4 = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12, value.M13, value.M14,
                    value.M21, value.M22, value.M23, value.M24,
                    value.M31, value.M32, value.M33, value.M34,
                    value.M41, value.M42, value.M43, value.M44
                });
                _gl.UniformMatrix4(model_scale_m4Location, 1, false, span);
            }
        }
        public int model_scale_m4Location { get ; protected set; } = -1;

        private Vector3D<float> _model_position;
        public Vector3D<float> model_position
        {
            set
            {
                if (model_positionLocation == -1) return;
                if (_model_position == value) return;
                _model_position = value;
                _gl.Uniform3(model_positionLocation, value.X, value.Y, value.Z);
            }
        }
        public int model_positionLocation { get ; protected set; } = -1;

        private Vector3D<float> _model_rotation;
        public Vector3D<float> model_rotation
        {
            set
            {
                if (model_rotationLocation == -1) return;
                if (_model_rotation == value) return;
                _model_rotation = value;
                _gl.Uniform3(model_rotationLocation, value.X, value.Y, value.Z);
            }
        }
        public int model_rotationLocation { get ; protected set; } = -1;

        private Vector3D<float> _model_scale;
        public Vector3D<float> model_scale
        {
            set
            {
                if (model_scaleLocation == -1) return;
                if (_model_scale == value) return;
                _model_scale = value;
                _gl.Uniform3(model_scaleLocation, value.X, value.Y, value.Z);
            }
        }
        public int model_scaleLocation { get ; protected set; } = -1;

        private Matrix4X4<float> _view;
        public Matrix4X4<float> view
        {
            set
            {
                if (viewLocation == -1) return;
                if (_view == value) return;
                _view = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12, value.M13, value.M14,
                    value.M21, value.M22, value.M23, value.M24,
                    value.M31, value.M32, value.M33, value.M34,
                    value.M41, value.M42, value.M43, value.M44
                });
                _gl.UniformMatrix4(viewLocation, 1, false, span);
            }
        }
        public int viewLocation { get ; protected set; } = -1;

        private Matrix4X4<float> _projection;
        public Matrix4X4<float> projection
        {
            set
            {
                if (projectionLocation == -1) return;
                if (_projection == value) return;
                _projection = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12, value.M13, value.M14,
                    value.M21, value.M22, value.M23, value.M24,
                    value.M31, value.M32, value.M33, value.M34,
                    value.M41, value.M42, value.M43, value.M44
                });
                _gl.UniformMatrix4(projectionLocation, 1, false, span);
            }
        }
        public int projectionLocation { get ; protected set; } = -1;

        private float _TIME;
        public float TIME
        {
            set
            {
                if (TIMELocation == -1) return;
                if (_TIME == value) return;
                _TIME = value;
                _gl.Uniform1(TIMELocation, value);
            }
        }
        public int TIMELocation { get ; protected set; } = -1;

        private float _delta_time;
        public float delta_time
        {
            set
            {
                if (delta_timeLocation == -1) return;
                if (_delta_time == value) return;
                _delta_time = value;
                _gl.Uniform1(delta_timeLocation, value);
            }
        }
        public int delta_timeLocation { get ; protected set; } = -1;

        private Vector2D<float> _screen_resolution;
        public Vector2D<float> screen_resolution
        {
            set
            {
                if (screen_resolutionLocation == -1) return;
                if (_screen_resolution == value) return;
                _screen_resolution = value;
                _gl.Uniform2(screen_resolutionLocation, value.X, value.Y);
            }
        }
        public int screen_resolutionLocation { get ; protected set; } = -1;

        private bool _isBool;
        public bool isBool
        {
            set
            {
                if (isBoolLocation == -1) return;
                if (_isBool == value) return;
                _isBool = value;
                _gl.Uniform1(isBoolLocation, value ? 1 : 0);
            }
        }
        public int isBoolLocation { get ; protected set; } = -1;

        private uint _unum;
        public uint unum
        {
            set
            {
                if (unumLocation == -1) return;
                if (_unum == value) return;
                _unum = value;
                _gl.Uniform1(unumLocation, value);
            }
        }
        public int unumLocation { get ; protected set; } = -1;

        private float _fnum;
        public float fnum
        {
            set
            {
                if (fnumLocation == -1) return;
                if (_fnum == value) return;
                _fnum = value;
                _gl.Uniform1(fnumLocation, value);
            }
        }
        public int fnumLocation { get ; protected set; } = -1;

        private double _dnum;
        public double dnum
        {
            set
            {
                if (dnumLocation == -1) return;
                if (_dnum == value) return;
                _dnum = value;
                _gl.Uniform1(dnumLocation, value);
            }
        }
        public int dnumLocation { get ; protected set; } = -1;

        private Vector2D<int> _ivector2;
        public Vector2D<int> ivector2
        {
            set
            {
                if (ivector2Location == -1) return;
                if (_ivector2 == value) return;
                _ivector2 = value;
                _gl.Uniform2(ivector2Location, value.X, value.Y);
            }
        }
        public int ivector2Location { get ; protected set; } = -1;

        private Vector3D<int> _ivector3;
        public Vector3D<int> ivector3
        {
            set
            {
                if (ivector3Location == -1) return;
                if (_ivector3 == value) return;
                _ivector3 = value;
                _gl.Uniform3(ivector3Location, value.X, value.Y, value.Z);
            }
        }
        public int ivector3Location { get ; protected set; } = -1;

        private Vector4D<int> _ivector4;
        public Vector4D<int> ivector4
        {
            set
            {
                if (ivector4Location == -1) return;
                if (_ivector4 == value) return;
                _ivector4 = value;
                _gl.Uniform4(ivector4Location, value.X, value.Y, value.Z, value.W);
            }
        }
        public int ivector4Location { get ; protected set; } = -1;

        private Vector2D<uint> _uvector2;
        public Vector2D<uint> uvector2
        {
            set
            {
                if (uvector2Location == -1) return;
                if (_uvector2 == value) return;
                _uvector2 = value;
                _gl.Uniform2(uvector2Location, value.X, value.Y);
            }
        }
        public int uvector2Location { get ; protected set; } = -1;

        private Vector3D<uint> _uvector3;
        public Vector3D<uint> uvector3
        {
            set
            {
                if (uvector3Location == -1) return;
                if (_uvector3 == value) return;
                _uvector3 = value;
                _gl.Uniform3(uvector3Location, value.X, value.Y, value.Z);
            }
        }
        public int uvector3Location { get ; protected set; } = -1;

        private Vector4D<uint> _uvector4;
        public Vector4D<uint> uvector4
        {
            set
            {
                if (uvector4Location == -1) return;
                if (_uvector4 == value) return;
                _uvector4 = value;
                _gl.Uniform4(uvector4Location, value.X, value.Y, value.Z, value.W);
            }
        }
        public int uvector4Location { get ; protected set; } = -1;

        private Vector2D<float> _vector2;
        public Vector2D<float> vector2
        {
            set
            {
                if (vector2Location == -1) return;
                if (_vector2 == value) return;
                _vector2 = value;
                _gl.Uniform2(vector2Location, value.X, value.Y);
            }
        }
        public int vector2Location { get ; protected set; } = -1;

        private Vector3D<float> _vector3;
        public Vector3D<float> vector3
        {
            set
            {
                if (vector3Location == -1) return;
                if (_vector3 == value) return;
                _vector3 = value;
                _gl.Uniform3(vector3Location, value.X, value.Y, value.Z);
            }
        }
        public int vector3Location { get ; protected set; } = -1;

        private Vector4D<float> _vector4;
        public Vector4D<float> vector4
        {
            set
            {
                if (vector4Location == -1) return;
                if (_vector4 == value) return;
                _vector4 = value;
                _gl.Uniform4(vector4Location, value.X, value.Y, value.Z, value.W);
            }
        }
        public int vector4Location { get ; protected set; } = -1;

        private Vector2D<double> _doubleVector2;
        public Vector2D<double> doubleVector2
        {
            set
            {
                if (doubleVector2Location == -1) return;
                if (_doubleVector2 == value) return;
                _doubleVector2 = value;
                _gl.Uniform2(doubleVector2Location, value.X, value.Y);
            }
        }
        public int doubleVector2Location { get ; protected set; } = -1;

        private Vector3D<double> _doubleVector3;
        public Vector3D<double> doubleVector3
        {
            set
            {
                if (doubleVector3Location == -1) return;
                if (_doubleVector3 == value) return;
                _doubleVector3 = value;
                _gl.Uniform3(doubleVector3Location, value.X, value.Y, value.Z);
            }
        }
        public int doubleVector3Location { get ; protected set; } = -1;

        private Vector4D<double> _doubleVector4;
        public Vector4D<double> doubleVector4
        {
            set
            {
                if (doubleVector4Location == -1) return;
                if (_doubleVector4 == value) return;
                _doubleVector4 = value;
                _gl.Uniform4(doubleVector4Location, value.X, value.Y, value.Z, value.W);
            }
        }
        public int doubleVector4Location { get ; protected set; } = -1;

        private Matrix2X2<float> _matrix2;
        public Matrix2X2<float> matrix2
        {
            set
            {
                if (matrix2Location == -1) return;
                if (_matrix2 == value) return;
                _matrix2 = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12,
                    value.M21, value.M22
                });
                _gl.UniformMatrix2(matrix2Location, 1, false, span);
            }
        }
        public int matrix2Location { get ; protected set; } = -1;

        private Matrix3X3<float> _matrix3;
        public Matrix3X3<float> matrix3
        {
            set
            {
                if (matrix3Location == -1) return;
                if (_matrix3 == value) return;
                _matrix3 = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12, value.M13,
                    value.M21, value.M22, value.M23,
                    value.M31, value.M32, value.M33
                });
                _gl.UniformMatrix3(matrix3Location, 1, false, span);
            }
        }
        public int matrix3Location { get ; protected set; } = -1;

        private Matrix4X4<float> _matrix4;
        public Matrix4X4<float> matrix4
        {
            set
            {
                if (matrix4Location == -1) return;
                if (_matrix4 == value) return;
                _matrix4 = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12, value.M13, value.M14,
                    value.M21, value.M22, value.M23, value.M24,
                    value.M31, value.M32, value.M33, value.M34,
                    value.M41, value.M42, value.M43, value.M44
                });
                _gl.UniformMatrix4(matrix4Location, 1, false, span);
            }
        }
        public int matrix4Location { get ; protected set; } = -1;

        private Matrix2X2<float> _matrix2x2;
        public Matrix2X2<float> matrix2x2
        {
            set
            {
                if (matrix2x2Location == -1) return;
                if (_matrix2x2 == value) return;
                _matrix2x2 = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12,
                    value.M21, value.M22
                });
                _gl.UniformMatrix2(matrix2x2Location, 1, false, span);
            }
        }
        public int matrix2x2Location { get ; protected set; } = -1;

        private Matrix2X3<float> _matrix2x3;
        public Matrix2X3<float> matrix2x3
        {
            set
            {
                if (matrix2x3Location == -1) return;
                if (_matrix2x3 == value) return;
                _matrix2x3 = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12, value.M13,
                    value.M21, value.M22, value.M23
                });
                _gl.UniformMatrix2x3(matrix2x3Location, 1, false, span);
            }
        }
        public int matrix2x3Location { get ; protected set; } = -1;

        private Matrix2X4<float> _matrix2x4;
        public Matrix2X4<float> matrix2x4
        {
            set
            {
                if (matrix2x4Location == -1) return;
                if (_matrix2x4 == value) return;
                _matrix2x4 = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12, value.M13, value.M14,
                    value.M21, value.M22, value.M23, value.M24
                });
                _gl.UniformMatrix2x4(matrix2x4Location, 1, false, span);
            }
        }
        public int matrix2x4Location { get ; protected set; } = -1;

        private Matrix3X2<float> _matrix3x2;
        public Matrix3X2<float> matrix3x2
        {
            set
            {
                if (matrix3x2Location == -1) return;
                if (_matrix3x2 == value) return;
                _matrix3x2 = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12,
                    value.M21, value.M22,
                    value.M31, value.M32
                });
                _gl.UniformMatrix3x2(matrix3x2Location, 1, false, span);
            }
        }
        public int matrix3x2Location { get ; protected set; } = -1;

        private Matrix3X3<float> _matrix3x3;
        public Matrix3X3<float> matrix3x3
        {
            set
            {
                if (matrix3x3Location == -1) return;
                if (_matrix3x3 == value) return;
                _matrix3x3 = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12, value.M13,
                    value.M21, value.M22, value.M23,
                    value.M31, value.M32, value.M33
                });
                _gl.UniformMatrix3(matrix3x3Location, 1, false, span);
            }
        }
        public int matrix3x3Location { get ; protected set; } = -1;

        private Matrix3X4<float> _matrix3x4;
        public Matrix3X4<float> matrix3x4
        {
            set
            {
                if (matrix3x4Location == -1) return;
                if (_matrix3x4 == value) return;
                _matrix3x4 = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12, value.M13, value.M14,
                    value.M21, value.M22, value.M23, value.M24,
                    value.M31, value.M32, value.M33, value.M34
                });
                _gl.UniformMatrix3x4(matrix3x4Location, 1, false, span);
            }
        }
        public int matrix3x4Location { get ; protected set; } = -1;

        private Matrix4X2<float> _matrix4x2;
        public Matrix4X2<float> matrix4x2
        {
            set
            {
                if (matrix4x2Location == -1) return;
                if (_matrix4x2 == value) return;
                _matrix4x2 = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12,
                    value.M21, value.M22,
                    value.M31, value.M32,
                    value.M41, value.M42
                });
                _gl.UniformMatrix4x2(matrix4x2Location, 1, false, span);
            }
        }
        public int matrix4x2Location { get ; protected set; } = -1;

        private Matrix4X3<float> _matrix4x3;
        public Matrix4X3<float> matrix4x3
        {
            set
            {
                if (matrix4x3Location == -1) return;
                if (_matrix4x3 == value) return;
                _matrix4x3 = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12, value.M13,
                    value.M21, value.M22, value.M23,
                    value.M31, value.M32, value.M33,
                    value.M41, value.M42, value.M43
                });
                _gl.UniformMatrix4x3(matrix4x3Location, 1, false, span);
            }
        }
        public int matrix4x3Location { get ; protected set; } = -1;

        private Matrix4X4<float> _matrix4x4;
        public Matrix4X4<float> matrix4x4
        {
            set
            {
                if (matrix4x4Location == -1) return;
                if (_matrix4x4 == value) return;
                _matrix4x4 = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12, value.M13, value.M14,
                    value.M21, value.M22, value.M23, value.M24,
                    value.M31, value.M32, value.M33, value.M34,
                    value.M41, value.M42, value.M43, value.M44
                });
                _gl.UniformMatrix4(matrix4x4Location, 1, false, span);
            }
        }
        public int matrix4x4Location { get ; protected set; } = -1;

        private Matrix2X2<double> _doubleMatrix2;
        public Matrix2X2<double> doubleMatrix2
        {
            set
            {
                if (doubleMatrix2Location == -1) return;
                if (_doubleMatrix2 == value) return;
                _doubleMatrix2 = value;
                var span = new Span<double>(new[]
                {
                    value.M11, value.M12,
                    value.M21, value.M22
                });
                _gl.UniformMatrix2(doubleMatrix2Location, 1, false, span);
            }
        }
        public int doubleMatrix2Location { get ; protected set; } = -1;

        private Matrix3X3<double> _doubleMatrix3;
        public Matrix3X3<double> doubleMatrix3
        {
            set
            {
                if (doubleMatrix3Location == -1) return;
                if (_doubleMatrix3 == value) return;
                _doubleMatrix3 = value;
                var span = new Span<double>(new[]
                {
                    value.M11, value.M12, value.M13,
                    value.M21, value.M22, value.M23,
                    value.M31, value.M32, value.M33
                });
                _gl.UniformMatrix3(doubleMatrix3Location, 1, false, span);
            }
        }
        public int doubleMatrix3Location { get ; protected set; } = -1;

        private Matrix4X4<double> _doubleMatrix4;
        public Matrix4X4<double> doubleMatrix4
        {
            set
            {
                if (doubleMatrix4Location == -1) return;
                if (_doubleMatrix4 == value) return;
                _doubleMatrix4 = value;
                var span = new Span<double>(new[]
                {
                    value.M11, value.M12, value.M13, value.M14,
                    value.M21, value.M22, value.M23, value.M24,
                    value.M31, value.M32, value.M33, value.M34,
                    value.M41, value.M42, value.M43, value.M44
                });
                _gl.UniformMatrix4(doubleMatrix4Location, 1, false, span);
            }
        }
        public int doubleMatrix4Location { get ; protected set; } = -1;

        private Matrix2X2<double> _doubleMatrix2x2;
        public Matrix2X2<double> doubleMatrix2x2
        {
            set
            {
                if (doubleMatrix2x2Location == -1) return;
                if (_doubleMatrix2x2 == value) return;
                _doubleMatrix2x2 = value;
                var span = new Span<double>(new[]
                {
                    value.M11, value.M12,
                    value.M21, value.M22
                });
                _gl.UniformMatrix2(doubleMatrix2x2Location, 1, false, span);
            }
        }
        public int doubleMatrix2x2Location { get ; protected set; } = -1;

        private Matrix2X3<double> _doubleMatrix2x3;
        public Matrix2X3<double> doubleMatrix2x3
        {
            set
            {
                if (doubleMatrix2x3Location == -1) return;
                if (_doubleMatrix2x3 == value) return;
                _doubleMatrix2x3 = value;
                var span = new Span<double>(new[]
                {
                    value.M11, value.M12, value.M13,
                    value.M21, value.M22, value.M23
                });
                _gl.UniformMatrix2x3(doubleMatrix2x3Location, 1, false, span);
            }
        }
        public int doubleMatrix2x3Location { get ; protected set; } = -1;

        private Matrix2X4<double> _doubleMatrix2x4;
        public Matrix2X4<double> doubleMatrix2x4
        {
            set
            {
                if (doubleMatrix2x4Location == -1) return;
                if (_doubleMatrix2x4 == value) return;
                _doubleMatrix2x4 = value;
                var span = new Span<double>(new[]
                {
                    value.M11, value.M12, value.M13, value.M14,
                    value.M21, value.M22, value.M23, value.M24
                });
                _gl.UniformMatrix2x4(doubleMatrix2x4Location, 1, false, span);
            }
        }
        public int doubleMatrix2x4Location { get ; protected set; } = -1;

        private Matrix3X2<double> _doubleMatrix3x2;
        public Matrix3X2<double> doubleMatrix3x2
        {
            set
            {
                if (doubleMatrix3x2Location == -1) return;
                if (_doubleMatrix3x2 == value) return;
                _doubleMatrix3x2 = value;
                var span = new Span<double>(new[]
                {
                    value.M11, value.M12,
                    value.M21, value.M22,
                    value.M31, value.M32
                });
                _gl.UniformMatrix3x2(doubleMatrix3x2Location, 1, false, span);
            }
        }
        public int doubleMatrix3x2Location { get ; protected set; } = -1;

        private Matrix3X3<double> _doubleMatrix3x3;
        public Matrix3X3<double> doubleMatrix3x3
        {
            set
            {
                if (doubleMatrix3x3Location == -1) return;
                if (_doubleMatrix3x3 == value) return;
                _doubleMatrix3x3 = value;
                var span = new Span<double>(new[]
                {
                    value.M11, value.M12, value.M13,
                    value.M21, value.M22, value.M23,
                    value.M31, value.M32, value.M33
                });
                _gl.UniformMatrix3(doubleMatrix3x3Location, 1, false, span);
            }
        }
        public int doubleMatrix3x3Location { get ; protected set; } = -1;

        private Matrix3X4<double> _doubleMatrix3x4;
        public Matrix3X4<double> doubleMatrix3x4
        {
            set
            {
                if (doubleMatrix3x4Location == -1) return;
                if (_doubleMatrix3x4 == value) return;
                _doubleMatrix3x4 = value;
                var span = new Span<double>(new[]
                {
                    value.M11, value.M12, value.M13, value.M14,
                    value.M21, value.M22, value.M23, value.M24,
                    value.M31, value.M32, value.M33, value.M34
                });
                _gl.UniformMatrix3x4(doubleMatrix3x4Location, 1, false, span);
            }
        }
        public int doubleMatrix3x4Location { get ; protected set; } = -1;

        private Matrix4X2<double> _doubleMatrix4x2;
        public Matrix4X2<double> doubleMatrix4x2
        {
            set
            {
                if (doubleMatrix4x2Location == -1) return;
                if (_doubleMatrix4x2 == value) return;
                _doubleMatrix4x2 = value;
                var span = new Span<double>(new[]
                {
                    value.M11, value.M12,
                    value.M21, value.M22,
                    value.M31, value.M32,
                    value.M41, value.M42
                });
                _gl.UniformMatrix4x2(doubleMatrix4x2Location, 1, false, span);
            }
        }
        public int doubleMatrix4x2Location { get ; protected set; } = -1;

        private Matrix4X3<double> _doubleMatrix4x3;
        public Matrix4X3<double> doubleMatrix4x3
        {
            set
            {
                if (doubleMatrix4x3Location == -1) return;
                if (_doubleMatrix4x3 == value) return;
                _doubleMatrix4x3 = value;
                var span = new Span<double>(new[]
                {
                    value.M11, value.M12, value.M13,
                    value.M21, value.M22, value.M23,
                    value.M31, value.M32, value.M33,
                    value.M41, value.M42, value.M43
                });
                _gl.UniformMatrix4x3(doubleMatrix4x3Location, 1, false, span);
            }
        }
        public int doubleMatrix4x3Location { get ; protected set; } = -1;

        private Matrix4X4<double> _doubleMatrix4x4;
        public Matrix4X4<double> doubleMatrix4x4
        {
            set
            {
                if (doubleMatrix4x4Location == -1) return;
                if (_doubleMatrix4x4 == value) return;
                _doubleMatrix4x4 = value;
                var span = new Span<double>(new[]
                {
                    value.M11, value.M12, value.M13, value.M14,
                    value.M21, value.M22, value.M23, value.M24,
                    value.M31, value.M32, value.M33, value.M34,
                    value.M41, value.M42, value.M43, value.M44
                });
                _gl.UniformMatrix4(doubleMatrix4x4Location, 1, false, span);
            }
        }
        public int doubleMatrix4x4Location { get ; protected set; } = -1;

        private DirectionalLight _light = new DirectionalLight();
        public DirectionalLight light
        {
            get
            {
                return _light;
            }
        }

        private DirectionalLight _light2 = new DirectionalLight();
        public DirectionalLight light2
        {
            get
            {
                return _light2;
            }
        }

        private DirectionalLight _light33 = new DirectionalLight();
        public DirectionalLight light33
        {
            get
            {
                return _light33;
            }
        }

        private Matrix4X4<float> _model;
        public Matrix4X4<float> model
        {
            set
            {
                if (modelLocation == -1) return;
                if (_model == value) return;
                _model = value;
                var span = new Span<float>(new[]
                {
                    value.M11, value.M12, value.M13, value.M14,
                    value.M21, value.M22, value.M23, value.M24,
                    value.M31, value.M32, value.M33, value.M34,
                    value.M41, value.M42, value.M43, value.M44
                });
                _gl.UniformMatrix4(modelLocation, 1, false, span);
            }
        }
        public int modelLocation { get ; protected set; } = -1;

    }
}
