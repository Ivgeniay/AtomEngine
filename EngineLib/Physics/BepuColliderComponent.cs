﻿using BepuPhysics.Collidables;

namespace AtomEngine
{
    public struct BepuColliderComponent : IComponent
    {
        public Entity Owner { get; set; }
        public TypedIndex ShapeIndex;

        public BepuColliderComponent(Entity owner, TypedIndex shapeIndex)
        {
            Owner = owner;
            ShapeIndex = shapeIndex;
        }
    }
}
