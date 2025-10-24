using System.Collections.Generic;
using System.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SignallingPowerApp.Core
{
    /// <summary>
    ///     This class manages all equipment item categories.
    /// </summary>
    public class AllItems : ImportText
    {
        private readonly List<IItem> allItems = new();

        /// <summary>
        ///     Gets the collection of conductor items.
        /// </summary>
        public Conductor[] Conductors { get { return allItems.OfType<Conductor>().ToArray(); } }

        /// <summary>
        ///     Gets the collection of transformer items.
        /// </summary>
        public TransformerUPS[] TransformerUPSs { get { return allItems.OfType<TransformerUPS>().ToArray(); } }

        /// <summary>
        ///     Gets the collection of alternator items.
        /// </summary>
        public Alternator[] Alternators { get { return allItems.OfType<Alternator>().ToArray(); } }

        /// <summary>
        ///     Gets the collection of consumer items.
        /// </summary>
        public Consumer[] Consumers { get { return allItems.OfType<Consumer>().ToArray(); } }

        /// <summary>
        ///     Adds an equipment item to the appropriate category based on its type.
        /// </summary>
        /// <param name="item">The equipment item to be added.</param>
        /// <returns>The added equipment item.</returns>
        /// <exception cref="ArgumentException"></exception>
        public void AddItem(IItem item)
        {
            allItems.Add(item);
        }

        /// <summary>
        ///     Clears all equipment items.
        /// </summary>
        public void ClearAllItems()
        {
            allItems.Clear();
        }

        /// <summary>
        ///     Checks if any category contains a specific equipment item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Returns true if the item exists in any category, otherwise false.</returns>
        public IItem? ContainsNonCustomItem(IItem item)
        {
            foreach (var existingItem in allItems)
            {
                if (existingItem.IsEqual(item) && !existingItem.IsCustom())
                {
                    return existingItem;
                }
            }
            return null;
        }

        /// <summary>
        ///     Exports all equipment items in the category to an array of strings.
        /// </summary>
        /// <returns>
        ///     An array of strings containing all equipment items in the category, formatted for export.
        /// </returns>
        private string[] ExportCategoryText(IItem[] category)
        {
            var allLines = new List<string>();
            for (int i = 0; i < category.Length; i++)
            {
                allLines.AddRange(category[i].ExportItemText());

                // Add a blank line after each item, but not after the very last one
                if (i < category.Length - 1)
                {
                    allLines.Add(string.Empty);
                }
            }
            return allLines.ToArray();
        }

        /// <summary>
        ///     Exports all equipment items to an array of strings.
        /// </summary>
        /// <returns>
        ///     Returns an array of strings containing all equipment items, formatted for export.
        /// </returns>
        public string[] ExportAllItemsText()
        {
            var allLines = new List<string>();
            allLines.Add(";===== ITEM DATA =====");
            allLines.Add(string.Empty);
            allLines.Add(";----- CONDUCTORS -----");
            allLines.Add(string.Empty);
            allLines.AddRange(ExportCategoryText(Conductors));
            allLines.Add(string.Empty);
            allLines.Add(";----- TRANSFORMERUPS -----");
            allLines.Add(string.Empty);
            allLines.AddRange(ExportCategoryText(TransformerUPSs));
            allLines.Add(string.Empty);
            allLines.Add(";----- ALTERNATORS -----");
            allLines.Add(string.Empty);
            allLines.AddRange(ExportCategoryText(Alternators));
            allLines.Add(string.Empty);
            allLines.Add(";----- CONSUMERS -----");
            allLines.Add(string.Empty);
            allLines.AddRange(ExportCategoryText(Consumers));
            return allLines.ToArray();
        }
    }

    /// <summary>
    ///     An interface representing a generic equipment item.
    /// </summary>
    public interface IItem
    {
        /// <summary>
        ///     Exports the item data to an array of strings.
        /// </summary>
        /// <returns>
        ///     An array of strings representing the item, formatted for export.
        /// </returns>
        string[] ExportItemText();

        /// <summary>
        ///     Checks if the item is a custom item.
        /// </summary>
        /// <remarks>This method checks if the item is costom/ user created.</remarks>
        /// <returns>True if the item is custom, otherwise false.</returns>
        bool IsCustom();

        /// <summary>
        ///     Sets the custom flag to true for the item.
        /// </summary>
        /// <param name="value">The value to set the custom flag to.</param>
        /// <returns>The updated value of the custom flag.</returns>
        bool SetCustom();

        /// <summary>
        ///     Checks if the current item is equal to another item.
        /// </summary>
        /// <remarks>Compares the current item to another item except the custom flag.</remarks>
        /// <returns>True if the items are equal, otherwise false.</returns>
        bool IsEqual(IItem item);
    }

    /// <summary>
    ///     Class representing a conductor item with various electrical properties.
    /// </summary>
    public class Conductor(bool custom = false) : ImportText, IItem
    {
        private bool custom = custom;                       // flag to indicate if the conductor is custom

        private int cores = 0;                              // number of cores in the conductor
        private int strandCount = 0;                        // number of strands in each core-
        private double strandDiameter = 0.0;                // in mm
        private double crossSectionalArea = 0.0;            // in mm²
        private string description = string.Empty;          // description of the conductor 

        // Three phase volrage drop (Vc) at 50Hz, mV/A.m
        // Sourced from AS/NZS 3008.1.1:2017 Table 46
        private double voltageDrop60 = 0.0;                 // for 60°C conductor
        private double voltageDrop90 = 0.0;                 // for 90°C conductor

        // Reactance (Xc) at 50Hz, ohm/km
        // Sourced from AS/NZS 3008.1.1:2017 Table 32
        private double reactance = 0.0;

        // Resistance (Rc) at 50Hz, ohm/km
        // Sourced from AS/NZS 3008.1.1:2017 Table 36
        private double resistance60 = 0.0;                  // for 60°C conductor
        private double resistance90 = 0.0;                  // for 90°C conductor

        public bool IsCustom()
        {
            return custom;
        }

        public bool SetCustom()
        {
            custom = true;
            return custom;
        }

        public bool IsEqual(IItem item)
        {
            if (item is not Conductor otherConductor) return false;
            return cores == otherConductor.Cores &&
                   strandCount == otherConductor.StrandCount &&
                   strandDiameter == otherConductor.StrandDiameter &&
                   crossSectionalArea == otherConductor.CrossSectionalArea &&
                   description == otherConductor.Description &&
                   voltageDrop60 == otherConductor.VoltageDrop60 &&
                   voltageDrop90 == otherConductor.VoltageDrop90 &&
                   reactance == otherConductor.Reactance &&
                   resistance60 == otherConductor.Resistance60 &&
                   resistance90 == otherConductor.Resistance90;
        }

        /// <summary>
        ///     Number of cores in the conductor.
        /// </summary>
        public int Cores
        {
            get { return cores; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Cores cannot be less than 0.");
                }

                cores = value;
            }
        }

        /// <summary>
        ///     Number of strands in each core of the conductor.
        /// </summary>
        public int StrandCount 
        { 
            get { return strandCount; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("StrandCount cannot be less than 0.");
                }

                strandCount = value;
            }
        }

        /// <summary>
        ///     Diameter of each strand in the conductor (in mm)
        /// </summary>
        public double StrandDiameter 
        { 
            get { return strandDiameter; }
            set
            {
                if (value < 0.0)
                {
                    throw new ArgumentOutOfRangeException("StrandDiameter cannot be less than 0.0.");
                }

                strandDiameter = value;
            }
        }

        /// <summary>
        ///     Cross-sectional area of the conductor (in mm²)
        /// </summary>
        public double CrossSectionalArea 
        { 
            get { return crossSectionalArea; }
            set
            {
                if (value < 0.0)
                {
                    throw new ArgumentOutOfRangeException("CrossSectionalArea cannot be less than 0.0.");
                }

                crossSectionalArea = value;
            }
        }

        /// <summary>
        ///  Description of the conductor.
        /// </summary>
        public string Description 
        { 
            get { return description; }
            set
            {
                if (value.Length > 150)
                {
                    throw new ArgumentOutOfRangeException("Description cannot be more than 150 characters.");
                }

                description = value;
            }
        }

        /// <summary>
        ///     Voltage drop (Vc) at 50Hz, mV/A.m for 60°C conductor.
        /// </summary>
        public double VoltageDrop60
        {
            get { return voltageDrop60; }
            set
            {
                if (value < 0.0)
                {
                    throw new ArgumentOutOfRangeException("VoltageDrop60 cannot be less than 0.0.");
                }

                voltageDrop60 = value;
            }
        }

        /// <summary>
        ///  Voltage drop (Vc) at 50Hz, mV/A.m for 90°C conductor.
        /// </summary>
        public double VoltageDrop90
        {
            get { return voltageDrop90; }
            set
            {
                if (value < 0.0)
                {
                    throw new ArgumentOutOfRangeException("VoltageDrop90 cannot be less than 0.0.");
                }

                voltageDrop90 = value;
            }
        }

        /// <summary>
        ///  Reactance (Xc) at 50Hz, ohm/km.
        /// </summary>
        public double Reactance
        {   get { return reactance; }
            set
            {
                if (value < 0.0)
                {
                    throw new ArgumentOutOfRangeException("Reactance cannot be less than 0.0.");
                }

                reactance = value;
            }
        }

        /// <summary>
        ///  Resistance (Rc) at 50Hz, ohm/km for 60°C conductor.
        /// </summary>
        public double Resistance60
        {
            get { return resistance60; }
            set
            {
                if (value < 0.0)
                {
                    throw new ArgumentOutOfRangeException("Resistance60 cannot be less than 0.0.");
                }

                resistance60 = value;
            }
        }

        /// <summary>
        ///     Resistance (Rc) at 50Hz, ohm/km for 90°C conductor.
        /// </summary>
        public double Resistance90
        {
            get { return resistance90; }
            set
            {
                if (value < 0.0)
                {
                    throw new ArgumentOutOfRangeException("Resistance90 cannot be less than 0.0.");
                }

                resistance90 = value;
            }
        }

        public string[] ExportItemText()
        {
            var lines = new List<string>
            {
                "Begin Conductor",
                $"    Cores {cores}",
                $"    StrandCount {strandCount}",
                $"    StrandDiameter {strandDiameter}",
                $"    CrossSectionalArea {crossSectionalArea}",
                $"    Description \"{description}\"",
                $"    VoltageDrop60 {voltageDrop60}",
                $"    VoltageDrop90 {voltageDrop90}",
                $"    Reactance {reactance}",
                $"    Resistance60 {resistance60}",
                $"    Resistance90 {resistance90}",
                "End Conductor"
            };
            return lines.ToArray();
        }
    }

    /// <summary>
    ///     Class representing a transformer item with various electrical properties.
    /// </summary>
    public class TransformerUPS(bool custom = false) : IItem
    {
        private bool custom = custom;                       // flag to indicate if the conductor is custom

        private int rating = 0;                             // in kVA
        private double percentageZ = 0.0;                   // percentage impedance (%Z)
        private int primaryVoltage = 0;                     // primary voltage in V
        private int secondaryVoltage = 0;                   // secondary voltage in V
        private string description = "None";                // description of the transformer

        public bool IsCustom()
        {
            return custom;
        }

        public bool SetCustom()
        {
            custom = true;
            return custom;
        }

        public bool IsEqual(IItem item)
        {
            if (item is not TransformerUPS otherTransformer) return false;
            return rating == otherTransformer.Rating &&
                   percentageZ == otherTransformer.PercentageZ &&
                   primaryVoltage == otherTransformer.PrimaryVoltage &&
                   secondaryVoltage == otherTransformer.SecondaryVoltage &&
                   description == otherTransformer.Description;
        }

        /// <summary>
        ///     Transformer rating in kVA.
        /// </summary>
        public int Rating
        {
            get { return rating; }
            set 
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Rating cannot be less than 0.");
                }
                rating = value; 
            }
        }

        /// <summary>
        ///     Percentage impedance of transformer (%Z).
        /// </summary>
        public double PercentageZ
        {
            get { return percentageZ; }
            set
            {
                if (value < 0.0)
                {
                    throw new ArgumentOutOfRangeException("PercentageZ cannot be less than 0.0.");
                }
                percentageZ = value;
            }
        }

        /// <summary>
        ///     Primary voltage in V.
        /// </summary>
        public int PrimaryVoltage
        {
            get { return primaryVoltage; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("PrimaryVoltage cannot be less than 0.");
                }
                primaryVoltage = value;
            }
        }

        /// <summary>
        ///     Secondary voltage in V.
        /// </summary>
        public int SecondaryVoltage
        {
            get { return secondaryVoltage; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("SecondaryVoltage cannot be less than 0.");
                }
                secondaryVoltage = value;
            }
        }

        /// <summary>
        ///  Description of the transformer.
        /// </summary>
        public string Description
        {
            get { return description; }
            set
            {                 
                if (value.Length > 150)
                {
                    throw new ArgumentOutOfRangeException("Description cannot be more than 150 characters.");
                }
                description = value;
            }
        }

        public string[] ExportItemText()
        {
            var lines = new List<string>
            {
                "Begin TransformerUPS",
                $"    Rating {Rating}",
                $"    PercentageZ {PercentageZ}",
                $"    PrimaryVoltage {PrimaryVoltage}",
                $"    SecondaryVoltage {SecondaryVoltage}",
                $"    Description \"{Description}\"",
                "End TransformerUPS"
            };
            return lines.ToArray();
        }
    }

    /// <summary>
    ///     Class representing a transformer item with various electrical properties.
    /// </summary>
    public class Alternator(bool custom = false) : IItem
    {
        private bool custom = custom;                       // flag to indicate if the conductor is custom

        private int ratingVA = 0;                           // in VA
        private int ratingW = 0;                            // in W
        private string description = "None";                // description of the alternator

        public bool IsCustom()
        {
            return custom;
        }

        public bool SetCustom()
        {
            custom = true;
            return custom;
        }

        public bool IsEqual(IItem item)
        {
            if (item is not Alternator otherAlternator) return false;
            return ratingVA == otherAlternator.RatingVA &&
                   ratingW == otherAlternator.RatingW &&
                   description == otherAlternator.Description;
        }

        /// <summary>
        ///     Alternator rating in VA.
        /// </summary>
        public int RatingVA
        {
            get { return ratingVA; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("RatingVA cannot be less than 0.");
                }
                ratingVA = value;
            }
        }

        /// <summary>
        ///     Prime mover rating in W.
        /// </summary>
        public int RatingW
        {
            get { return ratingW; }
            set
            {   
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("RatingW cannot be less than 0.");
                }
                ratingW = value;
            }
        }

        /// <summary>
        ///  Description of the alternator.
        /// </summary>
        public string Description
        {
            get { return description; }
            set 
            {                 
                if (value.Length > 150)
                {
                    throw new ArgumentOutOfRangeException("Description cannot be more than 150 characters.");
                }
                description = value;
            }
        }

        public string[] ExportItemText()
        {
            var lines = new List<string>
            {
                "Begin Alternator",
                $"    RatingVA {RatingVA}",
                $"    RatingW {RatingW}",
                $"    Description \"{Description}\"",
                "End Alternator"
            };
            return lines.ToArray();
        }
    }

    /// <summary>
    ///     Class representing a consumer item with various electrical properties.
    /// </summary>
    public class Consumer(bool custom = false) : IItem
    {
        private bool custom = custom;                       // flag to indicate if the conductor is custom

        private string name = "Load";                       // consumer name
        private string description = "None";                // consumer description
        private int load = 0;                               // in VA

        public bool IsCustom()
        {
            return custom;
        }

        public bool SetCustom()
        {
            custom = true;
            return custom;
        }

        public bool IsEqual(IItem item)
        {
            if (item is not Consumer otherConsumer) return false;
            return name == otherConsumer.Name &&
                   description == otherConsumer.Description &&
                   load == otherConsumer.Load;
        }

        /// <summary>
        ///     Consumer name.
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                if (value.Length > 150)
                {
                    throw new ArgumentOutOfRangeException("Description cannot be more than 150 characters.");
                }
                name = value;
            }
        }

        /// <summary>
        ///     Consumer description.
        /// </summary>
        public string Description
        {
            get { return description; }
            set
            {
                if (value.Length > 150)
                {
                    throw new ArgumentOutOfRangeException("Description cannot be more than 150 characters.");
                }
                description = value;
            }
        }

        /// <summary>
        ///  Consumer load in VA.
        /// </summary>
        public int Load
        {
            get { return load; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("Load cannot be less than 0.");
                }
                load = value;
            }
        }

        public string[] ExportItemText()
        {
            var lines = new List<string>
            {
                "Begin Consumer",
                $"    Name \"{Name}\"",
                $"    Description \"{Description}\"",
                $"    Load {Load}",
                "End Consumer"
            };
            return lines.ToArray();
        }
    }
}