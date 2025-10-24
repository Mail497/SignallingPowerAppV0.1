using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SignallingPowerApp.Core;

namespace SignallingPowerApp
{
    /// <summary>
    /// Properties panel management for MainWindow
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        ///     Updates the properties view based on selected item
        /// </summary>
        private void UpdatePropertiesView(object? data)
        {
            // Clear existing properties
            PropertiesPanel.Children.Clear();

            if (data == null)
            {
                var placeholder = new TextBlock
                {
                    Text = "Select an item to view and edit its properties",
                    FontStyle = FontStyles.Italic,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(20)
                };
                PropertiesPanel.Children.Add(placeholder);
                return;
            }

            // Add Remove button for blocks (not Project and not ExternalBusbar)
            if (data is IBlock block && data is not ExternalBusbar)
            {
                var removeButton = new Button
                {
                    Content = "Remove",
                    Width = 100,
                    Height = 30,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 0, 0, 15),
                    Background = new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                    Foreground = new SolidColorBrush(Colors.White),
                    FontWeight = FontWeights.SemiBold
                };
                
                // Add click handler
                removeButton.Click += (s, e) => RemoveBlock_Click(block);
                
                PropertiesPanel.Children.Add(removeButton);
            }

            // Add title
            var titleBlock = new TextBlock
            {
                Text = $"{data.GetType().Name} Properties",
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 15)
            };
            PropertiesPanel.Children.Add(titleBlock);

            // Create property editors based on object type
            if (data is Location location)
            {
                AddLocationProperties(location);
            }
            else if (data is Supply supply)
            {
                AddSupplyProperties(supply);
            }
            else if (data is AlternatorBlock alternatorBlock)
            {
                AddAlternatorBlockProperties(alternatorBlock);
            }
            else if (data is ConductorBlock conductorBlock)
            {
                AddConductorBlockProperties(conductorBlock);
            }
            else if (data is Busbar busbar)
            {
                AddBusbarProperties(busbar);
            }
            else if (data is TransformerUPSBlock transformerBlock)
            {
                AddTransformerBlockProperties(transformerBlock);
            }
            else if (data is Row row)
            {
                AddRowProperties(row);
            }
            else if (data is Load load)
            {
                AddLoadProperties(load);
            }
            else if (data is ExternalBusbar externalBusbar)
            {
                AddExternalBusbarProperties(externalBusbar);
            }
            else if (data is Project project)
            {
                AddProjectProperties(project);
            }
            else if (data is IBlock genericBlock)
            {
                AddReadOnlyProperty("ID", genericBlock.ID.ToString());
                AddReadOnlyProperty("Type", genericBlock.BlockType);
                AddReadOnlyProperty("Parent ID", genericBlock.ParentID.ToString());
            }
        }

        /// <summary>
        ///     Adds a read-only property display to the properties panel
        /// </summary>
        private void AddReadOnlyProperty(string label, string value)
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
                Text = label + ":",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(labelBlock, 0);
            propertyGrid.Children.Add(labelBlock);

            var valueBox = new TextBox
            {
                Text = value,
                IsReadOnly = true,
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(5, 3, 5, 3)
            };
            Grid.SetColumn(valueBox, 1);
            propertyGrid.Children.Add(valueBox);
            
            PropertiesPanel.Children.Add(propertyGrid);
        }

        /// <summary>
        ///     Adds an editable property to the properties panel
        /// </summary>
        private void AddEditableProperty(string label, string value, Action<string> onSave, string? tooltip = null)
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
                Text = label + ":",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            if (tooltip != null)
            {
                labelBlock.ToolTip = tooltip;
            }
            Grid.SetColumn(labelBlock, 0);
            propertyGrid.Children.Add(labelBlock);

            var valueBox = new TextBox
            {
                Text = value,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(5, 3, 5, 3),
                Margin = new Thickness(0, 0, 10, 0)
            };
            if (tooltip != null)
            {
                valueBox.ToolTip = tooltip;
            }
            Grid.SetColumn(valueBox, 1);
            propertyGrid.Children.Add(valueBox);

            // Store the original value to detect changes
            string originalValue = value;

            // Save on LostFocus
            valueBox.LostFocus += (s, e) =>
            {
                var textBox = s as TextBox;
                if (textBox != null && textBox.Text != originalValue)
                {
                    try
                    {
                        onSave(textBox.Text);
                        originalValue = textBox.Text;
                        
                        // Mark project as modified
                        MarkAsModified();
                        
                        // Refresh tree view to show updated names
                        PopulateTreeView();
                        
                        // Re-render if needed
                        if (data is Location || data is Supply || data is AlternatorBlock || data is ConductorBlock)
                        {
                            RenderProject();
                        }
                        else if (data is Busbar || data is TransformerUPSBlock || data is Load)
                        {
                            // Re-render the location canvas for busbar, transformer, and load updates
                            RefreshLocationCanvas(data as IBlock);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating {label}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        // Revert to original value on error
                        textBox.Text = originalValue;
                    }
                }
            };

            // Save on Enter key press
            valueBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    var textBox = s as TextBox;
                    if (textBox != null)
                    {
                        try
                        {
                            onSave(textBox.Text);
                            originalValue = textBox.Text;
                            
                            // Mark project as modified
                            MarkAsModified();
                            
                            // Refresh tree view to show updated names
                            PopulateTreeView();
                            
                            // Re-render if needed
                            if (data is Location || data is Supply || data is AlternatorBlock || data is ConductorBlock)
                            {
                                RenderProject();
                            }
                            else if (data is Busbar || data is TransformerUPSBlock || data is Load)
                            {
                                // Re-render the location canvas for busbar, transformer, and load updates
                                RefreshLocationCanvas(data as IBlock);
                            }
                            
                            // Move focus away from the textbox to trigger visual update
                            Keyboard.ClearFocus();
                            e.Handled = true;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error updating {label}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            // Revert to original value on error
                            textBox.Text = originalValue;
                            e.Handled = true;
                        }
                    }
                }
            };

            PropertiesPanel.Children.Add(propertyGrid);
        }

        /// <summary>
        ///     Adds an editable position property with separate X and Y inputs to the properties panel
        /// </summary>
        private void AddEditablePositionProperty(string label, (int?, int?) position, Action<int, int> onSave, string? tooltip = null)
        {
            // Create a grid for this property
            var propertyGrid = new Grid
            {
                Margin = new Thickness(0, 5, 0, 5)
            };
            
            propertyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            propertyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            propertyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            var labelBlock = new TextBlock
            {
                Text = label + ":",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            if (tooltip != null)
            {
                labelBlock.ToolTip = tooltip;
            }
            Grid.SetColumn(labelBlock, 0);
            propertyGrid.Children.Add(labelBlock);

            // X coordinate input
            var xPanel = new DockPanel { Margin = new Thickness(0, 0, 5, 0) };
            var xLabel = new TextBlock 
            { 
                Text = "X:",
                Width = 20,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            DockPanel.SetDock(xLabel, Dock.Left);
            xPanel.Children.Add(xLabel);
            
            var xBox = new TextBox
            {
                Text = position.Item1?.ToString() ?? "0",
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(5, 3, 5, 3)
            };
            xPanel.Children.Add(xBox);
            Grid.SetColumn(xPanel, 1);
            propertyGrid.Children.Add(xPanel);

            // Y coordinate input
            var yPanel = new DockPanel { Margin = new Thickness(0, 0, 10, 0) };
            var yLabel = new TextBlock 
            { 
                Text = "Y:",
                Width = 20,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            DockPanel.SetDock(yLabel, Dock.Left);
            yPanel.Children.Add(yLabel);
            
            var yBox = new TextBox
            {
                Text = position.Item2?.ToString() ?? "0",
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(5, 3, 5, 3)
            };
            yPanel.Children.Add(yBox);
            Grid.SetColumn(yPanel, 2);
            propertyGrid.Children.Add(yPanel);

            // Store original values
            string originalX = xBox.Text;
            string originalY = yBox.Text;

            // Helper method to save position
            void SavePosition()
            {
                if (!int.TryParse(xBox.Text, out int x))
                {
                    throw new FormatException("X coordinate must be an integer");
                }
                if (!int.TryParse(yBox.Text, out int y))
                {
                    throw new FormatException("Y coordinate must be an integer");
                }
                
                onSave(x, y);
                originalX = xBox.Text;
                originalY = yBox.Text;
                
                // Mark project as modified
                MarkAsModified();
                
                // Refresh tree view to show updated names
                PopulateTreeView();
                
                // Re-render if needed
                if (data is Location || data is Supply || data is AlternatorBlock || data is ConductorBlock)
                {
                    RenderProject();
                }
                else if (data is Busbar || data is TransformerUPSBlock || data is Load || data is ExternalBusbar)
                {
                    // Re-render the location canvas for busbar, transformer, load, and external busbar updates
                    RefreshLocationCanvas(data as IBlock);
                }
            }

            // Save on LostFocus for X box
            xBox.LostFocus += (s, e) =>
            {
                if (xBox.Text != originalX || yBox.Text != originalY)
                {
                    try
                    {
                        SavePosition();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating {label}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        xBox.Text = originalX;
                        yBox.Text = originalY;
                    }
                }
            };

            // Save on LostFocus for Y box
            yBox.LostFocus += (s, e) =>
            {
                if (xBox.Text != originalX || yBox.Text != originalY)
                {
                    try
                    {
                        SavePosition();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating {label}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        xBox.Text = originalX;
                        yBox.Text = originalY;
                    }
                }
            };

            // Save on Enter key press for X box
            xBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    try
                    {
                        SavePosition();
                        Keyboard.ClearFocus();
                        e.Handled = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating {label}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        xBox.Text = originalX;
                        yBox.Text = originalY;
                        e.Handled = true;
                    }
                }
            };

            // Save on Enter key press for Y box
            yBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    try
                    {
                        SavePosition();
                        Keyboard.ClearFocus();
                        e.Handled = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating {label}: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        xBox.Text = originalX;
                        yBox.Text = originalY;
                        e.Handled = true;
                    }
                }
            };

            PropertiesPanel.Children.Add(propertyGrid);
        }

        /// <summary>
        ///     Adds Location-specific properties
        /// </summary>
        private void AddLocationProperties(Location location)
        {
            data = location;
            AddReadOnlyProperty("ID", location.ID.ToString());
            AddReadOnlyProperty("Type", location.BlockType);
            AddEditableProperty("Name", location.Name, value => location.Name = value);
            
            AddEditablePositionProperty("Position", location.RenderPosition, (x, y) =>
            {
                location.RenderPosition = (x, y);
            });
        }

        /// <summary>
        ///     Adds Supply-specific properties
        /// </summary>
        private void AddSupplyProperties(Supply supply)
        {
            data = supply;
            AddReadOnlyProperty("ID", supply.ID.ToString());
            AddReadOnlyProperty("Type", supply.BlockType);
            AddEditableProperty("Name", supply.Name, value => supply.Name = value);
            AddEditableProperty("Voltage (V)", supply.Voltage.ToString(), value =>
            {
                if (int.TryParse(value, out int voltage))
                {
                    supply.Voltage = voltage;
                }
                else
                {
                    throw new FormatException("Voltage must be an integer");
                }
            });
            AddEditableProperty("Impedance (?)", supply.Impedance.ToString(), value =>
            {
                if (double.TryParse(value, out double impedance))
                {
                    supply.Impedance = impedance;
                }
                else
                {
                    throw new FormatException("Impedance must be a number");
                }
            });
            
            AddEditablePositionProperty("Position", supply.RenderPosition, (x, y) =>
            {
                supply.RenderPosition = (x, y);
            });
        }

        /// <summary>
        ///     Adds AlternatorBlock-specific properties
        /// </summary>
        private void AddAlternatorBlockProperties(AlternatorBlock alternatorBlock)
        {
            data = alternatorBlock;
            AddReadOnlyProperty("ID", alternatorBlock.ID.ToString());
            AddReadOnlyProperty("Type", alternatorBlock.BlockType);
            AddEditableProperty("Name", alternatorBlock.Name, value => alternatorBlock.Name = value);
            
            AddEditablePositionProperty("Position", alternatorBlock.RenderPosition, (x, y) =>
            {
                alternatorBlock.RenderPosition = (x, y);
            });
            
            // Add equipment selection
            AddEquipmentDropdown(alternatorBlock);
        }

        /// <summary>
        ///     Adds ConductorBlock-specific properties
        /// </summary>
        private void AddConductorBlockProperties(ConductorBlock conductorBlock)
        {
            data = conductorBlock;
            AddReadOnlyProperty("ID", conductorBlock.ID.ToString());
            AddReadOnlyProperty("Type", conductorBlock.BlockType);
            AddReadOnlyProperty("Parent ID", conductorBlock.ParentID.ToString());
            AddEditableProperty("Name", conductorBlock.Name, value => conductorBlock.Name = value);
            AddEditableProperty("Length (m)", conductorBlock.Length.ToString(), value =>
            {
                if (double.TryParse(value, out double length))
                {
                    conductorBlock.Length = length;
                }
                else
                {
                    throw new FormatException("Length must be a number");
                }
            }, "The length of the conductor in meters");
            
            AddEditablePositionProperty("Position", conductorBlock.RenderPosition, (x, y) =>
            {
                conductorBlock.RenderPosition = (x, y);
            });
            
            // Add equipment selection
            AddConductorEquipmentDropdown(conductorBlock);
        }

        /// <summary>
        ///     Adds Busbar-specific properties
        /// </summary>
        private void AddBusbarProperties(Busbar busbar)
        {
            data = busbar;
            AddReadOnlyProperty("ID", busbar.ID.ToString());
            AddReadOnlyProperty("Type", busbar.BlockType);
            AddReadOnlyProperty("Parent ID", busbar.ParentID.ToString());
            AddEditableProperty("Name", busbar.Name, value => busbar.Name = value);
            
            AddEditablePositionProperty("Position", busbar.RenderPosition, (x, y) =>
            {
                busbar.RenderPosition = (x, y);
            });
        }

        /// <summary>
        ///     Adds TransformerUPSBlock-specific properties
        /// </summary>
        private void AddTransformerBlockProperties(TransformerUPSBlock transformerBlock)
        {
            data = transformerBlock;
            AddReadOnlyProperty("ID", transformerBlock.ID.ToString());
            AddReadOnlyProperty("Type", transformerBlock.BlockType);
            AddReadOnlyProperty("Parent ID", transformerBlock.ParentID.ToString());
            AddEditableProperty("Name", transformerBlock.Name, value => transformerBlock.Name = value);
            
            AddEditablePositionProperty("Position", transformerBlock.RenderPosition, (x, y) =>
            {
                transformerBlock.RenderPosition = (x, y);
            });
            
            // Add equipment selection
            AddTransformerEquipmentDropdown(transformerBlock);
        }

        /// <summary>
        ///     Adds Load-specific properties
        /// </summary>
        private void AddLoadProperties(Load load)
        {
            data = load;
            AddReadOnlyProperty("ID", load.ID.ToString());
            AddReadOnlyProperty("Type", load.BlockType);
            AddReadOnlyProperty("Parent ID", load.ParentID.ToString());
            AddEditableProperty("Name", load.Name, value => load.Name = value);
            
            AddEditablePositionProperty("Position", load.RenderPosition, (x, y) =>
            {
                load.RenderPosition = (x, y);
            });
            
            // Add equipment selection
            AddLoadEquipmentDropdown(load);
        }

        /// <summary>
        ///     Adds ExternalBusbar-specific properties
        /// </summary>
        private void AddExternalBusbarProperties(ExternalBusbar externalBusbar)
        {
            data = externalBusbar;
            AddReadOnlyProperty("ID", externalBusbar.ID.ToString());
            AddReadOnlyProperty("Type", externalBusbar.BlockType);
            AddReadOnlyProperty("Parent ID", externalBusbar.ParentID.ToString());
            AddReadOnlyProperty("Name", externalBusbar.Name);
            
            AddEditablePositionProperty("Position", externalBusbar.RenderPosition, (x, y) =>
            {
                externalBusbar.RenderPosition = (x, y);
            });
        }

        /// <summary>
        ///     Adds Project-specific properties
        /// </summary>
        private void AddProjectProperties(Project project)
        {
            data = project;
            AddReadOnlyProperty("Session ID", project.SessionID.ToString());
            AddReadOnlyProperty("Save ID", project.SaveID.ToString());
            AddEditableProperty("Name", project.Name, value => project.Name = value);
            AddEditableProperty("Designer", project.Designer, value => project.Designer = value);
            AddEditableProperty("Checker", project.Checker, value => project.Checker = value);
            AddEditableProperty("Major Version", project.MajorVersion.ToString(), value =>
            {
                if (int.TryParse(value, out int version))
                {
                    project.MajorVersion = version;
                }
                else
                {
                    throw new FormatException("Version must be an integer");
                }
            });
            AddEditableProperty("Minor Version", project.MinorVersion.ToString(), value =>
            {
                if (int.TryParse(value, out int version))
                {
                    project.MinorVersion = version;
                }
                else
                {
                    throw new FormatException("Version must be an integer");
                }
            });
        }

        /// <summary>
        ///     Handles Remove button click for blocks
        /// </summary>
        private void RemoveBlock_Click(IBlock block)
        {
            try
            {
                // Confirm removal with user
                string blockTypeName = block.BlockType;
                string blockName = "";
                
                // Get block name if available
                if (block is Location location)
                {
                    blockName = location.Name;
                }
                else if (block is Supply supply)
                {
                    blockName = supply.Name;
                }
                else if (block is Busbar busbar)
                {
                    blockName = busbar.Name;
                }
                else if (block is TransformerUPSBlock transformer)
                {
                    blockName = transformer.Name;
                }
                else if (block is AlternatorBlock alternator)
                {
                    blockName = alternator.Name;
                }
                else if (block is ConductorBlock conductor)
                {
                    blockName = conductor.Name;
                }
                else if (block is Row row)
                {
                    blockName = $"{row.Type} (ID: {row.ID})";
                }
                else if (block is Load load)
                {
                    blockName = load.Name;
                }
                
                string message = !string.IsNullOrEmpty(blockName) 
                    ? $"Are you sure you want to remove {blockTypeName} '{blockName}'?\n\nThis action cannot be undone."
                    : $"Are you sure you want to remove this {blockTypeName}?\n\nThis action cannot be undone.";
                
                var result = MessageBox.Show(
                    message,
                    "Confirm Removal",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Check if this is the last block before removing
                    bool willBeEmpty = _currentProject != null && _currentProject.GetAllBlocks.Count() == 1;
                    
                    // Check block type to determine which canvas to refresh
                    bool isRow = block is Row;
                    bool isLocationBlock = block is Busbar || block is TransformerUPSBlock || block is Load;
                    bool isLocation = block is Location;
                    
                    // If it's a location, close its canvas tab if open
                    if (isLocation && block is Location locationToRemove)
                    {
                        CloseLocationCanvasTab(locationToRemove);
                    }
                    
                    // Remove the block using its Remove method
                    block.Remove();
                    
                    // Clear selection
                    DeselectAll();
                    
                    // Refresh the UI
                    PopulateTreeView();
                    
                    // Refresh the appropriate canvas
                    if (isRow)
                    {
                        // For rows, refresh all location canvases since we don't know which one
                        RefreshAllLocationCanvases();
                    }
                    else if (isLocationBlock)
                    {
                        // For busbars, transformers, and loads, refresh the specific location canvas
                        // Note: We need to do this before the block is removed, but it's already removed
                        // So we refresh all location canvases to ensure the change is reflected
                        RefreshAllLocationCanvases();
                    }
                    else
                    {
                        // For layout canvas blocks (Location, Supply, Alternator, Conductor)
                        RenderProject();
                    }
                    
                    // Clear properties panel
                    UpdatePropertiesView(null);
                    
                    // Mark project as modified
                    MarkAsModified();
                    
                    // If this was the last block, center on the empty state button
                    if (willBeEmpty)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            FitCanvasToBlocks();
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error removing block: {ex.Message}",
                    "Removal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Adds a button for selecting conductor equipment
        /// </summary>
        private void AddConductorEquipmentDropdown(ConductorBlock conductorBlock)
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
            Conductor? currentEquipment = null;
            try
            {
                currentEquipment = conductorBlock.Equipment;
            }
            catch (InvalidDataException)
            {
                // No equipment assigned
            }

            var equipmentTextBox = new TextBox
            {
                Text = currentEquipment != null ? $"{currentEquipment.Description} ({currentEquipment.Cores}C {currentEquipment.StrandCount}/{currentEquipment.CrossSectionalArea})" : "(None)",
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
                    selectWindow.SetupForConductorSelection(_allItems.Conductors, currentEquipment);
                    
                    if (selectWindow.ShowDialog() == true)
                    {
                        if (selectWindow.SelectedEquipment is Conductor selectedConductor)
                        {
                            conductorBlock.Equipment = selectedConductor;
                            
                            // Update the conductor block name to match the equipment
                            conductorBlock.Name = selectedConductor.Description;
                            
                            // Update the text box
                            equipmentTextBox.Text = $"{selectedConductor.Description} ({selectedConductor.Cores}C {selectedConductor.StrandCount}/{selectedConductor.CrossSectionalArea})";
                            
                            // Mark project as modified
                            MarkAsModified();
                            
                            // Refresh tree view and re-render
                            PopulateTreeView();
                            RenderProject();
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
        ///     Adds a button for selecting load equipment (consumer)
        /// </summary>
        private void AddLoadEquipmentDropdown(Load load)
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
            Consumer? currentEquipment = null;
            try
            {
                currentEquipment = load.Equipment;
            }
            catch (InvalidDataException)
            {
                // No equipment assigned
            }

            var equipmentTextBox = new TextBox
            {
                Text = currentEquipment != null ? $"{currentEquipment.Name} - {currentEquipment.Description} ({currentEquipment.Load} VA)" : "(None)",
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
                    selectWindow.SetupForConsumerSelection(_allItems.Consumers, currentEquipment);
                    
                    if (selectWindow.ShowDialog() == true)
                    {
                        if (selectWindow.SelectedEquipment is Consumer selectedConsumer)
                        {
                            load.Equipment = selectedConsumer;
                            
                            // Update the load name to match the equipment
                            load.Name = selectedConsumer.Name;
                            
                            // Update the text box
                            equipmentTextBox.Text = $"{selectedConsumer.Name} - {selectedConsumer.Description} ({selectedConsumer.Load} VA)";
                            
                            // Mark project as modified
                            MarkAsModified();
                            
                            // Refresh tree view and re-render location canvas
                            PopulateTreeView();
                            RefreshLocationCanvas(load);
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
