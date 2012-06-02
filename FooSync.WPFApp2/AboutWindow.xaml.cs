///
/// Codewise/FooSync/WPFApp2/AboutWindow.xaml.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Codewise.FooSync.WPFApp2
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            WPFAppVersionText.DataContext = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            FooSyncEngineVersionText.DataContext = typeof(FooSyncEngine).Assembly.GetName().Version;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo((sender as Hyperlink).NavigateUri.AbsoluteUri));
        }

    }
}
