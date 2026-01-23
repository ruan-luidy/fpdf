using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fpdf.Core.Models;
using fpdf.Core.Services;
using fpdf.Wpf.Services;

namespace fpdf.Wpf.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
  private readonly ISettingsService _settingsService;
  private readonly IPrintService _printService;
  private readonly INetworkService _networkService;
  private readonly LocalizationManager _localizationManager;

  private string? _originalLanguage; // Guarda idioma original para restaurar se cancelar
  private bool _isLoading; // Evita aplicar idioma durante carregamento

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

  [ObservableProperty]
  private LanguageInfo? _selectedLanguage;

  public ObservableCollection<string> FavoriteFolders { get; } = new();
  public ObservableCollection<string> RecentFolders { get; } = new();
  public ObservableCollection<string> CustomNetworkPaths { get; } = new();
  public ObservableCollection<PrinterInfo> Printers { get; } = new();
  public ObservableCollection<string> AvailableThemes { get; } = new() { "Light", "Dark", "Violet" };
  public ObservableCollection<LanguageInfo> AvailableLanguages { get; } = new();

  public SettingsViewModel(ISettingsService settingsService, IPrintService printService, INetworkService networkService, LocalizationManager localizationManager)
  {
    _settingsService = settingsService;
    _printService = printService;
    _networkService = networkService;
    _localizationManager = localizationManager;

    LoadAvailableLanguages();
  }

  [RelayCommand]
  private async Task LoadAsync()
  {
    _isLoading = true;
    System.Diagnostics.Debug.WriteLine("[SettingsViewModel] LoadAsync started");

    try
    {
      var settings = _settingsService.Settings;

      // Guarda o idioma ATUAL (do LocalizationManager) para restaurar se cancelar
      // Nao usa settings.Language porque pode estar desatualizado se houve mudanca sem salvar
      _originalLanguage = _localizationManager.GetCurrentLanguage();
      System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Original language set to: {_originalLanguage}");

      Theme = settings.Theme;
      ShowThumbnails = settings.ShowThumbnails;
      ThumbnailSize = settings.ThumbnailSize;
      RememberLastFolder = settings.RememberLastFolder;
      DefaultCopies = settings.DefaultCopies;
      DefaultDuplex = settings.DefaultDuplex;

      // Carrega o idioma atual do LocalizationManager (nao das configuracoes salvas)
      var currentLang = _localizationManager.GetCurrentLanguage();
      System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Current language from LocalizationManager: {currentLang}");
      SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == currentLang)
                         ?? AvailableLanguages.FirstOrDefault(l => l.Code == "pt-BR");
      System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] SelectedLanguage set to: {SelectedLanguage?.Code}");

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

      // Carrega impressoras PRIMEIRO, depois define a selecionada
      Printers.Clear();
      var printers = await _printService.GetPrintersAsync();
      System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Loaded {printers.Count} printers");
      foreach (var printer in printers)
      {
        Printers.Add(printer);
        System.Diagnostics.Debug.WriteLine($"[SettingsViewModel]   - {printer.Name}");
      }

      // Define a impressora padrao DEPOIS de carregar a lista
      System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Setting DefaultPrinter to: {settings.DefaultPrinter}");
      DefaultPrinter = settings.DefaultPrinter;
      System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] DefaultPrinter is now: {DefaultPrinter}");
    }
    finally
    {
      _isLoading = false;
      System.Diagnostics.Debug.WriteLine("[SettingsViewModel] LoadAsync completed");
    }
  }

  [RelayCommand]
  private async Task SaveAsync()
  {
    System.Diagnostics.Debug.WriteLine("[SettingsViewModel] SaveAsync started");

    var settings = _settingsService.Settings;

    settings.DefaultPrinter = DefaultPrinter;
    settings.Theme = Theme;
    settings.Language = SelectedLanguage?.Code ?? "pt-BR";
    settings.ShowThumbnails = ShowThumbnails;
    settings.ThumbnailSize = ThumbnailSize;
    settings.RememberLastFolder = RememberLastFolder;
    settings.DefaultCopies = DefaultCopies;
    settings.DefaultDuplex = DefaultDuplex;

    System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Saving - Language: {settings.Language}, Printer: {settings.DefaultPrinter}");

    settings.CustomNetworkPaths.Clear();
    foreach (var path in CustomNetworkPaths)
    {
      settings.CustomNetworkPaths.Add(path);
    }

    await _settingsService.SaveAsync();

    // Atualiza o idioma original para o novo valor salvo
    _originalLanguage = settings.Language;
    System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] SaveAsync completed. Original language updated to: {_originalLanguage}");
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

  private void LoadAvailableLanguages()
  {
    var languages = _localizationManager.GetAvailableLanguages();
    foreach (var lang in languages)
    {
      AvailableLanguages.Add(lang);
    }
  }

  partial void OnSelectedLanguageChanged(LanguageInfo? oldValue, LanguageInfo? newValue)
  {
    System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] OnSelectedLanguageChanged: {oldValue?.Code} -> {newValue?.Code}, isLoading={_isLoading}");

    // Nao aplica idioma durante carregamento inicial
    if (_isLoading)
    {
      System.Diagnostics.Debug.WriteLine("[SettingsViewModel] Skipping SetLanguage because isLoading=true");
      return;
    }

    if (newValue != null)
    {
      System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Calling SetLanguage({newValue.Code})");
      _localizationManager.SetLanguage(newValue.Code);
    }
  }

  /// <summary>
  /// Restaura o idioma original se o usuario cancelar
  /// </summary>
  public void RestoreOriginalLanguage()
  {
    System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] RestoreOriginalLanguage called. Original: {_originalLanguage}");
    if (!string.IsNullOrEmpty(_originalLanguage))
    {
      _localizationManager.SetLanguage(_originalLanguage);
    }
  }
}
