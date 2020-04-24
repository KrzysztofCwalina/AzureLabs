using System;
using System.Runtime.CompilerServices;

namespace Azure.Data
{
    public readonly struct Number
    {
        readonly long _value;

        private Number(long value) => _value = value;

        public static explicit operator Number(int from) => new Number(from);
        public static explicit operator Number(long from) => new Number(from);
        public static explicit operator Number(double from)
        {
            var value = Unsafe.As<double, long>(ref from);
            return new Number(value);
        }

        public static explicit operator int(Number from) => (int)from._value;
        public static explicit operator long(Number from) => (long)from._value;
        public static explicit operator double(Number from)
        {
            var value = from._value;
            return Unsafe.As<long, double>(ref value);
        }
    }
}