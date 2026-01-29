using fpdf.Core.Models;
using Newtonsoft.Json;
using System.IO;

namespace fpdf.Core.Services;

public class SettingsService : ISettingsService
{
  private readonly string _settingsPath;
  private readonly object _lock = new();

  public AppSettings Settings { get; private set; } = new();

  public SettingsService()
  {
    var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var appFolder = Path.Combine(appDataPath, "NetworkPdfManager");
    Directory.CreateDirectory(appFolder);
    _settingsPath = Path.Combine(appFolder, "settings.json");
  }

  public async Task LoadAsync(CancellationToken cancellationToken = default)
  {
    await Task.Run(() =>
    {
      lock (_lock)
      {
        try
        {
          if (File.Exists(_settingsPath))
          {
            var json = File.ReadAllText(_settingsPath);
            var serializerSettings = new JsonSerializerSettings
            {
              ObjectCreationHandling = ObjectCreationHandling.Replace
            };
            Settings = JsonConvert.DeserializeObject<AppSettings>(json, serializerSettings) ?? new AppSettings();
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Loaded from {_settingsPath}");
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Language: {Settings.Language}, Printer: {Settings.DefaultPrinter}");
          }
          else
          {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] No settings file found at {_settingsPath}, using defaults");
            Settings = new AppSettings();
          }
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine($"[SettingsService] Load FAILED: {ex.Message}");
          Settings = new AppSettings();
        }
      }
    }, cancellationToken);
  }

  public async Task SaveAsync(CancellationToken cancellationToken = default)
  {
    await Task.Run(() =>
    {
      lock (_lock)
      {
        try
        {
          // Garante que o diretorio existe
          var directory = Path.GetDirectoryName(_settingsPath);
          if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
          {
            Directory.CreateDirectory(directory);
          }

          var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
          File.WriteAllText(_settingsPath, json);

          System.Diagnostics.Debug.WriteLine($"[SettingsService] Saved to {_settingsPath}");
          System.Diagnostics.Debug.WriteLine($"[SettingsService] Language: {Settings.Language}, Printer: {Settings.DefaultPrinter}");
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine($"[SettingsService] Save FAILED: {ex.Message}");
        }
      }
    }, cancellationToken);
  }

  public void AddRecentFolder(string path)
  {
    if (string.IsNullOrWhiteSpace(path)) return;

    lock (_lock)
    {
      Settings.RecentFolders.Remove(path);
      Settings.RecentFolders.Insert(0, path);

      while (Settings.RecentFolders.Count > Settings.MaxRecentFolders)
      {
        Settings.RecentFolders.RemoveAt(Settings.RecentFolders.Count - 1);
      }

      Settings.LastOpenedFolder = path;
    }

    _ = SaveAsync();
  }

  public void AddFavoriteFolder(string path)
  {
    if (string.IsNullOrWhiteSpace(path)) return;

    lock (_lock)
    {
      if (!Settings.FavoriteFolders.Contains(path))
      {
        Settings.FavoriteFolders.Add(path);
      }
    }

    _ = SaveAsync();
  }

  public void RemoveFavoriteFolder(string path)
  {
    lock (_lock)
    {
      Settings.FavoriteFolders.Remove(path);
    }

    _ = SaveAsync();
  }

  public bool IsFavorite(string path)
  {
    lock (_lock)
    {
      return Settings.FavoriteFolders.Contains(path);
    }
  }
}
