using System.Numerics;

namespace AtomEngine
{
    public readonly struct ContactPoint
    {
        public readonly Vector3 Position;      
        public readonly Vector3 Normal;        
        public readonly float Penetration;     

        public ContactPoint(Vector3 position, Vector3 normal, float penetration)
        {
            Position = position;
            Normal = normal;
            Penetration = penetration;
        }
    }

    public struct CollisionManifold
    {
        public const int MaxContacts = 4;

        public readonly Entity BodyA { get; }
        public readonly Entity BodyB { get; }
        public readonly ContactPoint[] Contacts;

        public int ContactCount { get; private set; }
        public float RestitutionCoefficient { get; set; }  // Коэффициент восстановления [0,1]
        public float FrictionCoefficient { get; set; }     // Коэффициент трения

        public bool HasContacts => ContactCount > 0;

        public CollisionManifold(Entity bodyA, Entity bodyB, float restitution = 0.5f, float friction = 0.3f)
        {
            BodyA = bodyA;
            BodyB = bodyB;
            Contacts = new ContactPoint[MaxContacts];
            ContactCount = 0;
            RestitutionCoefficient = restitution;
            FrictionCoefficient = friction;
        }

        public bool TryAddContact(Vector3 position, Vector3 normal, float penetration)
        {
            if (ContactCount >= MaxContacts)
                return false;

            Contacts[ContactCount] = new ContactPoint(position, normal, penetration);
            ContactCount++;
            return true;
        }

        public void Clear()
        {
            ContactCount = 0;
        }

        public ReadOnlySpan<ContactPoint> GetContacts()
        {
            return new ReadOnlySpan<ContactPoint>(Contacts, 0, ContactCount);
        }

        // Вычисление точки разрешения столкновения
        public Vector3 GetContactPoint()
        {
            if (ContactCount == 0)
                return Vector3.Zero;

            Vector3 point = Vector3.Zero;
            for (int i = 0; i < ContactCount; i++)
            {
                point += Contacts[i].Position;
            }
            return point / ContactCount;
        }

        // Получение средней нормали для всех точек контакта
        public Vector3 GetAverageNormal()
        {
            if (ContactCount == 0)
                return Vector3.Zero;

            Vector3 normal = Vector3.Zero;
            for (int i = 0; i < ContactCount; i++)
            {
                normal += Contacts[i].Normal;
            }
            return Vector3.Normalize(normal);
        }

        // Получение максимальной глубины проникновения
        public float GetMaxPenetration()
        {
            float maxPenetration = 0;
            for (int i = 0; i < ContactCount; i++)
            {
                maxPenetration = Math.Max(maxPenetration, Contacts[i].Penetration);
            }
            return maxPenetration;
        }
    }
}
