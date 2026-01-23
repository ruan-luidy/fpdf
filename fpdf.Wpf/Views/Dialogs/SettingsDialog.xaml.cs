using System.Windows;
using fpdf.Wpf.ViewModels;

namespace fpdf.Wpf.Views.Dialogs;

public partial class SettingsDialog : HandyControl.Controls.Window
{
  public SettingsDialog(SettingsViewModel viewModel)
  {
    InitializeComponent();
    DataContext = viewModel;
  }

  private async void Save_Click(object sender, RoutedEventArgs e)
  {
    if (DataContext is SettingsViewModel vm)
    {
      await vm.SaveCommand.ExecuteAsync(null);
    }

    DialogResult = true;
    Close();
  }

  private void Cancel_Click(object sender, RoutedEventArgs e)
  {
    // Restaura o idioma original se o usuario mudou mas nao salvou
    if (DataContext is SettingsViewModel vm)
    {
      vm.RestoreOriginalLanguage();
    }

    DialogResult = false;
    Close();
  }
}
