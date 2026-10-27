// Removes per-user integration and downloaded engines after Filee exits. User settings remain for reinstalling.

using Filee.Core.Platform;
using Filee.Core.Settings;
using Filee.Platform.Linux;

namespace Filee.App.Services;

internal static class UninstallCleanup
{
    public static bool Run()
    {
        var platform = new LinuxPlatformServices();
        var succeeded = true;
        // A copy with its own data folder (FILEE_DATA_DIR) never registered anything: only its engines go.
        var integrated = UserDataStore.OverrideDirectory is null;
        if (integrated)
        {
            Clean(() => platform.SetContextMenu(false, "", ""));
            Clean(() => platform.SetStartWithSystem(false, ""));
        }
        if (succeeded)
        {
            Clean(() =>
            {
                var engines = Filee.Engines.Infrastructure.EngineEnvironment.DownloadRoot;
                if (Directory.Exists(engines))
                    Directory.Delete(engines, recursive: true);
            });
        }
        return succeeded;

        void Clean(Action action)
        {
            try { action(); }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                succeeded = false;
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
}
