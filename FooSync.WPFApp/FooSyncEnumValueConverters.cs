///
/// Codewise/FooSync/WPFApp/FooSyncEnumValueConverters.cs
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

namespace Codewise.FooSync.WPFApp
{
    public class FileOperationValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(int))
            {
                throw new ArgumentException("Can only convert FileOperation to int", "targetType");
            }

            if (value is FooSync.FileOperation)
            {
                return (int)value;
            }
            else
            {
                return -1;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(FooSync.FileOperation))
            {
                throw new ArgumentException("Can only convert to FileOperation", "targetType");
            }

            return (FooSync.FileOperation)((int)value);
        }
    }

    public class ConflictStatusValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(int))
            {
                throw new ArgumentException("Can only convert ConflictStatus to int", "targetType");
            }

            if (value is FooSync.ConflictStatus)
            {
                return (int)value;
            }
            else
            {
                return -1;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(FooSync.ConflictStatus))
            {
                throw new ArgumentException("Can only convert to ConflictStatus", "targetType");
            }

            return (FooSync.ConflictStatus)value;
        }
    }
}
