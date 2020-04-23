using System.Collections.Generic;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Azure.Data
{
    public class ReadOnlyJson : ReadOnlyModel
    {
        private JsonElement _json;

        public ReadOnlyJson(string jsonObject)
        {
            var document = JsonDocument.Parse(jsonObject);
            _json = document.RootElement;
            if (_json.ValueKind != JsonValueKind.Object && _json.ValueKind != JsonValueKind.Array) throw new InvalidOperationException("JSON is not an object or array");
        }

        public ReadOnlyJson(Stream jsonObject)
        {
            var document = JsonDocument.Parse(jsonObject);
            _json = document.RootElement;
            if (_json.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("JSON is not an object");
        }

        public static async Task<ReadOnlyJson> CreateAsync(Stream json, CancellationToken cancellationToken)
        {
            var document = await JsonDocument.ParseAsync(json, default, cancellationToken).ConfigureAwait(false);
            return new ReadOnlyJson(document.RootElement);
        }

        public ReadOnlyJson(JsonElement jsonObject)
        {
            if (jsonObject.ValueKind != JsonValueKind.Object && jsonObject.ValueKind != JsonValueKind.Array) throw new InvalidOperationException("JSON is not an object or array");
            _json = jsonObject;
        }

        protected override Model CreateCore(ReadOnlySpan<(string propertyName, object propertyValue)> properties)
            => new ReadOnlyDictionaryData(properties); // TODO: is this OK that it creates a defferent type?

        public override IEnumerable<string> PropertyNames {
            get {
                if (_json.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in _json.EnumerateObject())
                    {
                        yield return property.Name;
                    }
                }
            }
        }

        protected override bool TryGetPropertyCore(string propertyName, out object propertyValue)
        {
            if (!_json.TryGetProperty(propertyName, out JsonElement element))
            {
                propertyValue = default;
                return false;
            }

            return TryGetValue(element, out propertyValue);
        }

        private bool TryGetValue(JsonElement element, out object value)
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
                    value = new ReadOnlyJson(element);
                    break;
                case JsonValueKind.Number:
                    // TODO: is this what we want? i.e. we return the smallest integer if the value fits, the floats, the decimal.
                    if (element.TryGetUInt64(out var ulongValue))
                    {
                        if (ulongValue <= uint.MaxValue)
                        {
                            if (ulongValue <= ushort.MaxValue)
                            {
                                if (ulongValue <= byte.MaxValue)
                                {
                                    value = (byte)ulongValue;
                                    break;
                                }

                                value = (ushort)ulongValue;
                                break;
                            }

                            value = (uint)ulongValue;
                            break;
                        }

                        value = ulongValue;
                        break;
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
                                    value = (sbyte)longValue;
                                    break;
                                }

                                value = (short)longValue;
                                break;
                            }

                            value = (int)longValue;
                            break;
                        }

                        value = longValue;
                        break;
                    }

                    if (element.TryGetSingle(out var singleValue))
                    {
                        value = singleValue;
                        break;
                    }

                    if (element.TryGetDouble(out var doubleValue))
                    {
                        value = doubleValue;
                        break;
                    }

                    if (element.TryGetDecimal(out var decimalValue))
                    {
                        value = decimalValue;
                        break;
                    }
                    throw new NotImplementedException("this should never happen");
                case JsonValueKind.Array:
                    value = new ReadOnlyJson(element);
                    break;
                default:
                    throw new NotImplementedException("this should never happen");
            }
            return true;
        }

        protected override bool TryGetAtCore(int index, out object item)
        {
            item = default;
            if (_json.ValueKind != JsonValueKind.Array) return false;
            JsonElement itemElement = _json[index];
            return TryGetValue(itemElement, out item);
        }

        public object ToType(Type type)
        {
            var result = JsonSerializer.Deserialize(_json.GetRawText(), type);
            return result;
        }

        protected override bool TryConvertToCore(Type type, out object converted)
        {
            if (_json.ValueKind == JsonValueKind.Array && !IsPrimitiveArray(type))
            {
                var items = _json.GetArrayLength();
                var array = new ReadOnlyJson[items];
                int index = 0;
                foreach(var item in _json.EnumerateArray())
                {
                    array[index++] = new ReadOnlyJson(item); // TODO: this will throw for primitives.
                }
                converted = array;
                return true;
            }
            try
            {
                converted = JsonSerializer.Deserialize(_json.GetRawText(), type);
                return true;
            }
            catch { }

            converted = default;
            return false;
        }
    }
}