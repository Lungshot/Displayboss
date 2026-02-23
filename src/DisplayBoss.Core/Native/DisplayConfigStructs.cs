using System.Runtime.InteropServices;

namespace DisplayBoss.Core.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct LUID
{
    public uint LowPart;
    public int HighPart;
}

[StructLayout(LayoutKind.Sequential)]
internal struct POINTL
{
    public int x;
    public int y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_RATIONAL
{
    public uint Numerator;
    public uint Denominator;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_2DREGION
{
    public uint cx;
    public uint cy;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_SOURCE_INFO
{
    public LUID adapterId;
    public uint id;
    public uint modeInfoIdx; // Can be DISPLAYCONFIG_PATH_MODE_IDX_INVALID
    public DisplayConfigSourceStatus statusFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_TARGET_INFO
{
    public LUID adapterId;
    public uint id;
    public uint modeInfoIdx; // Can be DISPLAYCONFIG_PATH_MODE_IDX_INVALID
    public DisplayConfigOutputTechnology outputTechnology;
    public DisplayConfigRotation rotation;
    public DisplayConfigScaling scaling;
    public DISPLAYCONFIG_RATIONAL refreshRate;
    public DisplayConfigScanLineOrdering scanLineOrdering;
    public int targetAvailable; // BOOL
    public DisplayConfigTargetStatus statusFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_PATH_INFO
{
    public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
    public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
    public DisplayConfigPathInfoFlags flags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_VIDEO_SIGNAL_INFO
{
    public ulong pixelRate;
    public DISPLAYCONFIG_RATIONAL hSyncFreq;
    public DISPLAYCONFIG_RATIONAL vSyncFreq;
    public DISPLAYCONFIG_2DREGION activeSize;
    public DISPLAYCONFIG_2DREGION totalSize;
    public uint videoStandard;
    public DisplayConfigScanLineOrdering scanLineOrdering;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_SOURCE_MODE
{
    public uint width;
    public uint height;
    public DisplayConfigPixelFormat pixelFormat;
    public POINTL position;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_TARGET_MODE
{
    public DISPLAYCONFIG_VIDEO_SIGNAL_INFO targetVideoSignalInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_DESKTOP_IMAGE_INFO
{
    public POINTL PathSourceSize;
    public RECT DesktopImageRegion;
    public RECT DesktopImageClip;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int left;
    public int top;
    public int right;
    public int bottom;
}

// MODE_INFO uses a union for source/target/desktopImage mode
// The union is the largest of the three: DISPLAYCONFIG_TARGET_MODE (48 bytes)
// DISPLAYCONFIG_SOURCE_MODE is 20 bytes, DISPLAYCONFIG_DESKTOP_IMAGE_INFO is 40 bytes
[StructLayout(LayoutKind.Explicit)]
internal struct DISPLAYCONFIG_MODE_INFO
{
    [FieldOffset(0)]
    public DisplayConfigModeInfoType infoType;

    [FieldOffset(4)]
    public uint id;

    [FieldOffset(8)]
    public LUID adapterId;

    // Union starts at offset 16
    [FieldOffset(16)]
    public DISPLAYCONFIG_TARGET_MODE targetMode;

    [FieldOffset(16)]
    public DISPLAYCONFIG_SOURCE_MODE sourceMode;

    // Note: desktopImageInfo omitted from the union to avoid
    // LayoutKind.Explicit issues with managed types. Not needed for our use case.
}

// Device info header - used for DisplayConfigGetDeviceInfo calls
[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_DEVICE_INFO_HEADER
{
    public DisplayConfigDeviceInfoType type;
    public uint size;
    public LUID adapterId;
    public uint id;
}

// Target device name - for getting EDID info and friendly name
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct DISPLAYCONFIG_TARGET_DEVICE_NAME
{
    public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
    public DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS flags;
    public DisplayConfigOutputTechnology outputTechnology;
    public ushort edidManufactureId;
    public ushort edidProductCodeId;
    public uint connectorInstance;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
    public string monitorFriendlyDeviceName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string monitorDevicePath;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DISPLAYCONFIG_TARGET_DEVICE_NAME_FLAGS
{
    public uint value;

    public readonly bool FriendlyNameFromEdid => (value & 0x00000001) != 0;
    public readonly bool FriendlyNameForced => (value & 0x00000002) != 0;
    public readonly bool EdidIdsValid => (value & 0x00000004) != 0;
}

// Source device name
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
{
    public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string viewGdiDeviceName;
}
