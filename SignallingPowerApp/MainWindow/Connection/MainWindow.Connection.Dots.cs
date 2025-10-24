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
    /// Connection dot positioning and event handlers
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        ///     Renders connection dots on all connectable blocks
        /// </summary>
        private void RenderConnectionDots()
        {
            if (_currentProject == null) return;

            // Clear any existing connection dots first to prevent duplicates
            ClearConnectionDots();

            const double dotSize = 12;
            const double dotOffset = 6; // Half of dotSize for centering

            foreach (var block in _currentProject.GetAllBlocks)
            {
                RenderBlockConnectionDots(block, dotSize, dotOffset);
            }
        }



        /// <summary>
        /// Connection dot rendering using IInteractiveBlock interface
        /// </summary>
        private void RenderBlockConnectionDots(IBlock block, double dotSize, double dotOffset)
        {
            if (block is IInteractiveBlock interactive)
            {
                // Determine target canvas from interface
                Canvas? targetCanvas = interactive.PreferredCanvas switch
                {
                    CanvasType.Layout => DiagramCanvas,
                    CanvasType.Location => GetLocationCanvasForBlock(block),
                    _ => DiagramCanvas
                };

                if (targetCanvas == null) return;

                // Get block position (convert to canvas coordinates)
                Point canvasPos = GetBlockCanvasPosition(block, targetCanvas);

                // Get dot positions from the block itself - NO type switching!
                var dotInfos = interactive.GetConnectionDotPositions(dotOffset);

                // Create dots generically
                foreach (var dotInfo in dotInfos)
                {
                    double dotX = canvasPos.X + dotInfo.RelativeX - dotOffset;
                    double dotY = canvasPos.Y + dotInfo.RelativeY - dotOffset;

                    var dot = CreateConnectionDot(dotX, dotY, dotSize, dotInfo.TerminalId);
                    dot.Tag = dotInfo.Tag;

                    _connectionDots.Add(dot);
                    targetCanvas.Children.Add(dot);
                }
            }
        }

        // GetLocationCanvasForBlock has been moved to MainWindow.Helpers.Canvas.cs

        /// <summary>
        /// Gets the canvas position for a block (handles both Layout and Location canvases)
        /// </summary>
        private Point GetBlockCanvasPosition(IBlock block, Canvas targetCanvas)
        {
            if (block is not IInteractiveBlock interactive)
                return new Point(0, 0);
            
            var (width, height) = interactive.GetRenderDimensions();
            
            // Check if currently being dragged - use live UI position
            FrameworkElement? selectedElement = GetSelectedElementForBlock(block);
            
            if (selectedElement != null && IsBlockBeingDragged(block))
            {
                // Block is being dragged - use live UI position
                double left = Canvas.GetLeft(selectedElement);
                double top = Canvas.GetTop(selectedElement);
                double centerX = left + (width / 2);
                double centerY = top + (height / 2);
                return new Point(centerX, centerY);
            }
            
            // For Rows, we need to calculate their position within the parent Busbar
            if (block is Row row)
            {
                // Get the parent busbar
                var parentBusbar = _currentProject.GetBlock(row.ParentID) as Busbar;
                if (parentBusbar != null)
                {
                    Point busbarCanvasPos;
                    
                    // Check if the parent busbar is being dragged
                    if (_isDraggingBusbar && _selectedBusbar == parentBusbar && _selectedBusbarContainer != null)
                    {
                        // Parent busbar is being dragged - use live UI position
                        var busbarDimensions = parentBusbar.GetRenderDimensions();
                        double busbarLeft = Canvas.GetLeft(_selectedBusbarContainer);
                        double busbarTop = Canvas.GetTop(_selectedBusbarContainer);
                        double busbarCenterX = busbarLeft + (busbarDimensions.width / 2);
                        double busbarCenterY = busbarTop + (busbarDimensions.height / 2);
                        busbarCanvasPos = new Point(busbarCenterX, busbarCenterY);
                    }
                    else
                    {
                        // Busbar is not being dragged - use stored position
                        var busbarRenderPos = GetRenderPositionViaReflection(parentBusbar);
                        int busbarX = busbarRenderPos.Item1 ?? 0;
                        int busbarY = busbarRenderPos.Item2 ?? 0;
                        busbarCanvasPos = CartesianToCanvas(targetCanvas, busbarX, busbarY);
                    }
                    
                    // Get busbar dimensions from constants
                    var allRows = parentBusbar.GetRows().ToList();
                    double busbarContentHeight = Busbar.NameHeight + (allRows.Count * Busbar.RowHeight);
                    double totalBusbarHeight = busbarContentHeight + Busbar.PlusButtonSize + Busbar.PlusButtonGap;
                    
                    // Find the index of this row in the busbar
                    int rowIndex = allRows.FindIndex(r => r.ID == row.ID);
                    
                    if (rowIndex >= 0)
                    {
                        // Calculate the row's Y offset from the busbar top
                        // Busbar top is at: busbarCanvasPos.Y - (totalBusbarHeight / 2)
                        // Row is at: nameHeight + (rowIndex * rowHeight) + (rowHeight / 2)
                        double rowOffsetFromBusbarTop = Busbar.NameHeight + (rowIndex * Busbar.RowHeight) + (Busbar.RowHeight / 2);
                        double busbarTopY = busbarCanvasPos.Y - (totalBusbarHeight / 2);
                        double rowCenterY = busbarTopY + rowOffsetFromBusbarTop;
                        
                        // Row X is same as busbar X
                        return new Point(busbarCanvasPos.X, rowCenterY);
                    }
                }
            }
            
            // Not being dragged and not a row - use stored RenderPosition
            var renderPos = GetRenderPositionViaReflection(block);
            int x = renderPos.Item1 ?? 0;
            int y = renderPos.Item2 ?? 0;
            
            // Convert Cartesian to canvas coordinates
            return CartesianToCanvas(targetCanvas, x, y);
        }

        /// <summary>
        /// Helper method to check if block is currently being dragged
        /// </summary>
        private bool IsBlockBeingDragged(IBlock block)
        {
            return block switch
            {
                Location loc => _isDraggingLocation && _selectedLocation == loc,
                Supply sup => _isDraggingSupply && _selectedSupply == sup,
                AlternatorBlock alt => _isDraggingAlternator && _selectedAlternator == alt,
                ConductorBlock cond => _isDraggingConductor && _selectedConductor == cond,
                TransformerUPSBlock tfmr => _isDraggingTransformerUPS && _selectedTransformerUPS == tfmr,
                Busbar bus => _isDraggingBusbar && _selectedBusbar == bus,
                Load load => _isDraggingLoad && _selectedLoad == load,
                ExternalBusbar eb => _isDraggingExternalBusbar && _selectedExternalBusbar == eb,
                _ => false
            };
        }

        /// <summary>
        /// Helper method to get the selected UI element for a block
        /// </summary>
        private FrameworkElement? GetSelectedElementForBlock(IBlock block)
        {
            return block switch
            {
                Location loc when _selectedLocation == loc => _selectedLocationBorder,
                Supply sup when _selectedSupply == sup => _selectedSupplyEllipse,
                AlternatorBlock alt when _selectedAlternator == alt => _selectedAlternatorPolygon,
                ConductorBlock cond when _selectedConductor == cond => _selectedConductorBorder,
                TransformerUPSBlock tfmr when _selectedTransformerUPS == tfmr => _selectedTransformerUPSGrid,
                Busbar bus when _selectedBusbar == bus => _selectedBusbarContainer,
                Load load when _selectedLoad == load => _selectedLoadGrid,
                ExternalBusbar eb when _selectedExternalBusbar == eb => _selectedExternalBusbarContainer,
                _ => null
            };
        }

        /// <summary>
        /// Helper to get RenderPosition via reflection
        /// </summary>
        private (int?, int?) GetRenderPositionViaReflection(IBlock block)
        {
            var prop = block.GetType().GetProperty("RenderPosition");
            if (prop != null)
            {
                try
                {
                    var val = prop.GetValue(block);
                    if (val != null)
                    {
                        try
                        {
                            return ((int?, int?))val;
                        }
                        catch
                        {
                            // If direct cast fails, ignore and use default
                        }
                    }
                }
                catch
                {
                    // ignore and use default
                }
            }
            return (null, null);
        }

        /// <summary>
        ///     Creates a connection dot ellipse
        /// </summary>
        private Ellipse CreateConnectionDot(double left, double top, double size, int blockId)
        {
            var dot = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(Colors.Magenta),
                Stroke = new SolidColorBrush(Colors.DarkMagenta),
                StrokeThickness = 2,
                Cursor = Cursors.Hand,
                Tag = blockId // Store the block ID in the Tag property
            };

            Canvas.SetLeft(dot, left);
            Canvas.SetTop(dot, top);
            Canvas.SetZIndex(dot, 1000); // Ensure dots are above other elements

            // Add click handler
            dot.MouseLeftButtonDown += ConnectionDot_MouseLeftButtonDown;

            return dot;
        }

        /// <summary>
        ///     Handles click on a connection dot
        /// </summary>
        private void ConnectionDot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (sender is not Ellipse dot)
                return;

            // Extract block ID from tag
            int blockId = ExtractBlockIdFromDotTag(dot.Tag);
            if (blockId == -1) return;

            if (_firstSelectedTerminalId == null)
            {
                // First dot selected
                _firstSelectedTerminalId = blockId;
                _firstSelectedDot = dot;

                // Highlight the selected dot
                dot.Fill = new SolidColorBrush(Colors.Yellow);
                dot.Stroke = new SolidColorBrush(Colors.Orange);
                dot.StrokeThickness = 3;
            }
            else
            {
                // Second dot selected - create connection
                if (_firstSelectedTerminalId == blockId)
                {
                    // Same dot clicked twice - deselect
                    ResetFirstSelectedDot();
                    return;
                }

                try
                {
                    // Attempt to create connection
                    _currentProject?.AddConnection((int)_firstSelectedTerminalId, blockId);

                    // Mark project as modified
                    MarkAsModified();

                    // Reset selection but stay in connection edit mode
                    ResetFirstSelectedDot();

                    // Refresh display without exiting edit mode
                    // IMPORTANT: Order matters here!
                    // 1. RefreshAllLocationCanvases() clears and re-renders location canvases (including connection lines)
                    // 2. RenderConnectionLines() renders lines on layout canvas
                    // 3. RenderConnectionDots() renders dots on ALL canvases (must be last because RefreshAllLocationCanvases clears canvases)
                    
                    RefreshAllLocationCanvases(); // Clears location canvases, re-renders blocks and connection lines
                    RenderConnectionLines(DiagramCanvas); // Render lines on layout canvas
                    RenderConnectionDots(); // Render dots on all canvases (must be after RefreshAllLocationCanvases)
                    
                    PopulateTreeView();
                }
                catch (Exception ex)
                {
                    // Failed to create connection - show error and reset
                    MessageBox.Show($"Failed to create connection: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    ResetFirstSelectedDot();
                }
            }
        }

        /// <summary>
        ///     Resets the first selected dot to unselected state
        /// </summary>
        private void ResetFirstSelectedDot()
        {
            if (_firstSelectedDot != null)
            {
                _firstSelectedDot.Fill = new SolidColorBrush(Colors.Magenta);
                _firstSelectedDot.Stroke = new SolidColorBrush(Colors.DarkMagenta);
                _firstSelectedDot.StrokeThickness = 2;
            }

            _firstSelectedTerminalId = null;
            _firstSelectedDot = null;
        }

        /// <summary>
        /// Updates connection dots for any block during dragging (Phase 3 - Generic)
        /// </summary>
        private void UpdateConnectionDotsForBlock(IBlock? block)
        {
            if (block == null) return;

            if (block is Busbar busbar)
            {
                // Update dots for each row in the busbar
                var rows = busbar.GetRows();
                foreach (var row in rows)
                {
                    UpdateConnectionDotsForBlock(row);
                }
                return;
            }

            if (block is not IInteractiveBlock interactive) return;

            const double dotOffset = 6;

            // Determine target canvas
            Canvas? targetCanvas = interactive.PreferredCanvas switch
            {
                CanvasType.Layout => DiagramCanvas,
                CanvasType.Location => GetLocationCanvasForBlock(block),
                _ => null
            };

            if (targetCanvas == null) return;

            // Get current block position (during drag)
            Point canvasPos = GetBlockCanvasPosition(block, targetCanvas);

            // Get dot positions from the block itself
            var dotInfos = interactive.GetConnectionDotPositions(dotOffset);

            // Update each dot
            foreach (var dotInfo in dotInfos)
            {
                // Find the dot with this terminal ID
                foreach (var child in _connectionDots)
                {
                    if (child is Ellipse dot)
                    {
                        int dotTerminalId = ExtractBlockIdFromDotTag(dot.Tag);

                        if (dotTerminalId == dotInfo.TerminalId)
                        {
                            double dotX = canvasPos.X + dotInfo.RelativeX - dotOffset;
                            double dotY = canvasPos.Y + dotInfo.RelativeY - dotOffset;

                            Canvas.SetLeft(dot, dotX);
                            Canvas.SetTop(dot, dotY);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Extracts block ID from connection dot tag (handles both simple and complex tags)
        /// </summary>
        private int ExtractBlockIdFromDotTag(object? tag)
        {
            if (tag is int simpleId)
            {
                return simpleId;
            }
            else if (tag != null)
            {
                var tagType = tag.GetType();
                var blockIdProperty = tagType.GetProperty("BlockId");
                if (blockIdProperty != null)
                {
                    return (int)blockIdProperty.GetValue(tag)!;
                }

                var loadIdProperty = tagType.GetProperty("LoadId");
                if (loadIdProperty != null)
                {
                    return (int)loadIdProperty.GetValue(tag)!;
                }
            }
            return -1;
        }

        /// <summary>
        ///     Extracts position from connection dot tag for terminal blocks
        /// </summary>
        private int ExtractPositionFromDotTag(object tag, IBlock block)
        {
            var tagType = tag.GetType();

            string? blockIdPropertyName = block switch
            {
                ConductorBlock => "ConductorId",
                TransformerUPSBlock => "TransformerUPSId",
                Row => "RowId",
                ExternalBusbar => "ExternalBusbarId",
                _ => null
            };

            if (blockIdPropertyName == null) return -1;

            var blockIdProp = tagType.GetProperty(blockIdPropertyName);
            var positionProp = tagType.GetProperty("Position");

            if (blockIdProp != null && positionProp != null)
            {
                dynamic tagData = tag;
                var tagBlockId = (int)blockIdProp.GetValue(tagData);
                if (tagBlockId == block.ID)
                    return (int)tagData.Position;
            }

            return -1;
        }

        /// <summary>
        /// Gets the canvas position for a connection dot based on the block ID
        /// </summary>
        /// <param name="blockId">The block ID to get the position for</param>
        /// <param name="dotSize">Size of the connection dot</param>
        /// <param name="dotOffset">Offset for centering the dot</param>
        /// <param name="targetCanvas">The canvas we're rendering on</param>
        /// <returns>The position point, or null if not found or on wrong canvas</returns>
        private Point? GetConnectionDotPosition(int blockId, double dotSize, double dotOffset, Canvas? targetCanvas = null)
        {
            if (_currentProject == null) return null;

            IBlock block;
            try
            {
                block = _currentProject.GetBlock(blockId);
            }
            catch
            {
                return null;
            }

            // If this is a terminal, we need to ask its parent interactive block for dot positions
            if (block is Terminal terminal)
            {
                IBlock parentBlock;
                try
                {
                    parentBlock = _currentProject.GetBlock(terminal.ParentID);
                }
                catch
                {
                    return null;
                }

                if (parentBlock is IInteractiveBlock parentInteractive)
                {
                    // Determine the canvas the parent wants to render on
                    Canvas? canvas = parentInteractive.PreferredCanvas switch
                    {
                        CanvasType.Layout => DiagramCanvas,
                        CanvasType.Location => GetLocationCanvasForBlock(parentBlock),
                        _ => null
                    };

                    if (canvas == null) return null;
                    if (targetCanvas != null && targetCanvas != canvas) return null;

                    // Get parent center on that canvas
                    Point canvasPos = GetBlockCanvasPosition(parentBlock, canvas);

                    // Ask parent for its dot layout and find the dot for this terminal
                    var dotInfos = parentInteractive.GetConnectionDotPositions(dotOffset);
                    var dotInfo = dotInfos.FirstOrDefault(d => d.TerminalId == terminal.ID);
                    if (dotInfo.Equals(default(ConnectionDotInfo))) return null;

                    double dotX = canvasPos.X + dotInfo.RelativeX - dotOffset;
                    double dotY = canvasPos.Y + dotInfo.RelativeY - dotOffset;

                    return new Point(dotX, dotY);
                }

                // If parent is not interactive, give up
                return null;
            }

            // If block itself is interactive (e.g., Supply, AlternatorBlock, Load, ConductorBlock etc.)
            if (block is IInteractiveBlock interactive)
            {
                Canvas? canvas = interactive.PreferredCanvas switch
                {
                    CanvasType.Layout => DiagramCanvas,
                    CanvasType.Location => GetLocationCanvasForBlock(block),
                    _ => null
                };

                if (canvas == null) return null;
                if (targetCanvas != null && targetCanvas != canvas) return null;

                Point canvasPos = GetBlockCanvasPosition(block, canvas);

                var dotInfos = interactive.GetConnectionDotPositions(dotOffset);
                // Find dot matching this block (some blocks use their own ID as the terminal id)
                var dotInfo = dotInfos.FirstOrDefault(d => d.TerminalId == block.ID);
                if (dotInfo.Equals(default(ConnectionDotInfo)))
                {
                    // If no direct match, and there is only one dot defined, use it
                    if (dotInfos.Length == 1)
                    {
                        dotInfo = dotInfos[0];
                    }
                    else
                    {
                        return null;
                    }
                }

                double dotX = canvasPos.X + dotInfo.RelativeX - dotOffset;
                double dotY = canvasPos.Y + dotInfo.RelativeY - dotOffset;

                return new Point(dotX, dotY);
            }

            return null;
        }

        /// <summary>
        /// Helper to safely get a position from an array with bounds checking
        /// </summary>
        private Point? GetPositionFromArray((double, double)[] positions, int index)
        {
            if (index >= 0 && index < positions.Length)
            {
                return new Point(positions[index].Item1, positions[index].Item2);
            }
            return null;
        }
    }
}
