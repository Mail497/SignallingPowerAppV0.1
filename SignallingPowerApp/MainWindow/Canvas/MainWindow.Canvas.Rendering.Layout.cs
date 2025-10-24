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
    /// Layout canvas block rendering operations (Location, Supply, Alternator, Conductor)
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        ///     Renders a Location object on the canvas
        /// </summary>
        /// <param name="location">The Location object to render</param>
        private void RenderLocation(Location location)
        {
            const double locationWidth = 200;
            const double locationHeight = 200;

            // Get position from Location object (default to 0,0 if not set)
            int x = location.RenderPosition.Item1 ?? 0;
            int y = location.RenderPosition.Item2 ?? 0;

            // Convert Cartesian to Canvas coordinates
            Point canvasPos = CartesianToCanvas(x, y);

            // Adjust position so the center of the rectangle is at the specified point
            double left = canvasPos.X - (locationWidth / 2);
            double top = canvasPos.Y - (locationHeight / 2);

            // Create border (rectangle with border)
            var border = new Border
            {
                Width = locationWidth,
                Height = locationHeight,
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(3),
                Background = new SolidColorBrush(Colors.White),
                Tag = location, // Store reference to location
                Cursor = Cursors.Hand
            };
            // Add mouse event handlers for selection and dragging
            border.MouseLeftButtonDown += LocationBorder_MouseLeftButtonDown;
            border.MouseLeftButtonUp += LocationBorder_MouseLeftButtonUp;
            border.MouseMove += LocationBorder_MouseMove;

            // Create text block for location name
            var textBlock = new TextBlock
            {
                Text = location.Name,
                FontSize = 20,
                FontWeight = FontWeights.Normal,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10)
            };

            // Add text to border
            border.Child = textBlock;

            // Position the border on the canvas
            Canvas.SetLeft(border, left);
            Canvas.SetTop(border, top);

            // Add to canvas
            DiagramCanvas.Children.Add(border);
        }

        /// <summary>
        ///     Renders a Supply object on the canvas as a circle
        /// </summary>
        /// <param name="supply">The Supply object to render</param>
        private void RenderSupply(Supply supply)
        {
            const double supplyDiameter = 150;

            // Get position from Supply object (default to 0,0 if not set)
            int x = supply.RenderPosition.Item1 ?? 0;
            int y = supply.RenderPosition.Item2 ?? 0;

            // Convert Cartesian to Canvas coordinates
            Point canvasPos = CartesianToCanvas(x, y);

            // Adjust position so the center of the circle is at the specified point
            double left = canvasPos.X - (supplyDiameter / 2);
            double top = canvasPos.Y - (supplyDiameter / 2);

            // Create ellipse (circle)
            var ellipse = new Ellipse
            {
                Width = supplyDiameter,
                Height = supplyDiameter,
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 3,
                Fill = new SolidColorBrush(Colors.White),
                Tag = supply, // Store reference to supply
                Cursor = Cursors.Hand
            };

            // Add mouse event handlers for selection and dragging
            ellipse.MouseLeftButtonDown += SupplyEllipse_MouseLeftButtonDown;
            ellipse.MouseLeftButtonUp += SupplyEllipse_MouseLeftButtonUp;
            ellipse.MouseMove += SupplyEllipse_MouseMove;

            // Create text block for supply name
            var textBlock = new TextBlock
            {
                Text = supply.Name,
                FontSize = 16,
                FontWeight = FontWeights.Normal,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                IsHitTestVisible = false // Allow mouse events to pass through to ellipse
            };

            // Position the text in the center of the circle
            Canvas.SetLeft(textBlock, canvasPos.X - 60);
            Canvas.SetTop(textBlock, canvasPos.Y - 10);
            textBlock.Width = 120;

            // Position the ellipse on the canvas
            Canvas.SetLeft(ellipse, left);
            Canvas.SetTop(ellipse, top);

            // Add to canvas
            DiagramCanvas.Children.Add(ellipse);
            DiagramCanvas.Children.Add(textBlock);
        }

        /// <summary>
        ///     Renders an Alternator object on the canvas as a diamond
        /// </summary>
        /// <param name="alternator">The AlternatorBlock object to render</param>
        private void RenderAlternator(AlternatorBlock alternator)
        {
            const double diamondSize = 150;

            // Get position from AlternatorBlock object (default to 0,0 if not set)
            int x = alternator.RenderPosition.Item1 ?? 0;
            int y = alternator.RenderPosition.Item2 ?? 0;

            // Convert Cartesian to Canvas coordinates
            Point canvasPos = CartesianToCanvas(x, y);

            // Adjust position so the center of the diamond is at the specified point
            double left = canvasPos.X - (diamondSize / 2);
            double top = canvasPos.Y - (diamondSize / 2);

            // Create diamond shape (polygon)
            var polygon = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(diamondSize / 2, 0),              // Top
                    new Point(diamondSize, diamondSize / 2),    // Right
                    new Point(diamondSize / 2, diamondSize),    // Bottom
                    new Point(0, diamondSize / 2)               // Left
                },
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 3,
                Fill = new SolidColorBrush(Colors.White),
                Tag = alternator, // Store reference to alternator
                Cursor = Cursors.Hand
            };

            // Add mouse event handlers for selection and dragging
            polygon.MouseLeftButtonDown += AlternatorPolygon_MouseLeftButtonDown;
            polygon.MouseLeftButtonUp += AlternatorPolygon_MouseLeftButtonUp;
            polygon.MouseMove += AlternatorPolygon_MouseMove;

            // Create text block for alternator name
            var textBlock = new TextBlock
            {
                Text = alternator.Name,
                FontSize = 16,
                FontWeight = FontWeights.Normal,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                IsHitTestVisible = false // Allow mouse events to pass through to polygon
            };

            // Position the text in the center of the diamond
            Canvas.SetLeft(textBlock, canvasPos.X - 60);
            Canvas.SetTop(textBlock, canvasPos.Y - 10);
            textBlock.Width = 120;

            // Position the polygon on the canvas
            Canvas.SetLeft(polygon, left);
            Canvas.SetTop(polygon, top);

            // Add to canvas
            DiagramCanvas.Children.Add(polygon);
            DiagramCanvas.Children.Add(textBlock);
        }

        /// <summary>
        ///     Renders a Conductor object on the canvas as a rectangle
        /// </summary>
        /// <param name="conductor">The ConductorBlock object to render</param>
        private void RenderConductor(ConductorBlock conductor)
        {
            const double conductorWidth = 300;
            const double conductorHeight = 100;

            // Get position from ConductorBlock object (default to 0,0 if not set)
            int x = conductor.RenderPosition.Item1 ?? 0;
            int y = conductor.RenderPosition.Item2 ?? 0;

            // Convert Cartesian to Canvas coordinates
            Point canvasPos = CartesianToCanvas(x, y);

            // Adjust position so the center of the rectangle is at the specified point
            double left = canvasPos.X - (conductorWidth / 2);
            double top = canvasPos.Y - (conductorHeight / 2);

            // Create border (rectangle with border)
            var border = new Border
            {
                Width = conductorWidth,
                Height = conductorHeight,
                BorderBrush = new SolidColorBrush(Colors.Black),
                BorderThickness = new Thickness(3),
                Background = new SolidColorBrush(Colors.White),
                Tag = conductor, // Store reference to conductor
                Cursor = Cursors.Hand
            };

            // Add mouse event handlers for selection and dragging
            border.MouseLeftButtonDown += ConductorBorder_MouseLeftButtonDown;
            border.MouseLeftButtonUp += ConductorBorder_MouseLeftButtonUp;
            border.MouseMove += ConductorBorder_MouseMove;

            // Create text block for conductor name
            var textBlock = new TextBlock
            {
                Text = conductor.Name,
                FontSize = 20,
                FontWeight = FontWeights.Normal,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10)
            };

            // Add text to border
            border.Child = textBlock;

            // Position the border on the canvas
            Canvas.SetLeft(border, left);
            Canvas.SetTop(border, top);

            // Add to canvas
            DiagramCanvas.Children.Add(border);
        }
    }
}
