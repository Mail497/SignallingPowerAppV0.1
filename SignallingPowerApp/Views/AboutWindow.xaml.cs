using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using SignallingPowerApp.Core;

namespace SignallingPowerApp.Views
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// Displays application version information and version history
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            LoadVersionInformation();
        }

        /// <summary>
        /// Loads version information from AppVersion and populates the UI
        /// </summary>
        private void LoadVersionInformation()
        {
            // Get current version information
            var currentVersion = AppVersion.CurrentVersion;
            
            // Populate current version details
            CurrentVersionText.Text = $"{currentVersion.Major}.{currentVersion.Minor}";
            CurrentAuthorText.Text = currentVersion.Author;
            CurrentBuildDateText.Text = currentVersion.BuildDate.ToString("yyyy-MM-dd");
            CurrentDescriptionText.Text = currentVersion.Description;
            
            // Populate version history grid
            var versionHistory = AppVersion.VersionHistory
                .OrderByDescending(v => v.Major)
                .ThenByDescending(v => v.Minor)
                .Select(v => new VersionHistoryViewModel
                {
                    VersionNumber = $"{v.Major}.{v.Minor}",
                    BuildDate = v.BuildDate.ToString("yyyy-MM-dd"),
                    Author = v.Author,
                    Description = v.Description
                })
                .ToList();
            
            VersionHistoryGrid.ItemsSource = versionHistory;
        }

        /// <summary>
        /// Handles Close button click
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    /// <summary>
    /// ViewModel for displaying version history in the data grid
    /// </summary>
    public class VersionHistoryViewModel
    {
        public string VersionNumber { get; set; } = string.Empty;
        public string BuildDate { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
