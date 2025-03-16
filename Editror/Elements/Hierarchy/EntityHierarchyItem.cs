using System.ComponentModel;
using AtomEngine;
using System;
using System.Collections.Generic;

namespace Editor
{
    public struct EntityHierarchyItem : INotifyPropertyChanged, IEquatable<EntityHierarchyItem>
    {
        private bool isNull = true;
        private Entity _entity;

        public event PropertyChangedEventHandler? PropertyChanged;

        public EntityHierarchyItem(uint id, uint version, string name)
        {
            _entity = new Entity(id, Version);
            Name = name;
            isNull = false;
            ParentId = null;
            Children = new List<uint>();
            IsExpanded = true;
            Level = 0;
        }
        public EntityHierarchyItem(Entity entity, string name)
        {
            Name = name;
            _entity = entity;
            isNull = false;
            ParentId = null;
            Children = new List<uint>();
            IsExpanded = true;
            Level = 0;
        }

        public uint Id => _entity.Id;
        public uint Version => _entity.Version;
        public Entity EntityReference => _entity;
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }

        private uint? _parentId;
        public uint? ParentId
        {
            get => _parentId;
            set
            {
                if (_parentId != value)
                {
                    _parentId = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ParentId)));
                }
            }
        }

        private List<uint> _children;
        public List<uint> Children
        {
            get => _children;
            set
            {
                _children = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Children)));
            }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
                }
            }
        }

        private int _level;
        public int Level
        {
            get => _level;
            set
            {
                if (_level != value)
                {
                    _level = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Level)));
                }
            }
        }

        public bool IsActive { get; set; } = true;
        public bool IsVisible { get; set; } = true;

        public override int GetHashCode() => Id.GetHashCode();

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (obj is EntityHierarchyItem other)
            {
                return Id == other.Id && Version == other.Version;
            }
            return false;
        }

        public override string ToString() =>
            $"{_entity} Name:{Name} IsActive:{IsActive} IsVisible:{IsVisible} Level:{Level}";

        public bool Equals(EntityHierarchyItem other) => Id == other.Id && Version == other.Version && isNull == other.isNull;
        public static EntityHierarchyItem Null => new EntityHierarchyItem() { isNull = true };
        public static bool operator ==(EntityHierarchyItem left, EntityHierarchyItem right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);
            return left.Equals(right);
        }

        public static bool operator !=(EntityHierarchyItem left, EntityHierarchyItem right)
            => !(left == right);
    }

}