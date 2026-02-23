using System.Text.Json;
using System.Text.RegularExpressions;
using DisplayBoss.Core.Models;

namespace DisplayBoss.Core.Services;

public class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _profileDirectory;
    private readonly string _undoFilePath;

    public ProfileStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var baseDir = Path.Combine(appData, "DisplayBoss");
        _profileDirectory = Path.Combine(baseDir, "Profiles");
        _undoFilePath = Path.Combine(baseDir, "_previous.json");

        Directory.CreateDirectory(_profileDirectory);
    }

    // For testing - allow custom directory
    public ProfileStore(string baseDirectory)
    {
        _profileDirectory = Path.Combine(baseDirectory, "Profiles");
        _undoFilePath = Path.Combine(baseDirectory, "_previous.json");

        Directory.CreateDirectory(_profileDirectory);
    }

    public string ProfileDirectory => _profileDirectory;

    public void SaveProfile(DisplayProfile profile)
    {
        var fileName = SanitizeFileName(profile.Name) + ".json";
        var filePath = Path.Combine(_profileDirectory, fileName);
        var tempPath = filePath + ".tmp";

        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, filePath, overwrite: true);
    }

    public DisplayProfile? LoadProfile(string name)
    {
        var fileName = SanitizeFileName(name) + ".json";
        var filePath = Path.Combine(_profileDirectory, fileName);

        if (!File.Exists(filePath))
            return null;

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<DisplayProfile>(json, JsonOptions);
    }

    public List<DisplayProfile> ListProfiles()
    {
        var profiles = new List<DisplayProfile>();

        if (!Directory.Exists(_profileDirectory))
            return profiles;

        foreach (var file in Directory.GetFiles(_profileDirectory, "*.json").OrderBy(f => f))
        {
            try
            {
                var json = File.ReadAllText(file);
                var profile = JsonSerializer.Deserialize<DisplayProfile>(json, JsonOptions);
                if (profile != null)
                    profiles.Add(profile);
            }
            catch (JsonException)
            {
                // Skip malformed profile files
            }
        }

        return profiles;
    }

    public bool DeleteProfile(string name)
    {
        var fileName = SanitizeFileName(name) + ".json";
        var filePath = Path.Combine(_profileDirectory, fileName);

        if (!File.Exists(filePath))
            return false;

        File.Delete(filePath);
        return true;
    }

    public bool ProfileExists(string name)
    {
        var fileName = SanitizeFileName(name) + ".json";
        var filePath = Path.Combine(_profileDirectory, fileName);
        return File.Exists(filePath);
    }

    public void SaveUndoState(DisplayProfile profile)
    {
        var tempPath = _undoFilePath + ".tmp";
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _undoFilePath, overwrite: true);
    }

    public DisplayProfile? LoadUndoState()
    {
        if (!File.Exists(_undoFilePath))
            return null;

        var json = File.ReadAllText(_undoFilePath);
        return JsonSerializer.Deserialize<DisplayProfile>(json, JsonOptions);
    }

    public static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Profile name cannot be empty.", nameof(name));

        // Remove invalid filename characters
        var invalidChars = new string(Path.GetInvalidFileNameChars());
        var sanitized = Regex.Replace(name.Trim(), $"[{Regex.Escape(invalidChars)}]", "_");

        // Replace multiple underscores/spaces with single
        sanitized = Regex.Replace(sanitized, @"[_\s]+", "_").Trim('_');

        // Enforce max length
        if (sanitized.Length > 100)
            sanitized = sanitized[..100];

        if (string.IsNullOrEmpty(sanitized))
            throw new ArgumentException("Profile name results in empty filename after sanitization.", nameof(name));

        return sanitized;
    }
}
