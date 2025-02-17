using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AtomEngine
{
    public struct BoxCollider : ICollider
    {
        // Размеры коробки (половина ширины, высоты и глубины)
        private Vector3 _halfExtents;

        public BoxCollider(Vector3 size)
        {
            // size - полный размер коробки, поэтому делим на 2
            _halfExtents = size * 0.5f;
        }

        public Vector3 GetSupport(Vector3 direction)
        {
            // Для коробки support point - это просто комбинация её размеров
            // с учетом знаков компонентов направления
            return new Vector3(
                direction.X > 0 ? _halfExtents.X : -_halfExtents.X,
                direction.Y > 0 ? _halfExtents.Y : -_halfExtents.Y,
                direction.Z > 0 ? _halfExtents.Z : -_halfExtents.Z
            );
        }

        public IBoundingVolume ComputeBounds()
        {
            // Для коробки ограничивающий объем совпадает с самой коробкой
            return new BoundingBox(-_halfExtents, _halfExtents);
        }
    }
}
