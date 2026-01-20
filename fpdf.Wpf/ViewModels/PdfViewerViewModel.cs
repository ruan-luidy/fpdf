using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fpdf.Core.Models;
using fpdf.Core.Services;

namespace fpdf.Wpf.ViewModels;

public partial class PdfViewerViewModel : ObservableObject
{
  private readonly IPdfService _pdfService;
  private CancellationTokenSource? _renderCts;

  [ObservableProperty]
  private PdfFileInfo? _currentFile;

  [ObservableProperty]
  private BitmapSource? _currentPage;

  [ObservableProperty]
  private int _currentPageIndex;

  [ObservableProperty]
  private int _totalPages;

  [ObservableProperty]
  private double _zoom = 1.0;

  [ObservableProperty]
  private double _rotation;

  [ObservableProperty]
  private bool _isLoading;

  [ObservableProperty]
  private string? _errorMessage;

  public bool HasPreviousPage => CurrentPageIndex > 0;
  public bool HasNextPage => CurrentPageIndex < TotalPages - 1;
  public string PageInfo => TotalPages > 0 ? $"{CurrentPageIndex + 1} / {TotalPages}" : "0 / 0";

  public PdfViewerViewModel(IPdfService pdfService)
  {
    _pdfService = pdfService;
  }

  partial void OnCurrentPageIndexChanged(int value)
  {
    OnPropertyChanged(nameof(HasPreviousPage));
    OnPropertyChanged(nameof(HasNextPage));
    OnPropertyChanged(nameof(PageInfo));
    _ = RenderCurrentPageAsync();
  }

  partial void OnZoomChanged(double value)
  {
    _ = RenderCurrentPageAsync();
  }

  [RelayCommand]
  private async Task LoadFileAsync(PdfFileInfo file)
  {
    if (file == null) return;

    _renderCts?.Cancel();
    _renderCts = new CancellationTokenSource();

    CurrentFile = file;
    CurrentPageIndex = 0;
    Zoom = 1.0;
    Rotation = 0;
    ErrorMessage = null;

    IsLoading = true;

    try
    {
      TotalPages = await _pdfService.GetPageCountAsync(file.FullPath, _renderCts.Token);
      OnPropertyChanged(nameof(PageInfo));

      await RenderCurrentPageAsync();
    }
    catch (OperationCanceledException)
    {
      // Cancelado - ok
    }
    catch (Exception ex)
    {
      ErrorMessage = $"Erro ao carregar PDF: {ex.Message}";
      TotalPages = 0;
      CurrentPage = null;
    }
    finally
    {
      IsLoading = false;
    }
  }

  [RelayCommand]
  private void NextPage()
  {
    if (HasNextPage)
    {
      CurrentPageIndex++;
    }
  }

  [RelayCommand]
  private void PreviousPage()
  {
    if (HasPreviousPage)
    {
      CurrentPageIndex--;
    }
  }

  [RelayCommand]
  private void FirstPage()
  {
    CurrentPageIndex = 0;
  }

  [RelayCommand]
  private void LastPage()
  {
    if (TotalPages > 0)
    {
      CurrentPageIndex = TotalPages - 1;
    }
  }

  [RelayCommand]
  private void GoToPage(int pageNumber)
  {
    if (pageNumber >= 1 && pageNumber <= TotalPages)
    {
      CurrentPageIndex = pageNumber - 1;
    }
  }

  [RelayCommand]
  private void ZoomIn()
  {
    if (Zoom < 4.0)
    {
      Zoom = Math.Min(4.0, Zoom + 0.25);
    }
  }

  [RelayCommand]
  private void ZoomOut()
  {
    if (Zoom > 0.25)
    {
      Zoom = Math.Max(0.25, Zoom - 0.25);
    }
  }

  [RelayCommand]
  private void ResetZoom()
  {
    Zoom = 1.0;
  }

  [RelayCommand]
  private void FitToWidth()
  {
    // Implementar baseado no tamanho do container
    Zoom = 1.0;
  }

  [RelayCommand]
  private void RotateClockwise()
  {
    Rotation = (Rotation + 90) % 360;
  }

  [RelayCommand]
  private void RotateCounterClockwise()
  {
    Rotation = (Rotation - 90 + 360) % 360;
  }

  private async Task RenderCurrentPageAsync()
  {
    if (CurrentFile == null || TotalPages == 0) return;

    _renderCts?.Cancel();
    _renderCts = new CancellationTokenSource();

    IsLoading = true;

    try
    {
      CurrentPage = await _pdfService.RenderPageAsync(
          CurrentFile.FullPath,
          CurrentPageIndex,
          Zoom,
          _renderCts.Token);
    }
    catch (OperationCanceledException)
    {
      // Cancelado - ok
    }
    catch (Exception ex)
    {
      ErrorMessage = $"Erro ao renderizar pagina: {ex.Message}";
    }
    finally
    {
      IsLoading = false;
    }
  }

  public void Clear()
  {
    _renderCts?.Cancel();
    CurrentFile = null;
    CurrentPage = null;
    TotalPages = 0;
    CurrentPageIndex = 0;
    ErrorMessage = null;
  }
}
