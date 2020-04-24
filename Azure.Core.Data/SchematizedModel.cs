using System;
using System.Collections.Generic;

namespace Azure.Data
{
    public abstract class SchematizedModel : ReadWriteModel
    {
        ModelSchema _schema;

        internal SchematizedModel()
        {
            _schema = default;
        }
        // internal, as we don't want to make it publicly extensible yet.
        internal SchematizedModel(ModelSchema schema) {
            _schema = schema;
        }

        private protected override object SetProperty(string propertyName, object propertyValue)
        {
            if (_schema != default)
            {
                if (!_schema.TryGetSchema(propertyName, out var schema))
                {
                    throw new Exception($"Property {propertyName} does not exist");
                }
                if (!schema.Type.IsAssignableFrom(propertyValue.GetType()))
                {
                    throw new Exception($"Property {propertyName} is of type {schema.Type}");
                }
            }

            var valueType = propertyValue.GetType();

            if (!IsPrimitive(valueType) && !IsPrimitiveArray(valueType))
            {
                int debth = 100; // TODO: is this a good default? Should it be configurable?
                if (valueType.IsArray)
                {
                    object[] array = (object[])propertyValue;
                    ReadOnlyModel[] result = new ReadOnlyModel[array.Length];
                    for (int i = 0; i < array.Length; i++)
                    {
                        result[i] = FromComplex(array[i], ref debth);
                    }
                    propertyValue = result;
                }
                else
                {
                    propertyValue = FromComplex(propertyValue, ref debth);
                }
            }
            SetPropertyCore(propertyName, propertyValue);
            return propertyValue;
        }

        public override IEnumerable<string> PropertyNames => _schema.PropertyNames;
    }

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