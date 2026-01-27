using fpdf.Core.Models;

namespace fpdf.Core.Services;

public interface IPrintHistoryService
{
  Task InitializeAsync();

  Task SaveJobAsync(PrintHistoryRecord record);

  Task<List<PrintHistoryRecord>> GetHistoryAsync(
    string? searchText = null,
    string? status = null,
    string? printerName = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null,
    int page = 1,
    int pageSize = 50);

  Task<int> GetTotalCountAsync(
    string? searchText = null,
    string? status = null,
    string? printerName = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null);

  Task ClearHistoryBeforeAsync(DateTime date);

  Task<List<string>> GetDistinctPrintersAsync();
}
