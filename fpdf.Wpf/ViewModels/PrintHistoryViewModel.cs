using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fpdf.Core.Models;
using fpdf.Core.Services;
using fpdf.Wpf.Services;

namespace fpdf.Wpf.ViewModels;

public partial class PrintHistoryViewModel : ObservableObject
{
  private readonly IPrintHistoryService _historyService;
  private readonly IPrintService _printService;
  private readonly ISettingsService _settingsService;

  [ObservableProperty]
  private string _searchText = string.Empty;

  [ObservableProperty]
  private string? _selectedStatus;

  [ObservableProperty]
  private string? _selectedPrinter;

  [ObservableProperty]
  private DateTime? _dateFrom;

  [ObservableProperty]
  private DateTime? _dateTo;

  [ObservableProperty]
  private int _currentPage = 1;

  [ObservableProperty]
  private int _totalPages = 1;

  [ObservableProperty]
  private int _totalCount;

  [ObservableProperty]
  private bool _isLoading;

  private const int PageSize = 50;

  public ObservableCollection<PrintHistoryRecord> Records { get; } = new();
  public ObservableCollection<string> Printers { get; } = new();
  public ObservableCollection<string> Statuses { get; } = new();

  public string PageInfoText => $"{CurrentPage}/{TotalPages}";

  public PrintHistoryViewModel(
    IPrintHistoryService historyService,
    IPrintService printService,
    ISettingsService settingsService)
  {
    _historyService = historyService;
    _printService = printService;
    _settingsService = settingsService;

    LocalizationManager.Instance.PropertyChanged += (_, e) =>
    {
      if (e.PropertyName == "Item[]")
      {
        OnPropertyChanged(nameof(PageInfoText));
      }
    };
  }

  [RelayCommand]
  private async Task LoadAsync()
  {
    IsLoading = true;

    try
    {
      // Carrega filtros
      var printers = await _historyService.GetDistinctPrintersAsync();
      Printers.Clear();
      foreach (var p in printers) Printers.Add(p);

      Statuses.Clear();
      Statuses.Add("Completed");
      Statuses.Add("Failed");
      Statuses.Add("Cancelled");

      // Carrega registros
      await LoadRecordsAsync();
    }
    finally
    {
      IsLoading = false;
    }
  }

  [RelayCommand]
  private async Task SearchAsync()
  {
    CurrentPage = 1;
    await LoadRecordsAsync();
  }

  [RelayCommand]
  private async Task NextPageAsync()
  {
    if (CurrentPage < TotalPages)
    {
      CurrentPage++;
      await LoadRecordsAsync();
    }
  }

  [RelayCommand]
  private async Task PreviousPageAsync()
  {
    if (CurrentPage > 1)
    {
      CurrentPage--;
      await LoadRecordsAsync();
    }
  }

  [RelayCommand]
  private async Task ReprintAsync(PrintHistoryRecord record)
  {
    if (record == null) return;

    var job = new PrintJob
    {
      FilePath = record.FilePath,
      FileName = record.FileName,
      PrinterName = record.PrinterName,
      Copies = record.Copies,
      PageRange = record.PageRange,
      PageCount = record.PageCount,
      Duplex = record.Duplex
    };

    await _printService.PrintAsync(job);

    // Salva no historico novamente
    var newRecord = new PrintHistoryRecord
    {
      FileName = job.FileName,
      FilePath = job.FilePath,
      PrinterName = job.PrinterName,
      Copies = job.Copies,
      PageRange = job.PageRange,
      PageCount = job.PageCount,
      Duplex = job.Duplex,
      Status = job.Status.ToString(),
      ErrorMessage = job.ErrorMessage,
      CreatedAt = job.CreatedAt,
      CompletedAt = job.CompletedAt
    };

    await _historyService.SaveJobAsync(newRecord);
    await LoadRecordsAsync();
  }

  [RelayCommand]
  private async Task ClearOldAsync()
  {
    var cutoff = DateTime.Now.AddDays(-30);
    await _historyService.ClearHistoryBeforeAsync(cutoff);
    await LoadRecordsAsync();
  }

  [RelayCommand]
  private async Task ClearFiltersAsync()
  {
    SearchText = string.Empty;
    SelectedStatus = null;
    SelectedPrinter = null;
    DateFrom = null;
    DateTo = null;
    CurrentPage = 1;
    await LoadRecordsAsync();
  }

  private async Task LoadRecordsAsync()
  {
    IsLoading = true;

    try
    {
      TotalCount = await _historyService.GetTotalCountAsync(
        SearchText, SelectedStatus, SelectedPrinter, DateFrom, DateTo);

      TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));

      if (CurrentPage > TotalPages)
        CurrentPage = TotalPages;

      var records = await _historyService.GetHistoryAsync(
        SearchText, SelectedStatus, SelectedPrinter, DateFrom, DateTo,
        CurrentPage, PageSize);

      Records.Clear();
      foreach (var record in records)
        Records.Add(record);

      OnPropertyChanged(nameof(PageInfoText));
    }
    finally
    {
      IsLoading = false;
    }
  }

  partial void OnSearchTextChanged(string value) => _ = SearchAsync();
  partial void OnSelectedStatusChanged(string? value) => _ = SearchAsync();
  partial void OnSelectedPrinterChanged(string? value) => _ = SearchAsync();
  partial void OnDateFromChanged(DateTime? value) => _ = SearchAsync();
  partial void OnDateToChanged(DateTime? value) => _ = SearchAsync();

  partial void OnCurrentPageChanged(int value)
  {
    OnPropertyChanged(nameof(PageInfoText));
  }

  partial void OnTotalPagesChanged(int value)
  {
    OnPropertyChanged(nameof(PageInfoText));
  }
}
