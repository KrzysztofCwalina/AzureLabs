using System.Collections.Generic;
using System.Text;
using System;
using System.Reflection;

namespace Azure.Data
{
    class DictionaryStore : PropertyStore
    {
        Dictionary<string, object> _properties = new Dictionary<string, object>(StringComparer.Ordinal);
        bool _readonly = false;

        public DictionaryStore() { }

        public DictionaryStore(IReadOnlyDictionary<string, object> properties)
        {
            _properties = new Dictionary<string, object>();
            foreach (var property in properties)
            {
                _properties[property.Key] = property.Value;
            }
        }

        protected internal override bool IsReadOnly => false;

        protected internal override IEnumerable<string> PropertyNames => _properties.Keys;

        internal void Freeze() => _readonly = true;

        protected internal override DynamicData CreateDynamicData(ReadOnlySpan<(string propertyName, object propertyValue)> properties)
        {
            var result = new DictionaryStore();
            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                result.SetValue(property.propertyName, property.propertyValue);
            }
            if (_readonly) result.Freeze();
            return new DynamicData(result);
        }

        protected internal override void SetValue(string propertyName, object propertyValue)
        {
            if (_readonly) throw new InvalidOperationException("The data is read-only");
            _properties[propertyName] = propertyValue;
        }

        protected internal override bool TryConvertTo(Type type, out object converted)
        {
            try
            {
                converted = Activator.CreateInstance(type);
                foreach (var property in _properties)
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

        protected internal override bool TryGetValueAt(int index, out object item)
        {
            throw new NotImplementedException();
        }

        protected internal override bool TryGetValue(string propertyName, out object propertyValue)
            => _properties.TryGetValue(propertyName, out propertyValue);

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (var property in _properties)
            {
                if (first) first = false;
                else sb.Append(",\n");
                sb.Append($"\t{property.Key} : {property.Value}");
            }
            sb.Append("\n}");

            return sb.ToString();
        }
    }
}