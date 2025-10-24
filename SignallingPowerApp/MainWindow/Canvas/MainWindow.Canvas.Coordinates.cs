using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using SignallingPowerApp.Core;

namespace SignallingPowerApp
{
    /// <summary>
    /// Coordinate conversion utilities for canvas
    /// NOTE: This file is now deprecated. All coordinate conversion logic has been moved to:
    /// - MainWindow.Helpers.Coordinates.cs (coordinate system conversion)
    /// - MainWindow.Helpers.Position.cs (position update methods)
    /// 
    /// This file is kept for backward compatibility but will be removed in a future refactor.
    /// All methods now delegate to the helper classes.
    /// </summary>
    public partial class MainWindow
    {
        // All coordinate conversion and position update methods have been moved to:
        // - SignallingPowerApp/MainWindow/Helpers/MainWindow.Helpers.Coordinates.cs
        // - SignallingPowerApp/MainWindow/Helpers/MainWindow.Helpers.Position.cs
        // 
        // The methods are still available through the partial class structure.
    }
}
