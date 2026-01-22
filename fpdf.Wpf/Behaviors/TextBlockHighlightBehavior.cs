using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace fpdf.Wpf.Behaviors;

public static class TextBlockHighlightBehavior
{
  public static readonly DependencyProperty HighlightedInlinesProperty =
    DependencyProperty.RegisterAttached(
      "HighlightedInlines",
      typeof(IEnumerable<Inline>),
      typeof(TextBlockHighlightBehavior),
      new PropertyMetadata(null, OnHighlightedInlinesChanged));

  public static IEnumerable<Inline>? GetHighlightedInlines(DependencyObject obj)
  {
    return (IEnumerable<Inline>?)obj.GetValue(HighlightedInlinesProperty);
  }

  public static void SetHighlightedInlines(DependencyObject obj, IEnumerable<Inline>? value)
  {
    obj.SetValue(HighlightedInlinesProperty, value);
  }

  private static void OnHighlightedInlinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is not TextBlock textBlock)
      return;

    textBlock.Inlines.Clear();

    if (e.NewValue is IEnumerable<Inline> inlines)
    {
      foreach (var inline in inlines)
      {
        textBlock.Inlines.Add(inline);
      }
    }
  }
}
