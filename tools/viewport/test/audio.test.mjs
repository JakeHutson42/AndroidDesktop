import test from 'node:test';
import assert from 'node:assert/strict';
import vm from 'node:vm';
import {readFileSync} from 'node:fs';
import {ControllerAudio} from '../audio.mjs';
function fixture(){
  let Processor;const receipts=[];
  vm.runInNewContext(readFileSync(new URL('../audio-worklet.mjs',import.meta.url),'utf8'),{
    AudioWorkletProcessor:class{constructor(){this.port={postMessage:x=>receipts.push(x)};}},
    Int16Array,registerProcessor:(name,type)=>{Processor=type;}
  });
  const processor=new Processor();
  const render=()=>{const out=[new Float32Array(128),new Float32Array(128)];processor.process([], [out]);return out;};
  return {processor,receipts,render};
}
test('PCM stereo preserves channels and emits silence on underrun',()=>{
  const f=fixture();f.processor.port.onmessage({data:new Int16Array([16384,-16384]).buffer});
  const out=f.render();assert.equal(out[0][0],.5);assert.equal(out[1][0],-.5);
  assert.equal(out[0][1],0);assert.equal(f.receipts.length,1);
});
test('overflow drops oldest PCM and never grows the ring',()=>{
  const f=fixture();f.processor.port.onmessage({data:new Int16Array(9600).fill(1).buffer});
  f.processor.port.onmessage({data:new Int16Array(9600).fill(16384).buffer});
  assert.equal(f.processor.count,9600);assert.equal(f.processor.ring.length,9600);
  assert.equal(f.render()[0][0],.5);
  f.processor.port.onmessage({data:new Int16Array(9602).buffer});assert.equal(f.receipts.length,2);
});
test('muting closes socket, worklet port and context; reconnect releases prior audio',async()=>{
  const contexts=[],nodes=[],sockets=[];
  globalThis.AudioContext=class{constructor(){this.state='running';this.audioWorklet={addModule:async()=>{}};contexts.push(this);}resume(){return Promise.resolve();}close(){this.state='closed';return Promise.resolve();}};
  globalThis.AudioWorkletNode=class{constructor(){this.port={close:()=>this.port.closed=true,postMessage(){}};nodes.push(this);}connect(){}disconnect(){this.disconnected=true;}};
  globalThis.WebSocket=class{static OPEN=1;constructor(){this.readyState=1;sockets.push(this);}close(){this.closed=true;}send(){}};
  const audio=new ControllerAudio();await audio.start(8087,'test',()=>{});
  await audio.start(8087,'test',()=>{});
  assert.equal(contexts[0].state,'closed');assert.equal(nodes[0].port.closed,true);assert.equal(sockets[0].closed,true);
  await audio.stop();assert.equal(contexts[1].state,'closed');assert.equal(nodes[1].disconnected,true);
  assert.equal(sockets[1].onmessage,null);assert.equal(audio.enabled,false);
  await Promise.all([audio.start(8087,'test',()=>{}),audio.start(8087,'test',()=>{})]);
  assert.equal(contexts.length,3); // Superseded startup never allocates another context.
  await audio.stop();
});
