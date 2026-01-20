using fpdf.Core.Models;
using System.IO;

namespace fpdf.Core.Services;

public class NetworkService : INetworkService
{
  public async Task<List<NetworkFolder>> GetSubfoldersAsync(string path, CancellationToken cancellationToken = default)
  {
    return await Task.Run(() =>
    {
      var folders = new List<NetworkFolder>();

      try
      {
        // Caso especial: raiz da rede \\
        if (path == "\\\\")
        {
          // Lista computadores da rede (workgroup/domain)
          // Nota: Isso pode ser lento dependendo da rede
          // Por enquanto, retorna vazio e o usuário deve digitar o caminho UNC manualmente
          return folders;
        }

        var dirInfo = new DirectoryInfo(path);
        var directories = dirInfo.GetDirectories()
            .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden))
            .OrderBy(d => d.Name);

        foreach (var dir in directories)
        {
          cancellationToken.ThrowIfCancellationRequested();

          var folder = new NetworkFolder
          {
            Name = dir.Name,
            FullPath = dir.FullName
          };

          // Verifica se tem subpastas para mostrar seta de expansao
          try
          {
            if (dir.GetDirectories().Any(d => !d.Attributes.HasFlag(FileAttributes.Hidden)))
            {
              folder.AddDummyChild();
            }
          }
          catch
          {
            // Sem acesso - nao adiciona dummy
          }

          folders.Add(folder);
        }
      }
      catch (UnauthorizedAccessException)
      {
        // Sem permissao - retorna vazio
      }
      catch (DirectoryNotFoundException)
      {
        // Pasta nao existe
      }
      catch (IOException)
      {
        // Erro de rede
      }

      return folders;
    }, cancellationToken);
  }

  public async Task<List<PdfFileInfo>> GetPdfFilesAsync(string path, CancellationToken cancellationToken = default)
  {
    return await Task.Run(() =>
    {
      var files = new List<PdfFileInfo>();

      try
      {
        var dirInfo = new DirectoryInfo(path);
        var pdfFiles = dirInfo.GetFiles("*.pdf", SearchOption.TopDirectoryOnly)
            .OrderBy(f => f.Name);

        foreach (var file in pdfFiles)
        {
          cancellationToken.ThrowIfCancellationRequested();

          files.Add(new PdfFileInfo
          {
            FileName = file.Name,
            FullPath = file.FullName,
            FileSize = file.Length,
            LastModified = file.LastWriteTime
          });
        }
      }
      catch (UnauthorizedAccessException)
      {
        // Sem permissao
      }
      catch (DirectoryNotFoundException)
      {
        // Pasta nao existe
      }
      catch (IOException)
      {
        // Erro de rede
      }

      return files;
    }, cancellationToken);
  }

  public async Task<bool> FolderExistsAsync(string path, CancellationToken cancellationToken = default)
  {
    return await Task.Run(() =>
    {
      try
      {
        return Directory.Exists(path);
      }
      catch
      {
        return false;
      }
    }, cancellationToken);
  }

  public async Task<bool> HasSubfoldersAsync(string path, CancellationToken cancellationToken = default)
  {
    return await Task.Run(() =>
    {
      try
      {
        var dirInfo = new DirectoryInfo(path);
        return dirInfo.GetDirectories().Any(d => !d.Attributes.HasFlag(FileAttributes.Hidden));
      }
      catch
      {
        return false;
      }
    }, cancellationToken);
  }

  public async Task<List<NetworkFolder>> GetNetworkRootsAsync(CancellationToken cancellationToken = default)
  {
    return await Task.Run(() =>
    {
      var roots = new List<NetworkFolder>();

      // Adiciona caminho de rede customizado - GERENCIAMENTO DE PROJETOS
      try
      {
        var customNetworkPath = @"\\clbrfs\Operational\GERENCIAMENTO DE PROJETOS\";
        if (Directory.Exists(customNetworkPath))
        {
          var customFolder = new NetworkFolder
          {
            Name = "GERENCIAMENTO DE PROJETOS (clbrfs)",
            FullPath = customNetworkPath
          };

          if (Directory.GetDirectories(customNetworkPath).Any())
          {
            customFolder.AddDummyChild();
          }

          roots.Add(customFolder);
        }
      }
      catch
      {
        // Caminho nao disponivel - ignora
      }

      // Adiciona drives locais
      foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
      {
        cancellationToken.ThrowIfCancellationRequested();

        var volumeLabel = string.IsNullOrEmpty(drive.VolumeLabel) 
          ? "Disco Local" 
          : drive.VolumeLabel;

        var folder = new NetworkFolder
        {
          Name = $"{drive.Name.TrimEnd('\\')} ({volumeLabel})",
          FullPath = drive.Name
        };

        try
        {
          if (Directory.GetDirectories(drive.Name).Any())
          {
            folder.AddDummyChild();
          }
        }
        catch
        {
          // Sem acesso
        }

        roots.Add(folder);
      }

      return roots;
    }, cancellationToken);
  }
}
