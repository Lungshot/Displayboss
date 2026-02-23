using System.Runtime.InteropServices;
using DisplayBoss.Core.Models;
using DisplayBoss.Core.Native;

namespace DisplayBoss.Core.Services;

public class DisplayConfigService
{
    private const int ERROR_SUCCESS = 0;

    public List<MonitorConfig> GetConnectedMonitors()
    {
        var (paths, modes) = QueryAllPaths();
        var monitors = new List<MonitorConfig>();
        var seen = new HashSet<string>();

        for (int i = 0; i < paths.Length; i++)
        {
            ref var path = ref paths[i];

            // Get target device name (EDID info)
            var deviceName = GetTargetDeviceName(path.targetInfo.adapterId, path.targetInfo.id);
            if (deviceName == null)
                continue;

            // Deduplicate by device path (QDC_ALL_PATHS can return the same target multiple times)
            string devicePath = deviceName.Value.monitorDevicePath ?? string.Empty;
            if (!seen.Add(devicePath))
                continue;

            bool isActive = (path.flags & DisplayConfigPathInfoFlags.DISPLAYCONFIG_PATH_ACTIVE) != 0;

            var monitor = new MonitorConfig
            {
                EdidManufacturerId = DecodeEdidManufacturerId(deviceName.Value.edidManufactureId),
                EdidProductCode = deviceName.Value.edidProductCodeId,
                FriendlyName = deviceName.Value.monitorFriendlyDeviceName ?? string.Empty,
                ConnectorType = deviceName.Value.outputTechnology.ToString().Replace("DISPLAYCONFIG_OUTPUT_TECHNOLOGY_", ""),
                DevicePath = devicePath,
                IsActive = isActive,
                Rotation = (int)path.targetInfo.rotation,
                SourceId = path.sourceInfo.id,
                TargetId = path.targetInfo.id,
                AdapterIdLow = path.sourceInfo.adapterId.LowPart,
                AdapterIdHigh = path.sourceInfo.adapterId.HighPart,
            };

            // For active paths, get resolution/position from source mode and refresh from target mode
            if (isActive)
            {
                uint sourceModeIdx = path.sourceInfo.modeInfoIdx;
                if (sourceModeIdx != DisplayConfigConstants.DISPLAYCONFIG_PATH_MODE_IDX_INVALID
                    && sourceModeIdx < modes.Length
                    && modes[sourceModeIdx].infoType == DisplayConfigModeInfoType.DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE)
                {
                    var sourceMode = modes[sourceModeIdx].sourceMode;
                    monitor.Width = (int)sourceMode.width;
                    monitor.Height = (int)sourceMode.height;
                    monitor.PositionX = sourceMode.position.x;
                    monitor.PositionY = sourceMode.position.y;
                    monitor.IsPrimary = sourceMode.position.x == 0 && sourceMode.position.y == 0;
                }

                uint targetModeIdx = path.targetInfo.modeInfoIdx;
                if (targetModeIdx != DisplayConfigConstants.DISPLAYCONFIG_PATH_MODE_IDX_INVALID
                    && targetModeIdx < modes.Length
                    && modes[targetModeIdx].infoType == DisplayConfigModeInfoType.DISPLAYCONFIG_MODE_INFO_TYPE_TARGET)
                {
                    var targetMode = modes[targetModeIdx].targetMode;
                    monitor.RefreshRateNumerator = targetMode.targetVideoSignalInfo.vSyncFreq.Numerator;
                    monitor.RefreshRateDenominator = targetMode.targetVideoSignalInfo.vSyncFreq.Denominator;
                }
            }

            monitors.Add(monitor);
        }

        return monitors;
    }

    public DisplayProfile CaptureCurrentConfig()
    {
        var monitors = GetConnectedMonitors();
        var now = DateTime.UtcNow;
        return new DisplayProfile
        {
            Monitors = monitors,
            CreatedAt = now,
            ModifiedAt = now,
        };
    }

    public ApplyResult ApplyProfile(DisplayProfile profile)
    {
        if (profile.Monitors.Count == 0)
            return ApplyResult.Failed("Profile contains no monitors");

        // Get the active monitors from the profile (these are the ones we want enabled)
        var activeMonitors = profile.Monitors.Where(m => m.IsActive).ToList();
        if (activeMonitors.Count == 0)
            return ApplyResult.Failed("Safety check failed: profile has no active displays");

        // 1. Query ALL current paths to find available source-target combinations
        DISPLAYCONFIG_PATH_INFO[] allPaths;
        DISPLAYCONFIG_MODE_INFO[] allModes;
        try
        {
            (allPaths, allModes) = QueryAllPaths();
        }
        catch (Exception ex)
        {
            return ApplyResult.Failed($"Failed to query current display config: {ex.Message}");
        }

        // Build device name lookup for all current paths
        var deviceNameLookup = new Dictionary<(uint adapterLow, int adapterHigh, uint targetId), DISPLAYCONFIG_TARGET_DEVICE_NAME>();
        for (int i = 0; i < allPaths.Length; i++)
        {
            ref var path = ref allPaths[i];
            var key = (path.targetInfo.adapterId.LowPart, path.targetInfo.adapterId.HighPart, path.targetInfo.id);
            if (!deviceNameLookup.ContainsKey(key))
            {
                var dn = GetTargetDeviceName(path.targetInfo.adapterId, path.targetInfo.id);
                if (dn != null)
                    deviceNameLookup[key] = dn.Value;
            }
        }

        // 2. Match each active saved monitor to a current path
        var resultPaths = new List<DISPLAYCONFIG_PATH_INFO>();
        var resultModes = new List<DISPLAYCONFIG_MODE_INFO>();
        var usedPathIndices = new HashSet<int>();
        var usedSourceIds = new HashSet<(uint adapterLow, int adapterHigh, uint sourceId)>();
        var missing = new List<string>();
        int matchedCount = 0;

        foreach (var saved in activeMonitors)
        {
            int pathIdx = FindMatchingPath(saved, allPaths, deviceNameLookup, usedPathIndices);
            if (pathIdx < 0)
            {
                missing.Add(saved.DisplayName);
                continue;
            }

            usedPathIndices.Add(pathIdx);
            matchedCount++;

            var path = allPaths[pathIdx];

            // Set path as active with saved rotation
            path.flags = DisplayConfigPathInfoFlags.DISPLAYCONFIG_PATH_ACTIVE;
            path.targetInfo.rotation = (DisplayConfigRotation)saved.Rotation;

            // We need a unique source ID for each active path.
            // Try to use the saved sourceId; if it conflicts, find an unused one.
            var sourceKey = (path.sourceInfo.adapterId.LowPart, path.sourceInfo.adapterId.HighPart, saved.SourceId);
            if (!usedSourceIds.Contains(sourceKey))
            {
                path.sourceInfo.id = saved.SourceId;
                usedSourceIds.Add(sourceKey);
            }
            // else keep the path's existing sourceInfo.id and track it
            else
            {
                var existingKey = (path.sourceInfo.adapterId.LowPart, path.sourceInfo.adapterId.HighPart, path.sourceInfo.id);
                if (usedSourceIds.Contains(existingKey))
                {
                    // Find a free source ID
                    for (uint s = 0; s < 32; s++)
                    {
                        var tryKey = (path.sourceInfo.adapterId.LowPart, path.sourceInfo.adapterId.HighPart, s);
                        if (!usedSourceIds.Contains(tryKey))
                        {
                            path.sourceInfo.id = s;
                            usedSourceIds.Add(tryKey);
                            break;
                        }
                    }
                }
                else
                {
                    usedSourceIds.Add(existingKey);
                }
            }

            // Create source mode for this path
            var sourceMode = new DISPLAYCONFIG_MODE_INFO
            {
                infoType = DisplayConfigModeInfoType.DISPLAYCONFIG_MODE_INFO_TYPE_SOURCE,
                id = path.sourceInfo.id,
                adapterId = path.sourceInfo.adapterId,
                sourceMode = new DISPLAYCONFIG_SOURCE_MODE
                {
                    width = (uint)saved.Width,
                    height = (uint)saved.Height,
                    pixelFormat = DisplayConfigPixelFormat.DISPLAYCONFIG_PIXELFORMAT_32BPP,
                    position = new POINTL { x = saved.PositionX, y = saved.PositionY },
                },
            };
            path.sourceInfo.modeInfoIdx = (uint)resultModes.Count;
            resultModes.Add(sourceMode);

            // For target mode: try to copy from existing config, otherwise let Windows decide
            uint origTargetModeIdx = allPaths[pathIdx].targetInfo.modeInfoIdx;
            if (origTargetModeIdx != DisplayConfigConstants.DISPLAYCONFIG_PATH_MODE_IDX_INVALID
                && origTargetModeIdx < allModes.Length
                && allModes[origTargetModeIdx].infoType == DisplayConfigModeInfoType.DISPLAYCONFIG_MODE_INFO_TYPE_TARGET)
            {
                var targetMode = allModes[origTargetModeIdx];
                targetMode.id = path.targetInfo.id;
                targetMode.adapterId = path.targetInfo.adapterId;
                path.targetInfo.modeInfoIdx = (uint)resultModes.Count;
                resultModes.Add(targetMode);
            }
            else
            {
                // No existing target mode - let Windows choose via SDC_ALLOW_CHANGES
                path.targetInfo.modeInfoIdx = DisplayConfigConstants.DISPLAYCONFIG_PATH_MODE_IDX_INVALID;
            }

            resultPaths.Add(path);
        }

        if (matchedCount == 0)
            return ApplyResult.Failed("No monitors from the profile could be matched to currently connected displays");

        var pathArray = resultPaths.ToArray();
        var modeArray = resultModes.ToArray();

        // 3. Validate first
        int validateResult = NativeMethods.SetDisplayConfig(
            (uint)pathArray.Length, pathArray,
            (uint)modeArray.Length, modeArray,
            SetDisplayConfigFlags.SDC_VALIDATE
            | SetDisplayConfigFlags.SDC_USE_SUPPLIED_DISPLAY_CONFIG
            | SetDisplayConfigFlags.SDC_ALLOW_CHANGES);

        if (validateResult != ERROR_SUCCESS)
        {
            // Fallback: try with just topology, let Windows pick all modes
            return ApplyWithTopologyOnly(resultPaths, missing, matchedCount);
        }

        // 4. Apply
        int applyResult = NativeMethods.SetDisplayConfig(
            (uint)pathArray.Length, pathArray,
            (uint)modeArray.Length, modeArray,
            SetDisplayConfigFlags.SDC_APPLY
            | SetDisplayConfigFlags.SDC_USE_SUPPLIED_DISPLAY_CONFIG
            | SetDisplayConfigFlags.SDC_ALLOW_CHANGES
            | SetDisplayConfigFlags.SDC_SAVE_TO_DATABASE);

        if (applyResult != ERROR_SUCCESS)
            return ApplyResult.Failed($"SetDisplayConfig apply failed with Win32 error 0x{applyResult:X8}", applyResult);

        return ApplyResult.Succeeded(matchedCount, missing.Count > 0 ? missing : null);
    }

    /// <summary>
    /// Fallback: apply using SDC_TOPOLOGY_SUPPLIED, which passes only the path topology
    /// and lets Windows determine source and target modes automatically.
    /// </summary>
    private static ApplyResult ApplyWithTopologyOnly(
        List<DISPLAYCONFIG_PATH_INFO> paths,
        List<string> missing,
        int matchedCount)
    {
        // Set all mode indices to INVALID - let Windows figure out modes
        var topologyPaths = paths.ToArray();
        for (int i = 0; i < topologyPaths.Length; i++)
        {
            topologyPaths[i].sourceInfo.modeInfoIdx = DisplayConfigConstants.DISPLAYCONFIG_PATH_MODE_IDX_INVALID;
            topologyPaths[i].targetInfo.modeInfoIdx = DisplayConfigConstants.DISPLAYCONFIG_PATH_MODE_IDX_INVALID;
        }

        // Validate
        int validateResult = NativeMethods.SetDisplayConfig(
            (uint)topologyPaths.Length, topologyPaths,
            0, null,
            SetDisplayConfigFlags.SDC_VALIDATE
            | SetDisplayConfigFlags.SDC_TOPOLOGY_SUPPLIED
            | SetDisplayConfigFlags.SDC_ALLOW_CHANGES
            | SetDisplayConfigFlags.SDC_ALLOW_PATH_ORDER_CHANGES);

        if (validateResult != ERROR_SUCCESS)
            return ApplyResult.ValidationFailed($"Topology validation failed with Win32 error 0x{validateResult:X8}", validateResult);

        // Apply
        int applyResult = NativeMethods.SetDisplayConfig(
            (uint)topologyPaths.Length, topologyPaths,
            0, null,
            SetDisplayConfigFlags.SDC_APPLY
            | SetDisplayConfigFlags.SDC_TOPOLOGY_SUPPLIED
            | SetDisplayConfigFlags.SDC_ALLOW_CHANGES
            | SetDisplayConfigFlags.SDC_ALLOW_PATH_ORDER_CHANGES
            | SetDisplayConfigFlags.SDC_PATH_PERSIST_IF_REQUIRED);

        if (applyResult != ERROR_SUCCESS)
            return ApplyResult.Failed($"Topology apply failed with Win32 error 0x{applyResult:X8}", applyResult);

        return ApplyResult.Succeeded(matchedCount, missing.Count > 0 ? missing : null);
    }

    internal static string DecodeEdidManufacturerId(ushort encoded)
    {
        // EDID manufacturer ID is big-endian, so swap bytes first
        ushort be = (ushort)((encoded << 8) | (encoded >> 8));

        // Three 5-bit characters packed into bits 14..0 (bit 15 is reserved/zero)
        // Bits 14-10: first letter, bits 9-5: second letter, bits 4-0: third letter
        // 1=A, 2=B, ..., 26=Z
        int c1 = (be >> 10) & 0x1F;
        int c2 = (be >> 5) & 0x1F;
        int c3 = be & 0x1F;

        if (c1 < 1 || c1 > 26 || c2 < 1 || c2 > 26 || c3 < 1 || c3 > 26)
            return $"?{encoded:X4}";

        return new string(new[]
        {
            (char)('A' + c1 - 1),
            (char)('A' + c2 - 1),
            (char)('A' + c3 - 1),
        });
    }

    private static (DISPLAYCONFIG_PATH_INFO[] paths, DISPLAYCONFIG_MODE_INFO[] modes) QueryAllPaths()
    {
        int err = NativeMethods.GetDisplayConfigBufferSizes(
            QueryDisplayConfigFlags.QDC_ALL_PATHS,
            out uint pathCount,
            out uint modeCount);

        if (err != ERROR_SUCCESS)
            throw new InvalidOperationException($"GetDisplayConfigBufferSizes failed with error 0x{err:X8}");

        var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

        err = NativeMethods.QueryDisplayConfig(
            QueryDisplayConfigFlags.QDC_ALL_PATHS,
            ref pathCount, paths,
            ref modeCount, modes,
            IntPtr.Zero); // QDC_ALL_PATHS does not return topology

        if (err != ERROR_SUCCESS)
            throw new InvalidOperationException($"QueryDisplayConfig failed with error 0x{err:X8}");

        // Trim arrays if the API returned fewer elements than the buffer size
        if (pathCount < (uint)paths.Length)
            Array.Resize(ref paths, (int)pathCount);
        if (modeCount < (uint)modes.Length)
            Array.Resize(ref modes, (int)modeCount);

        return (paths, modes);
    }

    private static DISPLAYCONFIG_TARGET_DEVICE_NAME? GetTargetDeviceName(LUID adapterId, uint targetId)
    {
        var deviceName = new DISPLAYCONFIG_TARGET_DEVICE_NAME();
        deviceName.header.type = DisplayConfigDeviceInfoType.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
        deviceName.header.size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>();
        deviceName.header.adapterId = adapterId;
        deviceName.header.id = targetId;

        int err = NativeMethods.DisplayConfigGetDeviceInfo(ref deviceName);
        if (err != ERROR_SUCCESS)
            return null;

        return deviceName;
    }

    private static int FindMatchingPath(
        MonitorConfig saved,
        DISPLAYCONFIG_PATH_INFO[] paths,
        Dictionary<(uint adapterLow, int adapterHigh, uint targetId), DISPLAYCONFIG_TARGET_DEVICE_NAME> deviceNames,
        HashSet<int> usedIndices)
    {
        // Pass 1: Match by device path (most specific - unique per physical connection)
        if (!string.IsNullOrEmpty(saved.DevicePath))
        {
            for (int i = 0; i < paths.Length; i++)
            {
                if (usedIndices.Contains(i))
                    continue;

                var key = (paths[i].targetInfo.adapterId.LowPart, paths[i].targetInfo.adapterId.HighPart, paths[i].targetInfo.id);
                if (!deviceNames.TryGetValue(key, out var dn))
                    continue;

                if ((dn.monitorDevicePath ?? string.Empty) == saved.DevicePath)
                    return i;
            }
        }

        // Pass 2: EDID manufacturer + product code match
        for (int i = 0; i < paths.Length; i++)
        {
            if (usedIndices.Contains(i))
                continue;

            var key = (paths[i].targetInfo.adapterId.LowPart, paths[i].targetInfo.adapterId.HighPart, paths[i].targetInfo.id);
            if (!deviceNames.TryGetValue(key, out var dn))
                continue;

            string mfr = DecodeEdidManufacturerId(dn.edidManufactureId);
            if (mfr == saved.EdidManufacturerId && dn.edidProductCodeId == saved.EdidProductCode)
                return i;
        }

        // Pass 3: Friendly name match
        for (int i = 0; i < paths.Length; i++)
        {
            if (usedIndices.Contains(i))
                continue;

            var key = (paths[i].targetInfo.adapterId.LowPart, paths[i].targetInfo.adapterId.HighPart, paths[i].targetInfo.id);
            if (!deviceNames.TryGetValue(key, out var dn))
                continue;

            string name = dn.monitorFriendlyDeviceName ?? string.Empty;
            if (!string.IsNullOrEmpty(name) && name == saved.FriendlyName)
                return i;
        }

        // Pass 4: Target ID match (weakest, adapter-relative)
        for (int i = 0; i < paths.Length; i++)
        {
            if (usedIndices.Contains(i))
                continue;

            if (paths[i].targetInfo.id == saved.TargetId)
                return i;
        }

        return -1;
    }
}
