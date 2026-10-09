import argparse,json,statistics,pathlib

def q(values,p):
    if not values: return None
    v=sorted(values); idx=(len(v)-1)*p; lo=int(idx); hi=min(lo+1,len(v)-1)
    return round(v[lo]+(v[hi]-v[lo])*(idx-lo),3)
def summary(path):
    d=json.loads(path.read_text(encoding='utf-8-sig'))
    samples=[s for s in d['samples'] if s['processes']]
    totals=[sum(p['cpuPercentOfMachine'] for p in s['processes']) for s in samples]
    names=sorted({p['name'] for s in samples for p in s['processes']})
    procs={n:{'meanCpuPercent':round(statistics.mean(sum(p['cpuPercentOfMachine'] for p in s['processes'] if p['name']==n) for s in samples),3),'peakWorkingSetMiB':round(max(sum(p['workingSetMiB'] for p in s['processes'] if p['name']==n) for s in samples),1)} for n in names}
    ticks=sorted(set(d.get('guestPresentTimestampsNs',[]))); frame_ms=[(b-a)/1e6 for a,b in zip(ticks,ticks[1:])]
    gpu=[list(map(float,line.split(',')[1:])) for line in d.get('gpuSamples',[]) if len(line.split(','))==5]
    return {'mode':d['mode'],'workload':d['workload'],'requestedSeconds':d['requestedSampleSeconds'],'sampleCount':len(samples),'cpuMeanPercent':round(statistics.mean(totals),3),'cpuP95Percent':q(totals,.95),'processes':procs,'dispatcherP95Ms':q(d['dispatcherDelayMs'],.95),'dispatcherMaxMs':max(d['dispatcherDelayMs']),'guestFrameCount':len(ticks),'guestFramesPerSecond':round((len(ticks)-1)*1e9/(ticks[-1]-ticks[0]),3) if len(ticks)>1 else None,'guestFrameIntervalP50Ms':q(frame_ms,.5),'guestFrameIntervalP95Ms':q(frame_ms,.95),'guestFrameIntervalP99Ms':q(frame_ms,.99),'wholeGpuMeanPercent':round(statistics.mean(row[0] for row in gpu),2) if gpu else None,'wholeGpuPeakPercent':max(row[0] for row in gpu) if gpu else None,'wholeGpuMeanPowerWatts':round(statistics.mean(row[3] for row in gpu),2) if gpu else None,'startupSeconds':d['startupSeconds'],'shutdownSeconds':d['shutdownSeconds'],'shutdownUiMaxIntervalMs':max(d['shutdownUiIntervalsMs'],default=None),'saveReopen':d.get('saveReopen'),'error':d.get('error'),'scope':'Android compositor frame cadence; Windows displayed FPS and input-to-visible latency are not measured. GPU values cover the entire GPU.'}

if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('directory',type=pathlib.Path);ap.add_argument('--output',type=pathlib.Path);args=ap.parse_args()
    values=[summary(args.directory/f'{mode}-phase-2.json') for mode in ('native','standalone','controller') if (args.directory/f'{mode}-phase-2.json').exists()]
    result=json.dumps(values,indent=2)
    if args.output: args.output.write_text(result+'\n',encoding='utf-8')
    else: print(result)
