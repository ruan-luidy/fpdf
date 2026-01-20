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
    if (e.AddedItems.Count > 0 && e.AddedItems[0] is PdfFileInfo file)
    {
      if (ViewModel != null)
      {
        ViewModel.SelectFileCommand.Execute(file);
      }
    }
  }
}
