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
    public class CollectionItemAdderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
                return value;

            if (!(value is ICollection))
                throw new ArgumentException("CollectionItemAdderConverter only works with collections", "value");

            var newList = new ArrayList();
            newList.AddRange(value as ICollection);

            if (parameter is DataTemplate)
            {
                var newObject = ((DataTemplate)parameter).LoadContent();
                newList.Add(newObject);
            }
            else
            {
                newList.Add(parameter);
            }

            return newList;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
