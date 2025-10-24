using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using SignallingPowerApp.Core;
using SignallingPowerApp.ViewModels;

namespace SignallingPowerApp
{
    /// <summary>
    /// TreeView management for MainWindow
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        ///     Populates the tree view with the current project structure
        /// </summary>
        private void PopulateTreeView()
        {
            TreeViewItems.Clear();

            if (_currentProject == null)
                return;

            // Create root project node
            var projectNode = new TreeNodeViewModel($"Project", _currentProject)
            {
                IsExpanded = true
            };

            // Create Supplies section
            var suppliesNode = new TreeNodeViewModel("Supplies");
            foreach (var block in _currentProject.GetAllBlocks)
            {
                if (block is Supply supply)
                {
                    var supplyNode = new TreeNodeViewModel($"Supply [{supply.Name}]", supply);
                    suppliesNode.Children.Add(supplyNode);
                }
            }
            if (suppliesNode.Children.Count > 0)
            {
                suppliesNode.IsExpanded = true;
                projectNode.Children.Add(suppliesNode);
            }

            // Create Alternators section
            var alternatorsNode = new TreeNodeViewModel("Alternators");
            foreach (var block in _currentProject.GetAllBlocks)
            {
                if (block is AlternatorBlock alternator)
                {
                    var alternatorNode = new TreeNodeViewModel($"Alternator [{alternator.Name}]", alternator);
                    alternatorsNode.Children.Add(alternatorNode);
                }
            }
            if (alternatorsNode.Children.Count > 0)
            {
                alternatorsNode.IsExpanded = true;
                projectNode.Children.Add(alternatorsNode);
            }

            // Create Conductors section
            var conductorsNode = new TreeNodeViewModel("Conductors");
            foreach (var block in _currentProject.GetAllBlocks)
            {
                if (block is ConductorBlock conductor)
                {
                    var conductorNode = new TreeNodeViewModel($"Conductor [{conductor.Name}]", conductor);
                    conductorsNode.Children.Add(conductorNode);
                }
            }
            if (conductorsNode.Children.Count > 0)
            {
                conductorsNode.IsExpanded = true;
                projectNode.Children.Add(conductorsNode);
            }

            // Create Locations section
            var locationsNode = new TreeNodeViewModel("Locations");
            foreach (var block in _currentProject.GetAllBlocks)
            {
                if (block is Location location)
                {
                    var locationNode = CreateLocationNode(location);
                    locationsNode.Children.Add(locationNode);
                }
            }
            if (locationsNode.Children.Count > 0)
            {
                locationsNode.IsExpanded = true;
                projectNode.Children.Add(locationsNode);
            }

            TreeViewItems.Add(projectNode);
        }

        /// <summary>
        ///     Creates a tree node for a location and its children
        /// </summary>
        private TreeNodeViewModel CreateLocationNode(Location location)
        {
            var locationNode = new TreeNodeViewModel($"{location.Name}", location);

            // Add busbars and transformers
            foreach (var child in location.GetChildren())
            {
                if (child is Busbar busbar)
                {
                    var busbarNode = CreateBusbarNode(busbar);
                    locationNode.Children.Add(busbarNode);
                }
                else if (child is TransformerUPSBlock transformer)
                {
                    var transformerNode = new TreeNodeViewModel($"TransformerUPS [{transformer.Name}]", transformer);
                    locationNode.Children.Add(transformerNode);
                }
            }

            // Add loads that belong to this location
            // Loads are parented to the location directly, so we can get them from GetChildren()
            foreach (var child in location.GetChildren())
            {
                if (child is Load load)
                {
                    var loadNode = new TreeNodeViewModel($"Load [{load.Name}]", load);
                    locationNode.Children.Add(loadNode);
                }
            }

            return locationNode;
        }

        /// <summary>
        ///     Creates a tree node for a busbar and its rows
        /// </summary>
        private TreeNodeViewModel CreateBusbarNode(Busbar busbar)
        {
            var busbarNode = new TreeNodeViewModel($"Busbar [{busbar.Name}]", busbar);

            // Add rows
            foreach (var row in busbar.GetRows())
            {
                var rowNode = new TreeNodeViewModel($"Row [{row.Type}]", row);
                busbarNode.Children.Add(rowNode);
            }

            return busbarNode;
        }

        /// <summary>
        ///     Updates the tree view with current project data
        /// </summary>
        public void RefreshTreeView()
        {
            PopulateTreeView();
        }

        /// <summary>
        ///     Handles tree view selection changed event
        /// </summary>
        private void ProjectTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeNodeViewModel selectedNode)
            {
                // Update properties grid or other UI based on selected item
                UpdatePropertiesView(selectedNode.Data);
                
                // If a location is selected, highlight it on the canvas
                if (selectedNode.Data is Location location)
                {
                    SelectLocation(location);
                }
                // If a supply is selected, highlight it on the canvas
                else if (selectedNode.Data is Supply supply)
                {
                    SelectSupply(supply);
                }
                // If an alternator is selected, highlight it on the canvas
                else if (selectedNode.Data is AlternatorBlock alternator)
                {
                    SelectAlternator(alternator);
                }
                // If a conductor is selected, highlight it on the canvas
                else if (selectedNode.Data is ConductorBlock conductor)
                {
                    SelectConductor(conductor);
                }
                // If a busbar is selected, highlight it on the canvas
                else if (selectedNode.Data is Busbar busbar)
                {
                    SelectBusbar(busbar, null);
                }
                // If a transformer is selected, highlight it on the canvas
                else if (selectedNode.Data is TransformerUPSBlock transformer)
                {
                    SelectTransformerUPS(transformer, null);
                }
                // If a row is selected, highlight it on the canvas
                else if (selectedNode.Data is Row row)
                {
                    SelectRow(row, null);
                }
                // If a load is selected, highlight it on the canvas
                else if (selectedNode.Data is Load load)
                {
                    SelectLoad(load, null);
                }
                else
                {
                    // Clear selection if not a selectable block
                    DeselectAll();
                }
            }
        }

        /// <summary>
        ///     Recursively deselects all tree view items
        /// </summary>
        private void DeselectAllTreeViewItems(ObservableCollection<TreeNodeViewModel> items)
        {
            foreach (var item in items)
            {
                item.IsSelected = false;
                if (item.Children.Count > 0)
                {
                    DeselectAllTreeViewItems(item.Children);
                }
            }
        }

        /// <summary>
        ///     Highlights the location in the tree view
        /// </summary>
        private void HighlightLocationInTreeView(Location location)
        {
            // First, deselect all items
            DeselectAllTreeViewItems(TreeViewItems);

            // Find and select the location node
            foreach (var rootNode in TreeViewItems)
            {
                if (SelectLocationNode(rootNode, location))
                {
                    rootNode.IsExpanded = true;
                    break;
                }
            }
        }

        /// <summary>
        ///     Recursively searches for and selects the location node
        /// </summary>
        private bool SelectLocationNode(TreeNodeViewModel node, Location location)
        {
            if (node.Data is Location loc && loc == location)
            {
                node.IsSelected = true;
                node.IsExpanded = true;
                
                // Expand parent nodes
                ExpandParentNodes(node);
                return true;
            }

            foreach (var child in node.Children)
            {
                if (SelectLocationNode(child, location))
                {
                    node.IsExpanded = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Highlights the supply in the tree view
        /// </summary>
        private void HighlightSupplyInTreeView(Supply supply)
        {
            // First, deselect all items
            DeselectAllTreeViewItems(TreeViewItems);

            // Find and select the supply node
            foreach (var rootNode in TreeViewItems)
            {
                if (SelectSupplyNode(rootNode, supply))
                {
                    break;
                }
            }
        }

        /// <summary>
        ///     Recursively searches for and selects the supply node
        /// </summary>
        private bool SelectSupplyNode(TreeNodeViewModel node, Supply supply)
        {
            if (node.Data is Supply sup && sup == supply)
            {
                node.IsSelected = true;
                node.IsExpanded = true;
                
                // Expand parent nodes
                ExpandParentNodes(node);
                return true;
            }

            foreach (var child in node.Children)
            {
                if (SelectSupplyNode(child, supply))
                {
                    node.IsExpanded = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Highlights the alternator in the tree view
        /// </summary>
        private void HighlightAlternatorInTreeView(AlternatorBlock alternator)
        {
            // First, deselect all items
            DeselectAllTreeViewItems(TreeViewItems);

            // Find and select the alternator node
            foreach (var rootNode in TreeViewItems)
            {
                if (SelectAlternatorNode(rootNode, alternator))
                {
                    break;
                }
            }
        }

        /// <summary>
        ///     Recursively searches for and selects the alternator node
        /// </summary>
        private bool SelectAlternatorNode(TreeNodeViewModel node, AlternatorBlock alternator)
        {
            if (node.Data is AlternatorBlock alt && alt == alternator)
            {
                node.IsSelected = true;
                node.IsExpanded = true;
                
                // Expand parent nodes
                ExpandParentNodes(node);
                return true;
            }

            foreach (var child in node.Children)
            {
                if (SelectAlternatorNode(child, alternator))
                {
                    node.IsExpanded = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Highlights the conductor in the tree view
        /// </summary>
        private void HighlightConductorInTreeView(ConductorBlock conductor)
        {
            // First, deselect all items
            DeselectAllTreeViewItems(TreeViewItems);

            // Find and select the conductor node
            foreach (var rootNode in TreeViewItems)
            {
                if (SelectConductorNode(rootNode, conductor))
                {
                    break;
                }
            }
        }

        /// <summary>
        ///     Recursively searches for and selects the conductor node
        /// </summary>
        private bool SelectConductorNode(TreeNodeViewModel node, ConductorBlock conductor)
        {
            if (node.Data is ConductorBlock cond && cond == conductor)
            {
                node.IsSelected = true;
                node.IsExpanded = true;
                
                // Expand parent nodes
                ExpandParentNodes(node);
                return true;
            }

            foreach (var child in node.Children)
            {
                if (SelectConductorNode(child, conductor))
                {
                    node.IsExpanded = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Highlights the busbar in the tree view
        /// </summary>
        private void HighlightBusbarInTreeView(Busbar busbar)
        {
            // First, deselect all items
            DeselectAllTreeViewItems(TreeViewItems);

            // Find and select the busbar node
            foreach (var rootNode in TreeViewItems)
            {
                if (SelectBusbarNode(rootNode, busbar))
                {
                    break;
                }
            }
        }

        /// <summary>
        ///     Recursively searches for and selects the busbar node
        /// </summary>
        private bool SelectBusbarNode(TreeNodeViewModel node, Busbar busbar)
        {
            if (node.Data is Busbar bb && bb == busbar)
            {
                node.IsSelected = true;
                node.IsExpanded = true;
                
                // Expand parent nodes
                ExpandParentNodes(node);
                return true;
            }

            foreach (var child in node.Children)
            {
                if (SelectBusbarNode(child, busbar))
                {
                    node.IsExpanded = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Highlights the TransformerUPS in the tree view
        /// </summary>
        private void HighlightTransformerUPSInTreeView(TransformerUPSBlock transformer)
        {
            // First, deselect all items
            DeselectAllTreeViewItems(TreeViewItems);

            // Find and select the transformer node
            foreach (var rootNode in TreeViewItems)
            {
                if (SelectTransformerUPSNode(rootNode, transformer))
                {
                    break;
                }
            }
        }

        /// <summary>
        ///     Recursively searches for and selects the TransformerUPS node
        /// </summary>
        private bool SelectTransformerUPSNode(TreeNodeViewModel node, TransformerUPSBlock transformer)
        {
            if (node.Data is TransformerUPSBlock tfmr && tfmr == transformer)
            {
                node.IsSelected = true;
                node.IsExpanded = true;
                
                // Expand parent nodes
                ExpandParentNodes(node);
                return true;
            }

            foreach (var child in node.Children)
            {
                if (SelectTransformerUPSNode(child, transformer))
                {
                    node.IsExpanded = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Highlights the row in the tree view
        /// </summary>
        private void HighlightRowInTreeView(Row row)
        {
            // First, deselect all items
            DeselectAllTreeViewItems(TreeViewItems);

            // Find and select the row node
            foreach (var rootNode in TreeViewItems)
            {
                if (SelectRowNode(rootNode, row))
                {
                    break;
                }
            }
        }

        /// <summary>
        ///     Recursively searches for and selects the row node
        /// </summary>
        private bool SelectRowNode(TreeNodeViewModel node, Row row)
        {
            if (node.Data is Row r && r == row)
            {
                node.IsSelected = true;
                node.IsExpanded = true;
                
                // Expand parent nodes
                ExpandParentNodes(node);
                return true;
            }

            foreach (var child in node.Children)
            {
                if (SelectRowNode(child, row))
                {
                    node.IsExpanded = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Highlights the load in the tree view
        /// </summary>
        private void HighlightLoadInTreeView(Load load)
        {
            // First, deselect all items
            DeselectAllTreeViewItems(TreeViewItems);

            // Find and select the load node
            foreach (var rootNode in TreeViewItems)
            {
                if (SelectLoadNode(rootNode, load))
                {
                    break;
                }
            }
        }

        /// <summary>
        ///     Recursively searches for and selects the load node
        /// </summary>
        private bool SelectLoadNode(TreeNodeViewModel node, Load load)
        {
            if (node.Data is Load l && l == load)
            {
                node.IsSelected = true;
                node.IsExpanded = true;
                
                // Expand parent nodes
                ExpandParentNodes(node);
                return true;
            }

            foreach (var child in node.Children)
            {
                if (SelectLoadNode(child, load))
                {
                    node.IsExpanded = true;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Expands all parent nodes (not implemented yet as we need parent references)
        /// </summary>
        private void ExpandParentNodes(TreeNodeViewModel node)
        {
            // Parent expansion would require parent references in TreeNodeViewModel
            // For now, just ensure the node is expanded
            node.IsExpanded = true;
        }
    }
}
