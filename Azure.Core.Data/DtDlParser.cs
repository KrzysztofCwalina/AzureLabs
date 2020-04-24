using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Azure.Data
{
    public static class DtDlParser
    {
        public static ModelSchema ParseFile(string filename)
        {
            var schemaJson = File.ReadAllText(filename);
            ModelSchema schema = ParseJson(schemaJson);
            return schema;
        }

        public static ModelSchema ParseJson(string schemaJson)
        {
            var schema = new Dictionary<string, ModelSchema.PropertySchema>();

            var document = JsonDocument.Parse(schemaJson);
            var root = document.RootElement;

            var contents = root.GetProperty("contents");
            foreach (var property in contents.EnumerateArray())
            {
                var name = property.GetProperty("name").GetString();
                var type = property.GetProperty("schema").GetString();
                bool writable = false;
                if(property.TryGetProperty("writable", out var writableElement)){
                    writable = writableElement.ValueKind == JsonValueKind.True;
                }

                var clrType = ToClrType(type);
                schema.Add(name, new ModelSchema.PropertySchema(clrType, name, !writable, isRequired: false));
            }

            return new DtdlSchema(schema);
        }

        private static Type ToClrType(string type)
        {
            switch (type)
            {
                case "string": return typeof(string);
                case "number": return typeof(int);
                case "array": return typeof(object[]);
                case "object": return typeof(object);
                case "double": return typeof(double);
                default: throw new NotImplementedException(type);
            }
        }

        class DtdlSchema : ModelSchema
        {
            Dictionary<string, PropertySchema> _properties;

            public DtdlSchema(Dictionary<string, PropertySchema> properties)
                => _properties = properties;

            public override IEnumerable<string> PropertyNames => _properties.Keys;

            public override bool TryGetSchema(string propertyName, out PropertySchema schema)
            {
                return _properties.TryGetValue(propertyName, out schema);
            }
        }
    }
}