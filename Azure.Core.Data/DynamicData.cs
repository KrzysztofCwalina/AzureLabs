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
        DataSchema _schema; // TODO: should schema be exposed?

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
            var store = new DictionaryStore(properties);
            store.Freeze();
            _store = store;
        }

        // TODO (pri 1): if DynamicData could be an array, this could return DynamicData
        private object ToDataType(object arrayOrObject, Type objectType)
        {
            int debth = 100; // TODO: is this a good default? Should it be configurable?
            if (objectType.IsArray)
            {
                object[] array = (object[])arrayOrObject;
                DynamicData[] result = new DynamicData[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    result[i] = FromPoco(array[i], ref debth);
                }
                arrayOrObject = result;
            }
            else
            {
                arrayOrObject = FromPoco(arrayOrObject, ref debth);
            }
            return arrayOrObject;

            // TODO: do we want to allow cycles?
            // TODO: maybe we need plubable converters (both ways)
            DynamicData FromPoco(object poco, ref int allowedRecursionDebth)
            {
                if (--allowedRecursionDebth < 0) throw new InvalidOperationException("Object grath too deep");

                var pocoType = poco.GetType();
                Debug.Assert(!pocoType.IsDynamicDataPrimitive());

                var result = poco as DynamicData;
                if (result != null) return result;

                var pocoProperties = pocoType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                (string, object)[] dynamicDataProperties = ArrayPool<(string, object)>.Shared.Rent(pocoProperties.Length);
                try {
                    for (int i = 0; i < pocoProperties.Length; i++) {
                        var pocoProperty = pocoProperties[i];
                        object propertyValue = pocoProperty.GetValue(poco);
                        var valueType = propertyValue.GetType();
                        if (propertyValue != null && !valueType.IsDynamicDataPrimitive()) {
                            propertyValue = FromPoco(propertyValue, ref allowedRecursionDebth);
                        }
                        string propertyName = pocoProperty.Name;
                        dynamicDataProperties[i] = (propertyName, propertyValue);
                    }

                    return _store.CreateDynamicData(dynamicDataProperties.AsSpan(0, pocoProperties.Length));
                }
                finally {
                    if (dynamicDataProperties != null) ArrayPool<(string, object)>.Shared.Return(dynamicDataProperties);
                }
            }
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

        #region used by MetaObject
        private object GetValue(string propertyName)
        {
            if (_store.TryGetValue(propertyName, out object value))
            {
                Debug.Assert(value.GetType().IsDynamicDataType());
                return value;
            }
            throw new InvalidOperationException($"Property {propertyName} not found");
        }

        private object GetValueAt(int index)
        {
            if (_store.TryGetValueAt(index, out object item))
            {
                Debug.Assert(item.GetType().IsDynamicDataType());
                return item;
            }
            throw new IndexOutOfRangeException();
        }

        private object SetValue(string propertyName, object propertyValue)
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
                // TODO (pri 1): this type check needs to be done after type conversion
                if (!schema.Type.IsAssignableFrom(propertyValue.GetType()))
                {
                    throw new InvalidOperationException($"Property {propertyName} is of type {schema.Type}");
                }
            }

            var valueType = propertyValue.GetType();

            if (!valueType.IsDynamicDataType())
            {
                propertyValue =  ToDataType(propertyValue, valueType);
            }
            _store.SetValue(propertyName, propertyValue);
            return propertyValue;
        }

        private object ConvertTo(Type type)
        {
            if (_store.TryConvertTo(type, out var result)) return result;
            throw new InvalidCastException($"Cannot cast to {type}.");
        }
        #endregion

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new MetaObject(parameter, this);

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
    
        IEnumerator<string> IEnumerable<string>.GetEnumerator() => PropertyNames.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => PropertyNames.GetEnumerator();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => _store.ToString();
    }
}