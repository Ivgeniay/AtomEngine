using Silk.NET.Maths;
using Silk.NET.OpenGL;
using AtomEngine;

namespace OpenglLib
{
    //
    public class SceneData : CustomStruct
    {
        public SceneData(Silk.NET.OpenGL.GL gl) : base(gl) {
            _materials  = new StructArray<MaterialData>(3, _gl);
            _lights  = new StructArray<LightData>(5, _gl);
            _directionData  = new LocaleArray<Vector3D<float>>(3, _gl);
        }


        private StructArray<MaterialData> _materials;
        public StructArray<MaterialData> materials
        {
            get
            {
                return _materials;
            }
        }


        private StructArray<LightData> _lights;
        public StructArray<LightData> lights
        {
            get
            {
                return _lights;
            }
        }


        public int directionDataLocation
        {
             get => directionData.Location;
             set => directionData.Location = value;
        }
        private LocaleArray<Vector3D<float>> _directionData;
        public LocaleArray<Vector3D<float>> directionData
        {
            get
            {
                return _directionData;
            }
        }


        public int ambientColorLocation { get ; set; } = -1;
        private Vector3D<float> _ambientColor;
        public Vector3D<float> ambientColor
        {
            set
            {
                if (ambientColorLocation == -1)
                {
                   DebLogger.Warn("You try to set value to -1 lcation field");
                   return;
                }
                _ambientColor = value;
                _gl.Uniform3(ambientColorLocation, value.X, value.Y, value.Z);
            }
        }


    }
}
