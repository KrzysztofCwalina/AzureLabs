using System;
using System.Collections;
using System.Collections.Generic;

namespace Azure.Data
{
    public abstract class DataConverter : IReadOnlyDictionary<Type, DataConverter>
    {
        public abstract Type ForType { get; }

        public IEnumerable<Type> Keys => throw new NotImplementedException();

        public IEnumerable<DataConverter> Values => throw new NotImplementedException();

        public int Count => throw new NotImplementedException();

        public DataConverter this[Type key] => throw new NotImplementedException();

        public abstract DynamicData ConvertToDataType(object obj);
        public abstract object ConverFromDataType(DynamicData data);

        public bool ContainsKey(Type key)
            => ForType == key;

        public bool TryGetValue(Type key, out DataConverter value)
        {
            if(key == ForType)
            {
                value = this;
                return true;
            }
            value = default;
            return false;
        }

        public IEnumerator<KeyValuePair<Type, DataConverter>> GetEnumerator()
        {
            yield return new KeyValuePair<Type, DataConverter>(ForType, this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return new KeyValuePair<Type, DataConverter>(ForType, this);
        }

        internal static IReadOnlyDictionary<Type, DataConverter> CommonConverters = new Dictionary<Type, DataConverter>()
        {
            {  typeof(DateTime), new DateTimeConverter() },
            { typeof(DateTimeOffset), new DateTimeOffsetConverter() }
        };
    }

    sealed class DateTimeConverter : DataConverter
    {
        public override Type ForType => typeof(DateTime);

        public override object ConverFromDataType(DynamicData data)
        {
            if (DateTime.TryParse(data.ToString(), out var dt))
            {
                return dt;
            }
            throw new InvalidOperationException();
        }

        public override DynamicData ConvertToDataType(object obj)
        {
            var dt = (DateTime)obj;
            var data = new DynamicData(dt.ToString("O"), this);
            return data;
        }
    }

    sealed class DateTimeOffsetConverter : DataConverter
    {
        public override Type ForType => typeof(DateTimeOffset);

        public override object ConverFromDataType(DynamicData data)
        {
            if (DateTimeOffset.TryParse(data.ToString(), out var dt))
            {
                return dt;
            }
            throw new InvalidOperationException();
        }

        public override DynamicData ConvertToDataType(object obj)
        {
            var dt = (DateTimeOffset)obj;
            var data = new DynamicData(dt.ToString("O"), this);
            return data;
        }
    }
}