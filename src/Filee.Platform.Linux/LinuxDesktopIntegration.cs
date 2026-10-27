using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Filee.Core.Settings;

namespace Filee.Platform.Linux;

internal sealed class LinuxDesktopIntegration(string configHome, string dataHome)
{
    internal const string ThunarActionId = "wayfilee-convert";
    internal const string ThunarAcceleratorPath = "<Actions>/ThunarActions/uca-action-" + ThunarActionId;

    internal static string? ToThunarAccelerator(TriggerGesture gesture)
    {
        if (!gesture.Enabled || gesture.Kind != TriggerKind.KeyChord || string.IsNullOrEmpty(gesture.Key))
            return null;
        var key = gesture.Key switch
        {
            "Space" => "space",
            "Enter" => "Return",
            "Backspace" => "BackSpace",
            "PageUp" => "Page_Up",
            "PageDown" => "Page_Down",
            "Semicolon" => "semicolon",
            "Equals" => "equal",
            "Comma" => "comma",
            "Minus" => "minus",
            "Period" => "period",
            "Slash" => "slash",
            "BackQuote" => "grave",
            "OpenBracket" => "bracketleft",
            "Backslash" or "BackSlash" => "backslash",
            "CloseBracket" => "bracketright",
            "Quote" => "apostrophe",
            "PrintScreen" => "Print",
            "Pause" => "Pause",
            "Tab" or "Escape" or "Delete" or "Insert" or "Home" or "End" or "Left" or "Right" or "Up" or "Down" => gesture.Key,
            { Length: 1 } single when char.IsAsciiLetterOrDigit(single[0]) => single.ToLowerInvariant(),
            { Length: 7 } numberPad when numberPad.StartsWith("NumPad", StringComparison.Ordinal) && char.IsAsciiDigit(numberPad[6]) => "KP_" + numberPad[6],
            var function when function.StartsWith('F') && int.TryParse(function.AsSpan(1), out var number) && number is >= 1 and <= 24 => function,
            _ => throw new NotSupportedException($"The key '{gesture.Key}' has no Thunar shortcut mapping."),
        };
        return (gesture.Modifiers.HasFlag(ModifierKeys.Ctrl) ? "<Control>" : "")
            + (gesture.Modifiers.HasFlag(ModifierKeys.Alt) ? "<Alt>" : "")
            + (gesture.Modifiers.HasFlag(ModifierKeys.Shift) ? "<Shift>" : "")
            + (gesture.Modifiers.HasFlag(ModifierKeys.Meta) ? "<Super>" : "") + key;
    }

    public bool SetThunarShortcut(string? accelerator)
    {
        var path = Path.Combine(configHome, "Thunar", "accels.scm");
        var contents = File.Exists(path) ? File.ReadAllText(path) : "";
        var updated = UpdateThunarShortcut(contents, accelerator);
        if (contents == updated)
            return false;
        Write(path, updated);
        return true;
    }

    internal static string UpdateThunarShortcut(string contents, string? accelerator)
    {
        if (accelerator is not null && !Regex.IsMatch(accelerator,
                @"\A(?:<(?:Control|Primary|Alt|Shift|Super|Meta)>)*[A-Za-z0-9_]+\z", RegexOptions.CultureInvariant))
            throw new ArgumentException("Use a GTK shortcut such as <Control><Alt>space.", nameof(accelerator));
        var bindingPattern = @"(?m)^[ \t]*(?<disabled>;[ \t]*)?\(gtk_accel_path ""(?<path>[^""\r\n]+)"" ""(?<key>[^""\r\n]*)""\)[ \t]*\r?$";
        foreach (Match match in Regex.Matches(contents, bindingPattern, RegexOptions.CultureInvariant))
        {
            if (accelerator is not null && !match.Groups["disabled"].Success
                && match.Groups["path"].Value != ThunarAcceleratorPath
                && string.Equals(NormalizeAccelerator(match.Groups["key"].Value), NormalizeAccelerator(accelerator), StringComparison.Ordinal))
                throw new InvalidOperationException($"Thunar already uses {accelerator} for {match.Groups["path"].Value}.");
        }
        var ownPattern = @"(?m)^[ \t]*;?[ \t]*\(gtk_accel_path """ + Regex.Escape(ThunarAcceleratorPath)
            + @""" ""[^""\r\n]*""\)[ \t]*(?:\r?\n|$)";
        var updated = Regex.Replace(contents, ownPattern, "", RegexOptions.CultureInvariant);
        if (accelerator is not null)
        {
            if (updated.Length > 0 && !updated.EndsWith('\n'))
                updated += "\n";
            updated += $"(gtk_accel_path \"{ThunarAcceleratorPath}\" \"{accelerator}\")\n";
        }
        return updated;
    }

    private static string NormalizeAccelerator(string value)
    {
        var pieces = value.Replace("<Primary>", "<Control>", StringComparison.OrdinalIgnoreCase).ToLowerInvariant().Split('>');
        return string.Join('>', pieces[..^1].Order(StringComparer.Ordinal)) + ">" + pieces[^1];
    }

    public void SetAutostart(bool enabled, string executablePath)
    {
        var path = Path.Combine(configHome, "autostart", "wayfilee.desktop");
        if (!enabled)
        {
            if (IsManaged(path)) File.Delete(path);
            return;
        }
        RefuseForeignFile(path);
        Write(path, $"[Desktop Entry]\nType=Application\nName=WayFilee\nExec={DesktopArgument(executablePath)} --background\nTerminal=false\nX-WayFilee-Managed=true\n");
    }

    public void SetContextMenu(bool enabled, string executablePath, string label)
    {
        var path = Path.Combine(dataHome, "kio", "servicemenus", "wayfilee.desktop");
        if (!enabled)
        {
            if (IsManaged(path)) File.Delete(path);
            return;
        }

        RefuseForeignFile(path);
        var command = $"{DesktopArgument(executablePath)} --convert %F";
        var name = DesktopValue(label);
        Write(path, $"[Desktop Entry]\nType=Service\nMimeType=application/octet-stream;\nActions=convert;\nX-KDE-Protocols=file\nX-WayFilee-Managed=true\n\n[Desktop Action convert]\nName={name}\nIcon=document-save-as\nExec={command}\n", executable: true);
    }

    private static bool IsManaged(string path) =>
        File.Exists(path) && new FileInfo(path).LinkTarget is null &&
        File.ReadAllLines(path).Any(line => line == "X-WayFilee-Managed=true");

    private static void RefuseForeignFile(string path)
    {
        if ((File.Exists(path) || new FileInfo(path).LinkTarget is not null) && !IsManaged(path))
            throw new IOException($"Refusing to replace an existing file not created by WayFilee: {path}");
    }

    internal static void UpdateThunarAction(XDocument document, bool enabled, string executablePath, string label)
    {
        if (document.Root?.Name != "actions")
            throw new InvalidDataException("Thunar's custom actions file has an unexpected root element.");
        document.Root.Elements("action")
            .Where(action => (string?)action.Element("unique-id") == ThunarActionId).Remove();
        if (!enabled)
            return;
        var command = ShellArgument(executablePath).Replace("%", "%%", StringComparison.Ordinal) + " --convert %F";
        document.Root.Add(new XElement("action",
            new XElement("icon", "document-save-as"),
            new XElement("name", label),
            new XElement("unique-id", ThunarActionId),
            new XElement("command", command),
            new XElement("description", label),
            new XElement("patterns", "*"),
            new XElement("audio-files"),
            new XElement("image-files"),
            new XElement("other-files"),
            new XElement("text-files"),
            new XElement("video-files")));
    }

    private static XDocument LoadThunarActions(string path)
    {
        if (File.Exists(path))
            return XDocument.Load(path, LoadOptions.PreserveWhitespace);
        var systemDirectories = (Environment.GetEnvironmentVariable("XDG_CONFIG_DIRS") ?? "/etc/xdg")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var directory in systemDirectories.Where(Path.IsPathFullyQualified))
        {
            var defaults = Path.Combine(directory, "Thunar", "uca.xml");
            if (File.Exists(defaults))
                return XDocument.Load(defaults, LoadOptions.PreserveWhitespace);
        }
        return new XDocument(new XElement("actions"));
    }

    internal static string DesktopArgument(string value)
    {
        ValidateCommandValue(value);
        if (value.Contains('='))
            throw new ArgumentException("Desktop launchers do not support '=' in an executable path.", nameof(value));
        var argument = new StringBuilder("\"");
        foreach (var character in value)
        {
            if (character is '"' or '`' or '$' or '\\')
                argument.Append('\\');
            argument.Append(character);
        }
        argument.Append('"');
        return DesktopValue(argument.ToString()).Replace("%", "%%", StringComparison.Ordinal);
    }

    internal static string ShellArgument(string value)
    {
        ValidateCommandValue(value);
        return "'" + value.Replace("'", "'\\''", StringComparison.Ordinal) + "'";
    }

    internal static string DesktopValue(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal)
        .Replace("\t", "\\t", StringComparison.Ordinal);

    private static void ValidateCommandValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.IndexOfAny(['\0', '\r', '\n']) >= 0)
            throw new ArgumentException("The executable path cannot contain a null character or line break.", nameof(value));
    }

    private static void Write(string path, string contents, bool executable = false)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, contents.ReplaceLineEndings("\n"), new UTF8Encoding(false));
            if (!OperatingSystem.IsWindows())
            {
                var mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
                if (executable)
                    mode |= UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
                File.SetUnixFileMode(temporary, mode);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
