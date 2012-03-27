///
/// Codewise/FooSync/WPFApp/ImageTextButton.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Codewise.FooSync.WPFApp
{
    /// <summary>
    /// A button with an image and text below it.
    /// </summary>
    public class ImageTextButton : Button
    {
        static ImageTextButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageTextButton), new FrameworkPropertyMetadata(typeof(ImageTextButton)));
        }

        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register("ImageSource", typeof(ImageSource), typeof(ImageTextButton), new PropertyMetadata(null));
    }
}
