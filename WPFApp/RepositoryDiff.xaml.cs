using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for RepositoryDiff.xaml
    /// </summary>
    public partial class RepositoryDiff : UserControl
    {
        private SyncGroup _syncGroup;
        private bool _cancel;

        public event EventHandler Cancelled;

        public RepositoryDiff(SyncGroup syncGroup)
        {
            InitializeComponent();

            _syncGroup = syncGroup;
        }

        public void Start()
        {
            //TODO

            Thread workingThread = new Thread(
                delegate ()
                {
                    for (int i = 0; i <= 100; i++)
                    {
                        System.Threading.Thread.Sleep(50);
                        Dispatcher.Invoke(new Action(() => 
                            {
                                Progress.Value = i;
                                ProgressText.Text = string.Format("Not doing anything... {0}%", i);
                            }
                        ));

                        if (MainWindow.GetInstance().IsClosed || _cancel)
                        {
                            break;
                        }
                    }

                    Dispatcher.Invoke(new Action(() =>
                        {
                            ProgressView.Visibility = Visibility.Collapsed;
                            Grid.Visibility = Visibility.Visible;
                        }
                    ));
                }
            );

            workingThread.Name = "worker thread for sync group " + _syncGroup.Name;
            
            workingThread.Start();
        }

        private void Grid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // TODO
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cancel = true;

            if (Cancelled != null)
            {
                Cancelled(this, new EventArgs());
            }
        }

        private void Synchronize_Click(object sender, RoutedEventArgs e)
        {
            // TODO
        }
    }
}
