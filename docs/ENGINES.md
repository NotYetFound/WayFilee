# Conversion engines on KDE Wayland

WayFilee keeps Filee's .NET conversion engine system. Some conversions run entirely in managed code. Others use optional native tools such as LibreOffice, FFmpeg, ImageMagick, Tesseract, Ghostscript, Pandoc, or 7-Zip. The app's **Engines** page shows which tools are available and lets you install supported downloads.

The Linux release archive contains the app and CLI, with no optional engine bundles. This keeps the base download smaller and lets Fedora/Nobara users provide converters from system packages. Format availability depends on the engines present on the machine.

The engine manifest is `src/Filee.Engines/Infrastructure/engines.json`. Add or update a Linux entry there with a pinned URL and SHA256 hash when introducing a downloadable engine. Engine discovery and download paths should not assume Windows or macOS executables. Changes to converters belong in `src/Filee.Engines`, with a focused conversion test when a real format can regress.

Wayland affects the desktop UI and drag detection, not the conversion pipeline. A CLI conversion should behave the same in a terminal without a graphical session.
