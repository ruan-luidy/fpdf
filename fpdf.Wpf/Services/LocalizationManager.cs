using System.ComponentModel;
using System.Globalization;
using System.Resources;
using fpdf.Core.Services;

namespace fpdf.Wpf.Services;

public class LocalizationManager : ILocalizationService, INotifyPropertyChanged
{
    private static LocalizationManager? _instance;
    private readonly ResourceManager _resourceManager;

    public static LocalizationManager Instance => _instance ??= new LocalizationManager();

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Indexador para binding XAML dinâmico. Uso: {Binding [Key], Source={x:Static services:LocalizationManager.Instance}}
    /// </summary>
    public string this[string key] => GetString(key);

    private LocalizationManager()
    {
        _resourceManager = new ResourceManager("fpdf.Wpf.Resources.Strings", typeof(LocalizationManager).Assembly);
        SetLanguage("pt-BR");
    }

    public void SetLanguage(string cultureName)
    {
        var culture = new CultureInfo(cultureName);

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        // Notifica que TODAS as strings mudaram (o indexador mudou)
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public string GetCurrentLanguage()
    {
        return CultureInfo.CurrentUICulture.Name;
    }

    public string GetString(string key)
    {
        try
        {
            return _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? $"[{key}]";
        }
        catch
        {
            return $"[{key}]";
        }
    }

    public List<LanguageInfo> GetAvailableLanguages()
    {
        return
        [
            new LanguageInfo { Code = "pt-BR", Name = "Português (Brasil)", NativeName = "Português" },
            new LanguageInfo { Code = "en-US", Name = "English (United States)", NativeName = "English" },
            new LanguageInfo { Code = "es-ES", Name = "Español (España)", NativeName = "Español" }
        ];
    }
}

public class LanguageInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
}


