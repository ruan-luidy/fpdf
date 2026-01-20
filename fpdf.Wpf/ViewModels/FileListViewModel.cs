using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fpdf.Core.Models;
using fpdf.Core.Services;

namespace fpdf.Wpf.ViewModels;

public partial class FileListViewModel : ObservableObject
{
  private readonly INetworkService _networkService;
  private readonly IPdfService _pdfService;
  private CancellationTokenSource? _loadCts;
  private CancellationTokenSource? _thumbnailCts;

  [ObservableProperty]
  private string _currentPath = string.Empty;

  [ObservableProperty]
  private string _searchText = string.Empty;

  [ObservableProperty]
  private bool _isLoading;

  [ObservableProperty]
  private PdfFileInfo? _selectedFile;

  [ObservableProperty]
  private int _fileCount;

  [ObservableProperty]
  private int _selectedCount;

  [ObservableProperty]
  private string _sortColumn = "FileName";

  [ObservableProperty]
  private ListSortDirection _sortDirection = ListSortDirection.Ascending;

  public ObservableCollection<PdfFileInfo> Files { get; } = new();
  public ICollectionView FilesView { get; }

  public event EventHandler<PdfFileInfo>? FileSelected;
  public event EventHandler<IEnumerable<PdfFileInfo>>? FilesSelectedForPrint;

  public FileListViewModel(INetworkService networkService, IPdfService pdfService)
  {
    _networkService = networkService;
    _pdfService = pdfService;

    FilesView = CollectionViewSource.GetDefaultView(Files);
    FilesView.Filter = FilterFiles;
  }

  partial void OnSearchTextChanged(string value)
  {
    FilesView.Refresh();
  }

  [RelayCommand]
  private async Task LoadFilesAsync(string path)
  {
    if (string.IsNullOrWhiteSpace(path)) return;

    // Cancela operacoes anteriores
    _loadCts?.Cancel();
    _thumbnailCts?.Cancel();
    _loadCts = new CancellationTokenSource();
    _thumbnailCts = new CancellationTokenSource();

    CurrentPath = path;
    IsLoading = true;
    Files.Clear();

    try
    {
      var files = await _networkService.GetPdfFilesAsync(path, _loadCts.Token);

      foreach (var file in files)
      {
        Files.Add(file);
      }

      FileCount = Files.Count;

      // Carrega thumbnails em background
      _ = LoadThumbnailsAsync(_thumbnailCts.Token);
    }
    catch (OperationCanceledException)
    {
      // Cancelado - ok
    }
    finally
    {
      IsLoading = false;
    }
  }

  private async Task LoadThumbnailsAsync(CancellationToken cancellationToken)
  {
    foreach (var file in Files.ToList())
    {
      if (cancellationToken.IsCancellationRequested) break;

      try
      {
        file.IsLoadingThumbnail = true;
        file.Thumbnail = await _pdfService.GetThumbnailAsync(file.FullPath, 64, 64, cancellationToken);
        file.PageCount = await _pdfService.GetPageCountAsync(file.FullPath, cancellationToken);
      }
      catch (OperationCanceledException)
      {
        break;
      }
      catch
      {
        // Falha no thumbnail - continua
      }
      finally
      {
        file.IsLoadingThumbnail = false;
      }
    }
  }

  [RelayCommand]
  private void SelectFile(PdfFileInfo file)
  {
    SelectedFile = file;
    FileSelected?.Invoke(this, file);
    UpdateSelectedCount();
  }

  [RelayCommand]
  private void ToggleFileSelection(PdfFileInfo file)
  {
    file.IsSelected = !file.IsSelected;
    UpdateSelectedCount();
  }

  [RelayCommand]
  private void SelectAll()
  {
    foreach (var file in Files)
    {
      file.IsSelected = true;
    }
    UpdateSelectedCount();
  }

  [RelayCommand]
  private void ClearSelection()
  {
    foreach (var file in Files)
    {
      file.IsSelected = false;
    }
    UpdateSelectedCount();
  }

  [RelayCommand]
  private void PrintSelected()
  {
    var selectedFiles = Files.Where(f => f.IsSelected).ToList();

    if (selectedFiles.Count == 0 && SelectedFile != null)
    {
      selectedFiles.Add(SelectedFile);
    }

    if (selectedFiles.Count > 0)
    {
      FilesSelectedForPrint?.Invoke(this, selectedFiles);
    }
  }

  [RelayCommand]
  private void Sort(string column)
  {
    if (SortColumn == column)
    {
      SortDirection = SortDirection == ListSortDirection.Ascending
          ? ListSortDirection.Descending
          : ListSortDirection.Ascending;
    }
    else
    {
      SortColumn = column;
      SortDirection = ListSortDirection.Ascending;
    }

    FilesView.SortDescriptions.Clear();
    FilesView.SortDescriptions.Add(new SortDescription(column, SortDirection));
  }

  [RelayCommand]
  private async Task RefreshAsync()
  {
    await LoadFilesAsync(CurrentPath);
  }

  private bool FilterFiles(object obj)
  {
    if (obj is not PdfFileInfo file) return false;
    if (string.IsNullOrWhiteSpace(SearchText)) return true;

    return file.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
  }

  private void UpdateSelectedCount()
  {
    SelectedCount = Files.Count(f => f.IsSelected);
  }

  public IEnumerable<PdfFileInfo> GetSelectedFiles()
  {
    return Files.Where(f => f.IsSelected);
  }
}
