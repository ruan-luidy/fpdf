using System.Runtime.InteropServices;

namespace fpdf.Core.Native;

internal static class WNetInterop
{
  // Resource scope
  public const int RESOURCE_CONNECTED = 0x00000001;
  public const int RESOURCE_GLOBALNET = 0x00000002;

  // Resource type
  public const int RESOURCETYPE_ANY = 0x00000000;
  public const int RESOURCETYPE_DISK = 0x00000001;

  // Resource display type
  public const int RESOURCEDISPLAYTYPE_DOMAIN = 0x00000001;
  public const int RESOURCEDISPLAYTYPE_SERVER = 0x00000002;
  public const int RESOURCEDISPLAYTYPE_SHARE = 0x00000003;
  public const int RESOURCEDISPLAYTYPE_NETWORK = 0x00000006;

  // Resource usage
  public const int RESOURCEUSAGE_CONNECTABLE = 0x00000001;
  public const int RESOURCEUSAGE_CONTAINER = 0x00000002;

  // Error codes
  public const int NO_ERROR = 0;
  public const int ERROR_NO_MORE_ITEMS = 259;

  [StructLayout(LayoutKind.Sequential)]
  public class NETRESOURCE
  {
    public int dwScope;
    public int dwType;
    public int dwDisplayType;
    public int dwUsage;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string? lpLocalName;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string? lpRemoteName;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string? lpComment;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string? lpProvider;
  }

  [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
  public static extern int WNetOpenEnum(
    int dwScope,
    int dwType,
    int dwUsage,
    NETRESOURCE? lpNetResource,
    out IntPtr lphEnum);

  [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
  public static extern int WNetEnumResource(
    IntPtr hEnum,
    ref int lpcCount,
    IntPtr lpBuffer,
    ref int lpBufferSize);

  [DllImport("mpr.dll")]
  public static extern int WNetCloseEnum(IntPtr hEnum);
}
