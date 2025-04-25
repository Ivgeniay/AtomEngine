using System.Runtime.InteropServices;
using System.Numerics;

namespace AtomEngine
{
    public partial struct LightComponent : IComponent
    {
        public Entity Owner { get; set; }
        public LightType Type;

        [DefaultVector3(1,1,1)]
        public Vector3 Color;
        [DefaultVector3(1,1,1)]
        public float Intensity;
        [DefaultFloat(1f)]
        public float Enabled;
        [DefaultBool(true)]
        public bool CastShadows;
        public int LightId;

        public Matrix4x4 LightSpaceMatrix;

        [DefaultFloat(10f)]
        public float Radius;
        [DefaultFloat(10f)]
        public float FalloffExponent;

        public bool IsDirty;
        public LightComponent(Entity entity)
        {
            Owner = entity;
            Color = new Vector3(1, 1, 1);
            Intensity = 10;
            Enabled = 1;
            CastShadows = true;
            LightId = 0;
            IsDirty = true;
            Radius = 10f;
            FalloffExponent = 10f;
            Type = LightType.Directional;
        }
        public void MakeClean()
        {
            IsDirty = false;
        }
    }

    public enum LightType
    {
        Directional = 0,
        Point = 1,
    }

    [StructLayout(LayoutKind.Explicit, Size = 128)]
    public struct DirectionalLightData
    {
        [FieldOffset(0)]
        public Vector3 Direction;

        [FieldOffset(16)]
        public Vector3 Color;

        [FieldOffset(28)]
        public float Intensity;

        [FieldOffset(32)]
        public float CastShadows;

        [FieldOffset(48)]
        public Matrix4x4 LightSpaceMatrix;

        [FieldOffset(112)]
        public float Enabled;

        [FieldOffset(116)]
        public int LightId;
    }

    [StructLayout(LayoutKind.Explicit, Size = 64)]
    public struct PointLightData
    {
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(16)]
        public Vector3 Color;
        [FieldOffset(28)]
        public float Intensity;
        [FieldOffset(32)]
        public float Radius;
        [FieldOffset(36)]
        public float CastShadows;
        [FieldOffset(40)]
        public float FalloffExponent;
        [FieldOffset(44)]
        public float Enabled;
        [FieldOffset(48)]
        public int LightId;
    }

    [StructLayout(LayoutKind.Explicit, Size = 144)]
    public struct SpotLightData
    {
        [FieldOffset(0)]
        public Vector3 Position;
        [FieldOffset(16)]
        public Vector3 Direction;
        [FieldOffset(32)]
        public Vector3 Color;
        [FieldOffset(44)]
        public float Intensity;
        [FieldOffset(48)]
        public float InnerCutoff;
        [FieldOffset(52)]
        public float OuterCutoff;
        [FieldOffset(56)]
        public float Radius;
        [FieldOffset(60)]
        public float CastShadows;
        [FieldOffset(64)]
        public Matrix4x4 LightSpaceMatrix;
        [FieldOffset(128)]
        public float Enabled;
        [FieldOffset(132)]
        public int LightId;
    }

    [StructLayout(LayoutKind.Explicit, Size = 2224)]
    public struct LightsUboData
    {
        [FieldOffset(0)]
        public DirectionalLightData DirectionalLight0;
        [FieldOffset(128)]
        public DirectionalLightData DirectionalLight1;
        [FieldOffset(256)]
        public DirectionalLightData DirectionalLight2;
        [FieldOffset(384)]
        public DirectionalLightData DirectionalLight3;

        [FieldOffset(512)]
        public PointLightData PointLight0;
        [FieldOffset(576)]
        public PointLightData PointLight1;
        [FieldOffset(640)]
        public PointLightData PointLight2;
        [FieldOffset(704)]
        public PointLightData PointLight3;
        [FieldOffset(768)]
        public PointLightData PointLight4;
        [FieldOffset(832)]
        public PointLightData PointLight5;
        [FieldOffset(896)]
        public PointLightData PointLight6;
        [FieldOffset(960)]
        public PointLightData PointLight7;

        [FieldOffset(1024)]
        public SpotLightData SpotLight0;
        [FieldOffset(1168)]
        public SpotLightData SpotLight1;
        [FieldOffset(1312)]
        public SpotLightData SpotLight2;
        [FieldOffset(1456)]
        public SpotLightData SpotLight3;
        [FieldOffset(1600)]
        public SpotLightData SpotLight4;
        [FieldOffset(1744)]
        public SpotLightData SpotLight5;
        [FieldOffset(1888)]
        public SpotLightData SpotLight6;
        [FieldOffset(2032)]
        public SpotLightData SpotLight7;

        [FieldOffset(2176)]
        public Vector3 AmbientColor;
        [FieldOffset(2188)]
        public float AmbientIntensity;
        [FieldOffset(2192)]
        public int NumDirectionalLights;
        [FieldOffset(2196)]
        public int NumPointLights;
        [FieldOffset(2200)]
        public int NumSpotLights;
        [FieldOffset(2204)]
        public float ShadowBias;
        [FieldOffset(2208)]
        public int PcfKernelSize;
        [FieldOffset(2212)]
        public float ShadowIntensity;
    }


    /*
     LightsUBO BlockIndex:1 Size:2224 ActiveUniforms:188
    {"Name":"LightsUBO.directionalLights[0].direction","Index":44,"Offset":0,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[0].color","Index":48,"Offset":16,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[0].intensity","Index":52,"Offset":28,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[0].castShadows","Index":56,"Offset":32,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[0].lightSpaceMatrix","Index":60,"Offset":48,"Size":1,"Type":35676,"ArrayStride":0,"MatrixStride":16}
    {"Name":"LightsUBO.directionalLights[0].enabled","Index":64,"Offset":112,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[0].lightId","Index":68,"Offset":116,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[1].direction","Index":45,"Offset":128,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[1].color","Index":49,"Offset":144,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[1].intensity","Index":53,"Offset":156,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[1].castShadows","Index":57,"Offset":160,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[1].lightSpaceMatrix","Index":61,"Offset":176,"Size":1,"Type":35676,"ArrayStride":0,"MatrixStride":16}
    {"Name":"LightsUBO.directionalLights[1].enabled","Index":65,"Offset":240,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[1].lightId","Index":69,"Offset":244,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[2].direction","Index":46,"Offset":256,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[2].color","Index":50,"Offset":272,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[2].intensity","Index":54,"Offset":284,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[2].castShadows","Index":58,"Offset":288,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[2].lightSpaceMatrix","Index":62,"Offset":304,"Size":1,"Type":35676,"ArrayStride":0,"MatrixStride":16}
    {"Name":"LightsUBO.directionalLights[2].enabled","Index":66,"Offset":368,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[2].lightId","Index":70,"Offset":372,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[3].direction","Index":47,"Offset":384,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[3].color","Index":51,"Offset":400,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[3].intensity","Index":55,"Offset":412,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[3].castShadows","Index":59,"Offset":416,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[3].lightSpaceMatrix","Index":63,"Offset":432,"Size":1,"Type":35676,"ArrayStride":0,"MatrixStride":16}
    {"Name":"LightsUBO.directionalLights[3].enabled","Index":67,"Offset":496,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.directionalLights[3].lightId","Index":71,"Offset":500,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[0].position","Index":72,"Offset":512,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[0].color","Index":80,"Offset":528,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[0].intensity","Index":88,"Offset":540,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[0].radius","Index":96,"Offset":544,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[0].castShadows","Index":104,"Offset":548,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[0].falloffExponent","Index":112,"Offset":552,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[0].enabled","Index":120,"Offset":556,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[0].lightId","Index":128,"Offset":560,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[1].position","Index":73,"Offset":576,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[1].color","Index":81,"Offset":592,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[1].intensity","Index":89,"Offset":604,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[1].radius","Index":97,"Offset":608,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[1].castShadows","Index":105,"Offset":612,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[1].falloffExponent","Index":113,"Offset":616,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[1].enabled","Index":121,"Offset":620,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[1].lightId","Index":129,"Offset":624,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[2].position","Index":74,"Offset":640,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[2].color","Index":82,"Offset":656,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[2].intensity","Index":90,"Offset":668,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[2].radius","Index":98,"Offset":672,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[2].castShadows","Index":106,"Offset":676,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[2].falloffExponent","Index":114,"Offset":680,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[2].enabled","Index":122,"Offset":684,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[2].lightId","Index":130,"Offset":688,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[3].position","Index":75,"Offset":704,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[3].color","Index":83,"Offset":720,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[3].intensity","Index":91,"Offset":732,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[3].radius","Index":99,"Offset":736,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[3].castShadows","Index":107,"Offset":740,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[3].falloffExponent","Index":115,"Offset":744,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[3].enabled","Index":123,"Offset":748,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[3].lightId","Index":131,"Offset":752,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[4].position","Index":76,"Offset":768,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[4].color","Index":84,"Offset":784,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[4].intensity","Index":92,"Offset":796,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[4].radius","Index":100,"Offset":800,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[4].castShadows","Index":108,"Offset":804,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[4].falloffExponent","Index":116,"Offset":808,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[4].enabled","Index":124,"Offset":812,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[4].lightId","Index":132,"Offset":816,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[5].position","Index":77,"Offset":832,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[5].color","Index":85,"Offset":848,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[5].intensity","Index":93,"Offset":860,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[5].radius","Index":101,"Offset":864,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[5].castShadows","Index":109,"Offset":868,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[5].falloffExponent","Index":117,"Offset":872,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[5].enabled","Index":125,"Offset":876,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[5].lightId","Index":133,"Offset":880,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[6].position","Index":78,"Offset":896,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[6].color","Index":86,"Offset":912,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[6].intensity","Index":94,"Offset":924,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[6].radius","Index":102,"Offset":928,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[6].castShadows","Index":110,"Offset":932,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[6].falloffExponent","Index":118,"Offset":936,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[6].enabled","Index":126,"Offset":940,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[6].lightId","Index":134,"Offset":944,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[7].position","Index":79,"Offset":960,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[7].color","Index":87,"Offset":976,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[7].intensity","Index":95,"Offset":988,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[7].radius","Index":103,"Offset":992,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[7].castShadows","Index":111,"Offset":996,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[7].falloffExponent","Index":119,"Offset":1000,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[7].enabled","Index":127,"Offset":1004,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pointLights[7].lightId","Index":135,"Offset":1008,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[0].position","Index":136,"Offset":1024,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[0].direction","Index":144,"Offset":1040,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[0].color","Index":152,"Offset":1056,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[0].intensity","Index":160,"Offset":1068,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[0].innerCutoff","Index":168,"Offset":1072,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[0].outerCutoff","Index":176,"Offset":1076,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[0].radius","Index":184,"Offset":1080,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[0].castShadows","Index":192,"Offset":1084,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[0].lightSpaceMatrix","Index":200,"Offset":1088,"Size":1,"Type":35676,"ArrayStride":0,"MatrixStride":16}
    {"Name":"LightsUBO.spotLights[0].enabled","Index":208,"Offset":1152,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[0].lightId","Index":216,"Offset":1156,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[1].position","Index":137,"Offset":1168,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[1].direction","Index":145,"Offset":1184,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[1].color","Index":153,"Offset":1200,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[1].intensity","Index":161,"Offset":1212,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[1].innerCutoff","Index":169,"Offset":1216,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[1].outerCutoff","Index":177,"Offset":1220,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[1].radius","Index":185,"Offset":1224,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[1].castShadows","Index":193,"Offset":1228,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[1].lightSpaceMatrix","Index":201,"Offset":1232,"Size":1,"Type":35676,"ArrayStride":0,"MatrixStride":16}
    {"Name":"LightsUBO.spotLights[1].enabled","Index":209,"Offset":1296,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[1].lightId","Index":217,"Offset":1300,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[2].position","Index":138,"Offset":1312,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[2].direction","Index":146,"Offset":1328,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[2].color","Index":154,"Offset":1344,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[2].intensity","Index":162,"Offset":1356,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[2].innerCutoff","Index":170,"Offset":1360,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[2].outerCutoff","Index":178,"Offset":1364,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[2].radius","Index":186,"Offset":1368,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[2].castShadows","Index":194,"Offset":1372,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[2].lightSpaceMatrix","Index":202,"Offset":1376,"Size":1,"Type":35676,"ArrayStride":0,"MatrixStride":16}
    {"Name":"LightsUBO.spotLights[2].enabled","Index":210,"Offset":1440,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[2].lightId","Index":218,"Offset":1444,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[3].position","Index":139,"Offset":1456,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[3].direction","Index":147,"Offset":1472,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[3].color","Index":155,"Offset":1488,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[3].intensity","Index":163,"Offset":1500,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[3].innerCutoff","Index":171,"Offset":1504,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[3].outerCutoff","Index":179,"Offset":1508,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[3].radius","Index":187,"Offset":1512,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[3].castShadows","Index":195,"Offset":1516,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[3].lightSpaceMatrix","Index":203,"Offset":1520,"Size":1,"Type":35676,"ArrayStride":0,"MatrixStride":16}
    {"Name":"LightsUBO.spotLights[3].enabled","Index":211,"Offset":1584,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[3].lightId","Index":219,"Offset":1588,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[4].position","Index":140,"Offset":1600,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[4].direction","Index":148,"Offset":1616,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[4].color","Index":156,"Offset":1632,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[4].intensity","Index":164,"Offset":1644,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[4].innerCutoff","Index":172,"Offset":1648,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[4].outerCutoff","Index":180,"Offset":1652,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[4].radius","Index":188,"Offset":1656,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[4].castShadows","Index":196,"Offset":1660,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[4].lightSpaceMatrix","Index":204,"Offset":1664,"Size":1,"Type":35676,"ArrayStride":0,"MatrixStride":16}
    {"Name":"LightsUBO.spotLights[4].enabled","Index":212,"Offset":1728,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[4].lightId","Index":220,"Offset":1732,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[5].position","Index":141,"Offset":1744,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[5].direction","Index":149,"Offset":1760,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[5].color","Index":157,"Offset":1776,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[5].intensity","Index":165,"Offset":1788,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[5].innerCutoff","Index":173,"Offset":1792,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[5].outerCutoff","Index":181,"Offset":1796,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[5].radius","Index":189,"Offset":1800,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[5].castShadows","Index":197,"Offset":1804,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[5].lightSpaceMatrix","Index":205,"Offset":1808,"Size":1,"Type":35676,"ArrayStride":0,"MatrixStride":16}
    {"Name":"LightsUBO.spotLights[5].enabled","Index":213,"Offset":1872,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[5].lightId","Index":221,"Offset":1876,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[6].position","Index":142,"Offset":1888,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[6].direction","Index":150,"Offset":1904,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[6].color","Index":158,"Offset":1920,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[6].intensity","Index":166,"Offset":1932,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[6].innerCutoff","Index":174,"Offset":1936,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[6].outerCutoff","Index":182,"Offset":1940,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[6].radius","Index":190,"Offset":1944,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[6].castShadows","Index":198,"Offset":1948,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[6].lightSpaceMatrix","Index":206,"Offset":1952,"Size":1,"Type":35676,"ArrayStride":0,"MatrixStride":16}
    {"Name":"LightsUBO.spotLights[6].enabled","Index":214,"Offset":2016,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[6].lightId","Index":222,"Offset":2020,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[7].position","Index":143,"Offset":2032,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[7].direction","Index":151,"Offset":2048,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[7].color","Index":159,"Offset":2064,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[7].intensity","Index":167,"Offset":2076,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[7].innerCutoff","Index":175,"Offset":2080,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[7].outerCutoff","Index":183,"Offset":2084,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[7].radius","Index":191,"Offset":2088,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[7].castShadows","Index":199,"Offset":2092,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[7].lightSpaceMatrix","Index":207,"Offset":2096,"Size":1,"Type":35676,"ArrayStride":0,"MatrixStride":16}
    {"Name":"LightsUBO.spotLights[7].enabled","Index":215,"Offset":2160,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.spotLights[7].lightId","Index":223,"Offset":2164,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.ambientColor","Index":224,"Offset":2176,"Size":1,"Type":35665,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.ambientIntensity","Index":225,"Offset":2188,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.numDirectionalLights","Index":226,"Offset":2192,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.numPointLights","Index":227,"Offset":2196,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.numSpotLights","Index":228,"Offset":2200,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.shadowBias","Index":229,"Offset":2204,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.pcfKernelSize","Index":230,"Offset":2208,"Size":1,"Type":5124,"ArrayStride":0,"MatrixStride":0}
    {"Name":"LightsUBO.shadowIntensity","Index":231,"Offset":2212,"Size":1,"Type":5126,"ArrayStride":0,"MatrixStride":0}
     */
}
