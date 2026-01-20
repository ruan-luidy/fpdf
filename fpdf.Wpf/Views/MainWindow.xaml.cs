using System.Windows;
using fpdf.Wpf.ViewModels;

namespace fpdf.Wpf.Views;

public partial class MainWindow : HandyControl.Controls.Window
{
  public MainWindow(MainViewModel viewModel)
  {
    InitializeComponent();
    DataContext = viewModel;
  }

  private async void Window_Loaded(object sender, RoutedEventArgs e)
  {
    if (DataContext is MainViewModel vm)
    {
      await vm.InitializeCommand.ExecuteAsync(null);
    }
  }

  protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
  {
    base.OnClosing(e);

    // Salva layout ao fechar
    if (DataContext is MainViewModel vm)
    {
      _ = vm.SaveLayoutCommand.ExecuteAsync(null);
    }
  }
}
