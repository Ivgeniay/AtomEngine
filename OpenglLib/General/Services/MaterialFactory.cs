using AtomEngine.RenderEntity;
using Silk.NET.OpenGL;
using AtomEngine;
using EngineLib;

namespace OpenglLib
{

    public class MaterialFactory : IService, IDisposable
    {
        protected List<Material> _materials = new List<Material>();
        protected TextureFactory _textureFactory;
        protected AssemblyManager _assemblyManager;
        protected MaterialAssetManager _materialAssetManager;
        protected MetadataManager _metadataManager;

        public virtual Task InitializeAsync()
        {
            _textureFactory = ServiceHub.Get<TextureFactory>();
            _assemblyManager = ServiceHub.Get<AssemblyManager>();
            _materialAssetManager = ServiceHub.Get<MaterialAssetManager>();
            _metadataManager = ServiceHub.Get<MetadataManager>();

            return Task.CompletedTask;
        }


        public virtual ShaderBase GetShaderFromMaterialAsset(GL gl, MaterialAsset materialAsset)
        {
            try
            {
                Material? material = _materials.FirstOrDefault(e =>
                        e.MaterialAsset.Guid == materialAsset.Guid &&
                        e.GLContext == gl
                        );

                if (material != null) return material.Shader;
                return GetMaterialInstanceFromAsset(gl, materialAsset).Shader;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка загрузки шейдера {nameof(MaterialFactory)} {nameof(GetShaderFromMaterialAsset)}");
                return null;
            }
        }

        public virtual ShaderBase GetShaderFormMaterialAssetPath(GL gl, string materialAssetPath)
        {
            try
            {
                if (_materialAssetManager == null) _materialAssetManager = ServiceHub.Get<MaterialAssetManager>();
                MaterialAsset materialAsset = _materialAssetManager.GetMaterialAssetByPath(materialAssetPath);
                if (materialAsset == null)
                {
                    DebLogger.Error($"Не удалось загрузить material asset из пути: {materialAssetPath}");
                    return null;
                }

                Material? material = _materials.FirstOrDefault(e =>
                        e.MaterialAsset.Guid == materialAsset.Guid &&
                        e.GLContext == gl
                        );

                if (material != null) return material.Shader;
                return GetMaterialInstanceFromAssetPath(gl, materialAssetPath).Shader;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка загрузки шейдера {nameof(MaterialFactory)} {nameof(GetShaderFormMaterialAssetPath)}");
                return null;
            }

        }

        public virtual ShaderBase GetShaderFormMaterialAssetGUID(GL gl, string materislGuid)
        {
            try
            {
                Material? material = _materials.FirstOrDefault(e =>
                        e.MaterialAsset.Guid == materislGuid &&
                        e.GLContext == gl
                        );

                if (material != null) return material.Shader;
                return GetMaterialInstanceFromAssetGuid(gl, materislGuid).Shader;
            }
            catch(Exception e)
            {
                DebLogger.Error($"Неудачная попытка вернуть шейдер из {nameof(MaterialFactory)} {nameof(GetShaderFormMaterialAssetGUID)}");
                return null;
            }
        }


        public virtual Material GetMaterialInstanceFromAssetPath(GL gl, string materialPath)
        {
            try
            {
                if (_materialAssetManager == null) _materialAssetManager = ServiceHub.Get<MaterialAssetManager>();
                MaterialAsset material = _materialAssetManager.GetMaterialAssetByPath(materialPath);
                if (material == null)
                {
                    DebLogger.Error($"Не удалось загрузить material asset из пути: {materialPath}");
                    return null;
                }

                return GetMaterialInstanceFromAsset(gl, material);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка создания экземпляра материала из пути: {ex.Message}");
                return null;
            }
        }

        public virtual Material GetMaterialInstanceFromAssetGuid(GL gl, string materialAssetGuid)
        {
            try
            {
                string materialPath = ServiceHub.Get<MetadataManager>().GetPathByGuid(materialAssetGuid);
                if (string.IsNullOrEmpty(materialPath))
                {
                    DebLogger.Error($"Material Asset не найден для GUID: {materialAssetGuid}");
                    return null;
                }

                return GetMaterialInstanceFromAssetPath(gl, materialPath);
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка создания экземпляра материала из GUID: {ex.Message}");
                return null;
            }
        }

        public virtual Material GetMaterialInstanceFromAsset(GL gl, MaterialAsset materialAsset)
        {
            if (materialAsset == null)
            {
                DebLogger.Error("MaterialAsset is null");
                return null;
            }

            Material? material = _materials.FirstOrDefault(e => 
                    e.MaterialAsset.Guid == materialAsset.Guid &&
                    e.GLContext == gl
                    );

            if (material != null)
            {
                return material;
            }

            try
            {
                if (!materialAsset.HasValidShader)
                {
                    DebLogger.Warn($"Material {materialAsset} doesn't have a valid shader assigned.");
                    return null;
                }

                OpenglLib.Shader instance = null;
                if (string.IsNullOrWhiteSpace(materialAsset.ShaderRepresentationTypeName))
                {
                    instance = CreateShaderInstance(gl, materialAsset);
                }
                else
                {
                    instance = CreateShaderRepresentationInstance(gl, materialAsset.ShaderRepresentationTypeName);
                }


                if (instance == null)
                {
                    return null;
                }
                material = new Material(gl, instance, materialAsset);
                material.Factory = this;
                _materials.Add(material);

                var containers = materialAsset.GetAllContainers();
                Dictionary<string, object> forUbos = new Dictionary<string, object>();
                foreach (var container in containers)
                {
                    if (container is MaterialUniformDataContainer materialUniformDataContainer)
                    {
                        if (
                            !string.IsNullOrWhiteSpace(materialUniformDataContainer.Name) &&
                            materialUniformDataContainer.Value != null
                            )
                            material.SetUniform(materialUniformDataContainer.Name, materialUniformDataContainer.Value);
                    }
                    else if (container is MaterialSamplerDataContainer materialSamplerDataContainer)
                    {
                        if (
                            !string.IsNullOrWhiteSpace(materialSamplerDataContainer.Name) &&
                            !string.IsNullOrWhiteSpace(materialSamplerDataContainer.TextureGuid)
                            )
                        {
                            SetTexture(material, materialSamplerDataContainer.Name, materialSamplerDataContainer.TextureGuid);
                        }
                    }
                }

                return material;
            }
            catch (Exception ex)
            {
                DebLogger.Error($"Ошибка создания экземпляра материала: {ex.Message}");
                return null;
            }
        }

        protected IEnumerable<Material> GetMaterialsFrom(string materialAssetGuid)
        {
            foreach(var material in _materials)
            {
                if (material.IsValid && material.MaterialAsset.Guid == materialAssetGuid)
                    yield return material;
            }
        }


        protected virtual Shader CreateShaderInstance(GL gl, MaterialAsset materialAsset)
        {
            try
            {
                var shaderPath = _metadataManager.GetPathByGuid(materialAsset.ShaderGuid);
                var shaderModel = GlslExtractor.ExtractShaderModel(shaderPath);
                Shader shader = new Shader(gl);
                shader.SetUpShader(shaderModel.Vertex.FullText, shaderModel.Fragment.FullText);

                return shader;
            }
            catch
            {
                DebLogger.Error("Error creation shader instance");
                return null;
            }

        }

        protected virtual Shader CreateShaderRepresentationInstance(GL gl, string typeName)
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
                return (Shader)constructor.Invoke(new object[] { gl });
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

        public virtual void SetUniformValues(string materialAssetGuid, Dictionary<string, object> uniformValues)
        {
            IEnumerable<Material> materials = _materials.Where(e => e.IsValid && e.MaterialAsset.Guid == materialAssetGuid);
            if (materials.Count() == 0)
            {
                DebLogger.Info("Не найдено ни одного действующего материла для изменения свойств");
                return;
            }

            foreach (var material in materials)
            {
                SetUniformValues(material.Shader, uniformValues);
            }
        }
        public virtual void SetUniformValues(Shader instance, Dictionary<string, object> uniformValues)
        {
            if (uniformValues == null || uniformValues.Count == 0)
            {
                return;
            }

            var materials = _materials.Where(e => e.Shader == instance);
            foreach (var material in materials)
            {
                foreach (var kvp in uniformValues)
                {
                    string propertyName = kvp.Key;
                    object value = kvp.Value;

                    SetUniformValue(material, propertyName, value);
                }
            }
        }
        public virtual void SetUniformValue(MaterialAsset materialAsset, string name, object value)
        {
            var materials = _materials.Where(e => e.MaterialAsset.Guid == materialAsset.Guid);
            foreach(var material in materials)
            {
                SetUniformValue(material, name, value);
            }
        }
        public virtual void SetUniformValue(string materialAssetGuid, string name, object value)
        {
            IEnumerable<Material> materials = _materials.Where(e => e.MaterialAsset.Guid == materialAssetGuid);
            if (materials.Count() == 0)
            {
                return;
            }

            foreach (var material in materials)
            {
                SetUniformValue(material, name, value);
            }
        }
        public virtual void SetUniformValue(Material material, string name, object value)
        {
            if (value == null || string.IsNullOrWhiteSpace(name) || material == null) return;
            Type shaderType = material.Shader.GetType();

            try
            {
                material.Use();
                material.SetUniform(name, value);
            }
            catch (Exception ex)
            {
                DebLogger.Warn($"Не удалось установить свойство {name} для {shaderType.Name}: {ex.Message}");
            }
        }


        public virtual void SetTextures(string materialAssetGuid, Dictionary<string, string> textureReferences)
        {
            if (textureReferences == null || textureReferences.Count == 0)
            {
                return;
            }
            IEnumerable<Material> materials = _materials.Where(e => e.MaterialAsset.Guid == materialAssetGuid);
            if (materials.Count() == 0)
            {
                return;
            }

            foreach (Material material in materials)
            {
                foreach (var kvp in textureReferences)
                {
                    SetTexture(
                        material: material, 
                        samplerName: kvp.Key, 
                        textureGuid: kvp.Value);
                }
            }
        }
        public virtual void SetTextures(Material material, Dictionary<string, string> textureReferences) =>
            SetTextures(material.MaterialAsset.Guid, textureReferences);
        public virtual void SetTexture(MaterialAsset materialAsset, string samplerName, string textureGuid)
        {
            var materials = _materials.Where(e => e.MaterialAsset.Guid == materialAsset.Guid);
            foreach (var material in materials)
            {
                SetTexture(material, samplerName, textureGuid);
            }
        }
        public virtual void SetTexture(string materialAssetGuid, string samplerName, string textureGuid)
        {
            IEnumerable<Material> materials = _materials.Where(e => e.MaterialAsset.Guid == materialAssetGuid);
            if (materials.Count() == 0)
            {
                return;
            }

            foreach (var material in materials)
            {
                SetTexture(material, samplerName, textureGuid);
            }
        }
        public virtual void SetTexture(Material material, string samplerName, string textureGuid)
        {
            if (string.IsNullOrWhiteSpace(textureGuid) || string.IsNullOrWhiteSpace(samplerName) || material == null) return;

            try
            {
                if (material.IsValid)
                {
                    Texture texture = _textureFactory.CreateTextureFromGuid(material.GLContext, textureGuid, material);
                    material.Use();
                    material.SetTexture(samplerName, texture);
                }
            }
            catch (Exception ex)
            {
                DebLogger.Error("Setting texture value error");
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


        public virtual void ClearCache()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            _materials.ForEach(e => e.Dispose());
            _materials.Clear();
        }
    }


}
