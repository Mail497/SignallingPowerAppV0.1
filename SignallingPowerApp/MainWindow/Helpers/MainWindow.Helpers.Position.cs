using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using SignallingPowerApp.Core;

namespace SignallingPowerApp
{
    /// <summary>
    /// Generic position update utilities for blocks
    /// Phase 5: Uses IInteractiveBlock.GetRenderDimensions() to eliminate hardcoded dimensions
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Updates a block's render position based on its UI element position
        /// Phase 5: Simplified using IInteractiveBlock interface
        /// </summary>
        /// <param name="block">The block to update (must implement IInteractiveBlock)</param>
        /// <param name="element">The UI element representing the block</param>
        /// <param name="canvas">Optional canvas (if null, will find parent canvas)</param>
        private void UpdateBlockRenderPosition(IBlock block, FrameworkElement element, Canvas? canvas = null)
        {
            if (block is not IInteractiveBlock interactive) return;
            
            canvas ??= FindParentCanvas(element);
            if (canvas == null) return;
            
            var (width, height) = interactive.GetRenderDimensions();
            
            // Get the current canvas position (top-left corner)
            double left = Canvas.GetLeft(element);
            double top = Canvas.GetTop(element);
            
            // Calculate the center point
            double centerX = left + (width / 2);
            double centerY = top + (height / 2);
            
            // Convert to Cartesian coordinates
            var (x, y) = CanvasToCartesian(canvas, centerX, centerY);
            
            // Update the block's render position via reflection
            var prop = block.GetType().GetProperty("RenderPosition");
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(block, ((int?)x, (int?)y));
            }
        }

        // ========== SPECIFIC UPDATE METHODS (now simplified wrappers) ==========

        /// <summary>
        /// Updates a conductor's render position based on its border position on the canvas
        /// </summary>
        private void UpdateConductorRenderPosition(ConductorBlock conductor, Border border)
            => UpdateBlockRenderPosition(conductor, border);

        /// <summary>
        /// Updates a location's render position based on its border position on the canvas
        /// </summary>
        private void UpdateLocationRenderPosition(Location location, Border border)
            => UpdateBlockRenderPosition(location, border);

        /// <summary>
        /// Updates a supply's render position based on its ellipse position on the canvas
        /// </summary>
        private void UpdateSupplyRenderPosition(Supply supply, Ellipse ellipse)
            => UpdateBlockRenderPosition(supply, ellipse);

        /// <summary>
        /// Updates an alternator's render position based on its polygon position on the canvas
        /// </summary>
        private void UpdateAlternatorRenderPosition(AlternatorBlock alternator, Polygon polygon)
            => UpdateBlockRenderPosition(alternator, polygon);

        /// <summary>
        /// Updates a busbar's render position based on its container position on the canvas
        /// </summary>
        private void UpdateBusbarRenderPosition(Busbar busbar, StackPanel container)
        {
            var canvas = FindParentCanvas(container);
            if (canvas == null) return;
            
            UpdateBlockRenderPosition(busbar, container, canvas);
        }

        /// <summary>
        /// Updates a TransformerUPS's render position based on its grid position on the canvas
        /// </summary>
        private void UpdateTransformerUPSRenderPosition(TransformerUPSBlock transformerUPS, Grid grid)
        {
            var canvas = FindParentCanvas(grid);
            if (canvas == null) return;
            
            UpdateBlockRenderPosition(transformerUPS, grid, canvas);
        }

        /// <summary>
        /// Updates a Load's render position based on its grid position on the canvas
        /// </summary>
        private void UpdateLoadRenderPosition(Load load, Grid grid)
        {
            var canvas = FindParentCanvas(grid);
            if (canvas == null) return;
            
            UpdateBlockRenderPosition(load, grid, canvas);
        }

        /// <summary>
        /// Updates an ExternalBusbar's render position based on its canvas position
        /// </summary>
        private void UpdateExternalBusbarRenderPosition(ExternalBusbar externalBusbar, Canvas containerCanvas)
        {
            var canvas = FindParentCanvas(containerCanvas);
            if (canvas == null) return;
            
            UpdateBlockRenderPosition(externalBusbar, containerCanvas, canvas);
        }
    }
}
