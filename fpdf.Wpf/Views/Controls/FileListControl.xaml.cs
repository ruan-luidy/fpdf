using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using fpdf.Core.Models;
using fpdf.Wpf.ViewModels;

namespace fpdf.Wpf.Views.Controls;

public partial class FileListControl : UserControl
{
  private Point _dragStartPoint;
  private bool _isDragging;

  public FileListControl()
  {
    InitializeComponent();
    FilesDataGrid.PreviewMouseLeftButtonDown += DataGrid_PreviewMouseLeftButtonDown;
    FilesDataGrid.PreviewMouseMove += DataGrid_PreviewMouseMove;
    FilesDataGrid.PreviewMouseLeftButtonUp += DataGrid_PreviewMouseLeftButtonUp;
  }

  private FileListViewModel? ViewModel => DataContext as FileListViewModel;

  private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
  {
    _dragStartPoint = e.GetPosition(null);
    _isDragging = false;
  }

  private void DataGrid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
  {
    _isDragging = false;
  }

  private void DataGrid_PreviewMouseMove(object sender, MouseEventArgs e)
  {
    if (e.LeftButton != MouseButtonState.Pressed) return;
    if (_isDragging) return;

    var position = e.GetPosition(null);
    var diff = _dragStartPoint - position;

    if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
        Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
      return;

    var selectedFiles = ViewModel?.GetSelectedFiles().ToList();
    if (selectedFiles == null || selectedFiles.Count == 0)
    {
      if (ViewModel?.SelectedFile != null)
        selectedFiles = new List<PdfFileInfo> { ViewModel.SelectedFile };
      else
        return;
    }

    _isDragging = true;

    var filePaths = selectedFiles.Select(f => f.FullPath).ToArray();
    var dataObject = new DataObject(DataFormats.FileDrop, filePaths);
    
    // IMPORTANTE: Usa DragDropEffects.Copy para garantir que os arquivos
    // sejam COPIADOS e nunca MOVIDOS quando arrastados para fora do app
    DragDrop.DoDragDrop(FilesDataGrid, dataObject, DragDropEffects.Copy);

    _isDragging = false;
  }

  private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
  {
    System.Diagnostics.Debug.WriteLine($"FileListControl: SelectionChanged, AddedItems={e.AddedItems.Count}");
    if (e.AddedItems.Count > 0 && e.AddedItems[0] is PdfFileInfo file)
    {
      System.Diagnostics.Debug.WriteLine($"FileListControl: Selected file {file.FileName}");
      if (ViewModel != null)
      {
        ViewModel.SelectFileCommand.Execute(file);
      }
    }
  }

  private void DataGrid_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
  {
    e.Handled = true;
  }

  private void DataGridCell_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
  {
    e.Handled = true;
  }

  private void DataGridRow_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
  {
    e.Handled = true;
  }
}
