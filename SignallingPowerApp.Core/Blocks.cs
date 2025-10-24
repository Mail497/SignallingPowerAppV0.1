using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace SignallingPowerApp.Core
{
    /// <summary>
    /// Canvas type for connection rendering
    /// </summary>
    public enum CanvasType
    {
        Layout,     // Main diagram canvas
        Location    // Location-specific canvas
    }

    /// <summary>
    /// Information about a single connection dot
    /// </summary>
    public struct ConnectionDotInfo
    {
        /// <summary>
        /// X position relative to block center
        /// </summary>
        public double RelativeX { get; set; }

        /// <summary>
        /// Y position relative to block center
        /// </summary>
        public double RelativeY { get; set; }

        /// <summary>
        /// Terminal or block ID this dot represents
        /// </summary>
        public int TerminalId { get; set; }

        /// <summary>
        /// Position index for blocks with multiple dots
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Tag object to attach to the dot
        /// </summary>
        public object Tag { get; set; }
    }

    /// <summary>
    /// Interface for blocks that support interactive connection dots and UI rendering
    /// </summary>
    public interface IInteractiveBlock
    {
        /// <summary>
        /// Gets the canvas type where this block's dots should appear
        /// </summary>
        CanvasType PreferredCanvas { get; }

        /// <summary>
        /// Gets the connection dot positions for this block
        /// </summary>
        /// <param name="dotOffset">Offset for centering dots (usually half dot size)</param>
        /// <returns>Array of connection dot information</returns>
        ConnectionDotInfo[] GetConnectionDotPositions(double dotOffset);

        /// <summary>
        /// Gets the visual dimensions of this block when rendered
        /// </summary>
        /// <returns>Tuple of (width, height) in pixels</returns>
        (double width, double height) GetRenderDimensions();

        /// <summary>
        /// Gets the UI element type used to render this block
        /// </summary>
        /// <returns>Name of the UI element type ("Border", "Ellipse", "Polygon", "Grid", "StackPanel", "Canvas")</returns>
        string GetUIElementType();
    }

    public class ProjectBuilder : ImportText
    {
        private AllItems? items = null;

        public Project NewProject(string? name = null)
        {
            Project project = new(GenerateUniqueID());
            if (!string.IsNullOrWhiteSpace(name)) project.Name = name;
            return project;
        }

        /// <summary>
        ///     Generates a unique session ID based on the current date and time.
        /// </summary>
        /// <returns>A unique integer ID based on timestamp.</returns>
        private static long GenerateUniqueID()
        {
            // Use current DateTime to generate a unique ID
            // Format: yyMMddHHmmss (e.g., 240315143022 for March 15, 2024 at 14:30:22)
            var now = DateTime.Now;
            return long.Parse($"{now:yyMMddHHmmss}");
        }

        public AllItems OpenItemsFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified item file was not found.", filePath);
            }
            string[] lines = File.ReadAllLines(filePath);
            items = ImportItemsText(lines);

            return items;
        }

        public Project OpenProjectFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified project file was not found.", filePath);
            }

            string[] lines = File.ReadAllLines(filePath);

            if (items == null)
            {
                throw new InvalidOperationException("Items must be loaded before opening a project.");
            }

            return ImportProjectText(lines, items);
        }
    }

    /// <summary>
    ///     This class manages all project data including locations and connections.
    /// </summary>
    public class Project
    {
        private string name = "New Project";                    // Name of the project
        private readonly long sessionID;                        // Unique identifier for the session
        private long saveID;                                    // Identifier for the last saved state
        private readonly List<IBlock> blocks = [];              // List to store all blocks
        private readonly List<Connection> connections = [];     // List to store all connections
        private int idCount = 0;                                // Counter for block IDs

        private int majorVersion = 0;                           // Major version of the project
        private int minorVersion = 0;                           // Minor version of the project

        private string designer = "None";                       // Name of the project designer
        private int designDate = 0;                             // Date the project was designed
        private int designRPEQ = 0;                             // RPEQ of designer
        private string checker = "None";                        // Name of the project checker
        private int checkDate = 0;                              // Date the project was checked
        private int checkRPEQ = 0;                              // RPEQ of the checker

        private AllItems? items = null;

        /// <summary>
        ///     Initialises a new instance of the Project class. This class manages all project data.
        /// </summary>
        public Project(long saveID)
        {
            sessionID = GenerateUniqueID();
            this.saveID = saveID;
        }

        /// <summary>
        ///     Generates a unique session ID based on the current date and time.
        /// </summary>
        /// <returns>A unique integer ID based on timestamp.</returns>
        private static long GenerateUniqueID()
        {
            // Use current DateTime to generate a unique ID
            // Format: yyMMddHHmmss (e.g., 240315143022 for March 15, 2024 at 14:30:22)
            var now = DateTime.Now;
            return long.Parse($"{now:yyMMddHHmmss}");
        }

        public string Name 
        { 
            get { return name; } 
            set 
            { 
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Project name cannot be empty or whitespace.");
                }
                if (value.Length > 100)
                {
                    throw new ArgumentException("Project name cannot exceed 100 characters.");
                }
                name = value; 
            }
        }

        public AllItems? Items
        {
            get { return items; }
            set { items = value; }
        }

        public long SessionID { get { return sessionID; } }

        public long SaveID { get { return saveID; } }

        public int MajorVersion 
        { 
            get { return majorVersion; } 
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("MajorVersion must be a non-negative integer.");
                }
                majorVersion = value;
            }
        }

        public int MinorVersion 
        { 
            get { return minorVersion; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("MinorVersion must be a non-negative integer.");
                }
                minorVersion = value;
            }
        }

        public string Designer
        {
            get { return designer; }
            set
            {
                if(value.Length > 32)
                {
                    throw new ArgumentException("Designer name cannot exceed 32 characters.");
                }
                designer = value;
            }
        }

        public int DesignDate
        {
            get { return designDate; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("DesignDate must be a non-negative integer.");
                }
                designDate = value;
            }
        }

        public int DesignRPEQ
        {
            get { return designRPEQ; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("DesignRPEQ must be a non-negative integer.");
                }
                designRPEQ = value;
            }
        }

        public string Checker
        {
            get { return checker; }
            set
            {
                if (value.Length > 32)
                {
                    throw new ArgumentException("Checker name cannot exceed 32 characters.");
                }
                checker = value;
            }
        }

        public int CheckDate
        {
            get { return checkDate; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("CheckDate must be a non-negative integer.");
                }
                checkDate = value;
            }
        }

        public int CheckRPEQ
        {
            get { return checkRPEQ; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("CheckRPEQ must be a non-negative integer.");
                }
                checkRPEQ = value;
            }
        }

        public bool ContainsBlock(int id)
        {
            return blocks.Exists(block => block.ID == id);
        }

        /// <summary>
        ///     Adds a new block to the project.
        /// </summary>
        /// <param name="type">The type of block to add.</param>
        /// <returns>The newly created IBlock object.</returns>
        public IBlock AddBlock(string type, int ?optionalID = null)
        {
            switch (type)
            {
                case "Location":
                    Location location = new(idCount, this);
                    blocks.Add(location);
                    idCount++;
                    // Create 8 external terminals for the location
                    List<int> locationTerminalIds = new List<int>();
                    for (int i = 0; i < 8; i++)
                    {
                        Terminal locationTerminal = new Terminal(idCount, location.ID, i, this);
                        blocks.Add(locationTerminal);
                        locationTerminalIds.Add(idCount);
                        idCount++;
                    }
                    // Create the ExternalBusbar for the location
                    ExternalBusbar externalBusbar = new(idCount, location.ID, this);
                    blocks.Add(externalBusbar);
                    int externalBusbarId = idCount;
                    idCount++;
                    // Create 8 terminals for the ExternalBusbar and connect them to Location's external terminals
                    for (int i = 0; i < 8; i++)
                    {
                        Terminal externalBusbarTerminal = new Terminal(idCount, externalBusbarId, i, this);
                        blocks.Add(externalBusbarTerminal);
                        // Automatically connect Location's external terminal to ExternalBusbar's terminal
                        connections.Add(new Connection(locationTerminalIds[i], idCount, this));
                        idCount++;
                    }
                    return location;
                case "Busbar":
                    if (optionalID == null)
                    {
                        throw new InvalidDataException("Busbar requires a Location ID.");
                    }
                    if (GetBlock((int)optionalID) is not Location)
                    {
                        throw new InvalidDataException("optionalID must reference a Location");
                    }
                    Busbar busbar = new(idCount, (int)optionalID, this);
                    blocks.Add(busbar);
                    idCount++;
                    return busbar;
                case "Row":
                    if (optionalID == null)
                    {
                        throw new InvalidDataException("Row requires a Busbar ID.");
                    }
                    if (GetBlock((int)optionalID) is not Busbar)
                    {
                        throw new InvalidDataException("optionalID must reference a Busbar");
                    }
                    Row row = new(idCount, (int)optionalID, this);
                    blocks.Add(row);
                    idCount++;
                    Terminal terminal1 = new(idCount, row.ID, 0, this);
                    blocks.Add(terminal1);
                    idCount++;
                    Terminal terminal2 = new(idCount, row.ID, 1, this);
                    blocks.Add(terminal2);
                    idCount++;
                    connections.Add(new Connection(terminal1.ID, row.ID, this));
                    connections.Add(new Connection(row.ID, terminal2.ID, this));
                    return row;
                case "Terminal":
                    if (optionalID == null)
                    {
                        throw new InvalidDataException("Row requires a Busbar ID.");
                    }
                    if (GetBlock((int)optionalID) is not Location)
                    {
                        throw new InvalidDataException("optionalID must reference a Location");
                    }
                    Terminal terminal3 = new(idCount, (int)optionalID, 0, this);
                    blocks.Add(terminal3);
                    idCount++;
                    return terminal3;
                case "TransformerUPS":
                    if (optionalID == null)
                    {
                        throw new InvalidDataException("TransformerUPS requires a Location ID.");
                    }
                    if (GetBlock((int)optionalID) is not Location)
                    {
                        throw new InvalidDataException("optionalID must reference a Location.");
                    }
                    TransformerUPSBlock transformerUPSBlock = new(idCount, (int)optionalID, this);
                    blocks.Add(transformerUPSBlock);
                    idCount++;
                    Terminal terminal4 = new(idCount, transformerUPSBlock.ID, 0, this);
                    blocks.Add(terminal4);
                    idCount++;
                    Terminal terminal5 = new(idCount, transformerUPSBlock.ID, 1, this);
                    blocks.Add(terminal5);
                    idCount++;
                    connections.Add(new Connection(terminal4.ID, transformerUPSBlock.ID, this));
                    connections.Add(new Connection(transformerUPSBlock.ID, terminal5.ID, this));
                    return transformerUPSBlock;
                case "Supply":
                    Supply supply = new(idCount, this);
                    blocks.Add(supply);
                    idCount++;
                    return supply;
                case "AlternatorBlock":
                    AlternatorBlock alternatorBlock = new(idCount, this);
                    blocks.Add(alternatorBlock);
                    idCount++;
                    return alternatorBlock;
                case "Load":
                    if (optionalID == null)
                    {
                        throw new InvalidDataException("Load requires a Location ID.");
                    }
                    if (GetBlock((int)optionalID) is not Location)
                    {
                        throw new InvalidDataException("optionalID must reference a Location.");
                    }
                    Load load = new(idCount, (int)optionalID, this);
                    blocks.Add(load);
                    idCount++;
                    return load;
                case "ConductorBlock":
                    ConductorBlock conductorBlock = new(idCount, this);
                    blocks.Add(conductorBlock);
                    idCount++;
                    Terminal terminal6 = new(idCount, conductorBlock.ID, 0, this);
                    blocks.Add(terminal6);
                    idCount++;
                    Terminal terminal7 = new(idCount, conductorBlock.ID, 1, this);
                    blocks.Add(terminal7);
                    idCount++;
                    connections.Add(new Connection(terminal6.ID, conductorBlock.ID, this));
                    connections.Add(new Connection(conductorBlock.ID, terminal7.ID, this));
                    return conductorBlock;
                default:
                    throw new InvalidDataException($"Unknown block type '{type}'.");
            }
        }

        /// <summary>
        ///     Imports a block into the project.
        /// </summary>
        /// <param name="block">A block to import.</param>
        /// <exception cref="InvalidDataException"></exception>
        public void ImportBlock(IBlock block)
        {
            if (block.Project != this)
            {
                throw new InvalidDataException("Block does not belong to this project.");
            }
            if (blocks.Exists(b => b.ID == block.ID))
            {
                throw new InvalidDataException($"Block with ID '{block.ID}' already exists in the project.");
            }
            blocks.Add(block);
            if (block.ID >= idCount)
            {
                idCount = block.ID + 1;
            }
        }

        /// <summary>
        ///     Removes a block from the project.
        /// </summary>
        /// <param name="id">The ID of the block to remove.</param>
        /// <exception cref="KeyNotFoundException"></exception>
        public void RemoveBlock(int id) 
        {
            var block = GetBlock(id);

            // Remove all connections associated with this location
            connections.RemoveAll(connection => connection.LeftID == id || 
                connection.RightID == id);

            blocks.RemoveAll(block => block.ID == id);
        }

        /// <summary>
        ///     Gets a block by its ID.
        /// </summary>
        /// <param name="id">The ID of the block to retrieve.</param>
        /// <returns>The IBlock object with the specified ID.</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public IBlock GetBlock(int id) 
        {
            var block = blocks.Find(block => block.ID == id);

            if (block == null)
            {
                throw new KeyNotFoundException($"Block with the ID '{id}' not found.");
            }
            return block;
        }

        /// <summary>
        ///     Gets all blocks in the project.
        /// </summary>
        /// <returns>A read-only collection of all blocks.</returns>
        public IReadOnlyCollection<IBlock> GetAllBlocks
        {
            get { return blocks.AsReadOnly(); }
        }

        /// <summary>
        ///     Adds a new connection between two rows in the project.
        /// </summary>
        /// <param name="leftId">The connection ID for the left side.</param>
        /// <param name="rightId">The connection ID for the right side.</param>
        /// <returns>The newly created Connection object.</returns>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="KeyNotFoundException"></exception>
        public Connection AddConnection(int leftId, int rightId)
        {
            // Cannot create a connection to the same row
            if (leftId == rightId)
            {
                throw new InvalidDataException("Cannot create connection to same place.");
            }

            var leftBlock = GetBlock(leftId) ?? throw new KeyNotFoundException($"Block with the ID '{leftId}' not found.");
            var rightBlock = GetBlock(rightId) ?? throw new KeyNotFoundException($"Block with the ID '{rightId}' not found.");

            // Check if connection already exists
            if (connections.Exists(connection => (connection.LeftID == leftId && connection.RightID == rightId) ||
                                                 (connection.LeftID == rightId && connection.RightID == leftId)))
            {
                throw new InvalidDataException("Connection already exists.");
            }

            Connection newConnection = new(leftId, rightId, this);
            connections.Add(newConnection);
            return newConnection;
        }

        public void ImportConnections(int[][] connections)
        {
            foreach (int[] connection in connections)
            {
                if (connection.Length % 2 != 0)
                {
                    throw new InvalidDataException("Connection array must contain integar pairs.");
                }

                Connection conn = AddConnection(connection[0], connection[1]);

                if (connection.Length > 2)
                {
                    for (int i = 2; i < connection.Length; i += 2)
                    {
                        conn.AddRenderPoint(connection[i], connection[i + 1]);
                    }
                }
            }
        }

        /// <summary>
        ///     Removes a connection from the project.
        /// </summary>
        /// <param name="leftId">The connection ID for the left side.</param>
        /// <param name="rightId">The connection ID for the right side.</param>
        /// <exception cref="KeyNotFoundException"></exception>
        public void RemoveConnection(int leftId, int rightId)
        {
            if (!connections.Exists(connection => (connection.LeftID == leftId && connection.RightID == rightId) ||
                                                 (connection.LeftID == rightId && connection.RightID == leftId)))
            {
                throw new KeyNotFoundException($"Connection with the specified IDs ({leftId}, {rightId}) not found.");
            }
            connections.RemoveAll(connection => connection.LeftID == leftId && connection.RightID == rightId);
        }

        /// <summary>
        ///     Gets a connection by its endpoint IDs.
        /// </summary>
        /// <param name="leftId">The connection ID for the left side.</param>
        /// <param name="rightId">The connection ID for the right side.</param>
        /// <returns>The Connection object with the specified IDs.</returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public Connection GetConnection(int leftId, int rightId)
        {
            var connection = connections.Find(connection => (connection.LeftID == leftId && connection.RightID == rightId) ||
            (connection.LeftID == rightId && connection.RightID == leftId));
            if (connection == null)
            {
                throw new KeyNotFoundException($"Connection with the specified IDs not found.");
            }
            return connection;
        }

        /// <summary>
        ///     Gets all connections that reference the specified block ID.
        /// </summary>
        /// <param name="id">The block ID to search for in connections.</param>
        /// <returns>A read-only collection of connections that reference the specified ID.</returns>
        public Connection[] GetConnections(int id)
        {
            return connections.Where(connection => connection.LeftID == id || connection.RightID == id).ToArray();
        }

        /// <summary>
        ///     Gets all connections in the project.
        /// </summary>
        /// <returns>A read-only collection of all connections.</returns>
        public IReadOnlyCollection<Connection> GetAllConnections()
        {
            return connections.AsReadOnly();
        }

        public (IItem, int)[] UsedItems
        {
            get
            {
                var usedItems = new List<(IItem, int)>();
                foreach(ConductorBlock block in blocks.Where(b => b.BlockType == "ConductorBlock").Cast<ConductorBlock>())
                {
                    try
                    {
                        if (block.Equipment != null)
                        {
                            usedItems.Add((block.Equipment, block.ID));
                        }
                    }
                    catch (InvalidDataException)
                    {
                        // No equipment assigned - skip this block
                    }
                }

                foreach (TransformerUPSBlock block in blocks.Where(b => b.BlockType == "TransformerUPS").Cast<TransformerUPSBlock>())
                {
                    try
                    {
                        if (block.Equipment != null)
                        {
                            usedItems.Add((block.Equipment, block.ID));
                        }
                    }
                    catch (InvalidDataException)
                    {
                        // No equipment assigned - skip this block
                    }
                }

                foreach(AlternatorBlock block in blocks.Where(b => b.BlockType == "AlternatorBlock").Cast<AlternatorBlock>())
                {
                    try
                    {
                        if (block.Equipment != null)
                        {
                            usedItems.Add((block.Equipment, block.ID));
                        }
                    }
                    catch (InvalidDataException)
                    {
                        // No equipment assigned - skip this block
                    }
                }

                foreach (Load block in blocks.Where(b => b.BlockType == "Load").Cast<Load>())
                {
                    try
                    {
                        if (block.Equipment != null)
                        {
                            usedItems.Add((block.Equipment, block.ID));
                        }
                    }
                    catch (InvalidDataException)
                    {
                        // No equipment assigned - skip this block
                    }
                }

                return usedItems.ToArray();
            }
        }

        /// <summary>
        ///     Exports the entire project to an array of strings.
        /// </summary>
        /// <returns>An array of strings containing all project data, formatted for export.</returns>
        public string[] ExportProjectText()
        {
            var lines = new List<string>();
            lines.Add(";Queensland Rail Signalling Power App");
            lines.Add($";Initially developed by: {AppVersion.VersionHistory[0].Author}");
            lines.Add($";Version author: {AppVersion.CurrentVersion.Author}");
            lines.Add($";Version: {AppVersion.CurrentVersion.Major}.{AppVersion.CurrentVersion.Minor}");
            lines.Add($";Version released: {AppVersion.CurrentVersion.BuildDate:yyyy-MM-dd}");
            lines.Add(string.Empty);
            lines.Add($";Project save date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            lines.Add(string.Empty);
            lines.Add(";===== PROJECT DATA =====");
            lines.AddRange([
                "Begin Project",
                $"    Name \"{Name}\"",
                $"    SaveID {SaveID}",
                $"    MajorVersion {MajorVersion}",
                $"    MinorVersion {MinorVersion}",
                $"    Designer \"{Designer}\"",
                $"    DesignDate {DesignDate}",
                $"    DesignRPEQ {DesignRPEQ}",
                $"    Checker \"{Checker}\"",
                $"    CheckDate {CheckDate}",
                $"    CheckRPEQ {CheckRPEQ}",
                "End Project"
            ]);
            lines.Add(string.Empty);
            lines.Add(";===== EQUIPMENT DATA =====");
            
            // Group equipment by their properties
            var equipmentGroups = new Dictionary<string, List<int>>();
            foreach (var (item, blockId) in UsedItems)
            {
                // Create a unique key for this equipment based on its export text
                string equipmentKey = string.Join("|", item.ExportItemText());
                
                if (!equipmentGroups.ContainsKey(equipmentKey))
                {
                    equipmentGroups[equipmentKey] = new List<int>();
                }
                equipmentGroups[equipmentKey].Add(blockId);
            }
            
            // Export each unique equipment with AssignedTo line
            foreach (var (equipmentKey, blockIds) in equipmentGroups)
            {
                // Reconstruct the equipment lines from the key
                string[] equipmentLines = equipmentKey.Split('|');
                
                // Add all lines except the "End" line
                for (int i = 0; i < equipmentLines.Length - 1; i++)
                {
                    lines.Add(equipmentLines[i]);
                }
                
                // Add AssignedTo line before the End line
                lines.Add($"    AssignedTo {string.Join(" ", blockIds)}");
                
                // Add the End line
                lines.Add(equipmentLines[^1]);
                lines.Add(string.Empty);
            }
            
            lines.Add(";===== BLOCK DATA =====");
            foreach (var block in blocks)
            {
                // Skip Terminal blocks as they're now embedded in their parent blocks
                if (block is Terminal) continue;
                
                lines.AddRange(block.ExportBlockText());
                lines.Add(string.Empty);
            }
            lines.Add(";===== CONNECTION DATA =====");
            lines.Add("Begin Connections");
            foreach (var connection in connections)
            {
                lines.Add($"    {connection.ExportConnnectionText()}");
            }
            lines.Add("End Connections");
            lines.Add(string.Empty);

            return lines.ToArray();
        }
    }

    /// <summary>
    ///     Class representing a valid connection between two IDs.
    /// </summary>
    public class Connection
    {
        private readonly int leftId;                            // ID for the left endpoint
        private readonly int rightId;                           // ID for the right endpoint
        private readonly List<(int, int)> renderPoints = [];    // Points for rendering the connection
        private readonly Project project;


        /// <summary>
        ///     Initialises a new instance of the Connection class.
        /// </summary>
        /// <param name="leftId">The ID for the left endpoint.</param>
        /// <param name="rightId">The ID for the right endpoint.</param>
        public Connection(int leftId, int rightId, Project project)
        {
            if (project == null) 
            {
                throw new ArgumentNullException(nameof(project), "Project instance cannot be null.");
            }

            this.leftId = leftId;
            this.rightId = rightId;
            this.project = project;
        }

        /// <summary>
        ///     Gets the connection ID for the left endpoint.
        /// </summary>
        public int LeftID { get { return leftId; } }

        /// <summary>
        ///     Gets the connection ID for the right endpoint.
        /// </summary>
        public int RightID { get { return rightId; } }

        /// <summary>
        ///     A read-only collection of points for rendering the connection.
        /// </summary>
        /// <returns>A read-only collection of (x, y) tuples representing render points.</returns>
        public IReadOnlyCollection<(int, int)> RenderPoints { get { return renderPoints.AsReadOnly(); } }

        /// <summary>
        ///     Adds a render point to the connection.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void AddRenderPoint(int x, int y)
        {
            renderPoints.Add((x, y));
        }

        /// <summary>
        ///     Removes a render point from the connection.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void RemoveRenderPoint(int x, int y)
        {
            renderPoints.RemoveAll(point => point.Item1 == x && point.Item2 == y);
        }

        /// <summary>
        ///     Exports the connection to text format.
        /// </summary>
        /// <returns>The connection data formatted for export.</returns>
        public string ExportConnnectionText()
        {
            var parts = new List<string>();

            parts.Add(leftId.ToString());
            parts.Add(rightId.ToString());

            if (renderPoints.Count != 0)
            {
                foreach (var point in renderPoints)
                {
                    parts.Add(point.Item1.ToString());
                    parts.Add(point.Item2.ToString());
                }

                parts[2] = $"\"{parts[2]}";
                parts[^1] = $"{parts[^1]}\"";
            }

            return string.Join(" ", parts);
        }
    }

    /// <summary>
    ///     An interface representing a generic block item with an ID and type.
    /// </summary>
    public interface IBlock
    {
        /// <summary>
        ///     Gets the unique identifier for the block.
        /// </summary>
        int ID { get; }

        /// <summary>
        ///     Gets the unique identifier for the parent block.
        /// </summary>
        int ParentID { get; }

        /// <summary>
        ///     Gets the type of the block.
        /// </summary>
        string BlockType { get; }

        /// <summary>
        ///     Gets the parent project of the block.
        /// </summary>
        Project Project { get; }

        /// <summary>
        ///     Removes the block and all its children from the project.
        /// </summary>
        void Remove();

        /// <summary>
        ///     Exports the block to text format.
        /// </summary>
        /// <returns>The block data formatted for export.</returns>
        string[] ExportBlockText();
    }

    /// <summary>
    ///     Class representing a location that contains multiple busbars.
    /// </summary>
    public class Location : IBlock, IInteractiveBlock
    {
        private readonly int id;                            // Unique identifier for the location
        private readonly Project project;                   // Reference to the parent project
        private string name;                                // Name of the location
        private (int?, int?) renderPosition = (null,null);  // Render position of location

        private const double locationWidth = 200;
        private const double locationHeight = 200;

        /// <summary>
        ///     Initialises a new instance of the Location class.
        /// </summary>
        /// <param name="id">The unique identifier for the location.</param>
        /// <param name="project">The parent project instance.</param>
        public Location(int id, Project project)
        {
            this.project = project;
            if (project.ContainsBlock(id))
            {
                throw new InvalidDataException($"Block with ID '{id}' already exists in the project.");
            }
            this.id = id;
            this.name = $"Location {project.GetAllBlocks.Count(block => block.BlockType == "Location") + 1}";
        }

        public int ID { get { return id; } }
        public int ParentID { get { return -1; } }
        public string BlockType { get { return "Location"; } }
        public Project Project { get { return project; } }
        public (int?, int?) RenderPosition
        {
            get { return renderPosition; }
            set { renderPosition = value; }
        }

        /// <summary>
        ///     Gets or sets the name of the location.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        ///     Gets the external busbar of the location.
        /// </summary>
        public Terminal[] ExternalTerminals
        {
            get
            {
                return GetChildren().Where(block => block.BlockType == "Terminal").Cast<Terminal>()
                    .Cast<Terminal>()
                    .OrderBy(t => t.Side)
                    .ToArray();
            }
        }

        public ExternalBusbar ExternalBusbar
        {
            get
            {
                return GetChildren().Where(block => block.BlockType == "ExternalBusbar").Cast<ExternalBusbar>().FirstOrDefault()
                    ?? throw new InvalidDataException("Critical Error: Location does not have an external busbar.");
            }
        }

        public CanvasType PreferredCanvas => CanvasType.Layout;

        public ConnectionDotInfo[] GetConnectionDotPositions(double dotOffset)
        {
            var terminals = ExternalTerminals.OrderBy(t => t.Side).ToArray();
            var dotInfos = new List<ConnectionDotInfo>();

            // 8 positions around the border (clockwise from top-left)
            var relativePositions = new[]
            {
                // Top edge
                (locationWidth / 3 - locationWidth / 2, -locationHeight / 2),           // Top-left (0)
                (2 * locationWidth / 3 - locationWidth / 2, -locationHeight / 2),       // Top-right (1)
                
                // Right edge
                (locationWidth / 2, locationHeight / 3 - locationHeight / 2),           // Right-top (2)
                (locationWidth / 2, 2 * locationHeight / 3 - locationHeight / 2),       // Right-bottom (3)
                
                // Bottom edge
                (2 * locationWidth / 3 - locationWidth / 2, locationHeight / 2),        // Bottom-right (4)
                (locationWidth / 3 - locationWidth / 2, locationHeight / 2),            // Bottom-left (5)
                
                // Left edge
                (-locationWidth / 2, 2 * locationHeight / 3 - locationHeight / 2),      // Left-bottom (6)
                (-locationWidth / 2, locationHeight / 3 - locationHeight / 2)           // Left-top (7)
            };

            for (int i = 0; i < terminals.Length && i < relativePositions.Length; i++)
            {
                dotInfos.Add(new ConnectionDotInfo
                {
                    RelativeX = relativePositions[i].Item1,
                    RelativeY = relativePositions[i].Item2,
                    TerminalId = terminals[i].ID,
                    Position = i,
                    Tag = terminals[i].ID
                });
            }

            return dotInfos.ToArray();
        }

        public (double width, double height) GetRenderDimensions()
        {
            return (locationWidth, locationHeight);
        }

        public string GetUIElementType()
        {
            return "Border";
        }

        // FUTURE IMPLEMENTATION. ALLOW FOR MORE THAN FOUR TERMINALS

        /// <summary>
        ///     Adds a new external terminal to the location.
        /// </summary>
        /// <returns>The newly created Terminal object.</returns>
        //public Terminal AddExternalTerminal()
        //{
        //    return (Terminal)project.AddBlock("Terminal", id);
        //}

        //public void RemoveExternalTerminal(int id)
        //{
        //    var terminal = project.GetBlock(id);
        //    if (terminal.ParentID != this.id || terminal.BlockType != "Terminal")
        //    {
        //        throw new InvalidDataException("The specified block is not an external terminal of this location.");
        //    }
        //    project.RemoveBlock(id);
        //}

        /// <summary>
        ///     Adds a new busbar to the location.
        /// </summary>
        /// <returns>The newly created Busbar object.</returns>
        public Busbar AddBusbar()
        {
            return project.AddBlock("Busbar", id) as Busbar ?? throw new Exception("Failed to create a new Busbar");
        }

        /// <summary>
        ///     Adds a new TransformerUPS to the location.
        /// </summary>
        /// <returns>The newly created TransformerUPS object.</returns>
        /// <exception cref="Exception"></exception>
        public TransformerUPS AddTransformerUPS()
        {
            return project.AddBlock("TransformerUPS", id) as TransformerUPS ?? throw new Exception("Failed to create a new TransformerUPS");
        }

        /// <summary>
        ///     Gets all children in the location.
        /// </summary>
        /// <returns>A read-only collection of all busbars & transformers.</returns>
        public IReadOnlyCollection<IBlock> GetChildren()
        {
            var children = project.GetAllBlocks.Where(block => block.ParentID == id).Cast<IBlock>().ToList();
            return children.AsReadOnly();
        }

        public void Remove()
        {
            var children = GetChildren();
            foreach (var child in children)
            {
                child.Remove();
            }
            project.RemoveBlock(id);
        }

        public string[] ExportBlockText()
        {
            var terminalIds = ExternalTerminals.Select(t => t.ID.ToString()).ToArray();
            var lines = new List<string>
            {
                "Begin Location",
                $"    ID {ID}",
                $"    Name \"{Name}\"",
                $"    RenderPosition {RenderPosition.Item1} {RenderPosition.Item2}",
                $"    Terminals {string.Join(" ", terminalIds)}",
                "End Location"
            };
            return lines.ToArray();
        }
    }

    /// <summary>
    ///     Class representing a busbar that contains multiple rows.
    /// </summary>
    public class Busbar : IBlock, IInteractiveBlock
    {
        private readonly int id;                                // Unique identifier for the busbar
        private readonly int parentID;                          // ID of the parent location
        private readonly Project project;                       // Reference to the parent project
        private string name;                                    // Name of the busbar
        private (int?, int?) renderPosition = (null, null);     // Render position of busbar

        // Public static constants for rendering dimensions
        public const double BusbarWidth = 350;
        public const double NameHeight = 35;
        public const double RowHeight = 50;
        public const double PlusButtonSize = 40;
        public const double PlusButtonGap = 5;

        public Busbar(int id, int parentID, Project project)
        {
            if (project.ContainsBlock(id))
            {
                throw new InvalidDataException($"Block with ID '{id}' already exists in the project.");
            }
            if(!project.ContainsBlock(parentID))
            {
                throw new InvalidDataException($"Parent block with ID '{parentID}' does not exist in the project.");
            }
            if (project.GetBlock(parentID) is not Location parent)
            {
                throw new InvalidDataException($"Parent block with ID '{parentID}' must be a Location.");
            }

            this.id = id;
            this.parentID = parentID;
            this.project = project;
            this.name = $"Busbar {parent.GetChildren().Count(block => block.BlockType == "Busbar") + 1}";
        }

        public int ID { get { return id; } }
        public int ParentID { get { return parentID; } }
        public string BlockType { get { return "Busbar"; } }
        public Project Project { get { return project; } }
        public (int?, int?) RenderPosition
        {
            get { return renderPosition; }
            set { renderPosition = value; }
        }

        /// <summary>
        ///     Gets or sets the name of the busbar.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public CanvasType PreferredCanvas => CanvasType.Location;

        public ConnectionDotInfo[] GetConnectionDotPositions(double dotOffset)
        {
            // Busbar itself doesn't have connection dots - its rows do
            return Array.Empty<ConnectionDotInfo>();
        }

        public (double width, double height) GetRenderDimensions()
        {
            const double busbarWidth = BusbarWidth;
            const double nameHeight = NameHeight;
            const double rowHeight = RowHeight;
            const double titleHeight = PlusButtonSize;

            var rows = GetRows();
            double busbarContentHeight = nameHeight + (rows.Count * rowHeight);
            double totalHeight = busbarContentHeight + titleHeight;

            return (busbarWidth, totalHeight);
        }

        public string GetUIElementType()
        {
            return "StackPanel";
        }

        /// <summary>
        ///     Gets the index of a row within the busbar.
        /// </summary>
        /// <param name="rowId"></param>
        /// <returns>The zero-based index of the row.</returns>
        /// <exception cref="InvalidDataException"></exception>
        public int GetIndexOfRow(int rowId)
        {
            var rows = GetRows().ToList();
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].ID == rowId)
                {
                    return i;
                }
            }
            throw new InvalidDataException($"Row with ID '{rowId}' not found in this busbar.");
        }

        /// <summary>
        ///     Adds a new row to the busbar.
        /// </summary>
        /// <returns>The newly created Row object.</returns>
        public Row AddRow()
        {
            return project.AddBlock("Row", id) as Row ?? throw new Exception("Failed to create a new Row");
        }

        /// <summary>
        ///     Removes a row from the busbar.
        /// </summary>
        /// <param name="id">The ID of the row to remove.</param>
        /// <exception cref="KeyNotFoundException"></exception>
        public void RemoveRow(int id)
        {
            project.RemoveBlock(id);
        }

        /// <summary>
        ///     Gets all rows in the busbar.
        /// </summary>
        /// <returns>A read-only collection of all rows.</returns>
        public IReadOnlyCollection<Row> GetRows()
        {
            var allRows = project.GetAllBlocks.Where(block => block.BlockType == "Row" && block.ParentID == id).Cast<Row>().ToList();
            return allRows.AsReadOnly();
        }

        public void Remove()
        {
            var allRows = GetRows();
            foreach (var row in allRows)
            {
                row.Remove();
            }
            project.RemoveBlock(id);
        }

        public string[] ExportBlockText()
        {
            var lines = new List<string>
            {
                "Begin Busbar",
                $"    ID {ID}",
                $"    ParentID {ParentID}",
                $"    Name \" {Name} \"",
                $"    RenderPosition {RenderPosition.Item1} {RenderPosition.Item2}",
                "End Busbar"
            };
            return lines.ToArray();
        }
    }

    /// <summary>
    ///     Class representing a row in a busbar, which can be either a Pin or CircuitBreaker.
    /// </summary>
    public class Row : IBlock, IInteractiveBlock
    {
        private readonly int id;                                                // Unique identifier for the row
        private readonly int parentID;                                          // ID of the parent busbar
        private readonly Project project;                                       // Reference to the parent project
        private static readonly string[] types = { "Pin", "CircuitBreaker" };   // Valid row types
        private string type = "Pin";                                            // Type of protection
        private int rating = 0;                                                 // Rating for CircuitBreaker type

        private const double busbarWidth = 350;
        private const double rowHeight = 50;

        public Row(int id, int parentID, Project project)
        {
            if (project.ContainsBlock(id))
            {
                throw new InvalidDataException($"Block with ID '{id}' already exists in the project.");
            }
            if (!project.ContainsBlock(parentID))
            {
                throw new InvalidDataException($"Parent block with ID '{parentID}' does not exist in the project.");
            }
            if (project.GetBlock(parentID) is not Busbar)
            {
                throw new InvalidDataException($"Parent block with ID '{parentID}' must be a Busbar.");
            }

            this.id = id;
            this.parentID = parentID;
            this.project = project;
        }

        public int ID { get { return id; } }
        public int ParentID { get { return parentID; } }
        public string BlockType { get { return "Row"; } }
        public Project Project { get { return project; } }
        public Terminal[] Terminals 
        { 
            get 
            { 
                return project.GetAllBlocks.Where(block => block.BlockType == "Terminal" && block.ParentID == id)
                    .Cast<Terminal>()
                    .OrderBy(t => t.Side)
                    .ToArray();
            } 
        }

        /// <summary>
        ///     Gets or sets the type of the row (Pin or CircuitBreaker).
        /// </summary>
        /// <exception cref="InvalidDataException"></exception>
        public string Type 
        { 
            get { return type; }
            set 
            { 
                // When changing from CircuitBreaker to Pin, reset rating
                if (value == "Pin" && type == "CircuitBreaker")
                {
                    type = value;
                    rating = 0;
                } else if (Array.Exists(types, t => t == value))
                {
                    type = value;
                }
                else
                {
                    throw new InvalidDataException($"Protection type '{value}' is not valid. Valid types are: {string.Join(", ", types)}");
                }
            }
        }

        /// <summary>
        ///     Gets or sets the rating for CircuitBreaker type rows.
        /// </summary>
        /// <exception cref="InvalidDataException"></exception>
        public int Rating 
        { 
            get 
            { 
                if (type == "CircuitBreaker") 
                {
                    return rating;
                }
                throw new InvalidDataException("'Pin' type does not have a rating.");
            }
            set 
            {
                if (type != "CircuitBreaker")
                {
                    throw new InvalidDataException("'Pin' type does not have a rating.");
                }
                
                if (value < 0)
                {
                    throw new InvalidDataException("Rating must be a non-negative integer.");
                }
                
                rating = value;
            }
        }

        public CanvasType PreferredCanvas => CanvasType.Location;

        public ConnectionDotInfo[] GetConnectionDotPositions(double dotOffset)
        {
            var terminals = Terminals.OrderBy(t => t.Side).ToArray();

            return new[]
            {
                new ConnectionDotInfo
                {
                    RelativeX = -busbarWidth / 2,
                    RelativeY = 0,
                    TerminalId = terminals[0].ID,
                    Position = 0,
                    Tag = new { BlockId = terminals[0].ID, RowId = this.ID, Position = 0 }
                },
                new ConnectionDotInfo
                {
                    RelativeX = busbarWidth / 2,
                    RelativeY = 0,
                    TerminalId = terminals[1].ID,
                    Position = 1,
                    Tag = new { BlockId = terminals[1].ID, RowId = this.ID, Position = 1 }
                }
            };
        }

        public (double width, double height) GetRenderDimensions()
        {
            return (busbarWidth, rowHeight);
        }

        public string GetUIElementType()
        {
            return "Grid";
        }

        public void Remove()
        {
            int terminal1ID = Terminals[0].ID;
            int terminal2ID = Terminals[1].ID;
            project.RemoveBlock(terminal1ID);
            project.RemoveBlock(terminal2ID);
            project.RemoveBlock(id);
        }

        public string[] ExportBlockText()
        {
            var terminalIds = Terminals.Select(t => t.ID.ToString()).ToArray();
            var lines = new List<string>
            {
                "Begin Row",
                $"    ID {ID}",
                $"    ParentID {ParentID}",
                $"    Type \"{Type}\"",
            };
            if (Type == "CircuitBreaker")
            {
                lines.Add($"    Rating {Rating}");
            }
            lines.Add($"    Terminals {string.Join(" ", terminalIds)}");
            lines.Add("End Row");
            return lines.ToArray();
        }
    }

    /// <summary>
    ///     Class representing a terminal connection point on a row.
    /// </summary>
    public class Terminal : IBlock
    {
        private readonly int id;                // Terminal ID
        private readonly int parentID;          // ID of the parent row
        private readonly int side;              // Side of the terminal (0 or 1)
        private readonly Project project;       // Reference to the parent project

        public Terminal(int id, int parentID, int side, Project project)
        {
            if (project.ContainsBlock(id))
            {
                throw new InvalidDataException($"Block with ID '{id}' already exists in the project.");
            }
            if (!project.ContainsBlock(parentID))
            {
                throw new InvalidDataException($"Parent block with ID '{parentID}' does not exist in the project.");
            }
            if (project.GetBlock(parentID) is not (Location or TransformerUPSBlock or ConductorBlock or Row or ExternalBusbar))
            {
                throw new InvalidDataException($"Parent block with ID '{parentID}' must be a Location, TransformerUPSBlock, ConductorBlock, Row, or ExternalBusbar.");
            }

            this.id = id;
            this.parentID = parentID;
            this.project = project;
            this.side = side;
        }

        public int ID { get { return id; } }
        public int ParentID { get { return parentID; } }
        public string BlockType { get { return "Terminal"; } }
        public Project Project { get { return project; } }
        public int Side { get { return side; } }

        public void Remove()
        {
            project.RemoveBlock(id);
        }

        public string[] ExportBlockText()
        {
            // Terminal export is now handled by parent blocks
            // This method exists only to satisfy the IBlock interface
            var lines = new List<string>
            {
                "Begin Terminal",
                $"    ID {ID}",
                $"    ParentID {ParentID}",
                $"    Side {Side}",
                "End Terminal"
            };
            return lines.ToArray();
        }
    }

    /// <summary>
    ///     Class representing a supply block.
    /// </summary>
    public class Supply : IBlock, IInteractiveBlock
    {
        private readonly int id;                                // Unique identifier for the supply
        private readonly Project project;                       // Reference to the parent project
        private string name;                                    // Name of the supply
        private int voltage = 230;                              // Voltage level of the supply
        private double impedance = 1.6;                         // Impedance of the supply
        private (int?, int?) renderPosition = (null, null);     // Render position of supply

        private const double supplyDiameter = 150;

        public Supply(int id, Project project)
        {
            if (project.ContainsBlock(id))
            {
                throw new InvalidDataException($"Block with ID '{id}' already exists in the project.");
            }

            this.id = id;
            this.project = project;
            this.name = $"Supply {project.GetAllBlocks.Count(block => block.BlockType == "Supply") + 1}";
        }

        public int ID { get { return id; } }
        public int ParentID { get { return -1; } }
        public string BlockType { get { return "Supply"; } }
        public Project Project { get { return project; } }
        public (int?, int?) RenderPosition
        {
            get { return renderPosition; }
            set { renderPosition = value; }
        }
        public int Voltage 
        { 
            get { return voltage; } 
            set 
            { 
                if (value <= 0)
                {
                    throw new InvalidDataException("Voltage must be a positive integer.");
                }
                voltage = value; 
            }
        }
        public double Impedance 
        { 
            get { return impedance; } 
            set 
            { 
                if (value < 0)
                {
                    throw new InvalidDataException("Impedance must be a non-negative number.");
                }
                impedance = value; 
            }
        }

        /// <summary>
        ///     Gets or sets the name of the supply.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public CanvasType PreferredCanvas => CanvasType.Layout;

        public ConnectionDotInfo[] GetConnectionDotPositions(double dotOffset)
        {
            return new[]
            {
                new ConnectionDotInfo
                {
                    RelativeX = 0,
                    RelativeY = supplyDiameter / 2,
                    TerminalId = this.ID,
                    Position = 0,
                    Tag = this.ID
                }
            };
        }

        public (double width, double height) GetRenderDimensions()
        {
            return (supplyDiameter, supplyDiameter);
        }

        public string GetUIElementType()
        {
            return "Ellipse";
        }

        public void Remove()
        {
            project.RemoveBlock(id);
        }

        public string[] ExportBlockText()
        {
            var lines = new List<string>
            {
                "Begin Supply",
                $"    ID {ID}",
                $"    Name \" {Name} \"",
                $"    Voltage {Voltage}",
                $"    Impedance {Impedance}",
                $"    RenderPosition {RenderPosition.Item1} {RenderPosition.Item2}",
                "End Supply"
            };
            return lines.ToArray();
        }
    }

    /// <summary>
    ///     Class representing an alternator block that can contain alternator equipment.
    /// </summary>
    public class AlternatorBlock : IBlock, IInteractiveBlock
    {
        private readonly int id;                                // Unique identifier for the alternator block
        private readonly Project project;                       // Reference to the parent project
        private string name;                                    // Name of the alternator block
        private Alternator? equipment = null;                   // Optional alternator equipment
        private (int?, int?) renderPosition = (null, null);     // Render position of alternatorBlock

        private const double diamondSize = 150;

        public AlternatorBlock(int id, Project project)
        {
            if (project.ContainsBlock(id))
            {
                throw new InvalidDataException($"Block with ID '{id}' already exists in the project.");
            }

            this.id = id;
            this.project = project;
            this.name = $"Alternator {project.GetAllBlocks.Count(block => block.BlockType == "Alternator") + 1}";
        }

        public int ID { get { return id; } }
        public int ParentID { get { return -1; } }
        public string BlockType { get { return "AlternatorBlock"; } }
        public Project Project { get { return project; } }
        public (int?, int?) RenderPosition
        {
            get { return renderPosition; }
            set { renderPosition = value; }
        }

        /// <summary>
        ///     Gets or sets the name of the alternator block.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        ///     Gets or sets the alternator equipment assigned to this block.
        /// </summary>
        /// <exception cref="InvalidDataException"></exception>
        public Alternator Equipment
        {
            get 
            { 
                if (equipment == null)
                {
                    throw new InvalidDataException("No equipment assigned yet.");
                }
                return equipment;
            }
            set { equipment = value; }
        }

        public CanvasType PreferredCanvas => CanvasType.Layout;

        public ConnectionDotInfo[] GetConnectionDotPositions(double dotOffset)
        {
            return new[]
            {
                new ConnectionDotInfo
                {
                    RelativeX = 0,
                    RelativeY = diamondSize / 2,
                    TerminalId = this.ID,
                    Position = 0,
                    Tag = this.ID
                }
            };
        }

        public (double width, double height) GetRenderDimensions()
        {
            return (diamondSize, diamondSize);
        }

        public string GetUIElementType()
        {
            return "Polygon";
        }


        public void Remove()
        {
            project.RemoveBlock(id);
        }

        public string[] ExportBlockText()
        {
            var lines = new List<string>
            {
                "Begin AlternatorBlock",
                $"    ID {ID}",
                $"    Name \" {Name} \"",
                $"    RenderPosition {RenderPosition.Item1} {RenderPosition.Item2}",
                "End AlternatorBlock"
            };
            return lines.ToArray();
        }
    }

    /// <summary>
    ///     Class representing a transformerUPS block that can contain transformer equipment.
    /// </summary>
    public class TransformerUPSBlock : IBlock, IInteractiveBlock
    {
        private readonly int id;                                // Unique identifier for the transformer/UPS block
        private readonly int parentID;                          // ID of the parent block, if any
        private readonly Project project;                       // Reference to the parent project
        private string name;                                    // Name of the transformerUPS block
        private TransformerUPS? equipment = null;               // Optional transformerUPS equipment
        private (int?, int?) renderPosition = (null, null);     // Render position of transformerUPSBlock

        private const double circleRadius = 50;
        private const double circleDiameter = circleRadius * 2;
        private const double overlap = 30;
        private const double totalWidth = (circleDiameter * 2) - overlap;
        private const double totalHeight = circleDiameter;

        public TransformerUPSBlock(int id, int parentID, Project project)
        {
            if (project.ContainsBlock(id))
            {
                throw new InvalidDataException($"Block with ID '{id}' already exists in the project.");
            }
            if (!project.ContainsBlock(parentID))
            {
                throw new InvalidDataException($"Parent block with ID '{parentID}' does not exist in the project.");
            }
            if (project.GetBlock(parentID) is not Location parent)
            {
                throw new InvalidDataException($"Parent block with ID '{parentID}' must be a Location.");
            }

            this.id = id;
            this.parentID = parentID;
            this.project = project;
            this.name = $"TransformerUPS {parent.GetChildren().Count(block => block.BlockType == "TransformerUPS") + 1}";
        }

        public int ID { get { return id; } }
        public int ParentID { get { return parentID; } }
        public string BlockType { get { return "TransformerUPS"; } }
        public Project Project { get { return project; } }
        public (int?, int?) RenderPosition
        {
            get { return renderPosition; }
            set { renderPosition = value; }
        }

        /// <summary>
        ///     Gets or sets the name of the transformerUPS block.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        ///     Gets the terminal associated with this transformerUPS block.
        /// </summary>
        public Terminal[] Terminals
        {
            get
            {
                return project.GetAllBlocks.Where(block => block.BlockType == "Terminal" && block.ParentID == id)
                    .Cast<Terminal>()
                    .OrderBy(t => t.Side)
                    .ToArray();
            }
        }

        /// <summary>
        ///     Gets or sets the transformerUPS equipment assigned to this block.
        /// </summary>
        /// <exception cref="InvalidDataException"></exception>
        public TransformerUPS Equipment
        {
            get 
            { 
                if (equipment == null)
                {
                    throw new InvalidDataException("No equipment assigned yet.");
                }
                return equipment;
            }
            set { equipment = value; }
        }

        public CanvasType PreferredCanvas => CanvasType.Location;

        public ConnectionDotInfo[] GetConnectionDotPositions(double dotOffset)
        {
            var terminals = Terminals.OrderBy(t => t.Side).ToArray();

            return new[]
            {
                new ConnectionDotInfo
                {
                    RelativeX = -totalWidth / 2,
                    RelativeY = 0,
                    TerminalId = terminals[0].ID,
                    Position = 0,
                    Tag = new { BlockId = terminals[0].ID, TransformerUPSId = this.ID, Position = 0 }
                },
                new ConnectionDotInfo
                {
                    RelativeX = totalWidth / 2,
                    RelativeY = 0,
                    TerminalId = terminals[1].ID,
                    Position = 1,
                    Tag = new { BlockId = terminals[1].ID, TransformerUPSId = this.ID, Position = 1 }
                }
            };
        }

        public (double width, double height) GetRenderDimensions()
        {
            return (totalWidth, totalHeight);
        }

        public string GetUIElementType()
        {
            return "Grid";
        }

        public void Remove()
        {
            int terminal1ID = Terminals[0].ID;
            int terminal2ID = Terminals[1].ID;
            project.RemoveBlock(terminal1ID);
            project.RemoveBlock(terminal2ID);
            project.RemoveBlock(id);
        }

        public string[] ExportBlockText()
        {
            var terminalIds = Terminals.Select(t => t.ID.ToString()).ToArray();
            var lines = new List<string>
            {
                "Begin TransformerUPSBlock",
                $"    ID {ID}",
                $"    ParentID {ParentID}",
                $"    Name \" {Name} \"",
                $"    RenderPosition {RenderPosition.Item1} {RenderPosition.Item2}",
                $"    Terminals {string.Join(" ", terminalIds)}",
                "End TransformerUPSBlock"
            };
            return lines.ToArray();
        }
    }

    /// <summary>
    ///     Class representing a load block that can contain consumer equipment.
    /// </summary>
    public class Load : IBlock, IInteractiveBlock
    {
        private readonly int id;                                // Unique identifier for the load block
        private readonly int parentID;                          // ID of the parent block
        private readonly Project project;                       // Reference to the parent project
        private string name;                                    // Name of the load block
        private (int?, int?) renderPosition = (null, null);     // Render position of load block
        private Consumer? equipment = null;                     // Optional consumer equipment

        private const double hexagonSize = 120;
        
        public Load(int id, int parentID, Project project)
        {
            if (project.ContainsBlock(id))
            {
                throw new InvalidDataException($"Block with ID '{id}' already exists in the project.");
            }
            if (!project.ContainsBlock(parentID))
            {
                throw new InvalidDataException($"Parent block with ID '{parentID}' does not exist in the project.");
            }
            if (project.GetBlock(parentID) is not Location parent)
            {
                throw new InvalidDataException($"Parent block with ID '{parentID}' must be a location.");
            }


            this.id = id;
            this.parentID = parentID;
            this.project = project;
            this.name = $"Load {parent.GetChildren().Count(block => block.BlockType == "Load") + 1}";
        }

        public int ID { get { return id; } }
        public int ParentID { get { return parentID; } }
        public string BlockType { get { return "Load"; } }
        public Project Project { get { return project; } }
        public (int?, int?) RenderPosition
        {
            get { return renderPosition; }
            set { renderPosition = value; }
        }

        /// <summary>
        ///     Gets or sets the name of the load block.
        /// </summary>
        public string Name
        {
            get { return name; }
            set 
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidDataException("Load name cannot be empty.");
                }

                if (value.Length > 50)
                {
                    throw new InvalidDataException("Load name cannot exceed 50 characters.");
                }
                name = value; 
            }
        }

        /// <summary>
        ///     Gets or sets the consumer equipment assigned to this block.
        /// </summary>
        /// <exception cref="InvalidDataException"></exception>
        public Consumer Equipment
        {
            get 
            { 
                if (equipment == null)
                {
                    throw new InvalidDataException("No equipment assigned yet.");
                }
                return equipment;
            }
            set { equipment = value; }
        }

        public CanvasType PreferredCanvas => CanvasType.Location;

        public ConnectionDotInfo[] GetConnectionDotPositions(double dotOffset)
        {
            return new[]
            {
                new ConnectionDotInfo
                {
                    RelativeX = 0,
                    RelativeY = hexagonSize / 2,
                    TerminalId = this.ID,
                    Position = 0,
                    Tag = new { BlockId = this.ID, LoadId = this.ID }
                }
            };
        }

        public (double width, double height) GetRenderDimensions()
        {
            return (hexagonSize, hexagonSize);
        }

        public string GetUIElementType()
        {
            return "Grid";
        }

        public void Remove()
        {
            project.RemoveBlock(id);
        }

        public string[] ExportBlockText()
        {
            var lines = new List<string>
            {
                "Begin Load",
                $"    ID {ID}",
                $"    ParentID {ParentID}",
                $"    Name \" {Name} \"",
                $"    RenderPosition {RenderPosition.Item1} {RenderPosition.Item2}",
                "End Load"
            };
            return lines.ToArray();
        }
    }

    /// <summary>
    ///     Class representing a conductor block that can contain a conductor.
    /// </summary>
    public class ConductorBlock : IBlock, IInteractiveBlock
    {
        private readonly int id;                                                // Unique identifier for the conductor block
        private readonly Project project;                                       // Reference to the parent project
        private string name;                                                    // Name of the conductor block
        private (int?, int?) renderPosition = (null, null);                     // Render position of conductorBlock
        private Conductor? equipment = null;                                    // Optional conductor equipment
        private double length = 0;                                              // Length of the conductor in meters

        private const double conductorWidth = 300;
        private const double conductorHeight = 100;

        public ConductorBlock(int id, Project project)
        {
            if (project.ContainsBlock(id))
            {
                throw new InvalidDataException($"Block with ID '{id}' already exists in the project.");
            }

            this.id = id;
            this.project = project;
            this.name = $"Conductor {project.GetAllBlocks.Count(block => block.BlockType == "ConductorBlock") + 1}";
        }

        public int ID { get { return id; } }
        public int ParentID { get { return -1; } }
        public string BlockType { get { return "ConductorBlock"; } }
        public Project Project { get { return project; } }
        public (int?, int?) RenderPosition
        {
            get { return renderPosition; }
            set { renderPosition = value; }
        }

        /// <summary>
        ///     Gets or sets the name of the conductor block.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        ///     Gets or sets the length of the conductor in meters.
        /// </summary>
        public double Length
        {
            get { return length; }
            set
            {
                if (value < 0)
                {
                    throw new InvalidDataException("Length must be a non-negative number.");
                }
                length = value;
            }
        }

        /// <summary>
        ///     Gets the terminal associated with this conductor block.
        /// </summary>
        public Terminal[] Terminals
        {
            get
            {
                return project.GetAllBlocks.Where(block => block.BlockType == "Terminal" && block.ParentID == id)
                    .Cast<Terminal>()
                    .OrderBy(t => t.Side)
                    .ToArray();
            }
        }

        /// <summary>
        ///     Gets or sets the conductor equipment assigned to this block.
        /// </summary>
        /// <exception cref="InvalidDataException"></exception>
        public Conductor Equipment
        {
            get
            {
                if (equipment == null)
                {
                    throw new InvalidDataException("No equipment assigned yet.");
                }
                return equipment;
            }
            set { equipment = value; }
        }

        public CanvasType PreferredCanvas => CanvasType.Layout;

        public ConnectionDotInfo[] GetConnectionDotPositions(double dotOffset)
        {
            var terminals = Terminals.OrderBy(t => t.Side).ToArray();

            return new[]
            {
                new ConnectionDotInfo
                {
                    RelativeX = -conductorWidth / 2,
                    RelativeY = 0,
                    TerminalId = terminals[0].ID,
                    Position = 0,
                    Tag = new { BlockId = terminals[0].ID, ConductorId = this.ID, Position = 0 }
                },
                new ConnectionDotInfo
                {
                    RelativeX = conductorWidth / 2,
                    RelativeY = 0,
                    TerminalId = terminals[1].ID,
                    Position = 1,
                    Tag = new { BlockId = terminals[1].ID, ConductorId = this.ID, Position = 1 }
                }
            };
        }

        public (double width, double height) GetRenderDimensions()
        {
            return (conductorWidth, conductorHeight);
        }

        public string GetUIElementType()
        {
            return "Border";
        }

        public void Remove()
        {
            int terminal1ID = Terminals[0].ID;
            int terminal2ID = Terminals[1].ID;
            project.RemoveBlock(terminal1ID);
            project.RemoveBlock(terminal2ID);
            project.RemoveBlock(id);
        }

        public string[] ExportBlockText()
        {
            var terminalIds = Terminals.Select(t => t.ID.ToString()).ToArray();
            var lines = new List<string>
            {
                "Begin ConductorBlock",
                $"    ID {ID}",
                $"    Name \" {Name} \"",
                $"    Length {Length}",
                $"    RenderPosition {RenderPosition.Item1} {RenderPosition.Item2}",
                $"    Terminals {string.Join(" ", terminalIds)}",
                "End ConductorBlock"
            };
            return lines.ToArray();
        }
    }

    /// <summary>
    ///     Class representing an ExternalBusbar block.
    /// </summary>
    public class ExternalBusbar : IBlock, IInteractiveBlock
    {
        private readonly int id;                                                // Unique identifier for the externalBusbar block
        private readonly int parentID;                                          // ID of the parent block
        private readonly Project project;                                       // Reference to the parent project
        private (int?, int?) renderPosition = (null, null);                     // Render position of externalBusbar

        private const double nameWidth = 40;
        private const double rowsWidth = 100;
        private const double totalWidth = nameWidth + rowsWidth;
        private const double rowHeight = 50;
        private const int numberOfRows = 8;
        private const double totalHeight = numberOfRows * rowHeight;

        public ExternalBusbar(int id, int parentID, Project project)
        {
            if (project.ContainsBlock(id))
            {
                throw new InvalidDataException($"Block with ID '{id}' already exists in the project.");
            }
            if (!project.ContainsBlock(parentID))
            {
                throw new InvalidDataException($"Parent block with ID '{parentID}' does not exist in the project.");
            }
            if (project.GetBlock(parentID) is not Location)
            {
                throw new InvalidDataException($"Parent block with ID '{parentID}' must be a location.");
            }

            this.id = id;
            this.parentID = parentID;
            this.project = project;
        }

        public int ID { get { return id; } }
        public int ParentID { get { return parentID; } }
        public string BlockType { get { return "ExternalBusbar"; } }
        public Project Project { get { return project; } }
        public (int?, int?) RenderPosition
        {
            get { return renderPosition; }
            set { renderPosition = value; }
        }

        /// <summary>
        ///     Gets the name of the ExternalBusbar block.
        /// </summary>
        public string Name
        {
            get { return "External Busbar"; }
        }

        /// <summary>
        ///     Gets the terminals associated with this ExternalBusbar block (its own 8 terminals).
        /// </summary>
        public Terminal[] Terminals
        {
            get
            {
                return project.GetAllBlocks
                    .Where(block => block.BlockType == "Terminal" && block.ParentID == this.id)
                    .Cast<Terminal>()
                    .OrderBy(t => t.Side)
                    .ToArray();
            }
        }

        public CanvasType PreferredCanvas => CanvasType.Location;

        public ConnectionDotInfo[] GetConnectionDotPositions(double dotOffset)
        {
            var terminals = Terminals.OrderBy(t => t.Side).ToArray();
            if (terminals.Length != 8) return new ConnectionDotInfo[0];

            var dotInfos = new List<ConnectionDotInfo>();

            for (int i = 0; i < numberOfRows; i++)
            {
                // Calculate relative position from ExternalBusbar center
                double rowCenterY = (i * rowHeight) + (rowHeight / 2) - (numberOfRows * rowHeight / 2);

                dotInfos.Add(new ConnectionDotInfo
                {
                    RelativeX = totalWidth / 2,
                    RelativeY = rowCenterY,
                    TerminalId = terminals[i].ID,
                    Position = i,
                    Tag = new { BlockId = terminals[i].ID, ExternalBusbarId = this.ID, Position = i }
                });
            }

            return dotInfos.ToArray();
        }

        public (double width, double height) GetRenderDimensions()
        {
            return (totalWidth, totalHeight);
        }

        public string GetUIElementType()
        {
            return "Canvas";
        }

        public void Remove()
        {
            // Remove all terminals associated with this ExternalBusbar
            var terminals = Terminals;
            foreach (var terminal in terminals)
            {
                project.RemoveBlock(terminal.ID);
            }
            project.RemoveBlock(id);
        }

        public string[] ExportBlockText()
        {
            var terminalIds = Terminals.Select(t => t.ID.ToString()).ToArray();
            var lines = new List<string>
            {
                "Begin ExternalBusbar",
                $"    ID {ID}",
                $"    ParentID {ParentID}",
                $"    RenderPosition {RenderPosition.Item1} {RenderPosition.Item2}",
                $"    Terminals {string.Join(" ", terminalIds)}",
                "End ExternalBusbar"
            };
            return lines.ToArray();
        }
    }
}