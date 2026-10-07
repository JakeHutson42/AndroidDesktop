import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mapPoint, makeTouch, sameGeometry } from '../input.mjs';
import protobuf from 'protobufjs';
import { fileURLToPath } from 'node:url';
test('pillarbox ignores borders and clamps held pointers outside viewport', () => {
  const geometry = {width:720,height:1280,orientation:0}, box = {left:10,top:20,width:1000,height:1000};
  assert.equal(mapPoint(10,20,box,geometry),null);
  const center = mapPoint(510,520,box,geometry); assert.equal(center.x,360); assert.equal(center.y,640);
  const outside = mapPoint(3000,4000,box,geometry,true); assert.equal(outside.x,719); assert.equal(outside.y,1279);
});
test('landscape scales using decoded geometry and ignores letterbox', () => {
  const geometry={width:1280,height:720,orientation:90}, box={left:0,top:0,width:500,height:1000};
  assert.equal(mapPoint(250,0,box,geometry),null);
  assert.deepEqual(mapPoint(250,500,box,geometry),{x:640,y:360,normalizedX:.5,normalizedY:.5});
  assert.equal(sameGeometry(geometry,{...geometry,orientation:270}),false);
});
test('up and cancel encode zero pressure and retain pointer identity', () => {
  const root=protobuf.loadSync(fileURLToPath(new URL('../../../src/AndroidDesktop/Protocols/emulator_controller.proto',import.meta.url)));
  const input=root.lookupType('android.emulation.control.InputEvent');
  for (const phase of ['down','move','up','cancel']) {
    const payload=makeTouch({x:16,y:32,pointerId:7,phase,pressure:.5});
    const decoded=input.decode(input.encode(input.create(payload)).finish());
    assert.equal(decoded.touchEvent.touches[0].identifier,7);
    assert.equal(decoded.touchEvent.touches[0].pressure,['up','cancel'].includes(phase)?0:512);
    assert.equal(decoded.touchEvent.touches[0].expiration,1);
  }
});
test('zero geometry cannot inject input', () => assert.equal(mapPoint(10,10,{left:0,top:0,width:1,height:1},{width:0,height:0}),null));
