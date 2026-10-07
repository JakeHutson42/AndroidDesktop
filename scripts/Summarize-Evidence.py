"""Summarize Phase 0 counters. Does not turn missing hardware checks into passes."""
import argparse
import json
from pathlib import Path
import statistics

parser = argparse.ArgumentParser()
parser.add_argument('run',type=Path)
args=parser.parse_args()
samples=[]; processes=[]; warnings=[]
for line in (args.run/'measurements.jsonl').read_text(encoding='utf-8-sig').splitlines():
    row=json.loads(line)
    if row['kind']=='viewport.media': samples.append(row['data'])
    if row['kind']=='processes': processes.append(row['data'])
    if row['kind'] in ('viewport.fault','viewport.evidenceOverflow','viewport.releaseUnverified'): warnings.append(row)
summary={'run':str(args.run),'mediaSamples':len(samples),'processSamples':len(processes),
    'incomplete':(args.run/'INCOMPLETE.txt').exists(),'warnings':warnings,
    'hardwareAcceptance':'UNVERIFIED: media counters do not establish audio playback, gesture delivery or input latency'}
if processes:
    summary['maxSumWorkingSetBytes']=max(sum(p['WorkingSet64'] for p in group) for group in processes)
    summary['maxSumPrivateBytes']=max(sum(p['PrivateMemorySize64'] for p in group) for group in processes)
    summary['maxObservedProcessCount']=max(map(len,processes))
if samples:
    summary['lastMediaSample']=samples[-1]
    intervals=[s['presentationIntervalMs']['p95'] for s in samples if s['presentationIntervalMs']['p95'] is not None]
    if intervals: summary['medianOfWindowedPresentationP95Ms']=statistics.median(intervals)
    # Decode/jitter counters are cumulative; use consecutive deltas only within an increasing stream.
    rates=[]; delays=[]
    for first,last in zip(samples,samples[1:]):
        seconds=(last['timestampMs']-first['timestampMs'])/1000
        a=next((t for t in first['tracks'] if t['kind']=='video'),None)
        b=next((t for t in last['tracks'] if t['kind']=='video'),None)
        if a and b and seconds>0 and b.get('framesDecoded',0)>=a.get('framesDecoded',0):
            rates.append((b.get('framesDecoded',0)-a.get('framesDecoded',0))/seconds)
            emitted=b.get('jitterBufferEmittedCount',0)-a.get('jitterBufferEmittedCount',0)
            if emitted>0: delays.append(1000*(b.get('jitterBufferDelay',0)-a.get('jitterBufferDelay',0))/emitted)
    if rates: summary['meanDecodedFps']=statistics.mean(rates)
    if delays: summary['meanVideoJitterBufferDelayMs']=statistics.mean(delays)
path=args.run/'summary.json'
path.write_text(json.dumps(summary,indent=2),encoding='utf-8')
print(path)
