// Local emulator controller display. Exactly one PNG is acknowledged after decoding/drawing.
export default class ControllerDisplay {
  constructor(url, token, { onConnected, onDisconnected, onError }) {
    this.frames = 0; this.bytes = 0; this.closed = false; this.header = null;
    this.canvas = document.createElement('canvas');
    this.context = this.canvas.getContext('2d', { alpha: false });
    this.socket = new WebSocket(url, [token]); this.socket.binaryType = 'arraybuffer';
    const self = this;
    this.event_forwarders = { input: {
      get readyState() { return !self.closed && self.frames > 0 && self.socket.readyState === WebSocket.OPEN ? 'open' : 'closed'; },
      get bufferedAmount() { return self.socket.bufferedAmount; },
      send(bytes) { if (this.readyState !== 'open') throw new Error('Controller display is disconnected'); self.socket.send(bytes); }
    } };
    this.peerConnection = { getStats: async () => new Map([['controller', {
      type: 'controller-display', kind: 'video', framesDecoded: this.frames, bytesReceived: this.bytes,
      decoderImplementation: 'PNG/canvas controller; no WebRTC audio'
    }]]) };
    this.socket.onmessage = async ({ data }) => {
      try {
        if (this.closed) return;
        if (typeof data === 'string') {
          const header = JSON.parse(data);
          if (header.kind === 'fault') throw new Error('Controller frame service unavailable');
          if (this.header || header.kind !== 'frame' || !Number.isSafeInteger(header.seq) || header.seq !== this.frames + 1 ||
            !Number.isSafeInteger(header.width) || !Number.isSafeInteger(header.height) ||
            header.width <= 0 || header.height <= 0 || header.width > 4096 || header.height > 4096 || header.width * header.height > 4194304)
            throw new Error('Invalid controller frame header');
          this.header = header; return;
        }
        const header = this.header;
        if (this.decoding || !header || !(data instanceof ArrayBuffer) || !data.byteLength || data.byteLength > 8388608) throw new Error('Invalid controller image');
        this.decoding = true;
        const bitmap = await createImageBitmap(new Blob([data], { type: 'image/png' }));
        try {
          if (this.closed) return;
          if (bitmap.width !== header.width || bitmap.height !== header.height) throw new Error('Controller image dimensions differ');
          if (this.canvas.width !== header.width || this.canvas.height !== header.height) {
            this.canvas.width = header.width; this.canvas.height = header.height;
          }
          this.context.drawImage(bitmap, 0, 0); this.frames++; this.bytes += data.byteLength;
          if (this.frames === 1) {
            this.stream = this.canvas.captureStream(15);
            for (const track of this.stream.getVideoTracks()) onConnected(track);
          }
          this.header = null;
          this.socket.send(JSON.stringify({ ack: header.seq }));
        } finally { bitmap.close(); this.decoding = false; }
      } catch (error) { if (!this.closed) { onError(error); this.disconnect(); } }
    };
    this.socket.onerror = () => { if (!this.closed) onError(new Error('Controller socket failed')); };
    this.socket.onclose = () => { if (!this.closed) { this.disconnect(); onDisconnected(); } };
  }
  disconnect() {
    this.closed = true; this.header = null;
    this.stream?.getTracks().forEach(track => track.stop()); this.socket.close();
    this.stream = null;
    // Release the potentially multi-megabyte backing surface, including remote closure.
    this.canvas.width = 0; this.canvas.height = 0;
    this.socket.onmessage = this.socket.onerror = this.socket.onclose = null;
  }
}
