using fpdf.Core.Models;
using System.Diagnostics;
using System.Printing;
using CorePrintJobStatus = fpdf.Core.Models.PrintJobStatus;

namespace fpdf.Core.Services;

public class PrintService : IPrintService
{
  private readonly SemaphoreSlim _printSemaphore = new(1, 1);
  private readonly Dictionary<Guid, CancellationTokenSource> _jobCancellations = new();
  private CancellationTokenSource? _batchCancellation;

  public event EventHandler<PrintJob>? JobStatusChanged;

  public async Task<List<PrinterInfo>> GetPrintersAsync(CancellationToken cancellationToken = default)
  {
    return await Task.Run(() =>
    {
      var printers = new List<PrinterInfo>();

      try
      {
        using var printServer = new LocalPrintServer();
        var defaultPrinter = printServer.DefaultPrintQueue?.Name ?? string.Empty;

        var queues = printServer.GetPrintQueues(new[]
        {
                    EnumeratedPrintQueueTypes.Local,
                    EnumeratedPrintQueueTypes.Connections
                });

        foreach (var queue in queues)
        {
          cancellationToken.ThrowIfCancellationRequested();

          try
          {
            printers.Add(new PrinterInfo
            {
              Name = queue.Name,
              FullName = queue.FullName,
              IsDefault = queue.Name == defaultPrinter,
              IsNetwork = queue.FullName.StartsWith("\\\\"),
              IsOnline = !queue.IsOffline,
              PortName = queue.QueuePort?.Name ?? string.Empty,
              DriverName = queue.QueueDriver?.Name ?? string.Empty
            });
          }
          catch
          {
            // Impressora com problema
          }
          finally
          {
            queue.Dispose();
          }
        }
      }
      catch
      {
        // Fallback: sem impressoras
      }

      return printers.OrderByDescending(p => p.IsDefault)
                    .ThenBy(p => p.Name)
                    .ToList();
    }, cancellationToken);
  }

  public async Task<PrinterInfo?> GetDefaultPrinterAsync(CancellationToken cancellationToken = default)
  {
    var printers = await GetPrintersAsync(cancellationToken);
    return printers.FirstOrDefault(p => p.IsDefault);
  }

  public async Task<bool> PrintAsync(PrintJob job, CancellationToken cancellationToken = default)
  {
    var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    _jobCancellations[job.Id] = cts;

    try
    {
      await _printSemaphore.WaitAsync(cts.Token);

      try
      {
        job.Status = CorePrintJobStatus.Printing;
        OnJobStatusChanged(job);

        var success = await ExecutePrintAsync(job, cts.Token);

        job.Status = success ? CorePrintJobStatus.Completed : CorePrintJobStatus.Failed;
        job.CompletedAt = DateTime.Now;

        if (!success && string.IsNullOrEmpty(job.ErrorMessage))
        {
          job.ErrorMessage = "Falha na impressao";
        }

        OnJobStatusChanged(job);
        return success;
      }
      finally
      {
        _printSemaphore.Release();
      }
    }
    catch (OperationCanceledException)
    {
      job.Status = CorePrintJobStatus.Cancelled;
      job.CompletedAt = DateTime.Now;
      OnJobStatusChanged(job);
      return false;
    }
    finally
    {
      _jobCancellations.Remove(job.Id);
    }
  }

  public async Task<bool> PrintBatchAsync(IEnumerable<PrintJob> jobs, IProgress<PrintJob>? progress = null, CancellationToken cancellationToken = default)
  {
    _batchCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    var allSuccess = true;

    foreach (var job in jobs)
    {
      if (_batchCancellation.Token.IsCancellationRequested)
        break;

      var success = await PrintAsync(job, _batchCancellation.Token);
      progress?.Report(job);

      if (!success)
      {
        allSuccess = false;
      }
    }

    _batchCancellation = null;
    return allSuccess;
  }

  public void CancelJob(Guid jobId)
  {
    if (_jobCancellations.TryGetValue(jobId, out var cts))
    {
      cts.Cancel();
    }
  }

  public void CancelAllJobs()
  {
    _batchCancellation?.Cancel();

    foreach (var cts in _jobCancellations.Values)
    {
      cts.Cancel();
    }
  }

  private async Task<bool> ExecutePrintAsync(PrintJob job, CancellationToken cancellationToken)
  {
    return await Task.Run(() =>
    {
      try
      {
        // Imprime usando Shell com verbo "printto" para impressora especifica
        // ou "print" para impressora padrao
        for (int copy = 0; copy < job.Copies; copy++)
        {
          cancellationToken.ThrowIfCancellationRequested();

          var startInfo = new ProcessStartInfo
          {
            FileName = job.FilePath,
            Verb = "printto",
            Arguments = $"\"{job.PrinterName}\"",
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
          };

          using var process = Process.Start(startInfo);
          if (process == null)
          {
            job.ErrorMessage = "Falha ao iniciar processo de impressao";
            return false;
          }

          // Aguarda o processo terminar (com timeout)
          var completed = process.WaitForExit(120000); // 2 minutos timeout

          if (!completed)
          {
            try { process.Kill(); } catch { }
            job.ErrorMessage = "Timeout na impressao";
            return false;
          }

          // Pequena pausa entre copias
          if (copy < job.Copies - 1)
          {
            Thread.Sleep(500);
          }
        }

        return true;
      }
      catch (Exception ex)
      {
        job.ErrorMessage = ex.Message;
        return false;
      }
    }, cancellationToken);
  }

  protected virtual void OnJobStatusChanged(PrintJob job)
  {
    JobStatusChanged?.Invoke(this, job);
  }
}
