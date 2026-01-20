using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using fpdf.Core.Services;
using fpdf.Wpf.ViewModels;
using fpdf.Wpf.Views.Dialogs;

namespace fpdf.Wpf;

public partial class App : Application
{
  private readonly IServiceProvider _serviceProvider;

  public App()
  {
    var services = new ServiceCollection();
    ConfigureServices(services);
    _serviceProvider = services.BuildServiceProvider();
  }

  private static void ConfigureServices(IServiceCollection services)
  {
    // Services (Singleton)
    services.AddSingleton<INetworkService, NetworkService>();
    services.AddSingleton<IPdfService, PdfService>();
    services.AddSingleton<IPrintService, PrintService>();
    services.AddSingleton<ISettingsService, SettingsService>();

    // ViewModels (Transient)
    services.AddTransient<MainViewModel>();
    services.AddTransient<FolderTreeViewModel>();
    services.AddTransient<FileListViewModel>();
    services.AddTransient<PdfViewerViewModel>();
    services.AddTransient<PrintQueueViewModel>();
    services.AddTransient<SettingsViewModel>();

    // Views
    services.AddTransient<MainWindow>();
    services.AddTransient<SettingsDialog>();
  }

  protected override async void OnStartup(StartupEventArgs e)
  {
    base.OnStartup(e);

    // Carrega configuracoes
    var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
    await settingsService.LoadAsync();

    // Aplica tema
    ApplyTheme(settingsService.Settings.Theme);

    // Cria e exibe a janela principal
    var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
    mainWindow.Show();
  }

  protected override void OnExit(ExitEventArgs e)
  {
    // Dispose de services se necessario
    if (_serviceProvider is IDisposable disposable)
    {
      disposable.Dispose();
    }

    base.OnExit(e);
  }

  private void ApplyTheme(string themeName)
  {
    var skinUri = themeName switch
    {
      "Dark" => new Uri("pack://application:,,,/HandyControl;component/Themes/SkinDark.xaml"),
      "Violet" => new Uri("pack://application:,,,/HandyControl;component/Themes/SkinViolet.xaml"),
      _ => new Uri("pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml")
    };

    // Remove skin atual e aplica novo
    var resources = Application.Current.Resources.MergedDictionaries;

    // Encontra e remove o skin atual
    var currentSkin = resources.FirstOrDefault(r =>
        r.Source?.OriginalString.Contains("Skin") == true);

    if (currentSkin != null)
    {
      resources.Remove(currentSkin);
    }

    // Adiciona novo skin
    resources.Insert(0, new ResourceDictionary { Source = skinUri });
  }

  public static T GetService<T>() where T : class
  {
    var app = (App)Application.Current;
    return app._serviceProvider.GetRequiredService<T>();
  }
}
