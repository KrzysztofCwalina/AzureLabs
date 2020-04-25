using System.Collections.Generic;
using System;

namespace Azure.Data
{
    public abstract class DataStore 
    {
        internal protected abstract bool TryGetPropertyCore(string propertyName, out object propertyValue);

        internal protected abstract bool TryGetAtCore(int index, out object item);

        internal protected abstract bool TryConvertToCore(Type type, out object converted);

        internal protected abstract void SetPropertyCore(string propertyName, object propertyValue);

        internal protected abstract Data CreateCore(ReadOnlySpan<(string propertyName, object propertyValue)> properties);

        internal protected abstract IEnumerable<string> PropertyNames { get; }

        internal protected abstract bool IsReadOnly { get; }
    }
}