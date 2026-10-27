# WayFilee architecture

The fork keeps Filee's .NET conversion core and native Avalonia desktop UI. The active solution contains the app, CLI, core, engines, Linux platform implementation, and their tests. It targets Fedora/Nobara KDE Plasma Wayland.

## Desktop

`src/Filee.App/Program.cs` configures Avalonia's native Wayland, Skia, and HarfBuzz backends. The app checks for a KDE Wayland session before showing the UI. It does not select X11 or XWayland. The user interface is AXAML controls and windows; there is no browser or web server.

`src/Filee.App/Services/Triggers/TriggerService.cs` passes gestures to the platform-independent `GestureDetector`. On Linux, `WaylandInputHook` observes input through libinput. It never grabs a device or injects events. This path requires permission to read `/dev/input/event*`. The KWin script in `installer/linux/kwin/wayfilee-donut` positions the radial window at the pointer because an ordinary Wayland client cannot set a global window position.

The input stream has relative motion, so the detector keeps an estimate for the drag threshold. KWin uses the compositor's actual cursor position for placement. Wayland does not provide the active window beneath the pointer to this app, so file manager scope and excluded-app filtering are limited for drag gestures.

## Conversion

`src/Filee.Core` owns settings, presets, paths, and conversion contracts. `src/Filee.Engines` implements converters and discovers optional external engines. The app and CLI both use these projects. Release archives contain the app and CLI; optional engines are installed through the UI or provided by the system.

## Install and update

`build/build-wayland.sh` publishes self-contained `linux-x64` or `linux-arm64` archives. `installer/linux/install.sh` installs under `~/.local/share/wayfilee/application`, with a CLI link, desktop entry, and KWin script. Ownership markers prevent upgrades from replacing unrelated files. `build/build-linux-packages.sh` creates RPM, DEB, and Arch packages under `/opt/wayfilee`, with desktop integration under `/usr/share`. The updater selects a matching asset from GitHub Releases, verifies it against the asset's SHA-256 digest from GitHub's API, and either runs the per-user installer or delegates to DNF, APT, or pacman after the app exits. See the [portability audit](PORTABILITY_AUDIT.md) for validation status.
