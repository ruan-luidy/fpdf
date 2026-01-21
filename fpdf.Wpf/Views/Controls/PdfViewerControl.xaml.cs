using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using fpdf.Wpf.ViewModels;

namespace fpdf.Wpf.Views.Controls;

public partial class PdfViewerControl : UserControl
{
    public PdfViewerControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PreviewHost.Error += OnPreviewError;
    }

    private void OnPreviewError(string message)
    {
        if (DataContext is PdfViewerViewModel vm)
        {
            vm.ErrorMessage = message;
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is PdfViewerViewModel oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is PdfViewerViewModel newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PdfViewerViewModel.CurrentFile) && sender is PdfViewerViewModel vm)
        {
            vm.ErrorMessage = null;
            PreviewHost.FilePath = vm.CurrentFile?.FullPath;
        }
    }

    private void OnOpenExternalClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is PdfViewerViewModel vm && vm.CurrentFile != null)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = vm.CurrentFile.FullPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao abrir arquivo: {ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
