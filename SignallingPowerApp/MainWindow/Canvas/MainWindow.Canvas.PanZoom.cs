using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SignallingPowerApp.Core;

namespace SignallingPowerApp
{
    /// <summary>
    /// Pan and Zoom operations for canvas
    /// Phase 6: Refactored to use IInteractiveBlock.GetRenderDimensions()
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Handles mouse left button down event to start panning
        /// </summary>
        private void CanvasContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid grid)
            {
                // If we're clicking on the canvas (not on a location or supply), deselect
                if (e.OriginalSource == grid || e.OriginalSource is Canvas)
                {
                    DeselectAll();
                }

                _isPanning = true;
                _lastMousePosition = e.GetPosition(grid);
                grid.CaptureMouse();
                grid.Cursor = Cursors.Hand;
            }
        }

        /// <summary>
        /// Handles mouse left button up event to stop panning
        /// </summary>
        private void CanvasContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid grid)
            {
                if (_isPanning)
                {
                    _isPanning = false;
                    grid.ReleaseMouseCapture();
                    grid.Cursor = Cursors.Arrow;
                }

                if (_isDraggingLocation)
                {
                    _isDraggingLocation = false;
                    grid.ReleaseMouseCapture();
                    grid.Cursor = Cursors.Arrow;
                    
                    // Update the location's render position after dragging
                    if (_selectedLocation != null && _selectedLocationBorder != null)
                    {
                        UpdateLocationRenderPosition(_selectedLocation, _selectedLocationBorder);
                    }
                }

                if (_isDraggingSupply)
                {
                    _isDraggingSupply = false;
                    grid.ReleaseMouseCapture();
                    grid.Cursor = Cursors.Arrow;
                    
                    // Update the supply's render position after dragging
                    if (_selectedSupply != null && _selectedSupplyEllipse != null)
                    {
                        UpdateSupplyRenderPosition(_selectedSupply, _selectedSupplyEllipse);
                    }
                }

                if (_isDraggingAlternator)
                {
                    _isDraggingAlternator = false;
                    grid.ReleaseMouseCapture();
                    grid.Cursor = Cursors.Arrow;
                    
                    // Update the alternator's render position after dragging
                    if (_selectedAlternator != null && _selectedAlternatorPolygon != null)
                    {
                        UpdateAlternatorRenderPosition(_selectedAlternator, _selectedAlternatorPolygon);
                    }
                }

                if (_isDraggingConductor)
                {
                    _isDraggingConductor = false;
                    grid.ReleaseMouseCapture();
                    grid.Cursor = Cursors.Arrow;
                    
                    // Update the conductor's render position after dragging
                    if (_selectedConductor != null && _selectedConductorBorder != null)
                    {
                        UpdateConductorRenderPosition(_selectedConductor, _selectedConductorBorder);
                    }
                }
            }
        }

        /// <summary>
        /// Handles mouse move event for panning the canvas
        /// </summary>
        private void CanvasContainer_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is Grid grid)
            {
                if (_isPanning && !_isDraggingLocation && !_isDraggingSupply && !_isDraggingAlternator && !_isDraggingConductor)
                {
                    Point currentPosition = e.GetPosition(grid);
                    
                    // Calculate the offset
                    double offsetX = currentPosition.X - _lastMousePosition.X;
                    double offsetY = currentPosition.Y - _lastMousePosition.Y;
                    
                    // Get the canvas from the grid
                    Canvas? canvas = grid.Children.Count > 0 ? grid.Children[0] as Canvas : null;
                    if (canvas != null && canvas.RenderTransform is TransformGroup transformGroup)
                    {
                        // Find the translate transform
                        var translateTransform = transformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();
                        if (translateTransform != null)
                        {
                            // Update the canvas position
                            translateTransform.X += offsetX;
                            translateTransform.Y += offsetY;
                        }
                    }
                    
                    // Update last position
                    _lastMousePosition = currentPosition;
                }
                else if (_isDraggingLocation && _selectedLocationBorder != null)
                {
                    HandleLocationDrag(e);
                }
                else if (_isDraggingSupply && _selectedSupplyEllipse != null)
                {
                    HandleSupplyDrag(e);
                }
                else if (_isDraggingAlternator && _selectedAlternatorPolygon != null)
                {
                    HandleAlternatorDrag(e);
                }
                else if (_isDraggingConductor && _selectedConductorBorder != null)
                {
                    HandleConductorDrag(e);
                }
            }
        }

        /// <summary>
        /// Handles mouse wheel event for zooming the canvas
        /// </summary>
        private void CanvasContainer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is Grid grid)
            {
                // Get the canvas from the grid
                Canvas? canvas = grid.Children.Count > 0 ? grid.Children[0] as Canvas : null;
                if (canvas == null) return;
                
                // Get transforms from the canvas
                if (canvas.RenderTransform is not TransformGroup transformGroup) return;
                
                var scaleTransform = transformGroup.Children.OfType<ScaleTransform>().FirstOrDefault();
                var translateTransform = transformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();
                
                if (scaleTransform == null || translateTransform == null) return;
                
                // Get current zoom level from scale transform
                double currentZoom = scaleTransform.ScaleX;
                
                // Calculate new zoom level
                double zoomChange = e.Delta > 0 ? _zoomIncrement : -_zoomIncrement;
                double newZoom = currentZoom + zoomChange;
                
                // Clamp zoom level
                newZoom = Math.Max(_zoomMin, Math.Min(_zoomMax, newZoom));
                
                if (newZoom != currentZoom)
                {
                    // Get the center of the viewport
                    double viewportCenterX = grid.ActualWidth / 2;
                    double viewportCenterY = grid.ActualHeight / 2;
                    
                    // Calculate the scale factor
                    double scaleFactor = newZoom / currentZoom;
                    
                    // Adjust the translation to keep the viewport center stationary
                    translateTransform.X = viewportCenterX - scaleFactor * (viewportCenterX - translateTransform.X);
                    translateTransform.Y = viewportCenterY - scaleFactor * (viewportCenterY - translateTransform.Y);
                    
                    // Update zoom level
                    scaleTransform.ScaleX = newZoom;
                    scaleTransform.ScaleY = newZoom;
                }
                
                e.Handled = true;
            }
        }

        /// <summary>
        ///     Handles Fit button click event
        /// </summary>
        private void FitButton_Click(object sender, RoutedEventArgs e)
        {
            FitCanvasToBlocks();
        }

        /// <summary>
        ///     Handles Zoom In button click event
        /// </summary>
        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomCanvas(_zoomIncrement);
        }

        /// <summary>
        ///     Handles Zoom Out button click event
        /// </summary>
        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            ZoomCanvas(-_zoomIncrement);
        }

        /// <summary>
        ///     Zooms the canvas centered on the current viewport center
        /// </summary>
        /// <param name="zoomChange">The amount to change the zoom level (positive for zoom in, negative for zoom out)</param>
        private void ZoomCanvas(double zoomChange)
        {
            var (container, canvas) = GetActiveCanvas();
            if (container == null || canvas == null) return;

            // Get transforms from the canvas
            if (canvas.RenderTransform is not TransformGroup transformGroup) return;
            
            var scaleTransform = transformGroup.Children.OfType<ScaleTransform>().FirstOrDefault();
            var translateTransform = transformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();
            
            if (scaleTransform == null || translateTransform == null) return;

            // Get current zoom level
            double currentZoom = scaleTransform.ScaleX;
            
            // Calculate new zoom level
            double newZoom = currentZoom + zoomChange;
            
            // Clamp zoom level
            newZoom = Math.Max(_zoomMin, Math.Min(_zoomMax, newZoom));
            
            if (newZoom != currentZoom)
            {
                // Get the center of the viewport
                double viewportCenterX = container.ActualWidth / 2;
                double viewportCenterY = container.ActualHeight / 2;
                
                // Calculate the scale factor
                double scaleFactor = newZoom / currentZoom;
                
                // Adjust the translation to keep the viewport center stationary
                translateTransform.X = viewportCenterX - scaleFactor * (viewportCenterX - translateTransform.X);
                translateTransform.Y = viewportCenterY - scaleFactor * (viewportCenterY - translateTransform.Y);
                
                // Update zoom level
                scaleTransform.ScaleX = newZoom;
                scaleTransform.ScaleY = newZoom;
            }
        }

        /// <summary>
        ///     Fits the canvas zoom and position to show all blocks
        /// </summary>
        public void FitCanvasToBlocks()
        {
            var (container, canvas) = GetActiveCanvas();
            if (container == null || canvas == null) return;

            // Get transforms from the canvas
            if (canvas.RenderTransform is not TransformGroup transformGroup) return;
            
            var scaleTransform = transformGroup.Children.OfType<ScaleTransform>().FirstOrDefault();
            var translateTransform = transformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();
            
            if (scaleTransform == null || translateTransform == null) return;

            // Get viewport size (container size)
            double viewportWidth = container.ActualWidth;
            double viewportHeight = container.ActualHeight;
            
            // Check if this is a location canvas
            var canvasTabs = FindName("CanvasTabs") as TabControl;
            bool isLocationCanvas = canvasTabs?.SelectedItem is TabItem tabItem && tabItem.Tag is Location;
            
            if (isLocationCanvas)
            {
                FitLocationCanvas(canvas, scaleTransform, translateTransform, viewportWidth, viewportHeight);
                return;
            }
            
            FitLayoutCanvas(canvas, scaleTransform, translateTransform, viewportWidth, viewportHeight);
        }

        /// <summary>
        ///     Fits a location canvas to view
        ///     Phase 6: Refactored to use IInteractiveBlock.GetRenderDimensions()
        /// </summary>
        private void FitLocationCanvas(Canvas canvas, ScaleTransform scaleTransform, TranslateTransform translateTransform, 
            double viewportWidth, double viewportHeight)
        {
            // Get the location from canvas tag
            var canvasTabs = FindName("CanvasTabs") as TabControl;
            if (canvasTabs?.SelectedItem is not TabItem tabItem || tabItem.Tag is not Location location)
            {
                // Default centering if no location
                double centerX = canvas.Width / 2;
                double centerY = canvas.Height / 2;
                
                scaleTransform.ScaleX = 1.0;
                scaleTransform.ScaleY = 1.0;
                
                translateTransform.X = (viewportWidth / 2) - centerX;
                translateTransform.Y = (viewportHeight / 2) - centerY;
                return;
            }
            
            // Get all renderable blocks in this location using IInteractiveBlock
            var renderableBlocks = new System.Collections.Generic.List<(double x, double y, double width, double height)>();
            
            foreach (var child in location.GetChildren())
            {
                if (child is not IInteractiveBlock interactive) continue;
                
                var (width, height) = interactive.GetRenderDimensions();
                
                // Get render position via reflection
                var renderPos = GetRenderPositionViaReflection(child);
                int x = renderPos.Item1 ?? 0;
                int y = renderPos.Item2 ?? 0;
                Point canvasPos = CartesianToCanvas(canvas, x, y);
                
                double left = canvasPos.X - (width / 2);
                double top = canvasPos.Y - (height / 2);
                
                renderableBlocks.Add((left, top, width, height));
            }
            
            // Check if there are no blocks to fit
            if (renderableBlocks.Count == 0)
            {
                // Center on the empty state button or canvas center
                double centerX = canvas.Width / 2;
                double centerY = canvas.Height / 2;
                
                scaleTransform.ScaleX = 1.0;
                scaleTransform.ScaleY = 1.0;
                
                translateTransform.X = (viewportWidth / 2) - centerX;
                translateTransform.Y = (viewportHeight / 2) - centerY;
                return;
            }
            
            // Calculate bounding box
            double minX = renderableBlocks.Min(b => b.x);
            double minY = renderableBlocks.Min(b => b.y);
            double maxX = renderableBlocks.Max(b => b.x + b.width);
            double maxY = renderableBlocks.Max(b => b.y + b.height);

            double contentWidth = maxX - minX;
            double contentHeight = maxY - minY;

            // Add padding (10% on each side)
            const double padding = 0.1;
            double paddingX = contentWidth * padding;
            double paddingY = contentHeight * padding;

            contentWidth += paddingX * 2;
            contentHeight += paddingY * 2;
            minX -= paddingX;
            minY -= paddingY;

            // Calculate zoom level to fit content in viewport
            double zoomX = viewportWidth / contentWidth;
            double zoomY = viewportHeight / contentHeight;
            double newZoom = Math.Min(zoomX, zoomY);

            // Clamp zoom level
            newZoom = Math.Max(_zoomMin, Math.Min(_zoomMax, newZoom));

            // Calculate center of content
            double contentCenterX = minX + contentWidth / 2;
            double contentCenterY = minY + contentHeight / 2;

            // Calculate translation to center content in viewport
            double translateX = (viewportWidth / 2) - (contentCenterX * newZoom);
            double translateY = (viewportHeight / 2) - (contentCenterY * newZoom);

            // Apply transformations
            scaleTransform.ScaleX = newZoom;
            scaleTransform.ScaleY = newZoom;
            translateTransform.X = translateX;
            translateTransform.Y = translateY;
        }

        /// <summary>
        ///     Fits the layout canvas to view
        ///     Phase 6: Refactored to use IInteractiveBlock.GetRenderDimensions()
        /// </summary>
        private void FitLayoutCanvas(Canvas canvas, ScaleTransform scaleTransform, TranslateTransform translateTransform, 
            double viewportWidth, double viewportHeight)
        {
            // Check if we're in empty state (no blocks)
            if (_currentProject != null && !_currentProject.GetAllBlocks.Any())
            {
                double centerX = canvas.Width / 2;
                double centerY = canvas.Height / 2;
                
                scaleTransform.ScaleX = 1.0;
                scaleTransform.ScaleY = 1.0;
                
                translateTransform.X = (viewportWidth / 2) - centerX;
                translateTransform.Y = (viewportHeight / 2) - centerY;
                return;
            }
            
            // Get all blocks that have render positions using IInteractiveBlock
            var renderableBlocks = new System.Collections.Generic.List<(double x, double y, double width, double height)>();

            foreach (var block in _currentProject.GetAllBlocks)
            {
                // Only process blocks that should appear on the layout canvas
                if (block is not IInteractiveBlock interactive) continue;
                if (interactive.PreferredCanvas != CanvasType.Layout) continue;
                
                var (width, height) = interactive.GetRenderDimensions();
                
                // Get render position via reflection
                var renderPos = GetRenderPositionViaReflection(block);
                int x = renderPos.Item1 ?? 0;
                int y = renderPos.Item2 ?? 0;
                Point canvasPos = CartesianToCanvas(x, y);
                
                double left = canvasPos.X - (width / 2);
                double top = canvasPos.Y - (height / 2);
                
                renderableBlocks.Add((left, top, width, height));
            }

            if (renderableBlocks.Count == 0)
            {
                scaleTransform.ScaleX = 1.0;
                scaleTransform.ScaleY = 1.0;
                translateTransform.X = 0;
                translateTransform.Y = 0;
                return;
            }

            // Calculate bounding box
            double minX = renderableBlocks.Min(b => b.x);
            double minY = renderableBlocks.Min(b => b.y);
            double maxX = renderableBlocks.Max(b => b.x + b.width);
            double maxY = renderableBlocks.Max(b => b.y + b.height);

            double contentWidth = maxX - minX;
            double contentHeight = maxY - minY;

            // Add padding (10% on each side)
            const double padding = 0.1;
            double paddingX = contentWidth * padding;
            double paddingY = contentHeight * padding;

            contentWidth += paddingX * 2;
            contentHeight += paddingY * 2;
            minX -= paddingX;
            minY -= paddingY;

            // Calculate zoom level to fit content in viewport
            double zoomX = viewportWidth / contentWidth;
            double zoomY = viewportHeight / contentHeight;
            double newZoom = Math.Min(zoomX, zoomY);

            // Clamp zoom level
            newZoom = Math.Max(_zoomMin, Math.Min(_zoomMax, newZoom));

            // Calculate center of content
            double contentCenterX = minX + contentWidth / 2;
            double contentCenterY = minY + contentHeight / 2;

            // Calculate translation to center content in viewport
            double translateX = (viewportWidth / 2) - (contentCenterX * newZoom);
            double translateY = (viewportHeight / 2) - (contentCenterY * newZoom);

            // Apply transformations
            scaleTransform.ScaleX = newZoom;
            scaleTransform.ScaleY = newZoom;
            translateTransform.X = translateX;
            translateTransform.Y = translateY;
        }
    }
}
