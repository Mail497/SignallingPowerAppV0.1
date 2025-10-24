using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SignallingPowerApp.Core;

namespace SignallingPowerApp
{
    /// <summary>
    /// Render point management for connections
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        ///     Creates a render point dot ellipse
        /// </summary>
        private Ellipse CreateRenderPointDot(double left, double top, double size, Connection connection, int x, int y)
        {
            var dot = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(_isRemoveConnectionMode ? Colors.DarkRed : Colors.Cyan),
                Stroke = new SolidColorBrush(_isRemoveConnectionMode ? Colors.Red : Colors.DarkCyan),
                StrokeThickness = 2,
                Cursor = _isRemoveConnectionMode ? Cursors.No : Cursors.SizeAll,
                Tag = new { Connection = connection, X = x, Y = y }
            };

            Canvas.SetLeft(dot, left);
            Canvas.SetTop(dot, top);
            Canvas.SetZIndex(dot, 1001); // Ensure render point dots are above connection lines

            // Add mouse handlers based on mode
            if (_isRemoveConnectionMode)
            {
                dot.MouseLeftButtonDown += RenderPointDot_RemoveClick;
            }
            else
            {
                // Add mouse handlers for dragging in edit mode
                dot.MouseLeftButtonDown += RenderPointDot_MouseLeftButtonDown;
                dot.MouseLeftButtonUp += RenderPointDot_MouseLeftButtonUp;
                dot.MouseMove += RenderPointDot_MouseMove;
            }

            return dot;
        }

        /// <summary>
        ///     Handles click on a render point dot to remove it in remove connection mode
        /// </summary>
        private void RenderPointDot_RemoveClick(object sender, MouseButtonEventArgs e)
        {
            if (!_isRemoveConnectionMode) return;
            
            e.Handled = true;

            if (sender is not Ellipse dot)
                return;

            // Extract connection and position from tag
            dynamic tag = dot.Tag;
            Connection connection = tag.Connection;
            int x = tag.X;
            int y = tag.Y;

            // Confirm removal
            var result = MessageBox.Show(
                $"Remove render point at ({x}, {y})?", 
                "Confirm Remove Render Point",
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    connection.RemoveRenderPoint(x, y);
                    
                    // Mark project as modified
                    MarkAsModified();
                    
                    // Refresh the appropriate canvas
                    var targetCanvas = GetCanvasForConnection(connection);
                    if (targetCanvas != null)
                    {
                        RenderConnectionLines(targetCanvas);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to remove render point: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        ///     Handles click on a render point dot to start dragging or remove it
        /// </summary>
        private void RenderPointDot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (sender is not Ellipse dot)
                return;

            // Extract connection and position from tag
            dynamic tag = dot.Tag;
            Connection connection = tag.Connection;
            int x = tag.X;
            int y = tag.Y;

            // Check if right-click for removal
            if (e.RightButton == MouseButtonState.Pressed)
            {
                var result = MessageBox.Show($"Remove render point at ({x}, {y})?", "Confirm",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        connection.RemoveRenderPoint(x, y);
                        
                        // Mark project as modified
                        MarkAsModified();
                        
                        // Refresh the appropriate canvas
                        var targetCanvas = GetCanvasForConnection(connection);
                        if (targetCanvas != null)
                        {
                            RenderConnectionLines(targetCanvas);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to remove render point: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                return;
            }

            // Find the canvas that this dot is on
            var canvas = FindParentCanvas(dot);
            if (canvas == null) return;

            // Left-click starts dragging
            _isDraggingRenderPoint = true;
            _renderPointDragStartPoint = e.GetPosition(canvas);
            _selectedRenderPointConnection = connection;
            _selectedRenderPointX = x;
            _selectedRenderPointY = y;
            _selectedRenderPointDot = dot;
            _selectedRenderPointCanvas = canvas;
            
            dot.CaptureMouse();
            dot.Fill = new SolidColorBrush(Colors.Yellow); // Highlight during drag
        }

        /// <summary>
        ///     Handles mouse button up on a render point dot to stop dragging
        /// </summary>
        private void RenderPointDot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDraggingRenderPoint || sender is not Ellipse dot || _selectedRenderPointCanvas == null)
                return;

            e.Handled = true;

            // Update the render point position in the connection
            if (_selectedRenderPointConnection != null)
            {
                try
                {
                    // Get the final position
                    double left = Canvas.GetLeft(dot);
                    double top = Canvas.GetTop(dot);
                    const double dotOffset = 6;
                    double centerX = left + dotOffset;
                    double centerY = top + dotOffset;

                    // Convert to stored coordinates using coordinate system helper
                    var coordSystem = GetCoordinateSystem(_selectedRenderPointCanvas);
                    var (newX, newY) = coordSystem.FromCanvasPoint(centerX, centerY);

                    // Remove old point and add new point
                    _selectedRenderPointConnection.RemoveRenderPoint(_selectedRenderPointX, _selectedRenderPointY);
                    _selectedRenderPointConnection.AddRenderPoint(newX, newY);

                    // Mark project as modified
                    MarkAsModified();

                    // Re-render connections on the correct canvas
                    RenderConnectionLines(_selectedRenderPointCanvas);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to update render point: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    // Re-render to restore original position
                    if (_selectedRenderPointCanvas != null)
                    {
                        RenderConnectionLines(_selectedRenderPointCanvas);
                    }
                }
            }

            // Cleanup
            _isDraggingRenderPoint = false;
            _renderPointDragStartPoint = default;
            _selectedRenderPointConnection = null;
            _selectedRenderPointDot = null;
            _selectedRenderPointCanvas = null;
            
            dot.ReleaseMouseCapture();
        }

        /// <summary>
        ///     Handles mouse move on a render point dot for dragging
        /// </summary>
        private void RenderPointDot_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingRenderPoint || sender is not Ellipse dot || dot != _selectedRenderPointDot || _selectedRenderPointCanvas == null)
                return;

            Point currentPosition = e.GetPosition(_selectedRenderPointCanvas);
            
            // Snap to grid while dragging for cleaner alignment
            Point snappedPosition = SnapToGrid(currentPosition);
            
            // Update position
            const double dotOffset = 6;
            double newLeft = snappedPosition.X - dotOffset;
            double newTop = snappedPosition.Y - dotOffset;
            
            Canvas.SetLeft(dot, newLeft);
            Canvas.SetTop(dot, newTop);
            
            // Update drag start point to snapped position
            _renderPointDragStartPoint = snappedPosition;
            
            // Redraw connection lines to show real-time feedback on the correct canvas
            RenderConnectionLinesDuringDrag();
            
            e.Handled = true;
        }
    }
}
