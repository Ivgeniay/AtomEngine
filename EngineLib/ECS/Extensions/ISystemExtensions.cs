namespace AtomEngine
{
    public static class ISystemExtensions
    {
        public static ref T GetComponent<T>(this ICommonSystem self, Entity entity) where T : struct, IComponent =>
            ref self.World.GetComponent<T>(entity);

        public static bool HasComponent<T>(this ICommonSystem self, Entity entity) where T : struct, IComponent =>
            self.World.HasComponent<T>(entity);

        public static ref T AddComponent<T>(this ICommonSystem self, Entity entity, in T component) where T : struct, IComponent =>
            ref self.World.AddComponent<T>(entity, component);

        public static void RemoveComponent<T>(this ICommonSystem self, Entity entity) where T : struct, IComponent =>
            self.World.RemoveComponent<T>(entity);

        public static Entity CreateEntity(this ICommonSystem self) =>
            self.World.CreateEntity();

        public static void DestroyEntity(this ICommonSystem self, Entity entity) =>
            self.World.DestroyEntity(entity);

        public static QueryEntity CreateEntityQuery(this ICommonSystem self) =>
            self.World.CreateEntityQuery();

        public static IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1>(this ICommonSystem self) where T1 : struct, IComponent =>
            self.World.GetEntitiesByArchetypeHaving<T1>();

        public static IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1, T2>(this ICommonSystem self) where T1 : struct, IComponent where T2 : struct, IComponent =>
            self.World.GetEntitiesByArchetypeHaving<T1, T2>();

        public static IEnumerable<Entity> GetEntitiesByArchetypeHaving<T1, T2, T3>(this ICommonSystem self) where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent =>
            self.World.GetEntitiesByArchetypeHaving<T1, T2, T3>();

        public static IEnumerable<Entity> GetEntitiesByArchetype<T1>(this ICommonSystem self) where T1 : struct, IComponent =>
            self.World.GetEntitiesByArchetype<T1>();

        public static IEnumerable<Entity> GetEntitiesByArchetype<T1, T2>(this ICommonSystem self) where T1 : struct, IComponent where T2 : struct, IComponent =>
            self.World.GetEntitiesByArchetype<T1, T2>();

        public static IEnumerable<Entity> GetEntitiesByArchetype<T1, T2, T3>(this ICommonSystem self) where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent =>
            self.World.GetEntitiesByArchetype<T1, T2, T3>();


    }
}
