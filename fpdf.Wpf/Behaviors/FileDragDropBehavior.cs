using System.IO;
using System.Windows;
using System.Windows.Input;

namespace fpdf.Wpf.Behaviors;

public static class FileDragDropBehavior
{
  public static readonly DependencyProperty DropCommandProperty =
    DependencyProperty.RegisterAttached(
      "DropCommand",
      typeof(ICommand),
      typeof(FileDragDropBehavior),
      new PropertyMetadata(null, OnDropCommandChanged));

  public static readonly DependencyProperty IsDropTargetProperty =
    DependencyProperty.RegisterAttached(
      "IsDropTarget",
      typeof(bool),
      typeof(FileDragDropBehavior),
      new PropertyMetadata(false, OnIsDropTargetChanged));

  public static ICommand GetDropCommand(DependencyObject obj) =>
    (ICommand)obj.GetValue(DropCommandProperty);

  public static void SetDropCommand(DependencyObject obj, ICommand value) =>
    obj.SetValue(DropCommandProperty, value);

  public static bool GetIsDropTarget(DependencyObject obj) =>
    (bool)obj.GetValue(IsDropTargetProperty);

  public static void SetIsDropTarget(DependencyObject obj, bool value) =>
    obj.SetValue(IsDropTargetProperty, value);

  private static void OnDropCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is not UIElement element) return;

    if (e.NewValue != null)
    {
      element.AllowDrop = true;
      element.DragOver += OnDragOver;
      element.Drop += OnDrop;
    }
    else
    {
      element.AllowDrop = false;
      element.DragOver -= OnDragOver;
      element.Drop -= OnDrop;
    }
  }

  private static void OnIsDropTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  {
    if (d is not UIElement element) return;

    if ((bool)e.NewValue)
    {
      element.AllowDrop = true;
      element.DragOver += OnDragOver;
    }
    else
    {
      element.DragOver -= OnDragOver;
    }
  }

  private static void OnDragOver(object sender, DragEventArgs e)
  {
    if (e.Data.GetDataPresent(DataFormats.FileDrop))
    {
      var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
      if (files != null && files.Any(f => 
        f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
        f.EndsWith(".step", StringComparison.OrdinalIgnoreCase) ||
        f.EndsWith(".stp", StringComparison.OrdinalIgnoreCase) ||
        f.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase)))
      {
        e.Effects = DragDropEffects.Copy; // Sempre copia, nunca move
        e.Handled = true;
        return;
      }
    }

    e.Effects = DragDropEffects.None;
    e.Handled = true;
  }

  private static void OnDrop(object sender, DragEventArgs e)
  {
    if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

    var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
    if (files == null) return;

    // Aceita .pdf, .step, .stp e .dwg
    var supportedFiles = files
      .Where(f => File.Exists(f) && (
        f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
        f.EndsWith(".step", StringComparison.OrdinalIgnoreCase) ||
        f.EndsWith(".stp", StringComparison.OrdinalIgnoreCase) ||
        f.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase)))
      .ToArray();

    if (supportedFiles.Length == 0) return;

    var element = (UIElement)sender;
    var command = GetDropCommand(element);
    if (command?.CanExecute(supportedFiles) == true)
    {
      command.Execute(supportedFiles);
    }

    e.Handled = true;
  }

  public static string[] FilterSupportedFiles(DragEventArgs e)
  {
    if (!e.Data.GetDataPresent(DataFormats.FileDrop))
      return Array.Empty<string>();

    var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
    if (files == null) return Array.Empty<string>();

    return files
      .Where(f => File.Exists(f) && (
        f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
        f.EndsWith(".step", StringComparison.OrdinalIgnoreCase) ||
        f.EndsWith(".stp", StringComparison.OrdinalIgnoreCase) ||
        f.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase)))
      .ToArray();
  }

  // Mantido para compatibilidade
  public static string[] FilterPdfFiles(DragEventArgs e) => FilterSupportedFiles(e);
}
