using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fpdf.Core.Models;

namespace fpdf.Wpf.ViewModels;

public partial class PdfViewerViewModel : ObservableObject
{
  [ObservableProperty]
  private PdfFileInfo? _currentFile;

  [ObservableProperty]
  private bool _isLoading;

  [ObservableProperty]
  private string? _errorMessage;

  [RelayCommand]
  private void LoadFile(PdfFileInfo? file)
  {
    ErrorMessage = null;
    CurrentFile = file;
  }

  public void Clear()
  {
    CurrentFile = null;
    ErrorMessage = null;
  }
}
