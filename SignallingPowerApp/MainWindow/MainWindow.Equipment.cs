using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SignallingPowerApp.Core;
using SignallingPowerApp.ViewModels;
using SignallingPowerApp.Views;
using Microsoft.Win32;

namespace SignallingPowerApp
{
    /// <summary>
    /// Equipment library management for MainWindow
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        ///     Loads equipment data from EquipmentLibrary.txt file
        /// </summary>
        private void LoadEquipmentFromFile()
        {
            try
            {
                // Get the directory where the executable is located
                string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string equipmentFilePath = System.IO.Path.Combine(exeDirectory, "EquipmentLibrary.txt");

                // Check if the file exists
                if (!File.Exists(equipmentFilePath))
                {
                    MessageBox.Show(
                        $"Equipment library file not found:\n{equipmentFilePath}\n\nThe application will now close.",
                        "Missing Equipment Library",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    
                    // Close the application
                    Application.Current.Shutdown();
                    return;
                }

                // Load the equipment library using ProjectBuilder
                var builder = new ProjectBuilder();
                _allItems = builder.OpenItemsFile(equipmentFilePath);


                // Populate equipment grids
                PopulateEquipmentGrids();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading equipment library:\n{ex.Message}\n\nThe application will now close.",
                    "Equipment Library Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                // Close the application
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        ///     Populates the equipment data grids with items from AllItems
        /// </summary>
        private void PopulateEquipmentGrids()
        {
            // Clear existing ViewModels
            ConductorViewModels.Clear();
            TransformerViewModels.Clear();
            AlternatorViewModels.Clear();
            ConsumerViewModels.Clear();

            // Wrap equipment items in ViewModels
            foreach (var conductor in _allItems.Conductors)
            {
                ConductorViewModels.Add(new ConductorViewModel(conductor));
            }

            foreach (var transformer in _allItems.TransformerUPSs)
            {
                TransformerViewModels.Add(new TransformerUPSViewModel(transformer));
            }

            foreach (var alternator in _allItems.Alternators)
            {
                AlternatorViewModels.Add(new AlternatorViewModel(alternator));
            }

            foreach (var consumer in _allItems.Consumers)
            {
                ConsumerViewModels.Add(new ConsumerViewModel(consumer));
            }

            // Create collection views for filtering
            _conductorsView = CollectionViewSource.GetDefaultView(ConductorViewModels);
            _transformersView = CollectionViewSource.GetDefaultView(TransformerViewModels);
            _alternatorsView = CollectionViewSource.GetDefaultView(AlternatorViewModels);
            _consumersView = CollectionViewSource.GetDefaultView(ConsumerViewModels);

            // Populate Conductors Grid
            ConductorsGrid.ItemsSource = _conductorsView;

            // Populate Transformers Grid
            TransformersGrid.ItemsSource = _transformersView;

            // Populate Alternators Grid
            AlternatorsGrid.ItemsSource = _alternatorsView;

            // Populate Consumers Grid
            ConsumersGrid.ItemsSource = _consumersView;
        }

        /// <summary>
        ///     Handles the search box text changed event to filter equipment
        /// </summary>
        private void EquipmentSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = EquipmentSearchBox.Text.ToLower();

            // Filter Conductors
            if (_conductorsView != null)
            {
                _conductorsView.Filter = item =>
                {
                    if (string.IsNullOrWhiteSpace(searchText))
                        return true;

                    if (item is ConductorViewModel conductor)
                    {
                        return conductor.Description.ToLower().Contains(searchText) ||
                               conductor.Cores.ToString().Contains(searchText) ||
                               conductor.StrandCount.ToString().Contains(searchText) ||
                               conductor.CrossSectionalArea.ToString().Contains(searchText) ||
                               conductor.IsCustomText.ToLower().Contains(searchText);
                    }
                    return false;
                };
            }

            // Filter Transformers
            if (_transformersView != null)
            {
                _transformersView.Filter = item =>
                {
                    if (string.IsNullOrWhiteSpace(searchText))
                        return true;

                    if (item is TransformerUPSViewModel transformer)
                    {
                        return transformer.Description.ToLower().Contains(searchText) ||
                               transformer.Rating.ToString().Contains(searchText) ||
                               transformer.PercentageZ.ToString().Contains(searchText) ||
                               transformer.IsCustomText.ToLower().Contains(searchText);
                    }
                    return false;
                };
            }

            // Filter Alternators
            if (_alternatorsView != null)
            {
                _alternatorsView.Filter = item =>
                {
                    if (string.IsNullOrWhiteSpace(searchText))
                        return true;

                    if (item is AlternatorViewModel alternator)
                    {
                        return alternator.Description.ToLower().Contains(searchText) ||
                               alternator.RatingVA.ToString().Contains(searchText) ||
                               alternator.RatingW.ToString().Contains(searchText) ||
                               alternator.IsCustomText.ToLower().Contains(searchText);
                    }
                    return false;
                };
            }

            // Filter Consumers
            if (_consumersView != null)
            {
                _consumersView.Filter = item =>
                {
                    if (string.IsNullOrWhiteSpace(searchText))
                        return true;

                    if (item is ConsumerViewModel consumer)
                    {
                        return consumer.Name.ToLower().Contains(searchText) ||
                               consumer.Description.ToLower().Contains(searchText) ||
                               consumer.Load.ToString().Contains(searchText) ||
                               consumer.IsCustomText.ToLower().Contains(searchText);
                    }
                    return false;
                };
            }
        }

        /// <summary>
        ///     Handles the clear search button click event
        /// </summary>
        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            EquipmentSearchBox.Text = string.Empty;
        }

        /// <summary>
        ///     Handles DataGrid PreviewMouseWheel to forward scrolling to parent ScrollViewer
        /// </summary>
        private void DataGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new System.Windows.Input.MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
                {
                    RoutedEvent = UIElement.MouseWheelEvent,
                    Source = sender
                };
                var parent = ((Control)sender).Parent as UIElement;
                parent?.RaiseEvent(eventArg);
            }
        }

        /// <summary>
        ///     Handles Add Equipment button click
        /// </summary>
        private void AddEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            // Show equipment type selection dialog
            var selectionDialog = new AddEquipmentDialog
            {
                Owner = this
            };

            if (selectionDialog.ShowDialog() == true)
            {
                var equipmentType = selectionDialog.SelectedEquipmentType;
                var editWindow = new EditEquipmentWindow
                {
                    Owner = this
                };

                // Setup the form based on selected equipment type
                switch (equipmentType)
                {
                    case "Conductor":
                        editWindow.SetupForNewConductor();
                        if (editWindow.ShowDialog() == true && editWindow.NewConductor != null)
                        {
                            _allItems.AddItem(editWindow.NewConductor);
                            ConductorViewModels.Add(new ConductorViewModel(editWindow.NewConductor));
                            _conductorsView?.Refresh();
                        }
                        break;

                    case "Transformer/UPS":
                        editWindow.SetupForNewTransformer();
                        if (editWindow.ShowDialog() == true && editWindow.NewTransformer != null)
                        {
                            _allItems.AddItem(editWindow.NewTransformer);
                            TransformerViewModels.Add(new TransformerUPSViewModel(editWindow.NewTransformer));
                            _transformersView?.Refresh();
                        }
                        break;

                    case "Alternator":
                        editWindow.SetupForNewAlternator();
                        if (editWindow.ShowDialog() == true && editWindow.NewAlternator != null)
                        {
                            _allItems.AddItem(editWindow.NewAlternator);
                            AlternatorViewModels.Add(new AlternatorViewModel(editWindow.NewAlternator));
                            _alternatorsView?.Refresh();
                        }
                        break;

                    case "Consumer":
                        editWindow.SetupForNewConsumer();
                        if (editWindow.ShowDialog() == true && editWindow.NewConsumer != null)
                        {
                            _allItems.AddItem(editWindow.NewConsumer);
                            ConsumerViewModels.Add(new ConsumerViewModel(editWindow.NewConsumer));
                            _consumersView?.Refresh();
                        }
                        break;
                }
            }
        }

        /// <summary>
        ///     Handles Export Equipment button click
        /// </summary>
        private void ExportEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create SaveFileDialog
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultExt = ".txt",
                    FileName = "EquipmentLibrary",
                    Title = "Export Equipment Library"
                };

                // Show dialog
                if (saveFileDialog.ShowDialog() == true)
                {
                    // Get equipment data from AllItems
                    string[] equipmentLines = _allItems.ExportAllItemsText();
                    
                    // Write to file
                    File.WriteAllLines(saveFileDialog.FileName, equipmentLines);
                    
                    MessageBox.Show($"Equipment library exported successfully to:\n{saveFileDialog.FileName}", 
                        "Export Successful", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting equipment library: {ex.Message}", 
                    "Export Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Handles Edit button click for Conductors
        /// </summary>
        private void EditConductorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ConductorViewModel viewModel)
            {
                var editWindow = new EditEquipmentWindow
                {
                    Owner = this
                };
                editWindow.SetupForConductor(viewModel);
                
                if (editWindow.ShowDialog() == true)
                {
                    if (editWindow.RemoveClicked)
                    {
                        // Remove from both collections
                        ConductorViewModels.Remove(viewModel);
                        _conductorsView?.Refresh();
                    }
                    else
                    {
                        // Refresh the view to show updated values
                        _conductorsView?.Refresh();
                    }
                }
            }
        }

        /// <summary>
        ///     Handles Edit button click for Transformers
        /// </summary>
        private void EditTransformerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is TransformerUPSViewModel viewModel)
            {
                var editWindow = new EditEquipmentWindow
                {
                    Owner = this
                };
                editWindow.SetupForTransformer(viewModel);
                
                if (editWindow.ShowDialog() == true)
                {
                    if (editWindow.RemoveClicked)
                    {
                        // Remove from both collections
                        TransformerViewModels.Remove(viewModel);
                        _transformersView?.Refresh();
                    }
                    else
                    {
                        // Refresh the view to show updated values
                        _transformersView?.Refresh();
                    }
                }
            }
        }

        /// <summary>
        ///     Handles Edit button click for Alternators
        /// </summary>
        private void EditAlternatorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is AlternatorViewModel viewModel)
            {
                var editWindow = new EditEquipmentWindow
                {
                    Owner = this
                };
                editWindow.SetupForAlternator(viewModel);
                
                if (editWindow.ShowDialog() == true)
                {
                    if (editWindow.RemoveClicked)
                    {
                        // Remove from both collections
                        AlternatorViewModels.Remove(viewModel);
                        _alternatorsView?.Refresh();
                    }
                    else
                    {
                        // Refresh the view to show updated values
                        _alternatorsView?.Refresh();
                    }
                }
            }
        }

        /// <summary>
        ///     Handles Edit button click for Consumers
        /// </summary>
        private void EditConsumerButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ConsumerViewModel viewModel)
            {
                var editWindow = new EditEquipmentWindow
                {
                    Owner = this
                };
                editWindow.SetupForConsumer(viewModel);
                
                if (editWindow.ShowDialog() == true)
                {
                    if (editWindow.RemoveClicked)
                    {
                        // Remove from both collections
                        ConsumerViewModels.Remove(viewModel);
                        _consumersView?.Refresh();
                    }
                    else
                    {
                        // Refresh the view to show updated values
                        _consumersView?.Refresh();
                    }
                }
            }
        }
    }
}
