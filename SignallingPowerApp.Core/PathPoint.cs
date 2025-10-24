using System;

namespace SignallingPowerApp.Core
{
    /// <summary>
    ///     Represents a point in a sequential path through the project.
    ///     Stores information about a specific block at a specific position in a path.
    /// </summary>
    public class PathPoint
    {
        public string? BlockName { get; set; } = null;
        public string? BlockType { get; set; } = null;
        public int? BlockID { get; set; } = null;
        public IItem? Equipment { get; set; } = null;
        public string? EquipmentName { get; set; } = null;
        public double? DistanceFromSource { get; set; } = null;
        public double? AddedDistance { get; set; } = null;
        public double? AddedLoad { get; set; } = null;
        public double? LoadAtPoint { get; set; } = null;
        public double? IdealVoltageAtPoint { get; set; } = null;    
        public double? VoltageAtPoint { get; set; } = null;
        public double? CurrentAtPoint { get; set; } = null;
        public double? TheoreticalVoltageDropRate { get; set; } = null;
        public string? SuggestedConductorName { get; set; } = null;
        public double? SuggestedConductorVoltageDropRate { get; set; } = null;
        public double? SelectedConductorVoltageDropRate { get; set; } = null;
        public double? VoltageDropAtPoint { get; set; } = null;
        public double? AddedImpedance { get; set; } = null;
        public double? ImpedanceAtPoint { get; set; } = null;
        public double? PrimaryVoltage { get; set; } = null;
        public double? PrimaryCurrent { get; set; } = null;
        public double? PrimaryTransformerImpedance { get; set; } = null;
        public double? SecondarySourceImpedance { get; set; } = null;
        public double? FaultCurrent { get; set; } = null;
        public double? MinimumCircuitBreakerRating { get; set; } = null;
        public double? SelectedCircuitBreakerRating { get; set; } = null;
        public double? In { get; set; } = null;

        /// <summary>
        ///     Initializes a new instance of the PathPoint class.
        /// </summary>
        /// <param name="blockID">The ID of the block at this point in the path.</param>
        /// <param name="idealVoltage">The ideal voltage at this point (default is 0, will be set during path building).</param>
        public PathPoint(IBlock block)
        {
            BlockID = block.ID;
            BlockType = block.GetType().Name;

            if (block is ConductorBlock conductorBlock)
            {
                Conductor con = conductorBlock.Equipment;
                BlockName = conductorBlock.Name.Trim();
                Equipment = con;
                EquipmentName = $"{con.Cores} Core {con.CrossSectionalArea}mm";
                SelectedConductorVoltageDropRate = conductorBlock.Equipment.VoltageDrop90;
            }

            if (block is Load load)
            {
                BlockName = load.Name.Trim();
                Equipment = load.Equipment;
                EquipmentName = load.Equipment.Name;
            }

            if (block is Row row)
            {
                BlockName = ((Busbar)row.Project.GetBlock(row.ParentID)).Name.Trim();

                if (row.Type == "CircuitBreaker")
                {
                    SelectedCircuitBreakerRating = row.Rating;
                }
            }

            if (block is Terminal terminal)
            {
                IBlock parentBlock = block.Project.GetBlock(block.ParentID);

                BlockName = parentBlock switch
                {
                    Location l => $"{l.Name.Trim()} T{terminal.Side}",
                    TransformerUPSBlock t => $"{t.Name.Trim()} T{terminal.Side}",
                    Load lo => $"{lo.Name.Trim()} T{terminal.Side}",
                    AlternatorBlock a => $"{a.Name.Trim()} Terminal",
                    ExternalBusbar e => $"{e.Name.Trim()} T{terminal.Side}",
                    Supply s => $"{s.Name.Trim()} Terminal",
                    Row r => $"{((Busbar)block.Project.GetBlock(r.ParentID)).Name.Trim()} Row {((Busbar)block.Project.GetBlock(r.ParentID)).GetIndexOfRow(r.ID)} T{terminal.Side}",
                    _ => "Position"
                };
            }

            if (block is Supply supply)
            {
                BlockName = supply.Name.Trim();
                AddedImpedance = supply.Impedance;
            }

            if (block is TransformerUPSBlock transformerUPSBlock)
            {
                BlockName = transformerUPSBlock.Name.Trim();
                TransformerUPS tran = transformerUPSBlock.Equipment;
                Equipment = tran;
                EquipmentName = $"{tran.PrimaryVoltage}/{tran.SecondaryVoltage} {tran.Rating}VA {tran.PercentageZ}%";
            }
        }

        /// <summary>
        ///     Returns a string representation of this path point.
        /// </summary>
        /// <returns>A string containing the block ID, ideal voltage, load, and distance.</returns>
        public override string ToString()
        {
            return $"PathPoint(BlockID: {BlockID}";
        }

        /// <summary>
        ///     Determines whether the specified object is equal to the current path point.
        /// </summary>
        /// <param name="obj">The object to compare with the current path point.</param>
        /// <returns>True if the objects are equal, false otherwise.</returns>
        public override bool Equals(object? obj)
        {
            if (obj is PathPoint other)
            {
                return BlockID == other.BlockID;
            }
            return false;
        }

        /// <summary>
        ///     Returns the hash code for this path point.
        /// </summary>
        /// <returns>A hash code for the current path point.</returns>
        public override int GetHashCode()
        {
            return BlockID.GetHashCode();
        }
    }
}
