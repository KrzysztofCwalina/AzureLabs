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
            public PropertySchema(Type type, string name, bool isReadOnly, bool isRequired)
            {
                Type = type;
                Name = name;
                IsReadOnly = isReadOnly;
                IsRequired = isRequired;
            }

            public Type Type { get; }
            public string Name { get;  }
            public bool IsReadOnly { get; }
            public bool IsRequired { get; }

            public override string ToString()
            {
                var suffix = IsReadOnly ? "{ get; }" : "{ get; set; }";
                return $"{Type} {Name} {suffix}";
            }
        }

        // TODO: this needs to be fixed. maybe we need converters
        // primitives are: Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single.
        // What about: decimal, DateTime, DateTimeOffset, TimeSpan
        public static bool IsPrimitive(Type type)
        {
            if (type == typeof(string)) return true;
            if (type.IsPrimitive) return true;
            return false;
        }
        public static bool IsPrimitiveArray(Type type)
        {
            if (type.IsArray && IsPrimitive(type.GetElementType())) return true;
            return false;
        }
    }
}