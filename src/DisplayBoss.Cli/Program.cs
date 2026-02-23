using DisplayBoss.Core.Models;
using DisplayBoss.Core.Services;

namespace DisplayBoss.Cli;

internal class Program
{
    private static readonly ProfileService Service = new(new ProfileStore(), new DisplayConfigService());

    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0].ToLowerInvariant();

        try
        {
            return command switch
            {
                "list" => ListProfiles(),
                "save" => SaveProfile(args),
                "load" => LoadProfile(args),
                "current" => ShowCurrent(),
                "delete" => DeleteProfile(args),
                "undo" => Undo(),
                "help" or "--help" or "-h" => ShowHelp(),
                _ => UnknownCommand(command),
            };
        }
        catch (NotImplementedException)
        {
            WriteError("DisplayBoss display engine not yet implemented. Core library is a stub.");
            return 1;
        }
        catch (Exception ex)
        {
            WriteError($"Unexpected error: {ex.Message}");
            return 1;
        }
    }

    private static int ListProfiles()
    {
        var profiles = Service.ListProfiles();

        if (profiles.Count == 0)
        {
            Console.WriteLine("No saved profiles.");
            Console.WriteLine("Use 'displayboss save \"Name\"' to save the current display configuration.");
            return 0;
        }

        Console.WriteLine($"{"Name",-30} {"Monitors",-15} {"Created",-20} Description");
        Console.WriteLine(new string('-', 90));

        foreach (var profile in profiles)
        {
            var created = profile.CreatedAt == default ? "N/A" : profile.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            Console.WriteLine($"{Truncate(profile.Name, 28),-30} {profile.Summary,-15} {created,-20} {Truncate(profile.Description, 30)}");
        }

        Console.WriteLine();
        Console.WriteLine($"{profiles.Count} profile(s) found.");
        return 0;
    }

    private static int SaveProfile(string[] args)
    {
        if (args.Length < 2)
        {
            WriteError("Usage: displayboss save \"Profile Name\" [--description \"desc\"] [--force]");
            return 1;
        }

        var name = args[1];
        var description = "";
        var force = false;

        for (int i = 2; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--description" or "-d":
                    if (i + 1 < args.Length)
                        description = args[++i];
                    break;
                case "--force" or "-f":
                    force = true;
                    break;
            }
        }

        if (Service.ProfileExists(name) && !force)
        {
            WriteError($"Profile '{name}' already exists. Use --force to overwrite.");
            return 1;
        }

        var profile = Service.SaveCurrentAsProfile(name, description);

        WriteSuccess($"Profile '{name}' saved successfully.");
        Console.WriteLine($"  Monitors: {profile.Summary}");

        foreach (var monitor in profile.Monitors)
        {
            var status = monitor.IsActive ? "Active" : "Disabled";
            var primary = monitor.IsPrimary ? " [Primary]" : "";
            Console.WriteLine($"    {monitor.DisplayName}: {monitor.ResolutionString} @ {monitor.RefreshRateHz}Hz ({status}{primary})");
        }

        return 0;
    }

    private static int LoadProfile(string[] args)
    {
        if (args.Length < 2)
        {
            WriteError("Usage: displayboss load \"Profile Name\"");
            return 1;
        }

        var name = args[1];
        Console.WriteLine($"Applying profile '{name}'...");

        var result = Service.ApplyProfileByName(name);

        if (result.Success)
        {
            WriteSuccess(result.Message);

            if (result.MissingMonitors.Count > 0)
            {
                WriteWarning("Missing monitors (not currently connected):");
                foreach (var m in result.MissingMonitors)
                    Console.WriteLine($"    - {m}");
            }

            return 0;
        }
        else
        {
            WriteError(result.Message);
            if (result.ErrorCode != 0)
                Console.WriteLine($"  Win32 error code: {result.ErrorCode}");
            return result.ErrorCode == 0 ? 1 : 2;
        }
    }

    private static int ShowCurrent()
    {
        var profile = Service.GetCurrentConfig();

        Console.WriteLine("Current Display Configuration:");
        Console.WriteLine(new string('-', 70));

        foreach (var monitor in profile.Monitors)
        {
            var status = monitor.IsActive ? "Active" : "Disabled";
            var primary = monitor.IsPrimary ? " [Primary]" : "";
            var rotation = monitor.Rotation switch
            {
                2 => " (Portrait)",       // DISPLAYCONFIG_ROTATION_ROTATE90
                3 => " (Flipped)",        // DISPLAYCONFIG_ROTATION_ROTATE180
                4 => " (Portrait Flipped)", // DISPLAYCONFIG_ROTATION_ROTATE270
                _ => ""
            };

            Console.WriteLine($"  {monitor.DisplayName}{primary}");
            Console.WriteLine($"    Status:     {status}");
            Console.WriteLine($"    Resolution: {monitor.ResolutionString}{rotation}");
            Console.WriteLine($"    Refresh:    {monitor.RefreshRateHz}Hz");
            Console.WriteLine($"    Position:   ({monitor.PositionX}, {monitor.PositionY})");
            Console.WriteLine($"    Connector:  {monitor.ConnectorType}");
            Console.WriteLine($"    EDID:       {monitor.EdidManufacturerId} / {monitor.EdidProductCode}");
            Console.WriteLine($"    Device:     {monitor.DevicePath}");
            Console.WriteLine();
        }

        Console.WriteLine($"Total: {profile.Monitors.Count} monitor(s), {profile.ActiveMonitorCount} active");
        return 0;
    }

    private static int DeleteProfile(string[] args)
    {
        if (args.Length < 2)
        {
            WriteError("Usage: displayboss delete \"Profile Name\" [--yes]");
            return 1;
        }

        var name = args[1];
        var skipConfirm = args.Length > 2 && args[2].ToLowerInvariant() is "--yes" or "-y";

        if (!Service.ProfileExists(name))
        {
            WriteError($"Profile '{name}' not found.");
            return 2;
        }

        if (!skipConfirm)
        {
            Console.Write($"Delete profile '{name}'? [y/N] ");
            var response = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (response is not ("y" or "yes"))
            {
                Console.WriteLine("Cancelled.");
                return 0;
            }
        }

        if (Service.DeleteProfile(name))
        {
            WriteSuccess($"Profile '{name}' deleted.");
            return 0;
        }
        else
        {
            WriteError($"Failed to delete profile '{name}'.");
            return 1;
        }
    }

    private static int Undo()
    {
        Console.WriteLine("Reverting to previous display configuration...");

        var result = Service.RevertToUndo();

        if (result.Success)
        {
            WriteSuccess(result.Message);
            return 0;
        }
        else
        {
            WriteError(result.Message);
            return 1;
        }
    }

    private static int ShowHelp()
    {
        PrintUsage();
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        WriteError($"Unknown command: '{command}'");
        Console.WriteLine();
        PrintUsage();
        return 1;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("DisplayBoss - Display Profile Switcher");
        Console.WriteLine();
        Console.WriteLine("Usage: displayboss <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  list                          List all saved profiles");
        Console.WriteLine("  save \"Name\" [options]         Save current display configuration");
        Console.WriteLine("    --description, -d \"text\"      Add a description");
        Console.WriteLine("    --force, -f                   Overwrite existing profile");
        Console.WriteLine("  load \"Name\"                   Apply a saved profile");
        Console.WriteLine("  current                       Show current display configuration");
        Console.WriteLine("  delete \"Name\" [--yes]         Delete a saved profile");
        Console.WriteLine("  undo                          Revert to previous display configuration");
        Console.WriteLine("  help                          Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  displayboss save \"Office\" -d \"All 4 monitors active\"");
        Console.WriteLine("  displayboss save \"Remote\" -d \"Primary monitor only\" -f");
        Console.WriteLine("  displayboss load \"Office\"");
        Console.WriteLine("  displayboss undo");
    }

    private static void WriteSuccess(string message)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ForegroundColor = prev;
    }

    private static void WriteError(string message)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"Error: {message}");
        Console.ForegroundColor = prev;
    }

    private static void WriteWarning(string message)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ForegroundColor = prev;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= maxLength ? value : value[..(maxLength - 2)] + "..";
    }
}
