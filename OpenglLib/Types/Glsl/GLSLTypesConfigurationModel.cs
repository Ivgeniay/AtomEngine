using System;
using System.Text;

namespace OpenglLib
{
    internal partial class GLSLTypesConfigurationModel : Dictionary<string, GlslTypeModel>
    {
        // Скалярные типы
        public GlslTypeModel Float => base["float"];
        public GlslTypeModel Int => base["int"];
        public GlslTypeModel Bool => base["bool"];
        public GlslTypeModel Double => base["double"];

        // Векторные типы (float)
        public GlslTypeModel Vec2 => base["vec2"];
        public GlslTypeModel Vec3 => base["vec3"];
        public GlslTypeModel Vec4 => base["vec4"];

        // Векторные типы (int)
        public GlslTypeModel IVec2 => base["ivec2"];
        public GlslTypeModel IVec3 => base["ivec3"];
        public GlslTypeModel IVec4 => base["ivec4"];

        // Векторные типы (bool)
        public GlslTypeModel BVec2 => base["bvec2"];
        public GlslTypeModel BVec3 => base["bvec3"];
        public GlslTypeModel BVec4 => base["bvec4"];

        // Векторные типы (double)
        public GlslTypeModel DVec2 => base["dvec2"];
        public GlslTypeModel DVec3 => base["dvec3"];
        public GlslTypeModel DVec4 => base["dvec4"];

        // Векторные типы (unsigned int)
        public GlslTypeModel UVec2 => base["uvec2"];
        public GlslTypeModel UVec3 => base["uvec3"];
        public GlslTypeModel UVec4 => base["uvec4"];

        // Матричные типы (float)
        public GlslTypeModel Mat2 => base["mat2"];
        public GlslTypeModel Mat3 => base["mat3"];
        public GlslTypeModel Mat4 => base["mat4"];
        public GlslTypeModel Mat2x3 => base["mat2x3"];
        public GlslTypeModel Mat2x4 => base["mat2x4"];
        public GlslTypeModel Mat3x2 => base["mat3x2"];
        public GlslTypeModel Mat3x4 => base["mat3x4"];
        public GlslTypeModel Mat4x2 => base["mat4x2"];
        public GlslTypeModel Mat4x3 => base["mat4x3"];

        // Матричные типы (double)
        public GlslTypeModel DMat2 => base["dmat2"];
        public GlslTypeModel DMat3 => base["dmat3"];
        public GlslTypeModel DMat4 => base["dmat4"];

        // Сэмплеры
        public GlslTypeModel Sampler1D => base["sampler1D"];
        public GlslTypeModel Sampler2D => base["sampler2D"];
        public GlslTypeModel Sampler3D => base["sampler3D"];
        public GlslTypeModel SamplerCube => base["samplerCube"];
        public GlslTypeModel Sampler1DShadow => base["sampler1DShadow"];
        public GlslTypeModel Sampler2DShadow => base["sampler2DShadow"];
        public GlslTypeModel SamplerCubeShadow => base["samplerCubeShadow"];
    }

}



