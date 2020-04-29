using System.Collections.Generic;
using System;
using System.IO;
using System.Text.Json;
using System.Diagnostics;
using System.Text;

namespace Azure.Data
{
    internal class JsonDocumentStore : PropertyStore
    {
        private bool _deserialized;
        private object _originalData; // either string or stream

        private JsonElement _root; // DO NOT ACCESS THIS DIRECTLY. USE GetRootElement METHOD.
        private JsonElement GetRoot()
        {
            if (!_deserialized) Deserialize();
            return _root;
        }

        public JsonDocumentStore(string jsonObject) => _originalData = jsonObject;

        public JsonDocumentStore(Stream jsonObject) => _originalData = jsonObject;

        public JsonDocumentStore(JsonElement jsonObject)
        {
            if (jsonObject.ValueKind != JsonValueKind.Object && jsonObject.ValueKind != JsonValueKind.Array) throw new InvalidOperationException("JSON is not an object or array");
            _root = jsonObject;
            _deserialized = true;
        }

        public JsonDocumentStore(JsonDocumentStore original)
        {
            _root = original._root;
            _originalData = original._originalData;
            _deserialized = original._deserialized;
        }

        private void Deserialize()
        {
            Debug.Assert(_deserialized == false);
            var jsonString = _originalData as string;
            if (jsonString != null)
            {
                var document = JsonDocument.Parse(jsonString);
                _root = document.RootElement;
            }
            else
            {
                var jsonStream = _originalData as Stream;
                var document = JsonDocument.Parse(jsonStream);
                _root = document.RootElement;
            }
            if (_root.ValueKind != JsonValueKind.Object && _root.ValueKind != JsonValueKind.Array) throw new InvalidOperationException("JSON is not an object or array");
            _deserialized = true;
        }

        protected internal override DynamicData CreateDynamicData(ReadOnlySpan<(string propertyName, object propertyValue)> properties)
            => throw new NotImplementedException();

        protected internal override IEnumerable<string> PropertyNames {
            get {
                JsonElement element = GetRoot();
                if (element.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in element.EnumerateObject())
                    {
                        yield return property.Name;
                    }
                }
            }
        }

        // TODO: the UTF16 string has 30% perf overhead for simple property lookups
        protected internal override bool TryGetValue(string propertyName, out object propertyValue)
        {
            JsonElement json = GetRoot();
            if (!json.TryGetProperty(propertyName, out JsonElement element))
            {
                propertyValue = default;
                return false;
            }

            return TryGetValue(element, out propertyValue);
        }

        protected internal override bool TryGetValueAt(int index, out object item)
        {
            item = default;
            JsonElement json = GetRoot();
            if (json.ValueKind != JsonValueKind.Array) return false;
            JsonElement itemElement = json[index];
            return TryGetValue(itemElement, out item);
        }

        protected internal override bool TryConvertTo(Type type, out object converted)
        {
            JsonElement json = GetRoot();
            if (json.ValueKind == JsonValueKind.Array && !DataSchema.IsPrimitiveArray(type))
            {
                var items = json.GetArrayLength();
                var array = new DynamicData[items];
                int index = 0;
                foreach (var item in json.EnumerateArray())
                {
                    array[index++] = new DynamicData(new JsonDocumentStore(item)); // TODO: this will throw for primitives.
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

        protected internal override bool IsReadOnly => true;

        protected internal override void SetValue(string propertyName, object propertyValue)
            => new InvalidOperationException("This object is read-only");

        private static bool TryGetValue(JsonElement element, out object value)
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
                    value = new DynamicData(new JsonDocumentStore(element));
                    break;
                case JsonValueKind.Number:
                    if(element.TryGetInt64(out long longValue))
                    {
                        value = (double)longValue;
                        return true;
                    }
                    value = element.GetDouble(); // TODO: but the double parser is really bad!
                    break;
                case JsonValueKind.Array:
                    value = new DynamicData(new JsonDocumentStore(element));
                    break;
                default:
                    throw new NotImplementedException("this should never happen");
            }
            return true;
        }

        public override string ToString()
        {
            if (_originalData is string) return (string)_originalData;
            else return base.ToString();
        }
    }
}