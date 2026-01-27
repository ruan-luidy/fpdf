using System.Windows;
using System.Windows.Controls;
using fpdf.Wpf.ViewModels;
using fpdf.Wpf.Views.Dialogs;

namespace fpdf.Wpf.Views.Controls;

public partial class PrintQueueControl : UserControl
{
  public PrintQueueControl()
  {
    InitializeComponent();
  }

  private void OpenQueueDialog_Click(object sender, RoutedEventArgs e)
  {
    if (DataContext is PrintQueueViewModel vm)
    {
      var dialog = new PrintQueueDialog(vm)
      {
        Owner = Window.GetWindow(this)
      };
      dialog.Show();
    }
  }

  private async void OpenHistoryDialog_Click(object sender, RoutedEventArgs e)
  {
    var vm = App.GetService<PrintHistoryViewModel>();
    var dialog = new PrintHistoryDialog(vm)
    {
      Owner = Window.GetWindow(this)
    };

    await vm.LoadCommand.ExecuteAsync(null);
    dialog.Show();
  }
}
