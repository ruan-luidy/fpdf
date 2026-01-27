using fpdf.Core.Models;
using fpdf.Core.Native;
using System.IO;
using System.Runtime.InteropServices;

namespace fpdf.Core.Services;

public class NetworkService : INetworkService
{
  private readonly ISettingsService _settingsService;
  private readonly ILocalizationService _localizationService;

  public NetworkService(ISettingsService settingsService, ILocalizationService localizationService)
  {
    _settingsService = settingsService;
    _localizationService = localizationService;
  }

  public async Task<List<NetworkFolder>> EnumerateNetworkAsync(CancellationToken cancellationToken = default)
  {
    return await Task.Run(() =>
    {
      var folders = new List<NetworkFolder>();
      try
      {
        EnumerateNetworkResources(null, folders, cancellationToken);
      }
      catch
      {
        // Rede indisponivel - retorna vazio
      }
      return folders;
    }, cancellationToken);
  }

  private static void EnumerateNetworkResources(
    WNetInterop.NETRESOURCE? root,
    List<NetworkFolder> folders,
    CancellationToken cancellationToken)
  {
    int result = WNetInterop.WNetOpenEnum(
      WNetInterop.RESOURCE_GLOBALNET,
      WNetInterop.RESOURCETYPE_DISK,
      0,
      root,
      out IntPtr hEnum);

    if (result != WNetInterop.NO_ERROR)
      return;

    try
    {
      int bufferSize = 16384;
      IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

      try
      {
        while (!cancellationToken.IsCancellationRequested)
        {
          int count = -1;
          int size = bufferSize;
          result = WNetInterop.WNetEnumResource(hEnum, ref count, buffer, ref size);

          if (result == WNetInterop.ERROR_NO_MORE_ITEMS)
            break;

          if (result != WNetInterop.NO_ERROR)
            break;

          int entrySize = Marshal.SizeOf<WNetInterop.NETRESOURCE>();
          for (int i = 0; i < count; i++)
          {
            cancellationToken.ThrowIfCancellationRequested();

            IntPtr ptr = buffer + i * entrySize;
            var nr = Marshal.PtrToStructure<WNetInterop.NETRESOURCE>(ptr)!;

            string name = nr.lpRemoteName ?? nr.lpComment ?? "Unknown";
            string displayName = name;

            // Para shares, mostra apenas o nome do share
            if (nr.dwDisplayType == WNetInterop.RESOURCEDISPLAYTYPE_SHARE && name.Contains('\\'))
            {
              displayName = name.Split('\\', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? name;
            }
            // Para servers, remove as barras
            else if (nr.dwDisplayType == WNetInterop.RESOURCEDISPLAYTYPE_SERVER)
            {
              displayName = name.TrimStart('\\');
            }

            bool isContainer = (nr.dwUsage & WNetInterop.RESOURCEUSAGE_CONTAINER) != 0;
            bool isShare = nr.dwDisplayType == WNetInterop.RESOURCEDISPLAYTYPE_SHARE;

            string iconKind = nr.dwDisplayType switch
            {
              WNetInterop.RESOURCEDISPLAYTYPE_DOMAIN => "Globe",
              WNetInterop.RESOURCEDISPLAYTYPE_SERVER => "Desktop",
              WNetInterop.RESOURCEDISPLAYTYPE_SHARE => "Folder",
              _ => "Network"
            };

            var folder = new NetworkFolder
            {
              Name = displayName,
              FullPath = name,
              IconKind = iconKind
            };

            // Shares e containers podem ter filhos
            if (isContainer || isShare)
            {
              folder.AddDummyChild();
            }

            folders.Add(folder);
          }
        }
      }
      finally
      {
        Marshal.FreeHGlobal(buffer);
      }
    }
    finally
    {
      WNetInterop.WNetCloseEnum(hEnum);
    }
  }

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
          // Enumera via WNet API
          EnumerateNetworkResources(null, folders, cancellationToken);
          return folders;
        }

        // Verifica se eh um recurso de rede (workgroup/server) que precisa de WNet
        if (path.StartsWith(@"\\") && !Directory.Exists(path))
        {
          // Tenta enumerar como recurso de rede
          var nr = new WNetInterop.NETRESOURCE
          {
            dwScope = WNetInterop.RESOURCE_GLOBALNET,
            dwType = WNetInterop.RESOURCETYPE_DISK,
            dwUsage = WNetInterop.RESOURCEUSAGE_CONTAINER,
            lpRemoteName = path
          };
          EnumerateNetworkResources(nr, folders, cancellationToken);
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
        var extensions = _settingsService.Settings.SupportedFileExtensions;
        var searchOption = _settingsService.Settings.RecursiveSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        foreach (var extension in extensions)
        {
          var pattern = $"*{extension}";
          var matchingFiles = dirInfo.GetFiles(pattern, searchOption)
              .OrderBy(f => f.Name);

          foreach (var file in matchingFiles)
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

      // Adiciona caminhos de rede customizados das configuracoes
      foreach (var customNetworkPath in _settingsService.Settings.CustomNetworkPaths)
      {
        try
        {
          if (Directory.Exists(customNetworkPath))
          {
            var dirInfo = new DirectoryInfo(customNetworkPath);
            var customFolder = new NetworkFolder
            {
              Name = $"{dirInfo.Name} ({GetServerName(customNetworkPath)})",
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
      }

      // Adiciona drives locais
      foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
      {
        cancellationToken.ThrowIfCancellationRequested();

        var volumeLabel = string.IsNullOrEmpty(drive.VolumeLabel) 
          ? _localizationService.GetString("Network_LocalDisk")
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

  private static string GetServerName(string path)
  {
    if (path.StartsWith(@"\\"))
    {
      var parts = path.TrimStart('\\').Split('\\');
      if (parts.Length > 0)
        return parts[0];
    }
    return "Rede";
  }
}
