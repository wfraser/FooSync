///
/// Codewise/FooSync/WPFApp2/ServerEntryWindow.xaml.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections.Generic;
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
    /// Interaction logic for ServerEntryWindow.xaml
    /// </summary>
    public partial class ServerEntryWindow : Window
    {
        public ServerEntryWindow()
        {
            InitializeComponent();
            ServerUri.Focus();
            ServerUri.CaretIndex = ServerUri.Text.Length;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Uri uri = null;
            try
            {
                uri = new Uri(ServerUri.Text);
            }
            catch (FormatException) { }

            if (uri == null || uri.Scheme != "fs")
            {
                ErrorText.Text = "Invalid URL; it must be of the form \"fs://hostname\"";
                ErrorText.Visibility = System.Windows.Visibility.Visible;
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
