using System.Numerics;

namespace AtomEngine
{
    public static class GJKAlgorithm
    {
        public delegate Vector3 SupportFunction(Vector3 direction);

        private const float Epsilon = 1e-4f;
        private const int MaxIterations = 32;
        public struct Simplex
        {
            public Vector3[] Points;
            public int Count;

            public Simplex(int capacity)
            {
                Points = new Vector3[capacity];
                Count = 0;
            }

            public void PushFront(Vector3 point)
            {
                for (int i = Count; i > 0; i--)
                    Points[i] = Points[i - 1];
                Points[0] = point;
                Count++;
            }

            public Vector3 this[int index]
            {
                get => Points[index];
                set => Points[index] = value;
            }
        }

        public static bool Intersect(SupportFunction shapeA, SupportFunction shapeB, out Simplex simplex)
        {
            simplex = new Simplex(4);

            // Используем произвольное начальное направление
            Vector3 direction = Vector3.UnitX;

            // Получаем первую опорную точку с помощью support функций
            Vector3 support = GetSupport(shapeA, shapeB, direction);
            simplex.PushFront(support);

            direction = -support;

            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                if (direction.LengthSquared() > Epsilon)
                {
                    direction = Vector3.Normalize(direction);
                }

                support = GetSupport(shapeA, shapeB, direction);
                float dot = Vector3.Dot(support, direction);

                if (dot < 0)
                    return false;

                simplex.PushFront(support);

                if (DoSimplex(ref simplex, ref direction))
                    return true;
            }

            return false;
        }

        public static bool Intersect(Vector3[] verticesA, Vector3[] verticesB)
        {
            SupportFunction supportA = direction => GetSupportFromVertices(verticesA, direction);
            SupportFunction supportB = direction => GetSupportFromVertices(verticesB, direction);

            Simplex simplex;
            return Intersect(supportA, supportB, out simplex);
        }

        //public static bool Intersect(Vector3[] verticesA, Vector3[] verticesB)
        //{
        //    Vector3 centerA = GetCentroid(verticesA);
        //    Vector3 centerB = GetCentroid(verticesB);

        //    Vector3 direction = centerB - centerA;

        //    if (direction.LengthSquared() < Epsilon)
        //    {
        //        direction = Vector3.UnitX;
        //    }
        //    else
        //    {
        //        direction = Vector3.Normalize(direction);
        //    }

        //    Simplex simplex = new Simplex(4);
        //    Vector3 support = GetSupport(verticesA, direction) - GetSupport(verticesB, -direction);
        //    simplex.PushFront(support);
        //    direction = -support;

        //    for (int iteration = 0; iteration < MaxIterations; iteration++)
        //    {
        //        if (direction.LengthSquared() > Epsilon)
        //        {
        //            direction = Vector3.Normalize(direction);
        //        }
        //        else
        //        {
        //            return true;
        //        }

        //        support = GetSupport(verticesA, direction) - GetSupport(verticesB, -direction);
        //        float dot = Vector3.Dot(support, direction);

        //        if (dot < 0)
        //        {
        //            return false;
        //        }

        //        simplex.PushFront(support);
        //        if (DoSimplex(ref simplex, ref direction))
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        private static bool DoSimplex(ref Simplex simplex, ref Vector3 direction)
        {
            switch (simplex.Count)
            {
                case 2: return LineCase(ref simplex, ref direction);
                case 3: return TriangleCase(ref simplex, ref direction);
                case 4: return TetrahedronCase(ref simplex, ref direction);
                default: return false;
            }
        }

        private static bool LineCase(ref Simplex simplex, ref Vector3 direction)
        {
            Vector3 a = simplex[0];
            Vector3 b = simplex[1];
            Vector3 ab = b - a;
            Vector3 ao = -a;

            if (Vector3.Dot(ab, ao) > 0)
            {
                direction = Vector3.Cross(Vector3.Cross(ab, ao), ab);
                if (direction.LengthSquared() < Epsilon)
                {
                    direction = Vector3.Cross(ab, Vector3.UnitX);
                    if (direction.LengthSquared() < Epsilon)
                    {
                        direction = Vector3.Cross(ab, Vector3.UnitY);
                    }
                }
            }
            else
            {
                simplex.Count = 1;
                direction = ao;
            }

            return false;
        }

        private static bool TriangleCase(ref Simplex simplex, ref Vector3 direction)
        {
            Vector3 a = simplex[0];
            Vector3 b = simplex[1];
            Vector3 c = simplex[2];

            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ao = -a;

            Vector3 abc = Vector3.Cross(ab, ac);

            if (Vector3.Dot(Vector3.Cross(abc, ac), ao) > 0)
            {
                if (Vector3.Dot(ac, ao) > 0)
                {
                    simplex.Count = 2;
                    simplex[1] = c;
                    direction = Vector3.Cross(Vector3.Cross(ac, ao), ac);
                }
                else
                {
                    simplex.Count = 2;
                    simplex[1] = b;
                    return LineCase(ref simplex, ref direction);
                }
            }
            else
            {
                if (Vector3.Dot(Vector3.Cross(ab, abc), ao) > 0)
                {
                    simplex.Count = 2;
                    simplex[1] = b;
                    return LineCase(ref simplex, ref direction);
                }
                else
                {
                    if (Vector3.Dot(abc, ao) > 0)
                    {
                        direction = abc;
                    }
                    else
                    {
                        simplex[1] = c;
                        simplex[2] = b;
                        direction = -abc;
                    }
                }
            }

            return false;
        }

        private static bool TetrahedronCase(ref Simplex simplex, ref Vector3 direction)
        {
            Vector3 a = simplex[0];
            Vector3 b = simplex[1];
            Vector3 c = simplex[2];
            Vector3 d = simplex[3];

            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ad = d - a;
            Vector3 ao = -a;

            Vector3 abc = Vector3.Cross(ab, ac);
            Vector3 acd = Vector3.Cross(ac, ad);
            Vector3 adb = Vector3.Cross(ad, ab);

            if (Vector3.Dot(abc, ao) > 0)
            {
                simplex.Count = 3;
                simplex[2] = c;
                simplex[1] = b;
                return TriangleCase(ref simplex, ref direction);
            }

            if (Vector3.Dot(acd, ao) > 0)
            {
                simplex.Count = 3;
                simplex[2] = c;
                simplex[1] = d;
                return TriangleCase(ref simplex, ref direction);
            }

            if (Vector3.Dot(adb, ao) > 0)
            {
                simplex.Count = 3;
                simplex[2] = b;
                simplex[1] = d;
                return TriangleCase(ref simplex, ref direction);
            }

            return true;
        }

        private static Vector3 GetSupport(Vector3[] vertices, Vector3 direction)
        {
            float maxDot = float.MinValue;
            Vector3 support = Vector3.Zero;

            foreach (var vertex in vertices)
            {
                float dot = Vector3.Dot(vertex, direction);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    support = vertex;
                }
            }

            return support;
        }

        private static Vector3 GetSupport(SupportFunction shapeA, SupportFunction shapeB, Vector3 direction)
        {
            return shapeA(direction) - shapeB(-direction);
        }

        private static Vector3 GetSupportFromVertices(Vector3[] vertices, Vector3 direction)
        {
            float maxDot = float.MinValue;
            Vector3 support = Vector3.Zero;

            foreach (var vertex in vertices)
            {
                float dot = Vector3.Dot(vertex, direction);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    support = vertex;
                }
            }

            return support;
        }

        private static Vector3 GetCentroid(Vector3[] vertices)
        {
            Vector3 center = Vector3.Zero;
            foreach (var vertex in vertices)
            {
                center += vertex;
            }
            return center / vertices.Length;
        }
    }
}