using System;
using System.Collections.Generic;
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
    /// Connection-related operations for MainWindow
    /// This file has been refactored and split into focused files:
    /// - MainWindow.Connection.Modes.cs: Mode management (enter/exit edit/remove modes)
    /// - MainWindow.Connection.Dots.cs: Connection dot rendering, positioning, updates, and coordinate system helpers
    /// - MainWindow.Connection.Lines.cs: Connection line rendering and path building
    /// - MainWindow.Connection.Points.cs: Render point management (add, drag, remove)
    /// 
    /// Refactoring Summary:
    /// - Merged CoordinateHelper.cs into Dots.cs
    /// - Merged DotPositions.cs into Dots.cs
    /// - Renamed Rendering.cs to Lines.cs for clarity
    /// - Renamed RenderPoints.cs to Points.cs for brevity
    /// - Simplified RenderConnectionDots with dispatcher pattern
    /// - Unified update methods for terminal blocks
    /// - Added tag extraction helper methods
    /// - Organized code with #region comments
    /// 
    /// Total: 5 files (reduced from 7)
    /// </summary>
    public partial class MainWindow
    {
        // Variables for connection edit mode
        private bool _isConnectionEditMode = false;
        private List<Ellipse> _connectionDots = new();
        private int? _firstSelectedTerminalId = null;
        private Ellipse? _firstSelectedDot = null;

        // Variables for connection lines
        private List<UIElement> _connectionLines = new();

        // Variables for render point dragging
        private bool _isDraggingRenderPoint = false;
        private Point _renderPointDragStartPoint;
        private Connection? _selectedRenderPointConnection;
        private int _selectedRenderPointX;
        private int _selectedRenderPointY;
        private Ellipse? _selectedRenderPointDot;
        private Canvas? _selectedRenderPointCanvas; // Track which canvas the render point is on

        // Variables for remove connection mode
        private bool _isRemoveConnectionMode = false;
    }
}
