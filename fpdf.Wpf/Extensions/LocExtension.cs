using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace fpdf.Wpf.Extensions;

[MarkupExtensionReturnType(typeof(BindingExpression))]
[ContentProperty(nameof(Key))]
public class LocExtension : MarkupExtension
{
  private static ResourceManager? _resourceManager;

  public string Key { get; set; } = string.Empty;

  public LocExtension()
  {
  }

  public LocExtension(string key)
  {
    Key = key;
  }

  public override object ProvideValue(IServiceProvider serviceProvider)
  {
    if (string.IsNullOrEmpty(Key))
      return "[No Key]";

    _resourceManager ??= new ResourceManager("fpdf.Wpf.Resources.Strings", typeof(LocExtension).Assembly);

    try
    {
      var value = _resourceManager.GetString(Key, CultureInfo.CurrentUICulture);
      return value ?? $"[{Key}]";
    }
    catch
    {
      return $"[{Key}]";
    }
  }
}
