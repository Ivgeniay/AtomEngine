using Silk.NET.OpenGL;

namespace OpenglLib
{
    public class SamplerArray
    {
        private readonly GL _gl;
        private readonly int _size;
        private readonly string _target;
        private readonly int _baseTextureUnit;
        private readonly Texture[] _textures;
        public bool IsDirty { get; set; } = true;

        public int Location { get; set; }

        public SamplerArray(int size) {
            _textures = new Texture[size];
        }

        public SamplerArray(GL gl, int size, string target, int baseTextureUnit)
        {
            _gl = gl;
            _size = size;
            _target = target;
            _baseTextureUnit = baseTextureUnit;
            _textures = new Texture[size];
        }

        public Texture this[int index]
        {
            get => _textures[index];
            set
            {
                if (index < 0 || index >= _size)
                    throw new IndexOutOfRangeException();


                if (_gl != null && Location != -1 && value != null)
                {
                    _textures[index] = value;
                    SetTextureAtIndex(index, value);
                    IsDirty = true;
                }
            }
        }

        private void SetTextureAtIndex(int index, Texture texture)
        {
            int unitIndex = _baseTextureUnit + index;
            TextureUnit unit = TextureUnit.Texture0 + unitIndex;
            TextureTarget target = Enum.Parse<TextureTarget>(_target);

            texture.Target = target;
            texture.Bind(unit);

            _gl.Uniform1(Location + index, unitIndex);
        }
    }
}
