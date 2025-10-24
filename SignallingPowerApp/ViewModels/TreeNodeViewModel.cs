using System.Collections.ObjectModel;
using System.ComponentModel;
using SignallingPowerApp.Core;

namespace SignallingPowerApp.ViewModels
{
    /// <summary>
    ///     View model for tree view nodes representing project hierarchy
    /// </summary>
    public class TreeNodeViewModel : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private bool _isSelected;

        public TreeNodeViewModel(string header, object? data = null)
        {
            Header = header;
            Data = data;
            Children = new ObservableCollection<TreeNodeViewModel>();
        }

        /// <summary>
        ///     Display text for the tree node
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        ///     Associated data object (IBlock, Project, etc.)
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        ///     Child nodes
        /// </summary>
        public ObservableCollection<TreeNodeViewModel> Children { get; set; }

        /// <summary>
        ///     Whether the node is expanded
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged(nameof(IsExpanded));
                }
            }
        }

        /// <summary>
        ///     Whether the node is selected
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
