using System.Dynamic;
using System.Xml.Linq;

namespace SignallingPowerApp.Core
{
    /// <summary>
    ///     An abstract helper class for providing common text import methods.
    /// </summary>
    public abstract class ImportText
    {
        protected static AllItems ImportItemsText(string[] lines)
        {
            AllItems importedItems = new();

            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    string[] line = CleanLine(lines[i]);

                    if (line[0] != "Begin")
                    {
                        continue;
                    }

                    string[] sectionLines = [];

                    switch (line[1])
                    {
                        case "Conductor":
                            (sectionLines, i) = GetSectionLines(lines, "Conductor", i);
                            var (conductor, _) = ConductorText(i + 1 - sectionLines.Length, sectionLines);
                            importedItems.AddItem(conductor);
                            break;
                        case "TransformerUPS":
                            (sectionLines, i) = GetSectionLines(lines, "TransformerUPS", i);
                            var (transformerUPS, _) = TransformerUPSText(i + 1 - sectionLines.Length, sectionLines);
                            importedItems.AddItem(transformerUPS);
                            break;
                        case "Alternator":
                            (sectionLines, i) = GetSectionLines(lines, "Alternator", i);
                            var (alternator, _) = AlternatorText(i + 1 - sectionLines.Length, sectionLines);
                            importedItems.AddItem(alternator);
                            break;
                        case "Consumer":
                            (sectionLines, i) = GetSectionLines(lines, "Consumer", i);
                            var (consumer, _) = ConsumerText(i + 1 - sectionLines.Length, sectionLines);
                            importedItems.AddItem(consumer);
                            break;
                        default:
                            throw new FormatException($"Unknown section in equipment data.");
                    }
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"{ex.Message} [Line {1 + i}]");
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"{ex.Message} [Line {1 + i}]");
                }
            }

            return importedItems;
        }


        /// <summary>
        ///     Imports project metadata from text lines.
        /// </summary>
        /// <param name="startRef">The text file start line reference for the project.</param>
        /// <param name="lines">An array of strings representing the project.</param>
        /// <returns>A new Project object created from the imported data.</returns>
        /// <exception cref="FormatException"></exception>
        protected static Project ImportProjectText(string[] lines, AllItems items)
        {
            // Import project metadata
            Project? project = null;
            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    string[] line = CleanLine(lines[i]);

                    if (line.Length > 2 || line[0] != "Begin" || line[1] != "Project")
                    {
                        continue;
                    }

                    string[] sectionLines = [];
                    (sectionLines, i) = GetSectionLines(lines, "Project", i);
                    project = ProjectText(sectionLines.Length - 1, sectionLines);
                    break;
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"{ex.Message} [Line {1 + i}]");
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"{ex.Message} [Line {1 + i}]");
                }
            }

            if (project == null) throw new ArgumentNullException("Project object not yet initialised.");

            // Import blocks
            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    string[] line = CleanLine(lines[i]);

                    if (line[0] != "Begin")
                    {
                        continue;
                    }

                    string[] sectionLines = [];

                    switch (line[1])
                    {
                        case "Location":
                            (sectionLines, i) = GetSectionLines(lines, "Location", i);
                            // LocationText imports itself and its terminals
                            LocationText(1 - sectionLines.Length, sectionLines, project);
                            break;
                        case "Busbar":
                            (sectionLines, i) = GetSectionLines(lines, "Busbar", i);
                            project.ImportBlock(BusbarText(1 - sectionLines.Length, sectionLines, project));
                            break;
                        case "Row":
                            (sectionLines, i) = GetSectionLines(lines, "Row", i);
                            // RowText imports itself and its terminals
                            RowText(1 - sectionLines.Length, sectionLines, project);
                            break;
                        case "Terminal":
                            (sectionLines, i) = GetSectionLines(lines, "Terminal", i);
                            project.ImportBlock(TerminalText(1 - sectionLines.Length, sectionLines, project));
                            break;
                        case "TransformerUPSBlock":
                            (sectionLines, i) = GetSectionLines(lines, "TransformerUPSBlock", i);
                            // TransformerUPSBlockText imports itself and its terminals
                            TransformerUPSBlockText(1 - sectionLines.Length, sectionLines, project);
                            break;
                        case "Supply":
                            (sectionLines, i) = GetSectionLines(lines, "Supply", i);
                            project.ImportBlock(SupplyText(1 - sectionLines.Length, sectionLines, project));
                            break;
                        case "AlternatorBlock":
                            (sectionLines, i) = GetSectionLines(lines, "AlternatorBlock", i);
                            project.ImportBlock(AlternatorBlockText(1 - sectionLines.Length, sectionLines, project));
                            break;
                        case "Load":
                            (sectionLines, i) = GetSectionLines(lines, "Load", i);
                            project.ImportBlock(LoadText(1 - sectionLines.Length, sectionLines, project));
                            break;
                        case "ConductorBlock":
                            (sectionLines, i) = GetSectionLines(lines, "ConductorBlock", i);
                            // ConductorBlockText imports itself and its terminals
                            ConductorBlockText(1 - sectionLines.Length, sectionLines, project);
                            break;
                        case "ExternalBusbar":
                            (sectionLines, i) = GetSectionLines(lines, "ExternalBusbar", i);
                            // ExternalBusbarText imports itself and its terminals
                            ExternalBusbarText(1 - sectionLines.Length, sectionLines, project);
                            break;
                        default:
                            if (line[1] != "Project" && line[1] != "Connections" && line[1] != "Conductor" && line[1] != "TransformerUPS"
                                && line[1] != "Alternator" && line[1] != "Consumer")
                            {
                                throw new FormatException($"Unknown section in save file data.");
                            }
                            continue;
                    }
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"{ex.Message} [Line {1 + i}]");
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"{ex.Message} [Line {1 + i}]");
                }
            }

            // Import connections
            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    string[] line = CleanLine(lines[i]);

                    if (line.Length > 2 || line[0] != "Begin" || line[1] != "Connections")
                    {
                        continue;
                    }

                    string[] sectionLines = [];
                    (sectionLines, i) = GetSectionLines(lines, "Connections", i);
                    project.ImportConnections(ConnectionsText(1 - sectionLines.Length, sectionLines, project));
                    break;
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"{ex.Message} [Line {1 + i}]");
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"{ex.Message} [Line {1 + i}]");
                }
            }


            // Import items and assign to blocks
            List<(IItem, int[]?)> importedItems = [];
            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    string[] line = CleanLine(lines[i]);

                    if (line[0] != "Begin")
                    {
                        continue;
                    }

                    string[] sectionLines = [];

                    switch (line[1])
                    {
                        case "Conductor":
                            (sectionLines, i) = GetSectionLines(lines, "Conductor", i);
                            var conductorResult = ConductorText(i + 1 - sectionLines.Length, sectionLines);
                            // Parse AssignedTo IDs if present
                            int[]? conductorIDs = ParseAssignedToFromLines(sectionLines);
                            importedItems.Add((conductorResult.Item1, conductorIDs ?? (conductorResult.Item2 != null ? new int[] { (int)conductorResult.Item2 } : null)));
                            break;
                        case "TransformerUPS":
                            (sectionLines, i) = GetSectionLines(lines, "TransformerUPS", i);
                            var transformerResult = TransformerUPSText(i + 1 - sectionLines.Length, sectionLines);
                            // Parse AssignedTo IDs if present
                            int[]? transformerIDs = ParseAssignedToFromLines(sectionLines);
                            importedItems.Add((transformerResult.Item1, transformerIDs ?? (transformerResult.Item2 != null ? new int[] { (int)transformerResult.Item2 } : null)));
                            break;
                        case "Alternator":
                            (sectionLines, i) = GetSectionLines(lines, "Alternator", i);
                            var alternatorResult = AlternatorText(i + 1 - sectionLines.Length, sectionLines);
                            // Parse AssignedTo IDs if present
                            int[]? alternatorIDs = ParseAssignedToFromLines(sectionLines);
                            importedItems.Add((alternatorResult.Item1, alternatorIDs ?? (alternatorResult.Item2 != null ? new int[] { (int)alternatorResult.Item2 } : null)));
                            break;
                        case "Consumer":
                            (sectionLines, i) = GetSectionLines(lines, "Consumer", i);
                            var consumerResult = ConsumerText(i + 1 - sectionLines.Length, sectionLines);
                            // Parse AssignedTo IDs if present
                            int[]? consumerIDs = ParseAssignedToFromLines(sectionLines);
                            importedItems.Add((consumerResult.Item1, consumerIDs ?? (consumerResult.Item2 != null ? new int[] { (int)consumerResult.Item2 } : null)));
                            break;
                        default:
                            continue;
                    }
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"{ex.Message} [Line {1 + i}]");
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"{ex.Message} [Line {1 + i}]");
                }
            }

            if (project == null)
            {
                throw new FormatException("No project data found in import text.");
            }

            // Validate that all blocks that require two terminals have exactly two terminals
            IBlock[] checkBlocks = project.GetAllBlocks.Where(b => b is TransformerUPSBlock or Row or ConductorBlock).ToArray();
            foreach (IBlock block in checkBlocks)
            {
                int count = 0;
                foreach (Terminal terminal in project.GetAllBlocks.Where(b => b is Terminal && b.ParentID == block.ID))
                {
                    if (project.GetConnection(block.ID, terminal.ID) == null)
                    {
                        throw new FormatException($"Terminal {terminal.ID} on Block ID {block.ID} is not connected. Please make sure the save file was not edited");
                    }
                    count++;
                }

                if (count != 2)
                {
                    throw new FormatException($"Block ID {block.ID} does not have exactly two terminals. Please maek sure the save file was not edited");
                }
            }

            // Validate that all locations have exactly eight terminals
            checkBlocks = project.GetAllBlocks.Where(b => b is Location).ToArray();
            foreach (IBlock block in checkBlocks)
            {
                if (project.GetAllBlocks.Count(b => b.ParentID == block.ID && b is Terminal) != 8)
                {
                    throw new FormatException($"Location {block.ID} does not have exactly eight terminals. Please make sure the save file was not edited");
                }
            }

            // Validate that all ExternalBusbars have exactly eight terminals
            checkBlocks = project.GetAllBlocks.Where(b => b is ExternalBusbar).ToArray();
            foreach (IBlock block in checkBlocks)
            {
                int terminalCount = project.GetAllBlocks.Count(b => b.ParentID == block.ID && b is Terminal);
                if (terminalCount != 8)
                {
                    throw new FormatException($"ExternalBusbar {block.ID} does not have exactly eight terminals (found {terminalCount}). Please make sure the save file was not edited");
                }
            }

            // Assign imported items to blocks
            foreach ((IItem importedItem, int[]? blockParentIDs) in importedItems)
            {
                IItem? item = items.ContainsNonCustomItem(importedItem);

                if (item == null)
                {
                    item = importedItem;
                    item.SetCustom();
                    items.AddItem(item);
                }

                if (blockParentIDs == null)
                {
                    continue;
                }

                // Assign to all blocks in the AssignedTo list
                foreach (int blockParentID in blockParentIDs)
                {
                    IBlock parentBlock = project.GetBlock(blockParentID);

                    if ((item is Conductor conductor && parentBlock is ConductorBlock conductorBlock))
                    {
                        conductorBlock.Equipment = conductor;
                    }
                    else if (item is TransformerUPS transformerUPS && parentBlock is TransformerUPSBlock transformerUPSBlock)
                    {
                        transformerUPSBlock.Equipment = transformerUPS;
                    }
                    else if (item is Alternator alternator && parentBlock is AlternatorBlock alternatorBlock)
                    {
                        alternatorBlock.Equipment = alternator;
                    }
                    else if (item is Consumer consumer && parentBlock is Load load)
                    {
                        load.Equipment = consumer;
                    }
                    else
                    {
                        throw new FormatException($"Item type '{item.GetType}' does not match parent block type '{parentBlock.GetType}'.");
                    }
                }
            }

            return project;
        }

        /// <summary>
        ///     Gets lines for a section until the end statement is found.
        /// </summary>
        /// <param name="lines">Array of all lines in the file.</param>
        /// <param name="section">The type of section being parsed.</param>
        /// <param name="refLine">The current line reference.</param>
        /// <returns>A tuple containing the list of section lines and the updated line reference.</returns>
        /// <exception cref="FormatException"></exception>
        protected static (string[], int) GetSectionLines(string[] lines, string section, int refLine)
        {
            if (!lines[refLine].Trim().Equals($"Begin {section}"))
            {
                throw new FormatException($"Expected 'Begin {section}' statement. [Line: {refLine + 1}]");
            }

            var output = new List<string>();
            refLine++; // Move to the next line

            // Collect all lines until "End {section}" is found
            while (!lines[refLine].Trim().Equals($"End {section}"))
            {
                var checkLine = lines[refLine].Trim().Split(' ')[0];
                if (checkLine.Equals("Begin") || checkLine.Equals("End"))
                {
                    throw new FormatException($"Missing 'End' statement in {section} data. [Line: {refLine + 1}]");
                }

                output.Add(lines[refLine]);
                refLine++;

                if (refLine >= lines.Length)
                {
                    throw new FormatException($"Missing 'End' statement in {section} data. [Line: {refLine + 1}]");
                }
            }

            return (output.ToArray(), refLine);
        }

        protected static (Conductor, int?) ConductorText(int startRef, string[] lines)
        {
            bool? custom = null;
            int? cores = null;
            int? strandCount = null;
            double? strandDiameter = null;
            double? crossSectionalArea = null;
            string? description = null;
            double? voltageDrop60 = null;
            double? voltageDrop90 = null;
            double? reactance = null;
            double? resistance60 = null;
            double? resistance90 = null;

            int[]? parentIDs = null;

            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    // Get cleaned input
                    string[] input = CleanLine(lines[i]);

                    // Process each property based on the key
                    switch (input[0])
                    {
                        case "Custom":
                            custom = ValidateBool("Custom", input);
                            break;
                        case "Cores":
                            cores = ValidateInt("Cores", input);
                            break;
                        case "StrandCount":
                            strandCount = ValidateInt("StrandCount", input);
                            break;
                        case "StrandDiameter":
                            strandDiameter = ValidateDouble("StrandDiameter", input);
                            break;
                        case "CrossSectionalArea":
                            crossSectionalArea = ValidateDouble("CrossSectionalArea", input);
                            break;
                        case "Description":
                            description = ValidateString("Description", input);
                            break;
                        case "VoltageDrop60":
                            voltageDrop60 = ValidateDouble("VoltageDrop60", input);
                            break;
                        case "VoltageDrop90":
                            voltageDrop90 = ValidateDouble("VoltageDrop90", input);
                            break;
                        case "Reactance":
                            reactance = ValidateDouble("Reactance", input);
                            break;
                        case "Resistance60":
                            resistance60 = ValidateDouble("Resistance60", input);
                            break;
                        case "Resistance90":
                            resistance90 = ValidateDouble("Resistance90", input);
                            break;
                        case "ParentID":
                            parentIDs = new int[] { ValidateInt("ParentID", input) };
                            break;
                        case "AssignedTo":
                            parentIDs = ValidateAssignedToIds("AssignedTo", input);
                            break;
                        default:
                            // Skip empty lines
                            if (string.IsNullOrWhiteSpace(lines[i])) continue;

                            // Unknown property
                            throw new FormatException($"Unknown property in conductor data.");
                    }
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"{ex.Message} [Line {startRef + i}]");
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"{ex.Message} [Line {startRef + i}]");
                }
            }

            custom ??= false;
            Conductor conductor = new((bool)custom);
            if (cores != null) { conductor.Cores = (int)cores; }
            if (strandCount != null) { conductor.StrandCount = (int)strandCount; }
            if (strandDiameter != null) { conductor.StrandDiameter = (double)strandDiameter; }
            if (crossSectionalArea != null) { conductor.CrossSectionalArea = (double)crossSectionalArea; }
            if (description != null) { conductor.Description = description; }
            if (voltageDrop60 != null) { conductor.VoltageDrop60 = (double)voltageDrop60; }
            if (voltageDrop90 != null) { conductor.VoltageDrop90 = (double)voltageDrop90; }
            if (reactance != null) { conductor.Reactance = (double)reactance; }
            if (resistance60 != null) { conductor.Resistance60 = (double)resistance60; }
            if (resistance90 != null) { conductor.Resistance90 = (double)resistance90; }

            // Return conductor with parentIDs array (or null if not specified)
            return (conductor, parentIDs != null && parentIDs.Length > 0 ? parentIDs[0] : null);
        }

        protected static (TransformerUPS, int?) TransformerUPSText(int startRef, string[] lines)
        {
            bool? custom = null;
            int? rating = null;
            double? percentageZ = null;
            int? primaryVoltage = null;
            int? secondaryVoltage = null;
            string? description = null;

            int[]? parentIDs = null;

            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    // Get cleaned input
                    string[] input = CleanLine(lines[i]);

                    // Process each property based on the key
                    switch (input[0])
                    {
                        case "Custom":
                            custom = ValidateBool("Custom", input);
                            break;
                        case "Rating":
                            rating = ValidateInt("Rating", input);
                            break;
                        case "PercentageZ":
                            percentageZ = ValidateDouble("PercentageZ", input);
                            break;
                        case "PrimaryVoltage":
                            primaryVoltage = ValidateInt("PrimaryVoltage", input);
                            break;
                        case "SecondaryVoltage":
                            secondaryVoltage = ValidateInt("SecondaryVoltage", input);
                            break;
                        case "Description":
                            description = ValidateString("Description", input);
                            break;
                        case "ParentID":
                            parentIDs = new int[] { ValidateInt("ParentID", input) };
                            break;
                        case "AssignedTo":
                            parentIDs = ValidateAssignedToIds("AssignedTo", input);
                            break;
                        default:
                            // Skip empty lines
                            if (string.IsNullOrWhiteSpace(input[0])) continue;

                            // Unknown property
                            throw new FormatException($"Unknown property in transformerUPS data. [Line {startRef + i}]");
                    }
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"{ex.Message} [Line {startRef + i}]");
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"{ex.Message} [Line {startRef + i}]");
                }
            }

            custom ??= false;
            TransformerUPS transformerUPS = new((bool)custom);
            if (rating != null) { transformerUPS.Rating = (int)rating; }
            if (percentageZ != null) { transformerUPS.PercentageZ = (double)percentageZ; }
            if (primaryVoltage != null) { transformerUPS.PrimaryVoltage = (int)primaryVoltage; }
            if (secondaryVoltage != null) { transformerUPS.SecondaryVoltage = (int)secondaryVoltage; }
            if (description != null) { transformerUPS.Description = description; }

            return (transformerUPS, parentIDs != null && parentIDs.Length > 0 ? parentIDs[0] : null);
        }

        protected static (Alternator, int?) AlternatorText(int startRef, string[] lines)
        {
            bool? custom = null;
            int? ratingVA = null;
            int? ratingW = null;
            string? description = null;

            int[]? parentIDs = null;

            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    // Get cleaned input
                    string[] input = CleanLine(lines[i]);

                    // Process each property based on the key
                    switch (input[0])
                    {
                        case "Custom":
                            custom = ValidateBool("Custom", input);
                            break;
                        case "RatingVA":
                            ratingVA = ValidateInt("RatingVA", input);
                            break;
                        case "RatingW":
                            ratingW = ValidateInt("RatingW", input);
                            break;
                        case "Description":
                            description = ValidateString("Description", input);
                            break;
                        case "ParentID":
                            parentIDs = new int[] { ValidateInt("ParentID", input) };
                            break;
                        case "AssignedTo":
                            parentIDs = ValidateAssignedToIds("AssignedTo", input);
                            break;
                        default:
                            // Skip empty lines
                            if (string.IsNullOrWhiteSpace(lines[i])) continue;

                            // Unknown property
                            throw new FormatException($"Unknown property in alternator data.");
                    }
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"{ex.Message} [Line {startRef + i}]");
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"{ex.Message} [Line {startRef + i}]");
                }
            }

            custom ??= false;
            Alternator alternator = new((bool)custom);
            if (ratingVA != null) { alternator.RatingVA = (int)ratingVA; }
            if (ratingW != null) { alternator.RatingW = (int)ratingW; }
            if (description != null) { alternator.Description = description; }

            return (alternator, parentIDs != null && parentIDs.Length > 0 ? parentIDs[0] : null);
        }

        protected static (Consumer, int?) ConsumerText(int startRef, string[] lines)
        {
            bool? custom = null;
            string? name = null;
            string? description = null;
            int? load = null;

            int[]? parentIDs = null;

            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    // Get cleaned input
                    string[] input = CleanLine(lines[i]);

                    // Process each property based on the key
                    switch (input[0])
                    {
                        case "Custom":
                            custom = ValidateBool("Custom", input);
                            break;
                        case "Name":
                            name = ValidateString("Name", input);
                            break;
                        case "Description":
                            description = ValidateString("Description", input);
                            break;
                        case "Load":
                            load = ValidateInt("Load", input);
                            break;
                        case "ParentID":
                            parentIDs = new int[] { ValidateInt("ParentID", input) };
                            break;
                        case "AssignedTo":
                            parentIDs = ValidateAssignedToIds("AssignedTo", input);
                            break;
                        default:
                            // Skip empty lines
                            if (string.IsNullOrWhiteSpace(lines[i])) continue;

                            // Unknown property
                            throw new FormatException($"Unknown property in consumer data.");
                    }
                }
                catch (FormatException ex)
                {
                    throw new FormatException($"{ex.Message} [Line {startRef + i}]");
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"{ex.Message} [Line {startRef + i}]");
                }
            }

            custom ??= false;
            Consumer consumer = new((bool)custom);
            if (name != null) { consumer.Name = name; }
            if (description != null) { consumer.Description = description; }
            if (load != null) { consumer.Load = (int)load; }

            return (consumer, parentIDs != null && parentIDs.Length > 0 ? parentIDs[0] : null);
        }

        /// <summary>
        ///     Transforms a section of text into a Project object.
        /// </summary>
        /// <param name="startRef">The starting line reference for error reporting.</param>
        /// <param name="section">The lines of text in the section.</param>
        /// <returns>A Project object populated with the section data.</returns>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="Exception"></exception>
        protected static Project ProjectText(int startRef, string[] section)
        {
            string? name = null;
            long? saveID = null;
            string? designer = null;
            int? designDate = null;
            int? designRPEQ = null;
            string? checker = null;
            int? checkDate = null;
            int? checkRPEQ = null;

            int? majorVersion = null;
            int? minorVersion = null;


            for (int i = 0; i < section.Length; i++)
            {
                try
                {
                    string[] input = CleanLine(section[i]);

                    switch (input[0])
                    {
                        case "Name":
                            name = ValidateString("Name", input);
                            break;
                        case "SaveID":
                            saveID = ValidateLong("SaveID", input);
                            break;
                        case "Designer":
                            designer = ValidateString("Designer", input);
                            break;
                        case "DesignDate":
                            designDate = ValidateInt("DesignDate", input);
                            break;
                        case "Checker":
                            checker = ValidateString("Checker", input);
                            break;
                        case "CheckDate":
                            checkDate = ValidateInt("CheckDate", input);
                            break;
                        case "MajorVersion":
                            majorVersion = ValidateInt("MajorVersion", input);
                            break;
                        case "MinorVersion":
                            minorVersion = ValidateInt("MinorVersion", input);
                            break;
                        case "DesignRPEQ":
                            designRPEQ = ValidateInt("DesignRPEQ", input);
                            break;
                        case "CheckRPEQ":
                            checkRPEQ = ValidateInt("CheckRPEQ", input);
                            break;
                        default:
                            // Skip empty lines
                            if (string.IsNullOrWhiteSpace(input[0])) continue;

                            // Unknown property
                            throw new FormatException($"Unknown category in project data.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message} [Line {startRef + i}]");
                }
            }

            if (saveID == null)
            {
                throw new FormatException($"Missing SaveID in project data. [Line: {startRef}]");
            }

            // Build the project object
            Project project = new((long)saveID);
            if (name != null) { project.Name = name; }
            if (designer != null) { project.Designer = designer; }
            if (designDate != null) { project.DesignDate = (int)designDate; }
            if (checker != null) { project.Checker = checker; }
            if (checkDate != null) { project.CheckDate = (int)checkDate; }
            if (majorVersion != null) { project.MajorVersion = (int)majorVersion; }
            if (minorVersion != null) { project.MinorVersion = (int)minorVersion; }
            if (designRPEQ != null) { project.DesignRPEQ = (int)designRPEQ; }
            if (checkRPEQ != null) { project.CheckRPEQ = (int)checkRPEQ; }

            return project;
        }

        /// <summary>
        ///     Transforms a section of text into a Location object.
        /// </summary>
        /// <param name="startRef">The starting line reference for error reporting.</param>
        /// <param name="section">The lines of text in the section.</param>
        /// <returns>A Location object populated with the section data.</returns>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="Exception"></exception>
        protected static Location LocationText(int startRef, string[] section, Project project)
        {
            int? id = null;
            string? name = null;
            (int?, int?) renderPosition = (null, null);
            int[]? terminalIds = null;

            for (int i = 0; i < section.Length; i++)
            {
                try
                {
                    string[] input = CleanLine(section[i]);
                    switch (input[0])
                    {
                        case "ID":
                            id = ValidateInt("ID", input);
                            break;
                        case "Name":
                            name = ValidateString("Name", input);
                            break;
                        case "RenderPosition":
                            renderPosition = ValidatePosition("RenderPosition", input);
                            break;
                        case "Terminals":
                            terminalIds = ValidateTerminalIds("Terminals", input);
                            break;
                        default:
                            // Skip empty lines
                            if (string.IsNullOrWhiteSpace(input[0])) continue;

                            // Unknown property
                            throw new FormatException($"Unknown category in location data.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message} [Line {startRef + i}]");
                }
            }

            if (id == null)
            {
                throw new FormatException($"Missing ID in location data. [Line: {startRef}]");
            }

            if (terminalIds == null || terminalIds.Length != 8)
            {
                throw new FormatException($"Location must have exactly 8 terminals. [Line: {startRef}]");
            }

            Location location = new((int)id, project);
            if (name != null) { location.Name = name; }
            if (renderPosition.Item1 != null && renderPosition.Item2 != null) { location.RenderPosition = ((int)renderPosition.Item1, (int)renderPosition.Item2); }

            // Import the block before creating terminals
            project.ImportBlock(location);

            // Create the terminals with the specified IDs
            for (int i = 0; i < terminalIds.Length; i++)
            {
                Terminal terminal = new Terminal(terminalIds[i], (int)id, i, project);
                project.ImportBlock(terminal);
            }

            return location;
        }

        /// <summary>
        ///     Transforms a section of text into a Busbar object.
        /// </summary>
        /// <param name="startRef">The starting line reference for error reporting.</param>
        /// <param name="section">The lines of text in the section.</param>
        /// <returns>A Busbar object populated with the section data.</returns>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="Exception"></exception>
        protected static Busbar BusbarText(int startRef, string[] section, Project project)
        {
            int? id = null;
            int? parentID = null;
            string? name = null;
            (int?, int?) renderPosition = (null, null);

            for (int i = 0; i < section.Length; i++)
            {
                try
                {
                    string[] input = CleanLine(section[i]);
                    switch (input[0])
                    {
                        case "ID":
                            id = ValidateInt("ID", input);
                            break;
                        case "ParentID":
                            parentID = ValidateInt("ParentID", input);
                            break;
                        case "Name":
                            name = ValidateString("Name", input);
                            break;
                        case "RenderPosition":
                            renderPosition = ValidatePosition("RenderPosition", input);
                            break;
                        default:
                            // Skip empty lines
                            if (string.IsNullOrWhiteSpace(input[0])) continue;

                            // Unknown property
                            throw new FormatException($"Unknown category in busbar data.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message} [Line {startRef + i}]");
                }
            }

            if (id == null)
            {
                throw new FormatException($"Missing ID in busbar data. [Line: {startRef}]");
            }
            if (parentID == null)
            {
                throw new FormatException($"Missing ParentID in busbar data. [Line: {startRef}]");
            }

            Busbar busbar = new((int)id, (int)parentID, project);
            if (name != null) { busbar.Name = name; }
            if (renderPosition.Item1 != null && renderPosition.Item2 != null) { busbar.RenderPosition = ((int)renderPosition.Item1, (int)renderPosition.Item2); }

            return busbar;
        }

        /// <summary>
        ///     Transforms a section of text into a Row object.
        /// </summary>
        /// <param name="startRef">The starting line reference for error reporting.</param>
        /// <param name="section">The lines of text in the section.</param>
        /// <returns>A Row object populated with the section data.</returns>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="Exception"></exception>
        protected static Row RowText(int startRef, string[] section, Project project)
        {
            int? id = null;
            int? parentID = null;
            string? type = null;
            int? rating = null;
            int[]? terminalIds = null;

            for (int i = 0; i < section.Length; i++)
            {
                try
                {
                    string[] input = CleanLine(section[i]);
                    switch (input[0])
                    {
                        case "ID":
                            id = ValidateInt("ID", input);
                            break;
                        case "ParentID":
                            parentID = ValidateInt("ParentID", input);
                            break;
                        case "Type":
                            type = ValidateString("Type", input);
                            break;
                        case "Rating":
                            rating = ValidateInt("Rating", input);
                            break;
                        case "Terminals":
                            terminalIds = ValidateTerminalIds("Terminals", input);
                            break;
                        default:
                            // Skip empty lines
                            if (string.IsNullOrWhiteSpace(input[0])) continue;

                            // Unknown property
                            throw new FormatException($"Unknown category in row data.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message} [Line {startRef + i}]");
                }
            }

            if (id == null)
            {
                throw new FormatException($"Missing ID in row data. [Line: {startRef}]");
            }
            if (parentID == null)
            {
                throw new FormatException($"Missing ParentID in row data. [Line: {startRef}]");
            }

            if (terminalIds == null || terminalIds.Length != 2)
            {
                throw new FormatException($"Row must have exactly 2 terminals. [Line: {startRef}]");
            }

            Row row = new((int)id, (int)parentID, project);
            if (type != null) { row.Type = type; }
            if (rating != null) { row.Rating = (int)rating; }

            // Import the block before creating terminals
            project.ImportBlock(row);

            // Create the terminals with the specified IDs
            for (int i = 0; i < terminalIds.Length; i++)
            {
                Terminal terminal = new Terminal(terminalIds[i], (int)id, i, project);
                project.ImportBlock(terminal);
            }

            return row;
        }

        /// <summary>
        ///     Transforms a section of text into a Terminal object.
        /// </summary>
        /// <param name="startRef">The starting line reference for error reporting.</param>
        /// <param name="section">The lines of text in the section.</param>
        /// <returns>A Terminal object populated with the section data.</returns>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="Exception"></exception>
        protected static Terminal TerminalText(int startRef, string[] section, Project project)
        {
            int? id = null;
            int? parentID = null;
            int? side = null;

            for (int i = 0; i < section.Length; i++)
            {
                try
                {
                    string[] input = CleanLine(section[i]);
                    switch (input[0])
                    {
                        case "ID":
                            id = ValidateInt("ID", input);
                            break;
                        case "ParentID":
                            parentID = ValidateInt("ParentID", input);
                            break;
                        case "Side":
                            side = ValidateInt("Side", input);
                            break;
                        default:
                            // Skip empty lines
                            if (string.IsNullOrWhiteSpace(input[0])) continue;

                            // Unknown property
                            throw new FormatException($"Unknown category in terminal data.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message} [Line {startRef + i}]");
                }
            }

            if (id == null)
            {
                throw new FormatException($"Missing ID in terminal data. [Line: {startRef}]");
            }
            if (parentID == null)
            {
                throw new FormatException($"Missing ParentID in terminal data. [Line: {startRef}]");
            }
            if (side == null)
            {
                throw new FormatException($"Missing side in terminal data. [Line: {startRef}]");
            }

            Terminal terminal = new((int)id, (int)parentID, (int)side, project);
            return terminal;
        }

        /// <summary>
        ///     Transforms a section of text into a Supply object.
        /// </summary>
        /// <param name="startRef">The starting line reference for error reporting.</param>
        /// <param name="section">The lines of text in the section.</param>
        /// <returns>A Supply object populated with the section data.</returns>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="Exception"></exception>
        protected static Supply SupplyText(int startRef, string[] section, Project project)
        {
            int? id = null;
            string? name = null;
            int? voltage = null;
            double? impedance = null;
            (int?, int?) renderPosition = (null, null);

            for (int i = 0; i < section.Length; i++)
            {
                try
                {
                    string[] input = CleanLine(section[i]);
                    switch (input[0])
                    {
                        case "ID":
                            id = ValidateInt("ID", input);
                            break;
                        case "Name":
                            name = ValidateString("Name", input);
                            break;
                        case "Voltage":
                            voltage = ValidateInt("Voltage", input);
                            break;
                        case "Impedance":
                            impedance = ValidateDouble("Impedance", input);
                            break;
                        case "RenderPosition":
                            renderPosition = ValidatePosition("RenderPosition", input);
                            break;
                        default:
                            // Skip empty lines
                            if (string.IsNullOrWhiteSpace(input[0])) continue;

                            // Unknown property
                            throw new FormatException($"Unknown category in supply data.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message} [Line {startRef + i}]");
                }
            }

            if (id == null)
            {
                throw new FormatException($"Missing ID in supply data. [Line: {startRef}]");
            }

            Supply supply = new((int)id, project);
            if (name != null) { supply.Name = name; }
            if (voltage != null) { supply.Voltage = (int)voltage; }
            if (impedance != null) { supply.Impedance = (double)impedance; }
            if (renderPosition.Item1 != null && renderPosition.Item2 != null) { supply.RenderPosition = ((int)renderPosition.Item1, (int)renderPosition.Item2); }
            return supply;
        }

        /// <summary>
        ///     Transforms a section of text into a AlternatorBlock object.
        /// </summary>
        /// <param name="startRef">The starting line reference for error reporting.</param>
        /// <param name="section">The lines of text in the section.</param>
        /// <returns>A AlternatorBlock object populated with the section data.</returns>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="Exception"></exception>
        protected static AlternatorBlock AlternatorBlockText(int startRef, string[] section, Project project)
        {
            int? id = null;
            string? name = null;
            (int?, int?) renderPosition = (null, null);

            for (int i = 0; i < section.Length; i++)
            {
                try
                {
                    string[] input = CleanLine(section[i]);
                    switch (input[0])
                    {
                        case "ID":
                            id = ValidateInt("ID", input);
                            break;
                        case "Name":
                            name = ValidateString("Name", input);
                            break;
                        case "RenderPosition":
                            renderPosition = ValidatePosition("RenderPosition", input);
                            break;
                        default:
                            // Skip empty lines
                            if (string.IsNullOrWhiteSpace(input[0])) continue;

                            // Unknown property
                            throw new FormatException($"Unknown category in alternatorBlock data.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message} [Line {startRef + i}]");
                }
            }

            if (id == null)
            {
                throw new FormatException($"Missing ID in alternatorBlock data. [Line: {startRef}]");
            }

            AlternatorBlock alternatorBlock = new((int)id, project);
            if (name != null) { alternatorBlock.Name = name; }
            if (renderPosition.Item1 != null && renderPosition.Item2 != null) { alternatorBlock.RenderPosition = ((int)renderPosition.Item1, (int)renderPosition.Item2); }
            return alternatorBlock;
        }

        /// <summary>
        ///     Transforms a section of text into a TransformerUPSBlock object.
        /// </summary>
        /// <param name="startRef">The starting line reference for error reporting.</param>
        /// <param name="section">The lines of text in the section.</param>
        /// <returns>A TransformerUPSBlock object populated with the section data.</returns>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="Exception"></exception>
        protected static TransformerUPSBlock TransformerUPSBlockText(int startRef, string[] section, Project project)
        {
            int? id = null;
            int? parentID = null;
            string? name = null;
            (int?, int?) renderPosition = (null, null);
            int[]? terminalIds = null;

            for (int i = 0; i < section.Length; i++)
            {
                try
                {
                    string[] input = CleanLine(section[i]);
                    switch (input[0])
                    {
                        case "ID":
                            id = ValidateInt("ID", input);
                            break;
                        case "ParentID":
                            parentID = ValidateInt("ParentID", input);
                            break;
                        case "Name":
                            name = ValidateString("Name", input);
                            break;
                        case "RenderPosition":
                            renderPosition = ValidatePosition("RenderPosition", input);
                            break;
                        case "Terminals":
                            terminalIds = ValidateTerminalIds("Terminals", input);
                            break;
                        default:
                            // Skip empty lines
                            if (string.IsNullOrWhiteSpace(input[0])) continue;

                            // Unknown property
                            throw new FormatException($"Unknown category in transformerUPSBlock data.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message} [Line {startRef + i}]");
                }
            }

            if (id == null)
            {
                throw new FormatException($"Missing ID in transformerUPSBlock data. [Line: {startRef}]");
            }
            if (parentID == null)
            {
                throw new FormatException($"Missing ParentID in transformerUPSBlock data. [Line: {startRef}]");
            }

            if (terminalIds == null || terminalIds.Length != 2)
            {
                throw new FormatException($"TransformerUPSBlock must have exactly 2 terminals. [Line: {startRef}]");
            }

            TransformerUPSBlock transformerUPSBlock = new((int)id, (int)parentID, project);
            if (name != null) { transformerUPSBlock.Name = name; }
            if (renderPosition.Item1 != null && renderPosition.Item2 != null) { transformerUPSBlock.RenderPosition = ((int)renderPosition.Item1, (int)renderPosition.Item2); }

            // Import the block before creating terminals
            project.ImportBlock(transformerUPSBlock);

            // Create the terminals with the specified IDs
            for (int i = 0; i < terminalIds.Length; i++)
            {
                Terminal terminal = new Terminal(terminalIds[i], (int)id, i, project);
                project.ImportBlock(terminal);
            }

            return transformerUPSBlock;
        }

        /// <summary>
        ///     Transforms a section of text into a Load object.
        /// </summary>
        /// <param name="startRef">The starting line reference for error reporting.</param>
        /// <param name="section">The lines of text in the section.</param>
        /// <returns>A Load object populated with the section data.</returns>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="Exception"></exception>
        protected static Load LoadText(int startRef, string[] section, Project project)
        {
            int? id = null;
            int? parentID = null;
            string? name = null;
            (int?, int?) renderPosition = (null, null);

            for (int i = 0; i < section.Length; i++)
            {
                try
                {
                    string[] input = CleanLine(section[i]);
                    switch (input[0])
                    {
                        case "ID":
                            id = ValidateInt("ID", input);
                            break;
                        case "ParentID":
                            parentID = ValidateInt("ParentID", input);
                            break;
                        case "Name":
                            name = ValidateString("Name", input);
                            break;
                        case "RenderPosition":
                            renderPosition = ValidatePosition("RenderPosition", input);
                            break;
                        default:
                            // Skip empty lines
                            if (string.IsNullOrWhiteSpace(input[0])) continue;

                            // Unknown property
                            throw new FormatException($"Unknown category in load data.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message} [Line {startRef + i}]");
                }
            }

            if (id == null)
            {
                throw new FormatException($"Missing ID in load data. [Line: {startRef}]");
            }
            if (parentID == null)
            {
                throw new FormatException($"Missing ParentID in load data. [Line: {startRef}]");
            }

            Load load = new((int)id, (int)parentID, project);
            if (name != null) { load.Name = name; }
            if (renderPosition.Item1 != null && renderPosition.Item2 != null) { load.RenderPosition = ((int)renderPosition.Item1, (int)renderPosition.Item2); }
            return load;
        }

        /// <summary>
        ///     Transforms a section of text into a ConductorBlock object.
        /// </summary>
        /// <param name="startRef">The starting line reference for error reporting.</param>
        /// <param name="section">The lines of text in the section.</param>
        /// <returns>A ConductorBlock object populated with the section data.</returns>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="Exception"></exception>
        protected static ConductorBlock ConductorBlockText(int startRef, string[] section, Project project)
        {
            int? id = null;
            string? name = null;
            double? length = null;
            (int?, int?) renderPosition = (null, null);
            int[]? terminalIds = null;

            for (int i = 0; i < section.Length; i++)
            {
                try
                {
                    string[] input = CleanLine(section[i]);
                    switch (input[0])
                    {
                        case "ID":
                            id = ValidateInt("ID", input);
                            break;
                        case "Name":
                            name = ValidateString("Name", input);
                            break;
                        case "Length":
                            length = ValidateDouble("Length", input);
                            break;
                        case "RenderPosition":
                            renderPosition = ValidatePosition("RenderPosition", input);
                            break;
                        case "Terminals":
                            terminalIds = ValidateTerminalIds("Terminals", input);
                            break;
                        default:
                            // Skip empty lines
                            if (string.IsNullOrWhiteSpace(input[0])) continue;

                            // Unknown property
                            throw new FormatException($"Unknown category in conductorBlock data.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message} [Line {startRef + i}]");
                }
            }

            if (id == null)
            {
                throw new FormatException($"Missing ID in conductorBlock data. [Line: {startRef}]");
            }

            if (terminalIds == null || terminalIds.Length != 2)
            {
                throw new FormatException($"ConductorBlock must have exactly 2 terminals. [Line: {startRef}]");
            }

            ConductorBlock conductorBlock = new((int)id, project);
            if (name != null) { conductorBlock.Name = name; }
            if (length != null) { conductorBlock.Length = (double)length; }
            if (renderPosition.Item1 != null && renderPosition.Item2 != null) { conductorBlock.RenderPosition = ((int)renderPosition.Item1, (int)renderPosition.Item2); }

            // Import the block before creating terminals
            project.ImportBlock(conductorBlock);

            // Create the terminals with the specified IDs
            for (int i = 0; i < terminalIds.Length; i++)
            {
                Terminal terminal = new Terminal(terminalIds[i], (int)id, i, project);
                project.ImportBlock(terminal);
            }

            return conductorBlock;
        }

        /// <summary>
        ///     Transforms a section of text into a ExternalBusbar object.
        /// </summary>
        /// <param name="startRef">The starting line reference for error reporting.</param>
        /// <param name="section">The lines of text in the section.</param>
        /// <returns>A ExternalBusbar object populated with the section data.</returns>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="Exception"></exception>
        protected static ExternalBusbar ExternalBusbarText(int startRef, string[] section, Project project)
        {
            int? id = null;
            int? parentID = null;
            (int?, int?) renderPosition = (null, null);
            int[]? terminalIds = null;

            for (int i = 0; i < section.Length; i++)
            {
                try
                {
                    string[] input = CleanLine(section[i]);
                    switch (input[0])
                    {
                        case "ID":
                            id = ValidateInt("ID", input);
                            break;
                        case "ParentID":
                            parentID = ValidateInt("ParentID", input);
                            break;
                        case "RenderPosition":
                            renderPosition = ValidatePosition("RenderPosition", input);
                            break;
                        case "Terminals":
                            terminalIds = ValidateTerminalIds("Terminals", input);
                            break;
                        default:
                            // Skip empty lines
                            if (string.IsNullOrWhiteSpace(input[0])) continue;

                            // Unknown property
                            throw new FormatException($"Unknown category in externalBusbar data.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message} [Line {startRef + i}]");
                }
            }

            if (id == null)
            {
                throw new FormatException($"Missing ID in externalBusbar data. [Line: {startRef}]");
            }
            if (parentID == null)
            {
                throw new FormatException($"Missing ParentID in externalBusbar data. [Line: {startRef}]");
            }

            if (terminalIds == null || terminalIds.Length != 8)
            {
                throw new FormatException($"ExternalBusbar must have exactly 8 terminals. [Line: {startRef}]");
            }

            ExternalBusbar externalBusbar = new((int)id, (int)parentID, project);
            if (renderPosition.Item1 != null && renderPosition.Item2 != null) { externalBusbar.RenderPosition = ((int)renderPosition.Item1, (int)renderPosition.Item2); }

            // Import the block before creating terminals
            project.ImportBlock(externalBusbar);

            // Create the terminals with the specified IDs
            for (int i = 0; i < terminalIds.Length; i++)
            {
                Terminal terminal = new Terminal(terminalIds[i], (int)id, i, project);
                project.ImportBlock(terminal);
            }

            return externalBusbar;
        }

        /// <summary>
        ///     Transforms a section of text into an array of connections described as integer arrays.
        /// </summary>
        /// <param name="startRef">The starting line reference for error reporting.</param>
        /// <param name="section">The lines of text in the section.</param>
        /// <returns>An array of int arrays describing connections.</returns>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="Exception"></exception>
        protected static int[][] ConnectionsText(int startRef, string[] section, Project project)
        {
            List<int[]> connections = [];

            for (int i = 0; i < section.Length; i++)
            {
                try
                {
                    List<int> connection = [];
                    string[] input = CleanLine(section[i]);

                    if (input.Length > 2)
                    {
                        if (!(input[2].StartsWith("\"") && input[^1].EndsWith("\"")))
                        {
                            throw new FormatException($"Invalid format for connections data.");
                        }
                        input[2] = input[2][1..];
                        input[^1] = input[^1][..^1];
                    }

                    foreach (string var in input)
                    {
                        if (!int.TryParse(var, out int varInt))
                        {
                            throw new FormatException($"Invalid format for ID '{var}' in connections data.");
                        }

                        connection.Add(varInt);
                    }
                    connections.Add(connection.ToArray());
                }
                catch (Exception ex)
                {
                    throw new Exception($"{ex.Message} [Line {startRef + i}]");
                }
            }

            return connections.ToArray();
        }

        /// <summary>
        ///     Cleans a line of text input by removing comments and splitting into key-value pairs.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        protected static string[] CleanLine(string line)
        {
            // Remove comments if present
            var lineClean = line.Contains(';')
                ? line.Substring(0, line.IndexOf(';'))
                : line;

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(lineClean))
            {
                return [string.Empty];
            }

            // Split the line into key and value
            var input = lineClean.Trim().Split(' ');

            // Validate input length
            if (input.Length < 2)
            {
                throw new FormatException($"Invalid format.");
            }

            return input;
        }

        /// <summary>
        ///     Validates and parses an int value from the input array.
        /// </summary>
        /// <param name="variable">Variable name for error message.</param>
        /// <param name="input">Input string array.</param>
        /// <returns>Validated int value.</returns>
        /// <exception cref="FormatException"></exception>
        protected static int ValidateInt(string variable, string[] input)
        {
            // Validate input length
            if (input.Length > 2)
            {
                throw new FormatException($"Invalid format for {variable}.");
            }

            // Parse and assign the value
            if (!int.TryParse(input[1], out int tempInt))
            {
                throw new FormatException($"{variable} must be an integer value.");
            }

            return tempInt;
        }

        /// <summary>
        ///     Validates and parses a double value from the input array.
        /// </summary>
        /// <param name="variable">Variable name for error message.</param>
        /// <param name="input">Input string array.</param>
        /// <returns>Validated double value.</returns>
        /// <exception cref="FormatException"></exception>
        protected static double ValidateDouble(string variable, string[] input)
        {
            // Validate input length
            if (input.Length > 2)
            {
                throw new FormatException($"Invalid format for {variable}.");
            }

            // Parse and assign the value
            if (!double.TryParse(input[1], out double tempDouble))
            {
                throw new FormatException($"{variable} must be a numeric value.");
            }

            return tempDouble;
        }

        /// <summary>
        ///     Validates and parses a string value from the input array.
        /// </summary>
        /// <param name="variable">Variable name for error message.</param>
        /// <param name="input">Input string array.</param>
        /// <returns>Validated string value.</returns>
        /// <exception cref="FormatException"></exception>
        protected static string ValidateString(string variable, string[] input)
        {
            string tempString = string.Empty;

            tempString = string.Join(' ', input, 1, input.Length - 1);

            // Check input starts and ends with a double quote
            if (!(tempString.StartsWith("\"") && tempString.EndsWith("\"")))
            {
                throw new FormatException($"{variable} must start and end with a double quote (\").");
            }

            tempString = tempString[1..^1];

            if (string.IsNullOrWhiteSpace(tempString))
            {
                throw new FormatException($"{variable} cannot be empty or whitespace.");
            }

            return tempString;
        }

        /// <summary>
        ///     Validates and parses a boolean value from the input array.
        /// </summary>
        /// <param name="variable">Variable name for error message.</param>
        /// <param name="input">Input string array.</param>
        /// <returns>Validated boolean value.</returns>
        /// <exception cref="FormatException"></exception>
        protected static bool ValidateBool(string variable, string[] input)
        {
            // Validates input length
            if (input.Length > 2)
            {
                throw new FormatException($"Invalid format for {variable}.");
            }

            // Parse and assign the value
            if (!bool.TryParse(input[1], out bool tempBool))
            {
                throw new FormatException($"{variable} must be a boolean value (true/false).");
            }

            return tempBool;
        }

        /// <summary>
        ///     Validates and parses a long value from the input array.
        /// </summary>
        /// <param name="variable">Variable name for error message.</param>
        /// <param name="input">Input string array.</param>
        /// <returns>Validated long value.</returns>
        /// <exception cref="FormatException"></exception>
        protected static long ValidateLong(string variable, string[] input)
        {
            // Validate input length
            if (input.Length > 2)
            {
                throw new FormatException($"Invalid format for {variable}.");
            }
            // Parse and assign the value
            if (!long.TryParse(input[1], out long tempLong))
            {
                throw new FormatException($"{variable} must be a long integer value.");
            }
            return tempLong;
        }

        protected static (int?, int?) ValidatePosition(string variable, string[] input)
        {
            // Validate input length
            if (input.Length != 3)
            {
                throw new FormatException($"Invalid format for {variable}. Must be in format 'RenderPosition X Y'.");
            }
            // Parse and assign the X value
            if (!int.TryParse(input[1], out int x))
            {
                throw new FormatException($"X coordinate in {variable} must be an integer value.");
            }
            // Parse and assign the Y value
            if (!int.TryParse(input[2], out int y))
            {
                throw new FormatException($"Y coordinate in {variable} must be an integer value.");
            }
            return (x, y);
        }

        /// <summary>
        ///     Validates and parses an array of terminal IDs from the input array.
        /// </summary>
        /// <param name="variable">Variable name for error message.</param>
        /// <param name="input">Input string array.</param>
        /// <returns>Array of terminal IDs.</returns>
        /// <exception cref="FormatException"></exception>
        protected static int[] ValidateTerminalIds(string variable, string[] input)
        {
            if (input.Length < 2)
            {
                throw new FormatException($"Invalid format for {variable}. Must contain at least one terminal ID.");
            }

            var terminalIds = new List<int>();
            for (int i = 1; i < input.Length; i++)
            {
                if (!int.TryParse(input[i], out int terminalId))
                {
                    throw new FormatException($"Terminal ID '{input[i]}' in {variable} must be an integer value.");
                }
                terminalIds.Add(terminalId);
            }

            return terminalIds.ToArray();
        }

        /// <summary>
        ///     Validates and parses an array of block IDs from the AssignedTo input array.
        /// </summary>
        /// <param name="variable">Variable name for error message.</param>
        /// <param name="input">Input string array.</param>
        /// <returns>Array of block IDs.</returns>
        /// <exception cref="FormatException"></exception>
        protected static int[] ValidateAssignedToIds(string variable, string[] input)
        {
            if (input.Length < 2)
            {
                throw new FormatException($"Invalid format for {variable}. Must contain at least one block ID.");
            }

            var blockIds = new List<int>();
            for (int i = 1; i < input.Length; i++)
            {
                if (!int.TryParse(input[i], out int blockId))
                {
                    throw new FormatException($"Block ID '{input[i]}' in {variable} must be an integer value.");
                }
                blockIds.Add(blockId);
            }

            return blockIds.ToArray();
        }

        /// <summary>
        ///     Parses AssignedTo IDs from equipment section lines.
        /// </summary>
        /// <param name="lines">Section lines to parse.</param>
        /// <returns>Array of block IDs or null if not found.</returns>
        private static int[]? ParseAssignedToFromLines(string[] lines)
        {
            foreach (string line in lines)
            {
                string[] input = CleanLine(line);
                if (input.Length > 0 && input[0] == "AssignedTo")
                {
                    return ValidateAssignedToIds("AssignedTo", input);
                }
            }
            return null;
        }
    }
}
