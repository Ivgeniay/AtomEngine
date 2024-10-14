namespace AtomEngine.Math
{
    public struct Bounds
    {
        public Vector3D Center;
        public Vector3D Size;

        public Bounds(Vector3D center, Vector3D size)
        {
            Center = center;
            Size = size;
        }

        public Vector3D Min => Center - Size / 2;
        public Vector3D Max => Center + Size / 2;
        public Vector3D Extents => Size / 2;
        public bool Contains(Vector3D point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y &&
                   point.Z >= Min.Z && point.Z <= Max.Z;
        }

        public override string ToString()
        {
            return "Bounds: Center: " + Center + " Size: " + Size;
        }
    }
}
