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

namespace FooSync.WPFApp
{
    /// <summary>
    /// Interaction logic for CreateRepositoryWindow.xaml
    /// </summary>
    public partial class CreateRepositoryWindow : Window
    {
        public CreateRepositoryWindow()
        {
            InitializeComponent();
        }

        public string RepositoryPath { get; set; }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            var config = new RepositoryConfig();
            config.RepositoryName = RepositoryName.Text;
            //config.RepositoryPath = 

            //RepositoryConfigLoader.WriteRepositoryConfig(config, System.IO.Path.Combine(, FooSyncEngine.ConfigFileName));

            Close();
        }
    }
}
