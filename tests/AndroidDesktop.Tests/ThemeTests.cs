using AndroidDesktop.Services;

namespace AndroidDesktop.Tests;

public sealed class ThemeTests
{
    [Theory]
    [InlineData("Graphite")]
    [InlineData("Arctic")]
    [InlineData("Crimson")]
    [InlineData("Ultraviolet")]
    [InlineData("Cyberpunk")]
    public void EnabledTextMeetsContrastRequirements(string theme)
    {
        var palette = ThemeService.GetPalette(theme);
        foreach (var text in new[] { "TextPrimaryBrush", "TextSecondaryBrush", "TextMutedBrush" })
            foreach (var surface in new[] { "BackgroundBrush", "PanelBrush", "CardBrush", "SelectionBrush" })
                Assert.True(Contrast(palette[text], palette[surface]) >= 4.5, $"{theme}: {text} on {surface}");
        foreach (var surface in new[] { "PrimaryButtonBrush", "PrimaryHoverBrush", "PrimaryPressedBrush" })
            Assert.True(Contrast(palette["PrimaryButtonTextBrush"], palette[surface]) >= 4.5, $"{theme}: primary text on {surface}");
        Assert.True(Contrast(palette["AccentBrush"], palette["CardBrush"]) >= 3, $"{theme}: focus visibility");
    }

    private static double Contrast(string first, string second)
    {
        var a = Luminance(first); var b = Luminance(second);
        return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
    }
    private static double Luminance(string hex)
    {
        static double Channel(string value)
        {
            var s = Convert.ToInt32(value, 16) / 255.0;
            return s <= .04045 ? s / 12.92 : Math.Pow((s + .055) / 1.055, 2.4);
        }
        return .2126 * Channel(hex.Substring(1, 2)) + .7152 * Channel(hex.Substring(3, 2)) + .0722 * Channel(hex.Substring(5, 2));
    }
}
