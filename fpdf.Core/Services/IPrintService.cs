using fpdf.Core.Models;

namespace fpdf.Core.Services;

public interface IPrintService
{
  Task<List<PrinterInfo>> GetPrintersAsync(CancellationToken cancellationToken = default);

  Task<PrinterInfo?> GetDefaultPrinterAsync(CancellationToken cancellationToken = default);

  Task<bool> PrintAsync(PrintJob job, CancellationToken cancellationToken = default);

  Task<bool> PrintBatchAsync(IEnumerable<PrintJob> jobs, IProgress<PrintJob>? progress = null, CancellationToken cancellationToken = default);

  void CancelJob(Guid jobId);

  void CancelAllJobs();

  event EventHandler<PrintJob>? JobStatusChanged;
}
