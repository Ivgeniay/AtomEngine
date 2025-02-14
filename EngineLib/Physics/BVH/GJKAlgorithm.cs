using System.Numerics;

namespace AtomEngine
{
        // Вспомогательная структура для симплекса
    public static class GJKAlgorithm
    {
        private const float Epsilon = 1e-4f;
        private const int MaxIterations = 32;
        private struct Simplex
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

        public static bool Intersect(Vector3[] verticesA, Vector3[] verticesB)
        {
            // Получаем начальное направление из центров объектов
            Vector3 centerA = GetCentroid(verticesA);
            Vector3 centerB = GetCentroid(verticesB);
            Vector3 direction = centerB - centerA;

            // Если центры совпадают, используем произвольное направление
            if (direction.LengthSquared() < Epsilon)
            {
                direction = Vector3.UnitX;
            }
            else
            {
                direction = Vector3.Normalize(direction);
            }

            // Инициализация симплекса
            Simplex simplex = new Simplex(4);

            // Получаем первую опорную точку
            Vector3 support = GetSupport(verticesA, direction) - GetSupport(verticesB, -direction);
            simplex.PushFront(support);

            // Меняем направление к началу координат
            direction = -support;

            // Основной цикл GJK
            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                // Нормализуем направление
                if (direction.LengthSquared() > Epsilon)
                {
                    direction = Vector3.Normalize(direction);
                }
                else
                {
                    // Если direction близок к нулю, это может означать пересечение
                    return true;
                }

                // Получаем новую опорную точку
                support = GetSupport(verticesA, direction) - GetSupport(verticesB, -direction);

                // Проверяем, продвинулись ли мы в направлении начала координат
                float dot = Vector3.Dot(support, direction);

                //DebLogger.Debug($"Iteration {iteration}: Support = {support}, Direction = {direction}, Dot = {dot}");

                if (dot < 0) // Используем 0 вместо Epsilon для более точной проверки
                {
                    return false; // Нет пересечения
                }

                simplex.PushFront(support);

                // Обновляем симплекс и направление поиска
                if (DoSimplex(ref simplex, ref direction))
                {
                    return true; // Найдено пересечение
                }
            }

            return false; // Превышено максимальное количество итераций
        }

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

            return true; // Точка внутри тетраэдра
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