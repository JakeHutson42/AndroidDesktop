import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import vm from 'node:vm';
import protobuf from 'protobufjs';
import { fileURLToPath } from 'node:url';
const Input=protobuf.loadSync(fileURLToPath(new URL('../../../src/AndroidDesktop/Protocols/emulator_controller.proto',import.meta.url))).lookupType('android.emulation.control.InputEvent');
const bundle=readFileSync(new URL('../../../src/AndroidDesktop/Assets/Viewport/viewport.js',import.meta.url),'utf8');

async function fixture() {
  const messages=[], bytes=[], handlers={}, host={}, intervals=[];
  const video={videoWidth:720,videoHeight:1280,muted:true,
    addEventListener:(name,fn)=>handlers[name]=fn,focus(){},setPointerCapture(){},requestVideoFrameCallback(){},
    getBoundingClientRect:()=>({left:0,top:0,width:720,height:1280}),play:()=>Promise.resolve(),pause(){},
    getVideoPlaybackQuality:()=>({totalVideoFrames:0,droppedVideoFrames:0})};
  const channel={label:'input',readyState:'open',bufferedAmount:0,send:b=>bytes.push(Input.decode(b))};
  let socket;
  class Socket { static OPEN=1; constructor(url,token){this.readyState=1;socket=this;} send(){} close(){} }
  class Peer { constructor(config){assert.deepEqual(Array.from(config.iceServers),[]);} createDataChannel(){return channel;}
    addTransceiver(){} createOffer(){return Promise.resolve({type:'offer',sdp:'fake'});} setLocalDescription(){return Promise.resolve();}
    close(){} getStats(){return Promise.resolve(new Map());} }
  class Stream { constructor(){this.tracks=[];} getTracks(){return this.tracks;} addTrack(t){this.tracks.push(t);} removeTrack(t){this.tracks=this.tracks.filter(x=>x!==t);} getAudioTracks(){return [];} }
  const document={getElementById:()=>video,addEventListener(){},hidden:false};
  const chrome={webview:{postMessage:m=>messages.push(m),addEventListener:(name,fn)=>host[name]=fn}};
  const window={chrome,addEventListener:(name,fn)=>host[name]=fn};
  const context={window,document,performance,MediaStream:Stream,WebSocket:Socket,RTCPeerConnection:Peer,console,
    setInterval:(fn,ms)=>intervals.push({fn,ms}),clearTimeout,
    setTimeout:(fn,ms)=>ms>=15000?0:setTimeout(fn,ms),Uint8Array,ArrayBuffer,TextEncoder,TextDecoder};
  vm.runInNewContext(bundle,context,{codeGeneration:{strings:false,wasm:false}});
  assert.equal(messages[0].kind,'loaded');
  await host.message({data:{kind:'connect',port:8087,token:'fake'}});
  socket.onmessage({data:JSON.stringify({start:{iceServers:[]}})});
  await new Promise(resolve=>setImmediate(resolve));
  handlers.loadedmetadata();
  const pointer=(name,id,x=100,y=100)=>{
    handlers[name]({preventDefault(){},pointerId:id,clientX:x,clientY:y,pointerType:'touch',pressure:.5});
    assert.equal(messages.filter(m=>m.kind==='fault').map(m=>m.data).join(';'),'');
  };
  return {messages,bytes,host,video,channel,pointer,resize:()=>handlers.resize(),flush:()=>intervals.find(i=>i.ms===50).fn()};
}

test('device toolbar uses persistent input with atomic keypresses and rejects unknown controls',async()=>{
  const f=await fixture();
  f.pointer('pointerdown',99);
  for (const control of ['back','home','volumeUp','volumeDown']) await f.host.message({data:{kind:'deviceKey',data:control}});
  assert.equal(f.bytes[1].touchEvent.touches[0].pressure,0);
  const keys=f.bytes.filter(e=>e.keyEvent).map(e=>e.keyEvent);
  assert.deepEqual(keys.map(e=>e.eventType),[2,2,2,2]);
  assert.deepEqual(keys.slice(0,2).map(e=>e.key),['GoBack','GoHome']);
  assert.deepEqual(keys.slice(2).map(e=>[e.codeType,e.keyCode]),[[1,115],[1,114]]);
  const before=f.bytes.length;
  await f.host.message({data:{kind:'deviceKey',data:'untrusted-operation'}});
  assert.equal(f.bytes.length,before);
});
test('input readiness follows actual channel state and disables after closure',async()=>{
  const f=await fixture(); await f.flush();
  assert.equal(f.messages.filter(m=>m.kind==='inputReady').at(-1).data,true);
  f.channel.readyState='closed'; await f.flush();
  assert.equal(f.messages.filter(m=>m.kind==='inputReady').at(-1).data,false);
});
test('packaged CSP-safe bundle sends stable simultaneous pointer slots and releases on focus loss',async()=>{
  const f=await fixture(); f.pointer('pointerdown',101); f.pointer('pointerdown',205,200,300);
  assert.deepEqual(f.bytes.slice(-2).map(e=>e.touchEvent.touches[0].identifier),[0,1]);
  f.host.blur();
  assert.deepEqual(f.bytes.slice(-2).map(e=>e.touchEvent.touches[0].pressure),[0,0]);
});
test('diagnostic capture and replay use the same encoded events and cannot continue after geometry mismatch',async()=>{
  const f=await fixture(); await f.host.message({data:{kind:'arm'}});
  f.pointer('pointerdown',45); f.pointer('pointermove',45,120,180); f.pointer('pointerup',45,130,190);
  const original=f.bytes.slice(-3).map(e=>JSON.stringify(e));
  assert.ok(f.messages.some(m=>m.kind==='gesture'&&m.data.length===3));
  await f.host.message({data:{kind:'replay'}});
  assert.deepEqual(f.bytes.slice(-3).map(e=>JSON.stringify(e)),original);
  const count=f.bytes.length;
  f.video.videoWidth=1280; f.video.videoHeight=720;
  f.resize();
  await f.host.message({data:{kind:'replay'}});
  assert.equal(f.bytes.length,count);
  assert.ok(f.messages.some(m=>m.kind==='status'&&m.data.includes('current geometry')));
});
test('held pointer can leave the content rectangle and always emits up',async()=>{
  const f=await fixture(); f.pointer('pointerdown',5); f.pointer('pointerup',5,-100,3000);
  const touch=f.bytes.at(-1).touchEvent.touches[0];
  assert.equal(touch.pressure,0); assert.equal(touch.x,0); assert.equal(touch.y,1279);
});
test('cancel during a scheduled replay releases the active pointer and sends no late event',async()=>{
  const f=await fixture(); await f.host.message({data:{kind:'arm'}});
  f.pointer('pointerdown',71);
  await new Promise(resolve=>setTimeout(resolve,25));
  f.pointer('pointerup',71);
  const count=f.bytes.length;
  const replay=f.host.message({data:{kind:'replay'}});
  await f.host.message({data:{kind:'cancel'}});
  await replay;
  assert.equal(f.bytes.length,count+2);
  assert.equal(f.bytes.at(-1).touchEvent.touches[0].pressure,0);
});
test('persistent recording batches touch, keyboard and release events on the same live input path',async()=>{
  const f=await fixture();await f.host.message({data:{kind:'recordingStart',data:{id:'record'}}});
  f.pointer('pointerdown',101);f.pointer('pointerdown',202,300,400);f.host.blur();
  await f.host.message({data:{kind:'deviceKey',data:'volumeUp'}});
  await f.host.message({data:{kind:'recordingStop'}});
  const events=f.messages.filter(m=>m.kind==='recordingBatch').flatMap(m=>m.data.events);
  assert.deepEqual(events.map(e=>e.sequence),[1,2,3,4,5,6]);assert.equal(events[0].kind,'geometry');
  assert.deepEqual(events.slice(1,5).map(e=>e.phase),['down','down','cancel','cancel']);assert.equal(events.at(-1).kind,'key');assert.equal(events.at(-1).keyCode,115);
  assert.equal(f.messages.find(m=>m.kind==='recordingStopped').data.error,null);
  assert.equal(f.messages.find(m=>m.kind==='recordingStopped').data.count,events.length);
});
test('recording overload stops archive capture explicitly while live input continues',async()=>{
  const f=await fixture();await f.host.message({data:{kind:'recordingStart',data:{id:'record'}}});
  f.pointer('pointerdown',1);for(let i=0;i<260;i++)f.pointer('pointermove',1,100+i%10,200);
  const fault=f.messages.find(m=>m.kind==='recordingFault');assert.ok(fault);assert.match(fault.data.error,/overflow/);
  const count=f.bytes.length;f.pointer('pointerup',1);assert.equal(f.bytes.length,count+1);assert.equal(f.bytes.at(-1).touchEvent.touches[0].pressure,0);
});
test('persisted replay uses the same encoded touch path and records actual dispatch timing',async()=>{
  const f=await fixture();const geometry={width:720,height:1280,orientation:0};
  const events=['down','move','up'].map((phase,i)=>({...geometry,kind:'touch',sequence:i+1,timestampMs:i*5,phase,pointerId:0,x:100+i,y:200,normalizedX:(100+i)/720,normalizedY:200/1280,pressure:.5}));
  await f.host.message({data:{kind:'playbackStart',data:{id:'run',durationMs:10,options:{loops:1,infinite:false,loopDelayMs:0,variations:false,seed:1,coordinatePixels:0,pathPixels:0,timingMs:0}}}});
  const request=f.messages.find(m=>m.kind==='playbackRequest');assert.ok(request);
  await f.host.message({data:{kind:'playbackBatch',data:{runId:'run',requestId:request.data.requestId,events,done:true}}});
  await new Promise(resolve=>setTimeout(resolve,30));
  assert.deepEqual(f.bytes.slice(-3).map(e=>[e.touchEvent.touches[0].x,e.touchEvent.touches[0].pressure]),[[100,512],[101,512],[102,0]]);
  const audit=f.messages.filter(m=>m.kind==='playbackDispatch').flatMap(m=>m.data.events);assert.equal(audit.length,3);
  assert.ok(audit.every(e=>Number.isFinite(e.dispatchedMs)&&Number.isFinite(e.scheduledMs)&&Number.isFinite(e.lateMs)));
  assert.equal(f.messages.filter(m=>m.kind==='playbackState').at(-1).data.status,'completed');
});
test('host cancellation during persisted replay releases held keys and prevents later sends',async()=>{
  const f=await fixture();const events=[0,100].map((timestampMs,i)=>({kind:'key',sequence:i+1,timestampMs,phase:i?'up':'down',key:'a',width:720,height:1280,orientation:0}));
  await f.host.message({data:{kind:'playbackStart',data:{id:'run',durationMs:100,options:{loops:1,infinite:false,loopDelayMs:0,variations:false,seed:1,coordinatePixels:0,pathPixels:0,timingMs:0}}}});
  const request=f.messages.find(m=>m.kind==='playbackRequest');
  await f.host.message({data:{kind:'playbackBatch',data:{runId:'run',requestId:request.data.requestId,events,done:true}}});
  await new Promise(setImmediate);await f.host.message({data:{kind:'cancel'}});await new Promise(resolve=>setTimeout(resolve,30));
  assert.deepEqual(f.bytes.map(e=>e.keyEvent.eventType),[0,1]);
  assert.equal(f.messages.filter(m=>m.kind==='playbackState').at(-1).data.status,'stopped');
  assert.ok(f.messages.filter(m=>m.kind==='playbackDispatch').flatMap(m=>m.data.events).some(e=>e.release&&e.phase==='cancel'));
});
