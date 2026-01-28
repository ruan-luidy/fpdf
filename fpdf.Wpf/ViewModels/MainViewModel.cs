using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fpdf.Core.Models;
using fpdf.Core.Services;
using System.Windows;
using System.Windows.Media.Animation;

namespace fpdf.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
  private readonly ISettingsService _settingsService;

  [ObservableProperty]
  private FolderTreeViewModel _folderTree;

  [ObservableProperty]
  private FileListViewModel _fileList;

  [ObservableProperty]
  private PdfViewerViewModel _pdfViewer;

  [ObservableProperty]
  private PrintQueueViewModel _printQueue;

  [ObservableProperty]
  private SettingsViewModel _settings;

  [ObservableProperty]
  private string _statusMessage = "Pronto";

  [ObservableProperty]
  private bool _isSettingsOpen;

  [ObservableProperty]
  private double _treeViewWidth = 250;

  [ObservableProperty]
  private double _fileListWidth = 350;

  [ObservableProperty]
  private bool _isTreeViewVisible = true;

  private double _savedTreeViewWidth = 250;
  private CancellationTokenSource? _animationCts;

  public event EventHandler? OpenSettingsRequested;
  public event EventHandler? OpenPrintHistoryRequested;

  partial void OnIsTreeViewVisibleChanged(bool value)
  {
    // Cancela animação anterior se existir
    _animationCts?.Cancel();
    _animationCts = new CancellationTokenSource();

    _ = AnimateTreeViewWidthAsync(value, _animationCts.Token);
  }

  private async Task AnimateTreeViewWidthAsync(bool show, CancellationToken cancellationToken)
  {
    double from = TreeViewWidth;
    double to;

    if (!show)
    {
      // Salvando o tamanho atual antes de ocultar
      if (TreeViewWidth > 0)
      {
        _savedTreeViewWidth = TreeViewWidth;
      }
      to = 0;
    }
    else
    {
      // Restaurando o tamanho salvo ao mostrar
      to = _savedTreeViewWidth > 0 ? _savedTreeViewWidth : 250;
    }

    // Animação suave com easing
    const int durationMs = 300;
    const int frameMs = 16; // ~60 FPS
    int frames = durationMs / frameMs;
    double step = (to - from) / frames;

    try
    {
      for (int i = 0; i < frames; i++)
      {
        if (cancellationToken.IsCancellationRequested) return;

        // Easing CubicInOut
        double t = (double)i / frames;
        double easedT = t < 0.5
          ? 4 * t * t * t
          : 1 - Math.Pow(-2 * t + 2, 3) / 2;

        TreeViewWidth = from + (to - from) * easedT;

        await Task.Delay(frameMs, cancellationToken);
      }

      TreeViewWidth = to; // Garante valor final
    }
    catch (TaskCanceledException)
    {
      // Animação cancelada
    }
  }

  public MainViewModel(
      FolderTreeViewModel folderTree,
      FileListViewModel fileList,
      PdfViewerViewModel pdfViewer,
      PrintQueueViewModel printQueue,
      SettingsViewModel settings,
      ISettingsService settingsService)
  {
    _folderTree = folderTree;
    _fileList = fileList;
    _pdfViewer = pdfViewer;
    _printQueue = printQueue;
    _settings = settings;
    _settingsService = settingsService;

    // Conecta eventos
    _folderTree.FolderSelected += OnFolderSelected;
    _fileList.FileSelected += OnFileSelected;
    _fileList.FilesSelectedForPrint += OnFilesSelectedForPrint;
  }

  [RelayCommand]
  private async Task InitializeAsync()
  {
    await _settingsService.LoadAsync();

    // Aplica configuracoes salvas
    TreeViewWidth = _settingsService.Settings.TreeViewWidth;
    _savedTreeViewWidth = TreeViewWidth > 0 ? TreeViewWidth : 250;
    FileListWidth = _settingsService.Settings.FileListWidth;

    // Carrega dados iniciais
    await _folderTree.LoadRootsCommand.ExecuteAsync(null);
    await _printQueue.LoadPrintersCommand.ExecuteAsync(null);

    // Navega para ultima pasta
    if (_settingsService.Settings.RememberLastFolder &&
        !string.IsNullOrEmpty(_settingsService.Settings.LastOpenedFolder))
    {
      await _folderTree.NavigateToPathCommand.ExecuteAsync(_settingsService.Settings.LastOpenedFolder);
    }

    StatusMessage = "Pronto";
  }

  [RelayCommand]
  private void OpenSettings()
  {
    OpenSettingsRequested?.Invoke(this, EventArgs.Empty);
  }

  [RelayCommand]
  private void OpenPrintHistory()
  {
    OpenPrintHistoryRequested?.Invoke(this, EventArgs.Empty);
  }

  // REMOVIDO: Nao aceita mais drop de arquivos de fora para dentro
  /*
  [RelayCommand]
  private void DropFiles(string[] paths)
  {
    if (paths == null || paths.Length == 0) return;
    FileList.DropFilesCommand.Execute(paths);
  }
  */

  [RelayCommand]
  private async Task CloseSettingsAsync(bool save)
  {
    if (save)
    {
      await Settings.SaveCommand.ExecuteAsync(null);
    }

    IsSettingsOpen = false;
  }

  [RelayCommand]
  private async Task NavigateToPathAsync(string path)
  {
    if (string.IsNullOrWhiteSpace(path)) return;

    await _folderTree.NavigateToPathCommand.ExecuteAsync(path);
  }

  [RelayCommand]
  private void PrintCurrentFile()
  {
    if (PdfViewer.CurrentFile != null)
    {
      PrintQueue.AddToQueueCommand.Execute(new[] { PdfViewer.CurrentFile });
    }
  }

  [RelayCommand]
  private void PrintSelectedFiles()
  {
    var selectedFiles = FileList.GetSelectedFiles().ToList();

    if (selectedFiles.Count > 0)
    {
      PrintQueue.AddToQueueCommand.Execute(selectedFiles);
    }
    else if (PdfViewer.CurrentFile != null)
    {
      PrintQueue.AddToQueueCommand.Execute(new[] { PdfViewer.CurrentFile });
    }
  }

  [RelayCommand]
  private async Task RefreshAsync()
  {
    await FileList.RefreshCommand.ExecuteAsync(null);
    StatusMessage = $"Atualizado - {FileList.FileCount} arquivos";
  }

  [RelayCommand]
  private void ToggleTreeView()
  {
    IsTreeViewVisible = !IsTreeViewVisible;
  }

  [RelayCommand]
  private async Task SaveLayoutAsync()
  {
    _settingsService.Settings.TreeViewWidth = TreeViewWidth;
    _settingsService.Settings.FileListWidth = FileListWidth;
    await _settingsService.SaveAsync();
  }

  private async void OnFolderSelected(object? sender, NetworkFolder folder)
  {
    StatusMessage = $"Carregando {folder.Name}...";
    await FileList.LoadFilesCommand.ExecuteAsync(folder.FullPath);
    StatusMessage = $"{folder.Name} - {FileList.FileCount} arquivos PDF";
  }

  private void OnFileSelected(object? sender, PdfFileInfo file)
  {
    PdfViewer.LoadFileCommand.Execute(file);
    StatusMessage = file.FileName;
  }

  private void OnFilesSelectedForPrint(object? sender, IEnumerable<PdfFileInfo> files)
  {
    PrintQueue.AddToQueueCommand.Execute(files);
    StatusMessage = $"{files.Count()} arquivos adicionados a fila de impressao";
  }
}
