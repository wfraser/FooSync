using System;
using System.Globalization;
using System.Windows.Data;

namespace FooSync.WPFApp
{
    public class MultiBindingAsArrayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            //return (object[])value;
            throw new NotImplementedException();
        }
    }
}
