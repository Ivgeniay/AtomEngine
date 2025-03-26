using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Editor.Utils.Generator
{
    internal static class ShaderCodeAnalyzer
    {
        public static ShaderMatrixInfo AnalyzeShaderMatrices(string shaderCode)
        {
            var matrixInfo = new ShaderMatrixInfo();
            string vertexShader = ExtractVertexShader(shaderCode);

            if (string.IsNullOrEmpty(vertexShader))
            {
                return matrixInfo;
            }
            var positionMatch = Regex.Match(vertexShader, @"gl_Position\s*=\s*([^;]+);");
            if (!positionMatch.Success)
            {
                return matrixInfo;
            }
            string positionExpression = positionMatch.Groups[1].Value.Trim();

            var matrixMatches = Regex.Matches(vertexShader, @"uniform\s+mat4\s+(\w+);");
            var matrices = new HashSet<string>();

            foreach (Match match in matrixMatches)
            {
                matrices.Add(match.Groups[1].Value);
            }
            foreach (string matrix in matrices)
            {
                if (Regex.IsMatch(positionExpression, $@"\b{matrix}\b"))
                {
                    DetermineMatrixType(matrix, matrixInfo);
                }
            }

            return matrixInfo;
        }

        private static string ExtractVertexShader(string shaderCode)
        {
            var match = Regex.Match(shaderCode, @"protected new string VertexSource = @""([^""]+)""");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return string.Empty;
        }

        private static void DetermineMatrixType(string matrixName, ShaderMatrixInfo matrixInfo)
        {
            string normalizedName = matrixName.ToLowerInvariant();

            if (normalizedName.Contains("model") || normalizedName == "m" || normalizedName == "world")
            {
                matrixInfo.UsesModelMatrix = true;
                matrixInfo.ModelMatrixName = matrixName;
            }
            else if (normalizedName.Contains("view") || normalizedName == "v" || normalizedName.Contains("camera"))
            {
                matrixInfo.UsesViewMatrix = true;
                matrixInfo.ViewMatrixName = matrixName;
                matrixInfo.NeedsCamera = true;
            }
            else if (normalizedName.Contains("proj") || normalizedName == "p")
            {
                matrixInfo.UsesProjectionMatrix = true;
                matrixInfo.ProjectionMatrixName = matrixName;
                matrixInfo.NeedsCamera = true;
            }
            else if (normalizedName.Contains("viewproj") || normalizedName.Contains("projview") || normalizedName == "vp" || normalizedName == "pv")
            {
                matrixInfo.UsesViewProjectionMatrix = true;
                matrixInfo.ViewProjectionMatrixName = matrixName;
                matrixInfo.NeedsCamera = true;
            }
            else if (normalizedName.Contains("mvp"))
            {
                matrixInfo.UsesModelViewProjectionMatrix = true;
                matrixInfo.ModelViewProjectionMatrixName = matrixName;
                matrixInfo.NeedsCamera = true;
            }
        }

        public static bool IsTextureType(string type)
        {
            return type.StartsWith("sampler") ||
                   type.StartsWith("isampler") ||
                   type.StartsWith("usampler") ||
                   type.StartsWith("Texture");
        }
    }

    internal class ShaderMatrixInfo
    {
        public bool NeedsCamera { get; set; }
        public bool UsesModelMatrix { get; set; }
        public bool UsesViewMatrix { get; set; }
        public bool UsesProjectionMatrix { get; set; }
        public bool UsesViewProjectionMatrix { get; set; }
        public bool UsesModelViewProjectionMatrix { get; set; }

        public string ModelMatrixName { get; set; }
        public string ViewMatrixName { get; set; }
        public string ProjectionMatrixName { get; set; }
        public string ViewProjectionMatrixName { get; set; }
        public string ModelViewProjectionMatrixName { get; set; }

        public static ShaderMatrixInfo FromSourceCode(string sourceCode)
        {
            return ShaderCodeAnalyzer.AnalyzeShaderMatrices(sourceCode);
        }
    }

    internal static class ShaderMatrixDetector
    {
        private static readonly string[] ModelMatrixNames = new string[]
        {
            "MODEL",
            "MODELMATRIX",
            "MODELMAT",
            "M0DEL",
            "M0DELMATRIX",
            "M0DELMAT",
            "MMATRIX",
            "MMAT",
            "MODL",
            "MODLMATRIX",
            "MODLMAT",
            "MODELM",
            "MODELMATR",
            "WORLDMATRIX",
            "WORLD",
            "W0RLD",
            "WORLDMAT",
            "WMAT",
            "LOCAL",
            "LOCALMATRIX",
            "LOCALMAT",
            "OBJECTMATRIX",
            "OBJECT",
            "OBJECTMAT",
            "OMAT",
            "LOCALTOWORLD",
            "LOCALTOWORLDMATRIX",
            "MODELTRANSFORM",
            "WORLDTRANSFORM",
            "MODELTOWORLDMATRIX",
            "MODELTOWORLD",
        };
        private static readonly string[] ViewMatrixNames = new string[]
        {
            "VIEW",
            "VIEWMATRIX",
            "VIEWMAT",
            "V1EW",
            "V1EWMATRIX",
            "V1EWMAT",
            "VMATRIX",
            "VMAT",
            "VMODEL",
            "CAMERAMATRIX",
            "CAMERA",
            "CAMERATRANSFORM",
            "CAMERAMAT",
            "VIEWTRANSFORM",
            "LOOKMATRIX",
            "LOOK",
            "LOOKATMATRIX",
            "LOOKAT",
            "LOOKMAT",
            "CAMERAVIEWMATRIX",
            "CAMERAVIEW",
            "EYEMATRIX",
            "EYE",
            "EYEMAT",
            "WORLDTOCAMERA",
            "WORLDTOCAMERAMATRIX",
            "WORLDTOVIEW",
            "WORLDTOVIEWMATRIX",
        };
        private static readonly string[] ProjectionMatrixNames = new string[]
        {
            "PROJ",
            "PROJECTION",
            "PROJECTIONMATRIX",
            "PROJMAT",
            "PR0J",
            "PR0JECTION",
            "PR0JECTIONMATRIX",
            "PR0JMAT",
            "PMATRIX",
            "PMAT",
            "PROJM",
            "PROJMATR",
            "PERSPECTIVE",
            "PERSPECTIVEMATRIX",
            "PERSPECTIVEMAT",
            "PERSPMAT",
            "PERSPM",
            "CAMERAPROJECTION",
            "CAMERAPROJECTIONMATRIX",
            "CAMERAPROJMAT",
            "CAMERAPROJ",
            "CAMPROJ",
            "CAMPROJMATRIX",
            "CAMPROJMAT",
            "FRUSTUM",
            "FRUSTUMMATRIX",
            "FRUSTUMMAT",
        };
        private static readonly string[] ViewProjectionMatrixNames = new string[]
        {
            "VIEWPROJ",
            "VIEWPROJECTION",
            "VIEWPROJECTIONMATRIX",
            "VIEWPROJMAT",
            "VPMATRIX",
            "VPMAT",
            "VP",
            "V1EWPROJ",
            "PROJVIEW",
            "PROJECTIONVIEW",
            "PROJECTIONVIEWMATRIX",
            "PROJVIEWMAT",
            "PVMATRIX",
            "PVMAT",
            "PV",
            "PR0JVIEW",
            "CAMVP",
            "CAMVIEWPROJ",
            "CAMERAVIEWPROJECTION",
            "CAMERAVIEWPROJ",
            "MVPMATRIX",
            "MVP",
            "MODELVIEWPROJECTION",
            "MODELVIEWPROJ",
            "MODELVIEWPROJECTIONMATRIX",
            "MODELVIEWPROJMAT",
            "M0DELVIEWPROJ",
        };

        public static bool UsesModelMatrix(string sourceCode)
        {
            return CheckMatrixNamesInCode(sourceCode, ModelMatrixNames);
        }
        public static bool UsesViewMatrix(string sourceCode)
        {
            return CheckMatrixNamesInCode(sourceCode, ViewMatrixNames);
        }
        public static bool UsesProjectionMatrix(string sourceCode)
        {
            return CheckMatrixNamesInCode(sourceCode, ProjectionMatrixNames);
        }
        public static bool UsesViewProjectionMatrix(string sourceCode)
        {
            return CheckMatrixNamesInCode(sourceCode, ViewProjectionMatrixNames);
        }
        public static bool NeedsCamera(string sourceCode)
        {
            return UsesViewMatrix(sourceCode) ||
                   UsesProjectionMatrix(sourceCode) ||
                   UsesViewProjectionMatrix(sourceCode);
        }
        public static string GetModelMatrixName(string sourceCode)
        {
            return FindMatchingMatrixName(sourceCode, ModelMatrixNames);
        }
        public static string GetViewMatrixName(string sourceCode)
        {
            return FindMatchingMatrixName(sourceCode, ViewMatrixNames);
        }
        public static string GetProjectionMatrixName(string sourceCode)
        {
            return FindMatchingMatrixName(sourceCode, ProjectionMatrixNames);
        }
        public static string GetViewProjectionMatrixName(string sourceCode)
        {
            return FindMatchingMatrixName(sourceCode, ViewProjectionMatrixNames);
        }

       
        private static bool CheckMatrixNamesInCode(string sourceCode, string[] matrixNames)
        {
            string normalizedCode = NormalizeCode(sourceCode);

            foreach (var matrixName in matrixNames)
            {
                string normalizedMatrixName = NormalizeCode(matrixName);
                string pattern = $@"(uniform\s+\w+\s+{normalizedMatrixName}\b)|(\b{normalizedMatrixName}\s*[=;])";

                if (Regex.IsMatch(normalizedCode, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        private static string FindMatchingMatrixName(string sourceCode, string[] matrixNames)
        {
            string code = sourceCode;

            foreach (var matrixName in matrixNames)
            {
                string pattern = $@"public\s+(?:unsafe\s+)?(?:Matrix4X4<float>|Matrix4x4)\s+(\b{matrixName}\b)";

                var match = Regex.Match(code, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }
        private static string NormalizeCode(string code)
        {
            return code.Replace("_", "").ToLowerInvariant();
        }
    }
}
