using System;
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
    /// Selection management for MainWindow
    /// Updated: Added grid snapping to dragging (matches 20px grid)
    /// </summary>
    public partial class MainWindow
    {
        // Grid size constant - matches connection line grid (defined in MainWindow.Helpers.Drag.cs but repeated here for clarity)
        // private const double BlockGridSize = 20.0; // Already defined in MainWindow.Helpers.Drag.cs

        /// <summary>
        ///     Deselects all selected items (locations and supplies)
        /// </summary>
        private void DeselectAll()
        {
            if (_selectedLocation != null)
            {
                _selectedLocation = null;
                _selectedLocationBorder = null;
                RemoveSelectionRectangle();
            }
            
            if (_selectedSupply != null)
            {
                _selectedSupply = null;
                _selectedSupplyEllipse = null;
                RemoveSupplySelectionCircle();
            }
            
            if (_selectedAlternator != null)
            {
                _selectedAlternator = null;
                _selectedAlternatorPolygon = null;
                RemoveAlternatorSelectionDiamond();
            }
            
            if (_selectedBusbar != null)
            {
                _selectedBusbar = null;
                _selectedBusbarContainer = null;
                RemoveBusbarSelectionRectangle();
            }
            
            if (_selectedTransformerUPS != null)
            {
                _selectedTransformerUPS = null;
                _selectedTransformerUPSGrid = null;
                RemoveTransformerUPSSelectionRectangle();
            }
            
            if (_selectedConductor != null)
            {
                _selectedConductor = null;
                _selectedConductorBorder = null;
                RemoveConductorSelectionRectangle();
            }
            
            if (_selectedRow != null)
            {
                _selectedRow = null;
                _selectedRowGrid = null;
                RemoveRowSelectionRectangle();
            }
            
            if (_selectedLoad != null)
            {
                _selectedLoad = null;
                _selectedLoadGrid = null;
                RemoveLoadSelectionRectangle();
            }

            if (_selectedExternalBusbar != null)
            {
                _selectedExternalBusbar = null;
                _selectedExternalBusbarContainer = null;
                RemoveExternalBusbarSelectionRectangle();
            }

            // Clear tree view selection
            DeselectAllTreeViewItems(TreeViewItems);
        }

        /// <summary>
        ///     Selects a location and shows selection rectangle
        /// </summary>
        /// <param name="location">The location to select</param>
        /// <param name="border">The border element of the location</param>
        private void SelectLocation(Location location, Border? border = null)
        {
            // If already selected, do nothing
            if (_selectedLocation == location) return;

            // Deselect everything first
            DeselectAll();

            // Select new location
            _selectedLocation = location;
            _selectedLocationBorder = border;

            // Find border if not provided
            if (_selectedLocationBorder == null)
            {
                foreach (var child in DiagramCanvas.Children)
                {
                    if (child is Border b && b.Tag is Location loc && loc == location)
                    {
                        _selectedLocationBorder = b;
                        break;
                    }
                }
            }

            // Show selection rectangle
            if (_selectedLocationBorder != null)
            {
                ShowSelectionRectangle(_selectedLocationBorder);
            }

            // Highlight in tree view
            HighlightLocationInTreeView(location);
        }

        /// <summary>
        ///     Shows a dashed magenta selection rectangle around the location
        /// </summary>
        private void ShowSelectionRectangle(Border locationBorder)
        {
            const double selectionOffset = 15; // Distance from location border

            double left = Canvas.GetLeft(locationBorder) - selectionOffset;
            double top = Canvas.GetTop(locationBorder) - selectionOffset;
            double width = locationBorder.Width + (selectionOffset * 2);
            double height = locationBorder.Height + (selectionOffset * 2);

            // Create dashed magenta rectangle
            var rectangle = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = new SolidColorBrush(Colors.Magenta),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 3 },
                IsHitTestVisible = false
            };

            Canvas.SetLeft(rectangle, left);
            Canvas.SetTop(rectangle, top);
            
            DiagramCanvas.Children.Add(rectangle);
            _selectionRectangle = new Border { Tag = rectangle }; // Store rectangle reference
        }

        /// <summary>
        ///     Removes the selection rectangle
        /// </summary>
        private void RemoveSelectionRectangle()
        {
            if (_selectionRectangle?.Tag is Rectangle rectangle)
            {
                DiagramCanvas.Children.Remove(rectangle);
            }
            _selectionRectangle = null;
        }

        /// <summary>
        ///     Handles mouse click on a Location border
        /// </summary>
        private void LocationBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Location location)
            {
                // Prevent panning when clicking on location
                e.Handled = true;
                
                // Check for double-click
                var currentTime = DateTime.Now;
                var timeSinceLastClick = (currentTime - _lastLocationClickTime).TotalMilliseconds;
                
                if (_lastClickedLocation == location && timeSinceLastClick < DoubleClickThresholdMs)
                {
                    // This is a double-click - create a new canvas tab
                    CreateCanvasTabForLocation(location);
                    
                    // Reset double-click tracking
                    _lastLocationClickTime = DateTime.MinValue;
                    _lastClickedLocation = null;
                    return;
                }
                
                // Update double-click tracking
                _lastLocationClickTime = currentTime;
                _lastClickedLocation = location;
                
                // If this location is already selected, start dragging
                if (_selectedLocation == location)
                {
                    _isDraggingLocation = true;
                    // Store the current element position, not the mouse position
                    _locationDragStartPoint = new Point(Canvas.GetLeft(border), Canvas.GetTop(border));
                    _locationDragMouseOffset = new Point(
                        e.GetPosition(border).X,
                        e.GetPosition(border).Y
                    );
                    border.CaptureMouse();
                    border.Cursor = Cursors.SizeAll;
                }
                else
                {
                    // Just select the location, don't start dragging
                    SelectLocation(location, border);
                }
            }
        }

        /// <summary>
        ///     Handles mouse button up on a Location border
        /// </summary>
        private void LocationBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && _isDraggingLocation)
            {
                // IMPORTANT: Handle event FIRST to prevent CanvasContainer handler from processing it
                e.Handled = true;
                
                _isDraggingLocation = false;
                border.ReleaseMouseCapture();
                border.Cursor = Cursors.Hand;
                
                // Update the location's render position
                if (border.Tag is Location location)
                {
                    UpdateLocationRenderPosition(location, border);
                }
            }
        }

        /// <summary>
        ///     Handles mouse move on a Location border for dragging
        /// </summary>
        private void LocationBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingLocation && sender is Border border && border == _selectedLocationBorder)
            {
                // Use the unified drag handler which includes grid snapping
                HandleLocationDrag(e);
                e.Handled = true;
            }
        }

        /// <summary>
        ///     Selects a supply and shows selection circle
        /// </summary>
        /// <param name="supply">The supply to select</param>
        /// <param name="ellipse">The ellipse element of the supply</param>
        private void SelectSupply(Supply supply, Ellipse? ellipse = null)
        {
            // If already selected, do nothing
            if (_selectedSupply == supply) return;

            // Deselect everything first
            DeselectAll();

            // Select new supply
            _selectedSupply = supply;
            _selectedSupplyEllipse = ellipse;

            // Find ellipse if not provided
            if (_selectedSupplyEllipse == null)
            {
                foreach (var child in DiagramCanvas.Children)
                {
                    if (child is Ellipse e && e.Tag is Supply sup && sup == supply)
                    {
                        _selectedSupplyEllipse = e;
                        break;
                    }
                }
            }

            // Show selection circle
            if (_selectedSupplyEllipse != null)
            {
                ShowSupplySelectionCircle(_selectedSupplyEllipse);
            }

            // Highlight in tree view
            HighlightSupplyInTreeView(supply);
        }

        /// <summary>
        ///     Shows a dashed magenta selection circle around the supply
        /// </summary>
        private void ShowSupplySelectionCircle(Ellipse supplyEllipse)
        {
            const double selectionOffset = 15; // Distance from supply border

            double left = Canvas.GetLeft(supplyEllipse) - selectionOffset;
            double top = Canvas.GetTop(supplyEllipse) - selectionOffset;
            double diameter = supplyEllipse.Width + (selectionOffset * 2);

            // Create dashed magenta circle
            var selectionEllipse = new Ellipse
            {
                Width = diameter,
                Height = diameter,
                Stroke = new SolidColorBrush(Colors.Magenta),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 3 },
                IsHitTestVisible = false
            };

            Canvas.SetLeft(selectionEllipse, left);
            Canvas.SetTop(selectionEllipse, top);
            
            DiagramCanvas.Children.Add(selectionEllipse);
            _supplySelectionRectangle = new Border { Tag = selectionEllipse }; // Store ellipse reference
        }

        /// <summary>
        ///     Removes the supply selection circle
        /// </summary>
        private void RemoveSupplySelectionCircle()
        {
            if (_supplySelectionRectangle?.Tag is Ellipse ellipse)
            {
                DiagramCanvas.Children.Remove(ellipse);
            }
            _supplySelectionRectangle = null;
        }

        /// <summary>
        ///     Handles mouse click on a Supply ellipse
        /// </summary>
        private void SupplyEllipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse ellipse && ellipse.Tag is Supply supply)
            {
                // Prevent panning when clicking on supply
                e.Handled = true;
                
                // If this supply is already selected, start dragging
                if (_selectedSupply == supply)
                {
                    _isDraggingSupply = true;
                    // Store the current element position, not the mouse position
                    _supplyDragStartPoint = new Point(Canvas.GetLeft(ellipse), Canvas.GetTop(ellipse));
                    _supplyDragMouseOffset = new Point(
                        e.GetPosition(ellipse).X,
                        e.GetPosition(ellipse).Y
                    );
                    ellipse.CaptureMouse();
                    ellipse.Cursor = Cursors.SizeAll;
                }
                else
                {
                    // Just select the supply, don't start dragging
                    SelectSupply(supply, ellipse);
                }
            }
        }

        /// <summary>
        ///     Handles mouse button up on a Supply ellipse
        /// </summary>
        private void SupplyEllipse_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse ellipse && _isDraggingSupply)
            {
                // IMPORTANT: Handle event FIRST to prevent CanvasContainer handler from processing it
                e.Handled = true;
                
                _isDraggingSupply = false;
                ellipse.ReleaseMouseCapture();
                ellipse.Cursor = Cursors.Hand;
                
                // Update the supply's render position
                if (ellipse.Tag is Supply supply)
                {
                    UpdateSupplyRenderPosition(supply, ellipse);
                }
            }
        }

        /// <summary>
        ///     Handles mouse move on a Supply ellipse for dragging
        /// </summary>
        private void SupplyEllipse_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingSupply && sender is Ellipse ellipse && ellipse == _selectedSupplyEllipse)
            {
                // Use the unified drag handler which includes grid snapping
                HandleSupplyDrag(e);
                e.Handled = true;
            }
        }

        /// <summary>
        ///     Selects an alternator and shows selection diamond
        /// </summary>
        /// <param name="alternator">The alternator to select</param>
        /// <param name="polygon">The polygon element of the alternator</param>
        private void SelectAlternator(AlternatorBlock alternator, Polygon? polygon = null)
        {
            // If already selected, do nothing
            if (_selectedAlternator == alternator) return;

            // Deselect everything first
            DeselectAll();

            // Select new alternator
            _selectedAlternator = alternator;
            _selectedAlternatorPolygon = polygon;

            // Find polygon if not provided
            if (_selectedAlternatorPolygon == null)
            {
                foreach (var child in DiagramCanvas.Children)
                {
                    if (child is Polygon p && p.Tag is AlternatorBlock alt && alt == alternator)
                    {
                        _selectedAlternatorPolygon = p;
                        break;
                    }
                }
            }

            // Show selection diamond
            if (_selectedAlternatorPolygon != null)
            {
                ShowAlternatorSelectionDiamond(_selectedAlternatorPolygon);
            }

            // Highlight in tree view
            HighlightAlternatorInTreeView(alternator);
        }

        /// <summary>
        ///     Shows a dashed magenta selection diamond around the alternator
        /// </summary>
        private void ShowAlternatorSelectionDiamond(Polygon alternatorPolygon)
        {
            const double selectionOffset = 15; // Distance from alternator border
            const double diamondSize = 150;

            double left = Canvas.GetLeft(alternatorPolygon) - selectionOffset;
            double top = Canvas.GetTop(alternatorPolygon) - selectionOffset;
            double size = diamondSize + (selectionOffset * 2);

            // Create dashed magenta diamond
            var selectionPolygon = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(size / 2, 0),         // Top
                    new Point(size, size / 2),      // Right
                    new Point(size / 2, size),      // Bottom
                    new Point(0, size / 2)          // Left
                },
                Stroke = new SolidColorBrush(Colors.Magenta),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 3 },
                IsHitTestVisible = false
            };

            Canvas.SetLeft(selectionPolygon, left);
            Canvas.SetTop(selectionPolygon, top);
            
            DiagramCanvas.Children.Add(selectionPolygon);
            _alternatorSelectionRectangle = new Border { Tag = selectionPolygon }; // Store polygon reference
        }

        /// <summary>
        ///     Removes the alternator selection diamond
        /// </summary>
        private void RemoveAlternatorSelectionDiamond()
        {
            if (_alternatorSelectionRectangle?.Tag is Polygon polygon)
            {
                DiagramCanvas.Children.Remove(polygon);
            }
            _alternatorSelectionRectangle = null;
        }

        /// <summary>
        ///     Selects a conductor and shows selection rectangle
        /// </summary>
        /// <param name="conductor">The conductor to select</param>
        /// <param name="border">The border element of the conductor</param>
        private void SelectConductor(ConductorBlock conductor, Border? border = null)
        {
            // If already selected, do nothing
            if (_selectedConductor == conductor) return;

            // Deselect everything first
            DeselectAll();

            // Select new conductor
            _selectedConductor = conductor;
            _selectedConductorBorder = border;

            // Find border if not provided
            if (_selectedConductorBorder == null)
            {
                foreach (var child in DiagramCanvas.Children)
                {
                    if (child is Border b && b.Tag is ConductorBlock cond && cond == conductor)
                    {
                        _selectedConductorBorder = b;
                        break;
                    }
                }
            }

            // Show selection rectangle
            if (_selectedConductorBorder != null)
            {
                ShowConductorSelectionRectangle(_selectedConductorBorder);
            }

            // Highlight in tree view
            HighlightConductorInTreeView(conductor);
        }

        /// <summary>
        ///     Shows a dashed magenta selection rectangle around the conductor
        /// </summary>
        private void ShowConductorSelectionRectangle(Border conductorBorder)
        {
            const double selectionOffset = 15; // Distance from conductor border

            double left = Canvas.GetLeft(conductorBorder) - selectionOffset;
            double top = Canvas.GetTop(conductorBorder) - selectionOffset;
            double width = conductorBorder.Width + (selectionOffset * 2);
            double height = conductorBorder.Height + (selectionOffset * 2);

            // Create dashed magenta rectangle
            var rectangle = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = new SolidColorBrush(Colors.Magenta),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 3 },
                IsHitTestVisible = false
            };

            Canvas.SetLeft(rectangle, left);
            Canvas.SetTop(rectangle, top);
            
            DiagramCanvas.Children.Add(rectangle);
            _conductorSelectionRectangle = rectangle; // Store rectangle reference
        }

        /// <summary>
        ///     Removes the conductor selection rectangle
        /// </summary>
        private void RemoveConductorSelectionRectangle()
        {
            if (_conductorSelectionRectangle != null)
            {
                DiagramCanvas.Children.Remove(_conductorSelectionRectangle);
                _conductorSelectionRectangle = null;
            }
        }

        /// <summary>
        ///     Selects a row and shows selection rectangle
        /// </summary>
        /// <param name="row">The row to select</param>
        /// <param name="rowGrid">The grid element of the row</param>
        private void SelectRow(Row row, Grid? rowGrid = null)
        {
            // If already selected, do nothing
            if (_selectedRow == row) return;

            // Deselect everything first
            DeselectAll();

            // Select new row
            _selectedRow = row;
            _selectedRowGrid = rowGrid;

            // Find the row grid if not provided
            if (_selectedRowGrid == null)
            {
                // Get the canvas tabs to find the location canvas
                var canvasTabs = FindName("CanvasTabs") as TabControl;
                if (canvasTabs != null)
                {
                    foreach (TabItem tab in canvasTabs.Items)
                    {
                        if (tab.Content is Grid canvasContainer)
                        {
                            var locationCanvas = canvasContainer.Children.OfType<Canvas>().FirstOrDefault();
                            if (locationCanvas != null)
                            {
                                // Find the row grid within busbar containers
                                foreach (var child in locationCanvas.Children)
                                {
                                    if (child is StackPanel sp && sp.Tag is Busbar busbar)
                                    {
                                        // Get the busbar grid from the stack panel
                                        if (sp.Children[0] is Border busbarBorder && busbarBorder.Child is Grid busbarGrid)
                                        {
                                            // Search through row grids
                                            foreach (var gridChild in busbarGrid.Children)
                                            {
                                                if (gridChild is Grid rg)
                                                {
                                                    // Check if this grid belongs to our row by checking position
                                                    var rows = busbar.GetRows().ToArray();
                                                    for (int i = 0; i < rows.Length; i++)
                                                    {
                                                        if (rows[i] == row)
                                                        {
                                                            // Check if this is the right row by grid position
                                                            int rowIndex = Grid.GetRow(rg);
                                                            if (rowIndex == i + 1) // +1 because row 0 is the busbar name
                                                            {
                                                                _selectedRowGrid = rg;
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                                if (_selectedRowGrid != null) break;
                                            }
                                        }
                                        if (_selectedRowGrid != null) break;
                                    }
                                }
                            }
                        }
                        if (_selectedRowGrid != null) break;
                    }
                }
            }

            // Show selection rectangle
            if (_selectedRowGrid != null)
            {
                ShowRowSelectionRectangle(_selectedRowGrid);
            }

            // Highlight in tree view
            HighlightRowInTreeView(row);
        }

        /// <summary>
        ///     Shows a dashed magenta selection rectangle around the row
        /// </summary>
        private void ShowRowSelectionRectangle(Grid rowGrid)
        {
            const double selectionOffset = 5; // Distance from row border

            // Find the parent canvas for the row
            var canvas = FindParentCanvas(rowGrid);
            if (canvas == null) return;

            // Get the position of the row grid relative to the canvas
            var transform = rowGrid.TransformToAncestor(canvas);
            var topLeft = transform.Transform(new Point(0, 0));

            double left = topLeft.X - selectionOffset;
            double top = topLeft.Y - selectionOffset;
            double width = rowGrid.ActualWidth + (selectionOffset * 2);
            double height = rowGrid.ActualHeight + (selectionOffset * 2);

            // Create dashed magenta rectangle
            var rectangle = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = new SolidColorBrush(Colors.Magenta),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 3 },
                IsHitTestVisible = false
            };

            Canvas.SetLeft(rectangle, left);
            Canvas.SetTop(rectangle, top);
            
            canvas.Children.Add(rectangle);
            _rowSelectionRectangle = rectangle; // Store rectangle reference
        }

        /// <summary>
        ///     Removes the row selection rectangle
        /// </summary>
        private void RemoveRowSelectionRectangle()
        {
            if (_rowSelectionRectangle != null)
            {
                var canvas = FindParentCanvas(_rowSelectionRectangle);
                if (canvas != null)
                {
                    canvas.Children.Remove(_rowSelectionRectangle);
                }
                _rowSelectionRectangle = null;
            }
        }

        /// <summary>
        ///     Selects a TransformerUPS and shows selection rectangle
        /// </summary>
        private void SelectTransformerUPS(TransformerUPSBlock transformerUPS, Grid? grid)
        {
            // If already selected, do nothing
            if (_selectedTransformerUPS == transformerUPS) return;
            
            // Deselect other items
            DeselectAll();
            
            // Select new transformer
            _selectedTransformerUPS = transformerUPS;
            _selectedTransformerUPSGrid = grid;
            
            // Find grid if not provided
            if (_selectedTransformerUPSGrid == null)
            {
                // Get the canvas tabs to find the location canvas
                var canvasTabs = FindName("CanvasTabs") as TabControl;
                if (canvasTabs != null)
                {
                    foreach (TabItem tab in canvasTabs.Items)
                    {
                        if (tab.Content is Grid canvasContainer)
                        {
                            var locationCanvas = canvasContainer.Children.OfType<Canvas>().FirstOrDefault();
                            if (locationCanvas != null)
                            {
                                foreach (var child in locationCanvas.Children)
                                {
                                    if (child is Grid g && g.Tag is TransformerUPSBlock tfmr && tfmr == transformerUPS)
                                    {
                                        _selectedTransformerUPSGrid = g;
                                        break;
                                    }
                                }
                            }
                        }
                        if (_selectedTransformerUPSGrid != null) break;
                    }
                }
            }
            
            // Show selection rectangle
            if (_selectedTransformerUPSGrid != null)
            {
                ShowTransformerUPSSelectionRectangle(_selectedTransformerUPSGrid);
            }
            
            // Highlight in tree view
            HighlightTransformerUPSInTreeView(transformerUPS);
        }
        
        /// <summary>
        ///     Shows a dashed magenta selection rectangle around the TransformerUPS
        /// </summary>
        private void ShowTransformerUPSSelectionRectangle(Grid grid)
        {
            const double selectionOffset = 15; // Distance from transformer border
            
            var canvas = FindParentCanvas(grid);
            if (canvas == null) return;
            
            double left = Canvas.GetLeft(grid) - selectionOffset;
            double top = Canvas.GetTop(grid) - selectionOffset;
            double width = grid.Width + (selectionOffset * 2);
            double height = grid.Height + (selectionOffset * 2);
            
            // Create dashed magenta rectangle
            var rectangle = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = new SolidColorBrush(Colors.Magenta),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 3 },
                IsHitTestVisible = false
            };
            
            Canvas.SetLeft(rectangle, left);
            Canvas.SetTop(rectangle, top);
            
            canvas.Children.Add(rectangle);
            _transformerUPSSelectionRectangle = rectangle; // Store rectangle reference
        }
        
        /// <summary>
        ///     Removes the TransformerUPS selection rectangle
        /// </summary>
        private void RemoveTransformerUPSSelectionRectangle()
        {
            if (_transformerUPSSelectionRectangle != null)
            {
                var canvas = FindParentCanvas(_transformerUPSSelectionRectangle);
                if (canvas != null)
                {
                    canvas.Children.Remove(_transformerUPSSelectionRectangle);
                }
                _transformerUPSSelectionRectangle = null;
            }
        }

        /// <summary>
        ///     Selects a busbar and shows selection rectangle
        /// </summary>
        private void SelectBusbar(Busbar busbar, StackPanel? container)
        {
            // If already selected, do nothing
            if (_selectedBusbar == busbar) return;
            
            // Deselect other items
            DeselectAll();
            
            // Select new busbar
            _selectedBusbar = busbar;
            _selectedBusbarContainer = container;
            
            // Find container if not provided
            if (_selectedBusbarContainer == null)
            {
                // Get the canvas tabs to find the location canvas
                var canvasTabs = FindName("CanvasTabs") as TabControl;
                if (canvasTabs != null)
                {
                    foreach (TabItem tab in canvasTabs.Items)
                    {
                        if (tab.Content is Grid canvasContainer)
                        {
                            var locationCanvas = canvasContainer.Children.OfType<Canvas>().FirstOrDefault();
                            if (locationCanvas != null)
                            {
                                foreach (var child in locationCanvas.Children)
                                {
                                    if (child is StackPanel sp && sp.Tag is Busbar bb && bb == busbar)
                                    {
                                        _selectedBusbarContainer = sp;
                                        break;
                                    }
                                }
                            }
                        }
                        if (_selectedBusbarContainer != null) break;
                    }
                }
            }
            
            // Show selection rectangle
            if (_selectedBusbarContainer != null)
            {
                ShowBusbarSelectionRectangle(_selectedBusbarContainer);
            }
            
            // Highlight in tree view
            HighlightBusbarInTreeView(busbar);
        }
        
        /// <summary>
        ///     Shows a dashed magenta selection rectangle around the busbar
        /// </summary>
        private void ShowBusbarSelectionRectangle(StackPanel containerStack)
        {
            const double selectionOffset = 15; // Distance from busbar border
            
            var canvas = FindParentCanvas(containerStack);
            if (canvas == null) return;
            
            double left = Canvas.GetLeft(containerStack) - selectionOffset;
            double top = Canvas.GetTop(containerStack) - selectionOffset;
            double width = containerStack.Width + (selectionOffset * 2);
            double height = containerStack.ActualHeight + (selectionOffset * 2);
            
            // Create dashed magenta rectangle
            var rectangle = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = new SolidColorBrush(Colors.Magenta),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 3 },
                IsHitTestVisible = false
            };
            
            Canvas.SetLeft(rectangle, left);
            Canvas.SetTop(rectangle, top);
            
            canvas.Children.Add(rectangle);
            _busbarSelectionRectangle = rectangle; // Store rectangle reference
        }
        
        /// <summary>
        ///     Removes the busbar selection rectangle
        /// </summary>
        private void RemoveBusbarSelectionRectangle()
        {
            if (_busbarSelectionRectangle != null)
            {
                var canvas = FindParentCanvas(_busbarSelectionRectangle);
                if (canvas != null)
                {
                    canvas.Children.Remove(_busbarSelectionRectangle);
                }
                _busbarSelectionRectangle = null;
            }
        }

        /// <summary>
        ///     Selects a Load and shows selection rectangle
        /// </summary>
        private void SelectLoad(Load load, Grid? grid)
        {
            // If already selected, do nothing
            if (_selectedLoad == load) return;
            
            // Deselect other items
            DeselectAll();
            
            // Select new load
            _selectedLoad = load;
            _selectedLoadGrid = grid;
            
            // Find grid if not provided
            if (_selectedLoadGrid == null)
            {
                // Get the canvas tabs to find the location canvas
                var canvasTabs = FindName("CanvasTabs") as TabControl;
                if (canvasTabs != null)
                {
                    foreach (TabItem tab in canvasTabs.Items)
                    {
                        if (tab.Content is Grid canvasContainer)
                        {
                            var locationCanvas = canvasContainer.Children.OfType<Canvas>().FirstOrDefault();
                            if (locationCanvas != null)
                            {
                                foreach (var child in locationCanvas.Children)
                                {
                                    if (child is Grid g && g.Tag is Load l && l == load)
                                    {
                                        _selectedLoadGrid = g;
                                        break;
                                    }
                                }
                            }
                        }
                        if (_selectedLoadGrid != null) break;
                    }
                }
            }
            
            // Show selection rectangle
            if (_selectedLoadGrid != null)
            {
                ShowLoadSelectionRectangle(_selectedLoadGrid);
            }
            
            // Highlight in tree view
            HighlightLoadInTreeView(load);
        }
        
        /// <summary>
        ///     Shows a dashed magenta selection rectangle around the Load
        /// </summary>
        private void ShowLoadSelectionRectangle(Grid grid)
        {
            const double selectionOffset = 15; // Distance from load border
            
            var canvas = FindParentCanvas(grid);
            if (canvas == null) return;
            
            double left = Canvas.GetLeft(grid) - selectionOffset;
            double top = Canvas.GetTop(grid) - selectionOffset;
            double width = grid.Width + (selectionOffset * 2);
            double height = grid.Height + (selectionOffset * 2);
            
            // Create dashed magenta rectangle
            var rectangle = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = new SolidColorBrush(Colors.Magenta),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 3 },
                IsHitTestVisible = false
            };
            
            Canvas.SetLeft(rectangle, left);
            Canvas.SetTop(rectangle, top);
            
            canvas.Children.Add(rectangle);
            _loadSelectionRectangle = rectangle; // Store rectangle reference
        }
        
        /// <summary>
        ///     Removes the Load selection rectangle
        /// </summary>
        private void RemoveLoadSelectionRectangle()
        {
            if (_loadSelectionRectangle != null)
            {
                var canvas = FindParentCanvas(_loadSelectionRectangle);
                if (canvas != null)
                {
                    canvas.Children.Remove(_loadSelectionRectangle);
                }
                _loadSelectionRectangle = null;
            }
        }

        /// <summary>
        ///     Selects an ExternalBusbar and shows selection rectangle
        /// </summary>
        private void SelectExternalBusbar(ExternalBusbar externalBusbar, Canvas? containerCanvas)
        {
            // If already selected, do nothing
            if (_selectedExternalBusbar == externalBusbar) return;
            
            // Deselect other items
            DeselectAll();
            
            // Select new external busbar
            _selectedExternalBusbar = externalBusbar;
            _selectedExternalBusbarContainer = containerCanvas;
            
            // Find container if not provided
            if (_selectedExternalBusbarContainer == null)
            {
                // Get the canvas tabs to find the location canvas
                var canvasTabs = FindName("CanvasTabs") as TabControl;
                if (canvasTabs != null)
                {
                    foreach (TabItem tab in canvasTabs.Items)
                    {
                        if (tab.Content is Grid canvasContainer)
                        {
                            var locationCanvas = canvasContainer.Children.OfType<Canvas>().FirstOrDefault();
                            if (locationCanvas != null)
                            {
                                foreach (var child in locationCanvas.Children)
                                {
                                    if (child is Canvas c && c.Tag is ExternalBusbar eb && eb == externalBusbar)
                                    {
                                        _selectedExternalBusbarContainer = c;
                                        break;
                                    }
                                }
                            }
                        }
                        if (_selectedExternalBusbarContainer != null) break;
                    }
                }
            }
            
            // Show selection rectangle
            if (_selectedExternalBusbarContainer != null)
            {
                ShowExternalBusbarSelectionRectangle(_selectedExternalBusbarContainer);
            }
            
            // Highlight in tree view
            HighlightExternalBusbarInTreeView(externalBusbar);
            
            // Display properties
            UpdatePropertiesView(externalBusbar);
        }
        
        /// <summary>
        ///     Shows a dashed magenta selection rectangle around the ExternalBusbar
        /// </summary>
        private void ShowExternalBusbarSelectionRectangle(Canvas containerCanvas)
        {
            const double selectionOffset = 15; // Distance from external busbar border
            
            var canvas = FindParentCanvas(containerCanvas);
            if (canvas == null) return;
            
            double left = Canvas.GetLeft(containerCanvas) - selectionOffset;
            double top = Canvas.GetTop(containerCanvas) - selectionOffset;
            double width = containerCanvas.Width + (selectionOffset * 2);
            double height = containerCanvas.Height + (selectionOffset * 2);
            
            // Create dashed magenta rectangle
            var rectangle = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = new SolidColorBrush(Colors.Magenta),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 5, 3 },
                IsHitTestVisible = false
            };
            
            Canvas.SetLeft(rectangle, left);
            Canvas.SetTop(rectangle, top);
            
            canvas.Children.Add(rectangle);
            _externalBusbarSelectionRectangle = rectangle; // Store rectangle reference
        }
        
        /// <summary>
        ///     Removes the ExternalBusbar selection rectangle
        /// </summary>
        private void RemoveExternalBusbarSelectionRectangle()
        {
            if (_externalBusbarSelectionRectangle != null)
            {
                var canvas = FindParentCanvas(_externalBusbarSelectionRectangle);
                if (canvas != null)
                {
                    canvas.Children.Remove(_externalBusbarSelectionRectangle);
                }
                _externalBusbarSelectionRectangle = null;
            }
        }

        /// <summary>
        ///     Highlights an ExternalBusbar in the tree view
        /// </summary>
        private void HighlightExternalBusbarInTreeView(ExternalBusbar externalBusbar)
        {
            // TODO: Implement tree view highlighting for external busbar
            // This would highlight the external busbar node in the tree view
            // For now, this is a placeholder to prevent compilation errors
        }
    }
}
