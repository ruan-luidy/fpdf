using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace fpdf.Wpf.Converters;

public class Double2GridLengthConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value is double doubleValue)
    {
      return new GridLength(doubleValue);
    }
    return new GridLength(1, GridUnitType.Star);
  }

  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
    if (value is GridLength gridLength)
    {
      return gridLength.Value;
    }
    return 0.0;
  }
}
