using AtomEngine;

namespace OpenglLib.ECS.Components
{
    public partial struct ShadowMapComponent : IComponent
    {
        public const int MAX_SHADOW_MAPS = 32;

        public Entity Owner { get; set; }
        [HideInInspector]
        public uint ShadowMapArrayTextureId;
        [HideInInspector]
        public int[] LightIds;
        [HideInInspector]
        public LightType[] LightTypes;
        [HideInInspector]
        public int[] CascadeIndices; 

        public ShadowMapComponent(Entity entity)
        {
            Owner = entity;
            ShadowMapArrayTextureId = 0;
            LightIds = new int[MAX_SHADOW_MAPS];
            LightTypes = new LightType[MAX_SHADOW_MAPS];
            CascadeIndices = new int[MAX_SHADOW_MAPS]; 

            for (int i = 0; i < MAX_SHADOW_MAPS; i++)
            {
                LightIds[i] = -1;
                LightTypes[i] = (LightType)(-1);
                CascadeIndices[i] = -1;
            }
        }

        public int GetDirectionalLightIndex(int lightId)
        {
            return GetDirectionalLightCascadeIndex(lightId, 0);
        }

        public int GetDirectionalLightCascadeIndex(int lightId, int cascadeIndex)
        {
            for (int i = 0; i < MAX_SHADOW_MAPS; i++)
            {
                if (LightIds[i] == lightId &&
                    LightTypes[i] == LightType.Directional &&
                    CascadeIndices[i] == cascadeIndex)
                {
                    return i;
                }
            }
            return -1;
        }

        public int GetPointLightLayerIndex(int lightId)
        {
            for (int i = 0; i < MAX_SHADOW_MAPS; i++)
            {
                if (LightIds[i] == lightId && LightTypes[i] == LightType.Point)
                {
                    return i;
                }
            }
            return -1;
        }

        public int GetSpotLightLayerIndex(int lightId)
        {
            for (int i = 0; i < MAX_SHADOW_MAPS; i++)
            {
                if (LightIds[i] == lightId && LightTypes[i] == LightType.Spot)
                {
                    return i;
                }
            }
            return -1;
        }

        public bool IsLayerUsed(int layerIndex)
        {
            return layerIndex >= 0 &&
                   layerIndex < MAX_SHADOW_MAPS &&
                   LightIds[layerIndex] != -1;
        }
    }
}
