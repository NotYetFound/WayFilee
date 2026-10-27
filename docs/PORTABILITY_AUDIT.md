# KDE Linux portability audit

WayFilee targets KDE Plasma Wayland on Linux x64 and arm64. Fedora/Nobara is the main tested setup. Ubuntu, Debian, Arch, and other Plasma distributions are targets, but are not yet validated end to end.

| Area | Current implementation | Package/release gate |
| --- | --- | --- |
| UI and compositor | Native Avalonia Wayland backend; KDE session check; KWin script positions the donut | Test Plasma 6 on each target, including donut placement, focus dismissal, icons, and tray startup. X11 sessions are unsupported. |
| Runtime | Self-contained .NET 10 `linux-x64` and `linux-arm64` publish | Build on an old enough glibc baseline and inspect native dependencies with `ldd` and `build/check-elf-glibc.py`. Self-contained publish still needs system graphics/input libraries. |
| Desktop integration | XDG desktop entry/autostart, Dolphin service menu, KWin script | Packages own `/opt/wayfilee` and their `/usr/share` integration. Test coexistence with the per-user archive installation and KWin script enabling on each Plasma release. |
| Input gestures | libinput reads `/dev/input/event*`; explicit device permission is required | Test device access and hotplug on each target. Do not silently grant the `input` group. Drop zone and Dolphin action work without global gestures. |
| Reduced motion | Reads Plasma's animation factor through `kreadconfig6` | Test the Plasma animation slider at normal and instant settings. |
| External tools | `xdg-open`, `gdbus`, `kreadconfig6`, KWin's script tools, Python 3, libinput, Wayland client and font libraries | Express dependencies in each package format using that distro's package names. Test on clean installs. Optional conversion engines stay optional. |
| Updates | Per-user archive installer verifies SHA256 and upgrades its own files. Package builds fetch matching GitHub Release assets and hand them to DNF/APT/pacman after exiting. Portable copies open GitHub Releases. | Test a real N→N+1 package upgrade with administrator authentication on each distribution. Never run the archive installer against package-owned files. |

## GitHub Release update contract

The app checks GitHub Releases for a newer version and shows release notes. Package builds select the matching RPM, DEB, or Arch asset for their architecture, download it to a private workspace, verify its SHA256 release checksum, and ask `pkexec` to run DNF, APT, or pacman after the app exits. The package manager owns the transaction; the app does not copy package files into `/opt` or `/usr`. If authentication or installation fails, the helper restarts the previous app and records an update failure marker.

Package builds use `-p:WayFileePackageKind=deb`, `rpm`, or `arch` so the assembly identifies its package format. They must not include the `.wayfilee-package` marker used by the per-user archive installer. The package manager owns `/opt/wayfilee`, `/usr/bin/wayfilee`, and `/usr/share` integration. CI should inspect emitted assembly metadata, then install version N and upgrade to N+1 on a clean system before claiming the in-app update is proven on that distribution.

GitHub Release assets are not an APT, DNF, or pacman repository. Discover and ordinary system update checks will not list WayFilee updates through this route. The app needs `pkexec` and a working graphical PolicyKit agent for authentication. Arch users should keep the rest of their system current before installing a locally downloaded package; the app does not refresh pacman's sync database or run a full system upgrade.

## Validation status

- **Fedora/Nobara KDE:** primary interactive test environment; existing archive installer and in-app archive update path have automated tests.
- **Ubuntu/Debian KDE:** target, not yet validated on clean systems.
- **Arch KDE:** target, not yet validated on a clean system.
- **RPM x64:** package metadata, contents, and checksum were inspected locally. WayFilee 2.1.0 was installed and launched on Nobara KDE, with its autostart, Dolphin action, and KWin script paths checked. A synthetic 2.1.1 RPM passed a DNF/RPM upgrade dry run; an actual N→N+1 in-app GitHub update remains untested.
- **DEB and Arch x64:** packages build locally; content inspection only. Clean-system install and upgrades remain untested.
- **arm64:** CI build configured; local machine cannot run arm64 packages. Release workflow and installed behavior remain unverified.

For each target, validate first launch, normal and instant animation settings, Dolphin action, KWin placement, optional input gestures, a conversion with bundled tools, archive upgrade, and package install/upgrade/removal. Record the exact distribution and Plasma versions, library versions, and the in-app GitHub package upgrade result.
