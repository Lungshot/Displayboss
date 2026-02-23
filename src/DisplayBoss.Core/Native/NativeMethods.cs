using System.Runtime.InteropServices;

namespace DisplayBoss.Core.Native;

internal static class NativeMethods
{
    private const string User32 = "user32.dll";

    [DllImport(User32)]
    public static extern int GetDisplayConfigBufferSizes(
        QueryDisplayConfigFlags flags,
        out uint numPathArrayElements,
        out uint numModeInfoArrayElements);

    [DllImport(User32)]
    public static extern int QueryDisplayConfig(
        QueryDisplayConfigFlags flags,
        ref uint numPathArrayElements,
        [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
        ref uint numModeInfoArrayElements,
        [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
        out DisplayConfigTopologyId currentTopologyId);

    // Overload without topology (for QDC_ALL_PATHS which doesn't return topology)
    [DllImport(User32)]
    public static extern int QueryDisplayConfig(
        QueryDisplayConfigFlags flags,
        ref uint numPathArrayElements,
        [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
        ref uint numModeInfoArrayElements,
        [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
        IntPtr currentTopologyId);

    [DllImport(User32)]
    public static extern int SetDisplayConfig(
        uint numPathArrayElements,
        [In] DISPLAYCONFIG_PATH_INFO[]? pathArray,
        uint numModeInfoArrayElements,
        [In] DISPLAYCONFIG_MODE_INFO[]? modeInfoArray,
        SetDisplayConfigFlags flags);

    [DllImport(User32)]
    public static extern int DisplayConfigGetDeviceInfo(
        ref DISPLAYCONFIG_TARGET_DEVICE_NAME deviceName);

    [DllImport(User32)]
    public static extern int DisplayConfigGetDeviceInfo(
        ref DISPLAYCONFIG_SOURCE_DEVICE_NAME deviceName);
}
