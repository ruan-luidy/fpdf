using fpdf.Core.Models;

namespace fpdf.Core.Services;

public interface INetworkService
{
  Task<List<NetworkFolder>> GetSubfoldersAsync(string path, CancellationToken cancellationToken = default);

  Task<List<PdfFileInfo>> GetPdfFilesAsync(string path, CancellationToken cancellationToken = default);

  Task<bool> FolderExistsAsync(string path, CancellationToken cancellationToken = default);

  Task<bool> HasSubfoldersAsync(string path, CancellationToken cancellationToken = default);

  Task<List<NetworkFolder>> GetNetworkRootsAsync(CancellationToken cancellationToken = default);
}
