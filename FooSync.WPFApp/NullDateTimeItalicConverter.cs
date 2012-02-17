using System;
using System.Globalization;
using System.Windows.Data;

namespace FooSync.WPFApp
{
    public class NullDateTimeItalicConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (value as DateTime?);

            if (v == null || !v.HasValue)
            {
                return "Italic";
            }
            else
            {
                return "Normal";
            }
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
