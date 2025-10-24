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
    /// Generic drag handling utilities for blocks
    /// Phase 4: Unified drag handling using IInteractiveBlock interface
    /// Updated: Added grid snapping to match connection line grid (20px)
    /// Updated: Fixed drag jump issue by preserving initial grab offset
    /// </summary>
    public partial class MainWindow
    {
        // Grid size constant - matches connection line grid
        private const double BlockGridSize = 20.0;

        /// <summary>
        /// Unified drag handler that uses IInteractiveBlock interface
        /// Phase 4: Truly generic implementation
        /// Updated: Added grid snapping support
        /// Updated: Use mouse offset to prevent jump on drag start
        /// </summary>
        private void HandleBlockDrag(IBlock? block, MouseEventArgs e, Canvas canvas)
        {
            if (block is not IInteractiveBlock interactive) return;
            
            var selectedElement = GetSelectedElementForBlock(block);
            if (selectedElement == null) return;
            
            var (width, height) = interactive.GetRenderDimensions();
            
            Point currentMousePosition = e.GetPosition(canvas);
            Point mouseOffset = GetMouseOffsetForBlock(block);
            
            // Calculate new position using mouse position and offset
            // The offset is where the mouse was clicked within the element
            double newLeft = currentMousePosition.X - mouseOffset.X;
            double newTop = currentMousePosition.Y - mouseOffset.Y;
            
            // Store old position for special post-drag updates
            double oldLeft = Canvas.GetLeft(selectedElement);
            double oldTop = Canvas.GetTop(selectedElement);
            Point oldCenter = new Point(oldLeft + width / 2, oldTop + height / 2);
            
            // Snap the CENTER point to grid, then calculate top-left from that
            double centerX = newLeft + (width / 2);
            double centerY = newTop + (height / 2);
            
            // Snap center to grid
            double snappedCenterX = Math.Round(centerX / BlockGridSize) * BlockGridSize;
            double snappedCenterY = Math.Round(centerY / BlockGridSize) * BlockGridSize;
            
            // Calculate snapped top-left position from snapped center
            newLeft = snappedCenterX - (width / 2);
            newTop = snappedCenterY - (height / 2);
            
            // Apply snapped position
            Canvas.SetLeft(selectedElement, newLeft);
            Canvas.SetTop(selectedElement, newTop);
            
            // Update selection element if it exists
            var selectionElement = GetSelectionElementForBlock(block);
            if (selectionElement != null)
            {
                const double selectionOffset = 15;
                Canvas.SetLeft(selectionElement, newLeft - selectionOffset);
                Canvas.SetTop(selectionElement, newTop - selectionOffset);
            }
            
            // Allow block-specific post-drag updates (e.g., alternator text block)
            Point newCenter = new Point(snappedCenterX, snappedCenterY);
            PerformBlockSpecificDragUpdate(block, selectedElement, canvas, oldCenter, newCenter);
            
            // Update connection dots and lines
            UpdateConnectionDotsForBlock(block);
            RenderConnectionLines();
        }

        /// <summary>
        /// Performs block-specific updates after dragging (e.g., alternator text block positioning)
        /// </summary>
        private void PerformBlockSpecificDragUpdate(IBlock block, FrameworkElement element, Canvas canvas, Point oldCenter, Point newCenter)
        {
            // Special case: Alternator text block needs to move with the polygon
            if (block is AlternatorBlock)
            {
                const double diamondSize = 150;
                double oldLeft = oldCenter.X - (diamondSize / 2);
                double oldTop = oldCenter.Y - (diamondSize / 2);
                
                UpdateTextBlockPosition(canvas, oldLeft, oldTop, newCenter.X, newCenter.Y, diamondSize);
            }
            // Special case: Supply text block needs to move with the ellipse
            else if (block is Supply)
            {
                const double supplyDiameter = 150;
                double oldLeft = oldCenter.X - (supplyDiameter / 2);
                double oldTop = oldCenter.Y - (supplyDiameter / 2);
                
                UpdateTextBlockPosition(canvas, oldLeft, oldTop, newCenter.X, newCenter.Y, supplyDiameter);
            }
        }

        /// <summary>
        /// Gets the mouse offset for a specific block type
        /// </summary>
        private Point GetMouseOffsetForBlock(IBlock block)
        {
            return block switch
            {
                Location => _locationDragMouseOffset,
                Supply => _supplyDragMouseOffset,
                AlternatorBlock => _alternatorDragMouseOffset,
                ConductorBlock => _conductorDragMouseOffset,
                TransformerUPSBlock => _transformerUPSDragMouseOffset,
                Busbar => _busbarDragMouseOffset,
                Load => _loadDragMouseOffset,
                ExternalBusbar => _externalBusbarDragMouseOffset,
                _ => new Point()
            };
        }

        /// <summary>
        /// Gets the selection UI element for a specific block
        /// </summary>
        private UIElement? GetSelectionElementForBlock(IBlock block)
        {
            return block switch
            {
                Location => _selectionRectangle?.Tag as Rectangle,
                Supply => _supplySelectionRectangle?.Tag as Ellipse,
                AlternatorBlock => _alternatorSelectionRectangle?.Tag as Polygon,
                ConductorBlock => _conductorSelectionRectangle,
                TransformerUPSBlock => _transformerUPSSelectionRectangle,
                Busbar => _busbarSelectionRectangle,
                Load => _loadSelectionRectangle,
                ExternalBusbar => _externalBusbarSelectionRectangle,
                _ => null
            };
        }

        // ========== SPECIFIC DRAG HANDLERS (now simplified wrappers) ==========

        /// <summary>
        /// Handles dragging of a location block
        /// </summary>
        private void HandleLocationDrag(MouseEventArgs e) 
            => HandleBlockDrag(_selectedLocation, e, DiagramCanvas);

        /// <summary>
        /// Handles dragging of a supply block
        /// </summary>
        private void HandleSupplyDrag(MouseEventArgs e) 
            => HandleBlockDrag(_selectedSupply, e, DiagramCanvas);

        /// <summary>
        /// Handles dragging of an alternator block
        /// </summary>
        private void HandleAlternatorDrag(MouseEventArgs e) 
            => HandleBlockDrag(_selectedAlternator, e, DiagramCanvas);

        /// <summary>
        /// Handles dragging of a conductor block
        /// </summary>
        private void HandleConductorDrag(MouseEventArgs e) 
            => HandleBlockDrag(_selectedConductor, e, DiagramCanvas);

        /// <summary>
        /// Handles dragging of a busbar block
        /// </summary>
        private void HandleBusbarDrag(MouseEventArgs e, Canvas canvas) 
            => HandleBlockDrag(_selectedBusbar, e, canvas);

        /// <summary>
        /// Handles dragging of a TransformerUPS block
        /// </summary>
        private void HandleTransformerUPSDrag(MouseEventArgs e, Canvas canvas) 
            => HandleBlockDrag(_selectedTransformerUPS, e, canvas);

        /// <summary>
        /// Handles dragging of a Load block
        /// </summary>
        private void HandleLoadDrag(MouseEventArgs e, Canvas canvas) 
            => HandleBlockDrag(_selectedLoad, e, canvas);

        /// <summary>
        /// Handles dragging of an ExternalBusbar block
        /// </summary>
        private void HandleExternalBusbarDrag(MouseEventArgs e, Canvas canvas) 
            => HandleBlockDrag(_selectedExternalBusbar, e, canvas);

        /// <summary>
        /// Updates the text block position for a block element during dragging
        /// </summary>
        /// <param name="canvas">The canvas containing the text block</param>
        /// <param name="oldLeft">The old left position of the block</param>
        /// <param name="oldTop">The old top position of the block</param>
        /// <param name="newCenterX">The new center X position</param>
        /// <param name="newCenterY">The new center Y position</param>
        /// <param name="blockSize">The size of the block</param>
        private void UpdateTextBlockPosition(Canvas canvas, double oldLeft, double oldTop, double newCenterX, double newCenterY, double blockSize)
        {
            // Text blocks are positioned at center - 60 for X and center - 10 for Y
            double expectedOldTextLeft = oldLeft + blockSize / 2 - 60;
            double expectedOldTextTop = oldTop + blockSize / 2 - 10;
            
            // Find and update the text block
            foreach (var child in canvas.Children)
            {
                if (child is TextBlock tb && !tb.IsHitTestVisible)
                {
                    // Check if this text block is near the block
                    var tbLeft = Canvas.GetLeft(tb);
                    var tbTop = Canvas.GetTop(tb);
                    if (Math.Abs(tbLeft - expectedOldTextLeft) < 5 && Math.Abs(tbTop - expectedOldTextTop) < 5)
                    {
                        Canvas.SetLeft(tb, newCenterX - 60);
                        Canvas.SetTop(tb, newCenterY - 10);
                        break;
                    }
                }
            }
        }
    }
}
