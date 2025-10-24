using SignallingPowerApp.Core;

namespace SignallingPowerApp.ViewModels
{
    /// <summary>
    /// View model wrapper for equipment items to expose properties for data binding
    /// </summary>
    /// <typeparam name="T">The type of equipment item (must implement IItem)</typeparam>
    public class EquipmentItemViewModel<T> where T : IItem
    {
        private readonly T _item;

        public EquipmentItemViewModel(T item)
        {
            _item = item;
        }

        /// <summary>
        /// Gets the underlying equipment item
        /// </summary>
        public T Item => _item;

        /// <summary>
        /// Gets whether this is a custom item (exposed as a property for binding)
        /// </summary>
        public bool IsCustom => _item.IsCustom();

        /// <summary>
        /// Gets a string representation of IsCustom for display
        /// </summary>
        public string IsCustomText => _item.IsCustom() ? "True" : "False";
    }

    /// <summary>
    /// View model for Conductor items
    /// </summary>
    public class ConductorViewModel
    {
        private readonly Conductor _conductor;

        public ConductorViewModel(Conductor conductor)
        {
            _conductor = conductor;
        }

        public Conductor Conductor => _conductor;
        public bool IsCustom => _conductor.IsCustom();
        public string IsCustomText => _conductor.IsCustom() ? "True" : "False";
        public int Cores
        {
            get => _conductor.Cores;
            set => _conductor.Cores = value;
        }
        public int StrandCount
        {
            get => _conductor.StrandCount;
            set => _conductor.StrandCount = value;
        }
        public double StrandDiameter
        {
            get => _conductor.StrandDiameter;
            set => _conductor.StrandDiameter = value;
        }
        public double CrossSectionalArea
        {
            get => _conductor.CrossSectionalArea;
            set => _conductor.CrossSectionalArea = value;
        }
        public string Description
        {
            get => _conductor.Description;
            set => _conductor.Description = value;
        }
        public double VoltageDrop60
        {
            get => _conductor.VoltageDrop60;
            set => _conductor.VoltageDrop60 = value;
        }
        public double VoltageDrop90
        {
            get => _conductor.VoltageDrop90;
            set => _conductor.VoltageDrop90 = value;
        }
        public double Reactance
        {
            get => _conductor.Reactance;
            set => _conductor.Reactance = value;
        }
        public double Resistance60
        {
            get => _conductor.Resistance60;
            set => _conductor.Resistance60 = value;
        }
        public double Resistance90
        {
            get => _conductor.Resistance90;
            set => _conductor.Resistance90 = value;
        }
    }

    /// <summary>
    /// View model for TransformerUPS items
    /// </summary>
    public class TransformerUPSViewModel
    {
        private readonly TransformerUPS _transformer;

        public TransformerUPSViewModel(TransformerUPS transformer)
        {
            _transformer = transformer;
        }

        public TransformerUPS Transformer => _transformer;
        public bool IsCustom => _transformer.IsCustom();
        public string IsCustomText => _transformer.IsCustom() ? "True" : "False";
        public int Rating
        {
            get => _transformer.Rating;
            set => _transformer.Rating = value;
        }
        public double PercentageZ
        {
            get => _transformer.PercentageZ;
            set => _transformer.PercentageZ = value;
        }
        public int PrimaryVoltage
        {
            get => _transformer.PrimaryVoltage;
            set => _transformer.PrimaryVoltage = value;
        }
        public int SecondaryVoltage
        {
            get => _transformer.SecondaryVoltage;
            set => _transformer.SecondaryVoltage = value;
        }
        public string Description
        {
            get => _transformer.Description;
            set => _transformer.Description = value;
        }
    }

    /// <summary>
    /// View model for Alternator items
    /// </summary>
    public class AlternatorViewModel
    {
        private readonly Alternator _alternator;

        public AlternatorViewModel(Alternator alternator)
        {
            _alternator = alternator;
        }

        public Alternator Alternator => _alternator;
        public bool IsCustom => _alternator.IsCustom();
        public string IsCustomText => _alternator.IsCustom() ? "True" : "False";
        public int RatingVA
        {
            get => _alternator.RatingVA;
            set => _alternator.RatingVA = value;
        }
        public int RatingW
        {
            get => _alternator.RatingW;
            set => _alternator.RatingW = value;
        }
        public string Description
        {
            get => _alternator.Description;
            set => _alternator.Description = value;
        }
    }

    /// <summary>
    /// View model for Consumer items
    /// </summary>
    public class ConsumerViewModel
    {
        private readonly Consumer _consumer;

        public ConsumerViewModel(Consumer consumer)
        {
            _consumer = consumer;
        }

        public Consumer Consumer => _consumer;
        public bool IsCustom => _consumer.IsCustom();
        public string IsCustomText => _consumer.IsCustom() ? "True" : "False";
        public string Name
        {
            get => _consumer.Name;
            set => _consumer.Name = value;
        }
        public string Description
        {
            get => _consumer.Description;
            set => _consumer.Description = value;
        }
        public int Load
        {
            get => _consumer.Load;
            set => _consumer.Load = value;
        }
    }
}
