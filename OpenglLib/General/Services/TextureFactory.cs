using Silk.NET.OpenGL;
using AtomEngine;
using EngineLib;

namespace OpenglLib
{
    public class TextureFactory : IService, IDisposable
    {
        protected Dictionary<string, Texture> _cacheTexture = new Dictionary<string, Texture>();

        public Task InitializeAsync() => Task.CompletedTask;
        public Texture CreateTextureFromGuid(GL gl, string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                throw new ArgumentException("Texture GUID cannot be null or empty", nameof(guid));
            }

            string texturePath = ServiceHub.Get<MetadataManager>().GetPathByGuid(guid);
            if (string.IsNullOrEmpty(texturePath) || !File.Exists(texturePath))
            {
                DebLogger.Error($"Texture file not found for GUID: {guid}");
                return null;
            }

            var metadata = ServiceHub.Get<MetadataManager>().GetMetadata(texturePath) as TextureMetadata;
            return CreateTextureFromPath(gl, texturePath, metadata);
        }

        public Texture CreateTextureFromPath(GL gl, string texturePath)
        {
            if (_cacheTexture.TryGetValue(texturePath, out Texture cacheTexture)) { return cacheTexture; }

            try
            {
                var texture = new Texture(
                    gl,
                texturePath,
                Silk.NET.Assimp.TextureType.Diffuse);
                _cacheTexture[texturePath] = texture;
                return texture;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Failed to create texture from {texturePath}: {ex.Message}");
                return null;
            }
        }

        public Texture CreateTextureFromPath(GL gl, string texturePath, TextureMetadata metadata)
        {
            if (_cacheTexture.TryGetValue(texturePath, out Texture cacheTexture)) { return cacheTexture; }

            try
            {
                var texture = new Texture(
                    gl,
                    texturePath,
                    metadata == null ? Silk.NET.Assimp.TextureType.Diffuse : metadata.TextureType);

                if (metadata != null)
                {
                    var minFilter = metadata.GenerateMipmaps ? TextureMinFilter.NearestMipmapNearest : metadata.MinFilter;

                    if (metadata.GenerateMipmaps)
                    {
                        switch (minFilter)
                        {
                            case TextureMinFilter.Nearest:
                                minFilter = TextureMinFilter.NearestMipmapNearest;
                                break;
                            case TextureMinFilter.Linear:
                                minFilter = TextureMinFilter.LinearMipmapNearest;
                                break;
                        }
                    }
                    else
                    {
                        switch (minFilter)
                        {
                            case TextureMinFilter.NearestMipmapNearest:
                            case TextureMinFilter.NearestMipmapLinear:
                                minFilter = TextureMinFilter.Nearest;
                                break;
                            case TextureMinFilter.LinearMipmapNearest:
                            case TextureMinFilter.LinearMipmapLinear:
                                minFilter = TextureMinFilter.Linear;
                                break;
                        }
                    }


                    texture.Target = metadata.TextureTarget;
                    texture.ConfigureFromParameters(
                        wrapMode: metadata.WrapMode,
                        anisoLevel: metadata.AnisoLevel,
                        generateMipmaps: metadata.GenerateMipmaps,
                        compressed: metadata.CompressTexture,
                        compressionFormat: metadata.CompressionFormat,
                        maxSize: (uint)metadata.MaxSize,
                        minFilter: minFilter,
                        magFilter: metadata.MagFilter
                    );
                }
                _cacheTexture[texturePath] = texture;
                return texture;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Failed to create texture from {texturePath}: {ex.Message}");
                return null;
            }
        }

        public void ClearCache()
        {
            Dispose();
        }

        public void Dispose()
        {
            foreach (var texturePairPathtexture in _cacheTexture)
            {
                texturePairPathtexture.Value.Dispose();
            }
            _cacheTexture.Clear();
        }
    }



    //public class MaterialFactory : IService, IDisposable
    //{
    //    private Dictionary<string, ShaderBase> _shaderInstanceCache = new Dictionary<string, ShaderBase>();
    //    private Dictionary<ShaderBase, GL> _glShaderMap = new Dictionary<ShaderBase, GL>();
    //    private TextureFactory _textureFactory;
    //    private AssemblyManager _assemblyManager;

    //    public Task InitializeAsync()
    //    {
    //        _textureFactory = ServiceHub.Get<TextureFactory>();
    //        _assemblyManager = ServiceHub.Get<AssemblyManager>();
    //        return Task.CompletedTask;
    //    }

    //    public ShaderBase CreateMaterialInstance(GL gl, MaterialAsset material)
    //    {
    //        if (material == null)
    //        {
    //            DebLogger.Error("Невозможно создать экземпляр материала из null MaterialAsset");
    //            return null;
    //        }

    //        if (_shaderInstanceCache.TryGetValue(material.Guid, out ShaderBase cachedInstance))
    //        {
    //            return cachedInstance;
    //        }

    //        try
    //        {
    //            var instance = CreateShaderRepresentationInstance(gl, material.ShaderRepresentationTypeName);
    //            if (instance == null)
    //            {
    //                return null;
    //            }
    //            ApplyUniformValues(instance, material.UniformValues);
    //            ApplyTextures(gl, instance, material.TextureReferences);
    //            _shaderInstanceCache[material.Guid] = instance;
    //            _glShaderMap[instance] = gl;
    //            return instance;
    //        }
    //        catch (Exception ex)
    //        {
    //            DebLogger.Error($"Ошибка создания экземпляра материала: {ex.Message}");
    //            return null;
    //        }
    //    }

    //    public ShaderBase CreateMaterialInstance(GL gl, string materialPath)
    //    {
    //        try
    //        {
    //            MaterialAsset material = ServiceHub.Get<MaterialManager>().LoadMaterial(materialPath);
    //            if (material == null)
    //            {
    //                DebLogger.Error($"Не удалось загрузить материал из пути: {materialPath}");
    //                return null;
    //            }

    //            return CreateMaterialInstance(gl, material);
    //        }
    //        catch (Exception ex)
    //        {
    //            DebLogger.Error($"Ошибка создания экземпляра материала из пути: {ex.Message}");
    //            return null;
    //        }
    //    }

    //    public ShaderBase CreateMaterialInstanceFromGuid(GL gl, string materialGuid)
    //    {
    //        try
    //        {
    //            string materialPath = ServiceHub.Get<MetadataManager>().GetPathByGuid(materialGuid);
    //            if (string.IsNullOrEmpty(materialPath))
    //            {
    //                DebLogger.Error($"Материал не найден для GUID: {materialGuid}");
    //                return null;
    //            }

    //            return CreateMaterialInstance(gl, materialPath);
    //        }
    //        catch (Exception ex)
    //        {
    //            DebLogger.Error($"Ошибка создания экземпляра материала из GUID: {ex.Message}");
    //            return null;
    //        }
    //    }

    //    private ShaderBase CreateShaderRepresentationInstance(GL gl, string typeName)
    //    {
    //        try
    //        {
    //            Type representationType = FindShaderRepresentationType(typeName);
    //            if (representationType == null)
    //            {
    //                DebLogger.Error($"Тип шейдерного представления не найден: {typeName}");
    //                return null;
    //            }

    //            if (!typeof(ShaderBase).IsAssignableFrom(representationType))
    //            {
    //                DebLogger.Error($"Тип {typeName} не является допустимым шейдерным представлением (должен наследовать от Mat)");
    //                return null;
    //            }

    //            var constructor = representationType.GetConstructor(new[] { typeof(GL) });
    //            if (constructor == null)
    //            {
    //                DebLogger.Error($"Тип шейдерного представления {typeName} не имеет конструктора, принимающего параметр GL");
    //                return null;
    //            }
    //            return (ShaderBase)constructor.Invoke(new object[] { gl });
    //        }
    //        catch (Exception ex)
    //        {
    //            DebLogger.Error($"Ошибка создания экземпляра шейдерного представления: {ex.Message}");
    //            return null;
    //        }
    //    }

    //    private Type FindShaderRepresentationType(string typeName)
    //    {
    //        Type type = _assemblyManager.FindType(typeName, true);
    //        if (type != null)
    //        {
    //            return type;
    //        }
    //        return null;
    //    }

    //    internal void ApplyUniformValues(string materialGuid, Dictionary<string, object> uniformValues)
    //    {
    //        if (_shaderInstanceCache.TryGetValue(materialGuid, out var shader))
    //        {
    //            ApplyUniformValues(shader, uniformValues);
    //        }
    //    }

    //    private void ApplyUniformValues(ShaderBase instance, Dictionary<string, object> uniformValues)
    //    {
    //        if (uniformValues == null || uniformValues.Count == 0)
    //        {
    //            return;
    //        }

    //        Type instanceType = instance.GetType();

    //        foreach (var kvp in uniformValues)
    //        {
    //            string propertyName = kvp.Key;
    //            object value = kvp.Value;

    //            if (value == null)
    //            {
    //                continue;
    //            }

    //            try
    //            {
    //                PropertyInfo property = instanceType.GetProperty(propertyName);
    //                if (property != null && property.CanWrite)
    //                {
    //                    object convertedValue = ConvertValueToTargetType(value, property.PropertyType);
    //                    Type type = convertedValue.GetType();
    //                    instance.Use();
    //                    property.SetValue(instance, convertedValue);
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                DebLogger.Warn($"Не удалось установить свойство {propertyName} для {instanceType.Name}: {ex.Message}");
    //            }
    //        }
    //    }

    //    private object ConvertValueToTargetType(object value, Type targetType)
    //    {
    //        if (value != null && targetType.IsAssignableFrom(value.GetType()))
    //            return value;

    //        if (value is System.Numerics.Vector2 vec2 && targetType == typeof(Silk.NET.Maths.Vector2D<float>))
    //            return vec2.ToSilk();

    //        if (value is System.Numerics.Vector3 vec3 && targetType == typeof(Silk.NET.Maths.Vector3D<float>))
    //            return vec3.ToSilk();

    //        if (value is System.Numerics.Vector4 vec4 && targetType == typeof(Silk.NET.Maths.Vector4D<float>))
    //            return vec4.ToSilk();

    //        if (value is Newtonsoft.Json.Linq.JObject jObject)
    //        {
    //            if (targetType == typeof(System.Numerics.Vector2))
    //            {
    //                float x = jObject["X"]?.ToObject<float>() ?? 0f;
    //                float y = jObject["Y"]?.ToObject<float>() ?? 0f;
    //                return new Silk.NET.Maths.Vector2D<float>(x, y);
    //            }
    //            else if (targetType == typeof(System.Numerics.Vector3))
    //            {
    //                float x = jObject["X"]?.ToObject<float>() ?? 0f;
    //                float y = jObject["Y"]?.ToObject<float>() ?? 0f;
    //                float z = jObject["Z"]?.ToObject<float>() ?? 0f;
    //                return new Silk.NET.Maths.Vector3D<float>(x, y, z);
    //            }
    //            else if (targetType == typeof(System.Numerics.Vector4))
    //            {
    //                float x = jObject["X"]?.ToObject<float>() ?? 0f;
    //                float y = jObject["Y"]?.ToObject<float>() ?? 0f;
    //                float z = jObject["Z"]?.ToObject<float>() ?? 0f;
    //                float w = jObject["W"]?.ToObject<float>() ?? 0f;
    //                return new Silk.NET.Maths.Vector4D<float>(x, y, z, w);
    //            }
    //            else if (targetType == typeof(Silk.NET.Maths.Vector2D<float>))
    //            {
    //                float x = jObject["X"]?.ToObject<float>() ?? 0f;
    //                float y = jObject["Y"]?.ToObject<float>() ?? 0f;
    //                return new Silk.NET.Maths.Vector2D<float>(x, y);
    //            }
    //            else if (targetType == typeof(Silk.NET.Maths.Vector3D<float>))
    //            {
    //                float x = jObject["X"]?.ToObject<float>() ?? 0f;
    //                float y = jObject["Y"]?.ToObject<float>() ?? 0f;
    //                float z = jObject["Z"]?.ToObject<float>() ?? 0f;
    //                return new Silk.NET.Maths.Vector3D<float>(x, y, z);
    //            }
    //            else if (targetType == typeof(Silk.NET.Maths.Vector4D<float>))
    //            {
    //                float x = jObject["X"]?.ToObject<float>() ?? 0f;
    //                float y = jObject["Y"]?.ToObject<float>() ?? 0f;
    //                float z = jObject["Z"]?.ToObject<float>() ?? 0f;
    //                float w = jObject["W"]?.ToObject<float>() ?? 0f;
    //                return new Silk.NET.Maths.Vector4D<float>(x, y, z, w);
    //            }
    //            else
    //            {
    //                return jObject.ToObject(targetType);
    //            }
    //        }

    //        if (targetType.IsPrimitive || targetType == typeof(string))
    //        {
    //            return Convert.ChangeType(value, targetType);
    //        }

    //        if (targetType.IsEnum)
    //        {
    //            if (value is string strValue)
    //            {
    //                return Enum.Parse(targetType, strValue);
    //            }
    //            else
    //            {
    //                return Enum.ToObject(targetType, value);
    //            }
    //        }

    //        return value;
    //    }

    //    internal void ApplyTextures(string materialGuid, Dictionary<string, string> textureReferences)
    //    {
    //        if (_shaderInstanceCache.TryGetValue(materialGuid, out var shaderInstance))
    //        {
    //            ApplyTextures(gl, shaderInstance, textureReferences);
    //        }
    //    }

    //    private void ApplyTextures(GL gl, ShaderBase instance, Dictionary<string, string> textureReferences)
    //    {
    //        if (textureReferences == null || textureReferences.Count == 0)
    //        {
    //            return;
    //        }

    //        Type instanceType = instance.GetType();

    //        foreach (var kvp in textureReferences)
    //        {
    //            string textureName = kvp.Key;
    //            string textureGuid = kvp.Value;

    //            if (string.IsNullOrEmpty(textureGuid))
    //            {
    //                continue;
    //            }

    //            try
    //            {
    //                string methodName = $"{textureName}_SetTexture";
    //                MethodInfo method = instanceType.GetMethod(methodName);

    //                if (method != null)
    //                {
    //                    Texture texture = _textureFactory.CreateTextureFromGuid(gl, textureGuid);

    //                    if (texture != null)
    //                    {
    //                        instance.Use();
    //                        gl.ActiveTexture(TextureUnit.Texture0);
    //                        method.Invoke(instance, new object[] { texture });
    //                        DebLogger.Debug($"Применена текстура {textureName} к материалу {instanceType.Name}");
    //                    }
    //                }
    //                else
    //                {
    //                    DebLogger.Warn($"Метод установки текстуры {methodName} не найден в типе {instanceType.Name}");
    //                }
    //            }
    //            catch (Exception ex)
    //            {
    //                DebLogger.Warn($"Не удалось установить текстуру {textureName} для {instanceType.Name}: {ex.Message}");
    //            }
    //        }
    //    }

    //    public void ClearCache()
    //    {
    //        Dispose();
    //    }

    //    public void Dispose()
    //    {
    //        foreach (var kvp in _shaderInstanceCache)
    //        {
    //            kvp.Value.Dispose();
    //        }
    //        _shaderInstanceCache.Clear();
    //        _glShaderMap.Clear();
    //    }
    //}
}
