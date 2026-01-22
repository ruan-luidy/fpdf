using System.Globalization;
using System.Windows.Data;

namespace fpdf.Wpf.Converters;

public class IntGreaterThan1BoolConverter : IValueConverter
{
  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is int intValue)
    {
      return intValue > 1;
    }
    return false;
  }

  public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}
