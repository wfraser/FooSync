///
/// Codewise/FooSync/WPFApp/ObservableDictionary.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
///

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Codewise.FooSync.WPFApp
{
    public class ObservableDictionary<K,V> : IDictionary<K,V>, IDictionary, INotifyCollectionChanged
    {
        private Dictionary<K, V> _dictionary;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public ObservableDictionary()
        {
            _dictionary = new Dictionary<K, V>();
        }

        public ObservableDictionary(IDictionary<K, V> otherDictionary)
        {
            _dictionary = new Dictionary<K, V>(otherDictionary);
        }

        public ICollection<K> Keys
        {
            get { return _dictionary.Keys; }
        }

        ICollection System.Collections.IDictionary.Keys
        {
            get { return (ICollection)_dictionary.Keys; }
        }

        public ICollection<V> Values
        {
            get { return _dictionary.Values; }
        }

        ICollection System.Collections.IDictionary.Values
        {
            get { return (ICollection)_dictionary.Values; }
        }

        public V this[K key]
        {
            get { return _dictionary[key]; }
            set
            {
                V oldValue = _dictionary[key];
                _dictionary[key] = value;

                if (CollectionChanged != null)
                {
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Replace,
                        new KeyValuePair<K,V>(key, oldValue),
                        new KeyValuePair<K,V>(key, value)));
                }
            }
        }

        public object this[object key]
        {
            get { return this[(K)key]; }
            set { this[(K)key] = (V)value; }
        }

        public void Add(K key, V value)
        {
            _dictionary.Add(key, value);

            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, key));
            }
        }

        public void Add(object key, object value)
        {
            Add((K)key, (V)value);
        }

        public void Add(KeyValuePair<K, V> pair)
        {
            _dictionary.Add(pair.Key, pair.Value);

            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, pair.Key));
            }
        }

        public void Clear()
        {
            _dictionary.Clear();

            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null));
            }
        }

        public bool ContainsKey(K key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool Contains(object key)
        {
            return ContainsKey((K)key);
        }

        public bool Contains(KeyValuePair<K, V> pair)
        {
            return _dictionary.ContainsKey(pair.Key) && _dictionary[pair.Key].Equals(pair.Value);
        }

        public bool Remove(K key)
        {
            bool result = _dictionary.Remove(key);

            if (result && CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, key));
            }

            return result;
        }

        public void Remove(object key)
        {
            Remove((K)key);
        }

        public bool Remove(KeyValuePair<K, V> pair)
        {
            if (_dictionary.ContainsKey(pair.Key) && _dictionary[pair.Key].Equals(pair.Value))
            {
                _dictionary.Remove(pair.Key);

                if (CollectionChanged != null)
                {
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, pair.Key));
                }

                return true;
            }

            return false;
        }

        public bool TryGetValue(K key, out V value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int startIndex)
        {
            int i = 0;
            foreach (KeyValuePair<K, V> pair in _dictionary)
            {
                array[startIndex + i] = pair;
                i++;
            }
        }

        public void CopyTo(Array array, int startIndex)
        {
            throw new NotImplementedException();
        }

        public object SyncRoot
        {
            get { return _dictionary; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public int Count
        {
            get { return _dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool IsFixedSize
        {
            get { return false; }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (System.Collections.IEnumerator)_dictionary.GetEnumerator();
        }

        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator()
        {
            return (System.Collections.IDictionaryEnumerator)_dictionary.GetEnumerator();
        }

        public IEnumerator<KeyValuePair<K,V>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }
    }
}
