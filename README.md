# WayFilee

WayFilee is a KDE Plasma Wayland fork of [Filee](https://github.com/KnifeLemon/Filee), the file converter with a donut toolbar. It keeps Filee's conversion core and native Avalonia app, then adapts the desktop integration for Wayland. There is no browser-based UI or X11 fallback.

Fedora and Nobara KDE are the main test environment. The goal is to work on other Linux distributions running KDE Plasma Wayland, including Ubuntu, Debian, and Arch. Those distributions still need installation and upgrade testing.

## What changed from Filee

| Area | WayFilee |
| --- | --- |
| Desktop | Runs on KDE Plasma Wayland with Avalonia's native Wayland backend. Windows, macOS, X11, and other compositors are outside the current target. |
| Donut toolbar | A KWin script places the donut at the pointer. Global mouse gestures use libinput; the donut also opens from the app's drop zone or Dolphin's **Convert with WayFilee** action. |
| Appearance | WayFilee branding and a new SVG-based icon. Material You (M3) is the default for new settings. The donut has Instant, Fast, Normal, and Slow presets for its opening and closing animations. |
| Linux integration | A per-user installer adds the desktop launcher, CLI link, and KWin script. Release archives are self-contained for `linux-x64` and `linux-arm64`. |
| Window behavior | The donut closes on focus loss, and background startup stays out of the taskbar until a window is needed. Navigation between the heavier settings pages reuses their view models. |

The smaller changes you can notice in the app are listed in [User-visible changes](docs/USER_VISIBLE_CHANGES.md). Filee's existing presets and conversion engines remain the foundation; see [Engines](docs/ENGINES.md) for optional tools.

WayFilee keeps the everyday Filee workflow: choose conversion presets on the donut, convert multiple files, review recent results, and set up watched folders. Some formats need an optional engine; the app's **Engines** page shows what is available.

## Getting started

Download a WayFilee Linux archive when one is available, extract it, and run `./Filee` to try the app. Run `./install.sh` from the extracted directory to install it for your user. The installer also installs a `wayfilee` command in `~/.local/bin` (add that directory to `PATH` if needed). RPM, DEB, and Arch packages are also built from the same source for system installation.

```sh
./install.sh
```

For later archives, run `./install.sh --upgrade`. Run `./uninstall.sh` to remove the installation. Read [Linux installation notes](installer/linux/INSTALL.txt) for dependencies, KWin setup, and permission details. The bundled .NET runtime does not include optional conversion engines.

To use the donut, drop files on the Home page, use the Dolphin context action, or enable a global drag gesture in **Shortcuts**. Global gestures require read access to `/dev/input/event*`; the app does not change that permission. Granting your account access to the `input` group can expose all keyboard and pointer input to programs running as that account. The drop zone and Dolphin action work without it. The global gesture path has been exercised on the main Fedora/Nobara KDE setup, but needs testing across other KDE distributions and Plasma versions.

Wayland does not give WayFilee the identity of the window under the pointer. A file-manager-scoped drag gesture can therefore start over another app, and excluded-app rules cannot reliably filter global drag gestures. Choose a modifier in **Shortcuts** that does not interfere with your other apps.

## Packages and platform status

The repository builds self-contained `.tar.gz`, `.rpm`, `.deb`, and `.pkg.tar.zst` artifacts with SHA256 checksums for x64 and arm64 Linux. Fedora/Nobara KDE is the primary tested setup; the other distributions need installation and upgrade testing. See the [portability audit](docs/PORTABILITY_AUDIT.md) for the distro validation matrix and package requirements.

The archive installation can update itself from a verified WayFilee release. System packages download their matching package from GitHub Releases, verify its checksum, and ask APT, DNF, or pacman to install it with administrator authentication. The app never writes directly into package-owned files. GitHub-hosted packages will not appear in ordinary Discover or system update checks without a repository.

To install a downloaded package manually, use `sudo dnf install ./WayFilee-*.rpm` on Fedora/Nobara, `sudo apt install ./WayFilee-*.deb` on Ubuntu/Debian, or `sudo pacman -U ./WayFilee-*.pkg.tar.zst` on Arch after your normal full system update. System packages put the app under `/opt/wayfilee` and its launcher, icon, and KWin script under `/usr/share`. In KDE System Settings, confirm that **WayFilee donut at the cursor** is enabled after installation; sign out and in if KWin does not load it immediately. Do not install the system package and the per-user archive installation simultaneously, since they register the same desktop app ID.

## Build from source

The project uses .NET 10:

```sh
dotnet build Filee.slnx
dotnet test Filee.slnx
bash build/build-wayland.sh
bash build/build-linux-packages.sh
```

The last two commands create the release archive and distro packages under `Releases/`. See [Architecture](docs/ARCHITECTURE.md) for the Wayland integration.

## Credits and license

WayFilee is based on [Filee](https://github.com/KnifeLemon/Filee). The original project can be supported through [KnifeLemon's GitHub Sponsors page](https://github.com/sponsors/KnifeLemon). See [LICENSE](LICENSE) and [third-party notices](THIRD-PARTY-NOTICES.md).

<!-- sponsors: src/Filee.App/Assets/Sponsors/sponsors.json -->
