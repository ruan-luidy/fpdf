using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace fpdf.Core.Models;

public partial class PdfFileInfo : ObservableObject
{
  [ObservableProperty]
  private string _fileName = string.Empty;

  [ObservableProperty]
  private string _fullPath = string.Empty;

  [ObservableProperty]
  private long _fileSize;

  [ObservableProperty]
  private DateTime _lastModified;

  [ObservableProperty]
  private int _pageCount;

  [ObservableProperty]
  private BitmapSource? _thumbnail;

  [ObservableProperty]
  private bool _isSelected;

  [ObservableProperty]
  private bool _isLoadingThumbnail;

  public string FileSizeFormatted => FormatFileSize(FileSize);

  public string LastModifiedFormatted => LastModified.ToString("dd/MM/yyyy HH:mm");

  private static string FormatFileSize(long bytes)
  {
    string[] sizes = { "B", "KB", "MB", "GB" };
    int order = 0;
    double size = bytes;

    while (size >= 1024 && order < sizes.Length - 1)
    {
      order++;
      size /= 1024;
    }

    return $"{size:0.##} {sizes[order]}";
  }
}
