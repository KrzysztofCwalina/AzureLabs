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
    enum DataType {
        Null,
        Properties,
        String,         
    }

    // TDODO: this should implement IDictionary<string, object>
    public class DynamicData : IDynamicMetaObjectProvider, IEnumerable<string>
    {
        DataType _type;
        object _data;
        DataSchema _schema; // TODO: should schema be exposed? Should it be part of the Store?
        public IDictionary<Type, DataConverter> Converters = new Dictionary<Type, DataConverter>();

        public DynamicData() => _type = DataType.Null;

        public DynamicData(string text)
        {
            _data = text;
            _type = DataType.String;
        }

        public DynamicData(PropertyStore store)
        {
            _data = store;
            _type = DataType.Properties;
        }

        // TODO: I don't like this ctor
        public DynamicData(DataSchema schema) : this() => _schema = schema;

        // TODO: I dont like this ctor. Maybe we ask users to create store.
        public DynamicData(bool isReadOnly, params (string propertyName, object propertyValue)[] properties)
        {
            var store = new DictionaryStore();
            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                store.SetValue(property.propertyName, property.propertyValue);
            }
            if (isReadOnly) store.Freeze();
            _data = store;
            _type = DataType.Properties;
        }

        public DynamicData(IReadOnlyDictionary<string, object> properties)
        {
            var store = new DictionaryStore(properties);
            store.Freeze();
            _data = store;
            _type = DataType.Properties;
        }

        // TODO (pri 1): if DynamicData could be an array, this could return DynamicData
        private object ToDataType(object arrayOrObject, Type objectType)
        {
            if (Converters.TryGetValue(objectType, out var converter))
            {
                return converter.ConvertToDataType(arrayOrObject);
            }

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

            // TODO: maybe we need plubable converters (both ways)
            DynamicData FromPoco(object poco, ref int allowedRecursionDebth)
            {
                if (--allowedRecursionDebth < 0) throw new InvalidOperationException("Object grath contains a cycle or is too deep");

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
                        if (propertyValue != null && !propertyValue.GetType().IsDynamicDataPrimitive()) {
                            propertyValue = FromPoco(propertyValue, ref allowedRecursionDebth);
                        }
                        string propertyName = pocoProperty.Name;
                        dynamicDataProperties[i] = (propertyName, propertyValue);
                    }

                    var store = _data as PropertyStore;
                    if (store == null) throw new NotImplementedException(); // TODO: implement
                    return store.CreateDynamicData(dynamicDataProperties.AsSpan(0, pocoProperties.Length));
                }
                finally {
                    if (dynamicDataProperties != null) ArrayPool<(string, object)>.Shared.Return(dynamicDataProperties);
                }
            }
        }

        public object this[string propertyName] {
            get => GetValue(propertyName);
            set => SetValue(propertyName, value);
        }

        public IEnumerable<string> PropertyNames {
            get {
                var store = _data as PropertyStore;
                if (store != null) return store.PropertyNames;
                return Array.Empty<string>();
            }
        }

        #region used by MetaObject
        private object GetValue(string propertyName)
        {
            var store = _data as PropertyStore;

            if (store == null) return _data;

            if (store.TryGetValue(propertyName, out object value))
            {
                Debug.Assert(value.GetType().IsDynamicDataType());
                return value;
            }
            throw new InvalidOperationException($"Property {propertyName} not found");
        }

        private object GetValueAt(int index)
        {
            var store = _data as PropertyStore;

            // TODO: implement
            if (store == null) throw new NotImplementedException();

            if (store.TryGetValueAt(index, out object item))
            {
                Debug.Assert(item.GetType().IsDynamicDataType());
                return item;
            }
            throw new IndexOutOfRangeException();
        }

        private object SetValue(string propertyName, object propertyValue)
        {
            var store = _data as PropertyStore;
            if(store == null && _type == DataType.Null)
            {
                store = new DictionaryStore();
                _data = store;
                _type = DataType.Properties;
            }
            if (store.IsReadOnly)
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
            store.SetValue(propertyName, propertyValue);
            return propertyValue;
        }

        private object ConvertTo(Type type)
        {
            if(Converters.TryGetValue(type, out var converter))
            {
                return converter.ConverFromDataType(this);
            }

            var store = _data as PropertyStore;
            if (store.TryConvertTo(type, out var result)) return result;
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

        public override string ToString() => _data.ToString();
    }
}