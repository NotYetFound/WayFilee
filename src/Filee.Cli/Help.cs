// Usage text of the "wayfilee" command.

using System.Reflection;
using Filee.Core.Watching;

namespace Filee.Cli;

internal static class Help
{
    public static string Version =>
        typeof(Help).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "?";

    public static string Text(string? command) => command?.ToLowerInvariant() switch
    {
        "convert" => Convert,
        "watch" => Watch,
        "formats" => "Usage: wayfilee formats [<format>]\n\nLists every format, or what one format converts to.\n",
        "presets" => "Usage: wayfilee presets\n\nLists the presets saved in WayFilee (built-in and your own).\n",
        _ => General,
    };

    private const string General = """
        WayFilee: convert files offline from the command line.

        Usage:
          wayfilee convert <files or folders...> --to <format> [options]
          wayfilee convert <files or folders...> --preset <preset> [options]
          wayfilee watch <folder> --to <format> [options]
          wayfilee formats [<format>]
          wayfilee presets
          wayfilee help [<command>]
          wayfilee version

        Examples:
          wayfilee convert photo.heic --to jpg
          wayfilee convert *.png --to webp --quality 80 -o converted
          wayfilee convert /home/me/Scans --recursive --preset to-pdf
          wayfilee watch /home/me/Inbox --to pdf --move-originals

        Presets and engines are the ones in the WayFilee app. Exit codes: 0 all converted, 1 some failed,
        2 wrong arguments or a missing input, 3 nothing to convert.

        """;

    private const string Convert = """
        Usage: wayfilee convert <files or folders...> (--to <format> | --preset <preset>) [options]

        Files can be wildcards (*.heic). Folders convert the files in them that WayFilee knows.

        Options:
          -t, --to <format>      Target format, e.g. jpg, png, pdf, docx, mp4 (see 'wayfilee formats')
          -p, --preset <preset>  A preset from the app, by id or name (see 'wayfilee presets')
          -o, --output <folder>  Save there instead of next to the sources
          -r, --recursive        Include sub folders
          -q, --quality <1-100>  Image quality for JPG, WebP, AVIF, …
              --overwrite        Replace existing output files (default: add a number)
              --skip             Leave existing output files alone
              --keep-dates       Give converted files the created and modified dates of the originals
              --zip              Save the result of each file as one ZIP (a Shapefile's files together)
              --json             Print the result as JSON (for scripts and other programs)
              --quiet            Print nothing but errors

        """;

    private static string Watch => FormattableString.Invariant($"""
        Usage: wayfilee watch <folder> (--to <format> | --preset <preset>) [options]

        Converts files that land in the folder until Ctrl+C. Files are converted once they are complete (no
        longer growing or open in another program). Output goes to a "converted" folder inside it unless -o is given.

        Options:
          -t, --to <format>      Target format
          -p, --preset <preset>  A preset from the app, by id or name
          -o, --output <folder>  Where converted files go (default: <folder>/converted)
          -r, --recursive        Also watch sub folders (their files keep the sub folder in the output)
              --move-originals   Move converted sources to <folder>/originals; files already waiting are
                                 converted at start
              --settle <seconds> How long a file must stay unchanged (default {FolderWatcher.DefaultSettleTime.TotalSeconds})
              --keep-dates       Give converted files the created and modified dates of the originals
              --zip              Save the result of each file as one ZIP (a Shapefile's files together)
              --quiet            Print nothing but errors

        """);
}
