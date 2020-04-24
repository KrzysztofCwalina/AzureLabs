using System;
using System.Collections.Generic;

namespace Azure.Data
{
    public abstract class ModelSchema
    {
        public abstract bool TryGetSchema(string propertyName, out PropertySchema schema);

        public abstract IEnumerable<string> PropertyNames { get; }
        public readonly struct PropertySchema
        {
            public PropertySchema(Type type, string name, bool isReadOnly)
            {
                Type = type;
                Name = name;
                IsReadOnly = isReadOnly;
            }

            public Type Type { get; }
            public string Name { get;  }
            public bool IsReadOnly { get; }

            public override string ToString()
            {
                var suffix = IsReadOnly ? "{ get; }" : "{ get; set; }";
                return $"{Type} {Name} {suffix}";
            }
        }
    }
}