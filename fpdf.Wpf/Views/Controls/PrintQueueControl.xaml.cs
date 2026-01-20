using System.Windows;
using System.Windows.Controls;
using fpdf.Wpf.ViewModels;

namespace fpdf.Wpf.Views.Controls;

public partial class PrintQueueControl : UserControl
{
  public PrintQueueControl()
  {
    InitializeComponent();
  }

  private PrintQueueViewModel? ViewModel => DataContext as PrintQueueViewModel;

  private void AllPages_Checked(object sender, RoutedEventArgs e)
  {
    if (ViewModel != null)
    {
      ViewModel.PageRange = "all";
    }
  }
}
