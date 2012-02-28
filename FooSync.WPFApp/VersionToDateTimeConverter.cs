using System;
using System.Globalization;
using System.Windows.Data;

namespace FooSync.WPFApp
{
    public class VersionToDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if (!(value is Version))
            {
                throw new ArgumentException("Input value must be a Version object (it is a " + value.GetType().FullName + ")", "value");
            }

            var v = (Version)value;

            if (v.Build != 0 && v.Revision != 0)
            {
                return new DateTime(2000, 1, 1).AddDays(v.Build).AddSeconds(v.Revision * 2);
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
