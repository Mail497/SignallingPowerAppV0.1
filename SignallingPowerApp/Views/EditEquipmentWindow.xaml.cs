using System.Windows;
using System.Windows.Controls;
using SignallingPowerApp.Core;
using SignallingPowerApp.ViewModels;

namespace SignallingPowerApp.Views
{
    /// <summary>
    /// Interaction logic for EditEquipmentWindow.xaml
    /// </summary>
    public partial class EditEquipmentWindow : Window
    {
        private object? _viewModel;
        private bool _saveClicked = false;
        private bool _isAddMode = false;
        private bool _removeClicked = false;
        private Conductor? _newConductor;
        private TransformerUPS? _newTransformer;
        private Alternator? _newAlternator;
        private Consumer? _newConsumer;

        public EditEquipmentWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets whether the user clicked Save
        /// </summary>
        public bool SaveClicked => _saveClicked;

        /// <summary>
        /// Gets whether the user clicked Remove
        /// </summary>
        public bool RemoveClicked => _removeClicked;

        /// <summary>
        /// Gets the newly created Conductor (only valid in add mode)
        /// </summary>
        public Conductor? NewConductor => _newConductor;

        /// <summary>
        /// Gets the newly created TransformerUPS (only valid in add mode)
        /// </summary>
        public TransformerUPS? NewTransformer => _newTransformer;

        /// <summary>
        /// Gets the newly created Alternator (only valid in add mode)
        /// </summary>
        public Alternator? NewAlternator => _newAlternator;

        /// <summary>
        /// Gets the newly created Consumer (only valid in add mode)
        /// </summary>
        public Consumer? NewConsumer => _newConsumer;

        /// <summary>
        /// Sets up the window for adding a new Conductor
        /// </summary>
        public void SetupForNewConductor()
        {
            _isAddMode = true;
            RemoveButton.Visibility = Visibility.Collapsed;
            _newConductor = new Conductor(custom: true);
            var viewModel = new ConductorViewModel(_newConductor);
            _viewModel = viewModel;
            TitleTextBlock.Text = "Add New Conductor";
            BuildConductorForm(viewModel, false);
        }

        /// <summary>
        /// Sets up the window for adding a new TransformerUPS
        /// </summary>
        public void SetupForNewTransformer()
        {
            _isAddMode = true;
            RemoveButton.Visibility = Visibility.Collapsed;
            _newTransformer = new TransformerUPS(custom: true);
            var viewModel = new TransformerUPSViewModel(_newTransformer);
            _viewModel = viewModel;
            TitleTextBlock.Text = "Add New Transformer/UPS";
            BuildTransformerForm(viewModel, false);
        }

        /// <summary>
        /// Sets up the window for adding a new Alternator
        /// </summary>
        public void SetupForNewAlternator()
        {
            _isAddMode = true;
            RemoveButton.Visibility = Visibility.Collapsed;
            _newAlternator = new Alternator(custom: true);
            var viewModel = new AlternatorViewModel(_newAlternator);
            _viewModel = viewModel;
            TitleTextBlock.Text = "Add New Alternator";
            BuildAlternatorForm(viewModel, false);
        }

        /// <summary>
        /// Sets up the window for adding a new Consumer
        /// </summary>
        public void SetupForNewConsumer()
        {
            _isAddMode = true;
            RemoveButton.Visibility = Visibility.Collapsed;
            _newConsumer = new Consumer(custom: true);
            var viewModel = new ConsumerViewModel(_newConsumer);
            _viewModel = viewModel;
            TitleTextBlock.Text = "Add New Consumer";
            BuildConsumerForm(viewModel, false);
        }

        /// <summary>
        /// Sets up the window for editing a Conductor
        /// </summary>
        public void SetupForConductor(ConductorViewModel viewModel)
        {
            _isAddMode = false;
            RemoveButton.Visibility = Visibility.Visible;
            _viewModel = viewModel;
            TitleTextBlock.Text = "Edit Conductor";
            BuildConductorForm(viewModel, true);
        }

        /// <summary>
        /// Builds the form for Conductor
        /// </summary>
        private void BuildConductorForm(ConductorViewModel viewModel, bool showCustomField)
        {
            FormContent.Children.Clear();
            
            if (showCustomField)
            {
                // Custom indicator (read-only)
                var customLabel = new Label { Content = "Custom:" };
                var customText = new TextBox 
                { 
                    Text = viewModel.IsCustomText, 
                    IsReadOnly = true,
                    Background = System.Windows.Media.Brushes.LightGray
                };
                FormContent.Children.Add(customLabel);
                FormContent.Children.Add(customText);
            }
            
            // Cores
            var coresLabel = new Label { Content = "Cores:" };
            var coresBox = new TextBox { Name = "CoresBox", Text = viewModel.Cores.ToString() };
            FormContent.Children.Add(coresLabel);
            FormContent.Children.Add(coresBox);
            
            // Strand Count
            var strandCountLabel = new Label { Content = "Strand Count:" };
            var strandCountBox = new TextBox { Name = "StrandCountBox", Text = viewModel.StrandCount.ToString() };
            FormContent.Children.Add(strandCountLabel);
            FormContent.Children.Add(strandCountBox);
            
            // Strand Diameter
            var strandDiameterLabel = new Label { Content = "Strand Diameter (mm):" };
            var strandDiameterBox = new TextBox { Name = "StrandDiameterBox", Text = viewModel.StrandDiameter.ToString() };
            FormContent.Children.Add(strandDiameterLabel);
            FormContent.Children.Add(strandDiameterBox);
            
            // Cross Sectional Area
            var csaLabel = new Label { Content = "Cross Sectional Area (mm²):" };
            var csaBox = new TextBox { Name = "CrossSectionalAreaBox", Text = viewModel.CrossSectionalArea.ToString() };
            FormContent.Children.Add(csaLabel);
            FormContent.Children.Add(csaBox);
            
            // Description
            var descLabel = new Label { Content = "Description:" };
            var descBox = new TextBox { Name = "DescriptionBox", Text = viewModel.Description };
            FormContent.Children.Add(descLabel);
            FormContent.Children.Add(descBox);
            
            // Voltage Drop 60
            var vd60Label = new Label { Content = "Voltage Drop 60°C:" };
            var vd60Box = new TextBox { Name = "VoltageDrop60Box", Text = viewModel.VoltageDrop60.ToString() };
            FormContent.Children.Add(vd60Label);
            FormContent.Children.Add(vd60Box);
            
            // Voltage Drop 90
            var vd90Label = new Label { Content = "Voltage Drop 90°C:" };
            var vd90Box = new TextBox { Name = "VoltageDrop90Box", Text = viewModel.VoltageDrop90.ToString() };
            FormContent.Children.Add(vd90Label);
            FormContent.Children.Add(vd90Box);
            
            // Reactance
            var reactanceLabel = new Label { Content = "Reactance:" };
            var reactanceBox = new TextBox { Name = "ReactanceBox", Text = viewModel.Reactance.ToString() };
            FormContent.Children.Add(reactanceLabel);
            FormContent.Children.Add(reactanceBox);
            
            // Resistance 60
            var r60Label = new Label { Content = "Resistance 60°C:" };
            var r60Box = new TextBox { Name = "Resistance60Box", Text = viewModel.Resistance60.ToString() };
            FormContent.Children.Add(r60Label);
            FormContent.Children.Add(r60Box);
            
            // Resistance 90
            var r90Label = new Label { Content = "Resistance 90°C:" };
            var r90Box = new TextBox { Name = "Resistance90Box", Text = viewModel.Resistance90.ToString() };
            FormContent.Children.Add(r90Label);
            FormContent.Children.Add(r90Box);
        }

        /// <summary>
        /// Sets up the window for editing a TransformerUPS
        /// </summary>
        public void SetupForTransformer(TransformerUPSViewModel viewModel)
        {
            _isAddMode = false;
            RemoveButton.Visibility = Visibility.Visible;
            _viewModel = viewModel;
            TitleTextBlock.Text = "Edit Transformer/UPS";
            BuildTransformerForm(viewModel, true);
        }

        /// <summary>
        /// Builds the form for TransformerUPS
        /// </summary>
        private void BuildTransformerForm(TransformerUPSViewModel viewModel, bool showCustomField)
        {
            FormContent.Children.Clear();
            
            if (showCustomField)
            {
                // Custom indicator (read-only)
                var customLabel = new Label { Content = "Custom:" };
                var customText = new TextBox 
                { 
                    Text = viewModel.IsCustomText, 
                    IsReadOnly = true,
                    Background = System.Windows.Media.Brushes.LightGray
                };
                FormContent.Children.Add(customLabel);
                FormContent.Children.Add(customText);
            }
            
            // Rating
            var ratingLabel = new Label { Content = "Rating (kVA):" };
            var ratingBox = new TextBox { Name = "RatingBox", Text = viewModel.Rating.ToString() };
            FormContent.Children.Add(ratingLabel);
            FormContent.Children.Add(ratingBox);
            
            // Percentage Z
            var percentageZLabel = new Label { Content = "% Z:" };
            var percentageZBox = new TextBox { Name = "PercentageZBox", Text = viewModel.PercentageZ.ToString() };
            FormContent.Children.Add(percentageZLabel);
            FormContent.Children.Add(percentageZBox);
            
            // Primary Voltage
            var primaryVoltageLabel = new Label { Content = "Primary Voltage (V):" };
            var primaryVoltageBox = new TextBox { Name = "PrimaryVoltageBox", Text = viewModel.PrimaryVoltage.ToString() };
            FormContent.Children.Add(primaryVoltageLabel);
            FormContent.Children.Add(primaryVoltageBox);
            
            // Secondary Voltage
            var secondaryVoltageLabel = new Label { Content = "Secondary Voltage (V):" };
            var secondaryVoltageBox = new TextBox { Name = "SecondaryVoltageBox", Text = viewModel.SecondaryVoltage.ToString() };
            FormContent.Children.Add(secondaryVoltageLabel);
            FormContent.Children.Add(secondaryVoltageBox);
            
            // Description
            var descLabel = new Label { Content = "Description:" };
            var descBox = new TextBox { Name = "DescriptionBox", Text = viewModel.Description };
            FormContent.Children.Add(descLabel);
            FormContent.Children.Add(descBox);
        }

        /// <summary>
        /// Sets up the window for editing an Alternator
        /// </summary>
        public void SetupForAlternator(AlternatorViewModel viewModel)
        {
            _isAddMode = false;
            RemoveButton.Visibility = Visibility.Visible;
            _viewModel = viewModel;
            TitleTextBlock.Text = "Edit Alternator";
            BuildAlternatorForm(viewModel, true);
        }

        /// <summary>
        /// Builds the form for Alternator
        /// </summary>
        private void BuildAlternatorForm(AlternatorViewModel viewModel, bool showCustomField)
        {
            FormContent.Children.Clear();
            
            if (showCustomField)
            {
                // Custom indicator (read-only)
                var customLabel = new Label { Content = "Custom:" };
                var customText = new TextBox 
                { 
                    Text = viewModel.IsCustomText, 
                    IsReadOnly = true,
                    Background = System.Windows.Media.Brushes.LightGray
                };
                FormContent.Children.Add(customLabel);
                FormContent.Children.Add(customText);
            }
            
            // Rating VA
            var ratingVALabel = new Label { Content = "Rating (VA):" };
            var ratingVABox = new TextBox { Name = "RatingVABox", Text = viewModel.RatingVA.ToString() };
            FormContent.Children.Add(ratingVALabel);
            FormContent.Children.Add(ratingVABox);
            
            // Rating W
            var ratingWLabel = new Label { Content = "Rating (W):" };
            var ratingWBox = new TextBox { Name = "RatingWBox", Text = viewModel.RatingW.ToString() };
            FormContent.Children.Add(ratingWLabel);
            FormContent.Children.Add(ratingWBox);
            
            // Description
            var descLabel = new Label { Content = "Description:" };
            var descBox = new TextBox { Name = "DescriptionBox", Text = viewModel.Description };
            FormContent.Children.Add(descLabel);
            FormContent.Children.Add(descBox);
        }

        /// <summary>
        /// Sets up the window for editing a Consumer
        /// </summary>
        public void SetupForConsumer(ConsumerViewModel viewModel)
        {
            _isAddMode = false;
            RemoveButton.Visibility = Visibility.Visible;
            _viewModel = viewModel;
            TitleTextBlock.Text = "Edit Consumer";
            BuildConsumerForm(viewModel, true);
        }

        /// <summary>
        /// Builds the form for Consumer
        /// </summary>
        private void BuildConsumerForm(ConsumerViewModel viewModel, bool showCustomField)
        {
            FormContent.Children.Clear();
            
            if (showCustomField)
            {
                // Custom indicator (read-only)
                var customLabel = new Label { Content = "Custom:" };
                var customText = new TextBox 
                { 
                    Text = viewModel.IsCustomText, 
                    IsReadOnly = true,
                    Background = System.Windows.Media.Brushes.LightGray
                };
                FormContent.Children.Add(customLabel);
                FormContent.Children.Add(customText);
            }
            
            // Name
            var nameLabel = new Label { Content = "Name:" };
            var nameBox = new TextBox { Name = "NameBox", Text = viewModel.Name };
            FormContent.Children.Add(nameLabel);
            FormContent.Children.Add(nameBox);
            
            // Description
            var descLabel = new Label { Content = "Description:" };
            var descBox = new TextBox { Name = "DescriptionBox", Text = viewModel.Description };
            FormContent.Children.Add(descLabel);
            FormContent.Children.Add(descBox);
            
            // Load
            var loadLabel = new Label { Content = "Load (VA):" };
            var loadBox = new TextBox { Name = "LoadBox", Text = viewModel.Load.ToString() };
            FormContent.Children.Add(loadLabel);
            FormContent.Children.Add(loadBox);
        }

        /// <summary>
        /// Handles Save button click
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel is ConductorViewModel conductorVM)
                {
                    SaveConductor(conductorVM);
                }
                else if (_viewModel is TransformerUPSViewModel transformerVM)
                {
                    SaveTransformer(transformerVM);
                }
                else if (_viewModel is AlternatorViewModel alternatorVM)
                {
                    SaveAlternator(alternatorVM);
                }
                else if (_viewModel is ConsumerViewModel consumerVM)
                {
                    SaveConsumer(consumerVM);
                }

                _saveClicked = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving equipment: {ex.Message}", 
                    "Validation Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Saves changes to a Conductor
        /// </summary>
        private void SaveConductor(ConductorViewModel viewModel)
        {
            viewModel.Cores = int.Parse(FindTextBox("CoresBox").Text);
            viewModel.StrandCount = int.Parse(FindTextBox("StrandCountBox").Text);
            viewModel.StrandDiameter = double.Parse(FindTextBox("StrandDiameterBox").Text);
            viewModel.CrossSectionalArea = double.Parse(FindTextBox("CrossSectionalAreaBox").Text);
            viewModel.Description = FindTextBox("DescriptionBox").Text;
            viewModel.VoltageDrop60 = double.Parse(FindTextBox("VoltageDrop60Box").Text);
            viewModel.VoltageDrop90 = double.Parse(FindTextBox("VoltageDrop90Box").Text);
            viewModel.Reactance = double.Parse(FindTextBox("ReactanceBox").Text);
            viewModel.Resistance60 = double.Parse(FindTextBox("Resistance60Box").Text);
            viewModel.Resistance90 = double.Parse(FindTextBox("Resistance90Box").Text);
        }

        /// <summary>
        /// Saves changes to a TransformerUPS
        /// </summary>
        private void SaveTransformer(TransformerUPSViewModel viewModel)
        {
            viewModel.Rating = int.Parse(FindTextBox("RatingBox").Text);
            viewModel.PercentageZ = double.Parse(FindTextBox("PercentageZBox").Text);
            viewModel.PrimaryVoltage = int.Parse(FindTextBox("PrimaryVoltageBox").Text);
            viewModel.SecondaryVoltage = int.Parse(FindTextBox("SecondaryVoltageBox").Text);
            viewModel.Description = FindTextBox("DescriptionBox").Text;
        }

        /// <summary>
        /// Saves changes to an Alternator
        /// </summary>
        private void SaveAlternator(AlternatorViewModel viewModel)
        {
            viewModel.RatingVA = int.Parse(FindTextBox("RatingVABox").Text);
            viewModel.RatingW = int.Parse(FindTextBox("RatingWBox").Text);
            viewModel.Description = FindTextBox("DescriptionBox").Text;
        }

        /// <summary>
        /// Saves changes to a Consumer
        /// </summary>
        private void SaveConsumer(ConsumerViewModel viewModel)
        {
            viewModel.Name = FindTextBox("NameBox").Text;
            viewModel.Description = FindTextBox("DescriptionBox").Text;
            viewModel.Load = int.Parse(FindTextBox("LoadBox").Text);
        }

        /// <summary>
        /// Finds a TextBox by name in the form content
        /// </summary>
        private TextBox FindTextBox(string name)
        {
            foreach (var child in FormContent.Children)
            {
                if (child is TextBox textBox && textBox.Name == name)
                {
                    return textBox;
                }
            }
            throw new InvalidOperationException($"TextBox with name '{name}' not found");
        }

        /// <summary>
        /// Handles Remove button click
        /// </summary>
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            // Confirm the removal
            var result = MessageBox.Show(
                "Are you sure you want to remove this equipment item? This action cannot be undone.",
                "Confirm Removal",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _removeClicked = true;
                _saveClicked = false;
                DialogResult = true;
                Close();
            }
        }

        /// <summary>
        /// Handles Cancel button click
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _saveClicked = false;
            _removeClicked = false;
            DialogResult = false;
            Close();
        }
    }
}
