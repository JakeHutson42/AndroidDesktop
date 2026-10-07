import test from 'node:test';
import assert from 'node:assert/strict';
import ControllerDisplay from '../controller.mjs';

function fixture(decode = async () => ({width:2,height:3,close(){}})) {
  const sent=[], drawn=[], tracks=[], errors=[];
  const track={stop(){track.stopped=true;}};
  let socket;
  globalThis.document={createElement:()=>({width:0,height:0,getContext:()=>({drawImage:image=>drawn.push(image)}),captureStream:()=>({getVideoTracks:()=>[track],getTracks:()=>[track]})})};
  globalThis.WebSocket=class {static OPEN=1; constructor(){socket=this;this.readyState=1;this.bufferedAmount=0;}send(data){sent.push(data);}close(){this.closed=true;}};
  globalThis.createImageBitmap=decode;
  const display=new ControllerDisplay('ws://127.0.0.1:8087/controller','test', {onConnected:track=>tracks.push(track),onDisconnected(){},onError:error=>errors.push(error)});
  const header=()=>socket.onmessage({data:JSON.stringify({kind:'frame',seq:1,width:2,height:3})});
  const frame=()=>socket.onmessage({data:new ArrayBuffer(8)});
  return {display,socket,sent,drawn,tracks,errors,header,frame,track};
}
test('controller readiness and acknowledgement wait for decoded frame; input shares authenticated socket',async()=>{
  let complete;
  const f=fixture(()=>new Promise(resolve=>complete=resolve));
  assert.equal(f.display.event_forwarders.input.readyState,'closed');
  await f.header(); const pending=f.frame();
  assert.equal(f.sent.length,0);
  complete({width:2,height:3,close(){}});await pending;
  assert.equal(f.drawn.length,1);assert.equal(f.tracks.length,1);
  assert.deepEqual(JSON.parse(f.sent[0]),{ack:1});
  assert.equal(f.display.event_forwarders.input.readyState,'open');
  const input=new Uint8Array([1,2]);f.display.event_forwarders.input.send(input);assert.equal(f.sent[1],input);
  f.display.disconnect();assert.equal(f.track.stopped,true);assert.equal(f.display.event_forwarders.input.readyState,'closed');
  assert.equal(f.display.canvas.width,0);assert.equal(f.display.canvas.height,0);
  assert.equal(f.socket.onmessage,null);
});

test('remote closure releases tracks, backing surface and socket callbacks',async()=>{
  const f=fixture();await f.header();await f.frame();f.socket.onclose();
  assert.equal(f.track.stopped,true);assert.equal(f.display.canvas.width,0);
  assert.equal(f.socket.onmessage,null);assert.equal(f.display.stream,null);
});
test('disconnect during decode releases bitmap without drawing or late acknowledgement',async()=>{
  let complete,closed=false;
  const f=fixture(()=>new Promise(resolve=>complete=resolve));await f.header();const pending=f.frame();f.display.disconnect();
  complete({width:2,height:3,close(){closed=true;}});await pending;
  assert.equal(closed,true);assert.equal(f.sent.length,0);assert.equal(f.drawn.length,0);
});
test('mismatched decoded dimensions fail and close; oversized headers are rejected',async()=>{
  const f=fixture(async()=>({width:99,height:3,close(){}}));await f.header();await f.frame();
  assert.equal(f.errors.length,1);assert.equal(f.socket.closed,true);assert.equal(f.sent.length,0);
  const g=fixture();await g.socket.onmessage({data:JSON.stringify({kind:'frame',seq:1,width:4096,height:4096})});
  assert.equal(g.errors.length,1);assert.equal(g.drawn.length,0);
});
