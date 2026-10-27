// Moving an original to the Recycle Bin / Trash for "Replace the original" (#332): it works where there is one, and
// anything else leaves the file where it is with a reason.

using Filee.Platform.Linux;

// Each test checks the OS it needs; the mount and missing-file checks are plain string and file work.
#pragma warning disable CA1416

namespace Filee.Platform.Tests;

public sealed class TrashTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "filee-trash-" + Guid.NewGuid().ToString("N"));

    public TrashTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void A_mount_holds_only_its_own_folders()
    {
        string[] roots = ["/", "/home", "/media/usb"];

        Assert.Equal("/home", FreedesktopTrash.MountOf("/home/ana/a.png", roots));
        Assert.Equal("/", FreedesktopTrash.MountOf("/home2/ana/a.png", roots));
        Assert.Equal("/media/usb", FreedesktopTrash.MountOf("/media/usb/a.png", roots));
        Assert.Equal("/", FreedesktopTrash.MountOf("/media/usb2/a.png", roots));
    }

    [Fact]
    public void A_missing_file_gives_a_reason()
    {
        var missing = Path.Combine(_root, "gone.png");
        Assert.NotNull(FreedesktopTrash.Move(missing, Path.Combine(_root, "Trash")));
    }

    [Fact]
    public void The_freedesktop_Trash_records_where_the_file_came_from()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("Requires Linux paths.");
            return;
        }
        var trash = Path.Combine(_root, "Trash");
        var first = Path.Combine(_root, "my photo.png");
        File.WriteAllText(first, "1");
        Assert.Null(FreedesktopTrash.Move(first, trash));
        File.WriteAllText(first, "2");
        Assert.Null(FreedesktopTrash.Move(first, trash));

        Assert.False(File.Exists(first));
        Assert.Equal("1", File.ReadAllText(Path.Combine(trash, "files", "my photo.png")));
        Assert.Equal("2", File.ReadAllText(Path.Combine(trash, "files", "my photo 2.png")));
        var info = File.ReadAllText(Path.Combine(trash, "info", "my photo 2.png.trashinfo"));
        Assert.StartsWith("[Trash Info]\nPath=" + FreedesktopTrash.Escape(first) + "\nDeletionDate=", info);
        Assert.Contains("my%20photo.png", info);
        foreach (var folder in new[] { trash, Path.Combine(trash, "files"), Path.Combine(trash, "info") })
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute, File.GetUnixFileMode(folder));

        // A folder of that name in the Trash is in the way too.
        Directory.CreateDirectory(Path.Combine(trash, "files", "my photo 3.png"));
        File.WriteAllText(first, "3");
        Assert.Null(FreedesktopTrash.Move(first, trash));
        Assert.Equal("3", File.ReadAllText(Path.Combine(trash, "files", "my photo 4.png")));
    }

}
