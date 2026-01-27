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

  [ObservableProperty]
  private bool _recursiveSearch;

  [ObservableProperty]
  private string _supportedExtensions = string.Empty;

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
      // IMPORTANTE: Recarrega as configuracoes do disco
      // Isso garante que sempre tenhamos os valores mais recentes salvos
      await _settingsService.LoadAsync();
      
      var settings = _settingsService.Settings;

      // Guarda o idioma salvo nas configuracoes para restaurar se cancelar
      // Usa settings.Language que eh o valor persistido
      _originalLanguage = settings.Language ?? "pt-BR";
      System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Original language set to: {_originalLanguage}");

      Theme = settings.Theme;
      ShowThumbnails = settings.ShowThumbnails;
      ThumbnailSize = settings.ThumbnailSize;
      RememberLastFolder = settings.RememberLastFolder;
      DefaultCopies = settings.DefaultCopies;
      DefaultDuplex = settings.DefaultDuplex;
      RecursiveSearch = settings.RecursiveSearch;
      SupportedExtensions = string.Join(", ", settings.SupportedFileExtensions);

      // Carrega o idioma das configuracoes salvas (nao do LocalizationManager atual)
      // Isso garante que ao reabrir o dialog, sempre mostre o idioma salvo
      var savedLang = settings.Language ?? "pt-BR";
      System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Saved language from settings: {savedLang}");
      SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == savedLang)
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

      // Carrega impressoras PRIMEIRO
      Printers.Clear();
      var printers = await _printService.GetPrintersAsync();
      System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Loaded {printers.Count} printers");
      foreach (var printer in printers)
      {
        Printers.Add(printer);
        System.Diagnostics.Debug.WriteLine($"[SettingsViewModel]   - {printer.Name}");
      }

      // IMPORTANTE: Define a impressora padrao DEPOIS de carregar a lista
      // Isso garante que o binding do ComboBox funcione corretamente
      var savedPrinter = settings.DefaultPrinter;
      System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Setting DefaultPrinter to: {savedPrinter}");
      
      // Se nao houver impressora salva ou ela nao existir mais, usa a primeira disponivel
      if (string.IsNullOrEmpty(savedPrinter) || !Printers.Any(p => p.Name == savedPrinter))
      {
        savedPrinter = Printers.FirstOrDefault()?.Name;
        System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Printer not found or empty, using first: {savedPrinter}");
      }
      
      DefaultPrinter = savedPrinter;
      System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] DefaultPrinter is now: {DefaultPrinter}");
    }
    finally
    {
      _isLoading = false;
      System.Diagnostics.Debug.WriteLine("[SettingsViewModel] LoadAsync completed");
      
      // IMPORTANTE: Garante que o idioma salvo seja aplicado ao abrir o dialog
      // Isso evita que o dialog abra com idioma diferente do salvo
      if (!string.IsNullOrEmpty(_originalLanguage))
      {
        System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Applying original language: {_originalLanguage}");
        _localizationManager.SetLanguage(_originalLanguage);
      }
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
    settings.RecursiveSearch = RecursiveSearch;

    // Parse supported extensions
    var extensions = SupportedExtensions
        .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(e => e.Trim())
        .Where(e => !string.IsNullOrWhiteSpace(e))
        .Select(e => e.StartsWith(".") ? e : "." + e)
        .Distinct()
        .ToList();

    if (extensions.Count > 0)
    {
      settings.SupportedFileExtensions = extensions;
    }

    System.Diagnostics.Debug.WriteLine($"[SettingsViewModel] Saving - Language: {settings.Language}, Printer: {settings.DefaultPrinter}");

    settings.CustomNetworkPaths.Clear();
    foreach (var path in CustomNetworkPaths)
    {
      settings.CustomNetworkPaths.Add(path);
    }

    await _settingsService.SaveAsync();

    // Atualiza o idioma original para o novo valor salvo
    _originalLanguage = settings.Language;
    
    // IMPORTANTE: Aplica o idioma salvo no LocalizationManager
    // Isso garante que o idioma salvo seja o ativo na aplicacao
    if (!string.IsNullOrEmpty(_originalLanguage))
    {
      _localizationManager.SetLanguage(_originalLanguage);
    }
    
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
