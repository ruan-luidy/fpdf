using System.Windows.Data;
using System.Windows.Markup;
using fpdf.Wpf.Services;

namespace fpdf.Wpf.Extensions;

[MarkupExtensionReturnType(typeof(BindingExpression))]
[ContentProperty(nameof(Key))]
public class LocExtension : MarkupExtension
{
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

        // Cria um Binding para o indexador do LocalizationManager
        // Quando o idioma muda, PropertyChanged("Item[]") notifica e a UI atualiza
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationManager.Instance,
            Mode = BindingMode.OneWay
        };

        return binding.ProvideValue(serviceProvider);
    }
}
