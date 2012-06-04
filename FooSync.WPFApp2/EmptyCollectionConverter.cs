///
/// Codewise/FooSync/WPFApp2/EmptyCollectionConverter.cs
/// 
/// by William R. Fraser
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
    public class EmptyCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (!(value is ICollection))
                throw new ArgumentException(GetType().Name + " only works with collections", "value");

            var coll = (ICollection)value;

            if (coll.Count == 0)
            {
                return null;
            }

            return value;
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
