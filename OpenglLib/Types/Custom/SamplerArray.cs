using Silk.NET.OpenGL;

namespace OpenglLib
{
    public class SamplerArray
    {
        private readonly GL _gl;
        private readonly int _size;
        private readonly int _location;
        private readonly string _target;

        public int Location { get; set; } = -1;

        public SamplerArray(GL gl, int size, string target)
        {
            _gl = gl;
            _size = size;
            _target = target;
        }

        public void SetTexture(int index, Texture texture, int unitOffset = 0)
        {
            if (index < 0 || index >= _size)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range for SamplerArray of size {_size}");
            }

            if (Location == -1)
            {
                return;
            }

            TextureUnit unit = TextureUnit.Texture0 + index + unitOffset;
            TextureTarget target = Enum.Parse<TextureTarget>(_target);

            texture.Target = target;
            texture.Bind(unit);

            _gl.Uniform1(Location + index, index + unitOffset);
        }
    }
}
