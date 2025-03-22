using AtomEngine.RenderEntity;
using Silk.NET.OpenGL;
using AtomEngine;
using EngineLib;
using System.Reflection;

namespace OpenglLib
{
    public class MaterialFactory : IService, IDisposable
    {
        protected Dictionary<string, ShaderBase> _shaderInstanceCache = new Dictionary<string, ShaderBase>();
        protected Dictionary<ShaderBase, GL> _glShaderMap = new Dictionary<ShaderBase, GL>();
        protected TextureFactory _textureFactory;
        protected AssemblyManager _assemblyManager;

        public virtual Task InitializeAsync()
        {
            _textureFactory = ServiceHub.Get<TextureFactory>();
            _assemblyManager = ServiceHub.Get<AssemblyManager>();

            return Task.CompletedTask;
        }

        public virtual ShaderBase CreateMaterialInstance(GL gl, MaterialAsset material)
        {
            if (material == null)
            {
                DebLogger.Error("Невозможно создать экземпляр материала из null MaterialAsset");
                return null;
            }

            if (_shaderInstanceCache.TryGetValue(material.Guid, out ShaderBase cachedInstance))
            {
                return cachedInstance;
            }

            try
            {
                var instance = CreateShaderRepresentationInstance(gl, material.ShaderRepresentationTypeName);
                if (instance == null)
                {
                    return null;
                }
                ApplyUniformValues(instance, material.UniformValues);
                ApplyTextures(gl, instance, material.TextureReferences);
                _shaderInstanceCache[material.Guid] = instance;
                _glShaderMap[instance] = gl;
                return instance;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка создания экземпляра материала: {ex.Message}");
                return null;
            }
        }

        public virtual ShaderBase CreateMaterialInstance(GL gl, string materialPath)
        {
            try
            {
                MaterialAsset material = ServiceHub.Get<MaterialCacher>().LoadMaterial(materialPath);
                if (material == null)
                {
                    DebLogger.Error($"Не удалось загрузить материал из пути: {materialPath}");
                    return null;
                }

                return CreateMaterialInstance(gl, material);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка создания экземпляра материала из пути: {ex.Message}");
                return null;
            }
        }

        public virtual ShaderBase CreateMaterialInstanceFromGuid(GL gl, string materialGuid)
        {
            try
            {
                string materialPath = ServiceHub.Get<MetadataManager>().GetPathByGuid(materialGuid);
                if (string.IsNullOrEmpty(materialPath))
                {
                    DebLogger.Error($"Материал не найден для GUID: {materialGuid}");
                    return null;
                }

                return CreateMaterialInstance(gl, materialPath);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка создания экземпляра материала из GUID: {ex.Message}");
                return null;
            }
        }

        protected virtual ShaderBase CreateShaderRepresentationInstance(GL gl, string typeName)
        {
            try
            {
                Type representationType = FindShaderRepresentationType(typeName);
                if (representationType == null)
                {
                    DebLogger.Error($"Тип шейдерного представления не найден: {typeName}");
                    return null;
                }

                if (!typeof(ShaderBase).IsAssignableFrom(representationType))
                {
                    DebLogger.Error($"Тип {typeName} не является допустимым шейдерным представлением (должен наследовать от Mat)");
                    return null;
                }

                var constructor = representationType.GetConstructor(new[] { typeof(GL) });
                if (constructor == null)
                {
                    DebLogger.Error($"Тип шейдерного представления {typeName} не имеет конструктора, принимающего параметр GL");
                    return null;
                }
                return (ShaderBase)constructor.Invoke(new object[] { gl });
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка создания экземпляра шейдерного представления: {ex.Message}");
                return null;
            }
        }

        protected virtual Type FindShaderRepresentationType(string typeName)
        {
            Type type = _assemblyManager?.FindType(typeName, true);
            if (type != null)
            {
                return type;
            }
            return null;
        }

        public virtual void ApplyUniformValues(string materialGuid, Dictionary<string, object> uniformValues)
        {
            throw new NotImplementedException();
        }

        protected virtual void ApplyUniformValues(ShaderBase instance, Dictionary<string, object> uniformValues)
        {
            if (uniformValues == null || uniformValues.Count == 0)
            {
                return;
            }

            Type instanceType = instance.GetType();

            foreach (var kvp in uniformValues)
            {
                string propertyName = kvp.Key;
                object value = kvp.Value;

                if (value == null)
                {
                    continue;
                }

                try
                {
                    PropertyInfo property = instanceType.GetProperty(propertyName);
                    if (property != null && property.CanWrite)
                    {
                        object convertedValue = ConvertValueToTargetType(value, property.PropertyType);
                        Type type = convertedValue.GetType();
                        instance.Use();
                        property.SetValue(instance, convertedValue);
                    }
                }
                catch (Exception ex)
                {
                    DebLogger.Warn($"Не удалось установить свойство {propertyName} для {instanceType.Name}: {ex.Message}");
                }
            }
        }

        protected virtual object ConvertValueToTargetType(object value, Type targetType)
        {
            if (value != null && targetType.IsAssignableFrom(value.GetType()))
                return value;

            if (value is System.Numerics.Vector2 vec2 && targetType == typeof(Silk.NET.Maths.Vector2D<float>))
                return vec2.ToSilk();

            if (value is System.Numerics.Vector3 vec3 && targetType == typeof(Silk.NET.Maths.Vector3D<float>))
                return vec3.ToSilk();

            if (value is System.Numerics.Vector4 vec4 && targetType == typeof(Silk.NET.Maths.Vector4D<float>))
                return vec4.ToSilk();

            if (value is Newtonsoft.Json.Linq.JObject jObject)
            {
                if (targetType == typeof(System.Numerics.Vector2))
                {
                    float x = jObject["X"]?.ToObject<float>() ?? 0f;
                    float y = jObject["Y"]?.ToObject<float>() ?? 0f;
                    return new Silk.NET.Maths.Vector2D<float>(x, y);
                }
                else if (targetType == typeof(System.Numerics.Vector3))
                {
                    float x = jObject["X"]?.ToObject<float>() ?? 0f;
                    float y = jObject["Y"]?.ToObject<float>() ?? 0f;
                    float z = jObject["Z"]?.ToObject<float>() ?? 0f;
                    return new Silk.NET.Maths.Vector3D<float>(x, y, z);
                }
                else if (targetType == typeof(System.Numerics.Vector4))
                {
                    float x = jObject["X"]?.ToObject<float>() ?? 0f;
                    float y = jObject["Y"]?.ToObject<float>() ?? 0f;
                    float z = jObject["Z"]?.ToObject<float>() ?? 0f;
                    float w = jObject["W"]?.ToObject<float>() ?? 0f;
                    return new Silk.NET.Maths.Vector4D<float>(x, y, z, w);
                }
                else if (targetType == typeof(Silk.NET.Maths.Vector2D<float>))
                {
                    float x = jObject["X"]?.ToObject<float>() ?? 0f;
                    float y = jObject["Y"]?.ToObject<float>() ?? 0f;
                    return new Silk.NET.Maths.Vector2D<float>(x, y);
                }
                else if (targetType == typeof(Silk.NET.Maths.Vector3D<float>))
                {
                    float x = jObject["X"]?.ToObject<float>() ?? 0f;
                    float y = jObject["Y"]?.ToObject<float>() ?? 0f;
                    float z = jObject["Z"]?.ToObject<float>() ?? 0f;
                    return new Silk.NET.Maths.Vector3D<float>(x, y, z);
                }
                else if (targetType == typeof(Silk.NET.Maths.Vector4D<float>))
                {
                    float x = jObject["X"]?.ToObject<float>() ?? 0f;
                    float y = jObject["Y"]?.ToObject<float>() ?? 0f;
                    float z = jObject["Z"]?.ToObject<float>() ?? 0f;
                    float w = jObject["W"]?.ToObject<float>() ?? 0f;
                    return new Silk.NET.Maths.Vector4D<float>(x, y, z, w);
                }
                else
                {
                    return jObject.ToObject(targetType);
                }
            }

            if (targetType.IsPrimitive || targetType == typeof(string))
            {
                return Convert.ChangeType(value, targetType);
            }

            if (targetType.IsEnum)
            {
                if (value is string strValue)
                {
                    return Enum.Parse(targetType, strValue);
                }
                else
                {
                    return Enum.ToObject(targetType, value);
                }
            }

            return value;
        }

        public virtual void ApplyTextures(string materialGuid, Dictionary<string, string> textureReferences)
        {
            throw new NotImplementedException();
        }
        public virtual void ApplyTextures(GL gl, ShaderBase instance, Dictionary<string, string> textureReferences)
        {
            if (textureReferences == null || textureReferences.Count == 0)
            {
                return;
            }

            Type instanceType = instance.GetType();

            foreach (var kvp in textureReferences)
            {
                string textureName = kvp.Key;
                string textureGuid = kvp.Value;

                if (string.IsNullOrEmpty(textureGuid))
                {
                    continue;
                }

                try
                {
                    string methodName = $"{textureName}_SetTexture";
                    MethodInfo method = instanceType.GetMethod(methodName);

                    if (method != null)
                    {
                        OpenglLib.Texture texture = _textureFactory.CreateTextureFromGuid(gl, textureGuid);

                        if (texture != null)
                        {
                            instance.Use();
                            gl.ActiveTexture(TextureUnit.Texture0);
                            method.Invoke(instance, new object[] { texture });
                            DebLogger.Debug($"Применена текстура {textureName} к материалу {instanceType.Name}");
                        }
                    }
                    else
                    {
                        DebLogger.Warn($"Метод установки текстуры {methodName} не найден в типе {instanceType.Name}");
                    }
                }
                catch (Exception ex)
                {
                    DebLogger.Warn($"Не удалось установить текстуру {textureName} для {instanceType.Name}: {ex.Message}");
                }
            }
        }


        public virtual void ClearCache()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            foreach (var kvp in _shaderInstanceCache)
            {
                kvp.Value.Dispose();
            }
            _shaderInstanceCache.Clear();
            _glShaderMap.Clear();
        }
    }
}
