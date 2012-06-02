///
/// Codewise/FooSync/WPFApp2/VersionToDateTimeConverter.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Globalization;
using System.Windows.Data;

namespace Codewise.FooSync.WPFApp2
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

            if (v.Build != 0)
            {
                var time = new DateTime(2000, 1, 1).AddDays(v.Build).AddSeconds(v.Revision * 2);
                if (time.IsDaylightSavingTime())
                    time = time.AddHours(1);
                return time;
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
