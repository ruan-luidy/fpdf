using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fpdf.Core.Models;
using fpdf.Core.Services;

namespace fpdf.Wpf.ViewModels;

public partial class PrintQueueViewModel : ObservableObject
{
  private readonly IPrintService _printService;
  private readonly ISettingsService _settingsService;

  [ObservableProperty]
  private PrinterInfo? _selectedPrinter;

  [ObservableProperty]
  private bool _isPrinting;

  [ObservableProperty]
  private int _pendingCount;

  [ObservableProperty]
  private int _completedCount;

  [ObservableProperty]
  private int _failedCount;

  [ObservableProperty]
  private int _copies = 1;

  [ObservableProperty]
  private string _pageRange = "all";

  [ObservableProperty]
  private bool _duplex;

  public ObservableCollection<PrinterInfo> Printers { get; } = new();
  public ObservableCollection<PrintJob> Jobs { get; } = new();

  public PrintQueueViewModel(IPrintService printService, ISettingsService settingsService)
  {
    _printService = printService;
    _settingsService = settingsService;

    _printService.JobStatusChanged += OnJobStatusChanged;
  }

  [RelayCommand]
  private async Task LoadPrintersAsync()
  {
    Printers.Clear();

    var printers = await _printService.GetPrintersAsync();

    foreach (var printer in printers)
    {
      Printers.Add(printer);
    }

    // Seleciona impressora padrao ou ultima usada
    var defaultPrinterName = _settingsService.Settings.DefaultPrinter;

    SelectedPrinter = Printers.FirstOrDefault(p => p.Name == defaultPrinterName)
                   ?? Printers.FirstOrDefault(p => p.IsDefault)
                   ?? Printers.FirstOrDefault();
  }

  [RelayCommand]
  private void AddToQueue(IEnumerable<PdfFileInfo> files)
  {
    if (SelectedPrinter == null) return;

    foreach (var file in files)
    {
      var job = new PrintJob
      {
        FilePath = file.FullPath,
        FileName = file.FileName,
        PrinterName = SelectedPrinter.Name,
        Copies = Copies,
        PageRange = PageRange,
        PageCount = file.PageCount,
        Duplex = Duplex
      };

      Jobs.Add(job);
    }

    UpdateCounts();
  }

  [RelayCommand]
  private async Task PrintQueueAsync()
  {
    if (!Jobs.Any(j => j.Status == PrintJobStatus.Pending)) return;

    IsPrinting = true;

    try
    {
      var pendingJobs = Jobs.Where(j => j.Status == PrintJobStatus.Pending).ToList();

      var progress = new Progress<PrintJob>(job =>
      {
        UpdateCounts();
      });

      await _printService.PrintBatchAsync(pendingJobs, progress);
    }
    finally
    {
      IsPrinting = false;
      UpdateCounts();
    }
  }

  [RelayCommand]
  private async Task PrintSingleAsync(PrintJob job)
  {
    if (job == null || job.Status != PrintJobStatus.Pending) return;

    IsPrinting = true;

    try
    {
      await _printService.PrintAsync(job);
    }
    finally
    {
      IsPrinting = false;
      UpdateCounts();
    }
  }

  [RelayCommand]
  private void CancelJob(PrintJob job)
  {
    if (job == null) return;

    if (job.Status == PrintJobStatus.Pending)
    {
      Jobs.Remove(job);
    }
    else if (job.Status == PrintJobStatus.Printing)
    {
      _printService.CancelJob(job.Id);
    }

    UpdateCounts();
  }

  [RelayCommand]
  private void CancelAllJobs()
  {
    _printService.CancelAllJobs();

    var pendingJobs = Jobs.Where(j => j.Status == PrintJobStatus.Pending).ToList();
    foreach (var job in pendingJobs)
    {
      Jobs.Remove(job);
    }

    UpdateCounts();
  }

  [RelayCommand]
  private void ClearCompleted()
  {
    var completedJobs = Jobs.Where(j => j.Status == PrintJobStatus.Completed ||
                                        j.Status == PrintJobStatus.Failed ||
                                        j.Status == PrintJobStatus.Cancelled).ToList();

    foreach (var job in completedJobs)
    {
      Jobs.Remove(job);
    }

    UpdateCounts();
  }

  [RelayCommand]
  private void RetryFailed()
  {
    foreach (var job in Jobs.Where(j => j.Status == PrintJobStatus.Failed))
    {
      job.Status = PrintJobStatus.Pending;
      job.ErrorMessage = null;
    }

    UpdateCounts();
  }

  [RelayCommand]
  private async Task SetDefaultPrinterAsync()
  {
    if (SelectedPrinter == null) return;

    _settingsService.Settings.DefaultPrinter = SelectedPrinter.Name;
    await _settingsService.SaveAsync();
  }

  private void OnJobStatusChanged(object? sender, PrintJob job)
  {
    App.Current.Dispatcher.Invoke(() =>
    {
      UpdateCounts();
    });
  }

  private void UpdateCounts()
  {
    PendingCount = Jobs.Count(j => j.Status == PrintJobStatus.Pending || j.Status == PrintJobStatus.Printing);
    CompletedCount = Jobs.Count(j => j.Status == PrintJobStatus.Completed);
    FailedCount = Jobs.Count(j => j.Status == PrintJobStatus.Failed);
  }
}
