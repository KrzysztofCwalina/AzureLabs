using System;

namespace Azure.Data
{
    public abstract class DataConverter
    {
        public abstract Type ForType { get; }
        public abstract DynamicData ConvertToDataType(object obj);
        public abstract object ConverFromDataType(DynamicData data);
    }

    public class DateTimeConverter : DataConverter
    {
        public override Type ForType => typeof(DateTime);

        public override object ConverFromDataType(DynamicData data)
        {
            if (DateTime.TryParse(data.Value, out var dt))
            {
                return dt;
            }
            throw new InvalidOperationException();
        }

        public override DynamicData ConvertToDataType(object obj)
        {
            var dt = (DateTime)obj; 
            var data = new DynamicData(dt.ToString("O"));
            data.Converters.Add(typeof(DateTime), this);
            return data;
        }
    }
}