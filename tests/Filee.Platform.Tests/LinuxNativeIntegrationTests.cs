using Filee.Platform.Linux;

namespace Filee.Platform.Tests;

public sealed class LinuxNativeIntegrationTests
{
    [Fact]
    public void DolphinActionQuotesExecutablePathAndLeavesOtherManagersAlone()
    {
        var root = Path.Combine(Path.GetTempPath(), "wayfilee-kio-" + Guid.NewGuid().ToString("N"));
        try
        {
            var data = Path.Combine(root, "data");
            var config = Path.Combine(root, "config");
            var target = Path.Combine(root, "WayFilee's app");
            new LinuxDesktopIntegration(config, data).SetContextMenu(true, target, "Convert");
            var menu = File.ReadAllText(Path.Combine(data, "kio", "servicemenus", "wayfilee.desktop"));
            Assert.Contains("Exec=\"" + target + "\" --convert %F", menu);
            Assert.False(Directory.Exists(Path.Combine(data, "nautilus")));
            Assert.False(Directory.Exists(Path.Combine(data, "Thunar")));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
