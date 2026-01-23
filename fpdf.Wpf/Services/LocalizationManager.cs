using System.Globalization;
using System.Resources;
using fpdf.Core.Services;

namespace fpdf.Wpf.Services;

public class LocalizationManager : ILocalizationService
{
    private static LocalizationManager? _instance;
    private readonly ResourceManager _resourceManager;
    
    public static LocalizationManager Instance => _instance ??= new LocalizationManager();

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


