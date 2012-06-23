///
/// Codewise/FooSync/WPFApp2/TreeViewExtensions.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

///
/// The general method in SetSelected comes from:
///     http://quickduck.com/blog/2008/12/11/selecting-an-item-in-a-treeview-in-wpf/
/// 

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Codewise.FooSync.WPFApp2
{
    public static class TreeViewExtensions
    {
        /// <summary>
        /// Select the item in the tree view that satisfies a number of predicates along the path
        /// to it.
        /// </summary>
        /// <param name="treeView">TreeView root</param>
        /// <param name="predicates">Sequence of predicates to apply at each level of the tree, to
        /// determine which nodes to expand.</param>
        /// <returns>true if all predicates were matched (or search interrupted due to unexpanded
        /// items)</returns>
        public static bool SelectPath(this TreeView treeView, Predicate<object>[] predicates)
        {
            return SetSelected(treeView, predicates);
        }

        private static bool SetSelected(ItemsControl parent, Predicate<object>[] predicates)
        {
            if (parent == null)
            {
                return false;
            }

            if (predicates.Count() == 0)
            {
                //
                // No more predicates. All done.
                //
                return true;
            }

            foreach (object child in parent.Items)
            {
                if (predicates.First()(child))
                {
                    if (parent.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                    {
                        TreeViewItem childNode = parent.ItemContainerGenerator.ContainerFromItem(child) as TreeViewItem;

                        if (childNode != null)
                        {
                            childNode.IsExpanded = true;
                            childNode.Focus();
                            childNode.IsSelected = true;

                            return SetSelected(childNode as ItemsControl, predicates.Skip(1).ToArray());
                        }

                        return false;
                    }
                    else
                    {
                        //
                        // Items aren't available yet.
                        // Register an event handler to continue the search when they become available.
                        //

                        EventHandler statusChangedHandler = null;

                        statusChangedHandler = delegate(object sender, EventArgs e)
                        {
                            ItemContainerGenerator generator = sender as ItemContainerGenerator;

                            if (generator == null || generator.Status != GeneratorStatus.ContainersGenerated)
                            {
                                return;
                            }

                            try
                            {
                                TreeViewItem childNode = generator.ContainerFromItem(child) as TreeViewItem;

                                if (childNode != null)
                                {
                                    childNode.IsExpanded = true;
                                    childNode.Focus();
                                    childNode.IsSelected = true;

                                    if (SetSelected(childNode as ItemsControl, predicates.Skip(1).ToArray()))
                                    {
                                        return;
                                    }
                                }
                            }
                            finally
                            {
                                generator.StatusChanged -= statusChangedHandler;
                            }
                        };

                        parent.ItemContainerGenerator.StatusChanged += statusChangedHandler;

                        //
                        // Return true here, even though we haven't worked through all the 
                        // predicates.
                        //
                        // (I would put a wait handle here and wait for the status changed
                        // delegate, but everything happens in the same thread.)
                        //

                        return true;
                    }
                }
            }

            return false;
        }
    }
}
