using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Sylvanas.Web
{
    public class NameValueCollectionWrapper : INameValueCollection
    {
        private readonly NameValueCollection _data;

        public NameValueCollectionWrapper(NameValueCollection data)
        {
            _data = data;
        }

        public IEnumerator GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            _data.CopyTo(array, index);
        }

        public int Count => _data.Count;
        public object SyncRoot => _data;
        public bool IsSynchronized => false;
        public object Original => _data;

        string INameValueCollection.this[int index] => _data[index];

        string INameValueCollection.this[string name]
        {
            get { return _data[name]; }
            set { _data[name] = value; }
        }

        public string[] AllKeys => _data.AllKeys;

        public void Add(string name, string value)
        {
            _data.Add(name, value);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public string Get(int index)
        {
            return _data.Get(index);
        }

        public string Get(string name)
        {
            return _data.Get(name);
        }

        public string GetKey(int index)
        {
            return _data.GetKey(index);
        }

        public string[] GetValues(string name)
        {
            return _data.GetValues(name);
        }

        public bool HasKeys()
        {
            return _data.HasKeys();
        }

        public void Remove(string name)
        {
            _data.Remove(name);
        }

        public void Set(string name, string value)
        {
            _data.Set(name, value);
        }

        public override string ToString()
        {
            return _data.ToString();
        }
    }

    public static class NameValueCollectionWrapperExtensions
    {
        public static NameValueCollectionWrapper InWrapper(this NameValueCollection nvc)
        {
            return new NameValueCollectionWrapper(nvc);
        }

        public static NameValueCollection ToNameValueCollection(this INameValueCollection nvc)
        {
            return (NameValueCollection) nvc.Original;
        }

        public static Dictionary<string, string> ToDictionary(this INameValueCollection nameValues)
        {
            return ToDictionary((NameValueCollection)nameValues.Original);
        }

        public static Dictionary<string, string> ToDictionary(this NameValueCollection nameValues)
        {
            if (nameValues == null)
            {
                return new Dictionary<string, string>();
            }

            var map = new Dictionary<string, string>();
            foreach (var key in nameValues.AllKeys)
            {
                if (key == null)
                {
                    //occurs when no value is specified, e.g. 'path/to/page?debug'
                    //throw new ArgumentNullException("key", "nameValues: " + nameValues);
                    continue;
                }

                var values = nameValues.GetValues(key);
                if (values != null && values.Length > 0)
                {
                    map[key] = string.Join(",", values);
                }
            }

            return map;
        }

        public static NameValueCollection ToNameValueCollection(this Dictionary<string, string> map)
        {
            if (map == null)
            {
                return new NameValueCollection();
            }

            var nameValues = new NameValueCollection();
            foreach (var item in map)
            {
                nameValues.Add(item.Key, item.Value);
            }

            return nameValues;
        }
    }
}