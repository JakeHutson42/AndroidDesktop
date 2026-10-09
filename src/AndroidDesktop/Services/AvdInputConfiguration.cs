using System.Text.RegularExpressions;

namespace AndroidDesktop.Services;

public static class AvdInputConfiguration
{
    public static string EnableDesktopKeyboard(string configuration)
    {
        var newline = configuration.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        foreach (var (key, value) in new[] { ("hw.keyboard", "yes"), ("hw.keyboard.lid", "no"), ("hw.dPad", "yes") }) {
            var pattern = "(?m)^" + Regex.Escape(key) + @"\s*=[^\r\n]*";
            if (Regex.IsMatch(configuration, pattern)) configuration = Regex.Replace(configuration, pattern, key + "=" + value);
            else configuration = configuration.TrimEnd('\r', '\n') + newline + key + "=" + value + newline;
        }
        return configuration;
    }
}
