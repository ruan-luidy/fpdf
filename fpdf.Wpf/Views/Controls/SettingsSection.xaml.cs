using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using fpdf.Wpf.Services;

namespace fpdf.Wpf.Views.Controls
{
  public partial class SettingsSection : UserControl
  {
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SettingsSection),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty SectionContentProperty =
        DependencyProperty.Register(nameof(SectionContent), typeof(object), typeof(SettingsSection));

    public string Title
    {
      get => (string)GetValue(TitleProperty);
      set => SetValue(TitleProperty, value);
    }

    public object SectionContent
    {
      get => GetValue(SectionContentProperty);
      set => SetValue(SectionContentProperty, value);
    }

    public SettingsSection()
    {
      InitializeComponent();

      // Forca atualizacao quando o idioma muda
      LocalizationManager.Instance.PropertyChanged += (_, e) =>
      {
        if (e.PropertyName == "Item[]")
        {
          var binding = BindingOperations.GetBindingExpression(this, TitleProperty);
          binding?.UpdateTarget();
        }
      };
    }
  }
}