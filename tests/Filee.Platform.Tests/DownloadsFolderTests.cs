// Where files dragged from a browser are saved by default: the user's Downloads folder, as each platform knows it.

using Filee.Core.Platform;
using Filee.Platform.Linux;

namespace Filee.Platform.Tests;

public class DownloadsFolderTests
{
    [Fact]
    public void Linux_reads_a_localized_downloads_folder_from_user_dirs()
    {
        const string userDirs = "# written by xdg-user-dirs-update\nXDG_DESKTOP_DIR=\"$HOME/바탕화면\"\nXDG_DOWNLOAD_DIR=\"$HOME/다운로드\"\n";
        Assert.Equal("/home/me/다운로드", XdgUserDirs.Download(userDirs, "/home/me"));
        Assert.Equal("/data/dl", XdgUserDirs.Download("XDG_DOWNLOAD_DIR=\"/data/dl\"", "/home/me"));
        // "$HOME/" alone turns the folder off; a relative path or no line at all means nothing.
        Assert.Null(XdgUserDirs.Download("XDG_DOWNLOAD_DIR=\"$HOME/\"", "/home/me"));
        Assert.Null(XdgUserDirs.Download("XDG_DOWNLOAD_DIR=\"dl\"", "/home/me"));
        Assert.Null(XdgUserDirs.Download("XDG_MUSIC_DIR=\"$HOME/Music\"", "/home/me"));
    }

    [Fact]
    public void Linux_names_an_absolute_downloads_folder()
    {
        var platform = new LinuxPlatformServices();
        Assert.True(Path.IsPathFullyQualified(platform.DownloadsFolder));
    }
}
