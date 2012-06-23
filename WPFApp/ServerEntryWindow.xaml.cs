///
/// Codewise/FooSync/WPFApp/ServerEntryWindow.xaml.cs
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

namespace Codewise.FooSync.WPFApp
{
    /// <summary>
    /// Interaction logic for ServerEntryWindow.xaml
    /// </summary>
    public partial class ServerEntryWindow : Window
    {
        public ICollection<string> Repositories { get; private set; }
        public string ReportedServerName { get; private set; }
        public string ServerDescription { get; private set; }

        private FooSyncUrl _url;

        public ServerEntryWindow()
        {
            InitializeComponent();
            Repositories = new List<string>();
            ServerNameEntry.Focus();
            ServerNameEntry.CaretIndex = ServerNameEntry.Text.Length;
        }

        public string ServerName
        {
            get
            {
                if (!ServerNameEntry.Text.Contains(':'))
                {
                    return ServerNameEntry.Text;
                }
                else
                {
                    return ServerNameEntry.Text.Split(new char[] { ':' }, 2)[0];
                }
            }
        }

        public short ServerPort
        {
            get
            {
                if (!ServerNameEntry.Text.Contains(':'))
                {
                    return FooSyncUrl.DefaultPort;
                }
                else
                {
                    return short.Parse(ServerNameEntry.Text.Split(new char[] { ':' }, 2)[1]);
                }
            }
        }

        public string Username
        {
            get
            {
                return (UsernameAndPassword.IsChecked ?? false)
                    ? UsernameEntry.Text : null;
            }
        }

        public string Password
        {
            get
            {
                return (UsernameAndPassword.IsChecked ?? false)
                    ? PasswordEntry.Password : null;
            }
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
                _url = new FooSyncUrl("fs://" + ServerNameEntry.Text);
            }
            catch (FormatException)
            {
                ErrorText.Text = "Invalid hostname.";
                ErrorText.Visibility = System.Windows.Visibility.Visible;
                return;
            }

            string username = (UsernameAndPassword.IsChecked ?? false) ? UsernameEntry.Text : null;
            SecureString password = (UsernameAndPassword.IsChecked ?? false) ? PasswordEntry.SecurePassword : null;
            var client = new NetClient(MainWindow.Foo, _url.Host, _url.Port, username, password);

            using (var connectionWorker = new BackgroundWorker())
            {
                connectionWorker.DoWork += new DoWorkEventHandler(connectionWorker_DoWork);
                connectionWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(connectionWorker_RunWorkerCompleted);
                connectionWorker.RunWorkerAsync(client);
            }
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
                ReportedServerName = client.ReportedHostname;
                ServerDescription = client.ServerDescription;
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
