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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FooSync.WPFApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Show();

            start = new StartWindow();
            start.Left = this.Left + (this.Width / 2) - (start.Width / 2);
            start.Top = this.Top + (this.Height / 2) - (start.Height / 2);
            start.Topmost = true;
            start.WindowStyle = System.Windows.WindowStyle.None;
            start.NewButton.Click += new RoutedEventHandler(NewRepository);
            start.OpenButton.Click += new RoutedEventHandler(OpenRepository);
            start.Show();
        }

        void EnableControls()
        {
        }

        void OpenRepository(object sender, RoutedEventArgs e)
        {
            EnableControls();
            start = null;
            MessageBox.Show("[TODO: Open Repository dialog]", "TODO", MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }

        void NewRepository(object sender, RoutedEventArgs e)
        {
            start = null;
            MessageBox.Show("[TODO: New Repository dialog]", "TODO", MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (start != null)
            {
                start.Close();
            }
        }

        private StartWindow start = null;
    }
}
