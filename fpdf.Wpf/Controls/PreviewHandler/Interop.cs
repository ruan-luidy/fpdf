using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace fpdf.Wpf.Controls.PreviewHandler;

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("8895b1c6-b41f-4c1c-a562-0d564250836f")]
public interface IPreviewHandler
{
    void SetWindow(IntPtr hwnd, ref RECT rect);
    void SetRect(ref RECT rect);
    void DoPreview();
    void Unload();
    void SetFocus();
    void QueryFocus(out IntPtr phwnd);
    [PreserveSig]
    uint TranslateAccelerator(ref MSG pmsg);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("b7d14566-0509-4cce-a71f-0a554233bd9b")]
public interface IInitializeWithFile
{
    void Initialize([MarshalAs(UnmanagedType.LPWStr)] string pszFilePath, uint grfMode);
}

[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("b824b49d-22ac-4161-ac8a-9916e8fa3f7f")]
public interface IInitializeWithStream
{
    void Initialize(IStream pstream, uint grfMode);
}

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public RECT(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct MSG
{
    public IntPtr hwnd;
    public uint message;
    public IntPtr wParam;
    public IntPtr lParam;
    public uint time;
    public POINT pt;
}

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}
