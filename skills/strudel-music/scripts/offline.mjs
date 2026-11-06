export async function renderToPcm(api, pattern, cps, endCycle, name) {
  const sampleRate = 44100;
  await api.getAudioContext().close();
  // Warmup and retained loop lengths are rounded separately; leave a guard frame.
  const frameCount = Math.ceil(endCycle / cps * sampleRate) + 1;
  const context = new OfflineAudioContext(2, frameCount, sampleRate);
  api.setAudioContext(context);
  api.setSuperdoughAudioController(null);
  await api.initAudio({ maxPolyphony: 128, multiChannelOrbits: false });

  async function schedule(begin, end) {
    const events = pattern.queryArc(begin, end, { _cps: cps })
      .filter((hap) => hap.hasOnset())
      .sort((a, b) => Number(a.whole.begin) - Number(b.whole.begin));
    for (const hap of events) {
      hap.ensureObjectValue();
      await api.superdough(hap.value, Number(hap.whole.begin) / cps, Number(hap.duration) / cps, cps, Number(hap.whole.begin) / cps);
    }
  }

  try {
    // A one-cycle lookahead lets ended nodes be reclaimed before the next batch.
    // Scheduling an entire song up front retains thousands of live audio nodes.
    let next = Math.min(2, endCycle);
    await schedule(0, next);
    let pause = next < endCycle ? context.suspend((next - 1) / cps) : null;
    const rendering = context.startRendering();
    if (pause) await pause;
    while (next < endCycle) {
      const end = Math.min(next + 1, endCycle);
      await schedule(next, end);
      next = end;
      pause = next < endCycle ? context.suspend((next - 1) / cps) : null;
      await context.resume();
      if (pause) await pause;
      if (Number.isInteger(next) && next % 8 === 0) console.log(`[webaudio] scheduled ${next}/${endCycle} cycles`);
    }
    const audio = await rendering;
    const left = audio.getChannelData(0);
    const right = audio.getChannelData(1);
    const bytes = new ArrayBuffer(audio.length * 4);
    const pcm = new DataView(bytes);
    let peak = 0;
    for (let frame = 0; frame < audio.length; frame++) {
      const l = left[frame];
      const r = right[frame];
      if (!Number.isFinite(l) || !Number.isFinite(r)) throw new Error('Non-finite rendered audio');
      peak = Math.max(peak, Math.abs(l), Math.abs(r));
      if (peak >= 1) throw new Error(`Raw floating-point audio clips at ${peak}; reduce source gains`);
      pcm.setInt16(frame * 4, Math.round(l * 32767), true);
      pcm.setInt16(frame * 4 + 2, Math.round(r * 32767), true);
    }
    const url = URL.createObjectURL(new Blob([bytes], { type: 'application/octet-stream' }));
    const link = document.createElement('a');
    link.href = url;
    link.download = `${name}.pcm`;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  } finally {
    api.resetGlobalEffects();
    api.setSuperdoughAudioController(null);
    api.setAudioContext(null);
  }
}
