using System;
using System.Globalization;
using System.Windows.Data;

namespace FooSync.WPFApp
{
    public class NullDateTimeConverter : IValueConverter
    {
        static readonly string ReplacementString = "(none)";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (value as DateTime?);

            if (v == null || !v.HasValue)
            {
                return ReplacementString;
            }
            else
            {
                return v.Value;
            }
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
