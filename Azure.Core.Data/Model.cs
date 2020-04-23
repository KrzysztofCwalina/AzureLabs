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
    public abstract class Model : ReadOnlyModel
    {
        internal Model() { } // internal, as we don't want to make it publicly extensible yet.

        // TODO: I really don't like that users cannot just new up an instance. But for this, we would need to put a filed in this abstraction.
        public static Model Create(params (string propertyName, object propertyValue)[] properties)
            => new ReadWriteDictionaryData(properties);

        public static Model CreateReadOnly(params (string propertyName, object propertyValue)[] properties)
        {
            return new ReadOnlyDictionaryData(properties);
        }

        public static Model CreateFromDictionary(IDictionary<string, object> properties) => new ReadWriteDictionaryData(properties);
        public static Model CreateFromDictionary(IReadOnlyDictionary<string, object> properties) => new ReadOnlyDictionaryData(properties);
        public static ReadOnlyModel CreateFromJson(string jsonObject) => new ReadOnlyJson(jsonObject);
        public static ReadOnlyModel CreateFromJson(Stream jsonObject) => new ReadOnlyJson(jsonObject);

        public static async Task<ReadOnlyModel> CreateFromJsonAsync(Stream jsonObject, CancellationToken cancellationToken = default) => await ReadOnlyJson.CreateAsync(jsonObject, cancellationToken);

        public new object this[string propertyName] {
            get => base[propertyName];
            set => SetProperty(propertyName, value);
        }

        #region Abstract Members
        public abstract bool IsReadOnly { get; }

        protected abstract void SetPropertyCore(string propertyName, object propertyValue);
        #endregion

        internal override DynamicMetaObject GetMetaObjectCore(Expression parameter) => new MetaObject(parameter, this);

        private class MetaObject : ReadOnlyMetaObject
        {
            internal MetaObject(Expression parameter, Model value) : base(parameter, value)
            { }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                Expression targetObject = Expression.Convert(Expression, LimitType);
                var methodImplementation = typeof(Model).GetMethod(nameof(SetProperty), BindingFlags.NonPublic | BindingFlags.Instance);
                var arguments = new Expression[2] { Expression.Constant(binder.Name), Expression.Convert(value.Expression, typeof(object)) };

                Expression setPropertyCall = Expression.Call(targetObject, methodImplementation, arguments);
                BindingRestrictions restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);
                DynamicMetaObject setProperty = new DynamicMetaObject(setPropertyCall, restrictions);
                return setProperty;
            }
        }

        private object SetProperty(string propertyName, object propertyValue)
        {
            EnsureNotReadOnly();
            var valueType = propertyValue.GetType();

            if (!IsPrimitive(valueType) && !IsPrimitiveArray(valueType))
            {
                int debth = 100; // TODO: is this a good default? Should it be configurable?
                if (valueType.IsArray)
                {
                    object[] array = (object[])propertyValue;
                    Model[] result = new Model[array.Length];
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

        protected static void ThrowReadOnlyException() => throw new InvalidOperationException("This dynamic data object is read-only.");
        protected void EnsureNotReadOnly()
        {
            if (IsReadOnly) ThrowReadOnlyException();
        }
    }
}