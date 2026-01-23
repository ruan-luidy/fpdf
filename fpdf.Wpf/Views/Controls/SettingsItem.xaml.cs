using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using fpdf.Wpf.Services;

namespace fpdf.Wpf.Views.Controls
{
  public partial class SettingsItem : UserControl
  {
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(SettingsItem),
            new FrameworkPropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsItem),
            new FrameworkPropertyMetadata(string.Empty));

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

      // Forca atualizacao quando o idioma muda
      LocalizationManager.Instance.PropertyChanged += (_, e) =>
      {
        if (e.PropertyName == "Item[]")
        {
          BindingOperations.GetBindingExpression(this, LabelProperty)?.UpdateTarget();
          BindingOperations.GetBindingExpression(this, DescriptionProperty)?.UpdateTarget();
        }
      };
    }
  }
}
