using System;
using System.Globalization;
using System.Windows.Data;

namespace FooSync.WPFApp
{
    public class FileOperationValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FooSync.FileOperation)
            {
                var v = (FooSync.FileOperation)value;

                switch (v)
                {
                    case FileOperation.DeleteRepo:
                        return "← Delete";
                    case FileOperation.DeleteSource:
                        return "Delete →";
                    case FileOperation.NoOp:
                        return "Do Nothing";
                    case FileOperation.UseRepo:
                        return "← Copy Repository";
                    case FileOperation.UseSource:
                        return "Copy Source →";
                    default:
                        return "(invalid)";
                }
            }
            else
            {
                return "(invalid)";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
