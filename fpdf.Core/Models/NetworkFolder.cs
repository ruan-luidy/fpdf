using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace fpdf.Core.Models;

public partial class NetworkFolder : ObservableObject
{
  [ObservableProperty]
  private string _name = string.Empty;

  [ObservableProperty]
  private string _fullPath = string.Empty;

  [ObservableProperty]
  private bool _isExpanded;

  [ObservableProperty]
  private bool _isSelected;

  [ObservableProperty]
  private bool _isFavorite;

  [ObservableProperty]
  private bool _isLoading;

  [ObservableProperty]
  private bool _hasLoadedChildren;

  [ObservableProperty]
  private bool _isCategory;

  [ObservableProperty]
  private string _iconKind = "Folder";

  public ObservableCollection<NetworkFolder> SubFolders { get; } = new();

  public NetworkFolder? Parent { get; set; }

  // Placeholder para lazy loading
  public bool HasDummyChild => SubFolders.Count == 1 && SubFolders[0].Name == "__dummy__";

  public void AddDummyChild()
  {
    SubFolders.Add(new NetworkFolder { Name = "__dummy__" });
  }

  public void ClearDummyChild()
  {
    if (HasDummyChild)
    {
      SubFolders.Clear();
    }
  }
}
