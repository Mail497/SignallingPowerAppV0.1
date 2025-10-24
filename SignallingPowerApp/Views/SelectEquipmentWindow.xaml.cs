using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using SignallingPowerApp.Core;
using SignallingPowerApp.ViewModels;

namespace SignallingPowerApp.Views
{
    /// <summary>
    /// Interaction logic for SelectEquipmentWindow.xaml
    /// </summary>
    public partial class SelectEquipmentWindow : Window
    {
        private ICollectionView? _equipmentView;
        private object? _selectedEquipment;
        private string _equipmentType = "";

        public SelectEquipmentWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the selected equipment item
        /// </summary>
        public object? SelectedEquipment => _selectedEquipment;

        /// <summary>
        /// Sets up the window for Conductor selection
        /// </summary>
        public void SetupForConductorSelection(IEnumerable<Conductor> conductors, Conductor? currentSelection = null)
        {
            _equipmentType = "Conductor";
            TitleTextBlock.Text = "Select Conductor";
            SubtitleTextBlock.Text = "Choose a conductor from the list below";

            // Wrap conductors in ViewModels
            var viewModels = conductors.Select(c => new ConductorViewModel(c)).ToList();

            // Setup DataGrid columns
            EquipmentDataGrid.Columns.Clear();
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Description",
                Binding = new Binding("Description"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                MinWidth = 150
            });
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Cores",
                Binding = new Binding("Cores"),
                Width = new DataGridLength(80)
            });
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Strands",
                Binding = new Binding("StrandCount"),
                Width = new DataGridLength(80)
            });
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "CSA (mm²)",
                Binding = new Binding("CrossSectionalArea"),
                Width = new DataGridLength(100)
            });
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Custom",
                Binding = new Binding("IsCustomText"),
                Width = new DataGridLength(80)
            });

            _equipmentView = CollectionViewSource.GetDefaultView(viewModels);
            EquipmentDataGrid.ItemsSource = _equipmentView;

            // Select current equipment if provided
            if (currentSelection != null)
            {
                var matchingViewModel = viewModels.FirstOrDefault(vm => vm.Conductor == currentSelection);
                if (matchingViewModel != null)
                {
                    EquipmentDataGrid.SelectedItem = matchingViewModel;
                    EquipmentDataGrid.ScrollIntoView(matchingViewModel);
                }
            }
        }

        /// <summary>
        /// Sets up the window for TransformerUPS selection
        /// </summary>
        public void SetupForTransformerSelection(IEnumerable<TransformerUPS> transformers, TransformerUPS? currentSelection = null)
        {
            _equipmentType = "TransformerUPS";
            TitleTextBlock.Text = "Select Transformer/UPS";
            SubtitleTextBlock.Text = "Choose a transformer/UPS from the list below";

            // Wrap transformers in ViewModels
            var viewModels = transformers.Select(t => new TransformerUPSViewModel(t)).ToList();

            // Setup DataGrid columns
            EquipmentDataGrid.Columns.Clear();
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Description",
                Binding = new Binding("Description"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                MinWidth = 150
            });
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Rating (kVA)",
                Binding = new Binding("Rating"),
                Width = new DataGridLength(120)
            });
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "%Z",
                Binding = new Binding("PercentageZ"),
                Width = new DataGridLength(80)
            });
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Primary (V)",
                Binding = new Binding("PrimaryVoltage"),
                Width = new DataGridLength(110)
            });
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Secondary (V)",
                Binding = new Binding("SecondaryVoltage"),
                Width = new DataGridLength(120)
            });
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Custom",
                Binding = new Binding("IsCustomText"),
                Width = new DataGridLength(80)
            });

            _equipmentView = CollectionViewSource.GetDefaultView(viewModels);
            EquipmentDataGrid.ItemsSource = _equipmentView;

            // Select current equipment if provided
            if (currentSelection != null)
            {
                var matchingViewModel = viewModels.FirstOrDefault(vm => vm.Transformer == currentSelection);
                if (matchingViewModel != null)
                {
                    EquipmentDataGrid.SelectedItem = matchingViewModel;
                    EquipmentDataGrid.ScrollIntoView(matchingViewModel);
                }
            }
        }

        /// <summary>
        /// Sets up the window for Alternator selection
        /// </summary>
        public void SetupForAlternatorSelection(IEnumerable<Alternator> alternators, Alternator? currentSelection = null)
        {
            _equipmentType = "Alternator";
            TitleTextBlock.Text = "Select Alternator";
            SubtitleTextBlock.Text = "Choose an alternator from the list below";

            // Wrap alternators in ViewModels
            var viewModels = alternators.Select(a => new AlternatorViewModel(a)).ToList();

            // Setup DataGrid columns
            EquipmentDataGrid.Columns.Clear();
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Description",
                Binding = new Binding("Description"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                MinWidth = 150
            });
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Rating (VA)",
                Binding = new Binding("RatingVA"),
                Width = new DataGridLength(120)
            });
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Rating (W)",
                Binding = new Binding("RatingW"),
                Width = new DataGridLength(120)
            });
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Custom",
                Binding = new Binding("IsCustomText"),
                Width = new DataGridLength(80)
            });

            _equipmentView = CollectionViewSource.GetDefaultView(viewModels);
            EquipmentDataGrid.ItemsSource = _equipmentView;

            // Select current equipment if provided
            if (currentSelection != null)
            {
                var matchingViewModel = viewModels.FirstOrDefault(vm => vm.Alternator == currentSelection);
                if (matchingViewModel != null)
                {
                    EquipmentDataGrid.SelectedItem = matchingViewModel;
                    EquipmentDataGrid.ScrollIntoView(matchingViewModel);
                }
            }
        }

        /// <summary>
        /// Sets up the window for Consumer selection
        /// </summary>
        public void SetupForConsumerSelection(IEnumerable<Consumer> consumers, Consumer? currentSelection = null)
        {
            _equipmentType = "Consumer";
            TitleTextBlock.Text = "Select Consumer";
            SubtitleTextBlock.Text = "Choose a consumer from the list below";

            // Wrap consumers in ViewModels
            var viewModels = consumers.Select(c => new ConsumerViewModel(c)).ToList();

            // Setup DataGrid columns
            EquipmentDataGrid.Columns.Clear();
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Name",
                Binding = new Binding("Name"),
                Width = new DataGridLength(200)
            });
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Description",
                Binding = new Binding("Description"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                MinWidth = 150
            });
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Load (VA)",
                Binding = new Binding("Load"),
                Width = new DataGridLength(120)
            });
            EquipmentDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Custom",
                Binding = new Binding("IsCustomText"),
                Width = new DataGridLength(80)
            });

            _equipmentView = CollectionViewSource.GetDefaultView(viewModels);
            EquipmentDataGrid.ItemsSource = _equipmentView;

            // Select current equipment if provided
            if (currentSelection != null)
            {
                var matchingViewModel = viewModels.FirstOrDefault(vm => vm.Consumer == currentSelection);
                if (matchingViewModel != null)
                {
                    EquipmentDataGrid.SelectedItem = matchingViewModel;
                    EquipmentDataGrid.ScrollIntoView(matchingViewModel);
                }
            }
        }

        /// <summary>
        /// Handles search box text changed event
        /// </summary>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower();

            if (_equipmentView != null)
            {
                _equipmentView.Filter = item =>
                {
                    if (string.IsNullOrWhiteSpace(searchText))
                        return true;

                    switch (_equipmentType)
                    {
                        case "Conductor":
                            if (item is ConductorViewModel conductor)
                            {
                                return conductor.Description.ToLower().Contains(searchText) ||
                                       conductor.Cores.ToString().Contains(searchText) ||
                                       conductor.StrandCount.ToString().Contains(searchText) ||
                                       conductor.CrossSectionalArea.ToString().Contains(searchText) ||
                                       conductor.IsCustomText.ToLower().Contains(searchText);
                            }
                            break;

                        case "TransformerUPS":
                            if (item is TransformerUPSViewModel transformer)
                            {
                                return transformer.Description.ToLower().Contains(searchText) ||
                                       transformer.Rating.ToString().Contains(searchText) ||
                                       transformer.PercentageZ.ToString().Contains(searchText) ||
                                       transformer.PrimaryVoltage.ToString().Contains(searchText) ||
                                       transformer.SecondaryVoltage.ToString().Contains(searchText) ||
                                       transformer.IsCustomText.ToLower().Contains(searchText);
                            }
                            break;

                        case "Alternator":
                            if (item is AlternatorViewModel alternator)
                            {
                                return alternator.Description.ToLower().Contains(searchText) ||
                                       alternator.RatingVA.ToString().Contains(searchText) ||
                                       alternator.RatingW.ToString().Contains(searchText) ||
                                       alternator.IsCustomText.ToLower().Contains(searchText);
                            }
                            break;

                        case "Consumer":
                            if (item is ConsumerViewModel consumer)
                            {
                                return consumer.Name.ToLower().Contains(searchText) ||
                                       consumer.Description.ToLower().Contains(searchText) ||
                                       consumer.Load.ToString().Contains(searchText) ||
                                       consumer.IsCustomText.ToLower().Contains(searchText);
                            }
                            break;
                    }

                    return false;
                };
            }
        }

        /// <summary>
        /// Handles clear search button click
        /// </summary>
        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
        }

        /// <summary>
        /// Handles equipment selection changed
        /// </summary>
        private void EquipmentDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectButton.IsEnabled = EquipmentDataGrid.SelectedItem != null;
        }

        /// <summary>
        /// Handles double-click to select
        /// </summary>
        private void EquipmentDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EquipmentDataGrid.SelectedItem != null)
            {
                SelectEquipment();
            }
        }

        /// <summary>
        /// Handles Select button click
        /// </summary>
        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectEquipment();
        }

        /// <summary>
        /// Handles Clear button click
        /// </summary>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _selectedEquipment = null;
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Handles Cancel button click
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Selects the currently selected equipment and closes the dialog
        /// </summary>
        private void SelectEquipment()
        {
            if (EquipmentDataGrid.SelectedItem == null)
                return;

            // Extract the actual equipment from the ViewModel
            switch (_equipmentType)
            {
                case "Conductor":
                    _selectedEquipment = (EquipmentDataGrid.SelectedItem as ConductorViewModel)?.Conductor;
                    break;
                case "TransformerUPS":
                    _selectedEquipment = (EquipmentDataGrid.SelectedItem as TransformerUPSViewModel)?.Transformer;
                    break;
                case "Alternator":
                    _selectedEquipment = (EquipmentDataGrid.SelectedItem as AlternatorViewModel)?.Alternator;
                    break;
                case "Consumer":
                    _selectedEquipment = (EquipmentDataGrid.SelectedItem as ConsumerViewModel)?.Consumer;
                    break;
            }

            DialogResult = true;
            Close();
        }
    }
}
