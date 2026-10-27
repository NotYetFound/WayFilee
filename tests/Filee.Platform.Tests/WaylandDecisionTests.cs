// The Wayland decisions that only depend on environment values (#367): which compositors can place the donut, and
// when the native backend is used.

using Filee.Platform.Linux;

namespace Filee.Platform.Tests;

public sealed class WaylandDecisionTests
{
    [Theory]
    [InlineData("KDE", true)]
    [InlineData("kde", true)]
    [InlineData("KDE:GNOME", true)]
    [InlineData("Hyprland", false)]
    [InlineData("GNOME", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void KDE_can_place_the_donut(string? xdgCurrentDesktop, bool expected)
    {
        var result = WaylandSupport.IsSupportedCompositor(xdgCurrentDesktop);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, "wayland-0", true)]
    [InlineData(true, "wayland-1", true)]
    [InlineData(false, "wayland-0", false)]
    [InlineData(true, null, false)]
    [InlineData(true, "", false)]
    public void The_native_backend_needs_Linux_and_a_display(
        bool isLinux, string? waylandDisplay, bool expected)
    {
        var result = WaylandSupport.IsNativeWaylandAvailable(isLinux, waylandDisplay);
        Assert.Equal(expected, result);
    }
}
