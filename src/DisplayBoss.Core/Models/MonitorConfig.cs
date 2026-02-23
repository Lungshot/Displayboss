using System.Text.Json.Serialization;

namespace DisplayBoss.Core.Models;

public class MonitorConfig
{
    // Stable identification (EDID-based, survives reboots/replug)
    [JsonPropertyName("edidManufacturerId")]
    public string EdidManufacturerId { get; set; } = string.Empty;

    [JsonPropertyName("edidProductCode")]
    public int EdidProductCode { get; set; }

    [JsonPropertyName("edidSerialNumber")]
    public string EdidSerialNumber { get; set; } = string.Empty;

    [JsonPropertyName("friendlyName")]
    public string FriendlyName { get; set; } = string.Empty;

    [JsonPropertyName("connectorType")]
    public string ConnectorType { get; set; } = string.Empty;

    [JsonPropertyName("devicePath")]
    public string DevicePath { get; set; } = string.Empty;

    // Display state
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("isPrimary")]
    public bool IsPrimary { get; set; }

    [JsonPropertyName("positionX")]
    public int PositionX { get; set; }

    [JsonPropertyName("positionY")]
    public int PositionY { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("refreshRateNumerator")]
    public uint RefreshRateNumerator { get; set; }

    [JsonPropertyName("refreshRateDenominator")]
    public uint RefreshRateDenominator { get; set; }

    [JsonPropertyName("rotation")]
    public int Rotation { get; set; }

    // Volatile IDs (used for fast matching, remapped on load)
    [JsonPropertyName("sourceId")]
    public uint SourceId { get; set; }

    [JsonPropertyName("targetId")]
    public uint TargetId { get; set; }

    [JsonPropertyName("adapterIdLow")]
    public uint AdapterIdLow { get; set; }

    [JsonPropertyName("adapterIdHigh")]
    public int AdapterIdHigh { get; set; }

    // Computed display name for UI
    [JsonIgnore]
    public string DisplayName => !string.IsNullOrEmpty(FriendlyName)
        ? FriendlyName
        : $"{EdidManufacturerId} ({EdidProductCode})";

    [JsonIgnore]
    public string ResolutionString => $"{Width}x{Height}";

    [JsonIgnore]
    public double RefreshRateHz => RefreshRateDenominator > 0
        ? Math.Round((double)RefreshRateNumerator / RefreshRateDenominator, 1)
        : 0;
}
