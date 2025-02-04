namespace OpenglLib
{
    public enum GlslType
    {
        // Скалярные типы
        Float,
        Int,
        Bool,
        Double,

        // Векторные типы (float)
        Vec2,
        Vec3,
        Vec4,

        // Векторные типы (int)
        IVec2,
        IVec3,
        IVec4,

        // Векторные типы (bool)
        BVec2,
        BVec3,
        BVec4,

        // Векторные типы (double)
        DVec2,
        DVec3,
        DVec4,

        // Векторные типы (unsigned int)
        UVec2,
        UVec3,
        UVec4,

        // Матричные типы (float)
        Mat2,
        Mat3,
        Mat4,
        Mat2x3,
        Mat2x4,
        Mat3x2,
        Mat3x4,
        Mat4x2,
        Mat4x3,

        // Матричные типы (double)
        DMat2,
        DMat3,
        DMat4,

        // Сэмплеры
        Sampler1D,
        Sampler2D,
        Sampler3D,
        SamplerCube,
        Sampler1DShadow,
        Sampler2DShadow,
        SamplerCubeShadow
    }

}