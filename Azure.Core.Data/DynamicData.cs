using System.Collections.Generic;
using System.Text;
using System.Dynamic;
using System.Linq.Expressions;
using System.ComponentModel;
using System.Collections;
using System;
using System.IO;
using System.Reflection;
using System.Buffers;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace Azure.Data
{
    public abstract class DynamicData : IDynamicMetaObjectProvider, IEnumerable<string>
    {
        static DynamicData s_empty = new ReadOnlyDictionaryData();

        internal DynamicData() { } // internal, as we don't want to make it publicly extensible yet.

        // TODO: I really don't like that users cannot just new up an instance. But for this, we would need to put a filed in this abstraction.
        public static DynamicData Create(params (string propertyName, object propertyValue)[] properties)
            => new ReadWriteDictionaryData(properties);

        public static DynamicData CreateReadOnly(params (string propertyName, object propertyValue)[] properties)
        {
            if (properties.Length == 0) return s_empty;
            return new ReadOnlyDictionaryData(properties);
        }

        public static DynamicData CreateFromDictionary(IDictionary<string, object> properties) => new ReadWriteDictionaryData(properties);
        public static DynamicData CreateFromDictionary(IReadOnlyDictionary<string, object> properties) => new ReadOnlyDictionaryData(properties);
        public static DynamicData CreateFromJson(string jsonObject) => new ReadOnlyJsonData(jsonObject);
        public static DynamicData CreateFromJson(Stream jsonObject) => new ReadOnlyJsonData(jsonObject);

        public static Task<DynamicData> CreateFromJsonAsync(Stream jsonObject, CancellationToken cancellationToken = default) => ReadOnlyJsonData.CreateAsync(jsonObject, cancellationToken);

        public object this[string propertyName] {
            get => GetProperty(propertyName);
            set => SetProperty(propertyName, value);
        }

        #region Abstract Members
        public abstract bool IsReadOnly { get; }
        public abstract IEnumerable<string> PropertyNames { get; }

        protected abstract void SetPropertyCore(string propertyName, object propertyValue);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        /// <remarks>When implemented, propertyValue has to be either a primitive (see IsPrimitive), or a DynamicData instance.</remarks>
        protected abstract bool TryGetPropertyCore(string propertyName, out object propertyValue);

        protected abstract bool TryGetAtCore(int index, out object item);

        protected abstract bool TryConvertToCore(Type type, out object converted);
        protected abstract DynamicData CreateCore(ReadOnlySpan<(string propertyName, object propertyValue)> properties);
        #endregion

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new MetaObject(parameter, this);

        private class MetaObject : DynamicMetaObject
        {
            internal MetaObject(Expression parameter, DynamicData value) : base(parameter, BindingRestrictions.Empty, value)
            { }

            public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
            {
                if (indexes.Length != 1) throw new InvalidOperationException();
                var index = (int)indexes[0].Value;

                var targetObject = Expression.Convert(Expression, LimitType);
                var methodIplementation = typeof(DynamicData).GetMethod(nameof(GetAt), BindingFlags.NonPublic | BindingFlags.Instance);
                var arguments = new Expression[] { Expression.Constant(index) };

                var getPropertyCall = Expression.Call(targetObject, methodIplementation, arguments);
                var restrictions = binder.FallbackGetIndex(this, indexes).Restrictions; // TODO: all these restrictions are a hack. Tthey need to be cleaned up.
                DynamicMetaObject getProperty = new DynamicMetaObject(getPropertyCall, restrictions);
                return getProperty;
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                Expression targetObject = Expression.Convert(Expression, LimitType);
                var methodImplementation = typeof(DynamicData).GetMethod(nameof(SetProperty), BindingFlags.NonPublic | BindingFlags.Instance);
                var arguments = new Expression[2] { Expression.Constant(binder.Name), Expression.Convert(value.Expression, typeof(object)) };

                Expression setPropertyCall = Expression.Call(targetObject, methodImplementation, arguments);
                BindingRestrictions restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);
                DynamicMetaObject setProperty = new DynamicMetaObject(setPropertyCall, restrictions);
                return setProperty;
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                var targetObject = Expression.Convert(Expression, LimitType);
                var methodIplementation = typeof(DynamicData).GetMethod(nameof(GetProperty), BindingFlags.NonPublic | BindingFlags.Instance);
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
        }

        private object GetProperty(string propertyName)
        {
            if (TryGetPropertyCore(propertyName, out object value))
            {
                if (IsPrimitive(value.GetType())) return value;
                if (value is DynamicData) return value;
                else throw new Exception("TryGetPropertyCore returned invalid object");
            }
            throw new InvalidOperationException("Property not found");
        }

        private object GetAt(int index)
        {
            if (TryGetAtCore(index, out object item))
            {
                if (IsPrimitive(item.GetType())) return item;
                if (item is DynamicData) return item;
                else throw new Exception("TryGetAt returned invalid object");
            }
            throw new InvalidOperationException("Property not found");
        }

        private object SetProperty(string propertyName, object propertyValue)
        {
            EnsureNotReadOnly();
            var valueType = propertyValue.GetType();
            if (!IsPrimitive(valueType) && !IsPrimitiveArray(valueType))
            {
                if (valueType.IsArray) throw new NotImplementedException();

                int debth = 100; // TODO: is this a good default? Should it be configurable?
                propertyValue = FromComplex(propertyValue, ref debth);
            }
            SetPropertyCore(propertyName, propertyValue);
            return propertyValue;
        }

        private object ConvertTo(Type toType)
        {
            if (TryConvertToCore(toType, out var result)) return result;
            throw new InvalidCastException($"Cannot cast to {toType}.");
        }

        protected static void ThrowReadOnlyException() => throw new InvalidOperationException("This dynamic data object is read-only.");
        protected void EnsureNotReadOnly()
        {
            if (IsReadOnly) ThrowReadOnlyException();
        }

        private DynamicData FromComplex(object obj, ref int allowedDebth)
        {
            if (--allowedDebth < 0) throw new InvalidOperationException("Object grath too deep");

            var type = obj.GetType();
            Debug.Assert(!IsPrimitive(type));
            Debug.Assert(!IsPrimitiveArray(type));

            var result = obj as DynamicData;
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
                    if (!IsPrimitive(value.GetType())) value = FromComplex(value, ref allowedDebth);
                    properties[i] = (name, value);
                }

                return CreateCore(properties.AsSpan(0, objectProperties.Length));
            }
            finally
            {
                if (properties != null) ArrayPool<(string, object)>.Shared.Return(properties);
            }
        }

        // TODO: this needs to be fixed
        // primitives are: Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single.
        // What about: decimal, DateTime, DateTimeOffset, TimeSpan
        private bool IsPrimitive(Type type)
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

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("{");
            bool first = true;
            foreach (var propertyName in this)
            {
                if (first) first = false;
                else sb.Append(",\n");
                sb.Append($"\t{propertyName} : {this[propertyName]}");
            }
            sb.Append("\n}");

            return sb.ToString();
        }
    }
}