using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignallingPowerApp.Core
{
    public class Calculator(Project project)
    {
        private double maxVoltageDrop = 0.1; // 10% max voltage drop
        private double loadCompensation = 0.1; // 10% load compensation
        private double cableLengthCompensation = 0.1; // 10% cable length compensation

        private PathPoint[][] paths;

        /// <summary>
        ///     Builds sequential connection paths from all source blocks in the project.
        ///     Paths start from Supply blocks and follow connections until there are no more.
        /// </summary>
        /// <returns>An array of PathPoint arrays representing all paths in the project.</returns>
        /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
        public PathPoint[][] BuildSequentialPaths()
        {
            var allPaths = new List<PathPoint[]>();

            // Get all source blocks (Supply only, not AlternatorBlock)
            var sourceBlocks = project.GetAllBlocks
                .Where(b => b.BlockType == "Supply")
                .ToList();

            // Validation 1: Check if there are any supply blocks
            if (sourceBlocks.Count == 0)
            {
                throw new InvalidOperationException("No supply blocks found in the project. At least one supply block is required.");
            }

            // Build paths from each source block
            var pathsBySupply = new Dictionary<int, List<PathPoint[]>>();
            
            foreach (var sourceBlock in sourceBlocks)
            {
                var supply = sourceBlock as Supply;
                if (supply == null)
                {
                    throw new InvalidOperationException($"Source block {sourceBlock.ID} is not a valid Supply block.");
                }

                // Start building paths from this source
                var pathsFromSource = BuildPathsFromBlock(sourceBlock.ID, new List<PathPoint> { new PathPoint(sourceBlock) });
                pathsBySupply[sourceBlock.ID] = pathsFromSource;
                allPaths.AddRange(pathsFromSource);
            }

            // Validation 2: Check for branch reconnection within the same supply's paths
            ValidateNoBranchReconnection(pathsBySupply);

            // Validation 3: Check that supply paths don't cross
            ValidateSupplyPathsIsolated(pathsBySupply);

            // Filter out non-branching terminals from all paths and trim to end at Load blocks
            var filteredPaths = allPaths
                .Select(path => FilterNonBranchingTerminals(path, allPaths))
                .Where(path => path != null)  // Remove paths that don't end with a Load
                .Cast<PathPoint[]>()
                .ToList();

            // Perform forward calculations (distance accumulation)
            CalculateIdealVoltageAndDistance(filteredPaths);

            // Perform backwards calculations (load accumulation)
            CalculateLoadAtPoint(filteredPaths);

            // Calculate added load for each point
            CalculateAddedLoads(filteredPaths);

            CalculateVoltageAndCurrent(filteredPaths);

            CalculateImpedances(filteredPaths);

            // Store the paths
            paths = filteredPaths.ToArray();
            return paths;
        }

        /// <summary>
        ///     Filters out terminals from a path that are not branching points.
        ///     A terminal is kept only if it appears in multiple paths (indicating a branching point)
        ///     or if it's at the start/end of a path.
        ///     Also removes trailing non-Load blocks from the end of paths.
        /// </summary>
        /// <param name="path">The path to filter.</param>
        /// <param name="allPaths">All paths in the project, used to check if terminals appear in multiple paths.</param>
        /// <returns>A filtered path with non-branching terminals removed and trimmed to end at a Load, or null if no Load exists.</returns>
        private PathPoint[]? FilterNonBranchingTerminals(PathPoint[] path, List<PathPoint[]> allPaths)
        {
            var filteredPath = new List<PathPoint>();

            for (int i = 0; i < path.Length; i++)
            {
                var point = path[i];
                if (!point.BlockID.HasValue) continue;
                
                var block = project.GetBlock(point.BlockID.Value);

                // Keep non-terminal blocks
                if (block.BlockType != "Terminal")
                {
                    filteredPath.Add(point);
                    continue;
                }

                // Keep terminals at the start or end of the path
                if (i == 0 || i == path.Length - 1)
                {
                    filteredPath.Add(point);
                    continue;
                }

                // For terminals in the middle, check if they appear in multiple paths
                // If a terminal appears in multiple paths, it's at a branching point
                bool isBranchingPoint = IsTerminalInMultiplePaths(point.BlockID.Value, allPaths);

                if (isBranchingPoint)
                {
                    filteredPath.Add(point);
                }
                // Otherwise, skip this terminal (don't add it to filtered path)
            }

            // Trim the path to end at the last Load block
            // If no Load block exists, return null (path will be removed)
            int lastLoadIndex = -1;
            for (int i = filteredPath.Count - 1; i >= 0; i--)
            {
                if (!filteredPath[i].BlockID.HasValue) continue;
                var block = project.GetBlock(filteredPath[i].BlockID.Value);
                if (block is Load)
                {
                    lastLoadIndex = i;
                    break;
                }
            }

            // If no Load found, return null to indicate this path should be removed
            if (lastLoadIndex == -1)
            {
                return null;
            }

            // If the last Load is not at the end, trim everything after it
            if (lastLoadIndex < filteredPath.Count - 1)
            {
                filteredPath.RemoveRange(lastLoadIndex + 1, filteredPath.Count - lastLoadIndex - 1);
            }

            return filteredPath.ToArray();
        }

        /// <summary>
        ///     Checks if a terminal is at a branching point by seeing if multiple paths
        ///     diverge starting from this terminal's position.
        /// </summary>
        /// <param name="terminalId">The ID of the terminal to check.</param>
        /// <param name="allPaths">All paths in the project.</param>
        /// <returns>True if multiple paths share the same prefix up to and including this terminal, false otherwise.</returns>
        private bool IsTerminalInMultiplePaths(int terminalId, List<PathPoint[]> allPaths)
        {
            // Find all paths that contain this terminal
            var pathsWithTerminal = new List<(PathPoint[] path, int index)>();
            
            foreach (var path in allPaths)
            {
                for (int i = 0; i < path.Length; i++)
                {
                    if (path[i].BlockID == terminalId)
                    {
                        pathsWithTerminal.Add((path, i));
                        break; // Only record the first occurrence in each path
                    }
                }
            }

            // If the terminal appears in only one path, it's not a branch point
            if (pathsWithTerminal.Count <= 1)
            {
                return false;
            }

            // Check if paths share the same prefix up to this terminal
            // If multiple paths have identical prefixes up to and including this terminal,
            // then this terminal is where they branch
            for (int i = 0; i < pathsWithTerminal.Count; i++)
            {
                for (int j = i + 1; j < pathsWithTerminal.Count; j++)
                {
                    var (path1, index1) = pathsWithTerminal[i];
                    var (path2, index2) = pathsWithTerminal[j];

                    // If the terminal is at the same position in both paths
                    // and they share the same prefix up to that point, it's a branch point
                    if (index1 == index2)
                    {
                        bool samePrefixUpToTerminal = true;
                        for (int k = 0; k <= index1; k++)
                        {
                            if (path1[k].BlockID != path2[k].BlockID)
                            {
                                samePrefixUpToTerminal = false;
                                break;
                            }
                        }

                        // If paths are identical up to and including this terminal,
                        // and they diverge after it, this is a branch point
                        if (samePrefixUpToTerminal)
                        {
                            // Check if they actually diverge after this terminal
                            if (index1 < path1.Length - 1 && index2 < path2.Length - 1)
                            {
                                if (path1[index1 + 1].BlockID != path2[index2 + 1].BlockID)
                                {
                                    return true; // Paths diverge right after this terminal
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Validates that paths from the same supply don't reconnect after branching.
        /// </summary>
        /// <param name="pathsBySupply">Dictionary of supply ID to their paths.</param>
        /// <exception cref="InvalidOperationException">Thrown when paths reconnect.</exception>
        private void ValidateNoBranchReconnection(Dictionary<int, List<PathPoint[]>> pathsBySupply)
        {
            foreach (var kvp in pathsBySupply)
            {
                int supplyId = kvp.Key;
                var paths = kvp.Value;

                // For each pair of paths from the same supply
                for (int i = 0; i < paths.Count; i++)
                {
                    for (int j = i + 1; j < paths.Count; j++)
                    {
                        var path1 = paths[i];
                        var path2 = paths[j];

                        // Find the divergence point (last common block)
                        int divergenceIndex = -1;
                        int minLength = Math.Min(path1.Length, path2.Length);
                        
                        for (int k = 0; k < minLength; k++)
                        {
                            if (path1[k].BlockID == path2[k].BlockID)
                            {
                                divergenceIndex = k;
                            }
                            else
                            {
                                break;
                            }
                        }

                        // If paths diverged, check if they reconnect later
                        if (divergenceIndex >= 0 && divergenceIndex < minLength - 1)
                        {
                            // Get the portions after divergence
                            var path1AfterDivergence = path1.Skip(divergenceIndex + 1).Select(p => p.BlockID);
                            var path2AfterDivergence = path2.Skip(divergenceIndex + 1).Select(p => p.BlockID);

                            // Check if any block appears in both paths after divergence
                            var commonBlocks = path1AfterDivergence.Intersect(path2AfterDivergence).ToList();
                        
                            if (commonBlocks.Any())
                            {
                                var supply = project.GetBlock(supplyId);
                                throw new InvalidOperationException(
                                    $"Path reconnection detected in supply '{supply.BlockType}' (ID: {supplyId}). " +
                                    $"Paths [{string.Join(",", path1.Select(p => p.BlockID))}] and [{string.Join(",", path2.Select(p => p.BlockID))}] " +
                                    $"reconnect at block(s): {string.Join(", ", commonBlocks)}. " +
                                    $"Branched paths cannot reconnect as this creates a loop."
                                );
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Validates that paths from different supplies remain isolated.
        /// </summary>
        /// <param name="pathsBySupply">Dictionary of supply ID to their paths.</param>
        /// <exception cref="InvalidOperationException">Thrown when supply paths cross.</exception>
        private void ValidateSupplyPathsIsolated(Dictionary<int, List<PathPoint[]>> pathsBySupply)
        {
            var supplyIds = pathsBySupply.Keys.ToList();

            // Compare paths from different supplies
            for (int i = 0; i < supplyIds.Count; i++)
            {
                for (int j = i + 1; j < supplyIds.Count; j++)
                {
                    int supply1Id = supplyIds[i];
                    int supply2Id = supplyIds[j];

                    var paths1 = pathsBySupply[supply1Id];
                    var paths2 = pathsBySupply[supply2Id];

                    // Get all blocks used by each supply (excluding the supply blocks themselves)
                    var blocks1 = paths1
                        .SelectMany(p => p.Skip(1).Select(point => point.BlockID)) // Skip the supply block itself
                        .Distinct()
                        .ToHashSet();

                    var blocks2 = paths2
                        .SelectMany(p => p.Skip(1).Select(point => point.BlockID)) // Skip the supply block itself
                        .Distinct()
                        .ToHashSet();

                    // Check for intersection
                    var commonBlocks = blocks1.Intersect(blocks2).ToList();

                    if (commonBlocks.Any())
                    {
                        var supply1 = project.GetBlock(supply1Id);
                        var supply2 = project.GetBlock(supply2Id);
                        
                        throw new InvalidOperationException(
                            $"Supply path crossing detected. Supplies must remain isolated from each other. " +
                            $"Supply '{supply1.BlockType}' (ID: {supply1Id}) and Supply '{supply2.BlockType}' (ID: {supply2Id}) " +
                            $"share common block(s): {string.Join(", ", commonBlocks)}. " +
                            $"Each supply must have its own independent path network."
                        );
                    }
                }
            }
        }

        /// <summary>
        ///     Recursively builds paths from a given block ID.
        ///     Creates unique PathPoint instances for each occurrence in a path.
        /// </summary>
        /// <param name="blockId">The current block ID to traverse from.</param>
        /// <param name="currentPath">The path built so far.</param>
        /// <returns>A list of completed paths.</returns>
        private List<PathPoint[]> BuildPathsFromBlock(int blockId, List<PathPoint> currentPath)
        {
            var completedPaths = new List<PathPoint[]>();

            // Get the current block
            var currentBlock = project.GetBlock(blockId);

            // Validate equipment for blocks that require it
            ValidateBlockEquipment(currentBlock);

            // Get all connections from this block
            var connections = project.GetConnections(blockId);

            // Filter out connections we've already visited in this path
            var unvisitedConnections = connections
                .Where(c =>
                {
                    int nextBlockId = c.LeftID == blockId ? c.RightID : c.LeftID;
                    // Check if the next block is already in the current path
                    return !currentPath.Any(p => p.BlockID == nextBlockId);
                })
                .ToList();

            // If no unvisited connections, this path is complete
            if (unvisitedConnections.Count == 0)
            {
                completedPaths.Add(currentPath.ToArray());
                return completedPaths;
            }

            // For each unvisited connection, create a branch
            foreach (var connection in unvisitedConnections)
            {
                // Get the next block ID (the other end of the connection)
                int nextBlockId = connection.LeftID == blockId ? connection.RightID : connection.LeftID;

                // Create a unique PathPoint for this block in this path
                var nextBlock = project.GetBlock(nextBlockId);
                var newPathPoint = new PathPoint(nextBlock);
                
                // Create a DEEP COPY of the current path with new PathPoint instances
                // This ensures each branch has its own independent PathPoint objects
                var newPath = new List<PathPoint>();
                foreach (var existingPoint in currentPath)
                {
                    // Create a new PathPoint instance for each point in the path
                    var copiedPoint = new PathPoint(project.GetBlock(existingPoint.BlockID!.Value));
                    newPath.Add(copiedPoint);
                }
                newPath.Add(newPathPoint);

                // Recursively build paths from the next block
                var branchPaths = BuildPathsFromBlock(nextBlockId, newPath);
                completedPaths.AddRange(branchPaths);
            }

            return completedPaths;
        }

        /// <summary>
        ///     Validates that a block has equipment assigned if it requires equipment.
        /// </summary>
        /// <param name="block">The block to validate.</param>
        /// <exception cref="InvalidOperationException">Thrown when equipment is missing.</exception>
        private void ValidateBlockEquipment(IBlock block)
        {
            try
            {
                switch (block.BlockType)
                {
                    case "ConductorBlock":
                        var conductorBlock = block as ConductorBlock;
                        if (conductorBlock != null)
                        {
                            // Validate that length is greater than 0
                            if (conductorBlock.Length <= 0)
                            {
                                throw new InvalidDataException("Conductor length must be greater than 0.");
                            }
                        }
                        break;

                    case "TransformerUPS":
                        var transformerBlock = block as TransformerUPSBlock;
                        if (transformerBlock != null)
                        {
                            // Accessing Equipment property will throw if not assigned
                            var _ = transformerBlock.Equipment;
                        }
                        break;

                    case "AlternatorBlock":
                        var alternatorBlock = block as AlternatorBlock;
                        if (alternatorBlock != null)
                        {
                            // Accessing Equipment property will throw if not assigned
                            var _ = alternatorBlock.Equipment;
                        }
                        break;

                    case "Load":
                        var load = block as Load;
                        if (load != null)
                        {
                            // Accessing Equipment property will throw if not assigned
                            var _ = load.Equipment;
                        }
                        break;

                    // Other block types don't require equipment
                    default:
                        break;
                }
            }
            catch (InvalidDataException ex)
            {
                // Get block name for better error message
                string blockName = GetBlockName(block);
                
                // Check if this is a length validation error
                if (ex.Message.Contains("length"))
                {
                    throw new InvalidOperationException(
                        $"Length not set for {block.BlockType} '{blockName}' (ID: {block.ID}). " +
                        $"Conductor length must be greater than 0 meters for calculations. " +
                        $"Please set the length in the properties panel.",
                        ex);
                }
                
                throw new InvalidOperationException(
                    $"Equipment not assigned to {block.BlockType} '{blockName}' (ID: {block.ID}). " +
                    $"All blocks in the path except conductors must have equipment assigned before calculations can be performed. " +
                    $"Please assign equipment from the properties panel.",
                    ex);
            }
        }

        /// <summary>
        ///     Gets a display name for a block.
        /// </summary>
        /// <param name="block">The block to get the name from.</param>
        /// <returns>The block's name or a default identifier.</returns>
        private string GetBlockName(IBlock block)
        {
            return block switch
            {
                ConductorBlock conductor => conductor.Name,
                TransformerUPSBlock transformer => transformer.Name,
                AlternatorBlock alternator => alternator.Name,
                Load load => load.Name,
                Location location => location.Name,
                Supply supply => supply.Name,
                Busbar busbar => busbar.Name,
                _ => $"Block {block.ID}"
            };
        }

        /// <summary>
        ///     Loops through the paths backwards and performs Load At Point calculations.
        /// </summary>
        /// <param name="paths">The list of paths to make calculations on.</param>
        private void CalculateLoadAtPoint(List<PathPoint[]> paths)
        {
            foreach (var path in paths)
            {
                // Iterate from the end of the path backwards
                for (int i = path.Length - 1; i >= 0; i--)
                {
                    var point = path[i];
                    if (!point.BlockID.HasValue) continue;

                    var block = project.GetBlock(point.BlockID.Value);

                    // Check if this point is a Load block
                    if (block is Load load)
                    {
                        // Get the load value from the equipment
                        double loadValue = load.Equipment.Load;

                        // Add this load to all points from here back to the start
                        for (int j = i; j >= 0; j--)
                        {
                            var currentPoint = path[j];
                            
                            // Initialize LoadAtPoint if it's null
                            if (currentPoint.LoadAtPoint == null)
                            {
                                currentPoint.LoadAtPoint = 0;
                            }

                            // Add the load value
                            currentPoint.LoadAtPoint += loadValue;

                            // Also add to the same block in other paths
                            foreach (var otherPath in paths)
                            {
                                if (otherPath == path) continue; // Skip the current path

                                // Find the same block in the other path
                                foreach (var otherPoint in otherPath)
                                {
                                    if (otherPoint.BlockID == currentPoint.BlockID)
                                    {
                                        // Initialize if null
                                        if (otherPoint.LoadAtPoint == null)
                                        {
                                            otherPoint.LoadAtPoint = 0;
                                        }

                                        // Add the load value
                                        otherPoint.LoadAtPoint += loadValue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Calculates the AddedLoad for each point based on the Load blocks in the path.
        ///     AddedLoad[i] = LoadAtPoint[i] - LoadAtPoint[i+1], except when i is the last index where it equals LoadAtPoint[i].
        /// </summary>
        /// <param name="paths">The list of paths to calculate added loads for.</param>
        private void CalculateAddedLoads(List<PathPoint[]> paths)
        {
            // For each path, calculate AddedLoad based on the path's specific sequence
            foreach (var path in paths)
            {
                // Iterate from the end of the path backwards
                for (int i = path.Length - 1; i >= 0; i--)
                {
                    var point = path[i];
                    var loadAtPoint = point.LoadAtPoint ?? 0.0;

                    // For the last point in the path, AddedLoad equals LoadAtPoint
                    // For other points, AddedLoad = LoadAtPoint[i] - LoadAtPoint[i+1]
                    if (i == path.Length - 1)
                    {
                        point.AddedLoad = loadAtPoint;
                    }
                    else
                    {
                        double nextLoadAtPoint = path[i + 1].LoadAtPoint ?? 0.0;
                        point.AddedLoad = loadAtPoint - nextLoadAtPoint;
                    }
                }
            }
        }

        /// <summary>
        ///     Loops through the paths forward and calculates cumulative distances from the source.
        ///     Sets DistanceFromSource, AddedDistance, and IdealVoltageAtPoint for each PathPoint in the path.
        ///     For ConductorBlocks, DistanceFromSource includes the conductor's length.
        ///     For Transformers, validates and converts voltage from primary to secondary or vice versa.
        /// </summary>
        /// <param name="paths">The list of paths to make calculations on.</param>
        private void CalculateIdealVoltageAndDistance(List<PathPoint[]> paths)
        {
            foreach (var path in paths)
            {
                double cumulativeDistance = 0.0;
                double currentVoltage = 0.0;

                // Iterate from the start of the path forward
                for (int i = 0; i < path.Length; i++)
                {
                    var point = path[i];
                    if (!point.BlockID.HasValue) continue;

                    var block = project.GetBlock(point.BlockID.Value);

                    // Set initial voltage from Supply block
                    if (block is Supply supply)
                    {
                        currentVoltage = supply.Voltage;
                        point.IdealVoltageAtPoint = currentVoltage;
                    }
                    // Handle transformer voltage conversion
                    else if (block is TransformerUPSBlock transformerBlock)
                    {
                        var transformer = transformerBlock.Equipment;
                        
                        // Check if current voltage matches primary or secondary voltage
                        if (Math.Abs(currentVoltage - transformer.PrimaryVoltage) < 0.01)
                        {
                            // Voltage matches primary, convert to secondary
                            currentVoltage = transformer.SecondaryVoltage;
                            point.IdealVoltageAtPoint = currentVoltage;
                        }
                        else if (Math.Abs(currentVoltage - transformer.SecondaryVoltage) < 0.01)
                        {
                            // Voltage matches secondary, convert to primary
                            currentVoltage = transformer.PrimaryVoltage;
                            point.IdealVoltageAtPoint = currentVoltage;
                        }
                        else
                        {
                            // Voltage doesn't match either side of transformer
                            throw new InvalidOperationException(
                                $"Voltage mismatch at transformer '{transformerBlock.Name}' (ID: {transformerBlock.ID}). " +
                                $"Incoming voltage is {currentVoltage}V, but transformer has {transformer.PrimaryVoltage}V primary " +
                                $"and {transformer.SecondaryVoltage}V secondary. The incoming voltage must match either the " +
                                $"primary or secondary voltage of the transformer.");
                        }
                    }
                    else
                    {
                        // For all other blocks, maintain current voltage
                        point.IdealVoltageAtPoint = currentVoltage;
                    }

                    // Check if this point is a ConductorBlock
                    if (block is ConductorBlock conductor)
                    {
                        // Calculate and set AddedDistance (conductor length in kilometers)
                        double addedDistance = conductor.Length / 1000.0;
                        point.AddedDistance = addedDistance;

                        // Add the conductor's length to the cumulative distance
                        cumulativeDistance += addedDistance;

                        // Set DistanceFromSource for the conductor point (includes the conductor's length)
                        point.DistanceFromSource = cumulativeDistance;
                    }
                    else
                    {
                        // For non-conductor blocks, set DistanceFromSource to current cumulative distance
                        point.DistanceFromSource = cumulativeDistance;
                    }
                }
            }
        }

        private void CalculateVoltageAndCurrent(List<PathPoint[]> paths)
        {
            foreach (var path in paths)
            {
                double? voltage = 0.0;
                double? voltageDrop = 0.0;
                double? current = 0.0;

                // Iterate from the start of the path forward
                for (int i = path.Length - 1; i >= 0; i--)
                {
                    var point = path[i];
                    if (i == path.Length - 1)
                    {
                        voltage = point.IdealVoltageAtPoint * (1 - maxVoltageDrop);
                        current = point.AddedLoad / voltage;
                        voltageDrop = 0.0;
                    }
                    else
                    {
                        var nextPoint = path[i + 1];

                        voltage = nextPoint.VoltageAtPoint + nextPoint.VoltageDropAtPoint;
                        current = nextPoint.CurrentAtPoint + (point.AddedLoad / voltage);

                        if (nextPoint.BlockType == "TransformerUPSBlock")
                        {
                            voltage = nextPoint.PrimaryVoltage;
                            current = nextPoint.PrimaryCurrent + (point.AddedLoad / voltage);
                        }

                        if (voltage > (double)point.IdealVoltageAtPoint)
                        {
                            point.VoltageAtPoint = voltage;
                            point.CurrentAtPoint = current;
                            break;
                        }

                        if (point.BlockType == "ConductorBlock")
                        {
                            Conductor cond = ((Conductor)point.Equipment);
                            point.TheoreticalVoltageDropRate = (point.IdealVoltageAtPoint - voltage) / (current * point.DistanceFromSource);

                            // Find conductor with VoltageDrop90 closest to but not over TheoreticalVoltageDropRate
                            var suitableConductors = project.Items.Conductors
                                .Where(c => c.VoltageDrop90 <= point.TheoreticalVoltageDropRate)
                                .OrderByDescending(c => c.VoltageDrop90)
                                .ToList();

                            if (suitableConductors.Any())
                            {
                                var suggestedConductor = suitableConductors.First();
                                point.SuggestedConductorVoltageDropRate = suggestedConductor.VoltageDrop90;
                                point.SuggestedConductorName = $"{suggestedConductor.Cores} Core {suggestedConductor.CrossSectionalArea}mm";
                            }
                            else
                            {
                                throw new InvalidOperationException(
                                    $"No suitable conductor found for ConductorBlock '{point.BlockName}' (ID: {point.BlockID}). " +
                                    $"Theoretical voltage drop rate is {point.TheoreticalVoltageDropRate} V/A·km, " +
                                    $"but no conductors have a VoltageDrop90 rating less than or equal to this value. " +
                                    $"Please add a suitable conductor to the project.");
                            }

                            voltageDrop = point.AddedDistance * current * point.SelectedConductorVoltageDropRate;
                            point.AddedImpedance = 2 * point.AddedDistance * Math.Sqrt(Math.Pow((double)cond.Resistance90, 2) + Math.Pow((double)cond.Reactance, 2));
                        }

                        if (point.BlockType == "TransformerUPSBlock")
                        {
                            TransformerUPS tran = ((TransformerUPS)point.Equipment);
                            double a = (double)tran.PrimaryVoltage / tran.SecondaryVoltage;

                            point.PrimaryCurrent = nextPoint.CurrentAtPoint / a;
                            point.PrimaryTransformerImpedance = tran.PercentageZ / 100 * (tran.PrimaryVoltage * tran.PrimaryVoltage) / (tran.Rating);
                            voltageDrop = current * point.PrimaryTransformerImpedance;
                            point.PrimaryVoltage = Math.Sqrt(Math.Pow(a * (double)nextPoint.VoltageAtPoint, 2) + Math.Pow((double)voltageDrop, 2));
                            point.AddedImpedance = point.PrimaryTransformerImpedance / (a * a);
                        }
                    }
                    point.VoltageAtPoint = voltage;
                    point.CurrentAtPoint = current;
                    point.VoltageDropAtPoint = voltageDrop;
                }
            }
        }

        private void CalculateImpedances(List<PathPoint[]> paths)
        {
            foreach (var path in paths)
            {
                var impedance = 0.0;
                var faultCurrent = 0.0;

                // Iterate from the start of the path forward
                for (int i = 0; i < path.Length; i++)
                {
                    var point = path[i];

                    if (point.AddedImpedance != null)
                    {
                        impedance += (double)point.AddedImpedance;
                    }

                    if (point.BlockType == "TransformerUPSBlock")
                    {
                        TransformerUPS tran = ((TransformerUPS)point.Equipment);
                        double a = (double)tran.PrimaryVoltage / tran.SecondaryVoltage;
                        double secondarySourceImpedance = (double)path[i-1].ImpedanceAtPoint / (a * a);
                        point.SecondarySourceImpedance = secondarySourceImpedance;
                        impedance += secondarySourceImpedance;
                    }

                    point.ImpedanceAtPoint = impedance;
                    point.FaultCurrent = point.IdealVoltageAtPoint / impedance;

                    if (point.BlockType == "Row" && point.SelectedCircuitBreakerRating != null)
                    {
                        point.MinimumCircuitBreakerRating = Math.Ceiling(impedance / 9.5);
                        point.In = point.FaultCurrent / point.SelectedCircuitBreakerRating;
                    }
                }
            }
        }
    }
}
