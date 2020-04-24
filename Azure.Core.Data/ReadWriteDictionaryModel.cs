using System.Collections.Generic;
using System.Collections;
using System;

namespace Azure.Data
{
    internal class ReadWriteDictionaryData : SchematizedModel, IDictionary<string, object>, IReadOnlyDictionary<string, object>
    {
        private IDictionary<string, object> _properties;

        public ReadWriteDictionaryData() => _properties = new Dictionary<string, object>(StringComparer.Ordinal);

        public ReadWriteDictionaryData(ModelSchema schema)
            : base(schema)
        {
            _properties = new Dictionary<string, object>(StringComparer.Ordinal);
        }

        public ReadWriteDictionaryData(IDictionary<string, object> properties) => _properties = properties;

        public ReadWriteDictionaryData(params (string propertyName, object propertyValue)[] properties)
            : this(properties.AsSpan())
        { }

        public ReadWriteDictionaryData(ReadOnlySpan<(string propertyName, object propertyValue)> properties)
        {
            var dictionary = new Dictionary<string, object>(properties.Length, StringComparer.Ordinal);
            foreach (var property in properties)
            {
                dictionary.Add(property.propertyName, property.propertyValue);
            }
            _properties = dictionary;
        }

        protected override ReadOnlyModel CreateCore(ReadOnlySpan<(string propertyName, object propertyValue)> properties)
            => new ReadWriteDictionaryData(properties);

        protected override void SetPropertyCore(string propertyName, object propertyValue)
        {
            _properties[propertyName] = propertyValue;
        }

        protected override bool TryGetPropertyCore(string propertyName, out object propertyValue) => _properties.TryGetValue(propertyName, out propertyValue);

        protected override bool TryGetAtCore(int index, out object item)
        {
            throw new NotImplementedException();
        }

        protected override bool TryConvertToCore(Type type, out object converted)
            => ReadOnlyDictionaryModel.TryConvertTo(_properties, type, out converted);

        public override IEnumerable<string> PropertyNames => _properties.Keys;

        #region IDictionary Implementation
        void IDictionary<string, object>.Add(string key, object value)
        {
            _properties.Add(key, value);
        }
        bool IDictionary<string, object>.ContainsKey(string key) => _properties.ContainsKey(key);
        bool IDictionary<string, object>.Remove(string key)
        {
            return _properties.Remove(key);
        }
        bool IDictionary<string, object>.TryGetValue(string key, out object value) => _properties.TryGetValue(key, out value);
        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item) => _properties.Add(item);
        void ICollection<KeyValuePair<string, object>>.Clear() => _properties.Clear();
        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item) => _properties.Contains(item);
        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => _properties.CopyTo(array, arrayIndex);
        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item) => _properties.Remove(item);
        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() => _properties.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _properties.GetEnumerator();
        bool IReadOnlyDictionary<string, object>.ContainsKey(string key) => _properties.ContainsKey(key);
        bool IReadOnlyDictionary<string, object>.TryGetValue(string key, out object value) => _properties.TryGetValue(key, out value);
        ICollection<string> IDictionary<string, object>.Keys => _properties.Keys;
        ICollection<object> IDictionary<string, object>.Values => _properties.Values;
        int ICollection<KeyValuePair<string, object>>.Count => _properties.Count;
        IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => _properties.Keys;
        IEnumerable<object> IReadOnlyDictionary<string, object>.Values => _properties.Values;
        int IReadOnlyCollection<KeyValuePair<string, object>>.Count => _properties.Count;
        bool ICollection<KeyValuePair<string, object>>.IsReadOnly => false;
        object IDictionary<string, object>.this[string key] { get => _properties[key]; set => _properties[key] = value; }
        #endregion
    }
}