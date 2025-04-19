using AtomEngine;
using EngineLib;

namespace OpenglLib
{
    public partial struct ShadowMapComponent : IComponent
    {
        public const int MAX_SHADOW_MAPS = LightParams.MAX_DIRECTIONAL_LIGHTS + LightParams.MAX_POINT_LIGHTS;

        public Entity Owner { get; set; }
        public uint ShadowMapArrayTextureId;
        public int[] LightIds;
        public LightType[] LightTypes;
        public int ResolutionWidth;
        public int ResolutionHeight;

        public ShadowMapComponent(Entity owner)
        {
            Owner = owner;
            ShadowMapArrayTextureId = 0;
            LightIds = new int[MAX_SHADOW_MAPS];
            LightTypes = new LightType[MAX_SHADOW_MAPS];
            ResolutionWidth = 2048;
            ResolutionHeight = 2048;

            for (int i = 0; i < MAX_SHADOW_MAPS; i++)
            {
                LightIds[i] = -1;
                LightTypes[i] = (LightType)(-1);
            }
        }

        public int GetDirectionalLightIndex(int lightId)
        {
            for (int i = 0; i < LightParams.MAX_DIRECTIONAL_LIGHTS; i++)
            {
                if (LightIds[i] == lightId && LightTypes[i] == LightType.Directional)
                    return i;
            }
            return -1;
        }

        public int GetPointLightIndex(int lightId)
        {
            for (int i = LightParams.MAX_DIRECTIONAL_LIGHTS; i < MAX_SHADOW_MAPS; i++)
            {
                if (LightIds[i] == lightId && LightTypes[i] == LightType.Point)
                    return i - LightParams.MAX_DIRECTIONAL_LIGHTS;
            }
            return -1;
        }

        public int GetPointLightLayerIndex(int lightId)
        {
            for (int i = LightParams.MAX_DIRECTIONAL_LIGHTS; i < MAX_SHADOW_MAPS; i++)
            {
                if (LightIds[i] == lightId && LightTypes[i] == LightType.Point)
                    return i;
            }
            return -1;
        }
    }

}
