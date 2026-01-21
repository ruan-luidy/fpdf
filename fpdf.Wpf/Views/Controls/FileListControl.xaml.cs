using System.Windows;
using System.Windows.Controls;
using fpdf.Core.Models;
using fpdf.Wpf.ViewModels;

namespace fpdf.Wpf.Views.Controls;

public partial class FileListControl : UserControl
{
  public FileListControl()
  {
    InitializeComponent();
  }

  private FileListViewModel? ViewModel => DataContext as FileListViewModel;

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
