namespace fpdf.Core.Services;

public interface ILocalizationService
{
    string GetString(string key);
    void SetLanguage(string cultureName);
    string GetCurrentLanguage();
}
