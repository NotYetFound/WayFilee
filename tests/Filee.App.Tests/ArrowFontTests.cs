using System.Globalization;
using Avalonia.Headless.XUnit;
using Avalonia.Media;

namespace Filee.App.Tests;

public sealed class ArrowFontTests
{
    [AvaloniaFact]
    public void Arrow_labels_resolve_to_a_real_glyph()
    {
        TestServices.EnsureInitialized("en");
        var fonts = FontManager.Current;
        Assert.True(fonts.TryMatchCharacter('→', FontStyle.Normal, FontWeight.Normal,
            FontStretch.Normal, new FontFamily("Noto Sans"), CultureInfo.InvariantCulture, out var face));
        Assert.True(fonts.TryGetGlyphTypeface(face, out var glyphs));
        Assert.True(glyphs.SupportedUnicodeRange.IsInRange('→'));
    }
}
