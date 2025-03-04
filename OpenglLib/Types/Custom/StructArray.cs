﻿using AtomEngine;
using Silk.NET.OpenGL;
using System.Collections;

namespace OpenglLib
{
    public class StructArray<T> : IEnumerable<T> where T : CustomStruct
    {
        public int Location = -1;
        private T[] array;
        private GL _gl;

        public StructArray(int size, GL gL)
        {
            array = new T[size];
            Type t = typeof(T);
            for (int i = 0; i < size; i++)
            {
                array[i] = (T)Activator.CreateInstance(t, gL);
            }

            _gl = gL;
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    DebLogger.Error("Index out of Range");
                    return default;
                }
                return array[index];
            }
            set
            {
                if (index < 0 || index >= Count)
                {
                    DebLogger.Error("Index out of Range");
                    return;
                }
                if (Location == -1)
                {
                    DebLogger.Warn("You try to set value to -1 lcation field");
                    return;
                }

                array[index] = value;
            }
        }

        public int Count => array.Length;
        public T[] ToArray() => (T[])array.Clone();
        public bool Contains(T item) => Array.Exists(array, element => EqualityComparer<T>.Default.Equals(element, item));

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
                yield return array[i];
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
