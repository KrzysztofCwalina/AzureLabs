using System.Collections.Generic;
using System;
using System.IO;
using System.Text.Json;

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

        public ReadOnlyJsonData(Stream json)
        {
            var document = JsonDocument.Parse(json);
            _json = document.RootElement;
            if (_json.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("JSON is not an object");
        }

        public override bool IsReadOnly => true;

        protected override IEnumerable<string> PropertyNames {
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
            if (!_json.TryGetProperty(propertyName, out var element))
            {
                propertyValue = default;
                return false;
            }

            // TODO: this needs to be finished
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    propertyValue = element.GetString();
                    break;
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var value))
                    {
                        propertyValue = value;
                        break;
                    }
                    else throw new NotImplementedException();
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