using AndroidDesktop.Services;
using AndroidDesktop.Adapters.Viewport;
namespace AndroidDesktop.Tests;
public class NativeInputTests
{
    [Theory]
    [InlineData(0, 108, 384)]
    [InlineData(90, 863, 192)]
    [InlineData(180, 971, 1535)]
    [InlineData(270, 216, 1727)]
    public void PointerMapsVisiblePositionToPhysicalPanel(int rotation, int x, int y)
        => Assert.Equal((x,y), NativeInputClient.Map(.1,.2,1080,1920,rotation));
    [Fact] public void CapturedPointerOutsideViewportClampsToPanel()
        => Assert.Equal((0,1919), NativeInputClient.Map(-1,2,1080,1920,0));
    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void KeyboardConfigPreservesDeviceIdentityAndIsIdempotent(string nl)
    {
        var original = string.Join(nl, "# owned persistent device", "hw.keyboard=no", "hw.keyboard.lid=yes", "hw.dPad=no", "disk.dataPartition.path=C:/saved/userdata.img", "AvdId=keep-me", "");
        var updated = AvdInputConfiguration.EnableDesktopKeyboard(original);
        Assert.Equal(original.Replace("hw.keyboard=no","hw.keyboard=yes").Replace("hw.keyboard.lid=yes","hw.keyboard.lid=no").Replace("hw.dPad=no","hw.dPad=yes"), updated);
        Assert.Equal(updated, AvdInputConfiguration.EnableDesktopKeyboard(updated));
    }
}
