using System;
using System.Windows;
using System.Windows.Controls;
using SignallingPowerApp.Core;

namespace SignallingPowerApp
{
    /// <summary>
    /// Coordinate system utilities for canvas operations
    /// Consolidates coordinate conversion logic from MainWindow.Canvas.Coordinates.cs
    /// and MainWindow.Connection.Dots.cs (CanvasCoordinateSystem class)
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Represents a coordinate system handler for a specific canvas
        /// All canvases use Cartesian coordinates for storage, with canvas-specific conversions for rendering
        /// </summary>
        private class CanvasCoordinateSystem
        {
            private readonly Canvas _canvas;

            public CanvasCoordinateSystem(Canvas canvas)
            {
                _canvas = canvas;
            }

            /// <summary>
            /// Converts from stored Cartesian coordinates to canvas pixel coordinates
            /// </summary>
            public Point ToCanvasPoint(int x, int y)
            {
                double canvasCenterX = _canvas.Width / 2;
                double canvasCenterY = _canvas.Height / 2;

                // In Cartesian system: right is +X, up is +Y
                // In Canvas system: right is +X, down is +Y
                // So we need to flip the Y coordinate
                return new Point(canvasCenterX + x, canvasCenterY - y);
            }

            /// <summary>
            /// Converts from canvas pixel coordinates to stored Cartesian coordinates
            /// </summary>
            public (int x, int y) FromCanvasPoint(double canvasX, double canvasY)
            {
                double canvasCenterX = _canvas.Width / 2;
                double canvasCenterY = _canvas.Height / 2;

                // Convert from canvas to Cartesian
                int x = (int)Math.Round(canvasX - canvasCenterX);
                int y = (int)Math.Round(canvasCenterY - canvasY);

                return (x, y);
            }

            /// <summary>
            /// Gets the canvas this coordinate system is for
            /// </summary>
            public Canvas Canvas => _canvas;
        }

        /// <summary>
        /// Gets the coordinate system for a specific canvas
        /// </summary>
        private CanvasCoordinateSystem GetCoordinateSystem(Canvas canvas)
        {
            return new CanvasCoordinateSystem(canvas);
        }

        /// <summary>
        /// Gets the coordinate system for a connection
        /// </summary>
        private CanvasCoordinateSystem? GetCoordinateSystemForConnection(Connection connection)
        {
            var canvas = GetCanvasForConnection(connection);
            if (canvas == null) return null;
            return new CanvasCoordinateSystem(canvas);
        }

        /// <summary>
        /// Converts Cartesian coordinates to Canvas coordinates for a specific canvas
        /// </summary>
        private Point CartesianToCanvas(Canvas canvas, int x, int y)
        {
            var coordSystem = GetCoordinateSystem(canvas);
            return coordSystem.ToCanvasPoint(x, y);
        }

        /// <summary>
        /// Converts Cartesian coordinates to Canvas coordinates (for DiagramCanvas - backward compatibility)
        /// </summary>
        private Point CartesianToCanvas(int x, int y)
        {
            return CartesianToCanvas(DiagramCanvas, x, y);
        }

        /// <summary>
        /// Converts Canvas coordinates back to Cartesian coordinates for a specific canvas
        /// </summary>
        private (int, int) CanvasToCartesian(Canvas canvas, double canvasX, double canvasY)
        {
            var coordSystem = GetCoordinateSystem(canvas);
            return coordSystem.FromCanvasPoint(canvasX, canvasY);
        }

        /// <summary>
        /// Converts Canvas coordinates back to Cartesian coordinates (for DiagramCanvas - backward compatibility)
        /// </summary>
        private (int, int) CanvasToCartesian(double canvasX, double canvasY)
        {
            return CanvasToCartesian(DiagramCanvas, canvasX, canvasY);
        }
    }
}
