///
/// Codewise/FooSync/WPFApp2/UrlPrettifierConverter.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace Codewise.FooSync.WPFApp2
{
    class UrlPrettifierConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable)
            {
                var ret = new List<string>();

                foreach (var item in (IEnumerable)value)
                {
                    ret.Add(Prettify(item.ToString()));
                }

                return ret;
            }
            else
            {
                return Prettify(value.ToString());
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private string Prettify(string input)
        {
            var noScheme = Regex.Replace(input, "^[a-zA-Z]+:///?", string.Empty);

            if (Regex.Match(noScheme, "^[a-zA-Z]:/").Success)
            {
                noScheme = noScheme.Replace('/', '\\');
            }

            return noScheme;
        }
    }
}
