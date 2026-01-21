using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fpdf.Core.Models;
using fpdf.Core.Services;

namespace fpdf.Wpf.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
  private readonly ISettingsService _settingsService;
  private readonly IPrintService _printService;
  private readonly INetworkService _networkService;

  [ObservableProperty]
  private string? _defaultPrinter;

  [ObservableProperty]
  private string _theme = "Light";

  [ObservableProperty]
  private bool _showThumbnails = true;

  [ObservableProperty]
  private int _thumbnailSize = 64;

  [ObservableProperty]
  private bool _rememberLastFolder = true;

  [ObservableProperty]
  private int _defaultCopies = 1;

  [ObservableProperty]
  private bool _defaultDuplex;

  [ObservableProperty]
  private string _newNetworkPath = string.Empty;

  public ObservableCollection<string> FavoriteFolders { get; } = new();
  public ObservableCollection<string> RecentFolders { get; } = new();
  public ObservableCollection<string> CustomNetworkPaths { get; } = new();
  public ObservableCollection<PrinterInfo> Printers { get; } = new();
  public ObservableCollection<string> AvailableThemes { get; } = new() { "Light", "Dark", "Violet" };

  public SettingsViewModel(ISettingsService settingsService, IPrintService printService, INetworkService networkService)
  {
    _settingsService = settingsService;
    _printService = printService;
    _networkService = networkService;
  }

  [RelayCommand]
  private async Task LoadAsync()
  {
    var settings = _settingsService.Settings;

    DefaultPrinter = settings.DefaultPrinter;
    Theme = settings.Theme;
    ShowThumbnails = settings.ShowThumbnails;
    ThumbnailSize = settings.ThumbnailSize;
    RememberLastFolder = settings.RememberLastFolder;
    DefaultCopies = settings.DefaultCopies;
    DefaultDuplex = settings.DefaultDuplex;

    FavoriteFolders.Clear();
    foreach (var folder in settings.FavoriteFolders)
    {
      FavoriteFolders.Add(folder);
    }

    RecentFolders.Clear();
    foreach (var folder in settings.RecentFolders)
    {
      RecentFolders.Add(folder);
    }

    CustomNetworkPaths.Clear();
    foreach (var path in settings.CustomNetworkPaths)
    {
      CustomNetworkPaths.Add(path);
    }

    Printers.Clear();
    var printers = await _printService.GetPrintersAsync();
    foreach (var printer in printers)
    {
      Printers.Add(printer);
    }
  }

  [RelayCommand]
  private async Task SaveAsync()
  {
    var settings = _settingsService.Settings;

    settings.DefaultPrinter = DefaultPrinter;
    settings.Theme = Theme;
    settings.ShowThumbnails = ShowThumbnails;
    settings.ThumbnailSize = ThumbnailSize;
    settings.RememberLastFolder = RememberLastFolder;
    settings.DefaultCopies = DefaultCopies;
    settings.DefaultDuplex = DefaultDuplex;

    settings.CustomNetworkPaths.Clear();
    foreach (var path in CustomNetworkPaths)
    {
      settings.CustomNetworkPaths.Add(path);
    }

    await _settingsService.SaveAsync();
  }

  [RelayCommand]
  private async Task RemoveFavoriteFolderAsync(string folder)
  {
    _settingsService.RemoveFavoriteFolder(folder);
    FavoriteFolders.Remove(folder);
    await _settingsService.SaveAsync();
  }

  [RelayCommand]
  private void ClearRecentFolders()
  {
    _settingsService.Settings.RecentFolders.Clear();
    RecentFolders.Clear();
    _ = _settingsService.SaveAsync();
  }

  [RelayCommand]
  private async Task AddNetworkPathAsync()
  {
    if (string.IsNullOrWhiteSpace(NewNetworkPath)) return;

    var path = NewNetworkPath.Trim();

    if (!await _networkService.FolderExistsAsync(path))
    {
      return;
    }

    if (!CustomNetworkPaths.Contains(path))
    {
      CustomNetworkPaths.Add(path);
      _settingsService.Settings.CustomNetworkPaths.Add(path);
      await _settingsService.SaveAsync();
    }

    NewNetworkPath = string.Empty;
  }

  [RelayCommand]
  private async Task RemoveNetworkPathAsync(string path)
  {
    CustomNetworkPaths.Remove(path);
    _settingsService.Settings.CustomNetworkPaths.Remove(path);
    await _settingsService.SaveAsync();
  }

  partial void OnThemeChanged(string value)
  {
    // Aplicar tema imediatamente
    ApplyTheme(value);
  }

  private void ApplyTheme(string themeName)
  {
    // Implementar mudanca de tema do HandyControl
    // ResourceDictionary theme = themeName switch...
  }
}
