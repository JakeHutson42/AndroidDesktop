export class ControllerAudio {
  async start(port, token, status) {
    const stopping = this.stop();
    const generation = this.generation;
    await stopping;
    if(generation!==this.generation)return;
    const context = this.context = new AudioContext({sampleRate:48000, latencyHint:'interactive'});
    this.packets=0;this.bytes=0;this.peak=0;this.droppedSamples=0;this.underruns=0;
    try {
      await context.audioWorklet.addModule('audio-worklet.js');
      if(context.state!=='running')status('Click the Android viewport to allow audio playback');
      let timer;
      try { await Promise.race([context.resume(),new Promise((_,reject)=>timer=setTimeout(()=>reject(new Error('Click the viewport, then enable audio again')),10000))]); }
      finally { clearTimeout(timer); }
      if (generation !== this.generation) return;
      if (context.state !== 'running') throw new Error('Audio playback needs a viewport click');
      const node = this.node = new AudioWorkletNode(context,'emulator-audio',{numberOfInputs:0,outputChannelCount:[2]});
      node.connect(context.destination);
      node.onprocessorerror=()=>{status('Audio renderer failed');void this.stop();};
      const socket = this.socket = new WebSocket(`ws://127.0.0.1:${port}/audio`,[token]);
      socket.binaryType='arraybuffer';
      let pending=false;
      node.port.onmessage=({data})=>{this.peak=Math.max(this.peak,data.peak);this.droppedSamples=data.droppedSamples;this.underruns=data.underruns;pending=false;if(socket.readyState===WebSocket.OPEN)socket.send('ack');};
      socket.onmessage=({data})=>{
        if (pending || !(data instanceof ArrayBuffer) || !data.byteLength || data.byteLength>19200 || data.byteLength%4) {
          status('Audio packet rejected'); void this.stop(); return;
        }
        this.packets++;this.bytes+=data.byteLength;pending=true;node.port.postMessage(data,[data]);
      };
      socket.onopen=()=>status('Android audio enabled');
      socket.onerror=()=>status('Audio connection failed');
      socket.onclose=()=>{status('Android audio stopped');void this.stop();};
    } catch(error) { if(this.context===context){await this.stop();throw error;} }
  }
  async stop() {
    this.generation=(this.generation||0)+1;
    const socket=this.socket; this.socket=null;
    if(socket){socket.onmessage=socket.onclose=socket.onerror=socket.onopen=null;socket.close();}
    if(this.node){this.node.onprocessorerror=null;this.node.port.onmessage=null;this.node.port.close();this.node.disconnect();this.node=null;}
    const context=this.context;this.context=null;
    if(context && context.state!=='closed')await context.close();
  }
  get enabled(){return !!this.context;}
  resume(){if(this.context?.state==='suspended')void this.context.resume();}
  get stats(){return {enabled:this.enabled,state:this.context?.state||'closed',packets:this.packets||0,bytes:this.bytes||0,peak:this.peak||0,droppedSamples:this.droppedSamples||0,underruns:this.underruns||0,bufferLimitMs:100};}
}
