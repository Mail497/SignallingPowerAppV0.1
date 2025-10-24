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
    /// Location canvas block rendering operations (Busbar, TransformerUPS, Load, ExternalBusbar)
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        ///     Renders a Busbar block on a location canvas
        /// </summary>
        /// <param name="busbar">The Busbar object to render</param>
        /// <param name="canvas">The canvas to render on</param>
        private void RenderBusbarOnLocationCanvas(Busbar busbar, Canvas canvas)
        {
            const double borderThickness = 3;
            
            // Get rows from busbar
            var rows = busbar.GetRows().ToList();
            
            // Calculate total height based on rows (name + rows + plus button)
            double busbarContentHeight = Busbar.NameHeight + (rows.Count * Busbar.RowHeight);
            double totalHeight = busbarContentHeight + Busbar.PlusButtonSize + Busbar.PlusButtonGap;
            
            // Get position from Busbar object (default to origin if not set)
            int x = busbar.RenderPosition.Item1 ?? 0;
            int y = busbar.RenderPosition.Item2 ?? 0;
            
            // Convert Cartesian to Canvas coordinates
            Point canvasPos = CartesianToCanvas(canvas, x, y);
            
            // Calculate position so busbar is centered at the point
            double left = canvasPos.X - (Busbar.BusbarWidth / 2);
            double top = canvasPos.Y - (totalHeight / 2);
            
            // Create main container stack panel for the busbar and plus button
            var containerStack = new StackPanel
            {
                Width = Busbar.BusbarWidth
            };
            
            // Create busbar container grid
            var busbarGrid = new Grid
            {
                Width = Busbar.BusbarWidth,
                Height = busbarContentHeight
            };
            
            // Define rows for label
            busbarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(Busbar.NameHeight) });
            
            // Add row definitions for each row
            foreach (var row in rows)
            {
                busbarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(Busbar.RowHeight) });
            }
            
            // Create label "Busbar Name" at the top
            var nameLabel = new TextBlock
            {
                Text = busbar.Name,
                FontSize = 14,
                FontWeight = FontWeights.Normal,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0),
                IsHitTestVisible = false // Allow mouse events to pass through to the grid
            };
            Grid.SetRow(nameLabel, 0);
            busbarGrid.Children.Add(nameLabel);
            
            // Create a clickable area for the name section (for selection and dragging)
            var nameClickArea = new Border
            {
                Background = new SolidColorBrush(Colors.Transparent),
                Cursor = Cursors.Hand
            };
            Grid.SetRow(nameClickArea, 0);
            busbarGrid.Children.Add(nameClickArea);
            
            // Add mouse event handlers to the name area for selection and dragging
            nameClickArea.MouseLeftButtonDown += BusbarNameArea_MouseLeftButtonDown;
            nameClickArea.MouseLeftButtonUp += BusbarNameArea_MouseLeftButtonUp;
            nameClickArea.MouseMove += BusbarNameArea_MouseMove;
            
            // Store busbar reference in the name click area
            nameClickArea.Tag = busbar;
            
            // Create border around the entire busbar content
            var busbarBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(borderThickness),
                Background = new SolidColorBrush(Colors.White),
                Child = busbarGrid
            };
            
            // Add rows to the grid
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                
                // Create a grid for this row with 3 columns
                var rowGrid = new Grid
                {
                    Height = Busbar.RowHeight,
                    Tag = row, // Store row reference
                    Background = new SolidColorBrush(Colors.Transparent), // Make it clickable
                    Cursor = Cursors.Hand
                };
                
                // Add mouse event handler for row selection
                rowGrid.MouseLeftButtonDown += RowGrid_MouseLeftButtonDown;
                
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                
                // Add vertical borders to separate columns
                var leftBorder = new Border
                {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(0, borderThickness, borderThickness, 0),
                    IsHitTestVisible = false // Allow clicks to pass through to rowGrid
                };
                Grid.SetColumn(leftBorder, 0);
                rowGrid.Children.Add(leftBorder);
                
                var middleBorder = new Border
                {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(0, borderThickness, borderThickness, 0),
                    IsHitTestVisible = false // Allow clicks to pass through to rowGrid
                };
                Grid.SetColumn(middleBorder, 1);
                rowGrid.Children.Add(middleBorder);
                
                // Add shape in the middle column based on protection type
                if (row.Type == "CircuitBreaker")
                {
                    // Circuit breaker - render a square
                    var square = new Rectangle
                    {
                        Width = 20,
                        Height = 20,
                        Fill = new SolidColorBrush(Colors.Black),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        IsHitTestVisible = false // Allow clicks to pass through to rowGrid
                    };
                    Grid.SetColumn(square, 1);
                    rowGrid.Children.Add(square);
                }
                else
                {
                    // Pin - render a circle
                    var circle = new Ellipse
                    {
                        Width = 20,
                        Height = 20,
                        Fill = new SolidColorBrush(Colors.Black),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        IsHitTestVisible = false // Allow clicks to pass through to rowGrid
                    };
                    Grid.SetColumn(circle, 1);
                    rowGrid.Children.Add(circle);
                }
                
                var rightBorder = new Border
                {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(0, borderThickness, 0, 0),
                    IsHitTestVisible = false // Allow clicks to pass through to rowGrid
                };
                Grid.SetColumn(rightBorder, 2);
                rowGrid.Children.Add(rightBorder);
                
                Grid.SetRow(rowGrid, i + 1); // +1 because row 0 is the name
                busbarGrid.Children.Add(rowGrid);
            }
            
            // Add the busbar to the container
            containerStack.Children.Add(busbarBorder);
            
            // Create plus button below the busbar
            var plusButtonBorder = new Border
            {
                Width = Busbar.PlusButtonSize,
                Height = Busbar.PlusButtonSize,
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(borderThickness),
                Background = new SolidColorBrush(Colors.White),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, Busbar.PlusButtonGap, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            // Create plus sign grid
            var plusGrid = new Grid
            {
                Width = Busbar.PlusButtonSize,
                Height = Busbar.PlusButtonSize
            };
            
            // Create plus sign using two rectangles
            var horizontalBar = new Rectangle
            {
                Width = 20,
                Height = 4,
                Fill = new SolidColorBrush(Colors.Black),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var verticalBar = new Rectangle
            {
                Width = 4,
                Height = 20,
                Fill = new SolidColorBrush(Colors.Black),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            plusGrid.Children.Add(horizontalBar);
            plusGrid.Children.Add(verticalBar);
            plusButtonBorder.Child = plusGrid;
            
            // Store busbar reference in the plus button
            plusButtonBorder.Tag = busbar;
            
            // Add click handler for plus button
            plusButtonBorder.MouseLeftButtonDown += BusbarPlusButton_Click;
            
            // Add plus button to container
            containerStack.Children.Add(plusButtonBorder);
            
            // Store reference to busbar in the main container
            containerStack.Tag = busbar;
            
            // Position the container on the canvas
            Canvas.SetLeft(containerStack, left);
            Canvas.SetTop(containerStack, top);
            
            // Remove empty state if it exists
            var emptyState = canvas.Children.OfType<Border>()
                .FirstOrDefault(b => b.Cursor == Cursors.Hand && b.Width == 200);
            if (emptyState != null)
            {
                canvas.Children.Remove(emptyState);
            }
            
            // Add to canvas
            canvas.Children.Add(containerStack);
        }
        
        /// <summary>
        ///     Renders a TransformerUPS block on a location canvas as two overlapping circles
        /// </summary>
        /// <param name="transformerUPS">The TransformerUPSBlock object to render</param>
        /// <param name="canvas">The canvas to render on</param>
        private void RenderTransformerUPSOnLocationCanvas(TransformerUPSBlock transformerUPS, Canvas canvas)
        {
            const double circleRadius = 50;
            const double circleDiameter = circleRadius * 2;
            const double overlap = 30; // How much the circles overlap
            const double totalWidth = (circleDiameter * 2) - overlap;
            const double totalHeight = circleDiameter;
            
            // Get position from TransformerUPSBlock object (default to origin if not set)
            int x = transformerUPS.RenderPosition.Item1 ?? 0;
            int y = transformerUPS.RenderPosition.Item2 ?? 0;
            
            // Convert Cartesian to Canvas coordinates
            Point canvasPos = CartesianToCanvas(canvas, x, y);
            
            // Calculate position so the TransformerUPS is centered at the point
            double left = canvasPos.X - (totalWidth / 2);
            double top = canvasPos.Y - (totalHeight / 2);
            
            // Create main container grid for the transformer
            var containerGrid = new Grid
            {
                Width = totalWidth,
                Height = totalHeight,
                Tag = transformerUPS,
                Background = new SolidColorBrush(Colors.Transparent),
                Cursor = Cursors.Hand
            };
            
            // Create left circle
            var leftCircle = new Ellipse
            {
                Width = circleDiameter,
                Height = circleDiameter,
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 3,
                Fill = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            // Create right circle
            var rightCircle = new Ellipse
            {
                Width = circleDiameter,
                Height = circleDiameter,
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 3,
                Fill = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            // Add circles to container
            containerGrid.Children.Add(leftCircle);
            containerGrid.Children.Add(rightCircle);
            
            // Create text label above the transformer showing rating
            var textLabel = new TextBlock
            {
                FontSize = 14,
                FontWeight = FontWeights.Normal,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -20, 0, 0),
                IsHitTestVisible = false // Allow mouse events to pass through
            };
            
            // Set text content based on whether equipment is assigned
            try
            {
                var equipment = transformerUPS.Equipment;
                textLabel.Text = $"{equipment.Rating} kVA";
            }
            catch
            {
                textLabel.Text = transformerUPS.Name;
            }
            
            containerGrid.Children.Add(textLabel);
            
            // Add mouse event handlers for selection and dragging
            containerGrid.MouseLeftButtonDown += TransformerUPSGrid_MouseLeftButtonDown;
            containerGrid.MouseLeftButtonUp += TransformerUPSGrid_MouseLeftButtonUp;
            containerGrid.MouseMove += TransformerUPSGrid_MouseLeftButtonMove;
            
            // Position the container on the canvas
            Canvas.SetLeft(containerGrid, left);
            Canvas.SetTop(containerGrid, top);
            
            // Remove empty state if it exists
            var emptyState = canvas.Children.OfType<Border>()
                .FirstOrDefault(b => b.Cursor == Cursors.Hand && b.Width == 200);
            if (emptyState != null)
            {
                canvas.Children.Remove(emptyState);
            }
            
            // Add to canvas
            canvas.Children.Add(containerGrid);
        }
        
        /// <summary>
        ///     Renders an ExternalBusbar block on a location canvas
        /// </summary>
        /// <param name="externalBusbar">The ExternalBusbar object to render</param>
        /// <param name="canvas">The canvas to render on</param>
        private void RenderExternalBusbarOnLocationCanvas(ExternalBusbar externalBusbar, Canvas canvas)
        {
            const double nameWidth = 40;        // Width for the vertical name section
            const double rowsWidth = 100;       // Width for the rows section
            const double totalWidth = nameWidth + rowsWidth;
            const double borderThickness = 3;
            const double rowHeight = 50;
            const int numberOfRows = 8;         // Eight rows as per specification
            
            // Calculate total height based on rows
            double totalHeight = numberOfRows * rowHeight;
            
            // Get position from ExternalBusbar object (default to origin if not set)
            int x = externalBusbar.RenderPosition.Item1 ?? 0;
            int y = externalBusbar.RenderPosition.Item2 ?? 0;
            
            // Convert Cartesian to Canvas coordinates
            Point canvasPos = CartesianToCanvas(canvas, x, y);
            
            // Calculate position so the ExternalBusbar is centered at the point
            double left = canvasPos.X - (totalWidth / 2);
            double top = canvasPos.Y - (totalHeight / 2);
            
            // Create main container canvas for the external busbar
            var containerCanvas = new Canvas
            {
                Width = totalWidth,
                Height = totalHeight,
                Tag = externalBusbar, // Store reference for identification
                Background = new SolidColorBrush(Colors.Transparent),
                Cursor = Cursors.Hand,
                ClipToBounds = false // Allow text to extend beyond bounds
            };
            
            // Add mouse event handlers for selection and dragging
            containerCanvas.MouseLeftButtonDown += ExternalBusbarCanvas_MouseLeftButtonDown;
            containerCanvas.MouseLeftButtonUp += ExternalBusbarCanvas_MouseLeftButtonUp;
            containerCanvas.MouseMove += ExternalBusbarCanvas_MouseMove;
            
            // Create the name section border (left column)
            var nameBorder = new Border
            {
                Width = nameWidth,
                Height = totalHeight,
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(borderThickness, borderThickness, 0, borderThickness),
                Background = new SolidColorBrush(Colors.White),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(nameBorder, 0);
            Canvas.SetTop(nameBorder, 0);
            containerCanvas.Children.Add(nameBorder);
            
            // Create vertical text for external busbar name using LayoutTransform
            var nameLabel = new TextBlock
            {
                Text = externalBusbar.Name,
                FontSize = 14,
                FontWeight = FontWeights.Normal,
                TextAlignment = TextAlignment.Center,
                LayoutTransform = new RotateTransform(-90),
                IsHitTestVisible = false
            };
            
            // With LayoutTransform, the element is rotated before layout
            // So a rotated TextBlock will have swapped dimensions
            // Position it so it's centered in the name column
            Canvas.SetLeft(nameLabel, (nameWidth - nameLabel.ActualHeight) / 2);
            Canvas.SetTop(nameLabel, totalHeight / 2);
            
            // We need to use a different approach - create a Border to hold and center the text
            var textBorder = new Border
            {
                Width = nameWidth,
                Height = totalHeight,
                Child = nameLabel,
                IsHitTestVisible = false,
                ClipToBounds = false
            };
            
            // Center the text within the border
            nameLabel.LayoutTransform = new RotateTransform(-90);
            nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            nameLabel.VerticalAlignment = VerticalAlignment.Center;
            
            Canvas.SetLeft(textBorder, 0);
            Canvas.SetTop(textBorder, 0);
            containerCanvas.Children.Add(textBorder);
            
            // Create the rows section border (right column)
            var rowsBorder = new Border
            {
                Width = rowsWidth,
                Height = totalHeight,
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(borderThickness, borderThickness, borderThickness, borderThickness),
                Background = new SolidColorBrush(Colors.White),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(rowsBorder, nameWidth);
            Canvas.SetTop(rowsBorder, 0);
            containerCanvas.Children.Add(rowsBorder);
            
            // Get the parent location to check connections
            var parentLocation = _currentProject.GetBlock(externalBusbar.ParentID) as Location;
            var externalTerminals = parentLocation?.ExternalTerminals ?? Array.Empty<Terminal>();
            
            // Add colored indicator boxes for each row
            for (int i = 0; i < numberOfRows; i++)
            {
                // Determine if this terminal has an external connection
                bool hasExternalConnection = false;
                
                if (i < externalTerminals.Length)
                {
                    var terminal = externalTerminals[i];
                    var connections = _currentProject.GetConnections(terminal.ID);
                    
                    // Check if any connection leads outside the location
                    foreach (var connection in connections)
                    {
                        // Get the other end of the connection
                        int otherEndId = connection.LeftID == terminal.ID ? connection.RightID : connection.LeftID;
                        var otherBlock = _currentProject.GetBlock(otherEndId);
                        
                        // Check if the other block's parent chain doesn't include this location
                        bool isExternal = IsConnectionExternal(otherBlock, parentLocation.ID);
                        
                        if (isExternal)
                        {
                            hasExternalConnection = true;
                            break;
                        }
                    }
                }
                
                // Create colored box (green if external connection, red otherwise)
                var indicatorBox = new Rectangle
                {
                    Width = 40,
                    Height = 30,
                    Fill = new SolidColorBrush(hasExternalConnection ? Colors.Green : Colors.Red),
                    IsHitTestVisible = false
                };
                
                // Position the box in the center of the row
                Canvas.SetLeft(indicatorBox, nameWidth + (rowsWidth - 40) / 2);
                Canvas.SetTop(indicatorBox, i * rowHeight + (rowHeight - 30) / 2);
                containerCanvas.Children.Add(indicatorBox);
            }
            
            // Add horizontal separators for each row (except the first one)
            for (int i = 1; i < numberOfRows; i++)
            {
                var separator = new Line
                {
                    X1 = nameWidth,
                    Y1 = i * rowHeight,
                    X2 = totalWidth,
                    Y2 = i * rowHeight,
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = borderThickness,
                    IsHitTestVisible = false
                };
                containerCanvas.Children.Add(separator);
            }
            
            // Position the container on the canvas
            Canvas.SetLeft(containerCanvas, left);
            Canvas.SetTop(containerCanvas, top);
            
            // Remove empty state if it exists
            var emptyState = canvas.Children.OfType<Border>()
                .FirstOrDefault(b => b.Cursor == Cursors.Hand && b.Width == 200);
            if (emptyState != null)
            {
                canvas.Children.Remove(emptyState);
            }
            
            // Add to canvas
            canvas.Children.Add(containerCanvas);
        }
        
        /// <summary>
        ///     Checks if a connection to a block is external to a given location
        /// </summary>
        /// <param name="block">The block to check</param>
        /// <param name="locationId">The location ID to compare against</param>
        /// <returns>True if the block is external to the location, false otherwise</returns>
        private bool IsConnectionExternal(IBlock block, int locationId)
        {
            // Walk up the parent chain to see if we find the location
            var currentBlock = block;
            
            while (currentBlock != null)
            {
                // If we found the location in the parent chain, it's internal
                if (currentBlock.ID == locationId)
                {
                    return false;
                }
                
                // If we've reached a root block (ParentID == -1), stop
                if (currentBlock.ParentID == -1)
                {
                    break;
                }
                
                // Move up to the parent
                try
                {
                    currentBlock = _currentProject.GetBlock(currentBlock.ParentID);
                }
                catch
                {
                    // If we can't get the parent, assume it's external
                    break;
                }
            }
            
            // If we didn't find the location in the parent chain, it's external
            return true;
        }
        
        /// <summary>
        ///     Renders a load block on a location canvas as a hexagon
        /// </summary>
        /// <param name="load">The Load object to render</param>
        /// <param name="canvas">The canvas to render on</param>
        private void RenderLoadOnLocationCanvas(Load load, Canvas canvas)
        {
            const double hexagonSize = 120;
            
            // Get position from Load object (default to origin if not set)
            int x = load.RenderPosition.Item1 ?? 0;
            int y = load.RenderPosition.Item2 ?? 0;
            
            // Convert Cartesian to Canvas coordinates
            Point canvasPos = CartesianToCanvas(canvas, x, y);
            
            // Calculate position so the Load is centered at the point
            double left = canvasPos.X - (hexagonSize / 2);
            double top = canvasPos.Y - (hexagonSize / 2);
            
            // Create main container grid for the load
            var containerGrid = new Grid
            {
                Width = hexagonSize,
                Height = hexagonSize,
                Tag = load,
                Background = new SolidColorBrush(Colors.Transparent),
                Cursor = Cursors.Hand
            };
            
            // Create hexagon shape
            var hexagon = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(hexagonSize * 0.5, 0),                    // Top
                    new Point(hexagonSize, hexagonSize * 0.25),         // Top right
                    new Point(hexagonSize, hexagonSize * 0.75),         // Bottom right
                    new Point(hexagonSize * 0.5, hexagonSize),          // Bottom
                    new Point(0, hexagonSize * 0.75),                   // Bottom left
                    new Point(0, hexagonSize * 0.25)                    // Top left
                },
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 3,
                Fill = new SolidColorBrush(Colors.White)
            };
            
            containerGrid.Children.Add(hexagon);
            
            // Create text label for load name
            var textLabel = new TextBlock
            {
                Text = load.Name,
                FontSize = 14,
                FontWeight = FontWeights.Normal,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10),
                IsHitTestVisible = false // Allow mouse events to pass through
            };
            
            containerGrid.Children.Add(textLabel);
            
            // Add mouse event handlers for selection and dragging
            containerGrid.MouseLeftButtonDown += LoadGrid_MouseLeftButtonDown;
            containerGrid.MouseLeftButtonUp += LoadGrid_MouseLeftButtonUp;
            containerGrid.MouseMove += LoadGrid_MouseLeftButtonMove;
            
            // Position the container on the canvas
            Canvas.SetLeft(containerGrid, left);
            Canvas.SetTop(containerGrid, top);
            
            // Remove empty state if it exists
            var emptyState = canvas.Children.OfType<Border>()
                .FirstOrDefault(b => b.Cursor == Cursors.Hand && b.Width == 200);
            if (emptyState != null)
            {
                canvas.Children.Remove(emptyState);
            }
            
            // Add to canvas
            canvas.Children.Add(containerGrid);
        }

        /// <summary>
        ///     Handles click on the busbar plus button to add a new row
        /// </summary>
        private void BusbarPlusButton_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            
            if (sender is Border border && border.Tag is Busbar busbar)
            {
                // Deselect the busbar before adding a row to avoid issues with re-rendering
                if (_selectedBusbar == busbar)
                {
                    DeselectAll();
                }
                
                // Add a new row to the busbar
                busbar.AddRow();
                
                // Get the canvas this busbar is on
                var canvasTabs = FindName("CanvasTabs") as TabControl;
                if (canvasTabs?.SelectedItem is TabItem selectedTab && selectedTab.Content is Grid canvasContainer)
                {
                    // Find the canvas within the container
                    var locationCanvas = canvasContainer.Children.OfType<Canvas>().FirstOrDefault();
                    
                    if (locationCanvas != null)
                    {
                        // Re-render the busbar
                        var existingBusbar = locationCanvas.Children.OfType<StackPanel>()
                            .FirstOrDefault(sp => sp.Tag is Busbar b && b.ID == busbar.ID);
                        
                        if (existingBusbar != null)
                        {
                            locationCanvas.Children.Remove(existingBusbar);
                        }
                        
                        RenderBusbarOnLocationCanvas(busbar, locationCanvas);
                        
                        // Refresh tree view to show the new row
                        PopulateTreeView();
                    }
                }
            }
        }
    }
}
