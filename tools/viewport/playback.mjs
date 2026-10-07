import { sameGeometry } from './input.mjs';

// Stable deterministic algorithm, independent of Math.random and wall-clock time.
export function seededRandom(seed) {
  let value = seed >>> 0;
  return () => { value = (value + 0x6D2B79F5) >>> 0; let n = value;
    n = Math.imul(n ^ (n >>> 15), n | 1); n ^= n + Math.imul(n ^ (n >>> 7), n | 61);
    return ((n ^ (n >>> 14)) >>> 0) / 4294967296; };
}
export function variation(options) {
  const random = seededRandom(options.seed), contacts = new Map(); let previous = 0;
  const bounded = maximum => (random() * 2 - 1) * maximum;
  const transform = original => {
    const e = { ...original };
    if (!options.variations) return e;
    if (e.kind === 'geometry') { e.timestampMs = Math.max(previous, e.timestampMs); previous = e.timestampMs; return e; }
    e.timestampMs = Math.max(previous, 0, e.timestampMs + bounded(options.timingMs)); previous = e.timestampMs;
    if (e.kind === 'touch') {
      if (e.phase === 'down') contacts.set(e.pointerId, { x: bounded(options.coordinatePixels), y: bounded(options.coordinatePixels), phase: random() * Math.PI * 2, start: original.timestampMs });
      const contact = contacts.get(e.pointerId);
      if (!contact) throw new Error('Variation contact has no down event');
      // Smooth path displacement starts at zero; every sample stays within the configured bound.
      const path = options.pathPixels * Math.sin((original.timestampMs - contact.start) / 250) * Math.sin(contact.phase);
      e.x = Math.max(0, Math.min(e.width - 1, Math.round(e.x + contact.x + path)));
      e.y = Math.max(0, Math.min(e.height - 1, Math.round(e.y + contact.y + path)));
      e.normalizedX = e.x / e.width; e.normalizedY = e.y / e.height;
      if (e.phase === 'up' || e.phase === 'cancel') contacts.delete(e.pointerId);
    }
    return e;
  };
  transform.resetLoop = () => { previous = 0; contacts.clear(); }; return transform;
}

export class PlaybackEngine {
  constructor(callbacks) { Object.assign(this, callbacks); this.active = false; this.generation = 0; this.receipts = []; }
  stop(reason = null) { this.stopReason = reason; this.generation++; this.resume(); }
  pause() { if (this.active) { this.pauseRequested = true; this.state({ state: 'pausing', message: 'Pausing at the next released-input boundary' }); } }
  resume() { this.pauseRequested = false; this.resumeWaiter?.(); this.resumeWaiter = null; }
  flushAudit() { if (this.receipts.length) { const batch = this.receipts; this.receipts = []; this.audit(batch); } }
  recordRelease(event) {
    if (!this.active) return;
    this.receipts.push({ ...event, receipt: ++this.receiptCount, loop: this.loop, release: true, scheduledMs: null, dispatchedMs: this.now() - this.origin, lateMs: null });
    if (this.receipts.length >= 64) this.flushAudit();
  }
  async start(run) {
    if (this.active) throw new Error('Playback already active');
    const o = run.options;
    if (!Number.isInteger(o.loops) || o.loops < 1 || o.loops > 10000 || o.loopDelayMs < 0 || o.loopDelayMs > 60000 ||
        o.coordinatePixels < 0 || o.coordinatePixels > 32 || o.pathPixels < 0 || o.pathPixels > 16 || o.timingMs < 0 || o.timingMs > 100)
      throw new Error('Invalid playback options');
    this.active = true; this.pauseRequested = false; this.stopReason = null; this.receiptCount = 0; this.receipts = [];
    const generation = ++this.generation; let completed = 0, result = 'completed', error = null;
    this.origin = this.now(); this.pauseOffset = 0; this.loop = 1;
    this.state({ state: 'loading' });
    const alive = () => generation === this.generation;
    const waitUntil = async deadline => {
      while (alive()) {
        if (this.pauseRequested && this.held() === 0) {
          const before = this.now(); this.state({ state: 'paused' });
          await new Promise(resolve => { this.resumeWaiter = resolve; });
          this.pauseOffset += this.now() - before;
          if (alive()) this.state({ state: 'playing' });
        }
        if (!alive()) return false;
        const remaining = deadline() - this.now(); if (remaining <= 0) return true;
        await this.sleep(Math.min(20, remaining));
      }
      return false;
    };
    try {
      let first = await this.requestBatch(run.id, 0, 1);
      if (!alive()) { result = this.stopReason ? 'faulted' : 'stopped'; error = this.stopReason; return; }
      this.origin = this.now(); let loopStart = this.origin;
      this.state({ state: 'playing' });
      // One generator for the entire run, so loop-to-loop variation is repeatable too.
      const vary = variation(o);
      for (this.loop = 1; alive() && (o.infinite || this.loop <= o.loops); this.loop++) {
        vary.resetLoop();
        let cursor = 0, batch = this.loop === 1 ? first : await this.requestBatch(run.id, 0, this.loop), lastTime = 0;
        while (alive()) {
          if (!Array.isArray(batch.events) || batch.events.length > 128 || (!batch.done && batch.events.length === 0)) throw new Error('Invalid replay batch');
          for (const original of batch.events) {
            if (!alive()) break;
            const event = vary(original); lastTime = Math.max(lastTime, event.timestampMs);
            if (!await waitUntil(() => loopStart + event.timestampMs + this.pauseOffset)) break;
            if (!this.ready() || !sameGeometry(event, this.geometry())) throw new Error('Playback disconnected or geometry changed; stopped safely');
            const target = loopStart + event.timestampMs + this.pauseOffset, actual = this.now();
            if (actual - target > 250) throw new Error('Playback fell over 250 ms behind; stopped rather than sending a stale burst');
            this.dispatch(event);
            this.receipts.push({ ...event, originalTimestampMs: original.timestampMs, receipt: ++this.receiptCount, loop: this.loop,
              scheduledMs: target - this.origin, dispatchedMs: this.now() - this.origin, lateMs: this.now() - target });
            if (this.receipts.length >= 64) this.flushAudit(); cursor++;
          }
          if (!alive() || batch.done) break;
          batch = await this.requestBatch(run.id, cursor, this.loop);
        }
        if (!alive()) break;
        if (this.held() !== 0) throw new Error('Recording ended with held input');
        if (!await waitUntil(() => loopStart + Math.max(run.durationMs, lastTime) + this.pauseOffset)) break;
        completed++; this.state({ state: 'playing', completedLoops: completed });
        loopStart += Math.max(run.durationMs, lastTime) + o.loopDelayMs;
      }
      if (!alive()) { result = this.stopReason ? 'faulted' : 'stopped'; error = this.stopReason; }
    } catch (failure) { result = alive() || this.stopReason ? 'faulted' : 'stopped'; error = this.stopReason || (alive() ? failure.message : null); }
    finally {
      this.release(); this.flushAudit(); this.active = false; this.resumeWaiter = null;
      this.state({ state: 'finished', status: result, error, completedLoops: completed, receipts: this.receiptCount });
    }
  }
}
