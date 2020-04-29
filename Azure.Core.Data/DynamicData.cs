﻿using System.Collections.Generic;
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
    enum DynamicDataType {
        Null,       // DynamicData._data is null
        String,     // DynamicData._data is string
        Properties, // DynamicData._data is DataStore
        Array,      // DynamicData._data is DynamicData[]
    }

    // TDODO: this should implement IDictionary<string, object>
    public class DynamicData : IDynamicMetaObjectProvider, IEnumerable<string>
    {
        object _data; // one of the DynamicDataType instances
        DynamicDataType _type;
        DataSchema _schema; // This schema is in terms of CLR types.
        IReadOnlyDictionary<Type, DataConverter> _converters;

        public DynamicData()
        {
            _converters = DataConverter.CommonConverters;
            _type = DynamicDataType.Null;
        }

        public DynamicData(DataSchema schema) : this()
        {
            _type = DynamicDataType.Null;
            _schema = schema;
        }

        public DynamicData(string text, DataConverter converter) : this()
        {
            _data = text;
            _type = DynamicDataType.String;
            _converters = converter;
        }

        public DynamicData(PropertyStore properties) : this()
        {
            _data = properties;
            _type = DynamicDataType.Properties;
        }

        public DynamicData(DynamicData[] array) : this()
        {
            _data = array;
            _type = DynamicDataType.Array;
        }

        public DynamicData(IReadOnlyDictionary<string, object> properties) : this()
        {
            var store = new DictionaryStore(properties);
            store.Freeze();
            _data = store;
            _type = DynamicDataType.Properties;
        }

        //// TODO: I dont like this ctor. Maybe we ask users to create store.
        public DynamicData(bool isReadOnly, params (string propertyName, object propertyValue)[] properties) : this()
        {
            var store = new DictionaryStore();
            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                store.SetValue(property.propertyName, property.propertyValue);
            }
            if (isReadOnly) store.Freeze();
            _data = store;
            _type = DynamicDataType.Properties;
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
            var array = _data as object[];
            if (array != null) return array[index];

            var store = _data as PropertyStore;

            if (store != null)
            {
                if (store.TryGetValueAt(index, out object item))
                {
                    Debug.Assert(item.GetType().IsDynamicDataType());
                    return item;
                }
                else throw new IndexOutOfRangeException();
            }
            throw new InvalidOperationException("The data is not an array");
        }

        private object SetValue(string propertyName, object propertyValue)
        {
            var propertyValueType = propertyValue.GetType();

            if (_schema != null)
            {
                if (!_schema.TryGetPropertyType(propertyName, out var propertySchema))
                {
                    throw new InvalidOperationException($"Property {propertyName} does not exist");
                }
                if (propertySchema.IsReadOnly)
                {
                    throw new InvalidOperationException($"Property {propertyName} is read-only");
                }
                if (!propertySchema.PropertyType.IsAssignableFrom(propertyValueType))
                {
                    throw new InvalidOperationException($"Property {propertyName} is of type {propertySchema.PropertyType}");
                }
            }

            var store = _data as PropertyStore;
            if (store == null && _type == DynamicDataType.Null)
            {
                store = new DictionaryStore();
                _data = store;
                _type = DynamicDataType.Properties;
            }
            // TODO: is this really property of the store, schema, or DynamicData?
            if (store.IsReadOnly)
            {
                throw new InvalidOperationException($"The data is read-only");
            }

            if (!propertyValueType.IsDynamicDataType())
            {
                propertyValue = ToDataType(propertyValue, propertyValueType);
            }

            store.SetValue(propertyName, propertyValue);
            return propertyValue;
        }

        private object ConvertTo(Type type)
        {
            if(_converters.TryGetValue(type, out var converter))
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

        private DynamicData ToDataType(object arrayOrObject, Type objectType)
        {
            int debth = 100; // TODO: is this a good default? Should it be configurable?

            if (_converters.TryGetValue(objectType, out var converter))
            {
                return converter.ConvertToDataType(arrayOrObject);
            }

            if (objectType.IsArray)
            {
                object[] array = (object[])arrayOrObject;
                DynamicData[] result = new DynamicData[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    result[i] = FromPoco(array[i], ref debth);
                }
                return new DynamicData(result);
            }

            return FromPoco(arrayOrObject, ref debth);

            DynamicData FromPoco(object poco, ref int allowedRecursionDebth)
            {
                if (--allowedRecursionDebth < 0) throw new InvalidOperationException("Object grath contains a cycle or is too deep");

                var pocoType = poco.GetType();
                Debug.Assert(!pocoType.IsDynamicDataPrimitive());

                var result = poco as DynamicData;
                if (result != null) return result;

                var pocoProperties = pocoType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                (string, object)[] dynamicDataProperties = ArrayPool<(string, object)>.Shared.Rent(pocoProperties.Length);
                try
                {
                    for (int i = 0; i < pocoProperties.Length; i++)
                    {
                        var pocoProperty = pocoProperties[i];
                        object propertyValue = pocoProperty.GetValue(poco);
                        if (propertyValue != null && !propertyValue.GetType().IsDynamicDataPrimitive())
                        {
                            propertyValue = FromPoco(propertyValue, ref allowedRecursionDebth);
                        }
                        string propertyName = pocoProperty.Name;
                        dynamicDataProperties[i] = (propertyName, propertyValue);
                    }

                    var store = _data as PropertyStore;
                    if (store == null) throw new NotImplementedException(); // TODO: implement
                    return store.CreateDynamicData(dynamicDataProperties.AsSpan(0, pocoProperties.Length));
                }
                finally
                {
                    if (dynamicDataProperties != null) ArrayPool<(string, object)>.Shared.Return(dynamicDataProperties);
                }
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