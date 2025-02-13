using System.Numerics;

namespace EngineLib
{
    public readonly struct Triangle
    {
        public readonly Vector3 A;
        public readonly Vector3 B;
        public readonly Vector3 C;

        public Triangle(in Vector3 a, in Vector3 b, in Vector3 c)
        {
            A = a;
            B = b;
            C = c;
        }

        public Vector3 Normal
        {
            get
            {
                var ab = B - A;
                var ac = C - A;
                return Vector3.Normalize(Vector3.Cross(ab, ac));
            }
        }
    }
}
