import { test } from 'node:test';
import assert from 'node:assert/strict';
import { PlaybackEngine, variation } from '../playback.mjs';

const geometry = {width:720,height:1280,orientation:0};
const touch = (sequence,timestampMs,phase,pointerId=0) => ({...geometry,kind:'touch',sequence,timestampMs,phase,pointerId,x:100,y:200,normalizedX:100/720,normalizedY:200/1280,pressure:.5});
const trace = [touch(1,0,'down'),touch(2,40,'move'),touch(3,80,'up')];
const options = {loops:1,infinite:false,loopDelayMs:20,variations:false,seed:1,coordinatePixels:0,pathPixels:0,timingMs:0};
function fixture(events=trace, extra={}) {
  let clock=0, current={...geometry}; const contacts=new Set(), dispatches=[], states=[], receipts=[], requests=[];
  let engine;
  engine=new PlaybackEngine({now:()=>clock,sleep:async ms=>{clock+=ms;await new Promise(setImmediate);},geometry:()=>current,ready:()=>true,
    held:()=>contacts.size,requestBatch:async(id,start,loop)=>{requests.push({start,loop});return {events:events.slice(start,start+128),done:start+128>=events.length};},
    dispatch:e=>{dispatches.push({...e,actual:clock}); if(e.phase==='down')contacts.add(e.pointerId);if(['up','cancel'].includes(e.phase))contacts.delete(e.pointerId);extra.dispatch?.(e,engine);},
    release:()=>{contacts.clear();},state:s=>{states.push(s);extra.state?.(s,engine,()=>clock+=1000);},audit:e=>receipts.push(...e)});
  return {engine,dispatches,states,receipts,requests,contacts,setGeometry:g=>current=g,run:o=>engine.start({id:'run',durationMs:100,options:{...options,...o}})};
}
test('exact replay preserves events and uses absolute deadlines without accumulated dispatch drift',async()=>{
  const f=fixture();await f.run();assert.deepEqual(f.dispatches.map(e=>e.timestampMs),[0,40,80]);assert.deepEqual(f.dispatches.map(e=>e.actual),[0,40,80]);
  assert.deepEqual(f.dispatches.map(({actual,...e})=>e),trace);assert.equal(f.states.at(-1).status,'completed');assert.equal(f.receipts.length,3);
});
test('finite loops restart event cursor and preserve fixed loop deadlines',async()=>{
  const f=fixture();await f.run({loops:3});assert.deepEqual(f.dispatches.map(e=>e.actual),[0,40,80,120,160,200,240,280,320]);
  assert.deepEqual(f.requests.map(r=>r.loop),[1,2,3]);assert.equal(f.states.at(-1).completedLoops,3);
});
test('infinite replay is cancellable and leaves no held contacts or late sends',async()=>{
  const f=fixture(trace,{state:(s,engine)=>{if(s.completedLoops===2)engine.stop();}});await f.run({infinite:true});
  assert.equal(f.states.at(-1).status,'stopped');assert.equal(f.dispatches.length,6);assert.equal(f.contacts.size,0);
});
test('pause waits for release boundary and shifts deadlines on resume without manufacturing touches',async()=>{
  const events=[...trace,touch(4,120,'down'),touch(5,160,'up')];
  const f=fixture(events,{dispatch:(e,engine)=>{if(e.sequence===1)engine.pause();},state:(s,engine,advance)=>{if(s.state==='paused'){advance();setImmediate(()=>engine.resume());}}});
  await f.run();assert.equal(f.states.filter(s=>s.state==='paused').length,1);assert.deepEqual(f.dispatches.map(e=>e.phase),['down','move','up','down','up']);
  assert.deepEqual(f.dispatches.map(e=>e.actual),[0,40,80,1120,1160]);
});
test('geometry mismatch stops before injecting incompatible coordinates and releases held contacts',async()=>{
  const f=fixture(trace,{dispatch:(e)=>{if(e.phase==='down')f.setGeometry({width:1280,height:720,orientation:90});}});await f.run();
  assert.equal(f.dispatches.length,1);assert.equal(f.contacts.size,0);assert.equal(f.states.at(-1).status,'faulted');assert.match(f.states.at(-1).error,/geometry/);
});
test('seeded variations are reproducible, bounded, monotonic and do not modify the original',()=>{
  const originals=[touch(1,0,'down'),...Array.from({length:20},(_,i)=>touch(i+2,(i+1)*20,'move')),touch(22,500,'up')];
  const before=JSON.stringify(originals),settings={...options,variations:true,seed:123,coordinatePixels:8,pathPixels:4,timingMs:20};
  const first=originals.map(variation(settings)),second=originals.map(variation(settings));assert.deepEqual(first,second);assert.equal(JSON.stringify(originals),before);
  first.forEach((e,i)=>{assert.ok(Math.abs(e.x-originals[i].x)<=12);assert.ok(Math.abs(e.y-originals[i].y)<=12);assert.ok(Math.abs(e.timestampMs-originals[i].timestampMs)<=20);if(i)assert.ok(e.timestampMs>=first[i-1].timestampMs);});
  assert.notDeepEqual(first,originals.map(variation({...settings,seed:124})));
});
test('variation loop timestamps reset while deterministic random sequence continues',async()=>{
  const f=fixture();await f.run({loops:3,variations:true,coordinatePixels:4,timingMs:10});assert.equal(f.dispatches.length,9);
  assert.ok(f.dispatches[3].timestampMs<=10);assert.ok(f.dispatches[6].timestampMs<=10);
});
test('streaming replay reads bounded batches rather than caching the complete trace',async()=>{
  const events=Array.from({length:300},(_,i)=>touch(i+1,i,i%2?'up':'down'));const f=fixture(events);await f.run();
  assert.deepEqual(f.requests.map(r=>r.start),[0,128,256]);assert.equal(f.dispatches.length,300);
});
test('late scheduling stops before a stale burst instead of accumulating timing errors',async()=>{
  let clock=0,sends=0;const held=new Set(),states=[];
  const engine=new PlaybackEngine({now:()=>clock,sleep:async ms=>clock+=ms,geometry:()=>geometry,ready:()=>true,held:()=>held.size,
    requestBatch:async()=>({events:trace,done:true}),dispatch:e=>{sends++;held.add(e.pointerId);clock+=300;},release:()=>held.clear(),audit:()=>{},state:s=>states.push(s)});
  await engine.start({id:'run',durationMs:100,options});assert.equal(sends,1);assert.equal(held.size,0);assert.match(states.at(-1).error,/250 ms/);
});
