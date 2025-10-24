using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SignallingPowerApp.Core;

namespace SignallingPowerApp
{
    /// <summary>
    /// Version Control tab management for MainWindow
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        ///     Populates the Version Control panel with designer and checker information
        /// </summary>
        private void PopulateVersionControlPanel()
        {
            // Clear existing content
            VersionControlPanel.Children.Clear();
            
            // Show placeholder if no project is loaded
            if (_currentProject == null)
            {
                var placeholder = new TextBlock
                {
                    Text = "No project loaded",
                    FontStyle = FontStyles.Italic,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(20)
                };
                VersionControlPanel.Children.Add(placeholder);
                return;
            }
            
            // Add title
            var titleBlock = new TextBlock
            {
                Text = "Project Version Control",
                FontWeight = FontWeights.Bold,
                FontSize = 18,
                Margin = new Thickness(0, 0, 0, 20)
            };
            VersionControlPanel.Children.Add(titleBlock);
            
            // Add Project Version Section
            AddVersionControlSection("Project Version", new (string, string, Action<string>, string)[]
            {
                ("Major Version", _currentProject.MajorVersion.ToString(), (value) => 
                {
                    if (int.TryParse(value, out int major))
                    {
                        _currentProject.MajorVersion = major;
                    }
                    else
                    {
                        throw new FormatException("Major Version must be a non-negative integer");
                    }
                }, "Major version number (e.g., 1 for v1.0)"),
                ("Minor Version", _currentProject.MinorVersion.ToString(), (value) => 
                {
                    if (int.TryParse(value, out int minor))
                    {
                        _currentProject.MinorVersion = minor;
                    }
                    else
                    {
                        throw new FormatException("Minor Version must be a non-negative integer");
                    }
                }, "Minor version number (e.g., 5 for v1.5)")
            });
            
            // Add separator
            var separator1 = new Border
            {
                BorderBrush = new SolidColorBrush(Colors.LightGray),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(0, 20, 0, 20)
            };
            VersionControlPanel.Children.Add(separator1);
            
            // Add Designer Section
            AddVersionControlSection("Designer Information", new (string, string, Action<string>, string)[]
            {
                ("Designer", _currentProject.Designer, (value) => _currentProject.Designer = value, "Designer name (max 32 characters)"),
                ("Design Date", _currentProject.DesignDate.ToString(), (value) => 
                {
                    if (int.TryParse(value, out int date))
                    {
                        _currentProject.DesignDate = date;
                    }
                    else
                    {
                        throw new FormatException("Design Date must be an integer");
                    }
                }, "Design date (format: YYYYMMDD)"),
                ("Design RPEQ", _currentProject.DesignRPEQ.ToString(), (value) => 
                {
                    if (int.TryParse(value, out int rpeq))
                    {
                        _currentProject.DesignRPEQ = rpeq;
                    }
                    else
                    {
                        throw new FormatException("Design RPEQ must be an integer");
                    }
                }, "Designer's RPEQ number")
            });
            
            // Add separator
            var separator2 = new Border
            {
                BorderBrush = new SolidColorBrush(Colors.LightGray),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(0, 20, 0, 20)
            };
            VersionControlPanel.Children.Add(separator2);
            
            // Add Checker Section
            AddVersionControlSection("Checker Information", new (string, string, Action<string>, string)[]
            {
                ("Checker", _currentProject.Checker, (value) => _currentProject.Checker = value, "Checker name (max 32 characters)"),
                ("Check Date", _currentProject.CheckDate.ToString(), (value) => 
                {
                    if (int.TryParse(value, out int date))
                    {
                        _currentProject.CheckDate = date;
                    }
                    else
                    {
                        throw new FormatException("Check Date must be an integer");
                    }
                }, "Check date (format: YYYYMMDD)"),
                ("Check RPEQ", _currentProject.CheckRPEQ.ToString(), (value) => 
                {
                    if (int.TryParse(value, out int rpeq))
                    {
                        _currentProject.CheckRPEQ = rpeq;
                    }
                    else
                    {
                        throw new FormatException("Check RPEQ must be an integer");
                    }
                }, "Checker's RPEQ number")
            });
        }
        
        /// <summary>
        ///     Adds a section with multiple editable fields to the Version Control panel
        /// </summary>
        private void AddVersionControlSection(string sectionTitle, (string Label, string Value, Action<string> OnSave, string Tooltip)[] fields)
        {
            // Add section title
            var sectionTitleBlock = new TextBlock
            {
                Text = sectionTitle,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10)
            };
            VersionControlPanel.Children.Add(sectionTitleBlock);
            
            // Add fields
            foreach (var field in fields)
            {
                AddVersionControlField(field.Label, field.Value, field.OnSave, field.Tooltip);
            }
        }
        
        /// <summary>
        ///     Adds an editable field to the Version Control panel
        /// </summary>
        private void AddVersionControlField(string label, string value, Action<string> onSave, string? tooltip = null)
        {
            // Create a grid for this field
            var fieldGrid = new Grid
            {
                Margin = new Thickness(0, 5, 0, 5)
            };
            
            fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            fieldGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
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
            fieldGrid.Children.Add(labelBlock);

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
            fieldGrid.Children.Add(valueBox);

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
                        // Optionally show a subtle indication of success
                        // For now, we'll silently save without notification
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

            VersionControlPanel.Children.Add(fieldGrid);
        }
    }
}
