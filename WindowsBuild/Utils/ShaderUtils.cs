using AtomEngine.RenderEntity;
using Silk.NET.OpenGL;

namespace WindowsBuild
{
    public static class ShaderUtils
    {
        public static void ApplyUniformValues(ShaderBase shader, Dictionary<string, object> uniformValues)
        {
            if (uniformValues == null || uniformValues.Count == 0) return;

            Type shaderType = shader.GetType();
            shader.Use();

            foreach (var pair in uniformValues)
            {
                string propertyName = pair.Key;
                object value = pair.Value;

                if (value == null) continue;

                try
                {
                    var property = shaderType.GetProperty(propertyName);
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(shader, TypeConverters.ConvertValueToTargetType(value, property.PropertyType));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при установке свойства {propertyName}: {ex.Message}");
                }
            }
        }

        public static void ApplyTextures(GL gl, ShaderBase shader, Dictionary<string, string> textureReferences, RuntimeResourceManager resourceManager)
        {
            if (textureReferences == null || textureReferences.Count == 0) return;

            Type shaderType = shader.GetType();
            shader.Use();

            int textureUnit = 0;
            foreach (var pair in textureReferences)
            {
                string samplerName = pair.Key;
                string textureGuid = pair.Value;

                if (string.IsNullOrEmpty(textureGuid)) continue;

                try
                {
                    string methodName = $"{samplerName}_SetTexture";
                    var method = shaderType.GetMethod(methodName);

                    if (method != null)
                    {
                        var texture = resourceManager.GetTexture(textureGuid);
                        if (texture != null)
                        {
                            gl.ActiveTexture(TextureUnit.Texture0 + textureUnit);
                            method.Invoke(shader, new object[] { texture });
                            textureUnit++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при установке текстуры {samplerName}: {ex.Message}");
                }
            }
        }

    }


}
