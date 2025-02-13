using System.Numerics;

namespace AtomEngine
{
    public struct ContactPoint
    {
        public Vector3 Position;      // Позиция контакта в мировых координатах
        public Vector3 Normal;        // Нормаль контакта
        public float Penetration;     // Глубина проникновения
        public float Restitution;     // Коэффициент упругости в точке контакта
        public float Friction;        // Коэффициент трения в точке контакта
    }
}
