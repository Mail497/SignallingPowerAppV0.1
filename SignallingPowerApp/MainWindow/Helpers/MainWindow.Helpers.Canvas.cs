using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SignallingPowerApp.Core;

namespace SignallingPowerApp
{
    /// <summary>
    /// Canvas-related helper methods for MainWindow
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Helper method to find the parent Canvas of an element
        /// </summary>
        private Canvas? FindParentCanvas(DependencyObject child)
        {
            DependencyObject? parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is Canvas canvas)
                    return canvas;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        /// <summary>
        /// Helper method to find a parent of a specific type in the visual tree
        /// </summary>
        private T? FindParentOfType<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typedParent)
                    return typedParent;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        /// <summary>
        /// Gets the currently active canvas and its container from the selected tab
        /// </summary>
        /// <returns>A tuple containing the canvas container Grid and the Canvas, or null if not found</returns>
        private (Grid? container, Canvas? canvas) GetActiveCanvas()
        {
            var canvasTabs = FindName("CanvasTabs") as TabControl;
            if (canvasTabs?.SelectedItem is not TabItem selectedTab)
                return (null, null);

            // Find the Grid container in the tab content
            if (selectedTab.Content is not Grid container)
                return (null, null);

            // Find the Canvas in the container
            Canvas? canvas = container.Children.Count > 0 ? container.Children[0] as Canvas : null;
            
            return (container, canvas);
        }

        /// <summary>
        /// Gets the canvas for a block based on its parent relationship
        /// </summary>
        private Canvas? GetCanvasForBlock(IBlock block)
        {
            if (block is IInteractiveBlock interactive)
            {
                return interactive.PreferredCanvas switch
                {
                    CanvasType.Layout => DiagramCanvas,
                    CanvasType.Location => GetLocationCanvasForBlock(block),
                    _ => null
                };
            }
            return null;
        }

        /// <summary>
        /// Gets the location canvas for a block based on its parent relationship
        /// </summary>
        private Canvas? GetLocationCanvasForBlock(IBlock block)
        {
            Location? parentLocation = null;

            // Find the parent location
            if (block.ParentID >= 0)
            {
                var parent = _currentProject?.GetBlock(block.ParentID);
                if (parent is Location loc)
                {
                    parentLocation = loc;
                }
                else if (parent != null && parent.ParentID >= 0)
                {
                    var grandparent = _currentProject?.GetBlock(parent.ParentID);
                    if (grandparent is Location grandLoc)
                    {
                        parentLocation = grandLoc;
                    }
                }
            }

            if (parentLocation == null) return null;

            // GetLocationCanvas is defined in MainWindow.Connection.Lines.cs with connection-specific logic
            return GetLocationCanvas(parentLocation);
        }

        // Note: GetLocationCanvas is implemented in MainWindow.Connection.Lines.cs
        // to avoid duplication while keeping connection-specific logic centralized.
    }
}
