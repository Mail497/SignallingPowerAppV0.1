using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SignallingPowerApp
{
    /// <summary>
    /// Connection mode management (edit and remove modes)
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        ///     Handles Add Connection button click - enters or exits connection edit mode
        /// </summary>
        private void AddConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnectionEditMode)
            {
                // Exit connection edit mode
                ExitConnectionEditMode();
            }
            else
            {
                // Exit other modes if active
                if (_isRemoveConnectionMode)
                {
                    ExitRemoveConnectionMode();
                }
                if (_isAddingBlock)
                {
                    CancelAddingBlock();
                }
                
                // Enter connection edit mode
                EnterConnectionEditMode();
            }
        }

        /// <summary>
        ///     Handles Remove Connection button click - enters or exits remove connection mode
        /// </summary>
        private void RemoveConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRemoveConnectionMode)
            {
                // Exit remove connection mode
                ExitRemoveConnectionMode();
            }
            else
            {
                // Exit other modes if active
                if (_isConnectionEditMode)
                {
                    ExitConnectionEditMode();
                }
                if (_isAddingBlock)
                {
                    CancelAddingBlock();
                }
                
                // Enter remove connection mode
                EnterRemoveConnectionMode();
            }
        }

        /// <summary>
        ///     Enters connection edit mode - shows connection dots on connectable blocks
        /// </summary>
        private void EnterConnectionEditMode()
        {
            _isConnectionEditMode = true;
            _firstSelectedTerminalId = null;
            _firstSelectedDot = null;

            // Change button appearance
            if (FindName("AddConnectionButton") is Button button)
            {
                button.Content = "Exit Edit Mode";
                button.Background = new SolidColorBrush(Color.FromRgb(255, 200, 200));
            }

            // Change cursor
            CanvasContainer.Cursor = Cursors.Cross;

            // Clear any existing selection
            DeselectAll();

            // Render connection dots on all connectable blocks
            RenderConnectionDots();
            
            // Re-render connection lines with edit mode features
            RenderConnectionLines();

            // Subscribe to keyboard events for Escape
            KeyDown += MainWindow_KeyDownForConnectionEdit;
        }

        /// <summary>
        ///     Exits connection edit mode - removes connection dots
        /// </summary>
        private void ExitConnectionEditMode()
        {
            _isConnectionEditMode = false;
            _firstSelectedTerminalId = null;
            _firstSelectedDot = null;

            // Reset button appearance
            if (FindName("AddConnectionButton") is Button button)
            {
                button.Content = "Edit Connections";
                button.Background = SystemColors.ControlBrush;
            }

            // Reset cursor
            CanvasContainer.Cursor = Cursors.Arrow;

            // Remove all connection dots
            ClearConnectionDots();
            
            // Re-render connection lines without edit mode features
            RenderConnectionLines();

            // Unsubscribe from keyboard events
            KeyDown -= MainWindow_KeyDownForConnectionEdit;
        }

        /// <summary>
        ///     Enters remove connection mode - allows clicking on connections to remove them
        /// </summary>
        private void EnterRemoveConnectionMode()
        {
            _isRemoveConnectionMode = true;

            // Change button appearance
            if (FindName("RemoveConnectionButton") is Button button)
            {
                button.Content = "Exit Remove Mode";
                button.Background = new SolidColorBrush(Color.FromRgb(255, 200, 200));
            }

            // Change cursor
            CanvasContainer.Cursor = Cursors.No;

            // Clear any existing selection
            DeselectAll();

            // Re-render connection lines with remove mode features
            RenderConnectionLines();

            // Subscribe to keyboard events for Escape
            KeyDown += MainWindow_KeyDownForRemoveConnection;
        }

        /// <summary>
        ///     Exits remove connection mode
        /// </summary>
        private void ExitRemoveConnectionMode()
        {
            _isRemoveConnectionMode = false;

            // Reset button appearance
            if (FindName("RemoveConnectionButton") is Button button)
            {
                button.Content = "Remove Connection";
                button.Background = SystemColors.ControlBrush;
            }

            // Reset cursor
            CanvasContainer.Cursor = Cursors.Arrow;

            // Re-render connection lines without remove mode features
            RenderConnectionLines();

            // Unsubscribe from keyboard events
            KeyDown -= MainWindow_KeyDownForRemoveConnection;
        }

        /// <summary>
        ///     Handles key down event for Escape key while in connection edit mode
        /// </summary>
        private void MainWindow_KeyDownForConnectionEdit(object sender, KeyEventArgs e)
        {
        if (e.Key == Key.Escape && _isConnectionEditMode)
            {
                ExitConnectionEditMode();
                e.Handled = true;
            }
        }

        /// <summary>
        ///     Handles key down event for Escape key while in remove connection mode
        /// </summary>
        private void MainWindow_KeyDownForRemoveConnection(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _isRemoveConnectionMode)
            {
                ExitRemoveConnectionMode();
                e.Handled = true;
            }
        }

        /// <summary>
        ///     Clears all connection dots from the canvas
        /// </summary>
        private void ClearConnectionDots()
        {
            foreach (var dot in _connectionDots)
            {
                // Find the parent canvas of this dot
                var parent = System.Windows.Media.VisualTreeHelper.GetParent(dot);
                if (parent is Canvas canvas)
                {
                    canvas.Children.Remove(dot);
                }
            }
            _connectionDots.Clear();
        }

        /// <summary>
        ///     Clears connection lines from the specified canvas, or from all canvases if no canvas is specified
        /// </summary>
        /// <param name="targetCanvas">Optional: specific canvas to clear lines from. If null, clears from all canvases.</param>
        private void ClearConnectionLines(Canvas? targetCanvas = null)
        {
            // If a specific canvas is provided, only remove lines from that canvas
            if (targetCanvas != null)
            {
                var linesToRemove = _connectionLines.Where(line =>
                {
                    var parent = System.Windows.Media.VisualTreeHelper.GetParent(line);
                    return parent == targetCanvas;
                }).ToList();

                foreach (var line in linesToRemove)
                {
                    targetCanvas.Children.Remove(line);
                    _connectionLines.Remove(line);
                }
            }
            else
            {
                // No specific canvas - remove all lines from their respective canvases
                foreach (var line in _connectionLines)
                {
                    // Find the parent canvas of this line
                    var parent = System.Windows.Media.VisualTreeHelper.GetParent(line);
                    if (parent is Canvas canvas)
                    {
                        canvas.Children.Remove(line);
                    }
                }
                _connectionLines.Clear();
            }
        }
    }
}
