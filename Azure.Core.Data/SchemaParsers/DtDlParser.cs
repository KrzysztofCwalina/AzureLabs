﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Azure.Data
{
    public static class DtDlParser
    {
        public static DataSchema ParseFile(string filename)
        {
            var schemaJson = File.ReadAllText(filename);
            DataSchema schema = ParseJson(schemaJson);
            return schema;
        }

        public static DataSchema ParseJson(string schemaJson)
        {
            var schema = new Dictionary<string, DataSchema.PropertySchema>(StringComparer.Ordinal);

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
                schema.Add(name, new DataSchema.PropertySchema(clrType, name, !writable, isRequired: false));
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

        class DtdlSchema : DataSchema
        {
            Dictionary<string, PropertySchema> _properties;

            public DtdlSchema(Dictionary<string, PropertySchema> properties)
                => _properties = properties;

            public override IEnumerable<string> PropertyNames => _properties.Keys;

            public override bool TryGetPropertyType(string propertyName, out PropertySchema schema)
            {
                return _properties.TryGetValue(propertyName, out schema);
            }
        }
    }
}