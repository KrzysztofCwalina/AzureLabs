using System.Collections.Generic;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace Azure.Data
{
    public class JsonData 
    {
        private JsonData() { }

        public static Data Create(string s_demo_payload)
        {
            var store = new ReadOnlyJsonStore(s_demo_payload);
            return new Data(store);
        }
        public static DataStore CreateStore(string s_demo_payload)
        {
            var store = new ReadOnlyJsonStore(s_demo_payload);
            return store;
        }
    }

    public class ReadOnlyJsonStore : DataStore
    {
        private JsonElement _json;
        private object _originalData;
        private bool _deserialized;

        public ReadOnlyJsonStore(string jsonObject)
        {
            _originalData = jsonObject;
        }

        public ReadOnlyJsonStore(Stream jsonObject)
        {
            _originalData = jsonObject;

        }

        public ReadOnlyJsonStore(ReadOnlyJsonStore copy)
        {
            _json = copy._json;
            _originalData = copy._originalData;
            _deserialized = copy._deserialized;
        }

        private void Deserialize()
        {
            Debug.Assert(_deserialized == false);
            var jsonString = _originalData as string;
            if (jsonString != null)
            {
                var document = JsonDocument.Parse(jsonString);
                _json = document.RootElement;
            }
            else
            {
                var jsonStream = _originalData as Stream;
                var document = JsonDocument.Parse(jsonStream);
                _json = document.RootElement;
            }
            if (_json.ValueKind != JsonValueKind.Object && _json.ValueKind != JsonValueKind.Array) throw new InvalidOperationException("JSON is not an object or array");
            _deserialized = true;
        }
        private JsonElement GetJsonElement()
        {
            if (!_deserialized) Deserialize();
            return _json;
        }
        public static async Task<Data> CreateAsync(Stream json, CancellationToken cancellationToken)
        {
            var document = await JsonDocument.ParseAsync(json, default, cancellationToken).ConfigureAwait(false);
            return new Data(new ReadOnlyJsonStore(document.RootElement));
        }

        public ReadOnlyJsonStore(JsonElement jsonObject)
        {
            if (jsonObject.ValueKind != JsonValueKind.Object && jsonObject.ValueKind != JsonValueKind.Array) throw new InvalidOperationException("JSON is not an object or array");
            _json = jsonObject;
            _deserialized = true;
        }

        protected internal override Data CreateCore(ReadOnlySpan<(string propertyName, object propertyValue)> properties)
            => throw new NotImplementedException();

        protected internal override IEnumerable<string> PropertyNames {
            get {
                JsonElement element = GetJsonElement();
                if (element.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in element.EnumerateObject())
                    {
                        yield return property.Name;
                    }
                }
            }
        }

        protected internal override bool TryGetPropertyCore(string propertyName, out object propertyValue)
        {
            JsonElement json = GetJsonElement();
            if (!json.TryGetProperty(propertyName, out JsonElement element))
            {
                propertyValue = default;
                return false;
            }

            return TryGetValue(element, out propertyValue);
        }

        private bool TryGetValue(JsonElement element, out object value, Type type = default)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    value = element.GetString();
                    break;
                case JsonValueKind.False:
                    value = false;
                    break;
                case JsonValueKind.True:
                    value = true;
                    break;
                case JsonValueKind.Null:
                    value = null;
                    break;
                case JsonValueKind.Object:
                    value = new Data(new ReadOnlyJsonStore(element));
                    break;
                case JsonValueKind.Number:
                    if (type == default)
                    {
                        if (element.TryGetDouble(out var doubleValue))
                        {
                            value = doubleValue;
                            break;
                        }
                    }
                    if (type == typeof(object))
                    {
                        value = BestFitNumber(element);
                        break;
                    }
                    throw new NotImplementedException();
                case JsonValueKind.Array:
                    value = new Data(new ReadOnlyJsonStore(element));
                    break;
                default:
                    throw new NotImplementedException("this should never happen");
            }
            return true;
        }

        private object BestFitNumber(JsonElement element)
        {
            // TODO: is this what we want? i.e. we return the smallest integer if the value fits, the floats, the decimal.
            if (element.TryGetUInt64(out var ulongValue))
            {
                if (ulongValue <= uint.MaxValue)
                {
                    if (ulongValue <= ushort.MaxValue)
                    {
                        if (ulongValue <= byte.MaxValue)
                        {
                            return (byte)ulongValue;
                        }

                        return (ushort)ulongValue;
                    }

                    return (uint)ulongValue;
                }

                return ulongValue;
            }

            // the value is negative
            if (element.TryGetInt64(out var longValue))
            {
                if (longValue >= int.MinValue)
                {
                    if (longValue >= short.MinValue)
                    {
                        if (longValue >= sbyte.MinValue)
                        {
                            return (sbyte)longValue;
                        }

                        return (short)longValue;
                    }

                    return (int)longValue;
                }

                return longValue;
            }

            if (element.TryGetSingle(out var singleValue))
            {
                return singleValue;
            }

            if (element.TryGetDouble(out var doubleValue))
            {
                return doubleValue;
            }

            if (element.TryGetDecimal(out var decimalValue))
            {
                return decimalValue;
            }
            throw new NotImplementedException("this should never happen");
        }
        protected internal override bool TryGetAtCore(int index, out object item)
        {
            item = default;
            JsonElement json = GetJsonElement();
            if (json.ValueKind != JsonValueKind.Array) return false;
            JsonElement itemElement = json[index];
            return TryGetValue(itemElement, out item);
        }

        public object ToType(Type type)
        {
            JsonElement json = GetJsonElement();
            var result = JsonSerializer.Deserialize(json.GetRawText(), type);
            return result;
        }

        protected internal override bool TryConvertToCore(Type type, out object converted)
        {
            JsonElement json = GetJsonElement();
            if (json.ValueKind == JsonValueKind.Array && !DataSchema.IsPrimitiveArray(type))
            {
                var items = json.GetArrayLength();
                var array = new Data[items];
                int index = 0;
                foreach (var item in json.EnumerateArray())
                {
                    array[index++] = new Data(new ReadOnlyJsonStore(item)); // TODO: this will throw for primitives.
                }
                converted = array;
                return true;
            }
            try
            {
                converted = JsonSerializer.Deserialize(json.GetRawText(), type);
                return true;
            }
            catch { }

            converted = default;
            return false;
        }

        public override string ToString()
        {
            if (_originalData is string) return (string)_originalData;
            else return base.ToString();
        }

        protected internal override bool IsReadOnly => true;

        protected internal override void SetPropertyCore(string propertyName, object propertyValue)
            => new InvalidOperationException("This object is read-only");
    }
}