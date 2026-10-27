// Renders PNG and ICO compatibility assets from the editable WayFilee SVG.
// The SVG source is generated from design/wayfilee-icon/tmp/work/spec.json.
// Run from the repository root: dotnet run build/tools/make-icon.cs
#:package Magick.NET-Q8-AnyCPU

using ImageMagick;

var directory = Path.Combine("src", "Filee.App", "Assets", "Icons");
var svg = Path.Combine(directory, "filee.svg");
using var master = new MagickImage(File.ReadAllBytes(svg), new MagickReadSettings
{
    BackgroundColor = MagickColors.Transparent,
    Format = MagickFormat.Svg,
});
master.Write(Path.Combine(directory, "filee.png"), MagickFormat.Png);
using (var sidebar = master.Clone())
{
    sidebar.FilterType = FilterType.Mitchell;
    sidebar.Resize(96, 96);
    sidebar.Write(Path.Combine(directory, "filee-sidebar.png"), MagickFormat.Png);
}
master.Settings.SetDefine(MagickFormat.Icon, "auto-resize", "256,64,48,32,24,16");
master.Write(Path.Combine(directory, "filee.ico"), MagickFormat.Icon);
Console.WriteLine($"Icons rendered from {svg}");
