using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.ComponentModel;
using System.Collections;
using System;
using System.Reflection;
using System.Buffers;
using System.Diagnostics;

namespace Azure.Data
{
    // TDODO: is IEnumerable<string> the right thing?
    public class DynamicData : IDynamicMetaObjectProvider, IEnumerable<string>
    {
        DataStore _store;
        DataSchema _schema; // TTODO: should schema be exposed?

        public DynamicData() => _store = new DictionaryStore();

        public DynamicData(DataStore store) => _store = store;

        public DynamicData(DataSchema schema) : this() => _schema = schema;

        public DynamicData(ReadOnlySpan<(string propertyName, object propertyValue)> properties, bool isReadOnly) {
            var store = new DictionaryStore();
            for (int i = 0; i < properties.Length; i++){
                var property = properties[i];
                store.SetValue(property.propertyName, property.propertyValue);
            }
            if (isReadOnly) store.Freeze(); 
            _store = store;
        }

        public DynamicData(IReadOnlyDictionary<string, object> properties)
        {
            var store = new DictionaryStore();
            foreach (var property in properties)
            {
                store.SetValue(property.Key, property.Value);
            }
            store.Freeze();
            _store = store;
        }

        // TODO: I dont like these create methods. 
        public static DynamicData Create(params (string propertyName, object propertyValue)[] properties)
            => new DynamicData(properties, isReadOnly: false);

        public static DynamicData CreateReadOnly(params (string propertyName, object propertyValue)[] properties)
            => new DynamicData(properties, isReadOnly: true);

        public object this[string propertyName] {
            get => GetValue(propertyName);
            set => SetValue(propertyName, value);
        }

        public IEnumerable<string> PropertyNames => _store.PropertyNames;

        private object GetValue(string propertyName)
        {
            if (_store.TryGetValue(propertyName, out object value))
            {
                Debug.Assert(IsPrimitive(value.GetType()) || value is DynamicData);
                return value;
            }
            throw new InvalidOperationException("Property not found");
        }

        private object GetValueAt(int index)
        {
            if (_store.TryGetValueAt(index, out object item))
            {
                if (IsPrimitive(item.GetType())) return item;
                if (item is DynamicData) return item;
                else throw new Exception("TryGetAt returned invalid object");
            }
            throw new InvalidOperationException("Property not found");
        }

        // TODO (Pri 1): should this be TryGetAs?
        private object ConvertTo(Type type)
        {
            // TODO: should this be TryGetAs?
            if (_store.TryConvertTo(type, out var result)) return result;
            throw new InvalidCastException($"Cannot cast to {type}.");
        }

        private protected virtual object SetValue(string propertyName, object propertyValue)
        {
            if (_store.IsReadOnly)
            {
                throw new InvalidOperationException($"The data is read-only");
            }
            if (_schema != default)
            {
                if (!_schema.TryGetSchema(propertyName, out var schema))
                {
                    throw new InvalidOperationException($"Property {propertyName} does not exist");
                }
                if (schema.IsReadOnly)
                {
                    throw new InvalidOperationException($"Property {propertyName} is read-only");
                }
                if (!schema.Type.IsAssignableFrom(propertyValue.GetType()))
                {
                    throw new InvalidOperationException($"Property {propertyName} is of type {schema.Type}");
                }
            }

            var valueType = propertyValue.GetType();

            if (!IsPrimitive(valueType) && !IsPrimitiveArray(valueType))
            {
                int debth = 100; // TODO: is this a good default? Should it be configurable?
                if (valueType.IsArray)
                {
                    object[] array = (object[])propertyValue;
                    DynamicData[] result = new DynamicData[array.Length];
                    for (int i = 0; i < array.Length; i++)
                    {
                        result[i] = FromPoco(array[i], ref debth);
                    }
                    propertyValue = result;
                }
                else
                {
                    propertyValue = FromPoco(propertyValue, ref debth);
                }
            }
             _store.SetValue(propertyName, propertyValue);
            return propertyValue;
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => GetMetaObjectCore(parameter);

        internal virtual DynamicMetaObject GetMetaObjectCore(Expression parameter) => new MetaObject(parameter, this);

        private class MetaObject : DynamicMetaObject
        {
            internal MetaObject(Expression parameter, IDynamicMetaObjectProvider value) : base(parameter, BindingRestrictions.Empty, value)
            { }

            public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
            {
                if (indexes.Length != 1) throw new InvalidOperationException();
                var index = (int)indexes[0].Value;

                var targetObject = Expression.Convert(Expression, LimitType);
                var methodIplementation = typeof(DynamicData).GetMethod(nameof(GetValueAt), BindingFlags.NonPublic | BindingFlags.Instance);
                var arguments = new Expression[] { Expression.Constant(index) };

                var getPropertyCall = Expression.Call(targetObject, methodIplementation, arguments);
                var restrictions = binder.FallbackGetIndex(this, indexes).Restrictions; // TODO: all these restrictions are a hack. Tthey need to be cleaned up.
                DynamicMetaObject getProperty = new DynamicMetaObject(getPropertyCall, restrictions);
                return getProperty;
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                var targetObject = Expression.Convert(Expression, LimitType);
                var methodIplementation = typeof(DynamicData).GetMethod(nameof(GetValue), BindingFlags.NonPublic | BindingFlags.Instance);
                var arguments = new Expression[] { Expression.Constant(binder.Name) };

                var getPropertyCall = Expression.Call(targetObject, methodIplementation, arguments);
                var restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);
                DynamicMetaObject getProperty = new DynamicMetaObject(getPropertyCall, restrictions);
                return getProperty;
            }

            // TODO: this needs to deal with primitives, natural concersions, etc.
            public override DynamicMetaObject BindConvert(ConvertBinder binder)
            {
                var sourceType = LimitType;
                var sourceInstance = Expression.Convert(Expression, LimitType);
                var destinationType = binder.Type;
                var destinationTypeExpression = Expression.Constant(destinationType);

                var restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);

                var methodIplementation = typeof(DynamicData).GetMethod(nameof(ConvertTo), BindingFlags.NonPublic | BindingFlags.Instance);
                Expression expression = Expression.Call(sourceInstance, methodIplementation, new Expression[] { destinationTypeExpression });
                expression = Expression.Convert(expression, binder.Type);
                return new DynamicMetaObject(expression, restrictions);
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                Expression targetObject = Expression.Convert(Expression, LimitType);
                var methodImplementation = typeof(DynamicData).GetMethod(nameof(SetValue), BindingFlags.NonPublic | BindingFlags.Instance);
                var arguments = new Expression[2] { Expression.Constant(binder.Name), Expression.Convert(value.Expression, typeof(object)) };

                Expression setPropertyCall = Expression.Call(targetObject, methodImplementation, arguments);
                BindingRestrictions restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);
                DynamicMetaObject setProperty = new DynamicMetaObject(setPropertyCall, restrictions);
                return setProperty;
            }
        }

        // TODO: do we want to allow cycles?
        private DynamicData FromPoco(object poco, ref int allowedRecursionDebth)
        {
            if (--allowedRecursionDebth < 0) throw new InvalidOperationException("Object grath too deep");

            var pocoType = poco.GetType();
            Debug.Assert(!IsPrimitive(pocoType));
            Debug.Assert(!IsPrimitiveArray(pocoType));

            var result = poco as DynamicData;
            if (result != null) return result;

            var pocoProperties = pocoType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            (string, object)[] dynamicDataProperties = ArrayPool<(string, object)>.Shared.Rent(pocoProperties.Length);
            try
            {
                for (int i = 0; i < pocoProperties.Length; i++)
                {
                    var pocoProperty = pocoProperties[i];
                    string propertyName = pocoProperty.Name;
                    object propertyValue = pocoProperty.GetValue(poco);
                    if (propertyValue != null && !IsPrimitive(propertyValue.GetType())) propertyValue = FromPoco(propertyValue, ref allowedRecursionDebth);
                    dynamicDataProperties[i] = (propertyName, propertyValue);
                }

                return _store.CreateDynamicData(dynamicDataProperties.AsSpan(0, pocoProperties.Length));
            }
            finally
            {
                if (dynamicDataProperties != null) ArrayPool<(string, object)>.Shared.Return(dynamicDataProperties);
            }
        }

        // TODO: this needs to be fixed. maybe we need converters
        // primitives are: Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single.
        // What about: decimal, DateTime, DateTimeOffset, TimeSpan
        // TODO: should these be combined? 
        private static bool IsPrimitive(Type type)
        {
            if (type == typeof(string)) return true;
            if (type.IsPrimitive) return true;
            return false;
        }
        private static bool IsPrimitiveArray(Type type)
        {
            if (type.IsArray && IsPrimitive(type.GetElementType())) return true;
            return false;
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => PropertyNames.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => PropertyNames.GetEnumerator();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => _store.ToString();
    }
}