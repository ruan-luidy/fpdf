using fpdf.Core.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.IO;
using System.Windows.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace fpdf.Core.Services;

public class PdfService : IPdfService, IDisposable
{
  private readonly object _lock = new();
  private bool _disposed;

  public async Task<BitmapSource?> GetThumbnailAsync(string filePath, int width = 64, int height = 64, CancellationToken cancellationToken = default)
  {
    try
    {
      cancellationToken.ThrowIfCancellationRequested();

      var file = await StorageFile.GetFileFromPathAsync(filePath);
      var pdfDocument = await PdfDocument.LoadFromFileAsync(file);

      if (pdfDocument.PageCount == 0) return null;

      using var page = pdfDocument.GetPage(0);

      // Calcula escala mantendo aspect ratio
      var pageSize = page.Size;
      var scale = Math.Min(width / pageSize.Width, height / pageSize.Height);
      var renderWidth = (uint)(pageSize.Width * scale);
      var renderHeight = (uint)(pageSize.Height * scale);

      using var stream = new InMemoryRandomAccessStream();

      var options = new PdfPageRenderOptions
      {
        DestinationWidth = renderWidth,
        DestinationHeight = renderHeight,
        BackgroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255)
      };

      await page.RenderToStreamAsync(stream, options);

      return await StreamToBitmapSourceAsync(stream);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch
    {
      return null;
    }
  }

  public async Task<BitmapSource?> RenderPageAsync(string filePath, int pageIndex, double zoom = 1.0, CancellationToken cancellationToken = default)
  {
    try
    {
      cancellationToken.ThrowIfCancellationRequested();

      var file = await StorageFile.GetFileFromPathAsync(filePath);
      var pdfDocument = await PdfDocument.LoadFromFileAsync(file);

      if (pageIndex < 0 || pageIndex >= (int)pdfDocument.PageCount) return null;

      using var page = pdfDocument.GetPage((uint)pageIndex);

      var pageSize = page.Size;
      var renderWidth = (uint)(pageSize.Width * zoom);
      var renderHeight = (uint)(pageSize.Height * zoom);

      using var stream = new InMemoryRandomAccessStream();

      var options = new PdfPageRenderOptions
      {
        DestinationWidth = renderWidth,
        DestinationHeight = renderHeight,
        BackgroundColor = Windows.UI.Color.FromArgb(255, 255, 255, 255)
      };

      await page.RenderToStreamAsync(stream, options);

      return await StreamToBitmapSourceAsync(stream);
    }
    catch (OperationCanceledException)
    {
      throw;
    }
    catch
    {
      return null;
    }
  }

  public async Task<int> GetPageCountAsync(string filePath, CancellationToken cancellationToken = default)
  {
    return await Task.Run(() =>
    {
      try
      {
        lock (_lock)
        {
          using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.ReadOnly);
          return document.PageCount;
        }
      }
      catch
      {
        return 0;
      }
    }, cancellationToken);
  }

  public async Task<PdfFileInfo> GetPdfInfoAsync(string filePath, CancellationToken cancellationToken = default)
  {
    return await Task.Run(() =>
    {
      var fileInfo = new FileInfo(filePath);
      var pdfInfo = new PdfFileInfo
      {
        FileName = fileInfo.Name,
        FullPath = fileInfo.FullName,
        FileSize = fileInfo.Length,
        LastModified = fileInfo.LastWriteTime
      };

      try
      {
        lock (_lock)
        {
          using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.ReadOnly);
          pdfInfo.PageCount = document.PageCount;
        }
      }
      catch
      {
        pdfInfo.PageCount = 0;
      }

      return pdfInfo;
    }, cancellationToken);
  }

  private static async Task<BitmapSource?> StreamToBitmapSourceAsync(IRandomAccessStream stream)
  {
    stream.Seek(0);

    using var memoryStream = new MemoryStream();
    await stream.AsStreamForRead().CopyToAsync(memoryStream);
    memoryStream.Position = 0;

    var bitmapImage = new BitmapImage();
    bitmapImage.BeginInit();
    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
    bitmapImage.StreamSource = memoryStream;
    bitmapImage.EndInit();
    bitmapImage.Freeze();

    return bitmapImage;
  }

  public void Dispose()
  {
    if (_disposed) return;
    _disposed = true;
    GC.SuppressFinalize(this);
  }
}
