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
    public abstract class ReadWriteModel : ReadOnlyModel
    {
        internal ReadWriteModel() { } // internal, as we don't want to make it publicly extensible yet.

        public static async Task<ReadOnlyModel> CreateFromJsonAsync(Stream jsonObject, CancellationToken cancellationToken = default) => await ReadOnlyJson.CreateAsync(jsonObject, cancellationToken);

        public new object this[string propertyName] {
            get => base[propertyName];
            set => SetProperty(propertyName, value);
        }

        #region Abstract Members
        protected abstract void SetPropertyCore(string propertyName, object propertyValue);
        #endregion

        internal override DynamicMetaObject GetMetaObjectCore(Expression parameter) => new MetaObject(parameter, this);

        private class MetaObject : ReadOnlyMetaObject
        {
            internal MetaObject(Expression parameter, ReadWriteModel value) : base(parameter, value)
            { }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                Expression targetObject = Expression.Convert(Expression, LimitType);
                var methodImplementation = typeof(ReadWriteModel).GetMethod(nameof(SetProperty), BindingFlags.NonPublic | BindingFlags.Instance);
                var arguments = new Expression[2] { Expression.Constant(binder.Name), Expression.Convert(value.Expression, typeof(object)) };

                Expression setPropertyCall = Expression.Call(targetObject, methodImplementation, arguments);
                BindingRestrictions restrictions = BindingRestrictions.GetTypeRestriction(Expression, LimitType);
                DynamicMetaObject setProperty = new DynamicMetaObject(setPropertyCall, restrictions);
                return setProperty;
            }
        }

        private protected virtual object SetProperty(string propertyName, object propertyValue)
        {
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
    }
}