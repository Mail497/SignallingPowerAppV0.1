using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SignallingPowerApp.Core;

namespace SignallingPowerApp
{
    /// <summary>
    /// Location tab creation and management for canvas
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        ///     Handles double-click on a Location border
        /// </summary>
        private void LocationBorder_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Location location)
            {
                e.Handled = true;
                
                // Create a new canvas tab for this location
                CreateCanvasTabForLocation(location);
            }
        }

        /// <summary>
        ///     Creates a new canvas tab for a location
        /// </summary>
        /// <param name="location">The location to create a tab for</param>
        private void CreateCanvasTabForLocation(Location location)
        {
            // Get the canvas tabs control
            var canvasTabs = FindName("CanvasTabs") as TabControl;
            if (canvasTabs == null) return;

            // Check if a tab for this location already exists
            foreach (TabItem existingTab in canvasTabs.Items)
            {
                if (existingTab.Tag is Location existingLocation && existingLocation == location)
                {
                    // Tab already exists, just select it
                    canvasTabs.SelectedItem = existingTab;
                    return;
                }
            }

            // Create new tab item with closable style
            var newTab = new TabItem
            {
                Header = location.Name,
                Tag = location,
                Style = FindResource("ClosableTabItemStyle") as Style
            };

            // Create a new canvas container for this tab
            var canvasContainer = new Grid
            {
                Background = new SolidColorBrush(Colors.White),
                ClipToBounds = true
            };

            // Add mouse event handlers for pan and zoom
            canvasContainer.MouseLeftButtonDown += CanvasContainer_MouseLeftButtonDown;
            canvasContainer.MouseLeftButtonUp += CanvasContainer_MouseLeftButtonUp;
            canvasContainer.MouseMove += CanvasContainer_MouseMove;
            canvasContainer.MouseWheel += CanvasContainer_MouseWheel;

            // Create a new canvas
            var canvas = new Canvas
            {
                Width = 2000,
                Height = 1500
            };

            // Create grid background
            var drawingBrush = new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 60, 60),
                ViewportUnits = BrushMappingMode.Absolute
            };

            var drawingGroup = new DrawingGroup();
            
            var geometryDrawing1 = new GeometryDrawing
            {
                Brush = new SolidColorBrush(Colors.White),
                Geometry = new RectangleGeometry(new Rect(0, 0, 60, 60))
            };
            
            var geometryGroup = new GeometryGroup();
            geometryGroup.Children.Add(new LineGeometry(new Point(0, 0), new Point(60, 0)));
            geometryGroup.Children.Add(new LineGeometry(new Point(0, 0), new Point(0, 60)));
            
            var geometryDrawing2 = new GeometryDrawing
            {
                Geometry = geometryGroup,
                Pen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8E8E8")), 1)
            };
            
            drawingGroup.Children.Add(geometryDrawing1);
            drawingGroup.Children.Add(geometryDrawing2);
            drawingBrush.Drawing = drawingGroup;
            
            canvas.Background = drawingBrush;

            // Create transform group for pan and zoom
            var transformGroup = new TransformGroup();
            var scaleTransform = new ScaleTransform(1.0, 1.0);
            var translateTransform = new TranslateTransform(0, 0);
            transformGroup.Children.Add(scaleTransform);
            transformGroup.Children.Add(translateTransform);
            canvas.RenderTransform = transformGroup;

            // Store transforms as properties on the canvas for later access
            canvas.Tag = new { ScaleTransform = scaleTransform, TranslateTransform = translateTransform, ZoomLevel = 1.0, Location = location };

            // Check if location has any busbars, transformers, or loads and render them, otherwise show empty state
            var busbars = location.GetChildren().Where(b => b.BlockType == "Busbar").Cast<Busbar>().ToArray();
            var transformerUPSs = location.GetChildren().Where(b => b.BlockType == "TransformerUPS").Cast<TransformerUPSBlock>().ToArray();
            // Load blocks are directly parented to the location
            var loads = _currentProject.GetAllBlocks.Where(b => b.BlockType == "Load" && b.ParentID == location.ID)
                .Cast<Load>()
                .ToArray();
            
            // Always render the ExternalBusbar first
            var externalBusbar = location.GetChildren().Where(b => b.BlockType == "ExternalBusbar").Cast<ExternalBusbar>().FirstOrDefault();
            if (externalBusbar != null)
            {
                // Set initial render position if not already set
                if (externalBusbar.RenderPosition.Item1 == null || externalBusbar.RenderPosition.Item2 == null)
                {
                    // Position in the center of the canvas
                    const double nameWidth = 40;
                    const double rowsWidth = 100;
                    const double totalWidth = nameWidth + rowsWidth;
                    const double rowHeight = 50;
                    const int numberOfRows = 8;
                    double totalHeight = numberOfRows * rowHeight;
                    
                    // Center the external busbar on the canvas
                    int defaultX = (int)(canvas.Width / 2 - totalWidth / 2);
                    int defaultY = (int)(canvas.Height / 2 - totalHeight / 2);
                    
                    externalBusbar.RenderPosition = (defaultX, defaultY);
                }
                
                RenderExternalBusbarOnLocationCanvas(externalBusbar, canvas);
            }
            
            if (busbars.Length > 0 || transformerUPSs.Length > 0 || loads.Length > 0)
            {
                // Render existing busbars
                foreach (var busbar in busbars)
                {
                    RenderBusbarOnLocationCanvas(busbar, canvas);
                }
                
                // Render existing transformerUPS blocks
                foreach (var transformerUPS in transformerUPSs)
                {
                    RenderTransformerUPSOnLocationCanvas(transformerUPS, canvas);
                }
                
                // Render existing load blocks
                foreach (var load in loads)
                {
                    RenderLoadOnLocationCanvas(load, canvas);
                }
                
                // Add canvas to container before rendering connections (so GetLocationCanvas can find it)
                canvasContainer.Children.Add(canvas);
                
                // Set container as tab content
                newTab.Content = canvasContainer;
                
                // Add tab to tab control
                canvasTabs.Items.Add(newTab);
                
                // Render connection lines for this specific location canvas
                RenderConnectionLines(canvas);
                
                // Render connection dots if in connection edit mode
                if (_isConnectionEditMode)
                {
                    RenderConnectionDots();
                }
            }
            else if (externalBusbar != null)
            {
                // Canvas has external busbar but no other equipment - don't show empty state
                // Add canvas to container
                canvasContainer.Children.Add(canvas);
                
                // Set container as tab content
                newTab.Content = canvasContainer;
                
                // Add tab to tab control
                canvasTabs.Items.Add(newTab);
            }
            else
            {
                // Render empty state with plus button (location canvases start empty)
                RenderLocationCanvasEmptyState(canvas);
                
                // Add canvas to container
                canvasContainer.Children.Add(canvas);
                
                // Set container as tab content
                newTab.Content = canvasContainer;
                
                // Add tab to tab control
                canvasTabs.Items.Add(newTab);
            }

            // Select the new tab
            canvasTabs.SelectedItem = newTab;
            
            // Use Dispatcher to fit canvas after it's fully loaded and rendered
            Dispatcher.BeginInvoke(new Action(() =>
            {
                FitCanvasToBlocks();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        ///     Handles the close button click on a tab
        /// </summary>
        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                // Find the TabItem that contains this button
                var tabItem = FindParentOfType<TabItem>(button);
                if (tabItem != null)
                {
                    // Get the canvas tabs control
                    var canvasTabs = FindName("CanvasTabs") as TabControl;
                    if (canvasTabs != null && canvasTabs.Items.Contains(tabItem))
                    {
                        // Don't close if it's the only tab or if it's the Layout tab
                        if (canvasTabs.Items.Count > 1 && tabItem.Tag is Location)
                        {
                            // If this tab is currently selected, select another tab first
                            if (canvasTabs.SelectedItem == tabItem)
                            {
                                int index = canvasTabs.Items.IndexOf(tabItem);
                                // Select the previous tab, or the first tab if this is the first location tab
                                canvasTabs.SelectedIndex = index > 0 ? index - 1 : 0;
                            }
                            
                            // Remove the tab
                            canvasTabs.Items.Remove(tabItem);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Closes the location canvas tab for a given location (used when location is removed)
        /// </summary>
        private void CloseLocationCanvasTab(Location location)
        {
            var canvasTabs = FindName("CanvasTabs") as TabControl;
            if (canvasTabs == null) return;
            
            // Find the tab for this location
            TabItem? tabToRemove = null;
            foreach (TabItem tab in canvasTabs.Items)
            {
                if (tab.Tag is Location loc && loc.ID == location.ID)
                {
                    tabToRemove = tab;
                    break;
                }
            }
            
            // If found, remove it
            if (tabToRemove != null)
            {
                // If this tab is currently selected, select another tab first
                if (canvasTabs.SelectedItem == tabToRemove)
                {
                    int index = canvasTabs.Items.IndexOf(tabToRemove);
                    // Select the previous tab, or the first tab (Layout) if this is the first location tab
                    canvasTabs.SelectedIndex = index > 0 ? index - 1 : 0;
                }
                
                // Remove the tab
                canvasTabs.Items.Remove(tabToRemove);
            }
        }

        /// <summary>
        ///     Refreshes the location canvas for a given block (Busbar or TransformerUPS)
        /// </summary>
        private void RefreshLocationCanvas(IBlock? block)
        {
            if (block == null) return;
            
            // Get the parent location ID
            int parentLocationID = block.ParentID;
            if (parentLocationID == 0) return; // Not in a location
            
            // Find the location
            var location = _currentProject?.GetBlock(parentLocationID) as Location;
            if (location == null) return;
            
            // Find the tab for this location
            var canvasTabs = FindName("CanvasTabs") as TabControl;
            if (canvasTabs == null) return;
            
            foreach (TabItem tab in canvasTabs.Items)
            {
                if (tab.Tag is Location loc && loc == location)
                {
                    // Found the tab - refresh its canvas
                    if (tab.Content is Grid canvasContainer)
                    {
                        var canvas = canvasContainer.Children.OfType<Canvas>().FirstOrDefault();
                        if (canvas != null)
                        {
                            // Check if canvas was empty BEFORE clearing (more than just empty state button)
                            bool wasEmptyBefore = canvas.Children.Count <= 1; // 1 or 0 (might have empty state button)
                            
                            // Clear the canvas
                            canvas.Children.Clear();
                            
                            // Always render the ExternalBusbar first
                            var externalBusbar = location.GetChildren().Where(b => b.BlockType == "ExternalBusbar").Cast<ExternalBusbar>().FirstOrDefault();
                            if (externalBusbar != null)
                            {
                                RenderExternalBusbarOnLocationCanvas(externalBusbar, canvas);
                            }
                            
                            // Re-render all busbars and transformers in this location
                            var busbars = location.GetChildren().Where(b => b.BlockType == "Busbar").Cast<Busbar>().ToArray();
                            var transformerUPSs = location.GetChildren().Where(b => b.BlockType == "TransformerUPS").Cast<TransformerUPSBlock>().ToArray();
                            // Load blocks are directly parented to the location
                            var loads = _currentProject.GetAllBlocks.Where(b => b.BlockType == "Load" && b.ParentID == location.ID)
                                .Cast<Load>()
                                .ToArray();
                            
                            bool isEmptyNow = busbars.Length == 0 && transformerUPSs.Length == 0 && loads.Length == 0;
                            
                            if (busbars.Length > 0 || transformerUPSs.Length > 0 || loads.Length > 0)
                            {
                                foreach (var bb in busbars)
                                {
                                    RenderBusbarOnLocationCanvas(bb, canvas);
                                }
                                
                                foreach (var tfmr in transformerUPSs)
                                {
                                    RenderTransformerUPSOnLocationCanvas(tfmr, canvas);
                                }
                                
                                foreach (var load in loads)
                                {
                                    RenderLoadOnLocationCanvas(load, canvas);
                                }
                                
                                // Render connection lines for THIS specific location canvas
                                RenderConnectionLines(canvas);
                                
                                // Render connection dots if in connection edit mode
                                if (_isConnectionEditMode)
                                {
                                    RenderConnectionDots();
                                }
                            }
                            else if (externalBusbar != null)
                            {
                                // Canvas has external busbar but no other equipment - still render connection lines
                                RenderConnectionLines(canvas);
                                
                                // Render connection dots if in connection edit mode
                                if (_isConnectionEditMode)
                                {
                                    RenderConnectionDots();
                                }
                            }
                            else
                            {
                                // Render empty state
                                RenderLocationCanvasEmptyState(canvas);

                                // If tab is selected and canvas just became empty, fit to center on empty state
                                if (!wasEmptyBefore && isEmptyNow && canvasTabs.SelectedItem == tab)
                                {
                                    Dispatcher.BeginInvoke(new Action(() =>
                                    {
                                        FitCanvasToBlocks();
                                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                                }
                            }
                            
                            // Re-select the block if it was selected
                            if (block is Busbar selectedBusbar && _selectedBusbar == selectedBusbar)
                            {
                                SelectBusbar(selectedBusbar, null);
                            }
                            else if (block is TransformerUPSBlock selectedTransformer && _selectedTransformerUPS == selectedTransformer)
                            {
                                SelectTransformerUPS(selectedTransformer, null);
                            }
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        ///     Refreshes all location canvases (used when a row is removed)
        /// </summary>
        private void RefreshAllLocationCanvases()
        {
            var canvasTabs = FindName("CanvasTabs") as TabControl;
            if (canvasTabs == null || _currentProject == null) return;
            
            bool anyCanvasBecameEmpty = false;
            TabItem? selectedEmptyTab = null;
            
            // Clear connection lines collection for location canvases before clearing canvases
            // This prevents stale references when canvases are cleared
            var locationCanvases = new List<Canvas>();
            foreach (TabItem tab in canvasTabs.Items)
            {
                if (tab.Tag is Location && tab.Content is Grid canvasContainer)
                {
                    var canvas = canvasContainer.Children.OfType<Canvas>().FirstOrDefault();
                    if (canvas != null)
                    {
                        locationCanvases.Add(canvas);
                    }
                }
            }
            
            // Remove connection lines that belong to location canvases from the collection
            _connectionLines.RemoveAll(line => 
            {
                var parent = System.Windows.Media.VisualTreeHelper.GetParent(line);
                return locationCanvases.Contains(parent);
            });
            
            // Find all location tabs and refresh them
            foreach (TabItem tab in canvasTabs.Items)
            {
                if (tab.Tag is Location location)
                {
                    if (tab.Content is Grid canvasContainer)
                    {
                        var canvas = canvasContainer.Children.OfType<Canvas>().FirstOrDefault();
                        if (canvas != null)
                        {
                            // Check if canvas was empty before
                            bool wasEmptyBefore = canvas.Children.Count <= 1; // 1 or 0 (might have empty state)
                            
                            // Clear the canvas (this removes all visual elements including lines)
                            canvas.Children.Clear();
                            
                            // Always render the ExternalBusbar first
                            var externalBusbar = location.GetChildren().Where(b => b.BlockType == "ExternalBusbar").Cast<ExternalBusbar>().FirstOrDefault();
                            if (externalBusbar != null)
                            {
                                RenderExternalBusbarOnLocationCanvas(externalBusbar, canvas);
                            }
                            
                            // Re-render all busbars, transformers, and loads in this location
                            var busbars = location.GetChildren().Where(b => b.BlockType == "Busbar").Cast<Busbar>().ToArray();
                            var transformerUPSs = location.GetChildren().Where(b => b.BlockType == "TransformerUPS").Cast<TransformerUPSBlock>().ToArray();
                            // Load blocks are directly parented to the location
                            var loads = _currentProject.GetAllBlocks.Where(b => b.BlockType == "Load" && b.ParentID == location.ID)
                                .Cast<Load>()
                                .ToArray();
                            
                            bool isEmptyNow = busbars.Length == 0 && transformerUPSs.Length == 0 && loads.Length == 0;
                            
                            if (busbars.Length > 0 || transformerUPSs.Length > 0 || loads.Length > 0)
                            {
                                foreach (var bb in busbars)
                                {
                                    RenderBusbarOnLocationCanvas(bb, canvas);
                                }
                                
                                foreach (var tfmr in transformerUPSs)
                                {
                                    RenderTransformerUPSOnLocationCanvas(tfmr, canvas);
                                }
                                
                                foreach (var load in loads)
                                {
                                    RenderLoadOnLocationCanvas(load, canvas);
                                }
                                
                                // Render connection lines for THIS specific location canvas
                                RenderConnectionLines(canvas);
                            }
                            else if (externalBusbar != null)
                            {
                                // Canvas has external busbar but no other equipment - still render connection lines
                                RenderConnectionLines(canvas);
                            }
                            else
                            {
                                // Render empty state
                                RenderLocationCanvasEmptyState(canvas);
                            }
                        }
                    }
                }
            }
            
            // Also render connection lines for the layout canvas (DiagramCanvas)
            RenderConnectionLines(DiagramCanvas);
            
            // Re-render connection dots after all canvases are refreshed
            if (_isConnectionEditMode)
            {
                RenderConnectionDots();
            }
            
            // If a selected canvas just became empty, fit to center on empty state
            if (anyCanvasBecameEmpty && selectedEmptyTab != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    FitCanvasToBlocks();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }
    }
}
