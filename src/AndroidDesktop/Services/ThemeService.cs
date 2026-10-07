using System.Windows;
using System.Windows.Media;

namespace AndroidDesktop.Services;

/// <summary>Semantic, opaque theme resources shared by every application surface.</summary>
public sealed class ThemeService
{
    public IReadOnlyList<string> Themes { get; } = ["Graphite", "Ultraviolet", "Crimson", "Arctic", "Cyberpunk"];
    public static IReadOnlyDictionary<string, string> GetPalette(string theme)
    {
        var palette = new Dictionary<string, string>
        {
            ["BackgroundBrush"] = "#15181D", ["PanelBrush"] = "#1C2026", ["CardBrush"] = "#232830",
            ["BorderBrush"] = "#353C46", ["TextPrimaryBrush"] = "#F2F4F7", ["TextSecondaryBrush"] = "#B5BEC9",
            ["TextMutedBrush"] = "#A4AFBC", ["AccentBrush"] = "#8AB4F8", ["PrimaryButtonBrush"] = "#A9C7FF",
            ["PrimaryButtonTextBrush"] = "#172235", ["PrimaryHoverBrush"] = "#BBD3FF", ["PrimaryPressedBrush"] = "#8FB6F5",
            ["HoverBrush"] = "#2D3540", ["PressedBrush"] = "#394657", ["SelectionBrush"] = "#28384E",
            ["SuccessBrush"] = "#8DCEA8", ["WarningBrush"] = "#F0C47A", ["ErrorBrush"] = "#F29B9B",
            ["ViewportBrush"] = "#101216", ["DisabledBrush"] = "#20242A", ["DisabledTextBrush"] = "#77818E"
        };
        var accent = theme switch
        {
            "Ultraviolet" => "#C9B4F8", "Crimson" => "#F2A7B5", "Arctic" => "#B7F5FF",
            "Cyberpunk" => "#A9D8E8", _ => palette["AccentBrush"]
        };
        if (theme is "Ultraviolet" or "Crimson" or "Arctic" or "Cyberpunk")
        {
            palette["AccentBrush"] = accent; palette["PrimaryButtonBrush"] = accent;
            palette["PrimaryHoverBrush"] = "#E0E8F5"; palette["PrimaryPressedBrush"] = "#B2BFD2";
        }
        palette["NeonBrush"] = palette["BorderBrush"]; palette["AlertBrush"] = palette["ErrorBrush"];
        return palette;
    }
    public void Apply(string theme)
    {
        var scale = 1.0;
        try { scale = Math.Clamp(new Windows.UI.ViewManagement.UISettings().TextScaleFactor, 1, 2.25); }
        catch (Exception error) when (error is System.Runtime.InteropServices.COMException or TypeLoadException) { }
        foreach (var (name, size) in new[] { ("BodyFontSize", 14.0), ("SmallFontSize", 13.0), ("SectionFontSize", 16.0), ("HeadingFontSize", 18.0), ("PageFontSize", 20.0) })
            Application.Current.Resources[name] = size * scale;
        foreach (var (name, value) in GetPalette(theme))
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value)); brush.Freeze();
            Application.Current.Resources[name] = brush;
        }
    }
}

