using System.Collections.Generic;
using System;

namespace Azure.Data
{
    public abstract class DataStore 
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        /// <remarks>When implemented, propertyValue has to be either a primitive (see IsPrimitive or IsPrimitiveArray), or a DynamicData instance.</remarks>
        internal protected abstract bool TryGetValue(string propertyName, out object propertyValue);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="propertyValue"></param>
        /// <returns></returns>
        /// <remarks>When implemented, propertyValue has to be either a primitive (IsPrimitive or IsPrimitiveArray), or a DynamicData instance.</remarks>
        internal protected abstract bool TryGetValueAt(int index, out object item);

        // TODO: why isn't it a Try?
        /// <summary>
        /// 
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        /// <remarks>When implemented, propertyValue can be assumed to be a primitive (IsPrimitive or IsPrimitiveArray), or a DynamicData instance.</remarks>
        internal protected abstract void SetValue(string propertyName, object propertyValue);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        /// <remarks>When implemented, propertyValue can be assumed to be a primitive (IsPrimitive or IsPrimitiveArray), or a DynamicData instance.</remarks>
        internal protected abstract DynamicData CreateDynamicData(ReadOnlySpan<(string propertyName, object propertyValue)> properties);

        // TODO: should this be non-virtual? Why do we need many implementations? Should this return DynamicData?
        internal protected abstract bool TryConvertTo(Type type, out object converted);

        internal protected abstract IEnumerable<string> PropertyNames { get; }

        internal protected abstract bool IsReadOnly { get; }
    }
}