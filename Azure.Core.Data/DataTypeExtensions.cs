using System;

namespace Azure.Data
{
    static class DataTypeExtensions
    {
        public static bool IsDynamicDataType(this Type type)
        {
            return IsDynamicDataPrimitive(type) || typeof(DynamicData).IsAssignableFrom(type);
        }

        public static bool IsDynamicDataPrimitive(this Type type)
        {
            if (type == typeof(string)) return true;
            if (type.IsPrimitive) return true;
            if (type.IsArray && IsDynamicDataPrimitive(type.GetElementType())) return true;
            return false;
        }
        public static bool IsPrimitive(this Type type)
        {
            if (type == typeof(string)) return true;
            if (type.IsPrimitive) return true;
            return false;
        }
        public static bool IsPrimitiveArray(this Type type)
        {
            if (type.IsArray && IsPrimitive(type.GetElementType())) return true;
            return false;
        }
    }
}