namespace fpdf.Core.Models;

public class AppSettings
{
  public string? DefaultPrinter { get; set; }
  public string? LastOpenedFolder { get; set; }
  public List<string> FavoriteFolders { get; set; } = new();
  public List<string> RecentFolders { get; set; } = new();
  public int MaxRecentFolders { get; set; } = 10;
  public string Theme { get; set; } = "Light";
  public string Language { get; set; } = "pt-BR";
  public List<string> CustomNetworkPaths { get; set; } = new();

  public double WindowWidth { get; set; } = 1200;
  public double WindowHeight { get; set; } = 800;
  public double TreeViewWidth { get; set; } = 250;
  public double FileListWidth { get; set; } = 350;
  public bool ShowThumbnails { get; set; } = true;
  public int ThumbnailSize { get; set; } = 64;
  public bool RememberLastFolder { get; set; } = true;
  public int DefaultCopies { get; set; } = 1;
  public bool DefaultDuplex { get; set; }
  
  // Configuracoes da fila de impressao
  public int MaxCompletedJobsInQueue { get; set; } = 100; // Manter apenas os ultimos N jobs completados
  public bool AutoCleanupCompletedJobs { get; set; } = true; // Auto-limpar jobs completados automaticamente
}
