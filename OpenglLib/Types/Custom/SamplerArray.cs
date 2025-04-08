using Silk.NET.OpenGL;

namespace OpenglLib
{
    public class SamplerArray
    {
        private readonly GL _gl;
        private readonly int _size;
        private readonly Texture[] _textures;
        private readonly Shader _shader;
        public bool IsDirty { get; set; } = true;

        public int Location { get; set; }

        public SamplerArray(int size) {
            _textures = new Texture[size];
        }

        public SamplerArray(GL gl, int size, Shader shader)
        {
            _gl = gl;
            _size = size;
            _shader = shader;
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

                if (_gl == null &&  Location == -1)
                {
                    _textures[index] = value;
                }
            }
        }

        private void SetTextureAtIndex(int index, Texture texture)
        {
            _shader?.SetTexture(Location + index, texture);
        }

        public void Apply()
        {
            if (!IsDirty || _gl == null || Location == -1)
                return;

            for (int i = 0; i < _size; i++)
            {
                if (_textures[i] != null)
                {
                    SetTextureAtIndex(i, _textures[i]);
                }
            }

            IsDirty = false;
        }
        public void Clear()
        {
            for (int i = 0; i < _size; i++)
            {
                _textures[i] = null;
            }
            IsDirty = true;
        }
    }
}
