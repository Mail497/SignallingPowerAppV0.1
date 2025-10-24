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
    /// Mouse event handlers for rendered canvas elements
    /// </summary>
    public partial class MainWindow
    {
        // ========== CONDUCTOR MOUSE HANDLERS ==========
        
        /// <summary>
        ///     Handles mouse click on a Conductor border
        /// </summary>
        private void ConductorBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is ConductorBlock conductor)
            {
                // Prevent panning when clicking on conductor
                e.Handled = true;
                
                // If this conductor is already selected, start dragging
                if (_selectedConductor == conductor)
                {
                    _isDraggingConductor = true;
                    // Store the current element position, not the mouse position
                    // The drag handler will calculate the offset from mouse to element
                    _conductorDragStartPoint = new Point(Canvas.GetLeft(border), Canvas.GetTop(border));
                    _conductorDragMouseOffset = new Point(
                        e.GetPosition(border).X,
                        e.GetPosition(border).Y
                    );
                    border.CaptureMouse();
                    border.Cursor = Cursors.SizeAll;
                }
                else
                {
                    // Just select the conductor, don't start dragging
                    SelectConductor(conductor, border);
                }
            }
        }

        /// <summary>
        ///     Handles mouse button up on a Conductor border
        /// </summary>
        private void ConductorBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && _isDraggingConductor)
            {
                // IMPORTANT: Handle event FIRST to prevent CanvasContainer handler from processing it
                e.Handled = true;
                
                _isDraggingConductor = false;
                border.ReleaseMouseCapture();
                border.Cursor = Cursors.Hand;
                
                // Update the conductor's render position
                if (border.Tag is ConductorBlock conductor)
                {
                    UpdateConductorRenderPosition(conductor, border);
                }
            }
        }

        /// <summary>
        ///     Handles mouse move on a Conductor border for dragging
        /// </summary>
        private void ConductorBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingConductor && sender is Border border && border == _selectedConductorBorder)
            {
                HandleConductorDrag(e);
                e.Handled = true;
            }
        }

        // ========== ALTERNATOR MOUSE HANDLERS ==========

        /// <summary>
        ///     Handles mouse click on an Alternator polygon
        /// </summary>
        private void AlternatorPolygon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Polygon polygon && polygon.Tag is AlternatorBlock alternator)
            {
                // Prevent panning when clicking on alternator
                e.Handled = true;
                
                // If this alternator is already selected, start dragging
                if (_selectedAlternator == alternator)
                {
                    _isDraggingAlternator = true;
                    // Store the current element position, not the mouse position
                    _alternatorDragStartPoint = new Point(Canvas.GetLeft(polygon), Canvas.GetTop(polygon));
                    _alternatorDragMouseOffset = new Point(
                        e.GetPosition(polygon).X,
                        e.GetPosition(polygon).Y
                    );
                    polygon.CaptureMouse();
                    polygon.Cursor = Cursors.SizeAll;
                }
                else
                {
                    // Just select the alternator, don't start dragging
                    SelectAlternator(alternator, polygon);
                }
            }
        }

        /// <summary>
        ///     Handles mouse button up on an Alternator polygon
        /// </summary>
        private void AlternatorPolygon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Polygon polygon && _isDraggingAlternator)
            {
                // IMPORTANT: Handle event FIRST to prevent CanvasContainer handler from processing it
                e.Handled = true;
                
                _isDraggingAlternator = false;
                polygon.ReleaseMouseCapture();
                polygon.Cursor = Cursors.Hand;
                
                // Update the alternator's render position
                if (polygon.Tag is AlternatorBlock alternator)
                {
                    UpdateAlternatorRenderPosition(alternator, polygon);
                }
            }
        }

        /// <summary>
        ///     Handles mouse move on an Alternator polygon for dragging
        /// </summary>
        private void AlternatorPolygon_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingAlternator && sender is Polygon polygon && polygon == _selectedAlternatorPolygon)
            {
                HandleAlternatorDrag(e);
                e.Handled = true;
            }
        }

        // ========== BUSBAR MOUSE HANDLERS ==========

        /// <summary>
        ///     Handles mouse click on busbar name area
        /// </summary>
        private void BusbarNameArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is Busbar busbar)
            {
                // Prevent panning when clicking on busbar
                e.Handled = true;
                
                // Find the container StackPanel
                var containerStack = FindParentOfType<StackPanel>(border);
                
                // If this busbar is already selected, start dragging
                if (_selectedBusbar == busbar && containerStack != null)
                {
                    _isDraggingBusbar = true;
                    
                    // Get the canvas this busbar is on
                    var canvas = FindParentCanvas(border);
                    if (canvas != null)
                    {
                        // Store the current element position, not the mouse position
                        _busbarDragStartPoint = new Point(Canvas.GetLeft(containerStack), Canvas.GetTop(containerStack));
                        _busbarDragMouseOffset = new Point(
                            e.GetPosition(containerStack).X,
                            e.GetPosition(containerStack).Y
                        );
                        border.CaptureMouse(); // Capture on the border, not the container
                        border.Cursor = Cursors.SizeAll;
                    }
                }
                else
                {
                    // Just select the busbar, don't start dragging
                    SelectBusbar(busbar, containerStack);
                }
            }
        }
        
        /// <summary>
        ///     Handles mouse button up on busbar name area
        /// </summary>
        private void BusbarNameArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && _isDraggingBusbar)
            {
                // IMPORTANT: Handle event FIRST to prevent CanvasContainer handler from processing it
                e.Handled = true;
                
                _isDraggingBusbar = false;
                border.ReleaseMouseCapture();
                border.Cursor = Cursors.Hand;
                
                // Update the busbar's render position
                if (border.Tag is Busbar busbar && _selectedBusbarContainer != null)
                {
                    UpdateBusbarRenderPosition(busbar, _selectedBusbarContainer);
                }
            }
        }
        
        /// <summary>
        ///     Handles mouse move on busbar name area for dragging
        /// </summary>
        private void BusbarNameArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingBusbar && sender is Border border && _selectedBusbarContainer != null)
            {
                var canvas = FindParentCanvas(border);
                if (canvas == null) return;
                
                HandleBusbarDrag(e, canvas);
                
                e.Handled = true;
            }
        }

        // ========== TRANSFORMERUPS MOUSE HANDLERS ==========

        /// <summary>
        ///     Handles mouse click on TransformerUPS grid
        /// </summary>
        private void TransformerUPSGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid grid && grid.Tag is TransformerUPSBlock transformerUPS)
            {
                // Prevent panning when clicking on transformer
                e.Handled = true;
                
                // If this transformer is already selected, start dragging
                if (_selectedTransformerUPS == transformerUPS)
                {
                    _isDraggingTransformerUPS = true;
                    
                    // Get the canvas this transformer is on
                    var canvas = FindParentCanvas(grid);
                    if (canvas != null)
                    {
                        // Store the current element position, not the mouse position
                        _transformerUPSDragStartPoint = new Point(Canvas.GetLeft(grid), Canvas.GetTop(grid));
                        _transformerUPSDragMouseOffset = new Point(
                            e.GetPosition(grid).X,
                            e.GetPosition(grid).Y
                        );
                        grid.CaptureMouse();
                        grid.Cursor = Cursors.SizeAll;
                    }
                }
                else
                {
                    // Just select the transformer, don't start dragging
                    SelectTransformerUPS(transformerUPS, grid);
                }
            }
        }
        
        /// <summary>
        ///     Handles mouse button up on TransformerUPS grid
        /// </summary>
        private void TransformerUPSGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid grid && _isDraggingTransformerUPS)
            {
                // IMPORTANT: Handle event FIRST to prevent CanvasContainer handler from processing it
                e.Handled = true;
                
                _isDraggingTransformerUPS = false;
                grid.ReleaseMouseCapture();
                grid.Cursor = Cursors.Hand;
                
                // Update the transformer's render position
                if (grid.Tag is TransformerUPSBlock transformerUPS)
                {
                    UpdateTransformerUPSRenderPosition(transformerUPS, grid);
                }
            }
        }
        
        /// <summary>
        ///     Handles mouse move on TransformerUPS grid for dragging
        /// </summary>
        private void TransformerUPSGrid_MouseLeftButtonMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingTransformerUPS && sender is Grid grid && grid == _selectedTransformerUPSGrid)
            {
                var canvas = FindParentCanvas(grid);
                if (canvas == null) return;
                
                HandleTransformerUPSDrag(e, canvas);
                
                e.Handled = true;
            }
        }

        // ========== LOAD MOUSE HANDLERS ==========

        /// <summary>
        ///     Handles mouse click on Load grid
        /// </summary>
        private void LoadGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid grid && grid.Tag is Load load)
            {
                // Prevent panning when clicking on load
                e.Handled = true;
                
                // If this load is already selected, start dragging
                if (_selectedLoad == load)
                {
                    _isDraggingLoad = true;
                    
                    // Get the canvas this load is on
                    var canvas = FindParentCanvas(grid);
                    if (canvas != null)
                    {
                        // Store the current element position, not the mouse position
                        _loadDragStartPoint = new Point(Canvas.GetLeft(grid), Canvas.GetTop(grid));
                        _loadDragMouseOffset = new Point(
                            e.GetPosition(grid).X,
                            e.GetPosition(grid).Y
                        );
                        grid.CaptureMouse();
                        grid.Cursor = Cursors.SizeAll;
                    }
                }
                else
                {
                    // Just select the load, don't start dragging
                    SelectLoad(load, grid);
                }
            }
        }
        
        /// <summary>
        ///     Handles mouse button up on Load grid
        /// </summary>
        private void LoadGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid grid && _isDraggingLoad)
            {
                // IMPORTANT: Handle event FIRST to prevent CanvasContainer handler from processing it
                e.Handled = true;
                
                _isDraggingLoad = false;
                grid.ReleaseMouseCapture();
                grid.Cursor = Cursors.Hand;
                
                // Update the load's render position
                if (grid.Tag is Load load)
                {
                    UpdateLoadRenderPosition(load, grid);
                }
            }
        }
        
        /// <summary>
        ///     Handles mouse move on Load grid for dragging
        /// </summary>
        private void LoadGrid_MouseLeftButtonMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingLoad && sender is Grid grid && grid == _selectedLoadGrid)
            {
                var canvas = FindParentCanvas(grid);
                if (canvas == null) return;
                
                HandleLoadDrag(e, canvas);
                
                e.Handled = true;
            }
        }

        // ========== EXTERNAL BUSBAR MOUSE HANDLERS ==========

        /// <summary>
        ///     Handles mouse click on ExternalBusbar canvas
        /// </summary>
        private void ExternalBusbarCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Canvas containerCanvas && containerCanvas.Tag is ExternalBusbar externalBusbar)
            {
                // Prevent panning when clicking on external busbar
                e.Handled = true;
                
                // If this external busbar is already selected, start dragging
                if (_selectedExternalBusbar == externalBusbar)
                {
                    _isDraggingExternalBusbar = true;
                    
                    // Get the canvas this external busbar is on
                    var canvas = FindParentCanvas(containerCanvas);
                    if (canvas != null)
                    {
                        // Store the current element position, not the mouse position
                        _externalBusbarDragStartPoint = new Point(Canvas.GetLeft(containerCanvas), Canvas.GetTop(containerCanvas));
                        _externalBusbarDragMouseOffset = new Point(
                            e.GetPosition(containerCanvas).X,
                            e.GetPosition(containerCanvas).Y
                        );
                        containerCanvas.CaptureMouse();
                        containerCanvas.Cursor = Cursors.SizeAll;
                    }
                }
                else
                {
                    // Just select the external busbar, don't start dragging
                    SelectExternalBusbar(externalBusbar, containerCanvas);
                }
            }
        }
        
        /// <summary>
        ///     Handles mouse button up on ExternalBusbar canvas
        /// </summary>
        private void ExternalBusbarCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Canvas containerCanvas && _isDraggingExternalBusbar)
            {
                // IMPORTANT: Handle event FIRST to prevent CanvasContainer handler from processing it
                e.Handled = true;
                
                _isDraggingExternalBusbar = false;
                containerCanvas.ReleaseMouseCapture();
                containerCanvas.Cursor = Cursors.Hand;
                
                // Update the external busbar's render position
                if (containerCanvas.Tag is ExternalBusbar externalBusbar)
                {
                    UpdateExternalBusbarRenderPosition(externalBusbar, containerCanvas);
                }
            }
        }
        
        /// <summary>
        ///     Handles mouse move on ExternalBusbar canvas for dragging
        /// </summary>
        private void ExternalBusbarCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingExternalBusbar && sender is Canvas containerCanvas && containerCanvas == _selectedExternalBusbarContainer)
            {
                var canvas = FindParentCanvas(containerCanvas);
                if (canvas == null) return;
                
                HandleExternalBusbarDrag(e, canvas);
                
                e.Handled = true;
            }
        }

        /// <summary>
        ///     Handles mouse click on a row grid
        /// </summary>
        private void RowGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid rowGrid && rowGrid.Tag is Row row)
            {
                // Prevent panning when clicking on row
                e.Handled = true;
                
                // Select the row
                SelectRow(row, rowGrid);
            }
        }
    }
}
