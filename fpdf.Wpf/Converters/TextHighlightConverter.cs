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

    // Divide a busca em palavras (separadas por espaço)
    var searchWords = searchText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    
    if (searchWords.Length == 0)
    {
      return new List<Inline> { new Run(text) };
    }

    // Encontra todas as posições de matches para todas as palavras
    var matches = new List<(int Start, int Length)>();
    
    foreach (var word in searchWords)
    {
      var index = 0;
      while ((index = text.IndexOf(word, index, StringComparison.OrdinalIgnoreCase)) >= 0)
      {
        matches.Add((index, word.Length));
        index += word.Length;
      }
    }

    // Se não encontrou nenhum match, retorna o texto normal
    if (matches.Count == 0)
    {
      return new List<Inline> { new Run(text) };
    }

    // Ordena os matches por posição
    matches = matches.OrderBy(m => m.Start).ToList();

    // Remove matches sobrepostos
    var uniqueMatches = new List<(int Start, int Length)>();
    foreach (var match in matches)
    {
      if (uniqueMatches.Count == 0 || match.Start >= uniqueMatches.Last().Start + uniqueMatches.Last().Length)
      {
        uniqueMatches.Add(match);
      }
    }

    // Constrói os Inlines com highlights
    var inlines = new List<Inline>();
    var currentIndex = 0;

    var highlightBrush = new SolidColorBrush(Color.FromRgb(255, 241, 0)); // Amarelo

    foreach (var match in uniqueMatches)
    {
      // Adiciona texto antes do match
      if (match.Start > currentIndex)
      {
        inlines.Add(new Run(text.Substring(currentIndex, match.Start - currentIndex)));
      }

      // Adiciona o texto matched com highlight
      var matchedText = text.Substring(match.Start, match.Length);
      var highlightRun = new Run(matchedText)
      {
        Background = highlightBrush,
        Foreground = Brushes.Black,
      };
      inlines.Add(highlightRun);

      currentIndex = match.Start + match.Length;
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


