using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using fpdf.Wpf.ViewModels;

namespace fpdf.Wpf.Views.Controls;

public partial class PdfViewerControl : UserControl
{
    private bool _isWebViewReady;

    // Middle-click drag to pan in any direction
    private const string PanScript = """
        (function() {
            let panning = false, lastX = 0, lastY = 0;
            document.addEventListener('pointerdown', e => {
                if (e.button === 1) {
                    panning = true;
                    lastX = e.clientX;
                    lastY = e.clientY;
                    document.body.style.cursor = 'grabbing';
                    e.preventDefault();
                }
            });
            document.addEventListener('pointermove', e => {
                if (!panning) return;
                window.scrollBy(lastX - e.clientX, lastY - e.clientY);
                lastX = e.clientX;
                lastY = e.clientY;
            });
            document.addEventListener('pointerup', e => {
                if (e.button === 1) {
                    panning = false;
                    document.body.style.cursor = '';
                }
            });
        })();
        """;

    public PdfViewerControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await WebViewer.EnsureCoreWebView2Async();
            _isWebViewReady = true;

            WebViewer.CoreWebView2.Settings.AreDevToolsEnabled = false;
            WebViewer.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            WebViewer.CoreWebView2.Settings.IsStatusBarEnabled = false;
            WebViewer.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;

            // Inject pan script for every page load (including PDFs)
            await WebViewer.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(PanScript);

            WebViewer.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

            // If a file was already set before WebView2 was ready, navigate now
            if (DataContext is PdfViewerViewModel vm && vm.CurrentFile != null)
            {
                NavigateTo(vm.CurrentFile.FullPath);
            }
        }
        catch (Exception ex)
        {
            if (DataContext is PdfViewerViewModel vm)
            {
                vm.ErrorMessage = $"Falha ao inicializar WebView2: {ex.Message}";
            }
        }
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess && e.WebErrorStatus != CoreWebView2WebErrorStatus.OperationCanceled)
        {
            if (DataContext is PdfViewerViewModel vm)
            {
                vm.ErrorMessage = $"Erro ao carregar PDF: {e.WebErrorStatus}";
            }
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

            if (!_isWebViewReady)
                return;

            if (vm.CurrentFile != null)
            {
                NavigateTo(vm.CurrentFile.FullPath);
            }
            else
            {
                WebViewer.CoreWebView2.Navigate("about:blank");
            }
        }
    }

    private void NavigateTo(string filePath)
    {
        var uri = new Uri(filePath).AbsoluteUri;
        WebViewer.CoreWebView2.Navigate(uri);
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
