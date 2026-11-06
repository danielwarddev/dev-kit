import assert from 'node:assert/strict';
import { test } from 'node:test';
import { encodeWav, finishRender, inspectPcm, readPcmWav, validateConfig } from './audio.mjs';

const config = (overrides = {}) => validateConfig({
  name: 'test', bpm: 120, beatsPerCycle: 4, cycles: 1, loop: false, tailSeconds: 0, ...overrides,
});
const pcm = (frames, level) => {
  const data = Buffer.alloc(frames * 4);
  for (let offset = 0; offset < data.length; offset += 2) data.writeInt16LE(level, offset);
  return data;
};

test('rejects invalid timing, output traversal, and mismatched sections', () => {
  for (const invalid of [
    { bpm: 0 }, { cycles: NaN }, { name: '..\\outside' },
    { loop: true, tailSeconds: 1 }, { sections: [{ cycles: 2 }] },
  ]) assert.throws(() => config(invalid));
});

test('retains only the second complete pass for a loop, without an endpoint fade', () => {
  const first = pcm(88200, 1000);
  const second = pcm(88200, 4000);
  const result = finishRender(encodeWav(Buffer.concat([first, second])), config({ loop: true }));
  const rendered = readPcmWav(result.wav);
  assert.equal(rendered.frames, 88200);
  assert.ok(rendered.pcm.readInt16LE(0) > 23000);
  assert.equal(rendered.pcm.readInt16LE(0), rendered.pcm.readInt16LE(rendered.pcm.length - 2));
  assert.equal(result.report.clippedSamples, 0);
});

test('preserves a non-loop ending tail and normalizes the mix together', () => {
  const data = Buffer.concat([pcm(88200, 5000), pcm(44100, 500)]);
  const result = finishRender(encodeWav(data), config({ tailSeconds: 1 }));
  assert.equal(result.report.durationSeconds, 3);
  assert.equal(result.report.rmsBySecond.length, 3);
  assert.ok(Math.abs(result.report.peakDbfs + 3) < 0.001);
});

test('rejects silence, clipped raw audio, and truncated renders', () => {
  assert.throws(() => finishRender(encodeWav(pcm(88200, 0)), config()), /silent/);
  assert.throws(() => finishRender(encodeWav(pcm(88200, 32767)), config()), /clipped/);
  assert.throws(() => finishRender(encodeWav(pcm(100, 1000)), config()), /shorter/);
});

test('reads padded metadata chunks and detects malformed WAV data', () => {
  const original = encodeWav(pcm(100, 1200));
  const extra = Buffer.from([74, 85, 78, 75, 1, 0, 0, 0, 42, 0]);
  const withMetadata = Buffer.concat([original.subarray(0, 12), extra, original.subarray(12)]);
  withMetadata.writeUInt32LE(withMetadata.length - 8, 4);
  assert.equal(readPcmWav(withMetadata).frames, 100);
  assert.throws(() => readPcmWav(original.subarray(0, original.length - 1)), /Truncated/);
});

test('measures stereo boundary discontinuity and a partial final RMS window', () => {
  const data = pcm(44101, 1000);
  data.writeInt16LE(-1000, data.length - 2);
  const report = inspectPcm(data);
  assert.equal(report.rmsBySecond.length, 2);
  assert.equal(report.boundaryJump, 2000 / 32768);
});
