using Azure.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Azure.Core.Data.DataStores
{
    readonly struct PerfectHash
    {
        readonly int _min;
        readonly int _size;

        private PerfectHash(int min, int size) {
            _min = min;
            _size = size;
        }
        public int Size => _size;

        public static bool TryCreate(string[] keys, out PerfectHash hash)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            
            int[] codes = new int[keys.Length];
            for (int i=0; i<codes.Length; i++)
            {
                codes[i] = ComputeCode(keys[i]);
            }

            if (AreUnique(codes, out int min, out int max))
            {
                var size = max - min + 1;
                if (size < keys.Length * 5 && size < 256 * 256)
                {
                    hash = new PerfectHash(min, size);
                    return true;
                }
            }

            hash = default;
            return false;
        }

        static bool AreUnique(int[] values, out int min, out int max)
        {
            Array.Sort(values);
            if (values.Length == 0)
            {
                min = 0;
                max = 0;
                return true;
            }
            if (values.Length == 1)
            {
                min = values[0];
                max = values[0];
                return true;
            }
            min = values[0];

            int prev = values[0];
            max = 0;
            for(int i=1; i<values.Length; i++)
            {
                max = values[i];
                if (prev == max) return false;
                prev = max;
            }
            return true;
        }
        public int ComputeHash(string key)
        {
            var code = ComputeCode(key) - _min;
            if (code < _size) return code;
            return (code) % _size;
        }

        public static int ComputeCode(string key)
        {
            Debug.Assert(key != null && key.Length > 0);
            return key[0];
        }
    }

    public class PerfectHashStore : DataStore
    {
        object[] _values;
        readonly PerfectHash _hash;

        public static DataStore Create(IDictionary<string, object> properties)
        {
            var names = new string[properties.Count];
            int i = 0;
            foreach (var property in properties)
            {
                names[i++] = property.Key;
            }

            if(PerfectHash.TryCreate(names, out var hash))
            {
                return new PerfectHashStore(hash, properties);
            }

            return default;
        }
        private PerfectHashStore(PerfectHash hash, IDictionary<string, object> properties)
        {
            _hash = hash;
            _values = new object[_hash.Size];
            foreach (var property in properties)
            {
                var index = _hash.ComputeHash(property.Key);
                _values[index] = property.Value;
            }
        }

        protected internal override bool IsReadOnly => true;

        protected internal override bool TryGetValue(string propertyName, out object propertyValue)
        {
            propertyValue = _values[_hash.ComputeHash(propertyName)];
            return propertyValue != null;
        }

        protected internal override DynamicData CreateDynamicData(ReadOnlySpan<(string propertyName, object propertyValue)> properties)
            => throw new NotImplementedException();

        protected internal override void SetValue(string propertyName, object propertyValue)
            => throw new NotImplementedException();

        protected internal override bool TryConvertTo(Type type, out object converted)
            => throw new NotImplementedException();

        protected internal override bool TryGetValueAt(int index, out object item)
            => throw new NotImplementedException();

        protected internal override IEnumerable<string> PropertyNames
            => throw new NotImplementedException();
    }
}
