using AndroidDesktop.Adapters.Viewport;
using AndroidDesktop.Models;
using System.IO;

namespace AndroidDesktop.Tests;
public class NativeViewportTests
{
    [Theory]
    [InlineData(1080, 1920, 0.5625, 1080, 1920)]
    [InlineData(800, 600, 0.5625, 338, 600)]
    [InlineData(1000, 1000, 1.77777777777778, 1000, 562)]
    public void NativeSurfaceFitsPhysicalViewportWithoutStretching(int w, int h, double aspect, int expectedW, int expectedH)
    {
        var fit = NativeEmulatorHost.Fit(w, h, aspect);
        Assert.Equal(expectedW, fit.Width); Assert.Equal(expectedH, fit.Height);
        Assert.Equal((w - fit.Width) / 2, fit.X); Assert.Equal((h - fit.Height) / 2, fit.Y);
        Assert.InRange(fit.Width, 1, w); Assert.InRange(fit.Height, 1, h);
    }
    [Fact] public void HiddenViewportDoesNotProduceInvalidNativeSize()
    {
        Assert.Equal((0, 0, 0, 0), NativeEmulatorHost.Fit(0, 600, 1));
        Assert.Equal((0, 0, 0, 0), NativeEmulatorHost.Fit(800, 600, double.NaN));
    }
    [Fact] public void NativeAndStandaloneCannotBeEnabledTogether()
        => Assert.Throws<InvalidDataException>(() => (new DesktopSettings().Runtime with { DisplayTransport = "native", ShowStandaloneWindow = true }).Validate());
}
