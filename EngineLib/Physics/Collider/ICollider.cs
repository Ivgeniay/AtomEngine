using System.Numerics;

namespace AtomEngine
{
    public interface ICollider
    {
        // Support функция - ключевой метод для GJK алгоритма
        // Возвращает самую дальнюю точку коллайдера в заданном направлении
        Vector3 GetSupport(Vector3 direction);

        // Вычисляет ограничивающий объем в локальном пространстве
        // Этот метод нужен для BVH дерева
        IBoundingVolume ComputeBounds();
    }
}
