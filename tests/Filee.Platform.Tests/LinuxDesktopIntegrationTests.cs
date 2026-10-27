using System.Xml.Linq;
using Filee.Core.Settings;
using Filee.Platform.Linux;

namespace Filee.Platform.Tests;

public sealed class LinuxDesktopIntegrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "Filee.Platform.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void ContextMenuCreatesOnlyDolphinAction()
    {
        var configuration = Path.Combine(_root, "config");
        var data = Path.Combine(_root, "data");
        var integration = new LinuxDesktopIntegration(configuration, data);

        integration.SetContextMenu(true, "/opt/Filee's app/Filee.App", "Filee로 변환 & test");
        integration.SetContextMenu(true, "/opt/new/Filee.App", "Updated");

        var path = Path.Combine(data, "kio", "servicemenus", "wayfilee.desktop");
        Assert.Contains("Exec=\"/opt/new/Filee.App\" --convert %F", File.ReadAllText(path));
        Assert.False(Directory.Exists(Path.Combine(configuration, "Thunar")));
        Assert.False(Directory.Exists(Path.Combine(data, "nautilus")));

        integration.SetContextMenu(false, "", "");

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void ExistingThunarConfigurationIsNotTouched()
    {
        var configuration = Path.Combine(_root, "config");
        var path = Path.Combine(configuration, "Thunar", "uca.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "<actions><action>");

        new LinuxDesktopIntegration(configuration, Path.Combine(_root, "data"))
            .SetContextMenu(true, "/opt/Filee.App", "Convert");
        Assert.Equal("<actions><action>", File.ReadAllText(path));
        Assert.True(File.Exists(Path.Combine(_root, "data", "kio", "servicemenus", "wayfilee.desktop")));
    }

    [Fact]
    public void ForeignDolphinActionIsNotReplacedOrRemoved()
    {
        var path = Path.Combine(_root, "data", "kio", "servicemenus", "wayfilee.desktop");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "[Desktop Entry]\nName=Other application\n");
        var integration = new LinuxDesktopIntegration(Path.Combine(_root, "config"), Path.Combine(_root, "data"));

        Assert.Throws<IOException>(() => integration.SetContextMenu(true, "/opt/WayFilee", "Convert"));
        integration.SetContextMenu(false, "", "");
        Assert.Contains("Other application", File.ReadAllText(path));
    }

    [Fact]
    public void AutostartEscapesLineBreaksAndKeepsTheExecutableAsOneArgument()
    {
        var integration = new LinuxDesktopIntegration(_root, Path.Combine(_root, "data"));
        integration.SetAutostart(true, "/opt/Filee test/Filee.App");
        var path = Path.Combine(_root, "autostart", "wayfilee.desktop");
        Assert.Contains("Exec=\"/opt/Filee test/Filee.App\" --background\n", File.ReadAllText(path));
        Assert.Throws<ArgumentException>(() => integration.SetAutostart(true, "/opt/Filee\nExec=bad"));
        Assert.Contains("Exec=\"/opt/Filee test/Filee.App\" --background\n", File.ReadAllText(path));
        integration.SetAutostart(false, "");
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void DesktopArgumentsEscapeBothDesktopParsingLayers()
    {
        Assert.Equal("\"/tmp/a\\\\\"b\\\\$c\\\\`d\\\\\\\\e%%f\"",
            LinuxDesktopIntegration.DesktopArgument("/tmp/a\"b$c`d\\e%f"));
        Assert.Equal("'/tmp/a'\\''b $c'", LinuxDesktopIntegration.ShellArgument("/tmp/a'b $c"));
        Assert.Equal("label\\nExec=bad", LinuxDesktopIntegration.DesktopValue("label\nExec=bad"));
    }

    [Fact]
    public void ShortcutUpdatePreservesOthersAndIsIdempotent()
    {
        const string original = "; other comment\n(gtk_accel_path \"<Actions>/ThunarWindow/open\" \"<Control>o\")\n";
        var updated = LinuxDesktopIntegration.UpdateThunarShortcut(original, "<Control><Alt>space");
        Assert.StartsWith(original, updated);
        Assert.Contains("\"<Control><Alt>space\")\n", updated);
        Assert.Equal(updated, LinuxDesktopIntegration.UpdateThunarShortcut(updated, "<Control><Alt>space"));
        Assert.Equal(original, LinuxDesktopIntegration.UpdateThunarShortcut(updated, null));
        Assert.Throws<InvalidOperationException>(() => LinuxDesktopIntegration.UpdateThunarShortcut(original, "<Primary>o"));
        Assert.Throws<InvalidOperationException>(() => LinuxDesktopIntegration.UpdateThunarShortcut(
            original.Replace("<Control>o", "<Alt><Control>space", StringComparison.Ordinal), "<Control><Alt>space"));
        Assert.Throws<ArgumentException>(() => LinuxDesktopIntegration.UpdateThunarShortcut(original, "space\")\n(bad)"));
    }

    [Theory]
    [InlineData("Backslash", "backslash")]
    [InlineData("BackSlash", "backslash")]
    [InlineData("NumPad0", "KP_0")]
    [InlineData("NumPad7", "KP_7")]
    [InlineData("NumPad9", "KP_9")]
    [InlineData("PrintScreen", "Print")]
    [InlineData("Pause", "Pause")]
    [InlineData("PageDown", "Page_Down")]
    [InlineData("Space", "space")]
    public void RecordedKeysHaveGtkAccelerators(string recorded, string expected)
    {
        var gesture = new TriggerGesture
        {
            Kind = TriggerKind.KeyChord,
            Modifiers = ModifierKeys.Ctrl | ModifierKeys.Alt,
            Key = recorded,
        };
        var accelerator = LinuxDesktopIntegration.ToThunarAccelerator(gesture);
        Assert.Equal("<Control><Alt>" + expected, accelerator);
        var contents = LinuxDesktopIntegration.UpdateThunarShortcut("", accelerator);
        Assert.Contains("\"" + accelerator + "\"", contents);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
