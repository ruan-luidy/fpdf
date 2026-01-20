using fpdf.Core.Models;

namespace fpdf.Core.Services;

public interface ISettingsService
{
  AppSettings Settings { get; }

  Task LoadAsync(CancellationToken cancellationToken = default);

  Task SaveAsync(CancellationToken cancellationToken = default);

  void AddRecentFolder(string path);

  void AddFavoriteFolder(string path);

  void RemoveFavoriteFolder(string path);

  bool IsFavorite(string path);
}
