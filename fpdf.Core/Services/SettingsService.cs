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
            Settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
          }
        }
        catch
        {
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
          var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
          File.WriteAllText(_settingsPath, json);
        }
        catch
        {
          // Falha silenciosa no save
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
