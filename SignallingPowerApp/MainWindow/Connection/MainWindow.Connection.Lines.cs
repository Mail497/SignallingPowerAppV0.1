using System;
using System.Collections.Generic;
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
    /// Connection line and path rendering
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        ///     Renders connection lines between connected blocks
        /// </summary>
        /// <param name="specificCanvas">Optional: If provided, only render lines for this specific canvas. Otherwise render for currently active canvas.</param>
        private void RenderConnectionLines(Canvas? specificCanvas = null)
        {
            if (_currentProject == null) return;

            Canvas? currentCanvas = specificCanvas;
            
            // If no specific canvas provided, determine which canvas we're currently on
            if (currentCanvas == null)
            {
                var canvasTabs = FindName("CanvasTabs") as TabControl;
                if (canvasTabs?.SelectedItem is not TabItem selectedTab) return;

                bool isLayoutTab = selectedTab.Header?.ToString() == "Layout";
                
                if (isLayoutTab)
                {
                    currentCanvas = DiagramCanvas;
                }
                else if (selectedTab.Tag is Location location)
                {
                    currentCanvas = GetLocationCanvas(location);
                }
            }

            if (currentCanvas == null) return;

            // Clear existing connection lines ONLY from the current canvas
            ClearConnectionLines(currentCanvas);

            const double dotSize = 12;
            const double dotOffset = 6; // Half of dotSize for centering

            // Get all connections
            var connections = _currentProject.GetAllConnections();

            foreach (var connection in connections)
            {
                // Determine which canvas this connection should be rendered on
                var targetCanvas = GetCanvasForConnection(connection);
                if (targetCanvas == null) continue;

                // Only render connections that belong to the current canvas
                if (targetCanvas != currentCanvas) continue;

                // Get the position for the left endpoint (pass targetCanvas to ensure correct positioning)
                var leftPos = GetConnectionDotPosition(connection.LeftID, dotSize, dotOffset, targetCanvas);
                if (leftPos == null) continue;

                // Get the position for the right endpoint (pass targetCanvas to ensure correct positioning)
                var rightPos = GetConnectionDotPosition(connection.RightID, dotSize, dotOffset, targetCanvas);
                if (rightPos == null) continue;

                // Build point collection for the polyline with orthogonal segments
                var pointCollection = BuildOrthogonalPath(leftPos.Value, rightPos.Value, connection, dotOffset, targetCanvas);

                // Create polyline
                var polyline = new Polyline
                {
                    Points = pointCollection,
                    Stroke = new SolidColorBrush(_isRemoveConnectionMode ? Colors.DarkRed : Colors.Red),
                    StrokeThickness = (_isConnectionEditMode || _isRemoveConnectionMode) ? 6 : 2, // Thicker in edit/remove mode for easier clicking
                    Tag = connection, // Store connection reference
                    Cursor = _isConnectionEditMode ? Cursors.Hand : (_isRemoveConnectionMode ? Cursors.No : Cursors.Arrow),
                    IsHitTestVisible = true
                };

                // Add click handler based on mode
                if (_isConnectionEditMode)
                {
                    polyline.MouseLeftButtonDown += ConnectionLine_MouseLeftButtonDown;
                }
                else if (_isRemoveConnectionMode)
                {
                    polyline.MouseLeftButtonDown += ConnectionLine_RemoveClick;
                }

                Canvas.SetZIndex(polyline, 1); // Place lines below blocks but above background
                _connectionLines.Add(polyline);
                targetCanvas.Children.Add(polyline);

                // Render render point dots in edit mode or remove mode
                if (_isConnectionEditMode || _isRemoveConnectionMode)
                {
                    foreach (var renderPoint in connection.RenderPoints)
                    {
                        // Get coordinate system for target canvas
                        var coordSystem = GetCoordinateSystem(targetCanvas);
                        Point canvasPoint = coordSystem.ToCanvasPoint(renderPoint.Item1, renderPoint.Item2);
                        
                        var renderPointDot = CreateRenderPointDot(canvasPoint.X - dotOffset, canvasPoint.Y - dotOffset, 
                            dotSize, connection, renderPoint.Item1, renderPoint.Item2);
                        _connectionLines.Add(renderPointDot);
                        targetCanvas.Children.Add(renderPointDot);
                    }
                }
            }
        }

        /// <summary>
        ///     Determines which canvas a connection should be rendered on
        /// </summary>
        /// <param name="connection">The connection to check</param>
        /// <returns>The canvas to render on, or null if not found</returns>
        private Canvas? GetCanvasForConnection(Connection connection)
        {
            if (_currentProject == null) return null;

            // Get the blocks involved in the connection
            var leftBlock = _currentProject.GetBlock(connection.LeftID);
            var rightBlock = _currentProject.GetBlock(connection.RightID);

            // Check if either block is a Load - they're rendered on location canvases
            if (leftBlock is Load leftLoad)
            {
                var leftLocation = _currentProject.GetBlock(leftLoad.ParentID) as Location;
                if (leftLocation != null)
                {
                    return GetLocationCanvas(leftLocation);
                }
            }
            
            if (rightBlock is Load rightLoad)
            {
                var rightLocation = _currentProject.GetBlock(rightLoad.ParentID) as Location;
                if (rightLocation != null)
                {
                    return GetLocationCanvas(rightLocation);
                }
            }

            // Check if both blocks are terminals
            if (leftBlock is Terminal leftTerminal && rightBlock is Terminal rightTerminal)
            {
                // Get the parents of the terminals
                var leftParent = _currentProject.GetBlock(leftTerminal.ParentID);
                var rightParent = _currentProject.GetBlock(rightTerminal.ParentID);

                // Check if either terminal belongs to a Location (external terminals)
                Location? leftLocation = null;
                Location? rightLocation = null;
                
                if (leftParent is Location leftLoc)
                {
                    leftLocation = leftLoc;
                }
                
                if (rightParent is Location rightLoc)
                {
                    rightLocation = rightLoc;
                }
                
                // Check if either parent is an ExternalBusbar
                if (leftParent is ExternalBusbar leftEB)
                {
                    // Left is ExternalBusbar terminal, get its parent location
                    leftLocation = _currentProject.GetBlock(leftEB.ParentID) as Location;
                }
                
                if (rightParent is ExternalBusbar rightEB)
                {
                    // Right is ExternalBusbar terminal, get its parent location
                    rightLocation = _currentProject.GetBlock(rightEB.ParentID) as Location;
                }
                
                // Check if both terminals belong to the same location (one is an external terminal, other is internal)
                if (leftLocation != null && rightLocation != null && leftLocation == rightLocation)
                {
                    // Both are terminals of the same location - render on that location's canvas
                    return GetLocationCanvas(leftLocation);
                }
                else if (leftLocation != null && rightParent != null)
                {
                    // Left is a location external terminal, check if right is inside that location
                    var rightParentLocation = GetParentLocation(rightParent);
                    if (rightParentLocation == leftLocation)
                    {
                        // Connection is from location external terminal to something inside the location
                        return GetLocationCanvas(leftLocation);
                    }
                }
                else if (rightLocation != null && leftParent != null)
                {
                    // Right is a location external terminal, check if left is inside that location
                    var leftParentLocation = GetParentLocation(leftParent);
                    if (leftParentLocation == rightLocation)
                    {
                        // Connection is from location external terminal to something inside the location
                        return GetLocationCanvas(rightLocation);
                    }
                }

                // Check if both parents are inside a location (Row or TransformerUPS)
                if (leftParent is Row leftRow && rightParent is Row rightRow)
                {
                    // Both are row terminals - check if they're in the same location
                    var leftBusbar = _currentProject.GetBlock(leftRow.ParentID) as Busbar;
                    var rightBusbar = _currentProject.GetBlock(rightRow.ParentID) as Busbar;
                    
                    if (leftBusbar != null && rightBusbar != null)
                    {
                        var leftRowLocation = _currentProject.GetBlock(leftBusbar.ParentID) as Location;
                        var rightRowLocation = _currentProject.GetBlock(rightBusbar.ParentID) as Location;
                        
                        // If both rows are in the same location, render on that location's canvas
                        if (leftRowLocation == rightRowLocation && leftRowLocation != null)
                        {
                            return GetLocationCanvas(leftRowLocation);
                        }
                    }
                }
                else if (leftParent is TransformerUPSBlock leftTransformer && rightParent is TransformerUPSBlock rightTransformer)
                {
                    // Both are transformer terminals
                    var leftTransformerLocation = _currentProject.GetBlock(leftTransformer.ParentID) as Location;
                    var rightTransformerLocation = _currentProject.GetBlock(rightTransformer.ParentID) as Location;
                    
                    if (leftTransformerLocation == rightTransformerLocation && leftTransformerLocation != null)
                    {
                        return GetLocationCanvas(leftTransformerLocation);
                    }
                }
                else if (leftParent is Row leftRowMixed && rightParent is TransformerUPSBlock rightTransformerMixed)
                {
                    // Mixed: Row and TransformerUPS
                    var leftBusbar = _currentProject.GetBlock(leftRowMixed.ParentID) as Busbar;
                    if (leftBusbar != null)
                    {
                        var leftMixedLocation = _currentProject.GetBlock(leftBusbar.ParentID) as Location;
                        var rightMixedLocation = _currentProject.GetBlock(rightTransformerMixed.ParentID) as Location;
                        
                        if (leftMixedLocation == rightMixedLocation && leftMixedLocation != null)
                        {
                            return GetLocationCanvas(leftMixedLocation);
                        }
                    }
                }
                else if (leftParent is TransformerUPSBlock leftTransformerMixed && rightParent is Row rightRowMixed)
                {
                    // Mixed: TransformerUPS and Row
                    var rightBusbar = _currentProject.GetBlock(rightRowMixed.ParentID) as Busbar;
                    if (rightBusbar != null)
                    {
                        var leftMixedLocation = _currentProject.GetBlock(leftTransformerMixed.ParentID) as Location;
                        var rightMixedLocation = _currentProject.GetBlock(rightBusbar.ParentID) as Location;
                        
                        if (leftMixedLocation == rightMixedLocation && leftMixedLocation != null)
                        {
                            return GetLocationCanvas(leftMixedLocation);
                        }
                    }
                }
                // Handle ExternalBusbar connections with other blocks in the same location
                else if (leftParent is ExternalBusbar leftExternalBusbar)
                {
                    var leftExtLocation = _currentProject.GetBlock(leftExternalBusbar.ParentID) as Location;
                    var rightParentLocation = GetParentLocation(rightParent);
                    
                    if (leftExtLocation == rightParentLocation && leftExtLocation != null)
                    {
                        return GetLocationCanvas(leftExtLocation);
                    }
                }
                else if (rightParent is ExternalBusbar rightExternalBusbar)
                {
                    var rightExtLocation = _currentProject.GetBlock(rightExternalBusbar.ParentID) as Location;
                    var leftParentLocation = GetParentLocation(leftParent);
                    
                    if (rightExtLocation == leftParentLocation && rightExtLocation != null)
                    {
                        return GetLocationCanvas(rightExtLocation);
                    }
                }
            }

            // Default: render on main DiagramCanvas for all other connections
            // (Location terminals, Supply, Alternator, ConductorBlock, etc.)
            return DiagramCanvas;
        }

        // GetCanvasForConnection method has been moved to MainWindow.Helpers.Canvas.cs
        // but is kept here temporarily as it has additional logic specific to connections.
        // TODO: Consider refactoring to consolidate this logic.
        
        /// <summary>
        ///     Gets the parent Location for a given block (walks up the parent chain)
        /// </summary>
        /// <param name="block">The block to find the parent location for</param>
        /// <returns>The parent Location, or null if not found</returns>
        private Location? GetParentLocation(IBlock block)
        {
            var currentBlock = block;
            
            while (currentBlock != null)
            {
                if (currentBlock is Location location)
                {
                    return location;
                }
                
                // If we've reached a root block (ParentID == -1), stop
                if (currentBlock.ParentID == -1)
                {
                    break;
                }
                
                // Move up to the parent
                try
                {
                    currentBlock = _currentProject?.GetBlock(currentBlock.ParentID);
                }
                catch
                {
                    break;
                }
            }
            
            return null;
        }
        
        /// <summary>
        ///     Gets the canvas for a specific location
        /// </summary>
        /// <param name="location">The location to find the canvas for</param>
        /// <returns>The canvas for the location, or null if not found</returns>
        private Canvas? GetLocationCanvas(Location location)
        {
            var canvasTabs = FindName("CanvasTabs") as TabControl;
            if (canvasTabs == null) return null;

            foreach (TabItem tab in canvasTabs.Items)
            {
                if (tab.Tag is Location loc && loc == location && tab.Content is Grid canvasContainer)
                {
                    return canvasContainer.Children.OfType<Canvas>().FirstOrDefault();
                }
            }

            return null;
        }

        /// <summary>
        ///     Builds an orthogonal path between two points, using render points if they exist
        /// </summary>
        private PointCollection BuildOrthogonalPath(Point startPos, Point endPos, Connection connection, double dotOffset, Canvas? targetCanvas = null)
        {
            var pointCollection = new PointCollection();
            
            // Start point (center of dot)
            Point start = new Point(startPos.X + dotOffset, startPos.Y + dotOffset);
            Point end = new Point(endPos.X + dotOffset, endPos.Y + dotOffset);
            
            pointCollection.Add(start);

            // Get coordinate system for this canvas
            var coordSystem = targetCanvas != null ? GetCoordinateSystem(targetCanvas) : null;
            
            // Convert render points to canvas coordinates
            var renderPoints = connection.RenderPoints
                .Select(rp => {
                    if (coordSystem != null)
                    {
                        return coordSystem.ToCanvasPoint(rp.Item1, rp.Item2);
                    }
                    else
                    {
                        // Fallback - shouldn't happen
                        return new Point(rp.Item1, rp.Item2);
                    }
                })
                .ToList();

            if (renderPoints.Count == 0)
            {
                // No render points - create automatic orthogonal path
                BuildAutoOrthogonalPath(pointCollection, start, end);
            }
            else
            {
                // User has defined render points - connect through them with orthogonal segments
                Point currentPoint = start;
                
                foreach (var renderPoint in renderPoints)
                {
                    // Connect current point to render point orthogonally
                    ConnectOrthogonally(pointCollection, currentPoint, renderPoint);
                    currentPoint = renderPoint;
                }
                
                // Final segment to end point
                ConnectOrthogonally(pointCollection, currentPoint, end);
            }

            return pointCollection;
        }

        /// <summary>
        ///     Creates an automatic orthogonal path between two points with intelligent routing
        ///     Mimics the behavior shown in the reference image
        /// </summary>
        private void BuildAutoOrthogonalPath(PointCollection points, Point start, Point end)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            
            const double alignmentTolerance = 1.0;
            
            // Check if already aligned horizontally or vertically
            if (Math.Abs(dy) < alignmentTolerance)
            {
                // Horizontally aligned - direct connection
                points.Add(end);
                return;
            }
            
            if (Math.Abs(dx) < alignmentTolerance)
            {
                // Vertically aligned - direct connection
                points.Add(end);
                return;
            }

            // Not aligned - need to create a corner
            // Strategy: Create L-shaped path based on dominant direction
            double absDx = Math.Abs(dx);
            double absDy = Math.Abs(dy);
            
            // Use a simple L-routing strategy:
            // - If mostly horizontal movement needed, go horizontal first
            // - If mostly vertical movement needed, go vertical first
            
            if (absDx >= absDy)
            {
                // Horizontal-first routing
                points.Add(new Point(end.X, start.Y));  // Go horizontal first
                points.Add(end);                         // Then vertical to reach end
            }
            else
            {
                // Vertical-first routing
                points.Add(new Point(start.X, end.Y));  // Go vertical first
                points.Add(end);                         // Then horizontal to reach end
            }
        }

        /// <summary>
        ///     Connects two points with orthogonal (horizontal/vertical only) segments
        ///     If points are not aligned, creates the minimum segments needed (1 or 2)
        /// </summary>
        private void ConnectOrthogonally(PointCollection points, Point from, Point to)
        {
            const double alignmentTolerance = 1.0;
            
            // Check if already aligned
            bool horizontallyAligned = Math.Abs(from.Y - to.Y) < alignmentTolerance;
            bool verticallyAligned = Math.Abs(from.X - to.X) < alignmentTolerance;
            
            if (horizontallyAligned || verticallyAligned)
            {
                // Direct connection possible
                points.Add(to);
                return;
            }

            // Not aligned - create L-shaped connection
            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            double absDx = Math.Abs(dx);
            double absDy = Math.Abs(dy);
            
            // Choose routing direction based on which distance is greater
            if (absDx >= absDy)
            {
                // Go horizontal first, then vertical
                points.Add(new Point(to.X, from.Y));
                points.Add(to);
            }
            else
            {
                // Go vertical first, then horizontal
                points.Add(new Point(from.X, to.Y));
                points.Add(to);
            }
        }

        /// <summary>
        ///     Handles click on a connection line to add a new render point
        /// </summary>
        private void ConnectionLine_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isConnectionEditMode) return;
            
            e.Handled = true;

            if (sender is not Polyline polyline || polyline.Tag is not Connection connection)
                return;

            // Find the canvas that this line is on
            var canvas = FindParentCanvas(polyline);
            if (canvas == null) return;

            // Get the click position on the canvas
            Point clickPos = e.GetPosition(canvas);

            // Snap the click position to grid for cleaner alignment
            Point snappedPos = SnapToGrid(clickPos);

            // Convert to stored coordinates using coordinate system helper
            var coordSystem = GetCoordinateSystem(canvas);
            var (cartX, cartY) = coordSystem.FromCanvasPoint(snappedPos.X, snappedPos.Y);

            try
            {
                // Add the render point to the connection
                connection.AddRenderPoint(cartX, cartY);

                // Mark project as modified
                MarkAsModified();

                // Refresh the display on the correct canvas
                RenderConnectionLines(canvas);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add render point: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Handles click on a connection line to remove it in remove connection mode
        /// </summary>
        private void ConnectionLine_RemoveClick(object sender, MouseButtonEventArgs e)
        {
            if (!_isRemoveConnectionMode) return;
            
            e.Handled = true;

            if (sender is not Polyline polyline || polyline.Tag is not Connection connection)
                return;

            // Confirm removal
            var result = MessageBox.Show(
                $"Remove connection between Block ID {connection.LeftID} and Block ID {connection.RightID}?", 
                "Confirm Remove Connection",
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Remove the connection
                    _currentProject?.RemoveConnection(connection.LeftID, connection.RightID);

                    // Mark project as modified
                    MarkAsModified();

                    // Refresh the display without exiting remove mode
                    RenderConnectionLines();
                    PopulateTreeView();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to remove connection: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        ///     Snaps a point to a grid for better alignment
        /// </summary>
        private Point SnapToGrid(Point point)
        {
            const double gridSize = 20.0;
            
            double snappedX = Math.Round(point.X / gridSize) * gridSize;
            double snappedY = Math.Round(point.Y / gridSize) * gridSize;
            
            return new Point(snappedX, snappedY);
        }

        /// <summary>
        ///     Renders connection lines with temporary position for dragged render point
        /// </summary>
        private void RenderConnectionLinesDuringDrag()
        {
            if (_currentProject == null || _selectedRenderPointConnection == null || _selectedRenderPointDot == null || _selectedRenderPointCanvas == null)
                return;

            // Clear existing connection lines from the target canvas (but NOT the dots, as we need to keep the dragged dot intact)
            foreach (var line in _connectionLines.ToList())
            {
                // Skip the dragged dot - keep it in the list and on canvas
                if (line == _selectedRenderPointDot)
                    continue;
                    
                _selectedRenderPointCanvas.Children.Remove(line);
                _connectionLines.Remove(line);
            }

            const double dotSize = 12;
            const double dotOffset = 6;

            // Get temporary position of dragged dot
            double tempLeft = Canvas.GetLeft(_selectedRenderPointDot);
            double tempTop = Canvas.GetTop(_selectedRenderPointDot);
            Point tempPos = new Point(tempLeft + dotOffset, tempTop + dotOffset);

            var connections = _currentProject.GetAllConnections();

            foreach (var connection in connections)
            {
                // Only render connections for the current canvas
                var targetCanvas = GetCanvasForConnection(connection);
                if (targetCanvas != _selectedRenderPointCanvas) continue;

                var leftPos = GetConnectionDotPosition(connection.LeftID, dotSize, dotOffset, _selectedRenderPointCanvas);
                if (leftPos == null) continue;

                var rightPos = GetConnectionDotPosition(connection.RightID, dotSize, dotOffset, _selectedRenderPointCanvas);
                if (rightPos == null) continue;

                PointCollection pointCollection;

                if (connection == _selectedRenderPointConnection)
                {
                    // Build path using temporary position for dragged point
                    pointCollection = BuildOrthogonalPathWithTempPoint(leftPos.Value, rightPos.Value, 
                        connection, dotOffset, tempPos, _selectedRenderPointX, _selectedRenderPointY, _selectedRenderPointCanvas);
                }
                else
                {
                    // Normal path for other connections
                    pointCollection = BuildOrthogonalPath(leftPos.Value, rightPos.Value, connection, dotOffset, _selectedRenderPointCanvas);
                }

                var polyline = new Polyline
                {
                    Points = pointCollection,
                    Stroke = new SolidColorBrush(Colors.Red),
                    StrokeThickness = _isConnectionEditMode ? 6 : 2,
                    Tag = connection,
                    Cursor = _isConnectionEditMode ? Cursors.Hand : Cursors.Arrow,
                    IsHitTestVisible = false // Disable hit testing during drag to prevent interference
                };

                Canvas.SetZIndex(polyline, 1);
                _connectionLines.Add(polyline);
                _selectedRenderPointCanvas.Children.Add(polyline);

                // Render render point dots in edit mode (except the dragged one)
                if (_isConnectionEditMode)
                {
                    foreach (var renderPoint in connection.RenderPoints)
                    {
                        // Skip the dragged dot - it's already on canvas with mouse capture
                        if (connection == _selectedRenderPointConnection && 
                            renderPoint.Item1 == _selectedRenderPointX && 
                            renderPoint.Item2 == _selectedRenderPointY)
                        {
                            continue;
                        }

                        // Get coordinate system for target canvas
                        var coordSystem = GetCoordinateSystem(_selectedRenderPointCanvas);
                        Point canvasPoint = coordSystem.ToCanvasPoint(renderPoint.Item1, renderPoint.Item2);
                        
                        var dotPos = new Point(canvasPoint.X - dotOffset, canvasPoint.Y - dotOffset);

                        var renderPointDot = CreateRenderPointDot(dotPos.X, dotPos.Y, 
                            dotSize, connection, renderPoint.Item1, renderPoint.Item2);
                        
                        _connectionLines.Add(renderPointDot);
                        _selectedRenderPointCanvas.Children.Add(renderPointDot);
                    }
                }
            }
        }

        /// <summary>
        ///     Builds orthogonal path with a temporary position for one render point
        /// </summary>
        private PointCollection BuildOrthogonalPathWithTempPoint(Point startPos, Point endPos, 
            Connection connection, double dotOffset, Point tempPos, int tempX, int tempY, Canvas targetCanvas)
        {
            var pointCollection = new PointCollection();
            
            // Start point (center of dot)
            Point start = new Point(startPos.X + dotOffset, startPos.Y + dotOffset);
            Point end = new Point(endPos.X + dotOffset, endPos.Y + dotOffset);
            
            pointCollection.Add(start);

            // Get coordinate system for this canvas
            var coordSystem = GetCoordinateSystem(targetCanvas);
            
            // Get render points, replacing the dragged one with temp position
            var renderPoints = connection.RenderPoints
                .Select(rp => {
                    if (rp.Item1 == tempX && rp.Item2 == tempY)
                        return tempPos;
                    else
                        return coordSystem.ToCanvasPoint(rp.Item1, rp.Item2);
                })
                .ToList();

            if (renderPoints.Count == 0)
            {
                // No render points - create automatic orthogonal path
                BuildAutoOrthogonalPath(pointCollection, start, end);
            }
            else
            {
                // Connect through render points with orthogonal segments
                Point currentPoint = start;
                
                foreach (var renderPoint in renderPoints)
                {
                    ConnectOrthogonally(pointCollection, currentPoint, renderPoint);
                    currentPoint = renderPoint;
                }
                
                // Final segment to end point
                ConnectOrthogonally(pointCollection, currentPoint, end);
            }

            return pointCollection;
        }
    }
}
