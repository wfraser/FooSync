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

        /// <summary>
        /// Validate all paths in the TextBox elements of the selected setup tab. Highlites invalid
        /// paths.
        /// </summary>
        /// <returns>true if no errors, false otherwise.</returns>
        private bool ValidatePaths()
        {
            var errorElements = new List<Control>();

            if (string.IsNullOrEmpty(RepositoryName.Text))
            {
                errorElements.Add(RepositoryName);
            }

            if (SetupTabs.SelectedItem == SimpleSetupTab)
            {
                if (string.IsNullOrEmpty(SimpleRepositoryPath.Text) || !Directory.Exists(SimpleRepositoryPath.Text))
                    errorElements.Add(SimpleRepositoryPath);

                if (string.IsNullOrEmpty(SimpleSourcePath.Text) || !Directory.Exists(SimpleSourcePath.Text))
                    errorElements.Add(SimpleSourcePath);
            }
            else if (SetupTabs.SelectedItem == AdvancedSetupTab)
            {
                if (string.IsNullOrEmpty(RepositoryLocation.Text) || !Directory.Exists(RepositoryLocation.Text))
                    errorElements.Add(RepositoryLocation);

                if (string.IsNullOrEmpty(SubdirName.Text) || SubdirName.Text.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                    errorElements.Add(SubdirName);

                if (string.IsNullOrEmpty(SubdirSourcePath.Text) || !Directory.Exists(SubdirSourcePath.Text))
                    errorElements.Add(SubdirSourcePath);
            }

            if (errorElements.Count > 0)
            {
                ValidationErrors.Content = string.Format("{0} required field{1} empty or invalid", errorElements.Count, (errorElements.Count == 1) ? string.Empty : "s");

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
                return false;
            }

            ValidationErrors.Content = "";

            return true;
        }

        /// <summary>
        /// Handles clicks on the Create Repository button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidatePaths())
                return;

            if (SetupTabs.SelectedItem == SimpleSetupTab)
            {
                DialogResult = SimpleSetup();
            }
            else if (SetupTabs.SelectedItem == AdvancedSetupTab)
            {
                DialogResult = AdvancedSetup();
            }

            Close();
        }

        /// <summary>
        /// Handles creating and writing a new RepositoryConfig when the user has used the Simple Setup tab.
        /// </summary>
        /// <returns>true if successful.</returns>
        private bool SimpleSetup()
        {
            RepositoryPath = SimpleRepositoryPath.Text;

            var config = new RepositoryConfig();
            config.RepositoryName = RepositoryName.Text;
            config.Filename = Path.Combine(RepositoryPath, FooSyncEngine.ConfigFileName);
            config.Directories = new RepositoryDirectory[]
            {
                new RepositoryDirectory()
                {
                    Path = ".",

                    Sources = new RepositorySource[]
                    {
                        new RepositorySource()
                        {
                            Name = Environment.MachineName.ToLower(),
                            Path = SimpleSourcePath.Text
                        }
                    }
                }
            };

            RepositoryConfigLoader.WriteRepositoryConfig(config, config.Filename);

            return true;
        }

        /// <summary>
        /// Handles creating and writing a new RepositoryConfig when the user has used the Advanced Setup tab.
        /// </summary>
        /// <returns>true if successful.</returns>
        private bool AdvancedSetup()
        {
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

            return true;
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

        /// <summary>
        /// Handles a click on a Browse button.
        /// The Button that sends this should have a Tag attribute populated with a reference to
        /// the TextBox which is to receive the result of the browsing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Presents the user with a folder browser dialog.
        /// </summary>
        /// <param name="filter">Filename filter for if the OS doesn't support a folder browser.</param>
        /// <returns>Full path the user selected.</returns>
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

        /// <summary>
        /// Invoked when user changes the Repository Location.
        /// If the Repository Name field is empty, it is set to the repository directory name.
        /// 
        /// If the Advanced Setup tab is open, this also populates the list of subdirectories in
        /// the Subdirectory Name combobox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RepositoryLocation_TextChanged(object sender, RoutedEventArgs e)
        {
            // don't update until after the textbox has lost focus
            if (((TextBox)sender).IsFocused)
            {
                if (!_lostFocusTextBoxes.Contains((TextBox)sender))
                {
                    ((TextBox)sender).LostFocus += RepositoryLocation_TextChanged;
                    _lostFocusTextBoxes.Add((TextBox)sender);
                }
                return;
            }
            else if (_lostFocusTextBoxes.Contains((TextBox)sender))
            {
                ((TextBox)sender).LostFocus -= RepositoryLocation_TextChanged;
            }

            var dir = ((TextBox)sender).Text;

            if (string.IsNullOrEmpty(RepositoryName.Text))
            {
                RepositoryName.Text = Path.GetFileName(dir);
            }
            
            if (SetupTabs.SelectedItem == AdvancedSetupTab)
            {
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
        }

        public string RepositoryPath { get; set; }

        private Brush _defaultTextBoxBorderBrush;
        private HashSet<TextBox> _lostFocusTextBoxes = new HashSet<TextBox>();
    }
}
