using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json;
using System.Linq;
using System;

namespace Editor
{
    internal class WorldData
    {
        public string WorldName { get; set; } = "World_0";
        public List<EntityData> Entities { get; set; } = new List<EntityData>();
        public List<SystemDescriptor> SystemDescriptors { get; set; } = new List<SystemDescriptor>();
        [JsonIgnore]
        public bool IsDirty { get; set; }
    }

    public class ThreadSafeList<T> : ICollection<T>
    {
        private readonly List<T> _list = new List<T>();
        private readonly object _lock = new object();

        public T this[int index]
        {
            get
            {
                lock (_lock)
                {
                    return _list[index];
                }
            }
            set
            {
                lock (_lock)
                {
                    _list[index] = value;
                }
            }
        }

        public void Add(T item)
        {
            lock (_lock)
            {
                _list.Add(item);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _list.Clear();
            }
        }

        public bool Contains(T item)
        {
            lock (_lock)
            {
                return _list.Contains(item);
            }
        }

        public bool Remove(T item)
        {
            lock (_lock)
            {
                return _list.Remove(item);
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _list.Count;
                }
            }
        }

        public List<T> ToList()
        {
            lock (_lock)
            {
                return new List<T>(_list);
            }
        }

        public void FromList(List<T> list)
        {
            lock (_lock)
            {
                _list.Clear();
                _list.AddRange(list);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_lock)
            {
                _list.CopyTo(array, arrayIndex);
            }
        }

        public bool IsReadOnly => false;

        public IEnumerator<T> GetEnumerator()
        {
            lock (_lock)
            {
                return _list.ToList().GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    public class ThreadSafeListConverter<T> : JsonConverter<ThreadSafeList<T>>
    {
        public override ThreadSafeList<T> ReadJson(JsonReader reader, Type objectType, ThreadSafeList<T> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var list = serializer.Deserialize<List<T>>(reader);
            var threadSafeList = new ThreadSafeList<T>();
            threadSafeList.FromList(list);
            return threadSafeList;
        }

        public override void WriteJson(JsonWriter writer, ThreadSafeList<T> value, JsonSerializer serializer)
        {
            var list = value.ToList();
            serializer.Serialize(writer, list);
        }
    }

}
