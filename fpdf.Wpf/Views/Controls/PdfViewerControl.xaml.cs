using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using fpdf.Wpf.ViewModels;

namespace fpdf.Wpf.Views.Controls;

public partial class PdfViewerControl : UserControl
{
    private bool _isWebViewReady;
    private const string ViewerHost = "pdfjs.local";
    private const string FileHost = "pdffile.local";

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

            var pdfjsPath = Path.Combine(AppContext.BaseDirectory, "Assets", "pdfjs");
            WebViewer.CoreWebView2.SetVirtualHostNameToFolderMapping(
                ViewerHost, pdfjsPath, CoreWebView2HostResourceAccessKind.Allow);

            WebViewer.CoreWebView2.Settings.AreDevToolsEnabled = false;
            WebViewer.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            WebViewer.CoreWebView2.Settings.IsStatusBarEnabled = false;
            WebViewer.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;

            WebViewer.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

            _isWebViewReady = true;

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
        var directory = Path.GetDirectoryName(filePath)!;
        var fileName = Path.GetFileName(filePath);

        // Map the PDF's directory so pdf.js can fetch it via https
        WebViewer.CoreWebView2.SetVirtualHostNameToFolderMapping(
            FileHost, directory, CoreWebView2HostResourceAccessKind.Allow);

        var pdfUrl = $"https://{FileHost}/{Uri.EscapeDataString(fileName)}";
        var viewerUrl = $"https://{ViewerHost}/web/viewer.html?file={Uri.EscapeDataString(pdfUrl)}";
        WebViewer.CoreWebView2.Navigate(viewerUrl);
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
