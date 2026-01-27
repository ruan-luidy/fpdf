using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using fpdf.Core.Models;
using fpdf.Core.Services;
using fpdf.Wpf.Services;

namespace fpdf.Wpf.ViewModels;

public partial class FolderTreeViewModel : ObservableObject
{
    private readonly INetworkService _networkService;
    private readonly ISettingsService _settingsService;

    private NetworkFolder? _favoritesCategory;
    private NetworkFolder? _thisPcCategory;
    private NetworkFolder? _networkPathsCategory;
    private NetworkFolder? _networkCategory;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private NetworkFolder? _selectedFolder;

    [ObservableProperty]
    private string _rootPath = string.Empty;

    public ObservableCollection<NetworkFolder> RootFolders { get; } = new();

    public event EventHandler<NetworkFolder>? FolderSelected;

    public FolderTreeViewModel(INetworkService networkService, ISettingsService settingsService)
    {
        _networkService = networkService;
        _settingsService = settingsService;

        // Atualiza nomes das categorias quando o idioma muda
        LocalizationManager.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == "Item[]")
            {
                UpdateCategoryNames();
            }
        };
    }

    private void UpdateCategoryNames()
    {
        if (_favoritesCategory != null)
            _favoritesCategory.Name = LocalizationManager.Instance.GetString("Network_Favorites");
        if (_networkPathsCategory != null)
            _networkPathsCategory.Name = LocalizationManager.Instance.GetString("Network_NetworkPaths");
        if (_thisPcCategory != null)
            _thisPcCategory.Name = LocalizationManager.Instance.GetString("Network_ThisPC");
        if (_networkCategory != null)
            _networkCategory.Name = LocalizationManager.Instance.GetString("Network_Network");
    }

    [RelayCommand]
    private async Task LoadRootsAsync()
    {
        IsLoading = true;

        try
        {
            RootFolders.Clear();

            // Categoria: Favoritos
            _favoritesCategory = new NetworkFolder
            {
                Name = LocalizationManager.Instance.GetString("Network_Favorites"),
                IsCategory = true,
                IconKind = "Star",
                IsExpanded = true
            };
            await LoadFavoritesIntoCategory();
            RootFolders.Add(_favoritesCategory);

            // Categoria: Diretorios de Rede
            if (_settingsService.Settings.CustomNetworkPaths.Count > 0)
            {
                _networkPathsCategory = new NetworkFolder
                {
                    Name = LocalizationManager.Instance.GetString("Network_NetworkPaths"),
                    IsCategory = true,
                    IconKind = "Network",
                    IsExpanded = true
                };
                await LoadNetworkPathsIntoCategory();
                RootFolders.Add(_networkPathsCategory);
            }

            // Categoria: Este PC
            _thisPcCategory = new NetworkFolder
            {
                Name = LocalizationManager.Instance.GetString("Network_ThisPC"),
                IsCategory = true,
                IconKind = "Desktop",
                IsExpanded = true
            };

            var roots = await _networkService.GetNetworkRootsAsync();
            foreach (var root in roots)
            {
                root.IconKind = root.FullPath.StartsWith(@"\\") ? "Network" : "HardDrives";
                _thisPcCategory.SubFolders.Add(root);
            }
            _thisPcCategory.HasLoadedChildren = true;

            RootFolders.Add(_thisPcCategory);

            // Categoria: Rede
            _networkCategory = new NetworkFolder
            {
                Name = LocalizationManager.Instance.GetString("Network_Network"),
                IsCategory = true,
                IconKind = "Network",
                IsExpanded = false
            };
            _networkCategory.AddDummyChild();
            RootFolders.Add(_networkCategory);
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

        // Categoria "Rede" - carrega via WNet API
        if (folder == _networkCategory)
        {
            folder.IsLoading = true;
            try
            {
                folder.ClearDummyChild();
                var networkRoots = await _networkService.EnumerateNetworkAsync();
                foreach (var root in networkRoots)
                {
                    root.Parent = folder;
                    folder.SubFolders.Add(root);
                }
                folder.HasLoadedChildren = true;
            }
            finally
            {
                folder.IsLoading = false;
            }
            return;
        }

        if (folder.IsCategory) return;

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
        if (folder == null || folder.IsCategory) return;

        SelectedFolder = folder;
        OnFolderSelected(folder);

        _settingsService.AddRecentFolder(folder.FullPath);
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(NetworkFolder folder)
    {
        if (folder == null || folder.IsCategory) return;

        if (folder.IsFavorite)
        {
            _settingsService.RemoveFavoriteFolder(folder.FullPath);
            folder.IsFavorite = false;

            // Remove da categoria de favoritos
            var toRemove = _favoritesCategory?.SubFolders.FirstOrDefault(f => f.FullPath == folder.FullPath);
            if (toRemove != null)
            {
                _favoritesCategory?.SubFolders.Remove(toRemove);
            }
        }
        else
        {
            _settingsService.AddFavoriteFolder(folder.FullPath);
            folder.IsFavorite = true;

            // Adiciona na categoria de favoritos
            var favoriteFolder = new NetworkFolder
            {
                Name = folder.Name,
                FullPath = folder.FullPath,
                IsFavorite = true,
                IconKind = "Star"
            };

            if (await _networkService.HasSubfoldersAsync(folder.FullPath))
            {
                favoriteFolder.AddDummyChild();
            }

            _favoritesCategory?.SubFolders.Add(favoriteFolder);
        }

        await _settingsService.SaveAsync();
    }

    [RelayCommand]
    private void RefreshFolder(NetworkFolder folder)
    {
        if (folder == null || folder.IsCategory) return;

        folder.HasLoadedChildren = false;
        folder.SubFolders.Clear();
        folder.AddDummyChild();
    }

    public async Task RefreshNetworkPathsCategoryAsync()
    {
        if (_settingsService.Settings.CustomNetworkPaths.Count > 0)
        {
            if (_networkPathsCategory == null)
            {
                _networkPathsCategory = new NetworkFolder
                {
                    Name = LocalizationManager.Instance.GetString("Network_NetworkPaths"),
                    IsCategory = true,
                    IconKind = "Network",
                    IsExpanded = true
                };
                var index = RootFolders.IndexOf(_thisPcCategory!);
                if (index >= 0)
                {
                    RootFolders.Insert(index, _networkPathsCategory);
                }
            }
            await LoadNetworkPathsIntoCategory();
        }
        else if (_networkPathsCategory != null)
        {
            RootFolders.Remove(_networkPathsCategory);
            _networkPathsCategory = null;
        }
    }

    private async Task LoadFavoritesIntoCategory()
    {
        if (_favoritesCategory == null) return;

        _favoritesCategory.SubFolders.Clear();

        foreach (var path in _settingsService.Settings.FavoriteFolders)
        {
            if (await _networkService.FolderExistsAsync(path))
            {
                var folder = new NetworkFolder
                {
                    Name = Path.GetFileName(path.TrimEnd('\\')) ?? path,
                    FullPath = path,
                    IsFavorite = true,
                    IconKind = "Star"
                };

                if (await _networkService.HasSubfoldersAsync(path))
                {
                    folder.AddDummyChild();
                }

                _favoritesCategory.SubFolders.Add(folder);
            }
        }

        _favoritesCategory.HasLoadedChildren = true;
    }

    private async Task LoadNetworkPathsIntoCategory()
    {
        if (_networkPathsCategory == null) return;

        _networkPathsCategory.SubFolders.Clear();

        foreach (var path in _settingsService.Settings.CustomNetworkPaths)
        {
            if (await _networkService.FolderExistsAsync(path))
            {
                var folder = new NetworkFolder
                {
                    Name = Path.GetFileName(path.TrimEnd('\\')) ?? path,
                    FullPath = path,
                    IconKind = "Network"
                };

                if (await _networkService.HasSubfoldersAsync(path))
                {
                    folder.AddDummyChild();
                }

                _networkPathsCategory.SubFolders.Add(folder);
            }
        }

        _networkPathsCategory.HasLoadedChildren = true;
    }

    protected virtual void OnFolderSelected(NetworkFolder folder)
    {
        FolderSelected?.Invoke(this, folder);
    }
}
