///
/// Codewise/FooSync/WPFApp/DictionaryItemPickerConverter.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
///

using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace Codewise.FooSync.WPFApp
{
    public class DictionaryItemPickerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            IDictionary dict = value as IDictionary;

            if (dict == null)
            {
                throw new ArgumentException("Input value must implement IDictionary (it is a " + value.GetType().FullName + ")", "value");
            }

            object item = dict[parameter];

            return item;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
