using System.Numerics;

namespace AtomEngine
{
    public struct CollisionManifold
    {
        public const int MaxContacts = 4;

        public Entity BodyAId;                           // ID первого тела
        public Entity BodyBId;                           // ID второго тела
        public ContactPoint[] Contacts;               // Точки контакта
        public int ContactCount;                      // Количество точек контакта
        public float RestitutionCoefficient;         // Общий коэффициент упругости
        public float FrictionCoefficient;            // Общий коэффициент трения

        public CollisionManifold(Entity bodyAId, Entity bodyBId)
        {
            BodyAId = bodyAId;
            BodyBId = bodyBId;
            Contacts = new ContactPoint[MaxContacts];
            ContactCount = 0;
            RestitutionCoefficient = 0;
            FrictionCoefficient = 0;
        }

        public void AddContact(Vector3 position, Vector3 normal, float penetration)
        {
            if (ContactCount >= MaxContacts) return;

            Contacts[ContactCount] = new ContactPoint
            {
                Position = position,
                Normal = normal,
                Penetration = penetration
            };
            ContactCount++;
        }
    }
}
