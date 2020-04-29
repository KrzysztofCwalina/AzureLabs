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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        /// <remarks>If any of the keys are null or empty, the method will return false.</remarks>
        public static bool TryCreate(string[] keys, out PerfectHash hash)
        {
            if (keys == null) throw new ArgumentNullException(nameof(keys));
            
            int[] codes = new int[keys.Length];
            for (int i=0; i<codes.Length; i++)
            {
                var key = keys[i];
                if (string.IsNullOrEmpty(key))
                {
                    hash = default;
                    return false;
                }
                codes[i] = ComputeCode(key);
            }

            if (AreUnique(codes, out int min, out int max))
            {
                var size = max - min + 1;
                if (size < 256 * 256) // TODO: isn't it too large?
                {
                    hash = new PerfectHash(min, size);
                    return true;
                }
            }

            hash = default;
            return false;
        }

        public int ComputeHash(string key)
        {
            var code = ComputeCode(key) - _min;
            if (code < _size) return code;
            return (code) % _size;
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

        static int ComputeCode(string key)
        {
            Debug.Assert(key != null && key.Length > 0);
            var first = key[0] - 'A'; // typical range 0-25 (5 bits) Better use PascalCasing!
            var last = key[key.Length - 1] - '0'; // typical range 0-74 (7 bits) ... and no undescores
            last <<= 5;
            var code = (first | last) & 0xFFF; // 0xFFF is 12 (7 + 5 ) bits set
            return code;
        }
    }

    public class PerfectHashStore : PropertyStore
    {
        object[] _values;
        readonly PerfectHash _hash;

        public static PropertyStore Create(IReadOnlyDictionary<string, object> properties)
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

            // TODO: this should log very time a non-perfect store is created 
            return new DictionaryStore(properties);
        }
        private PerfectHashStore(PerfectHash hash, IReadOnlyDictionary<string, object> properties)
        {
            _hash = hash;
            _values = new object[_hash.Size];
            foreach (var property in properties)
            {
                var index = _hash.ComputeHash(property.Key);
                _values[index] = property.Value;
            }
        }

        protected sealed internal override bool IsReadOnly => true;

        protected sealed internal override bool TryGetValue(string propertyName, out object propertyValue)
        {
            propertyValue = _values[_hash.ComputeHash(propertyName)];
            return propertyValue != null;
        }

        protected sealed internal override DynamicData CreateDynamicData(ReadOnlySpan<(string propertyName, object propertyValue)> properties)
            => throw new NotImplementedException();

        protected sealed internal override void SetValue(string propertyName, object propertyValue)
            => throw new NotImplementedException();

        protected sealed internal override bool TryConvertTo(Type type, out object converted)
            => throw new NotImplementedException();

        protected sealed internal override bool TryGetValueAt(int index, out object item)
            => throw new NotImplementedException();

        protected sealed internal override IEnumerable<string> PropertyNames
            => throw new NotImplementedException();
    }
}
