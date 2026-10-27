# User-visible changes from Filee

This list describes WayFilee 2.1.0 relative to the Filee version it was forked from. It covers fork changes a user may notice; it is not a list of all conversions inherited from Filee or a promise that later upstream Filee releases have the same behavior.

## Desktop and launching

- The app, windows, tray, desktop launcher, CLI help, and installer use the **WayFilee** name. Some packaged executable filenames remain `Filee` and `filee-cli`; the installed terminal command is `wayfilee`.
- The desktop UI uses Avalonia's native Wayland backend. It does not open a web page or run a local web server for its UI.
- The app requires a KDE Plasma Wayland session. It has no X11 or XWayland fallback, and the active solution no longer builds Windows or macOS editions.
- The archive installer works per user. It adds a desktop entry, a `~/.local/bin/wayfilee` link, and the WayFilee KWin script. Upgrade and uninstall scripts are included in the archive. System packages install under `/opt/wayfilee` with integration under `/usr/share`.
- Starting in the background does not create an empty taskbar window. The main window appears when opened, and conversion progress creates a toast only when there is a job to show.

## Opening and using the donut

- Global mouse gestures on KDE Wayland read input through libinput. They require access to `/dev/input/event*` and do not grab the device or move the pointer. WayFilee does not grant itself that access.
- A KWin script places the donut at the real cursor position. The menu uses a borderless window and closes when it loses focus, so it should not leave an empty taskbar entry after you click elsewhere.
- The Home page drop zone and Dolphin's **Convert with WayFilee** context action provide ways to open the donut without global input access.
- **Donut toolbar → Appearance → Animation speed** offers Instant, Fast, Normal, and Slow. This changes only the donut's appear and disappear animations. **Theme → Reduce motion** still takes precedence.
- Wayland does not expose the window beneath the pointer to this app. File-manager scope and excluded-app filtering are therefore limited for global drag gestures; the gesture may begin over another application.

## Look and responsiveness

- The WayFilee icon adds a Wayland-inspired W to Filee's mark. Its source is SVG, with raster copies for places that need them.
- KDE Plasma matches WayFilee windows to its desktop launcher, so the taskbar uses the WayFilee icon instead of a generic Wayland icon.
- Material You (M3) is the default theme style for new settings. Existing saved theme choices are kept.
- Settings navigation reuses the Donut toolbar and Presets view models to reduce stutter when switching between those heavier pages.
- Preset and menu arrow symbols use a system symbol font fallback, and the donut's flip arrow is drawn directly. This addresses empty rectangular glyphs seen on the main Fedora/Nobara setup.
- The app includes fixes for intermittent donut borders, delayed opening, focus dismissal, and stray blank windows. Window behavior still needs validation on other Plasma versions.
- The automatic “Is Filee helping you?” feedback pop-up was removed. Support and issue links remain available from About.

## Releases and limits

- The build script produces self-contained `linux-x64` and `linux-arm64` `.tar.gz` archives with SHA256 checksums. It omits the old X11 native hook libraries from the archive.
- Optional converters such as FFmpeg or LibreOffice are not bundled. The **Engines** page can install supported downloads, or use system-provided tools.
- `.deb`, `.rpm`, and Arch packages are built alongside the archives. The RPM x64 package has been installed and launched on Nobara KDE; Ubuntu, Debian, Arch, and other KDE Wayland distributions remain targets for clean-system installation and upgrade testing.

For setup, dependencies, and the implications of input access, see [Linux installation notes](../installer/linux/INSTALL.txt). For inherited Filee features and changes, see the [upstream project](https://github.com/KnifeLemon/Filee).
