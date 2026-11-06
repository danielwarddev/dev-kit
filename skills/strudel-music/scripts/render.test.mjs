import assert from 'node:assert/strict';
import { execFile } from 'node:child_process';
import { mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { promisify } from 'node:util';
import { test } from 'node:test';

const execute = promisify(execFile);
const renderer = fileURLToPath(new URL('./render.mjs', import.meta.url));

async function render(source, extra = {}) {
  const directory = await mkdtemp(path.join(os.tmpdir(), 'strudel-music-test-'));
  try {
    const sourcePath = path.join(directory, 'test.strudel.js');
    const configPath = path.join(directory, 'test.json');
    await writeFile(sourcePath, source);
    await writeFile(configPath, JSON.stringify({
      name: 'test', bpm: 120, beatsPerCycle: 1, cycles: 1, loop: false, tailSeconds: 0, ...extra,
    }));
    const result = await execute(process.execPath, [renderer, sourcePath, configPath, directory], { timeout: 90000 });
    const report = JSON.parse(await readFile(path.join(directory, 'test.validation.json'), 'utf8'));
    return { ...result, report };
  } finally {
    await rm(directory, { recursive: true, force: true });
  }
}

test('real offline Strudel rendering preserves a non-loop tail', { timeout: 100000 }, async () => {
  const { report } = await render(
    'setcpm(120); note("c4").s("sine").gain(0.1).release(0.1).room(0.2).roomsize(1)',
    { tailSeconds: 0.25 },
  );
  assert.equal(report.durationSeconds, 0.75);
  assert.equal(report.pattern.sounds.sine, 1);
  assert.equal(report.clippedSamples, 0);
  assert.ok(Math.abs(report.peakDbfs + 3) < 0.001);
});

test('chunked offline scheduling crosses cycle boundaries and preserves loop length', { timeout: 100000 }, async () => {
  const { report } = await render(
    'setcpm(120); note("<c4 d4 e4 f4>").s("sine").gain(0.1).room(0.2).roomsize(1)',
    { cycles: 4, loop: true },
  );
  assert.equal(report.durationSeconds, 2);
  assert.equal(report.pattern.sounds.sine, 4);
  assert.equal(report.clippedSamples, 0);
});

test('fractional-frame loop periods retain their exact rounded length', { timeout: 100000 }, async () => {
  const { report } = await render(
    'setcpm(132/4); note("<c4 d4>").s("sine").gain(0.1)',
    { bpm: 132, beatsPerCycle: 4, cycles: 2, loop: true },
  );
  const expectedFrames = Math.round(2 * 4 * 60 / 132 * 44100);
  assert.equal(Math.round(report.durationSeconds * 44100), expectedFrames);
  assert.equal(report.clippedSamples, 0);
});

test('missing sound fails instead of producing a successful partially silent render', { timeout: 100000 }, async () => {
  await assert.rejects(render('note("c4").s("not_registered_test_voice")'), (error) => {
    assert.match(error.stderr, /has not been registered/);
    return true;
  });
});

test('bank-prefixed sounds resolve using the same naming rules as the engine', { timeout: 100000 }, async () => {
  const { report } = await render(
    `soundAlias('sine', 'testbank_tone'); note("c4").s("tone").bank('TestBank').gain(0.1)`,
  );
  assert.equal(report.pattern.sounds.testbank_tone, 1);
});

test('registered but undecodable sample fails instead of rendering silent audio', { timeout: 100000 }, async () => {
  await assert.rejects(render(
    `await samples({bad_audio: location.origin + '/strudel.mjs'}); s("bad_audio")`,
  ), (error) => {
    assert.match(error.stderr, /decode|audio data|EncodingError|could not load/i);
    return true;
  });
});

test('invalid Strudel code fails instead of silently falling back', { timeout: 100000 }, async () => {
  await assert.rejects(render('note("c4").thisMethodDoesNotExist()'), (error) => {
    assert.match(error.stderr, /Source did not produce a Strudel Pattern/);
    return true;
  });
});
