///
/// Codewise/FooSync/WPFApp/CreateRepositoryWindow.xaml.cs
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
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ookii.Dialogs.Wpf;

namespace Codewise.FooSync.WPFApp
{
    /// <summary>
    /// Interaction logic for CreateRepositoryWindow.xaml
    /// </summary>
    public partial class CreateRepositoryWindow : Window
    {
        public CreateRepositoryWindow()
        {
            InitializeComponent();
            _defaultTextBoxBorderBrush = RepositoryName.BorderBrush;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            //
            // Validate controls
            //

            var errorElements = new List<Control>();

            if (string.IsNullOrEmpty(RepositoryName.Text))
            {
                errorElements.Add(RepositoryName);
            }

            if (string.IsNullOrEmpty(RepositoryLocation.Text) || !Directory.Exists(RepositoryLocation.Text))
            {
                errorElements.Add(RepositoryLocation);
            }

            if (string.IsNullOrEmpty(SubdirName.Text) || SubdirName.Text.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                errorElements.Add(SubdirName);
            }

            if (string.IsNullOrEmpty(SubdirSourcePath.Text) || !Directory.Exists(SubdirSourcePath.Text))
            {
                errorElements.Add(SubdirSourcePath);
            }

            if (errorElements.Count > 0)
            {
                ValidationErrors.Content = string.Format("{0} required fields empty or invalid", errorElements.Count);

                foreach (var elem in errorElements)
                {
                    elem.BorderBrush = Brushes.Crimson;
                    elem.BorderThickness = new Thickness(2);
                    if (elem is TextBox)
                    {
                        ((TextBox)elem).TextChanged += new TextChangedEventHandler(RemoveBorderBrush);
                    }
                    else if (elem is ComboBox)
                    {
                        ((ComboBox)elem).SelectionChanged += new SelectionChangedEventHandler(RemoveBorderBrush);
                        ((ComboBox)elem).PreviewTextInput += new System.Windows.Input.TextCompositionEventHandler(RemoveBorderBrush);
                    }
                }
                return;
            }

            ValidationErrors.Content = "";

            //
            // Create new RepositoryConfig
            //

            // caller needs this
            RepositoryPath = RepositoryLocation.Text;

            var subdir = SubdirName.Text;
            if (SubdirName.SelectedItem is ComboBoxItem && ((ComboBoxItem)SubdirName.SelectedItem).Tag.ToString().Equals("MainDirectory"))
            {
                subdir = ".";
            }

            var config = new RepositoryConfig();
            config.RepositoryName = RepositoryName.Text;
            config.Filename = Path.Combine(RepositoryPath, FooSyncEngine.ConfigFileName);
            config.Directories = new RepositoryDirectory[] 
            { 
                new RepositoryDirectory() 
                {
                    Path = subdir,

                    Sources = new RepositorySource[]
                    {
                        new RepositorySource()
                        {
                            Name = Environment.MachineName.ToLower(),
                            Path = SubdirSourcePath.Text
                        }
                    }
                }
            };

            RepositoryConfigLoader.WriteRepositoryConfig(config, config.Filename);

            DialogResult = true;

            Close();
        }

        private void RemoveBorderBrush(object sender, RoutedEventArgs e)
        {
            var c = (sender as Control);

            if (c != null)
            {
                c.BorderBrush = _defaultTextBoxBorderBrush;
                c.BorderThickness = new Thickness(1);
            }

            if (c is TextBox)
            {
                ((TextBox)c).TextChanged -= RemoveBorderBrush;
            }
            else if (c is ComboBox)
            {
                ((ComboBox)c).PreviewTextInput -= RemoveBorderBrush;
                ((ComboBox)c).SelectionChanged -= RemoveBorderBrush;
            }
        }

        private void FolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement)
            {
                var target = ((FrameworkElement)sender).Tag as TextBox;

                if (target != null)
                {
                    var folder = ShowFolderBrowser("FooSync Repository Config|" + FooSyncEngine.ConfigFileName);
                    if (folder != null)
                    {
                        target.Text = folder;
                    }
                }
                else
                {
                    throw new InvalidCastException("FolderBrowse_Click needs the sender to have a tag set to a TextBox instance.");
                }
            }
        }

        private string ShowFolderBrowser(string filter)
        {
            bool cancelled = false;
            string path = null;

            if (VistaFolderBrowserDialog.IsVistaFolderDialogSupported)
            {
                var dlg = new VistaFolderBrowserDialog();

                cancelled = !(dlg.ShowDialog() ?? false);

                if (!cancelled)
                {
                    path = Path.GetFullPath(dlg.SelectedPath);
                }
            }
            else
            {
                var dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.Filter = filter;
                dlg.FilterIndex = 1;

                cancelled = !(dlg.ShowDialog() ?? false);

                if (!cancelled)
                {
                    path = Path.GetFullPath(Path.GetDirectoryName(dlg.FileName)); // Discard whatever filename they chose
                }
            }

            return path;
        }

        private void RepositoryLocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            var dir = ((TextBox)sender).Text;
            if (Directory.Exists(dir))
            {
                SubdirName.DataContext = (from subdir in Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly)
                                          select Path.GetFileName(subdir));
            }
            else
            {
                SubdirName.DataContext = null;
            }
        }

        public string RepositoryPath { get; set; }

        private Brush _defaultTextBoxBorderBrush;
    }
}
