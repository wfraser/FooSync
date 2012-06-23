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
    /// <summary>
    /// Makes URLs prettier by removing the scheme part, and switching forward slashes to
    /// backslashes in local Windows paths.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812",
        Justification = "Only instantiated by XAML")]
    class UrlPrettifierConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            IEnumerable inputList = value as IEnumerable;
            if (inputList != null)
            {
                var outputList = new List<string>();

                foreach (var item in inputList)
                {
                    outputList.Add(Prettify(item.ToString()));
                }

                return outputList;
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

        private static string Prettify(string input)
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
