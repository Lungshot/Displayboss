# DisplayBoss

Save and switch between Windows display configurations with a single click. No more manually enabling/disabling monitors in Display Settings every day.

![License](https://img.shields.io/github/license/Lungshot/DisplayBoss?cacheSeconds=3600)
![Release](https://img.shields.io/github/v/release/Lungshot/DisplayBoss?cacheSeconds=3600)

## The Problem

You have multiple monitors at your desk. When you leave the office and remote in, you want only one active display. When you're back, you want all four. Windows makes you go into Display Settings and toggle each monitor individually. Every. Single. Day.

## The Solution

DisplayBoss saves your monitor layouts as profiles. Switch between them from the system tray in one click.

- **"Office"** - All 4 monitors, two in portrait, primary on the left
- **"Remote"** - Just the primary monitor

That's it.

## Features

- **System tray app** - right-click to switch profiles
- **CLI tool** - `displayboss-cli save "Office"` / `displayboss-cli load "Remote"`
- **EDID-based matching** - identifies monitors by hardware ID, not port. Works across reboots and cable swaps
- **Rotation** - preserves portrait/landscape/flipped orientation per monitor
- **Position** - restores monitor arrangement (which is left, right, above, etc.)
- **Undo** - instantly revert the last profile switch
- **Start with Windows** - optional auto-start

## Install

Download from [Releases](https://github.com/Lungshot/DisplayBoss/releases):

| File | Description |
|------|-------------|
| **DisplayBoss-1.0.0-Setup.exe** | Installer. Handles .NET runtime, Start Menu shortcut, optional startup. |
| **DisplayBoss-1.0.0-Portable.zip** | Extract and run. |

Requires Windows 10/11 (x64) and [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (the installer downloads it automatically if missing).

## Usage

### Tray App

1. Run `DisplayBoss.exe` - it appears in the system tray
2. Set up your monitors the way you want them
3. Right-click tray icon > **Save Current as Profile...**
4. Repeat for each layout you use
5. Click any profile name to switch

### CLI

```
displayboss-cli save "Office" -d "All 4 monitors active"
displayboss-cli save "Remote" -d "Primary only" -f
displayboss-cli load "Office"
displayboss-cli load "Remote"
displayboss-cli current
displayboss-cli list
displayboss-cli undo
displayboss-cli delete "Old Profile" --yes
```

## How It Works

DisplayBoss uses the Windows [Connecting and Configuring Displays (CCD)](https://learn.microsoft.com/en-us/windows-hardware/drivers/display/ccd-apis) API via P/Invoke:

- `QueryDisplayConfig` enumerates all connected displays
- `DisplayConfigGetDeviceInfo` reads EDID data (manufacturer, product code) for hardware-based matching
- `SetDisplayConfig` applies the saved configuration

Profiles are stored as JSON in `%AppData%\DisplayBoss\Profiles\`.

## Building from Source

```
dotnet build
dotnet run --project src/DisplayBoss.Cli -- current
dotnet run --project src/DisplayBoss.Tray
```

Requires [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

## Project Structure

```
DisplayBoss.sln
src/
  DisplayBoss.Core/       # Class library: P/Invoke, models, services
  DisplayBoss.Cli/        # Console app
  DisplayBoss.Tray/       # WinForms system tray app
installer/
  DisplayBoss.iss         # Inno Setup installer script
```

## License

[MIT](LICENSE)
