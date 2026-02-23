using DisplayBoss.Core.Models;

namespace DisplayBoss.Core.Services;

public class ProfileService
{
    private readonly ProfileStore _store;
    private readonly DisplayConfigService _displayConfig;

    public ProfileService(ProfileStore store, DisplayConfigService displayConfig)
    {
        _store = store;
        _displayConfig = displayConfig;
    }

    public DisplayProfile SaveCurrentAsProfile(string name, string description = "")
    {
        var profile = _displayConfig.CaptureCurrentConfig();
        profile.Name = name;
        profile.Description = description;
        profile.CreatedAt = DateTime.UtcNow;
        profile.ModifiedAt = DateTime.UtcNow;

        _store.SaveProfile(profile);
        return profile;
    }

    public ApplyResult ApplyProfileByName(string name)
    {
        var profile = _store.LoadProfile(name);
        if (profile == null)
            return ApplyResult.Failed($"Profile '{name}' not found.");

        // Save current state for undo
        var currentState = _displayConfig.CaptureCurrentConfig();
        currentState.Name = "_previous";
        _store.SaveUndoState(currentState);

        return _displayConfig.ApplyProfile(profile);
    }

    public ApplyResult RevertToUndo()
    {
        var undoState = _store.LoadUndoState();
        if (undoState == null)
            return ApplyResult.Failed("No previous display configuration to revert to.");

        return _displayConfig.ApplyProfile(undoState);
    }

    public List<DisplayProfile> ListProfiles() => _store.ListProfiles();

    public bool DeleteProfile(string name) => _store.DeleteProfile(name);

    public bool ProfileExists(string name) => _store.ProfileExists(name);

    public DisplayProfile GetCurrentConfig()
    {
        var profile = _displayConfig.CaptureCurrentConfig();
        profile.Name = "(current)";
        return profile;
    }
}
