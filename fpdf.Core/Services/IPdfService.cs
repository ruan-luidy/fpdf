using fpdf.Core.Models;
using System.Windows.Media.Imaging;

namespace fpdf.Core.Services;

public interface IPdfService
{
  Task<BitmapSource?> GetThumbnailAsync(string filePath, int width = 64, int height = 64, CancellationToken cancellationToken = default);

  Task<BitmapSource?> RenderPageAsync(string filePath, int pageIndex, double zoom = 1.0, CancellationToken cancellationToken = default);

  Task<int> GetPageCountAsync(string filePath, CancellationToken cancellationToken = default);

  Task<PdfFileInfo> GetPdfInfoAsync(string filePath, CancellationToken cancellationToken = default);

  void Dispose();
}
