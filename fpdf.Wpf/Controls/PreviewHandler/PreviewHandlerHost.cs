using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace fpdf.Wpf.Controls.PreviewHandler;

public class PreviewHandlerHost : HwndHost
{
    private IPreviewHandler? _previewHandler;
    private IntPtr _hwndHost;
    private string? _currentFile;
    private bool _isPreviewLoaded;
    private IStream? _stream;

    private const int WS_CHILD = 0x40000000;
    private const int WS_VISIBLE = 0x10000000;
    private const int HOST_ID = 1;

    public event Action<string>? Error;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        int dwExStyle, string lpClassName, string lpWindowName,
        int dwStyle, int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateStreamOnFileEx(
        string pszFile,
        uint grfMode,
        uint dwAttributes,
        bool fCreate,
        IntPtr pstmTemplate,
        out IStream ppstm);

    public string? FilePath
    {
        get => _currentFile;
        set
        {
            if (_currentFile != value)
            {
                _currentFile = value;
                if (_hwndHost != IntPtr.Zero)
                {
                    Dispatcher.BeginInvoke(new Action(LoadPreview));
                }
            }
        }
    }

    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        _hwndHost = CreateWindowEx(
            0, "static", "",
            WS_CHILD | WS_VISIBLE,
            0, 0, (int)Math.Max(1, ActualWidth), (int)Math.Max(1, ActualHeight),
            hwndParent.Handle,
            (IntPtr)HOST_ID,
            IntPtr.Zero,
            IntPtr.Zero);

        if (!string.IsNullOrEmpty(_currentFile))
        {
            Dispatcher.BeginInvoke(new Action(LoadPreview));
        }

        return new HandleRef(this, _hwndHost);
    }

    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        UnloadPreview();
        DestroyWindow(hwnd.Handle);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdatePreviewRect();
    }

    private void LoadPreview()
    {
        UnloadPreview();

        if (string.IsNullOrEmpty(_currentFile))
        {
            return;
        }

        if (!File.Exists(_currentFile))
        {
            Error?.Invoke($"Arquivo não encontrado: {_currentFile}");
            return;
        }

        var extension = Path.GetExtension(_currentFile).ToLowerInvariant();
        var handlerGuid = GetPreviewHandlerGuid(extension);

        if (handlerGuid == Guid.Empty)
        {
            Error?.Invoke($"Nenhum Preview Handler encontrado para {extension}");
            return;
        }

        try
        {
            var handlerType = Type.GetTypeFromCLSID(handlerGuid, true);
            if (handlerType == null)
            {
                Error?.Invoke("Não foi possível obter o tipo do Preview Handler");
                return;
            }

            var instance = Activator.CreateInstance(handlerType);
            if (instance == null)
            {
                Error?.Invoke("Não foi possível criar instância do Preview Handler");
                return;
            }

            bool initialized = false;

            // Tenta inicializar com Stream primeiro (mais comum)
            if (instance is IInitializeWithStream initWithStream)
            {
                try
                {
                    SHCreateStreamOnFileEx(
                        _currentFile,
                        0x00000000, // STGM_READ
                        0,
                        false,
                        IntPtr.Zero,
                        out _stream);

                    initWithStream.Initialize(_stream, 0);
                    initialized = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"IInitializeWithStream falhou: {ex.Message}");
                }
            }

            // Fallback para IInitializeWithFile
            if (!initialized && instance is IInitializeWithFile initWithFile)
            {
                try
                {
                    initWithFile.Initialize(_currentFile, 0);
                    initialized = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"IInitializeWithFile falhou: {ex.Message}");
                }
            }

            if (!initialized)
            {
                Marshal.ReleaseComObject(instance);
                Error?.Invoke("Preview Handler não suporta inicialização com arquivo");
                return;
            }

            _previewHandler = instance as IPreviewHandler;
            if (_previewHandler == null)
            {
                Marshal.ReleaseComObject(instance);
                Error?.Invoke("Objeto não implementa IPreviewHandler");
                return;
            }

            var rect = new RECT(0, 0, (int)Math.Max(1, ActualWidth), (int)Math.Max(1, ActualHeight));
            _previewHandler.SetWindow(_hwndHost, ref rect);
            _previewHandler.DoPreview();
            _isPreviewLoaded = true;
        }
        catch (Exception ex)
        {
            Error?.Invoke($"Erro ao carregar preview: {ex.Message}");
            UnloadPreview();
        }
    }

    private void UnloadPreview()
    {
        if (_previewHandler != null)
        {
            try
            {
                _previewHandler.Unload();
            }
            catch { }

            try
            {
                Marshal.ReleaseComObject(_previewHandler);
            }
            catch { }

            _previewHandler = null;
        }

        if (_stream != null)
        {
            try
            {
                Marshal.ReleaseComObject(_stream);
            }
            catch { }

            _stream = null;
        }

        _isPreviewLoaded = false;
    }

    private void UpdatePreviewRect()
    {
        if (_previewHandler != null && _isPreviewLoaded && ActualWidth > 0 && ActualHeight > 0)
        {
            try
            {
                var rect = new RECT(0, 0, (int)ActualWidth, (int)ActualHeight);
                _previewHandler.SetRect(ref rect);
            }
            catch { }
        }
    }

    private static Guid GetPreviewHandlerGuid(string extension)
    {
        // Método 1: Direto na extensão
        var shellexKey = $@"{extension}\shellex\{{8895b1c6-b41f-4c1c-a562-0d564250836f}}";
        using (var key = Registry.ClassesRoot.OpenSubKey(shellexKey))
        {
            if (key != null)
            {
                var value = key.GetValue(null) as string;
                if (!string.IsNullOrEmpty(value) && Guid.TryParse(value, out var guid))
                {
                    return guid;
                }
            }
        }

        // Método 2: Via ProgId
        using (var extKey = Registry.ClassesRoot.OpenSubKey(extension))
        {
            if (extKey != null)
            {
                var progId = extKey.GetValue(null) as string;
                if (!string.IsNullOrEmpty(progId))
                {
                    var progIdShellexKey = $@"{progId}\shellex\{{8895b1c6-b41f-4c1c-a562-0d564250836f}}";
                    using var progKey = Registry.ClassesRoot.OpenSubKey(progIdShellexKey);
                    if (progKey != null)
                    {
                        var value = progKey.GetValue(null) as string;
                        if (!string.IsNullOrEmpty(value) && Guid.TryParse(value, out var guid))
                        {
                            return guid;
                        }
                    }
                }
            }
        }

        // Método 3: SystemFileAssociations
        var sysAssocKey = $@"SystemFileAssociations\{extension}\shellex\{{8895b1c6-b41f-4c1c-a562-0d564250836f}}";
        using (var key = Registry.ClassesRoot.OpenSubKey(sysAssocKey))
        {
            if (key != null)
            {
                var value = key.GetValue(null) as string;
                if (!string.IsNullOrEmpty(value) && Guid.TryParse(value, out var guid))
                {
                    return guid;
                }
            }
        }

        return Guid.Empty;
    }

    public void RefreshPreview()
    {
        LoadPreview();
    }
}
