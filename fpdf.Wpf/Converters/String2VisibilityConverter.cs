using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace fpdf.Wpf.Converters;

public class String2VisibilityConverter : IValueConverter
{
  public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    if (value is string str)
    {
      return string.IsNullOrEmpty(str) ? Visibility.Collapsed : Visibility.Visible;
    }
    return Visibility.Collapsed;
  }

  public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}
