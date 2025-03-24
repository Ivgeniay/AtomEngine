using EngineLib;

namespace OpenglLib
{
    public class BindingPointService : IService
    {
        private readonly Dictionary<uint, HashSet<uint>> _programBindingPools = new Dictionary<uint, HashSet<uint>>();
        private const uint MaxBindingPoints = 72;

        public void AllocateBindingPoint(uint programHandle, uint bindingPoint)
        {
            if (!_programBindingPools.TryGetValue(programHandle, out var bindingPool))
            {
                bindingPool = new HashSet<uint>();
                for (uint i = 0; i < MaxBindingPoints; i++)
                {
                    bindingPool.Add(i);
                }
                _programBindingPools[programHandle] = bindingPool;
            }

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
                throw new InvalidOperationError($"{bindingPoint} has already used.");
            }

        }

        public uint AllocateBindingPoint(uint programHandle)
        {
            if (!_programBindingPools.TryGetValue(programHandle, out var bindingPool))
            {
                bindingPool = new HashSet<uint>();
                for (uint i = 0; i < MaxBindingPoints; i++)
                {
                    bindingPool.Add(i);
                }
                _programBindingPools[programHandle] = bindingPool;
            }

            if (bindingPool.Count == 0)
            {
                throw new InvalidOperationError("No available binding points for the program.");
            }

            uint bindingPoint = bindingPool.First();
            bindingPool.Remove(bindingPoint);

            return bindingPoint;
        }


        public void ReleaseBindingPoint(uint programHandle, uint bindingPoint)
        {
            if (_programBindingPools.TryGetValue(programHandle, out var bindingPool))
            {
                bindingPool.Add(bindingPoint);
            }
        }

        public void ReleaseAllBindingPoints(uint programHandle)
        {
            if (_programBindingPools.TryGetValue(programHandle, out var bindingPool))
            {
                bindingPool.Clear();
                for (uint i = 0; i < MaxBindingPoints; i++)
                {
                    bindingPool.Add(i);
                }
            }
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
