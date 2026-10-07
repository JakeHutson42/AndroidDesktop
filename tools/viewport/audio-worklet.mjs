// Fixed 100 ms ring. Overflow discards stale samples; underrun emits silence.
class EmulatorAudio extends AudioWorkletProcessor {
  constructor() {
    super(); this.ring = new Int16Array(9600); this.read = 0; this.count = 0;this.droppedSamples=0;this.underruns=0;this.peak=0;
    this.port.onmessage = ({data}) => {
      const input = new Int16Array(data);
      if (!input.length || input.length > this.ring.length || input.length % 2) return;
      if (this.count + input.length > this.ring.length) {
        const discard = this.count + input.length - this.ring.length;
        this.read = (this.read + discard) % this.ring.length; this.count -= discard;
        this.droppedSamples+=discard;
      }
      let write = (this.read + this.count) % this.ring.length;
      for (let i=0;i<input.length;i++) { this.ring[write]=input[i]; write=(write+1)%this.ring.length; }
      this.count += input.length; this.port.postMessage({peak:this.peak,droppedSamples:this.droppedSamples,underruns:this.underruns});
    };
  }
  process(inputs, outputs) {
    const output = outputs[0];
    for (let i=0;i<output[0].length;i++) {
      if (this.count >= 2) {
        output[0][i]=this.ring[this.read]/32768; this.read=(this.read+1)%this.ring.length;
        output[1][i]=this.ring[this.read]/32768; this.read=(this.read+1)%this.ring.length; this.count-=2;
        this.peak=Math.max(this.peak,Math.abs(output[0][i]),Math.abs(output[1][i]));
      } else { output[0][i]=0; output[1][i]=0; this.underruns++; }
    }
    return true;
  }
}
registerProcessor('emulator-audio', EmulatorAudio);
