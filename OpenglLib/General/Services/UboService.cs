using EngineLib;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using OpenglLib.Buffers;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    public class UboService : IService
    {
        private readonly Dictionary<string, AutoUBO> _ubosByName = new Dictionary<string, AutoUBO>();
        private readonly Dictionary<uint, AutoUBO> _ubosByBindingPoint = new Dictionary<uint, AutoUBO>();
        private readonly object _lock = new object();
        private GL _gl;
        private BindingPointService _bindingPointService;

        public Task InitializeAsync()
        {
            _bindingPointService = ServiceHub.Get<BindingPointService>();
            return Task.CompletedTask;
        }

        public void SetGL(GL gl)
        {
            _gl = gl;
        }

        public AutoUBO GetOrCreateUbo(UniformBlockData blockData)
        {
            lock (_lock)
            {
                if (_ubosByName.TryGetValue(blockData.Name, out var existingUbo))
                {
                    return existingUbo;
                }

                uint bindingPoint;
                if (blockData.BindingPoint.HasValue)
                {
                    bindingPoint = (uint)blockData.BindingPoint.Value;

                    if (_ubosByBindingPoint.ContainsKey(bindingPoint))
                    {
                        throw new InvalidOperationError(
                            $"UBO с binding point {bindingPoint} уже существует: {_ubosByBindingPoint[bindingPoint].GetBlockName()}");
                    }

                    try
                    {
                        _bindingPointService.AllocateGlobalBindingPoint(bindingPoint);
                    }
                    catch (InvalidOperationError ex)
                    {
                        throw new InvalidOperationError($"Не удалось зарезервировать точку привязки {bindingPoint}: {ex.Message}");
                    }
                }
                else
                {
                    try
                    {
                        bindingPoint = _bindingPointService.AllocateGlobalBindingPoint();
                    }
                    catch (InvalidOperationError ex)
                    {
                        throw new InvalidOperationError($"Не удалось выделить глобальную точку привязки: {ex.Message}");
                    }

                    blockData = new UniformBlockData(
                        blockData.Name, 
                        blockData.BlockIndex, 
                        blockData.BlockSize, 
                        blockData.ActiveUniforms, 
                        blockData.Members, 
                        (int)bindingPoint
                        );
                }

                AutoUBO ubo = new AutoUBO(_gl, blockData);
                RegisterUbo(ubo);
                return ubo;
            }
        }

        public AutoUBO GetOrCreateUbo(string name, uint bindingPoint, int blockSize)
        {
            lock (_lock)
            {
                if (_ubosByName.TryGetValue(name, out var existingUbo))
                {
                    return existingUbo;
                }

                if (_ubosByBindingPoint.TryGetValue(bindingPoint, out var conflictingUbo))
                {
                    throw new InvalidOperationError(
                        $"UBO с binding point {bindingPoint} уже существует: {conflictingUbo.GetBlockName()}");
                }

                AutoUBO ubo = new AutoUBO(_gl, name, bindingPoint, blockSize);
                RegisterUbo(ubo);
                return ubo;
            }
        }

        public AutoUBO GetOrCreateUbo<T>() where T : struct, IUboStruct
        {
            T structInstance = default;
            string name = structInstance.BlockName;
            uint bindingPoint = structInstance.BindingPoint;
            int size = Marshal.SizeOf<T>();

            return GetOrCreateUbo(name, bindingPoint, size);
        }

        public AutoUBO GetExistingUboByName(string name)
        {
            lock (_lock)
            {
                if (_ubosByName.TryGetValue(name, out var ubo))
                {
                    return ubo;
                }
                return null;
            }
        }

        public AutoUBO GetExistingUboByBindingPoint(uint bindingPoint)
        {
            lock (_lock)
            {
                if (_ubosByBindingPoint.TryGetValue(bindingPoint, out var ubo))
                {
                    return ubo;
                }
                return null;
            }
        }

        public bool HasUboByName(string name)
        {
            lock (_lock)
            {
                return _ubosByName.ContainsKey(name);
            }
        }

        public bool HasUboByBindingPoint(uint bindingPoint)
        {
            lock (_lock)
            {
                return _ubosByBindingPoint.ContainsKey(bindingPoint);
            }
        }

        public void SetUboDataByName<T>(string name, T data) where T : struct
        {
            lock (_lock)
            {
                if (!_ubosByName.TryGetValue(name, out var ubo))
                {
                    throw new InvalidOperationError($"UBO с именем {name} не найден");
                }

                SetData(ubo, data);
            }
        }

        public void SetUboDataByBindingPoint<T>(uint bindingPoint, T data) where T : struct
        {
            lock (_lock)
            {
                if (!_ubosByBindingPoint.TryGetValue(bindingPoint, out var ubo))
                {
                    throw new InvalidOperationError($"UBO with binding point {bindingPoint} not found");
                }

                SetData(ubo, data);
            }
        }

        public void SetUboDataByBindingPoint(uint bindingPoint, Dictionary<string, object> data)
        {
            lock (_lock)
            {
                if (!_ubosByBindingPoint.TryGetValue(bindingPoint, out var ubo))
                {
                    throw new InvalidOperationError($"UBO with binding point {bindingPoint} not found");
                }
                ubo.SetUniforms(data);
                ubo.Update();
            }
        }

        private void SetData<T>(AutoUBO ubo, T data) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            if (size > ubo.GetBlockSize())
            {
                throw new InvalidOperationError($"Размер данных ({size}) превышает размер UBO ({ubo.GetBlockSize()})");
            }

            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(data, ptr, false);
                unsafe
                {
                    byte* ptrByte = (byte*)ptr.ToPointer();
                    for (int i = 0; i < size; i++)
                    {
                        ubo.SetRawByte(i, *(ptrByte + i));
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            ubo.Update();
        }

        public void SetUniform(string blockName, string uniformName, object value)
        {
            lock (_lock)
            {
                if (!_ubosByName.TryGetValue(blockName, out var ubo))
                {
                    throw new InvalidOperationError($"UBO with name {blockName} not found");
                }

                ubo.SetUniform(uniformName, value);
            }
        }

        public void SetUniform(uint bindingPoint, string uniformName, object value)
        {
            lock (_lock)
            {
                if (!_ubosByBindingPoint.TryGetValue(bindingPoint, out var ubo))
                {
                    throw new InvalidOperationError($"UBO with binding point {bindingPoint} not found");
                }

                ubo.SetUniform(uniformName, value);
            }
        }

        public void Update(uint bindingPoint)
        {
            lock (_lock)
            {
                if (!_ubosByBindingPoint.TryGetValue(bindingPoint, out var ubo))
                {
                    throw new InvalidOperationError($"UBO with binding point {bindingPoint} not found");
                }

                ubo.Update();
            }
        }
        public void UpdateAll()
        {
            lock (_lock)
            {
                foreach (var ubo in _ubosByName.Values)
                {
                    if (ubo.IsDirty)
                    {
                        ubo.Update();
                    }
                }
            }
        }

        private void RegisterUbo(AutoUBO ubo)
        {
            _ubosByName[ubo.GetBlockName()] = ubo;
            _ubosByBindingPoint[ubo.GetBindingPoint()] = ubo;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var ubo in _ubosByName.Values)
                {
                    ubo.Dispose();
                }
                _ubosByName.Clear();
                _ubosByBindingPoint.Clear();
                _gl = null;
            }
        }

        public bool TryGetValue(uint bindingPoint, string uniformName, out object value)
        {
            if (!_ubosByBindingPoint.TryGetValue(bindingPoint, out var ubo))
            {
                throw new InvalidOperationError($"UBO with binding point {bindingPoint} not found");
            }

            return ubo.TryGetValue(uniformName, out value);
        }
    }

    public interface IUboStruct
    {
        uint BindingPoint { get; }
        string BlockName { get; }
    }
}
