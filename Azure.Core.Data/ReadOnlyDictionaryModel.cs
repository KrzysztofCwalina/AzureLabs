using System.Collections.Generic;
using System.Collections;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Azure.Data
{
    internal class ReadOnlyDictionaryModel : ReadOnlyModel, IReadOnlyDictionary<string, object>
    {
        private IReadOnlyDictionary<string, object> _properties;

        public static ReadOnlyDictionaryModel Empty { get; } = new ReadOnlyDictionaryModel(new Dictionary<string, object>());

        public ReadOnlyDictionaryModel(params (string propertyName, object propertyValue)[] properties)
            : this(properties.AsSpan()) 
        { }

        // TODO: This is a backdoor for complext object. How many more of these we have? 
        public ReadOnlyDictionaryModel(ReadOnlySpan<(string propertyName, object propertyValue)> properties)
        {
            Debug.Assert(properties.Length > 0); // use ReadOnlyDictionaryData.Empty instead.
            var dictionary = new Dictionary<string, object>(properties.Length);
            foreach (var property in properties)
            {
                dictionary.Add(property.propertyName, property.propertyValue);
            }
            _properties = dictionary;
        }

        // TODO: This is a backdoor for complext object. How many more of these we have? 
        public ReadOnlyDictionaryModel(IReadOnlyDictionary<string, object> properties) => _properties = properties;

        // TODO: This is a backdoor for complext object. How many more of these we have? 
        protected override ReadOnlyModel CreateCore(ReadOnlySpan<(string propertyName, object propertyValue)> properties)
            => new ReadOnlyDictionaryModel(properties);

        protected override bool TryGetPropertyCore(string propertyName, out object propertyValue) => _properties.TryGetValue(propertyName, out propertyValue);

        // TODO: implement
        protected override bool TryGetAtCore(int index, out object item) => throw new NotImplementedException();

        protected override bool TryConvertToCore(Type type, out object converted) => TryConvertTo(_properties, type, out converted);

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

        public override IEnumerable<string> PropertyNames => _properties.Keys;

        // TODO: can this be moved to base class?
        #region IReadOnlyDictionary Implementation
        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() => _properties.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _properties.GetEnumerator();
        bool IReadOnlyDictionary<string, object>.ContainsKey(string key) => _properties.ContainsKey(key);
        bool IReadOnlyDictionary<string, object>.TryGetValue(string key, out object value) => _properties.TryGetValue(key, out value);
        IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => _properties.Keys;
        IEnumerable<object> IReadOnlyDictionary<string, object>.Values => _properties.Values;
        int IReadOnlyCollection<KeyValuePair<string, object>>.Count => _properties.Count;
        #endregion
    }
}