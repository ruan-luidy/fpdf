using System.Windows;
using System.Windows.Controls;

namespace fpdf.Wpf.Views.Controls
{
  public partial class SettingsItem : UserControl
  {
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(SettingsItem));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsItem));

    public static readonly DependencyProperty ItemContentProperty =
        DependencyProperty.Register(nameof(ItemContent), typeof(object), typeof(SettingsItem));

    public string Label
    {
      get => (string)GetValue(LabelProperty);
      set => SetValue(LabelProperty, value);
    }

    public string Description
    {
      get => (string)GetValue(DescriptionProperty);
      set => SetValue(DescriptionProperty, value);
    }

    public object ItemContent
    {
      get => GetValue(ItemContentProperty);
      set => SetValue(ItemContentProperty, value);
    }

    public SettingsItem()
    {
      InitializeComponent();
    }
  }
}
