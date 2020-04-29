using System;
using System.Collections.Generic;

namespace Azure.Data
{
    public abstract class DataSchema
    {
        public abstract bool TryGetPropertyType(string propertyName, out PropertySchema schema);
        public abstract IEnumerable<string> PropertyNames { get; }
        public readonly struct PropertySchema
        {
            public PropertySchema(Type type, string name, bool isReadOnly, bool isRequired)
            {
                PropertyType = type;
                Name = name;
                IsReadOnly = isReadOnly;
                IsRequired = isRequired;
            }

            public Type PropertyType { get; }
            public string Name { get;  }
            public bool IsReadOnly { get; }
            public bool IsRequired { get; }

            public override string ToString()
            {
                var suffix = IsReadOnly ? "{ get; }" : "{ get; set; }";
                return $"{PropertyType} {Name} {suffix}";
            }
        }
    }
}