using System.Collections.Generic;
using System.Text;
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
    public abstract class ReadOnlyModel : IDynamicMetaObjectProvider, IEnumerable<string>
    {
        static ReadOnlyModel s_empty = new ReadOnlyDictionaryModel();

        public object this[string propertyName] {
            get => GetProperty(propertyName);
        }

        public abstract IEnumerable<string> PropertyNames { get; }

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

        protected abstract ReadOnlyModel CreateCore(ReadOnlySpan<(string propertyName, object propertyValue)> properties);

        private object GetProperty(string propertyName)
        {
            if (TryGetPropertyCore(propertyName, out object value))
            {
                Debug.Assert(IsPrimitive(value.GetType()) || value is ReadOnlyModel);
                return value;
            }
            throw new InvalidOperationException("Property not found");
        }

        private object GetAt(int index)
        {
            if (TryGetAtCore(index, out object item))
            {
                if (IsPrimitive(item.GetType())) return item;
                if (item is ReadOnlyModel) return item;
                else throw new Exception("TryGetAt returned invalid object");
            }
            throw new InvalidOperationException("Property not found");
        }

        private object ConvertTo(Type toType)
        {
            if (TryConvertToCore(toType, out var result)) return result;
            throw new InvalidCastException($"Cannot cast to {toType}.");
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => GetMetaObjectCore(parameter);

        internal virtual DynamicMetaObject GetMetaObjectCore(Expression parameter) => new ReadOnlyMetaObject(parameter, this);

        internal class ReadOnlyMetaObject : DynamicMetaObject
        {
            internal ReadOnlyMetaObject(Expression parameter, ReadOnlyModel value) : base(parameter, BindingRestrictions.Empty, value)
            { }

            public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
            {
                if (indexes.Length != 1) throw new InvalidOperationException();
                var index = (int)indexes[0].Value;

                var targetObject = Expression.Convert(Expression, LimitType);
                var methodIplementation = typeof(ReadOnlyModel).GetMethod(nameof(GetAt), BindingFlags.NonPublic | BindingFlags.Instance);
                var arguments = new Expression[] { Expression.Constant(index) };

                var getPropertyCall = Expression.Call(targetObject, methodIplementation, arguments);
                var restrictions = binder.FallbackGetIndex(this, indexes).Restrictions; // TODO: all these restrictions are a hack. Tthey need to be cleaned up.
                DynamicMetaObject getProperty = new DynamicMetaObject(getPropertyCall, restrictions);
                return getProperty;
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                var targetObject = Expression.Convert(Expression, LimitType);
                var methodIplementation = typeof(ReadOnlyModel).GetMethod(nameof(GetProperty), BindingFlags.NonPublic | BindingFlags.Instance);
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

                var methodIplementation = typeof(ReadOnlyModel).GetMethod(nameof(ConvertTo), BindingFlags.NonPublic | BindingFlags.Instance);
                Expression expression = Expression.Call(sourceInstance, methodIplementation, new Expression[] { destinationTypeExpression });
                expression = Expression.Convert(expression, binder.Type);
                return new DynamicMetaObject(expression, restrictions);
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
                => throw new InvalidOperationException("this model ius read-only");
        }

        internal ReadOnlyModel FromComplex(object obj, ref int allowedDebth)
        {
            if (--allowedDebth < 0) throw new InvalidOperationException("Object grath too deep");

            var type = obj.GetType();
            Debug.Assert(!IsPrimitive(type));
            Debug.Assert(!IsPrimitiveArray(type));

            var result = obj as ReadOnlyModel;
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

                return CreateCore(properties.AsSpan(0, objectProperties.Length));
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