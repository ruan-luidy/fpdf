using CommunityToolkit.Mvvm.ComponentModel;

namespace fpdf.Core.Models;

public enum PrintJobStatus
{
  Pending,
  Printing,
  Completed,
  Failed,
  Cancelled
}

public partial class PrintJob : ObservableObject
{
  [ObservableProperty]
  private Guid _id = Guid.NewGuid();

  [ObservableProperty]
  private string _filePath = string.Empty;

  [ObservableProperty]
  private string _fileName = string.Empty;

  [ObservableProperty]
  private string _printerName = string.Empty;

  [ObservableProperty]
  private int _copies = 1;

  [ObservableProperty]
  private string _pageRange = "all";

  [ObservableProperty]
  private bool _duplex;

  [ObservableProperty]
  private PrintJobStatus _status = PrintJobStatus.Pending;

  [ObservableProperty]
  private string? _errorMessage;

  [ObservableProperty]
  private DateTime _createdAt = DateTime.Now;

  [ObservableProperty]
  private DateTime? _completedAt;

  [ObservableProperty]
  private int _progress;

  public string StatusText => Status switch
  {
    PrintJobStatus.Pending => "Aguardando",
    PrintJobStatus.Printing => "Imprimindo...",
    PrintJobStatus.Completed => "Concluido",
    PrintJobStatus.Failed => "Falhou",
    PrintJobStatus.Cancelled => "Cancelado",
    _ => "Desconhecido"
  };
}
