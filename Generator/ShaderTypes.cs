using System.Text;

namespace OpenglLib.Generator
{
    internal static class ShaderTypes
    {
        public static HashSet<string> GeneratedTypes = new HashSet<string>();

        public static bool IsCustomType(string csharpType, string type) =>
            csharpType == type && type != "float" && type != "bool" && type != "int" && type != "uint" && type != "float" && type != "double";

        public static string GetSimpleGetter(string cashFieldName)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("            get");
            builder.AppendLine("            {");
            builder.AppendLine($"                return {cashFieldName};");
            builder.AppendLine("            }");
            return builder.ToString();
        }

        public static string GetPropertyForLocaleArray(string type, string fieldName, string locationFieldName)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"        public int {locationFieldName}");
            builder.AppendLine($"        {{"); 
            builder.AppendLine($"             get => {fieldName}.Location;"); 
            builder.AppendLine($"             set => {fieldName}.Location = value;"); 
            builder.AppendLine($"        }}"); 


            return builder.ToString();
        }

        public static string GetSetter(string type, string locationFieldName, string cashFieldName)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("            set");
            builder.AppendLine("            {");
            builder.AppendLine($"                if ({locationFieldName} == -1)");
            builder.AppendLine($"                {{");
            builder.AppendLine($"                   DebLogger.Warn(\"You try to set value to -1 lcation field\");");
            builder.AppendLine($"                   return;");
            builder.AppendLine($"                }}");
            //builder.AppendLine($"                if ({cashFieldName} == value) return;");
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


                // Матричные типы float
                case "mat2":
                case "mat2x2":
                    builder.AppendLine($"                var mat2 = (Matrix2X2<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix2({locationFieldName}, 1, false, (float*)&mat2);");
                    break;

                case "mat2x3":
                    builder.AppendLine($"                var mat2x3 = (Matrix2X3<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix2x3({locationFieldName}, 1, false, (float*)&mat2x3);");
                    break;

                case "mat2x4":
                    builder.AppendLine($"                var mat2x4 = (Matrix2X4<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix2x4({locationFieldName}, 1, false, (float*)&mat2x4);");
                    break;

                case "mat3":
                case "mat3x3":
                    builder.AppendLine($"                var mat3 = (Matrix3X3<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix3({locationFieldName}, 1, false, (float*)&mat3);");
                    break;

                case "mat3x2":
                    builder.AppendLine($"                var mat3x2 = (Matrix3X2<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix3x2({locationFieldName}, 1, false, (float*)&mat3x2);");
                    break;

                case "mat3x4":
                    builder.AppendLine($"                var mat3x4 = (Matrix3X4<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix3x4({locationFieldName}, 1, false, (float*)&mat3x4);");
                    break;

                case "mat4":
                case "mat4x4":
                    builder.AppendLine($"                var mat4 = (Matrix4X4<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix4({locationFieldName}, 1, false, (float*)&mat4);");

                    break;

                case "mat4x2":
                    builder.AppendLine($"                var mat4x2 = (Matrix4X2<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix4x2({locationFieldName}, 1, false, (float*)&mat4x2);");
                    break;

                case "mat4x3":
                    builder.AppendLine($"                var mat4x3 = (Matrix4X3<float>)value;");
                    builder.AppendLine($"                _gl.UniformMatrix4x3({locationFieldName}, 1, false, (float*)&mat4x3);");
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

                "sampler1D"              => "int",
                "sampler2D"              => "int",
                "sampler3D"              => "int",
                "samplerCube"            => "int",
                "sampler2DRect"          => "int",
                "sampler1DArray"         => "int",
                "sampler2DArray"         => "int",
                "samplerCubeArray"       => "int",
                "samplerBuffer"          => "int",
                "sampler2DMS"            => "int",
                "sampler2DMSArray"       => "int",

                "sampler1DShadow"        => "int",
                "sampler2DShadow"        => "int",
                "samplerCubeShadow"      => "int",
                "sampler2DRectShadow"    => "int",
                "sampler1DArrayShadow"   => "int",
                "sampler2DArrayShadow"   => "int",
                "samplerCubeArrayShadow" => "int",

                "isampler1D"             => "int",
                "isampler2D"             => "int",
                "isampler3D"             => "int",
                "isamplerCube"           => "int",
                "isampler2DRect"         => "int",
                "isampler1DArray"        => "int",
                "isampler2DArray"        => "int",
                "isamplerCubeArray"      => "int",
                "isamplerBuffer"         => "int",
                "isampler2DMS"           => "int",
                "isampler2DMSArray"      => "int",

                "usampler1D"             => "int",
                "usampler2D"             => "int",
                "usampler3D"             => "int",
                "usamplerCube"           => "int",
                "usampler2DRect"         => "int",
                "usampler1DArray"        => "int",
                "usampler2DArray"        => "int",
                "usamplerCubeArray"      => "int",
                "usamplerBuffer"         => "int",
                "usampler2DMS"           => "int",
                "usampler2DMSArray"      => "int",

                "atomic_uint"            => "uint",
                "image1D"                => "int",
                "image2D"                => "int",
                "image3D"                => "int",

                "void"                   => "void",
                "struct"                 => "struct",
                _                        => glslType
            };
        }
    }
}
