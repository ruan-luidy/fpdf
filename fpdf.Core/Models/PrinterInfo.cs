using CommunityToolkit.Mvvm.ComponentModel;

namespace fpdf.Core.Models;

public partial class PrinterInfo : ObservableObject
{
  [ObservableProperty]
  private string _name = string.Empty;

  [ObservableProperty]
  private string _fullName = string.Empty;

  [ObservableProperty]
  private bool _isDefault;

  [ObservableProperty]
  private bool _isNetwork;

  [ObservableProperty]
  private bool _isOnline = true;

  [ObservableProperty]
  private string _portName = string.Empty;

  [ObservableProperty]
  private string _driverName = string.Empty;

  public string DisplayName => IsDefault ? $"{Name} (Padrao)" : Name;
}
