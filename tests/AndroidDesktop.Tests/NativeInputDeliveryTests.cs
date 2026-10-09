using Android.Emulation.Control;
using AndroidDesktop.Adapters.Viewport;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Collections.Concurrent;
namespace AndroidDesktop.Tests;
public class NativeInputDeliveryTests
{
    private sealed class FakeController : EmulatorController.EmulatorControllerClient
    {
        public readonly ConcurrentQueue<object> Events = new();
        public readonly TaskCompletionSource First = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task<Empty> Send(object message)
        {
            Events.Enqueue(message); First.TrySetResult();
            return Wait();
        }
        private async Task<Empty> Wait() { await Gate.Task; return new Empty(); }
        private static AsyncUnaryCall<Empty> Call(Task<Empty> value) => new(value, Task.FromResult(new Metadata()), () => Status.DefaultSuccess, () => new Metadata(), () => {});
        public override AsyncUnaryCall<Empty> sendKeyAsync(KeyboardEvent request, Metadata headers = null!, DateTime? deadline = null, CancellationToken cancellationToken = default) => Call(Send(request));
        public override AsyncUnaryCall<Empty> sendTouchAsync(TouchEvent request, Metadata headers = null!, DateTime? deadline = null, CancellationToken cancellationToken = default) => Call(Send(request));
    }
    [Fact] public async Task CoalescedMovementPreservesDownUpAndKeyReleases()
    {
        var controller = new FakeController();
        var input = new NativeInputClient(controller,1080,1920);
        input.Key(0x1e,true); await controller.First.Task.WaitAsync(TimeSpan.FromSeconds(2));
        input.Pointer(.1,.2,0,true);
        for (var i=0;i<1000;i++) input.Pointer(i/1000d,.5,0,true,move:true);
        input.Pointer(.9,.5,0,false);
        input.ReleaseAll(); controller.Gate.SetResult(); await input.DisposeAsync();
        var touch = controller.Events.OfType<TouchEvent>().Select(e=>e.Touches[0]).ToArray();
        Assert.Equal(3,touch.Length); Assert.Equal(1,touch[0].Pressure); Assert.Equal(1,touch[1].Pressure); Assert.Equal(0,touch[2].Pressure);
        var key = controller.Events.OfType<KeyboardEvent>().ToArray();
        Assert.Equal(new[]{KeyboardEvent.Types.KeyEventType.Keydown,KeyboardEvent.Types.KeyEventType.Keyup},key.Select(k=>k.EventType));
        Assert.InRange(input.PeakQueue,1,4);
    }
    [Fact] public async Task DisposalReleasesHeldContactBeforeClosingConnection()
    {
        var controller = new FakeController(); controller.Gate.SetResult();
        var input = new NativeInputClient(controller,1080,1920);
        input.Pointer(.2,.3,0,true); input.Key(0x1d,true); await input.DisposeAsync();
        Assert.Equal(new[]{1,0},controller.Events.OfType<TouchEvent>().Select(e=>e.Touches[0].Pressure));
        Assert.Equal(new[]{KeyboardEvent.Types.KeyEventType.Keydown,KeyboardEvent.Types.KeyEventType.Keyup},controller.Events.OfType<KeyboardEvent>().Select(e=>e.EventType));
    }
    [Fact] public async Task StalledQueueStopsAcceptingAndAppendsReleases()
    {
        var controller = new FakeController(); var input = new NativeInputClient(controller,1080,1920);
        input.Key(0x1e,true); await controller.First.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var faults=0; input.Fault += _=>faults++;
        for (var i=0;i<500;i++) input.Key(0x1e,true);
        controller.Gate.SetResult(); await input.DisposeAsync();
        Assert.Equal(1,faults); Assert.InRange(input.PeakQueue,1,129);
        Assert.Equal(KeyboardEvent.Types.KeyEventType.Keyup, ((KeyboardEvent)controller.Events.Last()).EventType);
    }
}
