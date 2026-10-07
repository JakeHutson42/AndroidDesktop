import { android } from './emulator.js';
import WsJsepProtocol from './upstream/ws_jsep_protocol_driver.ts';
import { mapPoint, sameGeometry, makeTouch } from './input.mjs';
import { PlaybackEngine } from './playback.mjs';
import ControllerDisplay from './controller.mjs';
import { ControllerAudio } from './audio.mjs';
const controllerAudio = new ControllerAudio();

const Input = android.emulation.control.InputEvent;
const video = document.getElementById('display');
const stream = new MediaStream();
let driver, token, epoch = performance.now(), sequence = 0;
let geometry = { width: 0, height: 0, orientation: 0 };
let pointers = new Map(), browserPointers = new Map(), keys = new Set(), inputBatch = [], capture = null, armed = false, gesture = null;
let replaying = false, replayGeneration = 0, frameTimes = [], previousFrame = null, previousStats = null;
let suppressed = false, rotationTimer = null, statsBusy = false, gatewayPort = null, inputWasReady = false;
let recording = null, recordingBatch = [], runId = null, requestSequence = 0;
const requests = new Map();
const post = (kind, data) => window.chrome.webview.postMessage({ kind, data });
const status = text => post('status', text);
const fault = text => { cancelAll(); suppressed = true; status(text); post('fault', text); };
function send(payload, releasing = false) {
  const channel = driver?.event_forwarders.input;
  if (channel?.readyState !== 'open') throw new Error('Input data channel is not open');
  if (!releasing && channel.bufferedAmount > 65536) throw new Error('Input backlog exceeded 64 KiB; interaction stopped');
  channel.send(Input.encode(payload).finish());
}
function observe(event) {
  event.kind ??= 'touch';
  if (recording && event.source !== 'playback') {
    const row = { ...event, sequence: ++recording.count, timestampMs: Math.max(0, event.timestampMs - recording.start), dispatchedMs: Math.max(0, (event.dispatchedMs ?? event.timestampMs) - recording.start) };
    if (recordingBatch.length >= 256) {
      const id = recording.id; recording = null; recordingBatch = [];
      post('recordingFault', { id, error: 'Browser recording queue overflow; capture is incomplete' });
    } else recordingBatch.push(row);
  }
  if (playback.active && event.source === 'cancel') playback.recordRelease(event);
  if (inputBatch.length >= 1024) { inputBatch = []; post('evidenceOverflow', 'Input evidence queue overflow; sample is invalid'); }
  inputBatch.push(event);
  if (capture) {
    if (capture.length >= 4096 || (capture.length && event.timestampMs - capture[0].timestampMs > 10000)) {
      capture = null; armed = false; gesture = null; status('Diagnostic gesture exceeded 10 seconds / 4096 events; discarded');
    } else capture.push(event);
  }
}
function dispatch(event, source) {
  if (!sameGeometry(event, geometry)) throw new Error('Input geometry mismatch');
  send(makeTouch(event), event.phase === 'up' || event.phase === 'cancel');
  if (['up', 'cancel'].includes(event.phase)) pointers.delete(event.pointerId);
  else pointers.set(event.pointerId, event);
  observe({ ...event, sequence: ++sequence, source, dispatchedMs: performance.now() - epoch });
}
function cancelAll(stopAutomation = true) {
  if (stopAutomation && playback.active) {
    playback.stop(playback.stopReason);
    for (const pending of requests.values()) { clearTimeout(pending.timer); pending.reject(new Error('Playback stopped')); } requests.clear();
  }
  replayGeneration++; replaying = false;
  let releaseFailed = false;
  const heldKeys = [...keys];
  for (const event of [...pointers.values()]) {
    try { dispatch({ ...event, phase: 'cancel', timestampMs: performance.now() - epoch }, 'cancel'); }
    catch { pointers.delete(event.pointerId); releaseFailed = true; post('releaseUnverified', 'Connection lost before pointer release; attempting gateway fallback'); }
  }
  for (const key of [...keys]) { try { dispatchKey({ key, phase: 'cancel', ...geometry, timestampMs: performance.now() - epoch }, 'cancel'); } catch { releaseFailed = true; } }
  keys.clear(); browserPointers.clear(); armed = false;
  if (releaseFailed && token && gatewayPort) {
    suppressed = true;
    fetch(`http://127.0.0.1:${gatewayPort}/release`, { method:'POST', headers:{Authorization:`Bearer ${token}`,'Content-Type':'application/json'},
      body: JSON.stringify({keys:heldKeys}) }).then(r => { if (!r.ok) throw new Error('Release rejected'); status('Gateway release requested; verify received releases in diagnostic APK'); })
      .catch(() => post('releaseUnverified', 'Gateway release also failed; reconnect and verify no held input remains'));
  }
  if (capture) { capture = null; gesture = null; status('Diagnostic gesture interrupted; discarded'); }
}
function dispatchKey(event, source) {
  if (!sameGeometry(event, geometry)) throw new Error('Keyboard geometry mismatch');
  const eventType = event.phase === 'press' ? 2 : event.phase === 'down' ? 0 : 1;
  send({ keyEvent: event.key ? { key: event.key, eventType } : { keyCode: event.keyCode, codeType: event.codeType, eventType } }, eventType === 1);
  if (eventType === 0) keys.add(event.key); else if (eventType === 1) keys.delete(event.key);
  observe({ ...event, kind: 'key', sequence: ++sequence, source, dispatchedMs: performance.now() - epoch });
}
function flushRecording() {
  if (recording && recordingBatch.length) { post('recordingBatch', { id: recording.id, events: recordingBatch }); recordingBatch = []; }
}
function stopRecording(error = null) {
  if (!recording) return;
  cancelAll(); flushRecording();
  const ended = recording; recording = null;
  post('recordingStopped', { id: ended.id, count: ended.count, durationMs: performance.now() - epoch - ended.start, error });
}
const playback = new PlaybackEngine({
  now: () => performance.now(), sleep: ms => new Promise(resolve => setTimeout(resolve, ms)),
  geometry: () => geometry, ready: () => !suppressed && driver?.event_forwarders.input?.readyState === 'open', held: () => pointers.size + keys.size,
  dispatch: event => { if (event.kind === 'touch') dispatch(event, 'playback'); else if (event.kind === 'key') dispatchKey(event, 'playback'); },
  release: () => cancelAll(false),
  state: state => post('playbackState', { id: runId, ...state }),
  audit: events => post('playbackDispatch', { id: runId, events }),
  requestBatch: (id, start, loop) => new Promise((resolve, reject) => {
    const requestId = String(++requestSequence);
    const timer = setTimeout(() => { requests.delete(requestId); reject(new Error('Playback batch timed out')); }, 10000);
    requests.set(requestId, { resolve, reject, timer }); post('playbackRequest', { id, start, loop, requestId });
  })
});
function updateGeometry() {
  if (!video.videoWidth || !video.videoHeight) return;
  const next = { ...geometry, width: video.videoWidth, height: video.videoHeight };
  if (next.width !== geometry.width || next.height !== geometry.height) {
    if (playback.active) playback.stop('Display geometry changed');
    cancelAll(); geometry = next; observe({ ...geometry, kind: 'geometry', phase: 'geometry', timestampMs: performance.now() - epoch, sequence: ++sequence });
    post('geometry', geometry);
  }
}
video.addEventListener('resize', updateGeometry);
video.addEventListener('loadedmetadata', updateGeometry);
video.addEventListener('contextmenu', e => e.preventDefault());
for (const [name, phase] of [['pointerdown','down'],['pointermove','move'],['pointerup','up'],['pointercancel','cancel']]) {
  video.addEventListener(name, e => {
    if(phase==='down')controllerAudio.resume();
    e.preventDefault();
    if (suppressed || replaying || playback.active || (phase !== 'down' && !browserPointers.has(e.pointerId))) return;
    if (phase === 'down' && pointers.size >= 10) { status('Transport supports at most ten pointers'); return; }
    const point = mapPoint(e.clientX, e.clientY, video.getBoundingClientRect(), geometry, phase !== 'down');
    if (!point) return;
    if (phase === 'down') {
      video.focus(); video.setPointerCapture(e.pointerId);
      video.play().catch(error => status(`Playback needs a viewport interaction: ${error.message}`));
      const slot = [...Array(10).keys()].find(id => !pointers.has(id)); browserPointers.set(e.pointerId,slot);
      if (armed && !capture) { capture = []; armed = false; gesture = null; }
    }
    const event = { ...point, ...geometry, phase, pointerId: browserPointers.get(e.pointerId),
      pressure: e.pointerType === 'mouse' ? 0.5 : Math.max(0.001, e.pressure), timestampMs: performance.now() - epoch };
    try {
      dispatch(event, 'live');
      if (['up','cancel'].includes(phase)) browserPointers.delete(e.pointerId);
      if (capture && pointers.size === 0) { gesture = capture; capture = null; post('gesture', gesture); status(`Captured ${gesture.length} events; Replay gesture sends them once`); }
    } catch (error) { fault(error.message); }
  });
}
video.addEventListener('lostpointercapture', e => {
  const event = pointers.get(browserPointers.get(e.pointerId));
  browserPointers.delete(e.pointerId);
  if (event) { try { dispatch({ ...event, phase: 'cancel', timestampMs: performance.now() - epoch }, 'cancel'); } catch (error) { fault(error.message); } }
});
for (const [name, type] of [['keydown',0],['keyup',1]]) video.addEventListener(name, e => {
  if (e.key === 'Escape') { cancelAll(); post('escape', true); return; }
  e.preventDefault(); if (suppressed || replaying || playback.active || e.repeat) return;
  try { dispatchKey({ key: e.key, phase: type === 0 ? 'down' : 'up', ...geometry, timestampMs: performance.now() - epoch }, 'live'); }
  catch (error) { fault(error.message); }
});
// WPF automation buttons take browser focus. The host's Window.Deactivated cancels
// playback when focus actually leaves the application; live held input still releases here.
window.addEventListener('blur', () => { if (!playback.active) cancelAll(); });
document.addEventListener('visibilitychange', () => { if (document.hidden) { cancelAll(); stopRecording('Viewport hidden; recording interrupted'); } });
async function replay() {
  if (!gesture?.length || !sameGeometry(gesture[0], geometry)) { status('Capture a gesture in the current geometry first'); return; }
  cancelAll(); replaying = true; const generation = replayGeneration;
  const start = performance.now(), first = gesture[0].timestampMs;
  try {
    for (const original of gesture) {
      const remaining = start + original.timestampMs - first - performance.now();
      if (remaining > 0) await new Promise(resolve => setTimeout(resolve, remaining));
      if (generation !== replayGeneration) return;
      if (!sameGeometry(original, geometry)) throw new Error('Geometry changed during diagnostic replay');
      dispatch(original, 'replay');
    }
    status('Diagnostic replay dispatched; verify reception in the APK log');
  } catch (error) { fault(error.message); }
  finally { if (generation === replayGeneration) cancelAll(); }
}
function onFrame(now, metadata) {
  if (previousFrame !== null) { frameTimes.push(now - previousFrame); if (frameTimes.length > 600) frameTimes.shift(); }
  previousFrame = now;
  video.requestVideoFrameCallback(onFrame);
}
video.requestVideoFrameCallback(onFrame);
setInterval(async () => {
  const ready = driver?.event_forwarders.input?.readyState === 'open' && !suppressed;
  if (ready !== inputWasReady) { inputWasReady = ready; post('inputReady', ready); }
  flushRecording(); playback.flushAudit();
  if (!ready && recording) stopRecording('Input channel disconnected; recording interrupted');
  if (!ready && playback.active) { playback.stop('Input channel disconnected'); cancelAll(false); }
  if (inputBatch.length) { post('input', inputBatch); inputBatch = []; }
}, 50);
setInterval(async () => {
  if (!driver?.peerConnection || statsBusy) return;
  statsBusy = true;
  try {
    const report = await driver.peerConnection.getStats();
    const tracks = [];
    report.forEach(s => {
      if (s.type === 'inbound-rtp' || s.type === 'controller-display') tracks.push({ kind: s.kind, framesDecoded: s.framesDecoded,
        framesDropped: s.framesDropped, framesPerSecond: s.framesPerSecond, decoderImplementation: s.decoderImplementation,
        totalDecodeTime: s.totalDecodeTime, jitterBufferDelay: s.jitterBufferDelay,
        jitterBufferEmittedCount: s.jitterBufferEmittedCount, packetsLost: s.packetsLost, jitter: s.jitter,
        totalSamplesReceived: s.totalSamplesReceived, concealedSamples: s.concealedSamples, bytesReceived: s.bytesReceived });
    });
    const sorted = [...frameTimes].sort((a,b) => a-b);
    const percentile = p => sorted.length ? sorted[Math.min(sorted.length-1, Math.floor(sorted.length*p))] : null;
    const quality = video.getVideoPlaybackQuality();
    post('media', { timestampMs: performance.now() - epoch, displayTransport: driver instanceof ControllerDisplay ? 'controller' : 'webrtc', tracks, geometry, muted: video.muted,
      audioTracks: stream.getAudioTracks().map(t => ({ readyState: t.readyState, enabled: t.enabled })),
      controllerAudio: controllerAudio.stats,
      totalVideoFrames: quality.totalVideoFrames, droppedVideoFrames: quality.droppedVideoFrames,
      presentationIntervalMs: { samples: sorted.length, p50: percentile(.5), p95: percentile(.95), p99: percentile(.99) },
      note: 'Presentation callback intervals are not input latency or Android frame times' });
  } catch (error) { status(`Media statistics failed: ${error.message}`); }
  finally { statsBusy = false; }
}, 1000);
window.chrome.webview.addEventListener('message', async ({ data }) => {
  switch (data.kind) {
    case 'recordingStart':
      if (recording || playback.active || suppressed || !geometry.width || driver?.event_forwarders.input?.readyState !== 'open') { post('recordingFault', { id: data.data.id, error: 'Input is not ready for recording' }); break; }
      cancelAll(); recording = { id: data.data.id, count: 0, start: performance.now() - epoch }; recordingBatch = [];
      observe({ ...geometry, kind: 'geometry', phase: 'geometry', timestampMs: recording.start, source: 'live' }); flushRecording();
      post('recordingStarted', { id: recording.id, geometry, clock: 'performance.now', timeOrigin: performance.timeOrigin }); break;
    case 'recordingStop': stopRecording(data.data?.error ?? null); break;
    case 'playbackStart':
      if (recording || playback.active || suppressed) { post('playbackState', { id: data.data.id, state: 'finished', status: 'faulted', error: 'Input is busy or unavailable', completedLoops: 0, receipts: 0 }); break; }
      cancelAll(); runId = data.data.id;
      void playback.start(data.data).catch(error => post('playbackState', { id: runId, state: 'finished', status: 'faulted', error: error.message, receipts: 0, completedLoops: 0 })); break;
    case 'playbackBatch': {
      const batch = data.data, pending = requests.get(batch.requestId);
      if (batch.runId !== runId || !pending) break;
      clearTimeout(pending.timer); requests.delete(batch.requestId);
      if (batch.error) pending.reject(new Error(batch.error)); else pending.resolve(batch); break;
    }
    case 'playbackPause': playback.pause(); break;
    case 'playbackResume': playback.resume(); break;
    case 'playbackStop': playback.stop(data.data?.error ?? null); cancelAll(); break;
    case 'connect': {
      stopRecording('Display reconnected; recording interrupted');
      cancelAll(); await controllerAudio.stop(); driver?.disconnect(); stream.getTracks().forEach(t => { stream.removeTrack(t); t.stop(); });
      token = data.token; gatewayPort = data.port; epoch = performance.now(); sequence = 0; suppressed = false; geometry = { width: 0, height: 0, orientation: 0 };
      gesture = null; frameTimes = []; previousFrame = null;
      const callbacks = { onConnected: track => {
        stream.addTrack(track); video.srcObject = stream; video.muted = data.transport === 'controller';
        video.play().catch(() => status('Click the viewport or Enable audio to allow playback'));
        status(`${track.kind} track received; verify actual output`);
      }, onDisconnected: () => { void controllerAudio.stop(); if (playback.active) playback.stop('Display disconnected'); cancelAll(); stopRecording('Display disconnected'); post('disconnected', true); } };
      if (data.transport === 'controller') {
        driver = new ControllerDisplay(`ws://127.0.0.1:${data.port}/controller`, token,
          { ...callbacks, onError: error => fault(`Controller: ${error.message}`) });
        status('Embedded controller display; enable audio when wanted');
      } else {
      driver = new WsJsepProtocol(`ws://127.0.0.1:${data.port}/api/v1/emulator/ws-jsep`, null,
        { sessionToken: token, maxReconnectAttempts: 0, onError: error => fault(`WebRTC: ${error.message || 'signalling failure'}`) });
      driver.startStream(callbacks);
      }
      const sessionDriver = driver;
      setTimeout(() => { if (driver === sessionDriver && driver?.event_forwarders.input?.readyState !== 'open') fault('Input channel did not open in 15 seconds'); },15000);
      break;
    }
    case 'audio':
      if(driver instanceof ControllerDisplay){
        if(controllerAudio.enabled){await controllerAudio.stop();status('Android audio muted');}
        else await controllerAudio.start(gatewayPort,token,status).catch(e=>status(`Audio: ${e.message}`));
      } else {video.muted=!video.muted;await video.play().catch(e=>status(e.message));}
      break;
    case 'arm': cancelAll(); gesture = null; armed = true; status('Capture armed: one gesture, up to 10 seconds; multi-touch allowed'); break;
    case 'deviceKey': {
      const controls = { back: { key: 'GoBack' }, home: { key: 'GoHome' }, volumeUp: { codeType: 1, keyCode: 115 }, volumeDown: { codeType: 1, keyCode: 114 } };
      if (!Object.hasOwn(controls, data.data)) { status('Unsupported device control'); break; }
      if (suppressed) { status('Reconnect input before using device controls'); break; }
      cancelAll();
      try { dispatchKey({ ...controls[data.data], phase: 'press', ...geometry, timestampMs: performance.now() - epoch }, 'live'); }
      catch (error) { fault(error.message); }
      break;
    }
    case 'replay': await replay(); break;
    case 'cancel': cancelAll(); stopRecording('Interaction interrupted; recording retained as incomplete'); break;
    case 'rotation':
      cancelAll(); suppressed = true; clearTimeout(rotationTimer);
      rotationTimer = setTimeout(() => { updateGeometry(); geometry.orientation = data.degrees;
        observe({ ...geometry, kind: 'geometry', phase: 'geometry', sequence: ++sequence, timestampMs: performance.now()-epoch });
        post('geometry', geometry); suppressed = false;
        status('Rotation requested; geometry follows decoded frame size. Verify orientation in diagnostic APK'); }, 1500);
      break;
    case 'disconnect':
      await controllerAudio.stop();
      cancelAll(); driver?.disconnect(); driver = null;
      stream.getTracks().forEach(track => { stream.removeTrack(track); track.stop(); });
      video.pause(); video.srcObject = null;
      break;
  }
});
post('loaded', { clock: 'performance.now', timeOrigin: performance.timeOrigin });

