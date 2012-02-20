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

    public class ConflictStatusValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FooSync.ConflictStatus)
            {
                var v = (FooSync.ConflictStatus)value;

                switch (v)
                {
                    case ConflictStatus.NoConflict:
                        return "(no conflict)";
                    case ConflictStatus.RepoChanged:
                    case ConflictStatus.SourceChanged:
                        return "Both Changed";
                    case ConflictStatus.ChangedInRepoDeletedInSource:
                        return "Changed in Repository and Deleted in Source";
                    case ConflictStatus.ChangedInSourceDeletedInRepo:
                        return "Changed in Source and Deleted in Repository";
                    case ConflictStatus.Undetermined:
                        return "???";
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
