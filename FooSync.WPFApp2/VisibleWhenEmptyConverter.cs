///
/// Codewise/FooSync/WPFApp2/VisibleWhenEmptyConverter.cs
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
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Codewise.FooSync.WPFApp2
{
    /// <summary>
    /// When given a collection as input value, returns Visibility.Visible if that collection is
    /// empty; otherwise returns Visibility.Collapsed.
    /// Useful for having a fallback value be displayed when the collection is empty.
    /// </summary>
    class VisibleWhenEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var coll = value as ICollection;

            if (coll == null || coll.Count == 0)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
