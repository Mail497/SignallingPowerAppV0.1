using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SignallingPowerApp.Core;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Diagnostics;
using System.IO;
using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;
using PdfColor = QuestPDF.Infrastructure.Color;
using PdfColors = QuestPDF.Helpers.Colors;

namespace SignallingPowerApp
{
    /// <summary>
    /// Calculations functionality for MainWindow
    /// </summary>
    public partial class MainWindow
    {
        // Store the generated paths for printing
        private PathPoint[][]? _generatedPaths;

        /// <summary>
        ///     Handles the Build Sequential Paths button click event
        /// </summary>
        private void BuildPathsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentProject == null)
            {
                MessageBox.Show("No project loaded.", "Build Paths", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Create a calculator instance for the current project
                var calculator = new Calculator(_currentProject);

                // Build the sequential paths
                PathPoint[][] paths = calculator.BuildSequentialPaths();

                // Store the paths for printing
                _generatedPaths = paths;

                // Display the paths
                DisplayPaths(paths);

                // Show the Print button
                if (FindName("PrintPathsButton") is Button printButton)
                {
                    printButton.Visibility = Visibility.Visible;
                }
            }
            catch (InvalidOperationException ex)
            {
                // Display validation errors in the panel instead of a message box
                DisplayValidationError(ex.Message);
                
                // Hide the Print button on error
                if (FindName("PrintPathsButton") is Button printButton)
                {
                    printButton.Visibility = Visibility.Collapsed;
                }
                
                _generatedPaths = null;
            }
            catch (Exception ex)
            {
                // Display other errors in the panel
                DisplayValidationError($"Unexpected error building paths: {ex.Message}");
                
                // Hide the Print button on error
                if (FindName("PrintPathsButton") is Button printButton)
                {
                    printButton.Visibility = Visibility.Collapsed;
                }
                
                _generatedPaths = null;
            }
        }

        /// <summary>
        ///     Handles the Print Paths button click event
        /// </summary>
        private void PrintPathsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_generatedPaths == null || _generatedPaths.Length == 0)
            {
                MessageBox.Show("No paths available to print. Please build paths first.", 
                    "Print Paths", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Configure QuestPDF license (Community license for open source)
                QuestPDF.Settings.License = LicenseType.Community;

                // Ensure the temp directory exists
                string tempDir = Path.GetTempPath();
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                // Generate PDF to temp location
                string tempPdfPath = Path.Combine(tempDir, $"PathsReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                
                // Store the current directory and ensure we're in a valid directory
                string originalDirectory = Directory.GetCurrentDirectory();
                
                try
                {
                    // Change to temp directory for PDF generation
                    Directory.SetCurrentDirectory(tempDir);
                    
                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            // Set page to landscape
                            page.Size(PageSizes.A4.Landscape());
                            page.Margin(20);

                            page.Header().Element(ComposeHeader);
                            page.Content().Element(ComposeContent);
                            page.Footer().AlignCenter().Text(text =>
                            {
                                text.CurrentPageNumber();
                                text.Span(" / ");
                                text.TotalPages();
                            });
                        });
                    })
                    .GeneratePdf(tempPdfPath);
                }
                finally
                {
                    // Restore the original directory
                    try
                    {
                        Directory.SetCurrentDirectory(originalDirectory);
                    }
                    catch
                    {
                        // If we can't restore, not critical
                    }
                }

                // Verify the PDF was created
                if (!File.Exists(tempPdfPath))
                {
                    throw new FileNotFoundException("PDF file was not created successfully.");
                }

                // Open the PDF with the default PDF viewer
                var startInfo = new ProcessStartInfo
                {
                    FileName = tempPdfPath,
                    UseShellExecute = true
                };

                Process.Start(startInfo);

                MessageBox.Show($"PDF generated successfully and opened.\n\nFile saved to:\n{tempPdfPath}\n\nYou can print the PDF from your PDF viewer (usually Ctrl+P).", 
                    "Print Paths", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating PDF: {ex.Message}\n\nDetails: {ex.InnerException?.Message ?? "No additional details"}", 
                    "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        ///     Composes the PDF header
        /// </summary>
        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text($"Path Analysis Report - {_currentProject?.Name ?? "Untitled Project"}")
                        .FontSize(16).SemiBold();
                    column.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                        .FontSize(10);
                });
            });
        }

        /// <summary>
        ///     Composes the PDF content with all path tables
        /// </summary>
        private void ComposeContent(IContainer container)
        {
            if (_generatedPaths == null) return;

            container.Column(column =>
            {
                for (int i = 0; i < _generatedPaths.Length; i++)
                {
                    var path = _generatedPaths[i];

                    // Add spacing between paths
                    if (i > 0)
                    {
                        column.Item().PaddingVertical(10);
                    }

                    // Path header
                    column.Item().Text($"Path {i + 1}")
                        .FontSize(14).SemiBold();

                    column.Item().PaddingVertical(5);

                    // Create table for this path
                    column.Item().Table(table =>
                    {
                        // Define columns with specific widths
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(60);  // Block Name
                            columns.ConstantColumn(50);  // Block Type
                            columns.ConstantColumn(30);  // Block ID
                            columns.ConstantColumn(70);  // Equipment Name
                            columns.ConstantColumn(45);  // Distance From Source
                            columns.ConstantColumn(40);  // Added Distance
                            columns.ConstantColumn(45);  // Added Load
                            columns.ConstantColumn(45);  // Load at Point
                            columns.ConstantColumn(45);  // Ideal Voltage
                            columns.ConstantColumn(45);  // Voltage at Point
                            columns.ConstantColumn(45);  // Current at Point
                            columns.ConstantColumn(50);  // Theoretical V.Drop Rate
                            columns.ConstantColumn(50);  // Suggested Conductor
                            columns.ConstantColumn(50);  // Selected V.Drop Rate
                            columns.ConstantColumn(40);  // Voltage Drop
                            columns.ConstantColumn(40);  // Impedance
                            columns.ConstantColumn(40);  // Added Impedance
                        });

                        // Header row
                        table.Header(header =>
                        {
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("Block Name").FontSize(7).SemiBold();
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("Block Type").FontSize(7).SemiBold();
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("ID").FontSize(7).SemiBold();
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("Equipment").FontSize(7).SemiBold();
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("Dist. (km)").FontSize(7).SemiBold();
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("Add. D.").FontSize(7).SemiBold();
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("Add. Load").FontSize(7).SemiBold();
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("Load").FontSize(7).SemiBold();
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("Ideal V").FontSize(7).SemiBold();
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("V").FontSize(7).SemiBold();
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("I (A)").FontSize(7).SemiBold();
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("Theo. VD").FontSize(7).SemiBold();
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("Sugg. VD").FontSize(7).SemiBold();
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("Sel. VD").FontSize(7).SemiBold();
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("V Drop").FontSize(7).SemiBold();
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("Z (?)").FontSize(7).SemiBold();
                            header.Cell().Background(PdfColors.Grey.Lighten3).Padding(2).Text("Add Z").FontSize(7).SemiBold();
                        });

                        // Data rows
                        foreach (var point in path)
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.BlockName ?? "").FontSize(6);
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.BlockType ?? "").FontSize(6);
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.BlockID?.ToString() ?? "").FontSize(6);
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.EquipmentName ?? "").FontSize(6);
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.DistanceFromSource?.ToString("F3") ?? "").FontSize(6);
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.AddedDistance?.ToString("F3") ?? "").FontSize(6);
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.AddedLoad?.ToString("F0") ?? "").FontSize(6);
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.LoadAtPoint?.ToString("F0") ?? "").FontSize(6);
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.IdealVoltageAtPoint?.ToString("F0") ?? "").FontSize(6);
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.VoltageAtPoint?.ToString("F1") ?? "").FontSize(6);
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.CurrentAtPoint?.ToString("F2") ?? "").FontSize(6);
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.TheoreticalVoltageDropRate?.ToString("F2") ?? "").FontSize(6);
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.SuggestedConductorVoltageDropRate?.ToString("F2") ?? "").FontSize(6);
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.SelectedConductorVoltageDropRate?.ToString("F2") ?? "").FontSize(6);
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.VoltageDropAtPoint?.ToString("F2") ?? "").FontSize(6);
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.ImpedanceAtPoint?.ToString("F3") ?? "").FontSize(6);
                            table.Cell().BorderBottom(0.5f).BorderColor(PdfColors.Grey.Lighten2).Padding(2)
                                .Text(point.AddedImpedance?.ToString("F3") ?? "").FontSize(6);
                        }
                    });
                }
            });
        }

        /// <summary>
        ///     Displays a validation error message in the PathsDisplayPanel
        /// </summary>
        /// <param name="errorMessage">The error message to display</param>
        private void DisplayValidationError(string errorMessage)
        {
            // Clear existing content
            PathsDisplayPanel.Children.Clear();

            // Create error header
            var errorHeader = new TextBlock
            {
                Text = "? Validation Error",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = new SolidColorBrush(WpfColors.DarkRed),
                Margin = new Thickness(0, 0, 0, 10)
            };
            PathsDisplayPanel.Children.Add(errorHeader);

            // Create error message border
            var errorBorder = new Border
            {
                BorderBrush = new SolidColorBrush(WpfColors.Red),
                BorderThickness = new Thickness(2),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(15),
                Background = new SolidColorBrush(WpfColor.FromArgb(40, 255, 0, 0))
            };

            var errorText = new TextBlock
            {
                Text = errorMessage,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(WpfColors.DarkRed),
                FontSize = 12
            };

            errorBorder.Child = errorText;
            PathsDisplayPanel.Children.Add(errorBorder);

            // Add helpful guidance
            var guidanceHeader = new TextBlock
            {
                Text = "What to do:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 5)
            };
            PathsDisplayPanel.Children.Add(guidanceHeader);

            var guidanceText = new TextBlock
            {
                Text = "• Review your project connections\n" +
                       "• Ensure supply blocks don't share paths\n" +
                       "• Check that branched paths don't reconnect\n" +
                       "• Verify that each supply has its own isolated network",
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(WpfColors.Gray),
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(15, 0, 0, 0)
            };
            PathsDisplayPanel.Children.Add(guidanceText);
        }

        /// <summary>
        ///     Displays the sequential paths in the PathsDisplayPanel as tables
        /// </summary>
        /// <param name="paths">Array of paths to display, where each path is an array of PathPoints</param>
        private void DisplayPaths(PathPoint[][] paths)
        {
            // Clear existing content
            PathsDisplayPanel.Children.Clear();

            if (paths.Length == 0)
            {
                // No paths found
                var noPaths = new TextBlock
                {
                    Text = "No paths found. Make sure you have Supply blocks with connections.",
                    FontStyle = FontStyles.Italic,
                    Foreground = new SolidColorBrush(WpfColors.Gray),
                    Margin = new Thickness(10)
                };
                PathsDisplayPanel.Children.Add(noPaths);
                return;
            }

            // Add success indicator
            var successHeader = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var successIcon = new TextBlock
            {
                Text = "? ",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = new SolidColorBrush(WpfColors.Green),
                Margin = new Thickness(0, 0, 5, 0)
            };
            successHeader.Children.Add(successIcon);

            var header = new TextBlock
            {
                Text = $"Found {paths.Length} valid path(s):",
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            successHeader.Children.Add(header);

            PathsDisplayPanel.Children.Add(successHeader);

            // Display each path as a table
            for (int i = 0; i < paths.Length; i++)
            {
                var path = paths[i];

                // Create a border for each path
                var pathBorder = new Border
                {
                    BorderBrush = new SolidColorBrush(WpfColors.LightGray),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 0, 15),
                    Padding = new Thickness(10),
                    Background = new SolidColorBrush(WpfColor.FromArgb(25, 100, 100, 100))
                };

                var pathPanel = new StackPanel();

                // Path header
                var pathHeader = new TextBlock
                {
                    Text = $"Path {i + 1}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                pathPanel.Children.Add(pathHeader);

                // Create a DataGrid to display the path as a table
                var pathGrid = new DataGrid
                {
                    AutoGenerateColumns = false,
                    CanUserAddRows = false,
                    CanUserDeleteRows = false,
                    CanUserResizeRows = false,
                    IsReadOnly = true,
                    HeadersVisibility = DataGridHeadersVisibility.Column,
                    GridLinesVisibility = DataGridGridLinesVisibility.All,
                    HorizontalGridLinesBrush = new SolidColorBrush(WpfColors.LightGray),
                    VerticalGridLinesBrush = new SolidColorBrush(WpfColors.LightGray),
                    Background = new SolidColorBrush(WpfColors.White),
                    RowBackground = new SolidColorBrush(WpfColors.White),
                    AlternatingRowBackground = new SolidColorBrush(WpfColor.FromArgb(25, 200, 200, 200)),
                    BorderBrush = new SolidColorBrush(WpfColors.Gray),
                    BorderThickness = new Thickness(1)
                };

                // Define columns for each property - bind directly to PathPoint properties
                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Block Name",
                    Binding = new System.Windows.Data.Binding("BlockName"),
                    Width = new DataGridLength(120, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Block Type",
                    Binding = new System.Windows.Data.Binding("BlockType"),
                    Width = new DataGridLength(100, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Block ID",
                    Binding = new System.Windows.Data.Binding("BlockID"),
                    Width = new DataGridLength(70, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Equipment Name",
                    Binding = new System.Windows.Data.Binding("EquipmentName"),
                    Width = new DataGridLength(150, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Distance From Source (Km)",
                    Binding = new System.Windows.Data.Binding("DistanceFromSource") { StringFormat = "F3" },
                    Width = new DataGridLength(120, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Added Distance (Km)",
                    Binding = new System.Windows.Data.Binding("AddedDistance") { StringFormat = "F3" },
                    Width = new DataGridLength(110, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Added Load (VA)",
                    Binding = new System.Windows.Data.Binding("AddedLoad"),
                    Width = new DataGridLength(100, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Load at Point (VA)",
                    Binding = new System.Windows.Data.Binding("LoadAtPoint"),
                    Width = new DataGridLength(110, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Ideal Voltage (V)",
                    Binding = new System.Windows.Data.Binding("IdealVoltageAtPoint"),
                    Width = new DataGridLength(100, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Voltage at Point (V)",
                    Binding = new System.Windows.Data.Binding("VoltageAtPoint"),
                    Width = new DataGridLength(110, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Current at Point (A)",
                    Binding = new System.Windows.Data.Binding("CurrentAtPoint"),
                    Width = new DataGridLength(110, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Theoretical Voltage Drop Rate (mV/Am)",
                    Binding = new System.Windows.Data.Binding("TheoreticalVoltageDropRate"),
                    Width = new DataGridLength(180, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Suggested Conductor Voltage Drop Rate (mV/Am)",
                    Binding = new System.Windows.Data.Binding("SuggestedConductorVoltageDropRate"),
                    Width = new DataGridLength(220, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Selected Voltage Drop Rate (mV/Am)",
                    Binding = new System.Windows.Data.Binding("SelectedConductorVoltageDropRate"),
                    Width = new DataGridLength(180, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Voltage Drop (V)",
                    Binding = new System.Windows.Data.Binding("VoltageDropAtPoint"),
                    Width = new DataGridLength(100, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Impedance at Point (?)",
                    Binding = new System.Windows.Data.Binding("ImpedanceAtPoint"),
                    Width = new DataGridLength(130, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Added Impedance (?)",
                    Binding = new System.Windows.Data.Binding("AddedImpedance"),
                    Width = new DataGridLength(120, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Primary Voltage (V)",
                    Binding = new System.Windows.Data.Binding("PrimaryVoltage"),
                    Width = new DataGridLength(110, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Primary Current (V)",
                    Binding = new System.Windows.Data.Binding("PrimaryCurrent"),
                    Width = new DataGridLength(110, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Primary Transformer Impedance (?)",
                    Binding = new System.Windows.Data.Binding("PrimaryTransformerImpedance"),
                    Width = new DataGridLength(180, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Secondary Source Impedance (?)",
                    Binding = new System.Windows.Data.Binding("SecondarySourceImpedance"),
                    Width = new DataGridLength(180, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Fault Current (A)",
                    Binding = new System.Windows.Data.Binding("FaultCurrent"),
                    Width = new DataGridLength(110, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Minimum Circuit Breaker Rating",
                    Binding = new System.Windows.Data.Binding("MinimumCircuitBreakerRating"),
                    Width = new DataGridLength(180, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Selected Circuit Breaker Rating",
                    Binding = new System.Windows.Data.Binding("SelectedCircuitBreakerRating"),
                    Width = new DataGridLength(180, DataGridLengthUnitType.Pixel)
                });

                pathGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = "In",
                    Binding = new System.Windows.Data.Binding("In"),
                    Width = new DataGridLength(80, DataGridLengthUnitType.Pixel)
                });

                // Bind directly to the PathPoint array
                pathGrid.ItemsSource = path;
                pathPanel.Children.Add(pathGrid);

                pathBorder.Child = pathPanel;
                PathsDisplayPanel.Children.Add(pathBorder);
            }
        }
    }
}
