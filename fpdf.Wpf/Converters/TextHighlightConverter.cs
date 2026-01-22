using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace fpdf.Wpf.Converters;

public class TextHighlightConverter : IMultiValueConverter
{
  public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
  {
    if (values.Length != 2 || values[0] is not string text || values[1] is not string searchText)
    {
      return new List<Inline> { new Run(values[0]?.ToString() ?? string.Empty) };
    }

    if (string.IsNullOrWhiteSpace(searchText))
    {
      return new List<Inline> { new Run(text) };
    }

    var inlines = new List<Inline>();
    var index = text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);

    if (index == -1)
    {
      inlines.Add(new Run(text));
      return inlines;
    }

    var currentIndex = 0;

    while (index >= 0)
    {
      // Adiciona texto antes do match
      if (index > currentIndex)
      {
        inlines.Add(new Run(text.Substring(currentIndex, index - currentIndex)));
      }

      // Adiciona o texto matched com highlight
      var matchedText = text.Substring(index, searchText.Length);
      var highlightRun = new Run(matchedText)
      {
        Background = new SolidColorBrush(Color.FromRgb(255, 241, 0)), // Amarelo semi-transparente
        Foreground = Brushes.Black,
      };
      inlines.Add(highlightRun);

      currentIndex = index + searchText.Length;
      index = text.IndexOf(searchText, currentIndex, StringComparison.OrdinalIgnoreCase);
    }

    // Adiciona o resto do texto
    if (currentIndex < text.Length)
    {
      inlines.Add(new Run(text.Substring(currentIndex)));
    }

    return inlines;
  }

  public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
  {
    throw new NotImplementedException();
  }
}

