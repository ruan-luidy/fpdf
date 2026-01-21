using System.Windows;
using fpdf.Wpf.ViewModels;
using fpdf.Wpf.Views.Dialogs;

namespace fpdf.Wpf.Views;

public partial class MainWindow : HandyControl.Controls.Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.OpenSettingsRequested += OnOpenSettingsRequested;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            await vm.InitializeCommand.ExecuteAsync(null);
        }
    }

    private async void OnOpenSettingsRequested(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            var dialog = new SettingsDialog(vm.Settings)
            {
                Owner = this
            };

            await vm.Settings.LoadCommand.ExecuteAsync(null);

            if (dialog.ShowDialog() == true)
            {
                // Configurações salvas - recarrega impressoras se necessário
                await vm.PrintQueue.LoadPrintersCommand.ExecuteAsync(null);
                
                // Atualiza os diretórios de rede na TreeView
                await vm.FolderTree.RefreshNetworkPathsCategoryAsync();
            }
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        base.OnClosing(e);

        if (DataContext is MainViewModel vm)
        {
            vm.OpenSettingsRequested -= OnOpenSettingsRequested;
            _ = vm.SaveLayoutCommand.ExecuteAsync(null);
        }
    }
}
