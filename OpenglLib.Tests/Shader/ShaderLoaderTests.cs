

namespace OpenglLib.Tests
{
    public class ShaderLoaderTests : IDisposable
    {
        private readonly string _testDirectory;

        public ShaderLoaderTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"ShaderTests_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);
            SetupTestFiles();
            ShaderLoader._customBasePath = _testDirectory;
        }

        private void SetupTestFiles()
        {
            // Создаем более сложную структуру файлов для тестирования различных сценариев
            var files = new Dictionary<string, string>
        {
            // Стандартные шейдеры
            {Path.Combine("StandardShader", "vertex.glsl"), "// Standard vertex shader\n"},
            {Path.Combine("StandardShader", "fragment.glsl"), "// Standard fragment shader\n"},
            
            // Пользовательские шейдеры
            {Path.Combine("CustomShader", "vertex.glsl"), "// Custom vertex shader\n"},
            {Path.Combine("CustomShader", "special.glsl"), "// Special shader\n"},
            
            // Шейдеры в подпапках
            {Path.Combine("Effects", "PostProcess", "blur.glsl"), "// Blur effect shader\n"},
            {Path.Combine("Effects", "PostProcess", "bloom.glsl"), "// Bloom effect shader\n"},
            
            // Уникальные шейдеры в корне
            {"unique.glsl", "// Unique shader\n"},
            {"test.glsl", "// Test shader\n"}
        };

            foreach (var file in files)
            {
                var fullPath = Path.Combine(_testDirectory, file.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllText(fullPath, file.Value);
            }
        }

        [Fact]
        public void LoadShader_WithExactPath_LoadsCorrectShader()
        {
            var result = ShaderLoader.LoadShader("StandardShader/vertex.glsl", false);
            Assert.Equal("// Standard vertex shader\n", result);
        }

        [Fact]
        public void LoadShader_WithDeepPath_LoadsCorrectShader()
        {
            var result = ShaderLoader.LoadShader("Effects/PostProcess/blur.glsl", false);
            Assert.Equal("// Blur effect shader\n", result);
        }

        [Fact]
        public void LoadShader_WithDifferentPathSeparators_LoadsCorrectShader()
        {
            var withSlash = ShaderLoader.LoadShader("StandardShader/fragment.glsl", false);
            var withBackslash = ShaderLoader.LoadShader("StandardShader\\fragment.glsl", false);
            var withDots = ShaderLoader.LoadShader("StandardShader.fragment.glsl", false);

            Assert.Equal("// Standard fragment shader\n", withSlash);
            Assert.Equal("// Standard fragment shader\n", withBackslash);
            Assert.Equal("// Standard fragment shader\n", withDots);
        }

        [Fact]
        public void LoadShader_WithNonExistentFile_ThrowsInformativeError()
        {
            var exception = Assert.Throws<ShaderError>(() =>
                ShaderLoader.LoadShader("nonexistent.glsl", false));

            Assert.Contains("Shader file not found", exception.Message);
            Assert.Contains("Available shaders:", exception.Message);
            Assert.Contains("unique.glsl", exception.Message);
            Assert.Contains("test.glsl", exception.Message);
        }

        [Fact]
        public void LoadShader_WithAmbiguousName_ThrowsException()
        {
            var exception = Assert.Throws<ShaderError>(() =>
                ShaderLoader.LoadShader("vertex.glsl", false));

            Assert.Contains("Ambiguous shader name", exception.Message);
            Assert.Contains("StandardShader", exception.Message);
            Assert.Contains("CustomShader", exception.Message);
        }

        [Fact]
        public void LoadShader_WithUniqueFileName_LoadsCorrectShader()
        {
            var result = ShaderLoader.LoadShader("unique.glsl", false);
            Assert.Equal("// Unique shader\n", result);
        }

        [Fact]
        public void LoadShader_WithInvalidPath_ThrowsError()
        {
            // Проверяем обработку некорректных путей
            var exception = Assert.Throws<ShaderError>(() =>
                ShaderLoader.LoadShader("../outside/shader.glsl", false));

            Assert.Contains("Shader file not found", exception.Message);
        }

        [Fact]
        public void LoadShader_CaseInsensitivePathComparison()
        {
            // Проверяем нечувствительность к регистру
            var lowerCase = ShaderLoader.LoadShader("standardshader/vertex.glsl", false);
            var upperCase = ShaderLoader.LoadShader("STANDARDSHADER/VERTEX.GLSL", false);

            Assert.Equal("// Standard vertex shader\n", lowerCase);
            Assert.Equal("// Standard vertex shader\n", upperCase);
        }

        [Fact]
        public void LoadShader_WithFileInSubdirectory_LoadsCorrectly()
        {
            var result = ShaderLoader.LoadShader("bloom.glsl", false);
            Assert.Equal("// Bloom effect shader\n", result);
        }

        public void Dispose()
        { 
            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, true);
                }
            }
            catch (IOException) { }
        }
    }
}
