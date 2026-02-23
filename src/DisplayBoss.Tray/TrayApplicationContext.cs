using DisplayBoss.Core.Models;
using DisplayBoss.Core.Services;
using DisplayBoss.Tray.Forms;
using Microsoft.Win32;

namespace DisplayBoss.Tray;

public class TrayApplicationContext : ApplicationContext
{
    private const string AppName = "DisplayBoss";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly ProfileStore _store;
    private readonly DisplayConfigService _displayConfig;
    private readonly ProfileService _profileService;
    private readonly FileSystemWatcher _watcher;

    public TrayApplicationContext()
    {
        _store = new ProfileStore();
        _displayConfig = new DisplayConfigService();
        _profileService = new ProfileService(_store, _displayConfig);

        _contextMenu = new ContextMenuStrip();
        _contextMenu.Opening += ContextMenu_Opening;

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "DisplayBoss - Display Profile Switcher",
            Visible = true,
            ContextMenuStrip = _contextMenu,
        };

        _notifyIcon.DoubleClick += (_, _) => _notifyIcon.ShowContextMenu();

        // Watch profile directory for external changes
        _watcher = new FileSystemWatcher(_store.ProfileDirectory, "*.json")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            EnableRaisingEvents = true,
        };

        BuildMenu();
    }

    private void ContextMenu_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        BuildMenu();
    }

    private void BuildMenu()
    {
        _contextMenu.Items.Clear();

        // Profiles header
        var header = new ToolStripLabel("Profiles")
        {
            Font = new Font(_contextMenu.Font, FontStyle.Bold),
        };
        _contextMenu.Items.Add(header);

        // List saved profiles
        var profiles = _profileService.ListProfiles();
        if (profiles.Count == 0)
        {
            var empty = new ToolStripMenuItem("(no saved profiles)")
            {
                Enabled = false,
            };
            _contextMenu.Items.Add(empty);
        }
        else
        {
            foreach (var profile in profiles)
            {
                var item = new ToolStripMenuItem(profile.Name)
                {
                    ToolTipText = $"{profile.Summary}\n{profile.Description}".Trim(),
                    Tag = profile.Name,
                };
                item.Click += ProfileItem_Click;
                _contextMenu.Items.Add(item);
            }
        }

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Save current
        var saveItem = new ToolStripMenuItem("Save Current as Profile...");
        saveItem.Click += SaveProfile_Click;
        _contextMenu.Items.Add(saveItem);

        // Manage profiles
        var manageItem = new ToolStripMenuItem("Manage Profiles...");
        manageItem.Click += ManageProfiles_Click;
        _contextMenu.Items.Add(manageItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Start with Windows
        var startupItem = new ToolStripMenuItem("Start with Windows")
        {
            Checked = IsStartupEnabled(),
            CheckOnClick = true,
        };
        startupItem.Click += StartupToggle_Click;
        _contextMenu.Items.Add(startupItem);

        // About
        var aboutItem = new ToolStripMenuItem("About DisplayBoss");
        aboutItem.Click += About_Click;
        _contextMenu.Items.Add(aboutItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Exit
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += Exit_Click;
        _contextMenu.Items.Add(exitItem);
    }

    private void ProfileItem_Click(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem item || item.Tag is not string profileName)
            return;

        try
        {
            var result = _profileService.ApplyProfileByName(profileName);
            if (result.Success)
            {
                _notifyIcon.ShowBalloonTip(
                    3000,
                    AppName,
                    result.Message,
                    ToolTipIcon.Info);
            }
            else
            {
                _notifyIcon.ShowBalloonTip(
                    5000,
                    AppName,
                    $"Failed: {result.Message}",
                    ToolTipIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _notifyIcon.ShowBalloonTip(
                5000,
                AppName,
                $"Error applying profile: {ex.Message}",
                ToolTipIcon.Error);
        }
    }

    private void SaveProfile_Click(object? sender, EventArgs e)
    {
        using var form = new SaveProfileForm();
        if (form.ShowDialog() != DialogResult.OK)
            return;

        string name = form.ProfileName;
        string description = form.ProfileDescription;

        // Check if profile already exists
        if (_profileService.ProfileExists(name))
        {
            var overwrite = MessageBox.Show(
                $"A profile named '{name}' already exists. Overwrite?",
                AppName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (overwrite != DialogResult.Yes)
                return;
        }

        try
        {
            _profileService.SaveCurrentAsProfile(name, description);
            _notifyIcon.ShowBalloonTip(
                3000,
                AppName,
                $"Profile '{name}' saved successfully.",
                ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to save profile: {ex.Message}",
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ManageProfiles_Click(object? sender, EventArgs e)
    {
        using var form = new ManageProfilesForm(_profileService);
        form.ShowDialog();
    }

    private void StartupToggle_Click(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem item)
            return;

        try
        {
            if (item.Checked)
                EnableStartup();
            else
                DisableStartup();
        }
        catch (Exception ex)
        {
            item.Checked = !item.Checked; // revert
            MessageBox.Show(
                $"Failed to change startup setting: {ex.Message}",
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void About_Click(object? sender, EventArgs e)
    {
        using var form = new AboutForm();
        form.ShowDialog();
    }

    private void Exit_Click(object? sender, EventArgs e)
    {
        _notifyIcon.Visible = false;
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _contextMenu.Dispose();
        }
        base.Dispose(disposing);
    }

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(AppName) != null;
    }

    private static void EnableStartup()
    {
        string exePath = Application.ExecutablePath;
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        key?.SetValue(AppName, $"\"{exePath}\"");
    }

    private static void DisableStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        key?.DeleteValue(AppName, false);
    }
}

// Extension to show context menu on double-click
internal static class NotifyIconExtensions
{
    public static void ShowContextMenu(this NotifyIcon icon)
    {
        // Use reflection to invoke the private ShowContextMenu method
        var method = typeof(NotifyIcon).GetMethod("ShowContextMenu",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method?.Invoke(icon, null);
    }
}
