using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using fpdf.Wpf.Services;

namespace fpdf.Wpf.Extensions;

[MarkupExtensionReturnType(typeof(object))]
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

        // Verifica se temos informacao sobre o alvo do binding
        var targetProvider = serviceProvider?.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;

        // Se nao temos informacao do alvo ou o alvo nao suporta binding, retorna valor estatico
        if (targetProvider?.TargetObject == null || targetProvider.TargetObject is not DependencyObject)
        {
            return LocalizationManager.Instance.GetString(Key);
        }

        // Se o alvo e um Setter (em Style), retorna o binding diretamente
        if (targetProvider.TargetObject.GetType().Name == "SharedDp")
        {
            return new Binding($"[{Key}]")
            {
                Source = LocalizationManager.Instance,
                Mode = BindingMode.OneWay
            };
        }

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
