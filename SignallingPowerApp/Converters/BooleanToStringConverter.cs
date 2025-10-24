using System;
using System.Globalization;
using System.Windows.Data;

namespace SignallingPowerApp.Converters
{
    /// <summary>
    /// Converts a boolean value to a string representation ("True" or "False")
    /// </summary>
    public class BooleanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue.ToString();
            }
            return "False";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return bool.TryParse(stringValue, out bool result) && result;
            }
            return false;
        }
    }
}
