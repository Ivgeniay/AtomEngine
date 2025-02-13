using System.Numerics;

namespace AtomEngine
{
    public struct Support
    {
        public Vector3 Point;
        public int Index;

        public Support(Vector3 point, int index)
        {
            Point = point;
            Index = index;
        }
    }
}
