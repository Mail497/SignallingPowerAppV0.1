using System.Windows;
using System.Windows.Controls;

namespace SignallingPowerApp.Views
{
    /// <summary>
    /// Interaction logic for AddEquipmentDialog.xaml
    /// </summary>
    public partial class AddEquipmentDialog : Window
    {
        public AddEquipmentDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the selected equipment type
        /// </summary>
        public string SelectedEquipmentType
        {
            get
            {
                if (EquipmentTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    return selectedItem.Content.ToString() ?? "Conductor";
                }
                return "Conductor";
            }
        }

        /// <summary>
        /// Handles OK button click
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
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
    }
}
