using EngineLib.Memory;

namespace EngineLib.Tests
{
    public class UnmanagedMemoryTests
    {
        [Fact]
        public void Constructor_WithValidLength_CreatesInstance()
        {
            // Arrange & Act
            using var memory = new UnmanagedMemory<int>(100);

            // Assert
            Assert.Equal(100, memory.Length);
            Assert.Equal(400, memory.Size); // 4 bytes per int
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WithInvalidLength_ThrowsArgumentException(int length)
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new UnmanagedMemory<int>(length));
        }

        [Fact]
        public void IndexOperator_WithValidIndex_WorksCorrectly()
        {
            // Arrange
            using var memory = new UnmanagedMemory<int>(10);

            // Act
            memory[0] = 42;
            memory[9] = 100;

            // Assert
            Assert.Equal(42, memory[0]);
            Assert.Equal(100, memory[9]);
        }

        [Fact]
        public void IndexOperator_WithInvalidIndex_ThrowsIndexOutOfRangeException()
        {
            // Arrange
            using var memory = new UnmanagedMemory<int>(10);

            // Act & Assert
            Assert.Throws<IndexOutOfRangeException>(() => memory[10]);
            Assert.Throws<IndexOutOfRangeException>(() => memory[-1]);
        }

        [Fact]
        public void AsSpan_ReturnsValidSpan()
        {
            // Arrange
            using var memory = new UnmanagedMemory<int>(10);
            for (int i = 0; i < 10; i++)
            {
                memory[i] = i;
            }

            // Act
            var span = memory.AsSpan();

            // Assert
            Assert.Equal(10, span.Length);
            for (int i = 0; i < 10; i++)
            {
                Assert.Equal(i, span[i]);
            }
        }

        [Fact]
        public void CopyFrom_WithValidSource_CopiesData()
        {
            // Arrange
            using var memory = new UnmanagedMemory<int>(5);
            var source = new int[] { 1, 2, 3, 4, 5 };

            // Act
            memory.CopyFrom(source);

            // Assert
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(source[i], memory[i]);
            }
        }

        [Fact]
        public void CopyFrom_WithTooLargeSource_ThrowsArgumentException()
        {
            // Arrange
            using var memory = new UnmanagedMemory<int>(3);
            var source = new int[] { 1, 2, 3, 4, 5 };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => memory.CopyFrom(source));
        }

        [Fact]
        public void CopyTo_WithValidDestination_CopiesData()
        {
            // Arrange
            using var memory = new UnmanagedMemory<int>(5);
            for (int i = 0; i < 5; i++)
            {
                memory[i] = i + 1;
            }
            var destination = new int[5];

            // Act
            memory.CopyTo(destination);

            // Assert
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(i + 1, destination[i]);
            }
        }

        [Fact]
        public void CopyTo_WithTooSmallDestination_ThrowsArgumentException()
        {
            // Arrange
            using var memory = new UnmanagedMemory<int>(5);
            var destination = new int[3];

            // Act & Assert
            Assert.Throws<ArgumentException>(() => memory.CopyTo(destination));
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            // Arrange
            var memory = new UnmanagedMemory<int>(5);

            // Act
            memory.Dispose();

            // Assert
            var exception = Record.Exception(() => memory.Dispose());
            Assert.Null(exception);
        }

        [Fact]
        public unsafe void UseAfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var memory = new UnmanagedMemory<int>(5);
            memory.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => memory[0] = 1);
            Assert.Throws<ObjectDisposedException>(() => memory.AsSpan());
            Assert.Throws<ObjectDisposedException>(() => memory.GetUnsafePtr());
        }
    }

    public class MemoryBlockTests
    {
        [Fact]
        public void Constructor_WithValidSize_CreatesInstance()
        {
            // Arrange & Act
            using var block = new MemoryBlock(100);

            // Assert
            Assert.Equal(100, (int)block.Size);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Constructor_WithInvalidSize_ThrowsArgumentException(int size)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new MemoryBlock(size));
        }

        [Fact]
        public void AsSpan_ReturnsValidSpan()
        {
            // Arrange
            const int size = 100;
            using var block = new MemoryBlock(size);

            // Act
            var span = block.AsSpan();

            // Assert
            Assert.Equal(size, span.Length);
        }

        [Fact]
        public unsafe void MemoryContents_CanBeWrittenAndRead()
        {
            // Arrange
            using var block = new MemoryBlock(sizeof(int));
            var ptr = (int*)block.GetPointer();

            // Act
            *ptr = 42;

            // Assert
            Assert.Equal(42, *ptr);
        }

        [Fact]
        public unsafe void SpanOperation_WritesAndReadsCorrectly()
        {
            // Arrange
            using var block = new MemoryBlock(100);
            var span = block.AsSpan();

            // Act
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = (byte)i;
            }

            // Assert
            for (int i = 0; i < span.Length; i++)
            {
                Assert.Equal((byte)i, span[i]);
            }
        }

        [Fact]
        public unsafe void GetPointer_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var block = new MemoryBlock(100);
            block.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => block.GetPointer());
        }

        [Fact]
        public void AsSpan_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var block = new MemoryBlock(100);
            block.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => block.AsSpan());
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            // Arrange
            var block = new MemoryBlock(100);

            // Act
            block.Dispose();

            // Assert
            var exception = Record.Exception(() => block.Dispose());
            Assert.Null(exception);
        }
    }

    // Вспомогательные тесты для проверки работы с разными типами данных
    public class UnmanagedMemoryTypeTests
    {
        [Fact]
        public void WorksWithStructs()
        {
            // Arrange
            using var memory = new UnmanagedMemory<TestStruct>(10);
            var testStruct = new TestStruct { X = 1, Y = 2.0f };

            // Act
            memory[0] = testStruct;

            // Assert
            Assert.Equal(testStruct.X, memory[0].X);
            Assert.Equal(testStruct.Y, memory[0].Y);
        }

        [Fact]
        public void WorksWithDifferentPrimitiveTypes()
        {
            // Проверяем работу с разными примитивными типами
            TestType<byte>();
            TestType<short>();
            TestType<int>();
            TestType<long>();
            TestType<float>();
            TestType<double>();
        }

        private void TestType<T>() where T : unmanaged
        {
            // Arrange
            using var memory = new UnmanagedMemory<T>(1);
            T value = GetTestValue<T>();

            // Act
            memory[0] = value;

            // Assert
            Assert.Equal(value, memory[0]);
        }

        private T GetTestValue<T>() where T : unmanaged
        {
            if (typeof(T) == typeof(byte)) return (T)(object)(byte)42;
            if (typeof(T) == typeof(short)) return (T)(object)(short)42;
            if (typeof(T) == typeof(int)) return (T)(object)42;
            if (typeof(T) == typeof(long)) return (T)(object)42L;
            if (typeof(T) == typeof(float)) return (T)(object)42.0f;
            if (typeof(T) == typeof(double)) return (T)(object)42.0;
            throw new NotSupportedException($"Type {typeof(T)} not supported in test");
        }

        private struct TestStruct
        {
            public int X;
            public float Y;
        }
    }
}
