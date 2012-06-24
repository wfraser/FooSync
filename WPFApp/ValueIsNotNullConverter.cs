///
/// Codewise/FooSync/WPFApp/ValueIsNotNullConverter.cs
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
using System.Windows;

namespace Codewise.FooSync.WPFApp
{
    /// <summary>
    /// Converts null to Visibility.Collapsed, and all other values to Visibility.Visible
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812",
        Justification="Only instantiated by XAML")]
    class ValueIsNotNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value != null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
