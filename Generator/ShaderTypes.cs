using System.Text;

namespace OpenglLib.Generator
{
    internal static class ShaderTypes
    {
        public static HashSet<string> GeneratedTypes = new HashSet<string>();


        public static string GetGetter(string cashFieldName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("            get");
            builder.AppendLine("            {");
            builder.AppendLine($"                return {cashFieldName};");
            builder.AppendLine("            }");
            return builder.ToString();
        }

        public static string GetSetter(string type, string locationFieldName, string cashFieldName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("            set");
            builder.AppendLine("            {");
            builder.AppendLine($"                if ({locationFieldName} == -1) return;");
            builder.AppendLine($"                if ({cashFieldName} == value) return;");
            builder.AppendLine($"                {cashFieldName} = value;");
            
            switch (type)
            {
                // Скалярные типы
                case "bool":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value ? 1 : 0);");
                    break;
                case "int":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;
                case "uint":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;
                case "float":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;
                case "double":
                    builder.AppendLine($"                _gl.Uniform1({locationFieldName}, value);");
                    break;

                // Векторные типы bool
                case "bvec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0);");
                    break;
                case "bvec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0, value.Z ? 1 : 0);");
                    break;
                case "bvec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X ? 1 : 0, value.Y ? 1 : 0, value.Z ? 1 : 0, value.W ? 1 : 0);");
                    break;

                // Векторные типы int
                case "ivec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;
                case "ivec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;
                case "ivec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                // Векторные типы uint
                case "uvec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;
                case "uvec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;
                case "uvec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                // Векторные типы float
                case "vec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;
                case "vec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;
                case "vec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                // Векторные типы double
                case "dvec2":
                    builder.AppendLine($"                _gl.Uniform2({locationFieldName}, value.X, value.Y);");
                    break;
                case "dvec3":
                    builder.AppendLine($"                _gl.Uniform3({locationFieldName}, value.X, value.Y, value.Z);");
                    break;
                case "dvec4":
                    builder.AppendLine($"                _gl.Uniform4({locationFieldName}, value.X, value.Y, value.Z, value.W);");
                    break;

                // Матричные типы float
                case "mat2":
                case "mat2x2":
                    builder.AppendLine("                var span = new Span<float>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12,");
                    builder.AppendLine("                    value.M21, value.M22");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix2({locationFieldName}, 1, false, span);");
                    break;

                case "mat2x3":
                    builder.AppendLine("                var span = new Span<float>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12, value.M13,");
                    builder.AppendLine("                    value.M21, value.M22, value.M23");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix2x3({locationFieldName}, 1, false, span);");
                    break;

                case "mat2x4":
                    builder.AppendLine("                var span = new Span<float>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12, value.M13, value.M14,");
                    builder.AppendLine("                    value.M21, value.M22, value.M23, value.M24");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix2x4({locationFieldName}, 1, false, span);");
                    break;

                case "mat3":
                case "mat3x3":
                    builder.AppendLine("                var span = new Span<float>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12, value.M13,");
                    builder.AppendLine("                    value.M21, value.M22, value.M23,");
                    builder.AppendLine("                    value.M31, value.M32, value.M33");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix3({locationFieldName}, 1, false, span);");
                    break;

                case "mat3x2":
                    builder.AppendLine("                var span = new Span<float>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12,");
                    builder.AppendLine("                    value.M21, value.M22,");
                    builder.AppendLine("                    value.M31, value.M32");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix3x2({locationFieldName}, 1, false, span);");
                    break;

                case "mat3x4":
                    builder.AppendLine("                var span = new Span<float>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12, value.M13, value.M14,");
                    builder.AppendLine("                    value.M21, value.M22, value.M23, value.M24,");
                    builder.AppendLine("                    value.M31, value.M32, value.M33, value.M34");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix3x4({locationFieldName}, 1, false, span);");
                    break;

                case "mat4":
                case "mat4x4":
                    builder.AppendLine("                var span = new Span<float>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12, value.M13, value.M14,");
                    builder.AppendLine("                    value.M21, value.M22, value.M23, value.M24,");
                    builder.AppendLine("                    value.M31, value.M32, value.M33, value.M34,");
                    builder.AppendLine("                    value.M41, value.M42, value.M43, value.M44");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix4({locationFieldName}, 1, false, span);");
                    break;

                case "mat4x2":
                    builder.AppendLine("                var span = new Span<float>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12,");
                    builder.AppendLine("                    value.M21, value.M22,");
                    builder.AppendLine("                    value.M31, value.M32,");
                    builder.AppendLine("                    value.M41, value.M42");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix4x2({locationFieldName}, 1, false, span);");
                    break;

                case "mat4x3":
                    builder.AppendLine("                var span = new Span<float>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12, value.M13,");
                    builder.AppendLine("                    value.M21, value.M22, value.M23,");
                    builder.AppendLine("                    value.M31, value.M32, value.M33,");
                    builder.AppendLine("                    value.M41, value.M42, value.M43");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix4x3({locationFieldName}, 1, false, span);");
                    break;

                case "dmat2":
                case "dmat2x2":
                    builder.AppendLine("                var span = new Span<double>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12,");
                    builder.AppendLine("                    value.M21, value.M22");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix2({locationFieldName}, 1, false, span);");
                    break;

                case "dmat2x3":
                    builder.AppendLine("                var span = new Span<double>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12, value.M13,");
                    builder.AppendLine("                    value.M21, value.M22, value.M23");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix2x3({locationFieldName}, 1, false, span);");
                    break;

                case "dmat2x4":
                    builder.AppendLine("                var span = new Span<double>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12, value.M13, value.M14,");
                    builder.AppendLine("                    value.M21, value.M22, value.M23, value.M24");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix2x4({locationFieldName}, 1, false, span);");
                    break;

                case "dmat3":
                case "dmat3x3":
                    builder.AppendLine("                var span = new Span<double>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12, value.M13,");
                    builder.AppendLine("                    value.M21, value.M22, value.M23,");
                    builder.AppendLine("                    value.M31, value.M32, value.M33");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix3({locationFieldName}, 1, false, span);");
                    break;

                case "dmat3x2":
                    builder.AppendLine("                var span = new Span<double>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12,");
                    builder.AppendLine("                    value.M21, value.M22,");
                    builder.AppendLine("                    value.M31, value.M32");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix3x2({locationFieldName}, 1, false, span);");
                    break;

                case "dmat3x4":
                    builder.AppendLine("                var span = new Span<double>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12, value.M13, value.M14,");
                    builder.AppendLine("                    value.M21, value.M22, value.M23, value.M24,");
                    builder.AppendLine("                    value.M31, value.M32, value.M33, value.M34");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix3x4({locationFieldName}, 1, false, span);");
                    break;

                case "dmat4":
                case "dmat4x4":
                    builder.AppendLine("                var span = new Span<double>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12, value.M13, value.M14,");
                    builder.AppendLine("                    value.M21, value.M22, value.M23, value.M24,");
                    builder.AppendLine("                    value.M31, value.M32, value.M33, value.M34,");
                    builder.AppendLine("                    value.M41, value.M42, value.M43, value.M44");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix4({locationFieldName}, 1, false, span);");
                    break;

                case "dmat4x2":
                    builder.AppendLine("                var span = new Span<double>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12,");
                    builder.AppendLine("                    value.M21, value.M22,");
                    builder.AppendLine("                    value.M31, value.M32,");
                    builder.AppendLine("                    value.M41, value.M42");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix4x2({locationFieldName}, 1, false, span);");
                    break;

                case "dmat4x3":
                    builder.AppendLine("                var span = new Span<double>(new[]");
                    builder.AppendLine("                {");
                    builder.AppendLine("                    value.M11, value.M12, value.M13,");
                    builder.AppendLine("                    value.M21, value.M22, value.M23,");
                    builder.AppendLine("                    value.M31, value.M32, value.M33,");
                    builder.AppendLine("                    value.M41, value.M42, value.M43");
                    builder.AppendLine("                });");
                    builder.AppendLine($"                _gl.UniformMatrix4x3({locationFieldName}, 1, false, span);");
                    break;

                default:
                    builder.AppendLine($"                throw new NotSupportedException(\"Unsupported uniform type: {type}\");");
                    break;
            }

            builder.AppendLine("            }");
            return builder.ToString();
        }


        public static string GlFunc(string type, string locationFieldName, string fielfName)
        {
            return type switch
            {
                "bool"    => $"_gl.Uniform1({locationFieldName}, {fielfName}.Value ? 1 : 0)",
                "int"     => $"_gl.Uniform1({locationFieldName}, {fielfName}.Value)",
                "uint"    => $"_gl.Uniform1({locationFieldName}, {fielfName}.Value)",
                "float"   => $"_gl.Uniform1({locationFieldName}, {fielfName}.Value)",
                "double"  => $"_gl.Uniform1({locationFieldName}, {fielfName}.Value)",
                               
                "bvec2"   => $"_gl.Uniform2({locationFieldName}, {fielfName}.Value.X ? 1 : 0, {fielfName}.Value.Y ? 1 : 0)",
                "bvec3"   => $"_gl.Uniform3({locationFieldName}, {fielfName}.Value.X ? 1 : 0, {fielfName}.Value.Y ? 1 : 0, {fielfName}.Value.Z ? 1 : 0)",
                "bvec4"   => $"_gl.Uniform4({locationFieldName}, {fielfName}.Value.X ? 1 : 0, {fielfName}.Value.Y ? 1 : 0, {fielfName}.Value.Z ? 1 : 0, {fielfName}.Value.W ? 1 : 0)",
                               
                "ivec2"   => $"_gl.Uniform2({locationFieldName}, {fielfName}.Value.X, {fielfName}.Value.Y)",
                "ivec3"   => $"_gl.Uniform3({locationFieldName}, {fielfName}.Value.X, {fielfName}.Value.Y, {fielfName}.Value.Z)",
                "ivec4"   => $"_gl.Uniform4({locationFieldName}, {fielfName}.Value.X, {fielfName}.Value.Y, {fielfName}.Value.Z, {fielfName}.Value.W)",
                               
                "uvec2"   => $"_gl.Uniform2({locationFieldName}, {fielfName}.Value.X, {fielfName}.Value.Y)",
                "uvec3"   => $"_gl.Uniform3({locationFieldName}, {fielfName}.Value.X, {fielfName}.Value.Y, {fielfName}.Value.Z)",
                "uvec4"   => $"_gl.Uniform4({locationFieldName}, {fielfName}.Value.X, {fielfName}.Value.Y, {fielfName}.Value.Z, {fielfName}.Value.W)",
                               
                "vec2"    => $"_gl.Uniform2({locationFieldName}, {fielfName}.Value.X, {fielfName}.Value.Y)",
                "vec3"    => $"_gl.Uniform3({locationFieldName}, {fielfName}.Value.X, {fielfName}.Value.Y, {fielfName}.Value.Z)",
                "vec4"    => $"_gl.Uniform4({locationFieldName}, {fielfName}.Value.X, {fielfName}.Value.Y, {fielfName}.Value.Z, {fielfName}.Value.W)",
                               
                "dvec2"   => $"_gl.Uniform2({locationFieldName}, {fielfName}.Value.X, {fielfName}.Value.Y)",
                "dvec3"   => $"_gl.Uniform3({locationFieldName}, {fielfName}.Value.X, {fielfName}.Value.Y, {fielfName}.Value.Z)",
                "dvec4"   => $"_gl.Uniform4({locationFieldName}, {fielfName}.Value.X, {fielfName}.Value.Y, {fielfName}.Value.Z,  {fielfName}.Value.W)",
                               
                "mat2"    => $"_gl.UniformMatrix2({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "mat3"    => $"_gl.UniformMatrix3({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "mat4"    => $"_gl.UniformMatrix4({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "mat2x2"  => $"_gl.UniformMatrix2({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "mat2x3"  => $"_gl.UniformMatrix2x3({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "mat2x4"  => $"_gl.UniformMatrix2x4({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "mat3x2"  => $"_gl.UniformMatrix3x2({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "mat3x3"  => $"_gl.UniformMatrix3({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "mat3x4"  => $"_gl.UniformMatrix3x4({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "mat4x2"  => $"_gl.UniformMatrix4x2({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "mat4x3"  => $"_gl.UniformMatrix4x3({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "mat4x4"  => $"_gl.UniformMatrix4({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                               
                "dmat2"   => $"_gl.UniformMatrix2({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "dmat3"   => $"_gl.UniformMatrix3({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "dmat4"   => $"_gl.UniformMatrix4({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "dmat2x2" => $"_gl.UniformMatrix2({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "dmat2x3" => $"_gl.UniformMatrix2x3({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "dmat2x4" => $"_gl.UniformMatrix2x4({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "dmat3x2" => $"_gl.UniformMatrix3x2({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "dmat3x3" => $"_gl.UniformMatrix3({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "dmat3x4" => $"_gl.UniformMatrix3x4({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "dmat4x2" => $"_gl.UniformMatrix4x2({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "dmat4x3" => $"_gl.UniformMatrix4x3({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                "dmat4x4" => $"_gl.UniformMatrix4({locationFieldName}, 1, false, ref {fielfName}.Value.M11)",
                _         => $"_gl.Uniform1({locationFieldName}, value)"
            };
        }
        public static string MapGlslTypeToCSharp(string glslType, HashSet<string> generatedTypes = null)
        {
            if (generatedTypes != null && generatedTypes.Contains(glslType))
            {
                return glslType;
            }

            return glslType switch
            {
                "bool"                   => "bool",
                "int"                    => "int",
                "uint"                   => "uint",
                "float"                  => "float",
                "double"                 => "double",

                "bvec2"                  => "Vector2D<bool>",
                "bvec3"                  => "Vector3D<bool>",
                "bvec4"                  => "Vector4D<bool>",

                "ivec2"                  => "Vector2D<int>",
                "ivec3"                  => "Vector3D<int>",
                "ivec4"                  => "Vector4D<int>",

                "uvec2"                  => "Vector2D<uint>",
                "uvec3"                  => "Vector3D<uint>",
                "uvec4"                  => "Vector4D<uint>",

                "vec2"                   => "Vector2D<float>",
                "vec3"                   => "Vector3D<float>",
                "vec4"                   => "Vector4D<float>",

                "dvec2"                  => "Vector2D<double>",
                "dvec3"                  => "Vector3D<double>",
                "dvec4"                  => "Vector4D<double>",

                "mat2"                   => "Matrix2X2<float>",
                "mat3"                   => "Matrix3X3<float>",
                "mat4"                   => "Matrix4X4<float>",
                "mat2x2"                 => "Matrix2X2<float>",
                "mat2x3"                 => "Matrix2X3<float>",
                "mat2x4"                 => "Matrix2X4<float>",
                "mat3x2"                 => "Matrix3X2<float>",
                "mat3x3"                 => "Matrix3X3<float>",
                "mat3x4"                 => "Matrix3X4<float>",
                "mat4x2"                 => "Matrix4X2<float>",
                "mat4x3"                 => "Matrix4X3<float>",
                "mat4x4"                 => "Matrix4X4<float>",

                "dmat2"                  => "Matrix2X2<double>",
                "dmat3"                  => "Matrix3X3<double>",
                "dmat4"                  => "Matrix4X4<double>",
                "dmat2x2"                => "Matrix2X2<double>",
                "dmat2x3"                => "Matrix2X3<double>",
                "dmat2x4"                => "Matrix2X4<double>",
                "dmat3x2"                => "Matrix3X2<double>",
                "dmat3x3"                => "Matrix3X3<double>",
                "dmat3x4"                => "Matrix3X4<double>",
                "dmat4x2"                => "Matrix4X2<double>",
                "dmat4x3"                => "Matrix4X3<double>",
                "dmat4x4"                => "Matrix4X4<double>",

                "sampler1D"              => "object",
                "sampler2D"              => "object",
                "sampler3D"              => "object",
                "samplerCube"            => "object",
                "sampler2DRect"          => "object",
                "sampler1DArray"         => "object",
                "sampler2DArray"         => "object",
                "samplerCubeArray"       => "object",
                "samplerBuffer"          => "object",
                "sampler2DMS"            => "object",
                "sampler2DMSArray"       => "object",

                "sampler1DShadow"        => "object",
                "sampler2DShadow"        => "object",
                "samplerCubeShadow"      => "object",
                "sampler2DRectShadow"    => "object",
                "sampler1DArrayShadow"   => "object",
                "sampler2DArrayShadow"   => "object",
                "samplerCubeArrayShadow" => "object",

                "isampler1D"             => "object",
                "isampler2D"             => "object",
                "isampler3D"             => "object",
                "isamplerCube"           => "object",
                "isampler2DRect"         => "object",
                "isampler1DArray"        => "object",
                "isampler2DArray"        => "object",
                "isamplerCubeArray"      => "object",
                "isamplerBuffer"         => "object",
                "isampler2DMS"           => "object",
                "isampler2DMSArray"      => "object",

                "usampler1D"             => "object",
                "usampler2D"             => "object",
                "usampler3D"             => "object",
                "usamplerCube"           => "object",
                "usampler2DRect"         => "object",
                "usampler1DArray"        => "object",
                "usampler2DArray"        => "object",
                "usamplerCubeArray"      => "object",
                "usamplerBuffer"         => "object",
                "usampler2DMS"           => "object",
                "usampler2DMSArray"      => "object",

                "atomic_uint"            => "uint",
                "image1D"                => "object",
                "image2D"                => "object",
                "image3D"                => "object",

                "void"                   => "void",
                "struct"                 => "struct",
                _                        => glslType
            };
        }
    }
}
