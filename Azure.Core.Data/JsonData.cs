using System.Collections.Generic;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace Azure.Data
{
    internal class ReadOnlyJsonData : DynamicData
    {
        private JsonElement _json;

        public ReadOnlyJsonData(string jsonObject)
        {
            var document = JsonDocument.Parse(jsonObject);
            _json = document.RootElement;
            if (_json.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("JSON is not an object");
        }

        public ReadOnlyJsonData(Stream jsonObject)
        {
            var document = JsonDocument.Parse(jsonObject);
            _json = document.RootElement;
            if (_json.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("JSON is not an object");
        }

        public static async Task<DynamicData> CreateAsync(Stream json, CancellationToken cancellationToken)
        {
            var document = await JsonDocument.ParseAsync(json, default, cancellationToken).ConfigureAwait(false);
            return new ReadOnlyJsonData(document.RootElement);
        }

        public ReadOnlyJsonData(JsonElement jsonObject)
        {
            if (_json.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("JSON is not an object");
            _json = jsonObject;
        }

        public override bool IsReadOnly => true;

        protected override DynamicData CreateCore(ReadOnlySpan<(string propertyName, object propertyValue)> properties)
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

        protected override void SetPropertyCore(string propertyName, object propertyValue) => ThrowReadOnlyException();

        protected override bool TryGetPropertyCore(string propertyName, out object propertyValue)
        {
            if (!_json.TryGetProperty(propertyName, out JsonElement element))
            {
                propertyValue = default;
                return false;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    propertyValue = element.GetString();
                    break;
                case JsonValueKind.False:
                    propertyValue = false;
                    break;
                case JsonValueKind.True:
                    propertyValue = true;
                    break;
                case JsonValueKind.Null:
                    propertyValue = null;
                    break;
                case JsonValueKind.Object:
                    propertyValue = new ReadOnlyJsonData(element);
                    break;
                case JsonValueKind.Number:
                    // TODO: is this what we want? i.e. we return the smallest integer if the value fits, the floats, the decimal.
                    if (element.TryGetUInt64(out var ulongValue))
                    {
                        if (ulongValue <= uint.MaxValue)
                        {
                            if (ulongValue <= ushort.MaxValue)
                            {
                                if (ulongValue <= byte.MaxValue) {
                                    propertyValue = (byte)ulongValue;
                                    break;
                                }

                                propertyValue = (ushort)ulongValue;
                                break;
                            }

                            propertyValue = (uint)ulongValue;
                            break;
                        }

                        propertyValue = ulongValue;
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
                                    propertyValue = (sbyte)longValue;
                                    break;
                                }

                                propertyValue = (short)longValue;
                                break;
                            }

                            propertyValue = (int)longValue;
                            break;
                        }

                        propertyValue = longValue;
                        break;
                    }

                    if (element.TryGetSingle(out var singleValue))
                    {
                        propertyValue = singleValue;
                        break;
                    }

                    if (element.TryGetDouble(out var doubleValue))
                    {
                        propertyValue = doubleValue;
                        break;
                    }

                    if (element.TryGetDecimal(out var decimalValue)) {
                        propertyValue = decimalValue;
                        break;
                    }
                    throw new NotImplementedException(); // this should never happen
                case JsonValueKind.Array:
                    // what do we do here? Deserialize all the values?
                    throw new NotImplementedException(); // TODO: this needs to be finished
                default:
                    throw new NotImplementedException();
            }
            return true;
        }

        public object ToType(Type type)
        {
            var result = JsonSerializer.Deserialize(_json.GetRawText(), type);
            return result;
        }

        protected override bool TryConvertToCore(Type type, out object converted)
        {
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