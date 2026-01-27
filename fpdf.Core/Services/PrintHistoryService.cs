using System.IO;
using Dapper;
using fpdf.Core.Models;
using Microsoft.Data.Sqlite;

namespace fpdf.Core.Services;

public class PrintHistoryService : IPrintHistoryService
{
  private readonly string _connectionString;

  public PrintHistoryService()
  {
    var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var dbFolder = Path.Combine(appData, "fpdf");
    Directory.CreateDirectory(dbFolder);
    var dbPath = Path.Combine(dbFolder, "print_history.db");
    _connectionString = $"Data Source={dbPath}";
  }

  public async Task InitializeAsync()
  {
    using var connection = new SqliteConnection(_connectionString);
    await connection.OpenAsync();

    await connection.ExecuteAsync("""
      CREATE TABLE IF NOT EXISTS PrintHistory (
        Id INTEGER PRIMARY KEY AUTOINCREMENT,
        FileName TEXT NOT NULL,
        FilePath TEXT NOT NULL,
        PrinterName TEXT NOT NULL,
        Copies INTEGER NOT NULL DEFAULT 1,
        PageRange TEXT NOT NULL DEFAULT 'all',
        PageCount INTEGER NOT NULL DEFAULT 0,
        Duplex INTEGER NOT NULL DEFAULT 0,
        Status TEXT NOT NULL,
        ErrorMessage TEXT,
        CreatedAt TEXT NOT NULL,
        CompletedAt TEXT
      )
      """);

    await connection.ExecuteAsync(
      "CREATE INDEX IF NOT EXISTS IX_PrintHistory_CreatedAt ON PrintHistory(CreatedAt DESC)");

    await connection.ExecuteAsync(
      "CREATE INDEX IF NOT EXISTS IX_PrintHistory_Status ON PrintHistory(Status)");
  }

  public async Task SaveJobAsync(PrintHistoryRecord record)
  {
    using var connection = new SqliteConnection(_connectionString);
    await connection.OpenAsync();

    await connection.ExecuteAsync("""
      INSERT INTO PrintHistory (FileName, FilePath, PrinterName, Copies, PageRange, PageCount, Duplex, Status, ErrorMessage, CreatedAt, CompletedAt)
      VALUES (@FileName, @FilePath, @PrinterName, @Copies, @PageRange, @PageCount, @Duplex, @Status, @ErrorMessage, @CreatedAt, @CompletedAt)
      """, record);
  }

  public async Task<List<PrintHistoryRecord>> GetHistoryAsync(
    string? searchText = null,
    string? status = null,
    string? printerName = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null,
    int page = 1,
    int pageSize = 50)
  {
    using var connection = new SqliteConnection(_connectionString);
    await connection.OpenAsync();

    var (whereClause, parameters) = BuildWhereClause(searchText, status, printerName, dateFrom, dateTo);

    var sql = $"""
      SELECT * FROM PrintHistory
      {whereClause}
      ORDER BY CreatedAt DESC
      LIMIT @PageSize OFFSET @Offset
      """;

    parameters.Add("PageSize", pageSize);
    parameters.Add("Offset", (page - 1) * pageSize);

    var records = await connection.QueryAsync<PrintHistoryRecord>(sql, parameters);
    return records.ToList();
  }

  public async Task<int> GetTotalCountAsync(
    string? searchText = null,
    string? status = null,
    string? printerName = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null)
  {
    using var connection = new SqliteConnection(_connectionString);
    await connection.OpenAsync();

    var (whereClause, parameters) = BuildWhereClause(searchText, status, printerName, dateFrom, dateTo);

    var sql = $"SELECT COUNT(*) FROM PrintHistory {whereClause}";
    return await connection.ExecuteScalarAsync<int>(sql, parameters);
  }

  public async Task ClearHistoryBeforeAsync(DateTime date)
  {
    using var connection = new SqliteConnection(_connectionString);
    await connection.OpenAsync();

    await connection.ExecuteAsync(
      "DELETE FROM PrintHistory WHERE CreatedAt < @Date",
      new { Date = date.ToString("o") });
  }

  public async Task<List<string>> GetDistinctPrintersAsync()
  {
    using var connection = new SqliteConnection(_connectionString);
    await connection.OpenAsync();

    var printers = await connection.QueryAsync<string>(
      "SELECT DISTINCT PrinterName FROM PrintHistory ORDER BY PrinterName");
    return printers.ToList();
  }

  private static (string whereClause, DynamicParameters parameters) BuildWhereClause(
    string? searchText,
    string? status,
    string? printerName,
    DateTime? dateFrom,
    DateTime? dateTo)
  {
    var conditions = new List<string>();
    var parameters = new DynamicParameters();

    if (!string.IsNullOrWhiteSpace(searchText))
    {
      conditions.Add("FileName LIKE @SearchText");
      parameters.Add("SearchText", $"%{searchText}%");
    }

    if (!string.IsNullOrWhiteSpace(status))
    {
      conditions.Add("Status = @Status");
      parameters.Add("Status", status);
    }

    if (!string.IsNullOrWhiteSpace(printerName))
    {
      conditions.Add("PrinterName = @PrinterName");
      parameters.Add("PrinterName", printerName);
    }

    if (dateFrom.HasValue)
    {
      conditions.Add("CreatedAt >= @DateFrom");
      parameters.Add("DateFrom", dateFrom.Value.ToString("o"));
    }

    if (dateTo.HasValue)
    {
      conditions.Add("CreatedAt <= @DateTo");
      parameters.Add("DateTo", dateTo.Value.Date.AddDays(1).ToString("o"));
    }

    var whereClause = conditions.Count > 0
      ? "WHERE " + string.Join(" AND ", conditions)
      : string.Empty;

    return (whereClause, parameters);
  }
}
