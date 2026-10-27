# Contributing to WayFilee

WayFilee targets Fedora and Nobara KDE Plasma on Wayland. Keep the desktop app native Avalonia and Linux only. Changes to the radial UI should work with the native Wayland backend; do not add X11 or browser rendering paths.

## Development

Install the .NET 10 SDK, then:

```sh
 dotnet restore Filee.slnx
 dotnet build Filee.slnx
 dotnet test Filee.slnx
```

`bash build/build-wayland.sh` creates a self-contained Linux archive in `Releases/`. The application and CLI are published together. Optional conversion engines are installed through the app or provided by the system.

For local application testing, set `FILEE_DATA_DIR` to a scratch directory so development settings and single-instance state stay separate from an installed copy. For native GUI testing use a KDE Plasma Wayland session. Gesture testing requires explicit read access to `/dev/input/event*`; see [INSTALL.txt](installer/linux/INSTALL.txt). Do not run the app as root.

The install scripts should be tested with a scratch `HOME` and `XDG_DATA_HOME` before changing release packaging. Keep ownership markers and rollback behavior intact so upgrades never replace another application's files.
