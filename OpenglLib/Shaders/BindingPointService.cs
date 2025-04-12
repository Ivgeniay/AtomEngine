using EngineLib;
using Silk.NET.OpenGL;

namespace OpenglLib
{
    public class BindingPointService : IService
    {
        private readonly Dictionary<uint, HashSet<uint>> _programBindingPools = new Dictionary<uint, HashSet<uint>>();
        private readonly HashSet<uint> _globallyReservedBindingPoints = new HashSet<uint>();
        private readonly object _lock = new object();
        private uint _maxBindingPoints = 32;


        private uint GetMaxBindingPoints(GL gl)
        {
            int maxUniformBindings = 0;
            gl.GetInteger(GLEnum.MaxUniformBufferBindings, out maxUniformBindings);
            return (uint)Math.Max(16, maxUniformBindings);
        }

        private HashSet<uint> GetOrCreateBindingPool(uint programHandle)
        {
            if (!_programBindingPools.TryGetValue(programHandle, out var bindingPool))
            {
                bindingPool = new HashSet<uint>();
                for (uint i = 0; i < _maxBindingPoints; i++)
                {
                    if (!_globallyReservedBindingPoints.Contains(i))
                    {
                        bindingPool.Add(i);
                    }
                }
                _programBindingPools[programHandle] = bindingPool;
            }
            return bindingPool;
        }

        public void ReserveBindingPointGlobally(uint bindingPoint)
        {
            lock (_lock)
            {
                if (bindingPoint >= _maxBindingPoints)
                {
                    throw new ArgumentOutOfRangeException(nameof(bindingPoint),
                        $"Binding point must be less than {_maxBindingPoints}");
                }

                _globallyReservedBindingPoints.Add(bindingPoint);

                foreach (var pool in _programBindingPools.Values)
                {
                    pool.Remove(bindingPoint);
                }
            }
        }

        public void ReleaseGlobalBindingPoint(uint bindingPoint)
        {
            lock (_lock)
            {
                if (_globallyReservedBindingPoints.Remove(bindingPoint))
                {
                    foreach (var pool in _programBindingPools.Values)
                    {
                        pool.Add(bindingPoint);
                    }
                }
            }
        }

        public IReadOnlySet<uint> GetGloballyReservedBindingPoints()
        {
            lock (_lock)
            {
                return new HashSet<uint>(_globallyReservedBindingPoints);
            }
        }

        public void AllocateBindingPoint(uint programHandle, uint bindingPoint)
        {
            lock (_lock)
            {
                if (bindingPoint >= _maxBindingPoints)
                {
                    throw new ArgumentOutOfRangeException(nameof(bindingPoint),
                        $"Binding point must be less than {_maxBindingPoints}");
                }

                if (_globallyReservedBindingPoints.Contains(bindingPoint))
                {
                    throw new InvalidOperationError($"Binding point {bindingPoint} is globally reserved.");
                }

                var bindingPool = GetOrCreateBindingPool(programHandle);

                if (bindingPool.Count == 0)
                {
                    throw new InvalidOperationError("No available binding points for the program.");
                }

                if (bindingPool.Contains(bindingPoint))
                {
                    bindingPool.Remove(bindingPoint);
                }
                else
                {
                    throw new InvalidOperationError($"Binding point {bindingPoint} has already been allocated for this program.");
                }
            }
        }

        public uint AllocateBindingPoint(uint programHandle)
        {
            lock (_lock)
            {
                var bindingPool = GetOrCreateBindingPool(programHandle);

                if (bindingPool.Count == 0)
                {
                    throw new InvalidOperationError("No available binding points for the program.");
                }

                // Берем точку привязки с наименьшим номером для эффективного использования
                uint bindingPoint = bindingPool.Min();
                bindingPool.Remove(bindingPoint);
                return bindingPoint;
            }
        }

        public uint[] AllocateBindingPointRange(uint programHandle, int count)
        {
            lock (_lock)
            {
                if (count <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
                }

                var bindingPool = GetOrCreateBindingPool(programHandle);

                if (bindingPool.Count < count)
                {
                    throw new InvalidOperationError($"Not enough available binding points. Requested: {count}, Available: {bindingPool.Count}");
                }

                var sortedPoints = bindingPool.OrderBy(p => p).ToArray();
                uint[] result = new uint[count];

                for (int i = 0; i <= sortedPoints.Length - count; i++)
                {
                    bool isSequential = true;
                    for (int j = 0; j < count - 1; j++)
                    {
                        if (sortedPoints[i + j + 1] != sortedPoints[i + j] + 1)
                        {
                            isSequential = false;
                            break;
                        }
                    }

                    if (isSequential)
                    {
                        for (int j = 0; j < count; j++)
                        {
                            result[j] = sortedPoints[i + j];
                            bindingPool.Remove(result[j]);
                        }
                        return result;
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    result[i] = sortedPoints[i];
                    bindingPool.Remove(result[i]);
                }

                return result;
            }
        }

        public void ReleaseBindingPoint(uint programHandle, uint bindingPoint)
        {
            lock (_lock)
            {
                if (_globallyReservedBindingPoints.Contains(bindingPoint))
                {
                    return;
                }

                if (_programBindingPools.TryGetValue(programHandle, out var bindingPool))
                {
                    bindingPool.Add(bindingPoint);
                }
            }
        }

        public void ReleaseAllBindingPoints(uint programHandle)
        {
            lock (_lock)
            {
                if (_programBindingPools.TryGetValue(programHandle, out var bindingPool))
                {
                    bindingPool.Clear();
                    for (uint i = 0; i < _maxBindingPoints; i++)
                    {
                        if (!_globallyReservedBindingPoints.Contains(i))
                        {
                            bindingPool.Add(i);
                        }
                    }
                }
            }
        }

        public void RemoveProgram(uint programHandle)
        {
            lock (_lock)
            {
                _programBindingPools.Remove(programHandle);
            }
        }

        public void UpdateMaxBindingPoints(GL _gl)
        {
            lock (_lock)
            {
                uint newMax = GetMaxBindingPoints(_gl);
                if (newMax < _maxBindingPoints)
                {
                    foreach (var pool in _programBindingPools.Values)
                    {
                        pool.RemoveWhere(p => p >= newMax);
                    }

                    _globallyReservedBindingPoints.RemoveWhere(p => p >= newMax);
                }
                else if (newMax > _maxBindingPoints)
                {
                    foreach (var pool in _programBindingPools.Values)
                    {
                        for (uint i = _maxBindingPoints; i < newMax; i++)
                        {
                            if (!_globallyReservedBindingPoints.Contains(i))
                            {
                                pool.Add(i);
                            }
                        }
                    }
                }

                _maxBindingPoints = newMax;
            }
        }

        public uint GetMaxBindingPointCount()
        {
            lock (_lock)
            {
                return _maxBindingPoints;
            }
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
    }

    //public class BindingPointService : IService
    //{
    //    private readonly Dictionary<uint, HashSet<uint>> _programBindingPools = new Dictionary<uint, HashSet<uint>>();
    //    private uint MaxBindingPoints = 30;

    //    public void SetMaxBindingPoints(uint maxBindinPoints) =>
    //        MaxBindingPoints = maxBindinPoints;

    //    public void AllocateBindingPoint(uint programHandle, uint bindingPoint)
    //    {
    //        if (!_programBindingPools.TryGetValue(programHandle, out var bindingPool))
    //        {
    //            bindingPool = new HashSet<uint>();
    //            for (uint i = 0; i < MaxBindingPoints; i++)
    //            {
    //                bindingPool.Add(i);
    //            }
    //            _programBindingPools[programHandle] = bindingPool;
    //        }

    //        if (bindingPool.Count == 0)
    //        {
    //            throw new InvalidOperationError("No available binding points for the program.");
    //        }
    //        if (bindingPool.Contains(bindingPoint))
    //        { 
    //            bindingPool.Remove(bindingPoint);
    //        }
    //        else
    //        {
    //            throw new InvalidOperationError($"{bindingPoint} has already used.");
    //        }

    //    }

    //    public uint AllocateBindingPoint(uint programHandle)
    //    {
    //        if (!_programBindingPools.TryGetValue(programHandle, out var bindingPool))
    //        {
    //            bindingPool = new HashSet<uint>();
    //            for (uint i = 0; i < MaxBindingPoints; i++)
    //            {
    //                bindingPool.Add(i);
    //            }
    //            _programBindingPools[programHandle] = bindingPool;
    //        }

    //        if (bindingPool.Count == 0)
    //        {
    //            throw new InvalidOperationError("No available binding points for the program.");
    //        }

    //        uint bindingPoint = bindingPool.First();
    //        bindingPool.Remove(bindingPoint);

    //        return bindingPoint;
    //    }


    //    public void ReleaseBindingPoint(uint programHandle, uint bindingPoint)
    //    {
    //        if (_programBindingPools.TryGetValue(programHandle, out var bindingPool))
    //        {
    //            bindingPool.Add(bindingPoint);
    //        }
    //    }

    //    public void ReleaseAllBindingPoints(uint programHandle)
    //    {
    //        if (_programBindingPools.TryGetValue(programHandle, out var bindingPool))
    //        {
    //            bindingPool.Clear();
    //            for (uint i = 0; i < MaxBindingPoints; i++)
    //            {
    //                bindingPool.Add(i);
    //            }
    //        }
    //    }

    //    public Task InitializeAsync()
    //    {
    //        return Task.CompletedTask;
    //    }
    //}

}
