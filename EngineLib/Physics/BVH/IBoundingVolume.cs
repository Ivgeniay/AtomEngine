﻿using System.Numerics;

namespace AtomEngine
{
    public interface IBoundingVolume 
    {
        public bool Intersects(IBoundingVolume other);
        public Vector3[] GetVertices();
        public uint[] GetIndices();
        public IBoundingVolume Transform(Matrix4x4 modelMatrix);
        public Vector3 Min { get; }
        public Vector3 Max { get; }
    }
}
