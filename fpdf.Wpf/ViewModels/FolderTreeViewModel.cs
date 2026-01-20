using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fpdf.Core.Models;
using fpdf.Core.Services;

namespace fpdf.Wpf.ViewModels;

public partial class FolderTreeViewModel : ObservableObject
{
  private readonly INetworkService _networkService;
  private readonly ISettingsService _settingsService;

  [ObservableProperty]
  private bool _isLoading;

  [ObservableProperty]
  private NetworkFolder? _selectedFolder;

  [ObservableProperty]
  private string _rootPath = string.Empty;

  public ObservableCollection<NetworkFolder> RootFolders { get; } = new();
  public ObservableCollection<NetworkFolder> FavoriteFolders { get; } = new();

  public event EventHandler<NetworkFolder>? FolderSelected;

  public FolderTreeViewModel(INetworkService networkService, ISettingsService settingsService)
  {
    _networkService = networkService;
    _settingsService = settingsService;
  }

  [RelayCommand]
  private async Task LoadRootsAsync()
  {
    IsLoading = true;

    try
    {
      RootFolders.Clear();
      var roots = await _networkService.GetNetworkRootsAsync();

      foreach (var root in roots)
      {
        RootFolders.Add(root);
      }

      // Carrega favoritos
      await LoadFavoritesAsync();
    }
    finally
    {
      IsLoading = false;
    }
  }

  [RelayCommand]
  private async Task NavigateToPathAsync(string path)
  {
    if (string.IsNullOrWhiteSpace(path)) return;

    IsLoading = true;

    try
    {
      if (!await _networkService.FolderExistsAsync(path)) return;

      // Cria folder e seleciona
      var folder = new NetworkFolder
      {
        Name = Path.GetFileName(path) ?? path,
        FullPath = path
      };

      if (await _networkService.HasSubfoldersAsync(path))
      {
        folder.AddDummyChild();
      }

      SelectedFolder = folder;
      OnFolderSelected(folder);

      _settingsService.AddRecentFolder(path);
    }
    finally
    {
      IsLoading = false;
    }
  }

  [RelayCommand]
  private async Task ExpandFolderAsync(NetworkFolder folder)
  {
    if (folder.HasLoadedChildren || folder.IsLoading) return;

    folder.IsLoading = true;

    try
    {
      folder.ClearDummyChild();

      var subfolders = await _networkService.GetSubfoldersAsync(folder.FullPath);

      foreach (var subfolder in subfolders)
      {
        subfolder.Parent = folder;
        subfolder.IsFavorite = _settingsService.IsFavorite(subfolder.FullPath);
        folder.SubFolders.Add(subfolder);
      }

      folder.HasLoadedChildren = true;
    }
    finally
    {
      folder.IsLoading = false;
    }
  }

  [RelayCommand]
  private void SelectFolder(NetworkFolder folder)
  {
    if (folder == null) return;

    SelectedFolder = folder;
    OnFolderSelected(folder);

    _settingsService.AddRecentFolder(folder.FullPath);
  }

  [RelayCommand]
  private async Task ToggleFavoriteAsync(NetworkFolder folder)
  {
    if (folder == null) return;

    if (folder.IsFavorite)
    {
      _settingsService.RemoveFavoriteFolder(folder.FullPath);
      folder.IsFavorite = false;
      FavoriteFolders.Remove(folder);
    }
    else
    {
      _settingsService.AddFavoriteFolder(folder.FullPath);
      folder.IsFavorite = true;
      FavoriteFolders.Add(folder);
    }

    await _settingsService.SaveAsync();
  }

  [RelayCommand]
  private void RefreshFolder(NetworkFolder folder)
  {
    if (folder == null) return;

    folder.HasLoadedChildren = false;
    folder.SubFolders.Clear();
    folder.AddDummyChild();
  }

  private async Task LoadFavoritesAsync()
  {
    FavoriteFolders.Clear();

    foreach (var path in _settingsService.Settings.FavoriteFolders)
    {
      if (await _networkService.FolderExistsAsync(path))
      {
        var folder = new NetworkFolder
        {
          Name = Path.GetFileName(path) ?? path,
          FullPath = path,
          IsFavorite = true
        };

        if (await _networkService.HasSubfoldersAsync(path))
        {
          folder.AddDummyChild();
        }

        FavoriteFolders.Add(folder);
      }
    }
  }

  protected virtual void OnFolderSelected(NetworkFolder folder)
  {
    FolderSelected?.Invoke(this, folder);
  }
}
