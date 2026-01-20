using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fpdf.Core.Models;
using fpdf.Core.Services;

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
    IsSettingsOpen = true;
    _ = Settings.LoadCommand.ExecuteAsync(null);
  }

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
    _ = PdfViewer.LoadFileCommand.ExecuteAsync(file);
    StatusMessage = $"{file.FileName} - {file.PageCount} paginas";
  }

  private void OnFilesSelectedForPrint(object? sender, IEnumerable<PdfFileInfo> files)
  {
    PrintQueue.AddToQueueCommand.Execute(files);
    StatusMessage = $"{files.Count()} arquivos adicionados a fila de impressao";
  }
}
