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
using System.ComponentModel;
using System.Linq;
using System.Security;
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
        public ICollection<string> Repositories { get; private set; }

        private FooSyncUrl _url;

        public ServerEntryWindow()
        {
            InitializeComponent();
            Repositories = new List<string>();
            ServerUri.Focus();
            ServerUri.CaretIndex = ServerUri.Text.Length;
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Submit(object sender, RoutedEventArgs e)
        {
            _url = null;
            try
            {
                _url = new FooSyncUrl(ServerUri.Text);
            }
            catch (FormatException)
            {
                ErrorText.Text = "Invalid URL; it must be of the form \"fs://hostname\"";
                ErrorText.Visibility = System.Windows.Visibility.Visible;
                return;
            }

            string username = (UsernameAndPassword.IsChecked ?? false) ? Username.Text : null;
            SecureString password = (UsernameAndPassword.IsChecked ?? false) ? Password.SecurePassword : null;
            var client = new NetClient(MainWindow.Foo, _url.Host, _url.Port, username, password);

            var connectionWorker = new BackgroundWorker();
            connectionWorker.DoWork += new DoWorkEventHandler(connectionWorker_DoWork);
            connectionWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(connectionWorker_RunWorkerCompleted);
            connectionWorker.RunWorkerAsync(client);
        }

        void connectionWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result != null)
                DialogResult = true;
        }

        void connectionWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            OKButton.Dispatcher.Invoke(new Action(() => 
            {
                ErrorText.Visibility = System.Windows.Visibility.Collapsed;
                OKButton.IsEnabled = false;
                OKButton.Content = "checking...";
            }));

            try
            {
                var client = e.Argument as NetClient;
                Repositories = client.ListRepositories();
                e.Result = true;
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    ErrorText.Text = ex.Message;
                    ErrorText.Visibility = System.Windows.Visibility.Visible;
                    OKButton.IsEnabled = true;
                    OKButton.Content = "OK";
                }));
                e.Result = null;
            }
        }
    }
}
