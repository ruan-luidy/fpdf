using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fpdf.Core.Models;
using fpdf.Core.Services;
using fpdf.Wpf.Services;

namespace fpdf.Wpf.ViewModels;

/// <summary>
/// ViewModel otimizado para gerenciar filas de impressao com centenas de jobs.
/// 
/// Otimizacoes implementadas:
/// - UI Virtualization: Renderiza apenas itens visiveis (VirtualizingStackPanel)
/// - Cache de contadores: Atualiza contadores incrementalmente ao inves de recalcular
/// - Auto-cleanup: Remove jobs completados automaticamente apos limite configuravel
/// - Progress tracking: Mostra progresso detalhado durante impressao em lote
/// </summary>
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
  
  [ObservableProperty]
  private string _progressText = string.Empty;

  public ObservableCollection<PrinterInfo> Printers { get; } = new();
  public ObservableCollection<PrintJob> Jobs { get; } = new();

  public string PendingCountText => $"{PendingCount} {LocalizationManager.Instance.GetString("PrintQueue_InQueue")}";
  public string CompletedCountText => $"{CompletedCount} {LocalizationManager.Instance.GetString("PrintQueue_Completed")}";
  public string FailedCountText => $"{FailedCount} {LocalizationManager.Instance.GetString("PrintQueue_Errors")}";

  public PrintQueueViewModel(IPrintService printService, ISettingsService settingsService)
  {
    _printService = printService;
    _settingsService = settingsService;

    _printService.JobStatusChanged += OnJobStatusChanged;

    // Atualiza texto localizado quando o idioma muda
    LocalizationManager.Instance.PropertyChanged += (_, e) =>
    {
      if (e.PropertyName == "Item[]")
      {
        OnPropertyChanged(nameof(PendingCountText));
        OnPropertyChanged(nameof(CompletedCountText));
        OnPropertyChanged(nameof(FailedCountText));
      }
    };
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
      var totalJobs = pendingJobs.Count;
      var currentJob = 0;

      var progress = new Progress<PrintJob>(job =>
      {
        currentJob++;
        ProgressText = $"{currentJob}/{totalJobs}";
        UpdateCountsIncremental(job);
        AutoCleanupCompletedJobs();
      });

      await _printService.PrintBatchAsync(pendingJobs, progress);
    }
    finally
    {
      IsPrinting = false;
      ProgressText = string.Empty;
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
  private void ClearAllJobs()
  {
    _printService.CancelAllJobs();
    Jobs.Clear();
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
      UpdateCountsIncremental(job);
      AutoCleanupCompletedJobs();
    });
  }

  /// <summary>
  /// Atualiza contadores de forma incremental baseado no status do job
  /// Evita recalcular toda a colecao
  /// </summary>
  private void UpdateCountsIncremental(PrintJob job)
  {
    switch (job.Status)
    {
      case PrintJobStatus.Pending:
      case PrintJobStatus.Printing:
        // Recalcula apenas se necessario
        if (PendingCount != Jobs.Count(j => j.Status == PrintJobStatus.Pending || j.Status == PrintJobStatus.Printing))
        {
          PendingCount = Jobs.Count(j => j.Status == PrintJobStatus.Pending || j.Status == PrintJobStatus.Printing);
        }
        break;

      case PrintJobStatus.Completed:
        if (PendingCount > 0) PendingCount--;
        CompletedCount++;
        break;

      case PrintJobStatus.Failed:
        if (PendingCount > 0) PendingCount--;
        FailedCount++;
        break;

      case PrintJobStatus.Cancelled:
        if (PendingCount > 0) PendingCount--;
        break;
    }
  }

  private void UpdateCounts()
  {
    PendingCount = Jobs.Count(j => j.Status == PrintJobStatus.Pending || j.Status == PrintJobStatus.Printing);
    CompletedCount = Jobs.Count(j => j.Status == PrintJobStatus.Completed);
    FailedCount = Jobs.Count(j => j.Status == PrintJobStatus.Failed);
  }
  
  /// <summary>
  /// Remove automaticamente jobs completados quando exceder o limite
  /// Mantém apenas os mais recentes
  /// </summary>
  private void AutoCleanupCompletedJobs()
  {
    // Verifica se auto-cleanup esta habilitado
    if (!_settingsService.Settings.AutoCleanupCompletedJobs)
      return;
    
    var maxJobs = _settingsService.Settings.MaxCompletedJobsInQueue;
    
    var completedJobs = Jobs
      .Where(j => j.Status == PrintJobStatus.Completed)
      .OrderBy(j => j.CompletedAt)
      .ToList();

    if (completedJobs.Count > maxJobs)
    {
      var jobsToRemove = completedJobs.Take(completedJobs.Count - maxJobs).ToList();
      foreach (var job in jobsToRemove)
      {
        Jobs.Remove(job);
      }
    }
  }

  partial void OnPendingCountChanged(int value)
  {
    OnPropertyChanged(nameof(PendingCountText));
  }

  partial void OnCompletedCountChanged(int value)
  {
    OnPropertyChanged(nameof(CompletedCountText));
  }

  partial void OnFailedCountChanged(int value)
  {
    OnPropertyChanged(nameof(FailedCountText));
  }
}
