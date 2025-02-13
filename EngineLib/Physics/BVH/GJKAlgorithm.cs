using System.Numerics;

namespace AtomEngine
{
    public class GJKAlgorithm
    {
        private const float Epsilon = 1e-6f;
        private const int MaxIterations = 32;

        // Вспомогательная структура для симплекса
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
        }

        // Функция поиска опорной точки
        private static Vector3 GetSupport(Vector3[] vertices, Vector3 direction)
        {
            float maxDot = float.MinValue;
            Vector3 support = Vector3.Zero;

            for (int i = 0; i < vertices.Length; i++)
            {
                float dot = Vector3.Dot(vertices[i], direction);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    support = vertices[i];
                }
            }

            return support;
        }

        // Проверка пересечения двух выпуклых множеств
        public static bool Intersect(Vector3[] verticesA, Vector3[] verticesB)
        {
            // Начальное направление
            Vector3 direction = Vector3.UnitX;

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
                // Получаем новую опорную точку
                support = GetSupport(verticesA, direction) - GetSupport(verticesB, -direction);

                // Проверяем, продвинулись ли мы в направлении начала координат
                float dot = Vector3.Dot(support, direction);
                if (dot < 0)
                    return false; // Нет пересечения

                simplex.PushFront(support);

                // Обновляем симплекс и направление поиска
                if (UpdateSimplex(ref simplex, ref direction))
                    return true; // Найдено пересечение
            }

            return false;
        }

        // Обновление симплекса и направления поиска
        private static bool UpdateSimplex(ref Simplex simplex, ref Vector3 direction)
        {
            switch (simplex.Count)
            {
                case 2:
                    return Line(ref simplex, ref direction);
                case 3:
                    return Triangle(ref simplex, ref direction);
                case 4:
                    return Tetrahedron(ref simplex, ref direction);
                default:
                    return false;
            }
        }

        // Обработка линии
        private static bool Line(ref Simplex simplex, ref Vector3 direction)
        {
            Vector3 a = simplex.Points[0];
            Vector3 b = simplex.Points[1];
            Vector3 ab = b - a;
            Vector3 ao = -a;

            direction = Vector3.Cross(Vector3.Cross(ab, ao), ab);
            return false;
        }

        // Обработка треугольника
        private static bool Triangle(ref Simplex simplex, ref Vector3 direction)
        {
            Vector3 a = simplex.Points[0];
            Vector3 b = simplex.Points[1];
            Vector3 c = simplex.Points[2];

            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ao = -a;

            Vector3 abc = Vector3.Cross(ab, ac);

            if (Vector3.Dot(Vector3.Cross(abc, ac), ao) >= 0)
            {
                simplex.Points[1] = c;
                simplex.Count = 2;
                direction = Vector3.Cross(Vector3.Cross(ac, ao), ac);
            }
            else if (Vector3.Dot(Vector3.Cross(ab, abc), ao) >= 0)
            {
                simplex.Points[2] = b;
                simplex.Count = 2;
                direction = Vector3.Cross(Vector3.Cross(ab, ao), ab);
            }
            else
            {
                if (Vector3.Dot(abc, ao) >= 0)
                {
                    direction = abc;
                }
                else
                {
                    direction = -abc;
                }
            }

            return false;
        }

        // Обработка тетраэдра
        private static bool Tetrahedron(ref Simplex simplex, ref Vector3 direction)
        {
            Vector3 a = simplex.Points[0];
            Vector3 b = simplex.Points[1];
            Vector3 c = simplex.Points[2];
            Vector3 d = simplex.Points[3];

            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ad = d - a;
            Vector3 ao = -a;

            Vector3 abc = Vector3.Cross(ab, ac);
            Vector3 acd = Vector3.Cross(ac, ad);
            Vector3 adb = Vector3.Cross(ad, ab);

            if (Vector3.Dot(abc, ao) > 0)
            {
                simplex.Points[3] = c;
                simplex.Points[2] = b;
                simplex.Count = 3;
                return Triangle(ref simplex, ref direction);
            }

            if (Vector3.Dot(acd, ao) > 0)
            {
                simplex.Points[1] = c;
                simplex.Count = 3;
                return Triangle(ref simplex, ref direction);
            }

            if (Vector3.Dot(adb, ao) > 0)
            {
                simplex.Points[2] = b;
                simplex.Count = 3;
                return Triangle(ref simplex, ref direction);
            }

            return true; // Начало координат внутри тетраэдра
        }
    }
}