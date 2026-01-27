namespace fpdf.Core.Models;

public class PrintHistoryRecord
{
  public long Id { get; set; }
  public string FileName { get; set; } = string.Empty;
  public string FilePath { get; set; } = string.Empty;
  public string PrinterName { get; set; } = string.Empty;
  public int Copies { get; set; } = 1;
  public string PageRange { get; set; } = "all";
  public int PageCount { get; set; }
  public bool Duplex { get; set; }
  public string Status { get; set; } = string.Empty;
  public string? ErrorMessage { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime? CompletedAt { get; set; }
}
