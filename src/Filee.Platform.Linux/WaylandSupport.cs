// Decisions about Wayland that only depend on environment values, kept apart from the Linux services so the app can ask
// them on every platform and tests can feed them values.

namespace Filee.Platform.Linux;

public static class WaylandSupport
{
    /// <summary>
    /// Whether this Linux process has a Wayland display to connect to.
    /// </summary>
    public static bool IsNativeWaylandAvailable(bool isLinux, string? waylandDisplay) =>
        isLinux
        && !string.IsNullOrEmpty(waylandDisplay);

    /// <summary><see cref="IsNativeWaylandAvailable(bool, string?)"/> for this process.</summary>
    public static bool NativeWaylandRequested => IsNativeWaylandAvailable(OperatingSystem.IsLinux(),
        Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));

    /// <summary>
    /// Whether KDE Plasma can place the donut using the bundled KWin script.
    /// </summary>
    public static bool IsSupportedCompositor(string? xdgCurrentDesktop)
    {
        var desktops = (xdgCurrentDesktop ?? "").Split(':');
        return desktops.Contains("KDE", StringComparer.OrdinalIgnoreCase);
    }
}
