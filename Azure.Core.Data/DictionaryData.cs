using System.Collections.Generic;
using System.Collections;
using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Reflection;

namespace Azure.Data
{
    internal class ReadOnlyDictionaryData : DynamicData, IReadOnlyDictionary<string, object>
    {
        private IReadOnlyDictionary<string, object> _properties;

        public static ReadOnlyDictionaryData Empty { get; } = new ReadOnlyDictionaryData(new Dictionary<string, object>());

        // TODO: This is a backdoor for complext object. How many more of these we have? 
        public ReadOnlyDictionaryData(params (string propertyName, object propertyValue)[] properties)
        {
            Debug.Assert(properties.Length > 0); // use ReadOnlyDictionaryData.Empty instead.
            var dictionary = new Dictionary<string, object>(properties.Length);
            foreach (var property in properties)
            {
                dictionary.Add(property.propertyName, property.propertyValue);
            }
            _properties = dictionary;
        }

        public ReadOnlyDictionaryData(IReadOnlyDictionary<string, object> properties) => _properties = properties;

        public override bool IsReadOnly => true;

        protected override void SetPropertyCore(string propertyName, object propertyValue) => ThrowReadOnlyException();

        protected override bool TryGetPropertyCore(string propertyName, out object propertyValue) => _properties.TryGetValue(propertyName, out propertyValue);

        protected override bool TryConvertToCore(Type type, out object converted)
            => TryConvertTo(_properties, type, out converted);

        internal static bool TryConvertTo(IEnumerable<KeyValuePair<string, object>> properties, Type type, out object converted)
        {
            try
            {
                converted = Activator.CreateInstance(type);
                foreach (var property in properties)
                {
                    PropertyInfo propertyInfo = type.GetProperty(property.Key, BindingFlags.Public | BindingFlags.Instance);
                    propertyInfo.SetValue(converted, property.Value);
                    // TDOO: this needs to deserialize complex objects
                }
                return true;
            }
            catch
            {
                converted = default;
                return false;
            }
        }

        protected override IEnumerable<string> PropertyNames => _properties.Keys;

        #region IDictionary Implementation
        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() => _properties.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _properties.GetEnumerator();
        bool IReadOnlyDictionary<string, object>.ContainsKey(string key) => _properties.ContainsKey(key);
        bool IReadOnlyDictionary<string, object>.TryGetValue(string key, out object value) => _properties.TryGetValue(key, out value);
        IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => _properties.Keys;
        IEnumerable<object> IReadOnlyDictionary<string, object>.Values => _properties.Values;
        int IReadOnlyCollection<KeyValuePair<string, object>>.Count => _properties.Count;
        #endregion
    }

    internal class ReadWriteDictionaryData : DynamicData, IDictionary<string, object>, IReadOnlyDictionary<string, object>
    {
        private IDictionary<string, object> _properties;

        public ReadWriteDictionaryData() => _properties = new Dictionary<string, object>();

        public ReadWriteDictionaryData(IDictionary<string, object> properties) => _properties = properties;

        public ReadWriteDictionaryData(params (string propertyName, object propertyValue)[] properties)
        {
            _properties = new Dictionary<string, object>(properties.Length);
            foreach (var property in properties)
            {
                _properties.Add(property.propertyName, property.propertyValue);
            }
        }

        public override bool IsReadOnly => false;

        protected override void SetPropertyCore(string propertyName, object propertyValue)
        {
            _properties[propertyName] = propertyValue;
        }

        protected override bool TryGetPropertyCore(string propertyName, out object propertyValue) => _properties.TryGetValue(propertyName, out propertyValue);

        protected override bool TryConvertToCore(Type type, out object converted)
            => ReadOnlyDictionaryData.TryConvertTo(_properties, type, out converted);

        protected override IEnumerable<string> PropertyNames => _properties.Keys;

        #region IDictionary Implementation
        void IDictionary<string, object>.Add(string key, object value)
        {
            EnsureNotReadOnly();
            _properties.Add(key, value);
        }
        bool IDictionary<string, object>.ContainsKey(string key) => _properties.ContainsKey(key);
        bool IDictionary<string, object>.Remove(string key)
        {
            EnsureNotReadOnly();
            return _properties.Remove(key);
        }
        bool IDictionary<string, object>.TryGetValue(string key, out object value) => _properties.TryGetValue(key, out value);
        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            EnsureNotReadOnly();
            ((ICollection<KeyValuePair<string, object>>)_properties).Add(item);
        }
        void ICollection<KeyValuePair<string, object>>.Clear()
        {
            EnsureNotReadOnly();
            _properties.Clear();
        }
        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item) => ((ICollection<KeyValuePair<string, object>>)_properties).Contains(item);
        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, object>>)_properties).CopyTo(array, arrayIndex);
        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            EnsureNotReadOnly();
            return ((ICollection<KeyValuePair<string, object>>)_properties).Remove(item);
        }
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
        #endregion
    }
}