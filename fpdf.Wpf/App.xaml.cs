using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using fpdf.Core.Services;
using fpdf.Wpf.ViewModels;
using fpdf.Wpf.Views;
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

  public static T GetService<T>() where T : class
  {
    var app = (App)Application.Current;
    return app._serviceProvider.GetRequiredService<T>();
  }
}
