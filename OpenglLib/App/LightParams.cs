namespace OpenglLib
{
    internal class LightParams
    {
        public const int MAX_DIRECTIONAL_LIGHTS = 4;
        public const int MAX_POINT_LIGHTS = 8;
        public const int MAX_SPOT_LIGHTS = 8;
        public const int MAX_CASCADES = 4;

        public const float MAX_SHADOW_DISTANCE = 100.0f;
        public const float CASCADE_DISTRIBUTION_LAMBDA = 0.95f;

        public const int MAX_CAMERAS = 4;
    }
}
