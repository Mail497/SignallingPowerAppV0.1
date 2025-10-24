using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SignallingPowerApp.Core;
using SignallingPowerApp.ViewModels;
using SignallingPowerApp.Views;

namespace SignallingPowerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Core initialization and orchestration - specific functionality is in partial classes
    /// </summary>
    public partial class MainWindow : Window
    {
        // Variables for panning
        private bool _isPanning = false;
        private Point _lastMousePosition;
        
        // Variables for zooming
        private double _zoomLevel = 1.0;
        private const double _zoomMin = 0.1;
        private const double _zoomMax = 5.0;
        private const double _zoomIncrement = 0.1;

        // Variables for location dragging
        private bool _isDraggingLocation = false;
        private Point _locationDragStartPoint;
        private Point _locationDragMouseOffset;
        
        // Variables for double-click detection
        private DateTime _lastLocationClickTime = DateTime.MinValue;
        private Location? _lastClickedLocation = null;
        private const int DoubleClickThresholdMs = 300;

        // Variables for supply dragging
        private bool _isDraggingSupply = false;
        private Point _supplyDragStartPoint;
        private Point _supplyDragMouseOffset;

        // Variables for alternator dragging
        private bool _isDraggingAlternator = false;
        private Point _alternatorDragStartPoint;
        private Point _alternatorDragMouseOffset;

        // Variables for conductor dragging
        private bool _isDraggingConductor = false;
        private Point _conductorDragStartPoint;
        private Point _conductorDragMouseOffset;

        // Variables for busbar dragging
        private bool _isDraggingBusbar = false;
        private Point _busbarDragStartPoint;
        private Point _busbarDragMouseOffset;
        private Busbar? _selectedBusbar;
        private StackPanel? _selectedBusbarContainer;
        private Rectangle? _busbarSelectionRectangle;

        // Variables for TransformerUPS dragging
        private bool _isDraggingTransformerUPS = false;
        private Point _transformerUPSDragStartPoint;
        private Point _transformerUPSDragMouseOffset;
        private TransformerUPSBlock? _selectedTransformerUPS;
        private Grid? _selectedTransformerUPSGrid;
        private Rectangle? _transformerUPSSelectionRectangle;

        // Variables for Row selection
        private Row? _selectedRow;
        private Grid? _selectedRowGrid;
        private Rectangle? _rowSelectionRectangle;

        // Variables for Load dragging
        private bool _isDraggingLoad = false;
        private Point _loadDragStartPoint;
        private Point _loadDragMouseOffset;
        private Load? _selectedLoad;
        private Grid? _selectedLoadGrid;
        private Rectangle? _loadSelectionRectangle;

        // Variables for ExternalBusbar dragging
        private bool _isDraggingExternalBusbar = false;
        private Point _externalBusbarDragStartPoint;
        private Point _externalBusbarDragMouseOffset;
        private ExternalBusbar? _selectedExternalBusbar;
        private Canvas? _selectedExternalBusbarContainer;
        private Rectangle? _externalBusbarSelectionRectangle;

        // Current project
        private Project? _currentProject;

        // Current file path for save operations
        private string? _currentFilePath;

        // Track if project has unsaved changes
        private bool _hasUnsavedChanges = false;

        // Track if this is the initial empty project created at startup
        private bool _isInitialEmptyProject = true;

        // AllItems for equipment library
        private AllItems _allItems;

        // Selected location
        private Location? _selectedLocation;
        private Border? _selectedLocationBorder;
        private Border? _selectionRectangle;

        // Selected supply
        private Supply? _selectedSupply;
        private System.Windows.Shapes.Ellipse? _selectedSupplyEllipse;
        private Border? _supplySelectionRectangle;

        // Selected alternator
        private AlternatorBlock? _selectedAlternator;
        private System.Windows.Shapes.Polygon? _selectedAlternatorPolygon;
        private Border? _alternatorSelectionRectangle;

        // Selected conductor
        private ConductorBlock? _selectedConductor;
        private Border? _selectedConductorBorder;
        private Rectangle? _conductorSelectionRectangle;

        // Tree view items collection
        public ObservableCollection<TreeNodeViewModel> TreeViewItems { get; set; }
            
        // Observable collections for equipment ViewModels
        public ObservableCollection<ConductorViewModel> ConductorViewModels { get; set; }
        public ObservableCollection<TransformerUPSViewModel> TransformerViewModels { get; set; }
        public ObservableCollection<AlternatorViewModel> AlternatorViewModels { get; set; }
        public ObservableCollection<ConsumerViewModel> ConsumerViewModels { get; set; }

        // Filtered collections for search functionality
        private ICollectionView? _conductorsView;
        private ICollectionView? _transformersView;
        private ICollectionView? _alternatorsView;
        private ICollectionView? _consumersView;

        // Variables for adding blocks
        private bool _isAddingBlock = false;
        private string _blockTypeToAdd = "";
        private UIElement? _shadowElement;

        // Store current data for property updates
        private object? data;

        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize transform values
            CanvasScaleTransform.ScaleX = _zoomLevel;
            CanvasScaleTransform.ScaleY = _zoomLevel;

            // Initialize tree view collection
            TreeViewItems = new ObservableCollection<TreeNodeViewModel>();
            
            // Initialize equipment ViewModel collections
            ConductorViewModels = new ObservableCollection<ConductorViewModel>();
            TransformerViewModels = new ObservableCollection<TransformerUPSViewModel>();
            AlternatorViewModels = new ObservableCollection<AlternatorViewModel>
            ();
            ConsumerViewModels = new ObservableCollection<ConsumerViewModel>();
            
            // Set data context for binding
            DataContext = this;

            // Loads initial project
            LoadEquipmentFromFile();
            var builder = new ProjectBuilder();
            _currentProject = builder.NewProject();
            _currentProject.Items = _allItems;

            // Populate tree view
            PopulateTreeView();
            
            // Populate the Version Control panel
            PopulateVersionControlPanel();

            // Render project on canvas
            RenderProject();

            // Populate grids after the window is loaded
            Loaded += (s, e) =>
            {
                // Fit canvas to blocks after everything is loaded and rendered
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    FitCanvasToBlocks();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            };
        }

        /// <summary>
        ///     Handles Add button click - shows dropdown or cancels adding
        /// </summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAddingBlock)
            {
                // If currently adding, cancel
                CancelAddingBlock();
            }
            else
            {
                // Update context menu based on current canvas tab
                UpdateAddBlockContextMenu();
                
                // Show context menu
                if (sender is Button button && button.ContextMenu != null)
                {
                    button.ContextMenu.PlacementTarget = button;
                    button.ContextMenu.IsOpen = true;
                }
            }
        }

        /// <summary>
        ///     Updates the Add Block context menu based on the currently selected canvas tab
        /// </summary>
        private void UpdateAddBlockContextMenu()
        {
            var contextMenu = FindName("AddContextMenu") as ContextMenu;
            if (contextMenu == null) return;

            // Clear existing menu items
            contextMenu.Items.Clear();

            // Get the currently selected tab
            var canvasTabs = FindName("CanvasTabs") as TabControl;
            if (canvasTabs?.SelectedItem is TabItem selectedTab)
            {
                // Check if this is the Layout tab or a Location tab
                bool isLayoutTab = selectedTab.Header?.ToString() == "Layout";
                bool isLocationTab = selectedTab.Tag is Location;

                if (isLayoutTab)
                {
                    // Layout tab - show Location and Supply
                    var locationMenuItem = new MenuItem { Header = "Location" };
                    locationMenuItem.Click += AddLocationMenuItem_Click;
                    contextMenu.Items.Add(locationMenuItem);

                    var supplyMenuItem = new MenuItem { Header = "Supply" };
                    supplyMenuItem.Click += AddSupplyMenuItem_Click;
                    contextMenu.Items.Add(supplyMenuItem);

                    var conductorMenuItem = new MenuItem { Header = "Conductor" };
                    conductorMenuItem.Click += AddConductorMenuItem_Click;
                    contextMenu.Items.Add(conductorMenuItem);
                }
                else if (isLocationTab)
                {
                    // Location tab - show Busbar and TransformerUPS options
                    var busbarMenuItem = new MenuItem { Header = "Busbar" };
                    busbarMenuItem.Click += AddBusbarMenuItem_Click;
                    contextMenu.Items.Add(busbarMenuItem);
                    
                    var transformerMenuItem = new MenuItem { Header = "TransformerUPS" };
                    transformerMenuItem.Click += AddTransformerUPSMenuItem_Click;
                    contextMenu.Items.Add(transformerMenuItem);
                    
                    var loadMenuItem = new MenuItem { Header = "Load" };
                    loadMenuItem.Click += AddLoadMenuItem_Click;
                    contextMenu.Items.Add(loadMenuItem);
                }
            }
        }

        /// <summary>
        ///     Handles Add Location menu item click
        /// </summary>
        private void AddLocationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            StartAddingBlock("Location");
        }

        /// <summary>
        ///     Handles Add Supply menu item click
        /// </summary>
        private void AddSupplyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            StartAddingBlock("Supply");
        }

        /// <summary>
        ///     Handles Add Alternator menu item click
        /// </summary>
        private void AddAlternatorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            StartAddingBlock("Alternator");
        }

        /// <summary>
        ///     Handles Add Conductor menu item click
        /// </summary>
        private void AddConductorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            StartAddingBlock("Conductor");
        }

        /// <summary>
        ///     Handles Add Busbar menu item click
        /// </summary>
        private void AddBusbarMenuItem_Click(object sender, RoutedEventArgs e)
        {
            StartAddingBlock("Busbar");
        }

        /// <summary>
        ///     Handles Add TransformerUPS menu item click
        /// </summary>
        private void AddTransformerUPSMenuItem_Click(object sender, RoutedEventArgs e)
        {
            StartAddingBlock("TransformerUPS");
        }

        /// <summary>
        ///     Handles Add Load menu item click
        /// </summary>
        private void AddLoadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            StartAddingBlock("Load");
        }

        /// <summary>
        ///     Starts the process of adding a new block to the canvas
        /// </summary>
        private void StartAddingBlock(string blockType)
        {
            // Exit other modes if active
            if (_isConnectionEditMode)
            {
                ExitConnectionEditMode();
            }
            if (_isRemoveConnectionMode)
            {
                ExitRemoveConnectionMode();
            }
            
            _isAddingBlock = true;
            _blockTypeToAdd = blockType;
            
            // Change button appearance to match edit/remove modes
            if (FindName("AddButton") is Button addButton)
            {
                addButton.Content = "Exit Add";
                addButton.Background = new SolidColorBrush(Color.FromRgb(255, 200, 200));
            }
            
            // Get the active canvas container
            var (container, canvas) = GetActiveCanvas();
            if (container == null) return;
            
            // Change cursor
            container.Cursor = Cursors.Cross;
            
            // Subscribe to mouse move and click events
            container.PreviewMouseMove += CanvasContainer_PreviewMouseMoveForAdding;
            container.PreviewMouseLeftButtonDown += CanvasContainer_PreviewMouseLeftButtonDownForAdding;
            
            // Subscribe to keyboard events for Escape
            KeyDown += MainWindow_KeyDownForAdding;
        }

        /// <summary>
        ///     Handles mouse move event while adding a block
        /// </summary>
        private void CanvasContainer_PreviewMouseMoveForAdding(object sender, MouseEventArgs e)
        {
            if (!_isAddingBlock) return;
            
            // Get the active canvas
            var (container, canvas) = GetActiveCanvas();
            if (canvas == null) return;
            
            // Remove old shadow if it exists
            if (_shadowElement != null && canvas.Children.Contains(_shadowElement))
            {
                canvas.Children.Remove(_shadowElement);
            }
            
            // Get mouse position on canvas
            Point mousePos = e.GetPosition(canvas);
            
            // Create shadow element based on block type
            if (_blockTypeToAdd == "Location")
            {
                const double locationWidth = 200;
                const double locationHeight = 200;
                
                var shadowBorder = new Border
                {
                    Width = locationWidth,
                    Height = locationHeight,
                    BorderBrush = new SolidColorBrush(Colors.Gray),
                    BorderThickness = new Thickness(3),
                    Background = new SolidColorBrush(Color.FromArgb(128, 200, 200, 200)),
                    IsHitTestVisible = false
                };
                
                // Position shadow at mouse cursor
                Canvas.SetLeft(shadowBorder, mousePos.X - locationWidth / 2);
                Canvas.SetTop(shadowBorder, mousePos.Y - locationHeight / 2);
                
                _shadowElement = shadowBorder;
                canvas.Children.Add(_shadowElement);
            }
            else if (_blockTypeToAdd == "Supply")
            {
                const double supplyDiameter = 150;
                
                var shadowEllipse = new System.Windows.Shapes.Ellipse
                {
                    Width = supplyDiameter,
                    Height = supplyDiameter,
                    Stroke = new SolidColorBrush(Colors.Gray),
                    StrokeThickness = 3,
                    Fill = new SolidColorBrush(Color.FromArgb(128, 200, 200, 200)),
                    IsHitTestVisible = false
                };
                
                // Position shadow at mouse cursor
                Canvas.SetLeft(shadowEllipse, mousePos.X - supplyDiameter / 2);
                Canvas.SetTop(shadowEllipse, mousePos.Y - supplyDiameter / 2);
                
                _shadowElement = shadowEllipse;
                canvas.Children.Add(_shadowElement);
            }
            else if (_blockTypeToAdd == "Alternator")
            {
                const double diamondSize = 150;
                
                // Create a diamond shape (rotated square)
                var shadowPolygon = new System.Windows.Shapes.Polygon
                {
                    Points = new System.Windows.Media.PointCollection
                    {
                        new Point(diamondSize / 2, 0),              // Top
                        new Point(diamondSize, diamondSize / 2),    // Right
                        new Point(diamondSize / 2, diamondSize),    // Bottom
                        new Point(0, diamondSize / 2)               // Left
                    },
                    Stroke = new SolidColorBrush(Colors.Gray),
                    StrokeThickness = 3,
                    Fill = new SolidColorBrush(Color.FromArgb(128, 200, 200, 200)),
                    IsHitTestVisible = false
                };
                
                // Position shadow at mouse cursor
                Canvas.SetLeft(shadowPolygon, mousePos.X - diamondSize / 2);
                Canvas.SetTop(shadowPolygon, mousePos.Y - diamondSize / 2);
                
                _shadowElement = shadowPolygon;
                canvas.Children.Add(_shadowElement);
            }
            else if (_blockTypeToAdd == "Conductor")
            {
                const double conductorWidth = 300;
                const double conductorHeight = 100;
                
                var shadowBorder = new Border
                {
                    Width = conductorWidth,
                    Height = conductorHeight,
                    BorderBrush = new SolidColorBrush(Colors.Gray),
                    BorderThickness = new Thickness(3),
                    Background = new SolidColorBrush(Color.FromArgb(128, 200, 200, 200)),
                    IsHitTestVisible = false
                };
                
                // Position shadow at mouse cursor
                Canvas.SetLeft(shadowBorder, mousePos.X - conductorWidth / 2);
                Canvas.SetTop(shadowBorder, mousePos.Y - conductorHeight / 2);
                
                _shadowElement = shadowBorder;
                canvas.Children.Add(_shadowElement);
            }
            else if (_blockTypeToAdd == "Busbar")
            {
                const double busbarWidth = 350;
                const double busbarHeight = 100;
                
                // Create busbar shadow
                var shadowBorder = new Border
                {
                    Width = busbarWidth,
                    Height = busbarHeight,
                    BorderBrush = new SolidColorBrush(Colors.Gray),
                    BorderThickness = new Thickness(3),
                    Background = new SolidColorBrush(Color.FromArgb(128, 200, 200, 200)),
                    IsHitTestVisible = false
                };
                
                // Position shadow at mouse cursor
                Canvas.SetLeft(shadowBorder, mousePos.X - busbarWidth / 2);
                Canvas.SetTop(shadowBorder, mousePos.Y - busbarHeight / 2);
                
                _shadowElement = shadowBorder;
                canvas.Children.Add(_shadowElement);
            }
            else if (_blockTypeToAdd == "TransformerUPS")
            {
                const double circleRadius = 50;
                const double circleDiameter = circleRadius * 2;
                const double overlap = 30;
                const double totalWidth = (circleDiameter * 2) - overlap;
                const double totalHeight = circleDiameter;
                
                // Create a grid to hold the two circles
                var shadowGrid = new Grid
                {
                    Width = totalWidth,
                    Height = totalHeight,
                    IsHitTestVisible = false
                };
                
                // Create left circle
                var leftCircle = new Ellipse
                {
                    Width = circleDiameter,
                    Height = circleDiameter,
                    Stroke = new SolidColorBrush(Colors.Gray),
                    StrokeThickness = 3,
                    Fill = new SolidColorBrush(Color.FromArgb(128, 200, 200, 200)),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                // Create right circle
                var rightCircle = new Ellipse
                {
                    Width = circleDiameter,
                    Height = circleDiameter,
                    Stroke = new SolidColorBrush(Colors.Gray),
                    StrokeThickness = 3,
                    Fill = new SolidColorBrush(Color.FromArgb(128, 200, 200, 200)),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                
                shadowGrid.Children.Add(leftCircle);
                shadowGrid.Children.Add(rightCircle);
                
                // Position shadow at mouse cursor
                Canvas.SetLeft(shadowGrid, mousePos.X - totalWidth / 2);
                Canvas.SetTop(shadowGrid, mousePos.Y - totalHeight / 2);
                
                _shadowElement = shadowGrid;
                canvas.Children.Add(_shadowElement);
            }
            else if (_blockTypeToAdd == "Load")
            {
                const double hexagonSize = 120;
                
                // Create a hexagon shape
                var shadowPolygon = new System.Windows.Shapes.Polygon
                {
                    Points = new System.Windows.Media.PointCollection
                    {
                        new Point(hexagonSize * 0.5, 0),                    // Top
                        new Point(hexagonSize, hexagonSize * 0.25),         // Top right
                        new Point(hexagonSize, hexagonSize * 0.75),         // Bottom right
                        new Point(hexagonSize * 0.5, hexagonSize),          // Bottom
                        new Point(0, hexagonSize * 0.75),                   // Bottom left
                        new Point(0, hexagonSize * 0.25)                    // Top left
                    },
                    Stroke = new SolidColorBrush(Colors.Gray),
                    StrokeThickness = 3,
                    Fill = new SolidColorBrush(Color.FromArgb(128, 200, 200, 200)),
                    IsHitTestVisible = false
                };
                
                // Position shadow at mouse cursor
                Canvas.SetLeft(shadowPolygon, mousePos.X - hexagonSize / 2);
                Canvas.SetTop(shadowPolygon, mousePos.Y - hexagonSize / 2);
                
                _shadowElement = shadowPolygon;
                canvas.Children.Add(_shadowElement);
            }
        }

        /// <summary>
        ///     Handles mouse click event while adding a block
        /// </summary>
        private void CanvasContainer_PreviewMouseLeftButtonDownForAdding(object sender, MouseButtonEventArgs e)
        {
            if (!_isAddingBlock) return;
            
            e.Handled = true;
            
            // Get the active canvas and tab
            var canvasTabs = FindName("CanvasTabs") as TabControl;
            var (container, canvas) = GetActiveCanvas();
            if (canvas == null || canvasTabs?.SelectedItem is not TabItem selectedTab) return;
            
            // Get mouse position on canvas
            Point mousePos = e.GetPosition(canvas);
            
            // Add the block to the project
            if (_currentProject != null)
            {
                IBlock newBlock;
                
                // Check if we're on a location canvas or layout canvas
                bool isLocationCanvas = selectedTab.Tag is Location location;
                
                if (_blockTypeToAdd == "Location")
                {
                    // Convert to Cartesian coordinates for layout canvas
                    var (x, y) = CanvasToCartesian(mousePos.X, mousePos.Y);
                    newBlock = _currentProject.AddBlock("Location");
                    ((Location)newBlock).RenderPosition = (x, y);
                    
                    // Set ExternalBusbar to center of location (Cartesian coordinates)
                    var newLocation = (Location)newBlock;
                    var externalBusbar = newLocation.ExternalBusbar;
                    
                    // Default to Cartesian origin (0, 0) - center of location canvas
                    externalBusbar.RenderPosition = (0, 0);
                }
                else if (_blockTypeToAdd == "Supply")
                {
                    // Convert to Cartesian coordinates for layout canvas
                    var (x, y) = CanvasToCartesian(mousePos.X, mousePos.Y);
                    newBlock = _currentProject.AddBlock("Supply");
                    ((Supply)newBlock).RenderPosition = (x, y);
                }
                else if (_blockTypeToAdd == "Alternator")
                {
                    // Convert to Cartesian coordinates for layout canvas
                    var (x, y) = CanvasToCartesian(mousePos.X, mousePos.Y);
                    newBlock = _currentProject.AddBlock("AlternatorBlock");
                    ((AlternatorBlock)newBlock).RenderPosition = (x, y);
                }
                else if (_blockTypeToAdd == "Conductor")
                {
                    // Convert to Cartesian coordinates for layout canvas
                    var (x, y) = CanvasToCartesian(mousePos.X, mousePos.Y);
                    newBlock = _currentProject.AddBlock("ConductorBlock");
                    ((ConductorBlock)newBlock).RenderPosition = (x, y);
                }
                else if (_blockTypeToAdd == "Busbar" && isLocationCanvas)
                {
                    // Add busbar to the location
                    Location locationParent = (Location)selectedTab.Tag;
                    newBlock = _currentProject.AddBlock("Busbar", locationParent.ID);
                    
                    // Convert to Cartesian coordinates for location canvas
                    var (x, y) = CanvasToCartesian(canvas, mousePos.X, mousePos.Y);
                    ((Busbar)newBlock).RenderPosition = (x, y);
                    
                    // Render the busbar on the location canvas
                    RenderBusbarOnLocationCanvas((Busbar)newBlock, canvas);
                }
                else if (_blockTypeToAdd == "TransformerUPS" && isLocationCanvas)
                {
                    // Add TransformerUPS to the location
                    Location locationParent = (Location)selectedTab.Tag;
                    newBlock = _currentProject.AddBlock("TransformerUPS", locationParent.ID);
                    
                    // Convert to Cartesian coordinates for location canvas
                    var (x, y) = CanvasToCartesian(canvas, mousePos.X, mousePos.Y);
                    ((TransformerUPSBlock)newBlock).RenderPosition = (x, y);
                    
                    // Render the TransformerUPS on the location canvas
                    RenderTransformerUPSOnLocationCanvas((TransformerUPSBlock)newBlock, canvas);
                }
                else if (_blockTypeToAdd == "Load" && isLocationCanvas)
                {
                    // Add Load to the location
                    Location locationParent = (Location)selectedTab.Tag;
                    newBlock = _currentProject.AddBlock("Load", locationParent.ID);
                        
                    // Convert to Cartesian coordinates for location canvas
                    var (x, y) = CanvasToCartesian(canvas, mousePos.X, mousePos.Y);
                    ((Load)newBlock).RenderPosition = (x, y);
                        
                    // Render the Load on the location canvas
                    RenderLoadOnLocationCanvas((Load)newBlock, canvas);
                }
                
                // Refresh tree view and canvas
                PopulateTreeView();
                if (_blockTypeToAdd != "Busbar" && _blockTypeToAdd != "TransformerUPS")
                {
                    RenderProject();
                }
                if (_blockTypeToAdd != "Busbar" && _blockTypeToAdd != "TransformerUPS" && _blockTypeToAdd != "Load")
                {
                    RenderProject();
                }
                
                // Mark project as modified
                MarkAsModified();
            }
            
            // Cancel adding mode
            CancelAddingBlock();
        }

        /// <summary>
        ///     Handles key down event for Escape key while adding a block
        /// </summary>
        private void MainWindow_KeyDownForAdding(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && _isAddingBlock)
            {
                CancelAddingBlock();
                e.Handled = true;
            }
        }

        /// <summary>
        ///     Cancels the adding block mode
        /// </summary>
        private void CancelAddingBlock()
        {
            _isAddingBlock = false;
            _blockTypeToAdd = "";
            
            // Remove shadow element from active canvas
            var (container, canvas) = GetActiveCanvas();
            if (_shadowElement != null && canvas != null && canvas.Children.Contains(_shadowElement))
            {
                canvas.Children.Remove(_shadowElement);
                _shadowElement = null;
            }
            
            // Reset button appearance to match normal state
            if (FindName("AddButton") is Button addButton)
            {
                addButton.Content = "Add Block";
                addButton.Background = SystemColors.ControlBrush;
            }
            
            // Reset cursor
            if (container != null)
            {
                container.Cursor = Cursors.Arrow;
            }
            
            // Unsubscribe from events
            if (container != null)
            {
                container.PreviewMouseMove -= CanvasContainer_PreviewMouseMoveForAdding;
                container.PreviewMouseLeftButtonDown -= CanvasContainer_PreviewMouseLeftButtonDownForAdding;
            }
            KeyDown -= MainWindow_KeyDownForAdding;
        }

        /// <summary>
        ///     Handles canvas tab selection changed - exits Add Block mode and connection modes when switching tabs
        /// </summary>
        private void CanvasTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isAddingBlock)
            {
                CancelAddingBlock();
            }
            
            if (_isConnectionEditMode)
            {
                ExitConnectionEditMode();
            }
            
            if (_isRemoveConnectionMode)
            {
                ExitRemoveConnectionMode();
            }

            // Update the Add Block context menu for the new tab
            UpdateAddBlockContextMenu();
            
            // Re-render connection lines when switching to Layout tab
            if (sender is TabControl tabControl && tabControl.SelectedItem is TabItem selectedTab)
            {
                // Check if we're switching to the Layout tab
                bool isLayoutTab = selectedTab.Header?.ToString() == "Layout";
                if (isLayoutTab)
                {
                    // Re-render connection lines on the layout canvas
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        RenderConnectionLines();
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
        }

        /// <summary>
        ///     Handles window closing event - prompts to save if there are unsaved changes
        /// </summary>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!PromptSaveIfNeeded())
            {
                e.Cancel = true; // Cancel closing if user clicks Cancel in save dialog
            }
        }
    }
}