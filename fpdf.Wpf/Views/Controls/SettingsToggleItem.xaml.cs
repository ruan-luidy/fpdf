using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using fpdf.Wpf.Services;

namespace fpdf.Wpf.Views.Controls
{
  public partial class SettingsToggleItem : UserControl
  {
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(SettingsToggleItem),
            new FrameworkPropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingsToggleItem),
            new FrameworkPropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(SettingsToggleItem),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

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

    public bool IsChecked
    {
      get => (bool)GetValue(IsCheckedProperty);
      set => SetValue(IsCheckedProperty, value);
    }

    public SettingsToggleItem()
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
