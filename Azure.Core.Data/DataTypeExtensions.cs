using System;

namespace Azure.Data
{
    static class DataTypeExtensions
    {
        public static bool IsDynamicDataType(this Type type)
        {
            return IsDynamicDataPrimitive(type) || typeof(DynamicData).IsAssignableFrom(type);
        }
        // primitives are: Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single.
        // TODO (Pri 1): What about: decimal, DateTime, DateTimeOffset, TimeSpan
        public static bool IsDynamicDataPrimitive(this Type type)
        {
            if (type == typeof(string)) return true;
            if (type.IsPrimitive) return true;
            if (type.IsArray && IsDynamicDataPrimitive(type.GetElementType())) return true;
            return false;
        }
    }
}