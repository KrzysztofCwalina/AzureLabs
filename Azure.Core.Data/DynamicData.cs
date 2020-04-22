using System.Collections.Generic;
using System.Text;
using System.Dynamic;
using System.Linq.Expressions;
using System.ComponentModel;
using System.Collections;
using System;
using System.IO;
using System.Reflection;

namespace Azure.Data
{
    public abstract class DynamicData : IDynamicMetaObjectProvider, IEnumerable<string>
    {
        static DynamicData s_empty = new ReadOnlyDictionaryData();

        internal DynamicData() { }         // internal, as we don't want to make it publicly extensible yet.

        public static DynamicData Create(params (string propertyName, object propertyValue)[] properties)
            => new ReadWriteDictionaryData(properties);

        public static DynamicData CreateReadOnly(params (string propertyName, object propertyValue)[] properties)
        {
            if (properties.Length == 0) return s_empty;
            return new ReadOnlyDictionaryData(properties);
        }

        public static DynamicData FromDictionary(IDictionary<string, object> properties) => new ReadWriteDictionaryData(properties);
        public static DynamicData FromReadOnlyDictionary(IReadOnlyDictionary<string, object> properties) => new ReadOnlyDictionaryData(properties);
        public static DynamicData FromJson(string jsonObject) => new ReadOnlyJsonData(jsonObject);
        public static DynamicData FromJson(Stream jsonObject) => new ReadOnlyJsonData(jsonObject);

        #region Abstract Members
        protected abstract void SetPropertyCore(string propertyName, object propertyValue);
        protected abstract bool TryGetPropertyCore(string propertyName, out object propertyValue);
        protected abstract bool TryConvertToCore(Type type, out object converted);

        protected abstract IEnumerable<string> PropertyNames { get; }
        public abstract bool IsReadOnly { get; }
        #endregion

        public object this[string propertyName] {
            get => GetProperty(propertyName);
            set => SetProperty(propertyName, value);
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new MetaObject(parameter, this);

        protected void ThrowReadOnlyException() => throw new InvalidOperationException("This dynamic data object is read-only.");
        protected void EnsureNotReadOnly()
        {
            if (IsReadOnly) ThrowReadOnlyException();
        }

        private object GetProperty(string propertyName)
        {
            if (TryGetPropertyCore(propertyName, out object value))
            {
                if (IsPrimitive(value)) return value;
                if (value is DynamicData) return value;
                else throw new Exception("TryGetPropertyCore returned invalid object");
            }
            throw new InvalidOperationException("Property not found");
        }

        private object SetProperty(string propertyName, object propertyValue)
        {
            EnsureNotReadOnly();
            if (!IsPrimitive(propertyValue))
            {
                int debth = 10;
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

        private class MetaObject : DynamicMetaObject
        {
            internal MetaObject(Expression parameter, DynamicData value) : base(parameter, BindingRestrictions.Empty, value)
            { }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                Expression targetObject = Expression.Convert(Expression, LimitType);
                var methodImplementation = typeof(DynamicData).GetMethod(nameof(SetProperty), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var arguments = new Expression[2] { Expression.Constant(binder.Name), Expression.Convert(value.Expression, typeof(object)) };

                Expression setPropertyCall = Expression.Call(targetObject, methodImplementation, arguments);
                BindingRestrictions restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);
                DynamicMetaObject setProperty = new DynamicMetaObject(setPropertyCall, restrictions);
                return setProperty;
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                var targetObject = Expression.Convert(Expression, LimitType);
                var methodIplementation = typeof(DynamicData).GetMethod(nameof(GetProperty), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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

        protected abstract DynamicData CreateCore((string propertyName, object value)[] properties);

        private DynamicData FromComplex(object obj, ref int debth)
        {
            if (debth-- < 0) throw new InvalidOperationException("Object grath too deep");

            if (IsPrimitive(obj)) throw new ArgumentException("Argument passed to obj is a primitive");

            var result = obj as DynamicData;
            if (result != null) return result;

            var type = obj.GetType();

            var objectProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var properties = new (string, object)[objectProperties.Length];
            for(int i=0; i< objectProperties.Length; i++)
            {
                var property = objectProperties[i];
                string name = property.Name;
                object value = property.GetValue(obj);
                if (!IsPrimitive(value)) value = FromComplex(value, ref debth); // TODO: what about cycles?
                properties[i] = (name, value);
            }

            return CreateCore(properties);
        }

        // TODO: this needs to be fixed
        // primitives are: Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single.
        // What about: decimal, DateTime, DateTimeOffset, TimeSpan
        private bool IsPrimitive(object obj)
        {
            var type = obj.GetType();
            if (type == typeof(string)) return true;
            if (type.IsPrimitive) return true;
            if (type == typeof(decimal)) return true;
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