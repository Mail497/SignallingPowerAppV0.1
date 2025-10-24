using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SignallingPowerApp.Core;

namespace SignallingPowerApp
{
    /// <summary>
    /// Extended properties panel management for MainWindow (equipment dropdowns and row properties)
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        ///     Adds Row-specific properties
        /// </summary>
        private void AddRowProperties(Row row)
        {
            data = row;
            AddReadOnlyProperty("ID", row.ID.ToString());
            AddReadOnlyProperty("Type", row.BlockType);
            AddReadOnlyProperty("Parent ID", row.ParentID.ToString());
            
            // Add row type dropdown (Pin or CircuitBreaker)
            AddRowTypeDropdown(row);
            
            // Add rating field if CircuitBreaker
            if (row.Type == "CircuitBreaker")
            {
                AddEditableProperty("Rating (A)", row.Rating.ToString(), value =>
                {
                    if (int.TryParse(value, out int rating))
                    {
                        row.Rating = rating;
                        
                        // Re-render the busbar to show the updated row
                        var parentBusbar = _currentProject?.GetBlock(row.ParentID) as Busbar;
                        if (parentBusbar != null)
                        {
                            RefreshLocationCanvas(parentBusbar);
                        }
                    }
                    else
                    {
                        throw new FormatException("Rating must be an integer");
                    }
                }, "Circuit breaker rating in Amperes");
            }
        }
        
        /// <summary>
        ///     Adds a dropdown for selecting row type (Pin or CircuitBreaker)
        /// </summary>
        private void AddRowTypeDropdown(Row row)
        {
            // Create a grid for this property
            var propertyGrid = new Grid
            {
                Margin = new Thickness(0, 5, 0, 5)
            };
            
            propertyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            propertyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            var labelBlock = new TextBlock
            {
                Text = "Row Type:",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(labelBlock, 0);
            propertyGrid.Children.Add(labelBlock);

            var comboBox = new ComboBox
            {
                VerticalContentAlignment = VerticalAlignment.Center
            };
            
            // Add row type options
            var pinItem = new ComboBoxItem { Content = "Pin", Tag = "Pin" };
            var circuitBreakerItem = new ComboBoxItem { Content = "Circuit Breaker", Tag = "CircuitBreaker" };
            
            comboBox.Items.Add(pinItem);
            comboBox.Items.Add(circuitBreakerItem);
            
            // Select current type
            if (row.Type == "Pin")
            {
                comboBox.SelectedItem = pinItem;
            }
            else if (row.Type == "CircuitBreaker")
            {
                comboBox.SelectedItem = circuitBreakerItem;
            }
            
            // Handle selection changed - update row type immediately
            comboBox.SelectionChanged += (s, e) =>
            {
                // Ignore if this is the initial selection
                if (!comboBox.IsDropDownOpen && e.AddedItems.Count == 0)
                    return;
                    
                try
                {
                    if (comboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string selectedType)
                    {
                        // Update the row type
                        row.Type = selectedType;
                        
                        // Mark project as modified
                        MarkAsModified();
                        
                        // Re-render the busbar to show the updated row (Pin circle vs CircuitBreaker square)
                        var parentBusbar = _currentProject?.GetBlock(row.ParentID) as Busbar;
                        if (parentBusbar != null)
                        {
                            // Find the location canvas this busbar is on
                            var canvasTabs = FindName("CanvasTabs") as TabControl;
                            if (canvasTabs?.SelectedItem is TabItem selectedTab && selectedTab.Content is Grid canvasContainer)
                            {
                                var locationCanvas = canvasContainer.Children.OfType<Canvas>().FirstOrDefault();
                                if (locationCanvas != null)
                                {
                                    // Remove the existing busbar from canvas
                                    var existingBusbar = locationCanvas.Children.OfType<StackPanel>()
                                        .FirstOrDefault(sp => sp.Tag is Busbar b && b.ID == parentBusbar.ID);
                                    
                                    if (existingBusbar != null)
                                    {
                                        locationCanvas.Children.Remove(existingBusbar);
                                    }
                                    
                                    // Re-render the busbar with updated row shapes
                                    RenderBusbarOnLocationCanvas(parentBusbar, locationCanvas);
                                    
                                    // If in connection edit mode, re-render connection dots
                                    if (_isConnectionEditMode)
                                    {
                                        RenderConnectionDots();
                                    }
                                }
                            }
                        }
                        
                        // Refresh the properties panel to show/hide the rating field
                        UpdatePropertiesView(row);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating row type: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            
            Grid.SetColumn(comboBox, 1);
            propertyGrid.Children.Add(comboBox);

            PropertiesPanel.Children.Add(propertyGrid);
        }

        /// <summary>
        ///     Adds a button for selecting alternator equipment
        /// </summary>
        private void AddEquipmentDropdown(AlternatorBlock alternatorBlock)
        {
            // Create a grid for this property
            var propertyGrid = new Grid
            {
                Margin = new Thickness(0, 5, 0, 5)
            };
            
            propertyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            propertyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            propertyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            
            var labelBlock = new TextBlock
            {
                Text = "Equipment:",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(labelBlock, 0);
            propertyGrid.Children.Add(labelBlock);

            // Get current equipment if assigned (handle null equipment gracefully)
            Alternator? currentEquipment = null;
            try
            {
                currentEquipment = alternatorBlock.Equipment;
            }
            catch (InvalidDataException)
            {
                // No equipment assigned
            }

            var equipmentTextBox = new TextBox
            {
                Text = currentEquipment != null ? $"{currentEquipment.Description} ({currentEquipment.RatingVA} VA, {currentEquipment.RatingW} W)" : "(None)",
                IsReadOnly = true,
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(5, 3, 5, 3),
                Margin = new Thickness(0, 0, 5, 0)
            };
            Grid.SetColumn(equipmentTextBox, 1);
            propertyGrid.Children.Add(equipmentTextBox);

            var selectButton = new Button
            {
                Content = "Select...",
                Width = 90,
                Height = 28,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(selectButton, 2);
            propertyGrid.Children.Add(selectButton);

            // Handle button click
            selectButton.Click += (s, e) =>
            {
                try
                {
                    var selectWindow = new Views.SelectEquipmentWindow
                    {
                        Owner = this
                    };
                    selectWindow.SetupForAlternatorSelection(_allItems.Alternators, currentEquipment);
                    
                    if (selectWindow.ShowDialog() == true)
                    {
                        if (selectWindow.SelectedEquipment is Alternator selectedAlternator)
                        {
                            alternatorBlock.Equipment = selectedAlternator;
                            
                            // Update the text box
                            equipmentTextBox.Text = $"{selectedAlternator.Description} ({selectedAlternator.RatingVA} VA, {selectedAlternator.RatingW} W)";
                            
                            // Mark project as modified
                            MarkAsModified();
                        }
                        else if (selectWindow.SelectedEquipment == null)
                        {
                            // Clear selection was chosen
                            equipmentTextBox.Text = "(None)";
                            // Note: We don't clear the equipment here as it would throw an exception
                            // The user needs to assign equipment to use the block
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error selecting equipment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            PropertiesPanel.Children.Add(propertyGrid);
        }

        /// <summary>
        ///     Adds a button for selecting transformer/UPS equipment
        /// </summary>
        private void AddTransformerEquipmentDropdown(TransformerUPSBlock transformerBlock)
        {
            // Create a grid for this property
            var propertyGrid = new Grid
            {
                Margin = new Thickness(0, 5, 0, 5)
            };
            
            propertyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            propertyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            propertyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            
            var labelBlock = new TextBlock
            {
                Text = "Equipment:",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(labelBlock, 0);
            propertyGrid.Children.Add(labelBlock);

            // Get current equipment if assigned (handle null equipment gracefully)
            TransformerUPS? currentEquipment = null;
            try
            {
                currentEquipment = transformerBlock.Equipment;
            }
            catch (InvalidDataException)
            {
                // No equipment assigned
            }

            var equipmentTextBox = new TextBox
            {
                Text = currentEquipment != null ? $"{currentEquipment.Description} ({currentEquipment.Rating} kVA, {currentEquipment.PercentageZ}% Z)" : "(None)",
                IsReadOnly = true,
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(5, 3, 5, 3),
                Margin = new Thickness(0, 0, 5, 0)
            };
            Grid.SetColumn(equipmentTextBox, 1);
            propertyGrid.Children.Add(equipmentTextBox);

            var selectButton = new Button
            {
                Content = "Select...",
                Width = 90,
                Height = 28,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(selectButton, 2);
            propertyGrid.Children.Add(selectButton);

            // Handle button click
            selectButton.Click += (s, e) =>
            {
                try
                {
                    var selectWindow = new Views.SelectEquipmentWindow
                    {
                        Owner = this
                    };
                    selectWindow.SetupForTransformerSelection(_allItems.TransformerUPSs, currentEquipment);
                    
                    if (selectWindow.ShowDialog() == true)
                    {
                        if (selectWindow.SelectedEquipment is TransformerUPS selectedTransformer)
                        {
                            transformerBlock.Equipment = selectedTransformer;
                            
                            // Update the text box
                            equipmentTextBox.Text = $"{selectedTransformer.Description} ({selectedTransformer.Rating} kVA, {selectedTransformer.PercentageZ}% Z)";
                            
                            // Mark project as modified
                            MarkAsModified();
                            
                            // Re-render the location canvas to update the label
                            RefreshLocationCanvas(transformerBlock);
                        }
                        else if (selectWindow.SelectedEquipment == null)
                        {
                            // Clear selection was chosen
                            equipmentTextBox.Text = "(None)";
                            // Note: We don't clear the equipment here as it would throw an exception
                            // The user needs to assign equipment to use the block
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error selecting equipment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            PropertiesPanel.Children.Add(propertyGrid);
        }
    }
}
