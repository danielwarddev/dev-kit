export function validateConfig(config) {
  if (!/^[a-z0-9][a-z0-9_-]*$/i.test(config.name ?? '')) {
    throw new Error('name must be a simple filename without directories');
  }
  for (const key of ['bpm', 'beatsPerCycle', 'cycles']) {
    if (!Number.isFinite(config[key]) || config[key] <= 0) {
      throw new Error(`${key} must be a positive finite number`);
    }
  }
  if (typeof config.loop !== 'boolean') {
    throw new Error('loop must be explicitly true or false');
  }
  const tail = config.tailSeconds ?? (config.loop ? 0 : 3);
  if (!Number.isFinite(tail) || tail < 0 || (config.loop && tail !== 0)) {
    throw new Error('tailSeconds must be nonnegative and zero for loops');
  }
  const duration = config.cycles * config.beatsPerCycle * 60 / config.bpm;
  if (duration + tail > 600) {
    throw new Error('Render at most ten minutes per cue');
  }
  if (config.sections) {
    if (!Array.isArray(config.sections) || config.sections.some((s) => !Number.isFinite(s.cycles) || s.cycles <= 0)) {
      throw new Error('sections must contain positive cycle lengths');
    }
    const total = config.sections.reduce((sum, section) => sum + section.cycles, 0);
    if (Math.abs(total - config.cycles) > 1e-8) {
      throw new Error('section lengths must sum to cycles');
    }
  }
  return { ...config, tailSeconds: tail, durationSeconds: duration };
}

export function readPcmWav(buffer) {
  if (buffer.toString('ascii', 0, 4) !== 'RIFF' || buffer.toString('ascii', 8, 12) !== 'WAVE') {
    throw new Error('Expected a RIFF WAV render');
  }
  let format;
  let pcm;
  for (let offset = 12; offset + 8 <= buffer.length;) {
    const kind = buffer.toString('ascii', offset, offset + 4);
    const length = buffer.readUInt32LE(offset + 4);
    const start = offset + 8;
    if (start + length > buffer.length) throw new Error('Truncated WAV chunk');
    if (kind === 'fmt ' && length >= 16) {
      format = {
        encoding: buffer.readUInt16LE(start),
        channels: buffer.readUInt16LE(start + 2),
        sampleRate: buffer.readUInt32LE(start + 4),
        bits: buffer.readUInt16LE(start + 14),
      };
    }
    if (kind === 'data') pcm = buffer.subarray(start, start + length);
    offset = start + length + (length % 2);
  }
  if (!format || !pcm || format.encoding !== 1 || format.bits !== 16 || format.channels !== 2 || format.sampleRate !== 44100) {
    throw new Error('Expected 44.1 kHz stereo 16-bit PCM');
  }
  if (!pcm.length || pcm.length % 4) throw new Error('Invalid PCM frame data');
  return { ...format, pcm, frames: pcm.length / 4 };
}

export function inspectPcm(pcm, sampleRate = 44100) {
  let peak = 0;
  let squares = 0;
  let clippedSamples = 0;
  let windowSquares = 0;
  let windowSamples = 0;
  const rmsBySecond = [];
  const samplesPerSecond = sampleRate * 2;
  for (let offset = 0; offset < pcm.length; offset += 2) {
    const value = pcm.readInt16LE(offset);
    if (value === -32768 || value === 32767) clippedSamples++;
    const level = value / 32768;
    peak = Math.max(peak, Math.abs(level));
    squares += level * level;
    windowSquares += level * level;
    windowSamples++;
    if (windowSamples === samplesPerSecond || offset + 2 === pcm.length) {
      rmsBySecond.push(Math.sqrt(windowSquares / windowSamples));
      windowSquares = 0;
      windowSamples = 0;
    }
  }
  const boundaryJump = Math.max(
    Math.abs(pcm.readInt16LE(0) - pcm.readInt16LE(pcm.length - 4)),
    Math.abs(pcm.readInt16LE(2) - pcm.readInt16LE(pcm.length - 2)),
  ) / 32768;
  return {
    durationSeconds: pcm.length / (sampleRate * 4),
    peak,
    peakDbfs: peak ? 20 * Math.log10(peak) : null,
    rmsDbfs: squares ? 20 * Math.log10(Math.sqrt(squares / (pcm.length / 2))) : null,
    clippedSamples,
    boundaryJump,
    rmsBySecond,
  };
}

export function finishRender(rawWav, config) {
  const audio = readPcmWav(rawWav);
  const raw = inspectPcm(audio.pcm);
  if (raw.clippedSamples) throw new Error(`Raw render clipped ${raw.clippedSamples} samples; reduce source gains`);
  const startFrame = config.loop ? Math.round(config.durationSeconds * audio.sampleRate) : 0;
  const frameCount = Math.round((config.durationSeconds + config.tailSeconds) * audio.sampleRate);
  if (audio.frames < startFrame + frameCount) throw new Error('Render shorter than expected');
  const pcm = Buffer.from(audio.pcm.subarray(startFrame * 4, (startFrame + frameCount) * 4));
  const before = inspectPcm(pcm);
  if (before.peak < 0.0001) throw new Error('Render is silent or effectively silent');
  const gain = (10 ** (-3 / 20)) / before.peak;
  for (let offset = 0; offset < pcm.length; offset += 2) {
    pcm.writeInt16LE(Math.round(pcm.readInt16LE(offset) * gain), offset);
  }
  return {
    wav: encodeWav(pcm),
    report: { rawPeakDbfs: raw.peakDbfs, normalizationGain: gain, ...inspectPcm(pcm) },
  };
}

export function encodeWav(pcm) {
  const header = Buffer.alloc(44);
  header.write('RIFF', 0);
  header.writeUInt32LE(36 + pcm.length, 4);
  header.write('WAVEfmt ', 8);
  header.writeUInt32LE(16, 16);
  header.writeUInt16LE(1, 20);
  header.writeUInt16LE(2, 22);
  header.writeUInt32LE(44100, 24);
  header.writeUInt32LE(44100 * 4, 28);
  header.writeUInt16LE(4, 32);
  header.writeUInt16LE(16, 34);
  header.write('data', 36);
  header.writeUInt32LE(pcm.length, 40);
  return Buffer.concat([header, pcm]);
}
