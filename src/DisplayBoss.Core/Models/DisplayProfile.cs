using System.Text.Json.Serialization;

namespace DisplayBoss.Core.Models;

public class DisplayProfile
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("modifiedAt")]
    public DateTime ModifiedAt { get; set; }

    [JsonPropertyName("monitors")]
    public List<MonitorConfig> Monitors { get; set; } = new();

    [JsonIgnore]
    public int ActiveMonitorCount => Monitors.Count(m => m.IsActive);

    [JsonIgnore]
    public string Summary => $"{ActiveMonitorCount}/{Monitors.Count} monitors active";
}
