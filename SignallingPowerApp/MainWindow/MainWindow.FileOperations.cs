using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using SignallingPowerApp.Core;
using SignallingPowerApp.Views;
using Path = System.IO.Path;

namespace SignallingPowerApp
{
    /// <summary>
    /// File operations (Open/Save) for MainWindow
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        ///     Handles New menu item click event
        /// </summary>
        private void NewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CreateNewProject();
        }

        /// <summary>
        ///     Handles Save menu item click event
        /// </summary>
        private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveProject();
        }

        /// <summary>
        ///     Handles Save As menu item click event
        /// </summary>
        private void SaveAsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveProjectAs();
        }

        /// <summary>
        ///     Handles Open menu item click event
        /// </summary>
        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenProject();
        }

        /// <summary>
        ///     Handles Exit menu item click event
        /// </summary>
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        ///     Creates a new project, prompting to save current project if needed
        /// </summary>
        private void CreateNewProject()
        {
            // Check if we need to save current project
            if (!PromptSaveIfNeeded())
            {
                return; // User cancelled
            }

            try
            {
                var builder = new ProjectBuilder();

                // First, reload the equipment library to ensure it's available
                string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string equipmentFilePath = Path.Combine(exeDirectory, "EquipmentLibrary.txt");

                if (!File.Exists(equipmentFilePath))
                {
                    MessageBox.Show(
                        $"Equipment library file not found:\n{equipmentFilePath}\n\nCannot open project without equipment library.",
                        "Missing Equipment Library",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Load equipment library first (required before loading project)
                _allItems = builder.OpenItemsFile(equipmentFilePath);

                // Create new project
                _currentProject = builder.NewProject();
                _currentProject.Items = _allItems; // Assign loaded equipment library to project
                _currentFilePath = null;
                _hasUnsavedChanges = false;
                _isInitialEmptyProject = true;

                // Update window title
                Title = "Signalling Power App - New Project";

                // Refresh the tree view
                PopulateTreeView();

                // Populate the Version Control panel
                PopulateVersionControlPanel();

                // Render the project on the canvas
                RenderProject();

                // Fit canvas to show all blocks
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    FitCanvasToBlocks();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating new project: {ex.Message}", "New Project Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Opens a project from a file, prompting to save current project if needed
        /// </summary>
        private void OpenProject()
        {
            // Check if we need to save current project
            if (!PromptSaveIfNeeded())
            {
                return; // User cancelled
            }

            try
            {
                // Create OpenFileDialog
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Signalling Power Project (*.spp)|*.spp",
                    DefaultExt = ".spp",
                    Title = "Open Project"
                };

                // Show dialog
                if (openFileDialog.ShowDialog() == true)
                {
                    // Load the project using ProjectBuilder
                    var builder = new ProjectBuilder();
                    
                    // First, reload the equipment library to ensure it's available
                    string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string equipmentFilePath = Path.Combine(exeDirectory, "EquipmentLibrary.txt");
                    
                    if (!File.Exists(equipmentFilePath))
                    {
                        MessageBox.Show(
                            $"Equipment library file not found:\n{equipmentFilePath}\n\nCannot open project without equipment library.",
                            "Missing Equipment Library",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    // Load equipment library first (required before loading project)
                    _allItems = builder.OpenItemsFile(equipmentFilePath);
                    
                    // Now load the project file
                    _currentProject = builder.OpenProjectFile(openFileDialog.FileName);

                    _currentProject.Items = _allItems; // Assign loaded equipment library to project

                    // Ensure all ExternalBusbars have render positions set
                    EnsureExternalBusbarPositions();
                    
                    // Update current file path and state
                    _currentFilePath = openFileDialog.FileName;
                    _hasUnsavedChanges = false;
                    _isInitialEmptyProject = false;
                    
                    // Update window title
                    Title = $"Signalling Power App - {Path.GetFileName(_currentFilePath)}";
                    
                    // Refresh the tree view with the loaded project
                    PopulateTreeView();
                    
                    // Repopulate equipment grids with the loaded equipment library
                    PopulateEquipmentGrids();
                    
                    // Populate the Version Control panel
                    PopulateVersionControlPanel();
                    
                    // Render the project on the canvas
                    RenderProject();
                    
                    // Fit canvas to show all blocks after rendering
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FitCanvasToBlocks();
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening project: {ex.Message}", "Open Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Saves the current project to a file
        /// </summary>
        private bool SaveProject()
        {
            if (_currentProject == null)
            {
                MessageBox.Show("No project to save.", "Save Project", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // If no file path exists, use Save As
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                return SaveProjectAs();
            }

            try
            {
                // Export project to string array
                string[] projectLines = _currentProject.ExportProjectText();
                
                // Write to file
                File.WriteAllLines(_currentFilePath, projectLines);
                
                // Update state
                _hasUnsavedChanges = false;
                _isInitialEmptyProject = false;
                
                MessageBox.Show("Project saved successfully.", "Save Project", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving project: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        ///     Saves the current project to a new file
        /// </summary>
        private bool SaveProjectAs()
        {
            if (_currentProject == null)
            {
                MessageBox.Show("No project to save.", "Save Project As", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Create SaveFileDialog
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Signalling Power Project (*.spp)|*.spp|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = ".spp",
                FileName = _currentProject.Name,
                Title = "Save Project As"
            };

            // Show dialog
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // Export project to string array
                    string[] projectLines = _currentProject.ExportProjectText();
                    
                    // Write to file
                    File.WriteAllLines(saveFileDialog.FileName, projectLines);
                    
                    // Update current file path and state
                    _currentFilePath = saveFileDialog.FileName;
                    _hasUnsavedChanges = false;
                    _isInitialEmptyProject = false;
                    
                    // Update window title
                    Title = $"Signalling Power App - {Path.GetFileName(_currentFilePath)}";
                    
                    MessageBox.Show("Project saved successfully.", "Save Project As", MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving project: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            return false; // User cancelled
        }

        /// <summary>
        ///     Prompts the user to save if there are unsaved changes
        /// </summary>
        /// <returns>True if operation should continue, false if cancelled</returns>
        private bool PromptSaveIfNeeded()
        {
            // Special case: Initial empty project with no changes
            if (_isInitialEmptyProject && !HasAnyBlocks())
            {
                return true; // Don't prompt for empty startup project
            }

            // Check if there are unsaved changes
            if (_hasUnsavedChanges || (_currentFilePath == null && HasAnyBlocks()))
            {
                string projectName = string.IsNullOrEmpty(_currentFilePath) 
                    ? "Untitled" 
                    : Path.GetFileNameWithoutExtension(_currentFilePath);

                var result = MessageBox.Show(
                    $"Do you want to save changes to '{projectName}'?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Save the project
                    return SaveProject();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    // User cancelled the operation
                    return false;
                }
                // If No, continue without saving
            }

            return true;
        }

        /// <summary>
        ///     Checks if the current project has any blocks
        /// </summary>
        /// <returns>True if project has blocks, false otherwise</returns>
        private bool HasAnyBlocks()
        {
            if (_currentProject == null)
            {
                return false;
            }

            // Check if there are any blocks in the project
            // We need to filter out auto-created blocks like terminals and external busbars
            return _currentProject.GetAllBlocks
                .Any(b => b.BlockType != "Terminal" && b.BlockType != "ExternalBusbar");
        }

        /// <summary>
        ///     Marks the project as having unsaved changes
        /// </summary>
        private void MarkAsModified()
        {
            _hasUnsavedChanges = true;
            _isInitialEmptyProject = false;

            // Update window title to show unsaved changes
            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                Title = $"Signalling Power App - {Path.GetFileName(_currentFilePath)}*";
            }
            else
            {
                Title = "Signalling Power App - New Project*";
            }
        }

        /// <summary>
        ///     Ensures all ExternalBusbars have render positions set
        /// </summary>
        private void EnsureExternalBusbarPositions()
        {
            if (_currentProject == null) return;
            
            const double nameWidth = 40;
            const double rowsWidth = 100;
            const double totalWidth = nameWidth + rowsWidth;
            const double rowHeight = 50;
            const int numberOfRows = 8;
            double totalHeight = numberOfRows * rowHeight;
            
            // Default to center position (these will be used in location canvas which is typically 2000x1500)
            int defaultX = (int)(1000 - totalWidth / 2);  // Center of typical canvas width (2000)
            int defaultY = (int)(750 - totalHeight / 2);   // Center of typical canvas height (1500)
            
            // Find all external busbars and ensure they have render positions
            var externalBusbars = _currentProject.GetAllBlocks
                .Where(b => b.BlockType == "ExternalBusbar")
                .Cast<ExternalBusbar>();
            
            foreach (var externalBusbar in externalBusbars)
            {
                // If render position is not set, assign the default
                if (externalBusbar.RenderPosition.Item1 == null || externalBusbar.RenderPosition.Item2 == null)
                {
                    externalBusbar.RenderPosition = (defaultX, defaultY);
                }
            }
        }

        /// <summary>
        ///     Handles About menu item click event
        /// </summary>
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Create and show the About window
            var aboutWindow = new AboutWindow
            {
                Owner = this
            };
            aboutWindow.ShowDialog();
        }
    }
}
