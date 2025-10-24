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
    /// Core rendering orchestration for canvas
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        ///     Renders the entire project on the canvas
        /// </summary>
        private void RenderProject()
        {
            if (_currentProject == null) return;

            // Clear selection
            DeselectAll();

            // Clear existing canvas elements
            DiagramCanvas.Children.Clear();
            
            // Check if project has any blocks
            bool hasBlocks = _currentProject.GetAllBlocks.Any();
            
            if (!hasBlocks)
            {
                // No blocks - render empty state with add button
                RenderEmptyState();
                return;
            }
            
            // Re-add placeholder if needed
            var placeholder = new TextBlock
            {
                Text = "Diagram Canvas Area - Click and drag to pan, mouse wheel to zoom",
                FontSize = 16,
                Foreground = new SolidColorBrush(Colors.LightGray)
            };
            Canvas.SetLeft(placeholder, 400);
            Canvas.SetTop(placeholder, 200);
            DiagramCanvas.Children.Add(placeholder);

            // Render connection lines first (so they appear behind blocks)
            RenderConnectionLines();

            // Render all blocks
            foreach (var block in _currentProject.GetAllBlocks)
            {
                if (block is Location location)
                {
                    RenderLocation(location);
                }
                else if (block is Supply supply)
                {
                    RenderSupply(supply);
                }
                else if (block is AlternatorBlock alternator)
                {
                    RenderAlternator(alternator);
                }
                else if (block is ConductorBlock conductor)
                {
                    RenderConductor(conductor);
                }
            }

            // If in connection edit mode, re-render connection dots
            if (_isConnectionEditMode)
            {
                _connectionDots.Clear();
                RenderConnectionDots();
            }
        }

        /// <summary>
        ///     Renders an empty state with a square and plus button in the center of the canvas
        /// </summary>
        private void RenderEmptyState()
        {
            RenderEmptyStateOnCanvas(DiagramCanvas);
        }

        /// <summary>
        ///     Renders an empty state for a location canvas with a square and plus button
        /// </summary>
        /// <param name="canvas">The canvas to render the empty state on</param>
        private void RenderLocationCanvasEmptyState(Canvas canvas)
        {
            RenderEmptyStateOnCanvas(canvas);
        }

        /// <summary>
        ///     Renders an empty state with a square and plus button in the center of the specified canvas
        /// </summary>
        /// <param name="canvas">The canvas to render the empty state on</param>
        private void RenderEmptyStateOnCanvas(Canvas canvas)
        {
            const double squareSize = 200;
            const double plusSize = 60;
            const double plusThickness = 8;
            
            // Calculate center of canvas
            double centerX = canvas.Width / 2;
            double centerY = canvas.Height / 2;
            
            // Create outer square border
            var emptyStateBorder = new Border
            {
                Width = squareSize,
                Height = squareSize,
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(3),
                Background = new SolidColorBrush(Colors.White),
                Cursor = Cursors.Hand
            };
            
            // Create a grid to hold the plus sign
            var grid = new Grid
            {
                Width = squareSize,
                Height = squareSize
            };
            
            // Create plus sign using two rectangles
            var horizontalBar = new Rectangle
            {
                Width = plusSize,
                Height = plusThickness,
                Fill = new SolidColorBrush(Colors.Black),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var verticalBar = new Rectangle
            {
                Width = plusThickness,
                Height = plusSize,
                Fill = new SolidColorBrush(Colors.Black),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            // Add rectangles to grid
            grid.Children.Add(horizontalBar);
            grid.Children.Add(verticalBar);
            
            // Set grid as border's child
            emptyStateBorder.Child = grid;
            
            // Position at canvas center
            Canvas.SetLeft(emptyStateBorder, centerX - (squareSize / 2));
            Canvas.SetTop(emptyStateBorder, centerY - (squareSize / 2));
            
            // Add click handler to show add menu
            emptyStateBorder.MouseLeftButtonDown += EmptyStateButton_Click;
            
            // Add to canvas
            canvas.Children.Add(emptyStateBorder);
        }

        /// <summary>
        ///     Handles click on the empty state add button
        /// </summary>
        private void EmptyStateButton_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            
            // Show the Add context menu
            if (FindName("AddButton") is Button addButton && addButton.ContextMenu != null)
            {
                addButton.ContextMenu.PlacementTarget = addButton;
                addButton.ContextMenu.IsOpen = true;
            }
        }
    }
}
