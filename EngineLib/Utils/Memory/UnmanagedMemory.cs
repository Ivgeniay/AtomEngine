using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EngineLib.Memory
{
    /// <summary>
    /// Представляет блок неуправляемой памяти с безопасным управлением жизненным циклом
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public unsafe sealed class UnmanagedMemory<T> : IDisposable where T : unmanaged
    {
        private readonly void* _ptr;
        private readonly int _length;
        private readonly int _elementSize;
        private bool _isDisposed;

        public int Length => _length;
        public int Size => _length * _elementSize;

        public UnmanagedMemory(int length)
        {
            if (length <= 0)
                throw new ArgumentException("Length must be positive", nameof(length));

            _length = length;
            _elementSize = sizeof(T);
            _ptr = NativeMemory.Alloc((nuint)(length * _elementSize));

            if (_ptr == null)
                throw new OutOfMemoryException();
        }

        public ref T this[int index]
        {
            get
            {
                ObjectDisposedException.ThrowIf(_isDisposed, nameof(UnmanagedMemory<T>));

                if ((uint)index >= (uint)_length)
                    throw new IndexOutOfRangeException();

                return ref Unsafe.AsRef<T>((byte*)_ptr + index * _elementSize);
            }
        }

        public void* GetUnsafePtr()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, nameof(UnmanagedMemory<T>));
            return _ptr;
        }

        public Span<T> AsSpan()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, nameof(UnmanagedMemory<T>));
            return new Span<T>(_ptr, _length);
        }

        public void CopyFrom(ReadOnlySpan<T> source)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, nameof(UnmanagedMemory<T>));

            if (source.Length > _length)
                throw new ArgumentException("Source is too large", nameof(source));

            source.CopyTo(AsSpan());
        }

        public void CopyTo(Span<T> destination)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, nameof(UnmanagedMemory<T>));

            if (destination.Length < _length)
                throw new ArgumentException("Destination is too small", nameof(destination));

            AsSpan().CopyTo(destination);
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            NativeMemory.Free(_ptr);
            _isDisposed = true;

            GC.SuppressFinalize(this);
        }

        ~UnmanagedMemory()
        {
            Dispose();
        }
    }

    /// <summary>
    /// Вспомогательный класс для управления блоками памяти
    /// </summary>
    public unsafe sealed class MemoryBlock : IDisposable
    {
        private readonly void* _ptr;
        private readonly int _size;
        private bool _isDisposed;

        public int Size => _size;

        public unsafe MemoryBlock(int size)
        {
            if (size <= 0)
                throw new ArgumentException("Size must be positive", nameof(size));

            _size = size;
            _ptr = NativeMemory.Alloc((nuint)size);

            if (_ptr == null)
                throw new OutOfMemoryException();
        }

        public unsafe void* GetPointer()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, nameof(MemoryBlock));
            return _ptr;
        }

        public unsafe Span<byte> AsSpan()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, nameof(MemoryBlock));
            return new Span<byte>(_ptr, _size);
        }

        public unsafe void Dispose()
        {
            if (_isDisposed) return;

            NativeMemory.Free(_ptr);
            _isDisposed = true;

            GC.SuppressFinalize(this);
        }

        ~MemoryBlock()
        {
            Dispose();
        }
    }
}