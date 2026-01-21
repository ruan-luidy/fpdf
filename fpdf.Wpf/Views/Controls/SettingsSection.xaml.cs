using System.Windows;
using System.Windows.Controls;

namespace fpdf.Wpf.Views.Controls
{
  public partial class SettingsSection : UserControl
  {
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(SettingsSection));

    public static readonly DependencyProperty SectionContentProperty =
        DependencyProperty.Register(nameof(SectionContent), typeof(object), typeof(SettingsSection));

    public string Title
    {
      get => (string)GetValue(TitleProperty);
      set
      {
        SetValue(TitleProperty, value);
        if (SectionTitle != null)
        {
          SectionTitle.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
        }
      }
    }

    public object SectionContent
    {
      get => GetValue(SectionContentProperty);
      set => SetValue(SectionContentProperty, value);
    }

    public SettingsSection()
    {
      InitializeComponent();
      Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      // Update title visibility when loaded
      if (SectionTitle != null)
      {
        SectionTitle.Visibility = string.IsNullOrEmpty(Title) ? Visibility.Collapsed : Visibility.Visible;
      }
    }
  }
}