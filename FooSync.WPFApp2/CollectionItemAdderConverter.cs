///
/// Codewise/FooSync/WPFApp2/CollectionItemAdderConverter.cs
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
    /// <summary>
    /// "Converts" a collection by copying it and appending an item (the converter parameter) to it.
    /// </summary>
    public class CollectionItemAdderConverter : IValueConverter
    {
        
        /// <summary>
        /// "Converts" a collection by copying it and appending an item (the converter parameter)
        /// to it.
        /// </summary>
        /// <param name="value">Must implement ICollection (not generic!)</param>
        /// <param name="targetType">ignored</param>
        /// <param name="parameter">Object to append to the collection. If it is a DataTemplate,
        /// it is instantiated first.</param>
        /// <param name="culture">ignored</param>
        /// <returns>An ArrayList with the items in the input collection, plus the new one.</returns>
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

        /// <summary>
        /// Not supported.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
