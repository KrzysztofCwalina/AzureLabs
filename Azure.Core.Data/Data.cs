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
    public class Data : IDynamicMetaObjectProvider, IEnumerable<string>
    {
        DataStore _store;
        DataSchema _schema;

        public Data() => _store = new DictionaryStore();

        public Data(DataStore provider) => _store = provider;

        public Data(DataSchema schema) : this() => _schema = schema;

        public Data(ReadOnlySpan<(string propertyName, object propertyValue)> properties, bool isReadOnly) {
            var store = new DictionaryStore();
            for (int i = 0; i < properties.Length; i++){
                var property = properties[i];
                store.SetPropertyCore(property.propertyName, property.propertyValue);
            }
            if (isReadOnly) store.Freeze(); 
            _store = store;
        }

        public Data(IReadOnlyDictionary<string, object> properties)
        {
            var store = new DictionaryStore();
            foreach (var property in properties)
            {
                store.SetPropertyCore(property.Key, property.Value);
            }
            store.Freeze();
            _store = store;
        }

        public static Data Create(params (string propertyName, object propertyValue)[] properties)
            => new Data(properties, isReadOnly: false);

        public static Data CreateReadOnly(params (string propertyName, object propertyValue)[] properties)
            => new Data(properties, isReadOnly: true);

        public object this[string propertyName] {
            get => GetProperty(propertyName);
            set => SetProperty(propertyName, value);
        }

        public IEnumerable<string> PropertyNames => _store.PropertyNames;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        /// <remarks>When implemented, propertyValue has to be either a primitive (see IsPrimitive), or a DynamicData instance.</remarks>

        private object GetProperty(string propertyName)
        {
            if (_store.TryGetPropertyCore(propertyName, out object value))
            {
                Debug.Assert(IsPrimitive(value.GetType()) || value is Data);
                return value;
            }
            throw new InvalidOperationException("Property not found");
        }

        private object GetAt(int index)
        {
            if (_store.TryGetAtCore(index, out object item))
            {
                if (IsPrimitive(item.GetType())) return item;
                if (item is Data) return item;
                else throw new Exception("TryGetAt returned invalid object");
            }
            throw new InvalidOperationException("Property not found");
        }

        private object ConvertTo(Type toType)
        {
            if (_store.TryConvertToCore(toType, out var result)) return result;
            throw new InvalidCastException($"Cannot cast to {toType}.");
        }

        private protected virtual object SetProperty(string propertyName, object propertyValue)
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
                    Data[] result = new Data[array.Length];
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
             _store.SetPropertyCore(propertyName, propertyValue);
            return propertyValue;
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => GetMetaObjectCore(parameter);

        internal virtual DynamicMetaObject GetMetaObjectCore(Expression parameter) => new ReadOnlyMetaObject(parameter, this);

        internal class ReadOnlyMetaObject : DynamicMetaObject
        {
            internal ReadOnlyMetaObject(Expression parameter, IDynamicMetaObjectProvider value) : base(parameter, BindingRestrictions.Empty, value)
            { }

            public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
            {
                if (indexes.Length != 1) throw new InvalidOperationException();
                var index = (int)indexes[0].Value;

                var targetObject = Expression.Convert(Expression, LimitType);
                var methodIplementation = typeof(Data).GetMethod(nameof(GetAt), BindingFlags.NonPublic | BindingFlags.Instance);
                var arguments = new Expression[] { Expression.Constant(index) };

                var getPropertyCall = Expression.Call(targetObject, methodIplementation, arguments);
                var restrictions = binder.FallbackGetIndex(this, indexes).Restrictions; // TODO: all these restrictions are a hack. Tthey need to be cleaned up.
                DynamicMetaObject getProperty = new DynamicMetaObject(getPropertyCall, restrictions);
                return getProperty;
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                var targetObject = Expression.Convert(Expression, LimitType);
                var methodIplementation = typeof(Data).GetMethod(nameof(GetProperty), BindingFlags.NonPublic | BindingFlags.Instance);
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

                var methodIplementation = typeof(Data).GetMethod(nameof(ConvertTo), BindingFlags.NonPublic | BindingFlags.Instance);
                Expression expression = Expression.Call(sourceInstance, methodIplementation, new Expression[] { destinationTypeExpression });
                expression = Expression.Convert(expression, binder.Type);
                return new DynamicMetaObject(expression, restrictions);
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                Expression targetObject = Expression.Convert(Expression, LimitType);
                var methodImplementation = typeof(Data).GetMethod(nameof(SetProperty), BindingFlags.NonPublic | BindingFlags.Instance);
                var arguments = new Expression[2] { Expression.Constant(binder.Name), Expression.Convert(value.Expression, typeof(object)) };

                Expression setPropertyCall = Expression.Call(targetObject, methodImplementation, arguments);
                BindingRestrictions restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);
                DynamicMetaObject setProperty = new DynamicMetaObject(setPropertyCall, restrictions);
                return setProperty;
            }
        }

        internal Data FromComplex(object obj, ref int allowedDebth)
        {
            if (--allowedDebth < 0) throw new InvalidOperationException("Object grath too deep");

            var type = obj.GetType();
            Debug.Assert(!IsPrimitive(type));
            Debug.Assert(!IsPrimitiveArray(type));

            var result = obj as Data;
            if (result != null) return result;

            var objectProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            (string, object)[] properties = ArrayPool<(string, object)>.Shared.Rent(objectProperties.Length);
            try
            {
                for (int i = 0; i < objectProperties.Length; i++)
                {
                    var property = objectProperties[i];
                    string name = property.Name;
                    object value = property.GetValue(obj);
                    if (value != null && !IsPrimitive(value.GetType())) value = FromComplex(value, ref allowedDebth);
                    properties[i] = (name, value);
                }

                return _store.CreateCore(properties.AsSpan(0, objectProperties.Length));
            }
            finally
            {
                if (properties != null) ArrayPool<(string, object)>.Shared.Return(properties);
            }
        }

        // TODO: this needs to be fixed. maybe we need converters
        // primitives are: Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single.
        // What about: decimal, DateTime, DateTimeOffset, TimeSpan
        protected bool IsPrimitive(Type type)
        {
            if (type == typeof(string)) return true;
            if (type.IsPrimitive) return true;
            return false;
        }
        protected bool IsPrimitiveArray(Type type)
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