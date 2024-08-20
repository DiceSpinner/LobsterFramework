using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework.Utility
{
    /// <summary>
    /// A wrapper class of an array of serializable objects.
    /// </summary>
    /// <typeparam name="T">The type of serializable object to be stored in the array</typeparam>
    [Serializable]
    public class SerializableList<T> : IList<T>, IEnumerable<T>, ICollection<T>
    {
        [SerializeField] protected List<T> items = new();

        public T this[int index] { get => items[index]; set => ((IList<T>)items)[index] = value; }

        public int Count => items.Count;

        public bool IsReadOnly => ((ICollection<T>)items).IsReadOnly;

        public void Add(T item)
        {
            ((ICollection<T>)items).Add(item);
        }

        public void Clear()
        {
            ((ICollection<T>)items).Clear();
        }

        public bool Contains(T item)
        {
            return ((ICollection<T>)items).Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ((ICollection<T>)this.items).CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)items).GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return ((IList<T>)items).IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            ((IList<T>)items).Insert(index, item);
        }

        public bool Remove(T item)
        {
            return ((ICollection<T>)items).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<T>)items).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }
    }

    /// <summary>
    /// A serializable list of <see cref="Component"/>
    /// </summary>
    [Serializable]
    public class ComponentList : SerializableList<Component> { }
}
