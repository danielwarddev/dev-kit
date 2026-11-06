import assert from 'node:assert/strict';
import { test } from 'node:test';
import { encodeMidi, midiNote } from './midi.mjs';

const config = {
  bpm: 120, beatsPerCycle: 4, cycles: 2, loop: false, tailSeconds: 0,
  midiTracks: [{ orbit: 1, channel: 2, program: 27 }],
};

function channelEvents(buffer, trackIndex = 1) {
  let offset = 14;
  for (let index = 0; index < trackIndex; index++) offset += 8 + buffer.readUInt32BE(offset + 4);
  const end = offset + 8 + buffer.readUInt32BE(offset + 4);
  offset += 8;
  let tick = 0;
  const result = [];
  const variable = () => {
    let value = 0;
    let byte;
    do { byte = buffer[offset++]; value = (value << 7) | (byte & 0x7f); } while (byte & 0x80);
    return value;
  };
  while (offset < end) {
    tick += variable();
    const status = buffer[offset++];
    if (status === 0xff) {
      offset++;
      const length = variable();
      offset += length;
    } else {
      const length = (status & 0xf0) === 0xc0 ? 1 : 2;
      const data = [...buffer.subarray(offset, offset + length)];
      offset += length;
      result.push({ tick, status, data });
    }
  }
  return result;
}

test('exports pitch, velocity, channel and clipped note duration in MIDI units', () => {
  const result = encodeMidi([{ begin: 0.25, duration: 0.125, value: { orbit: 1, note: 'c#4', velocity: 0.5, clip: 0.5 } }], config);
  const events = channelEvents(result.buffer);
  assert.deepEqual(events.filter(event => (event.status & 0xf0) === 0x90), [{ tick: 480, status: 0x91, data: [61, 64] }]);
  assert.deepEqual(events.filter(event => (event.status & 0xf0) === 0x80), [{ tick: 600, status: 0x81, data: [61, 0] }]);
  assert.ok(events.some(event => event.status === 0xc1 && event.data[0] === 27));
});

test('retriggering a held pitch releases it before the next attack', () => {
  const result = encodeMidi([
    { begin: 0, duration: 1, value: { orbit: 1, note: 'c4' } },
    { begin: 0.5, duration: 1, value: { orbit: 1, note: 'c4' } },
  ], config);
  const transitions = channelEvents(result.buffer).filter(event => event.tick === 960);
  assert.deepEqual(transitions.map(event => event.status), [0x81, 0x91]);
  assert.equal(result.noteCount, 2);
});

test('refuses unmapped tracks and fully muted patterns instead of guessing an instrument', () => {
  assert.throws(() => encodeMidi([{ begin: 0, duration: 1, value: { orbit: 7, note: 60 } }], config), /No MIDI mapping/);
  assert.throws(() => encodeMidi([{ begin: 0, duration: 1, value: { orbit: 1, note: 60, velocity: 0 } }], config), /no playable/);
});

test('handles enharmonic pitches and rejects unsupported microtones or out-of-range notes', () => {
  assert.equal(midiNote('Db4'), 61);
  assert.equal(midiNote('61'), 61);
  assert.equal(midiNote('A-1'), 9);
  for (const value of [60.5, 128, 'C-2', 'unknown']) assert.throws(() => midiNote(value));
});

test('opt-in common-tone ties sustain one voice without swallowing actual gaps', () => {
  const result = encodeMidi([
    { begin: 0, duration: 1, value: { orbit: 1, note: 60 } },
    { begin: 1, duration: 0.5, value: { orbit: 1, note: 60 } },
    { begin: 1.75, duration: 0.25, value: { orbit: 1, note: 60 } },
  ], { ...config, midiTracks: [{ ...config.midiTracks[0], tieCommonNotes: true }] });
  const events = channelEvents(result.buffer);
  assert.deepEqual(events.filter(event => (event.status & 0xf0) === 0x90).map(event => event.tick), [0, 3360]);
  assert.deepEqual(events.filter(event => (event.status & 0xf0) === 0x80).map(event => event.tick), [2880, 3840]);
  assert.equal(result.sourceNoteCount, 3);
  assert.equal(result.noteCount, 2);
});

test('pedal automation repeats for loop warmup and releases at the music end', () => {
  const result = encodeMidi([
    { begin: 0, duration: 1, value: { orbit: 1, note: 60 } },
    { begin: 2, duration: 1, value: { orbit: 1, note: 60 } },
  ], {
    ...config, loop: true, midiTracks: [{
      ...config.midiTracks[0], controllerEvents: [
        { cycle: 0, controller: 64, value: 127 },
        { cycle: 0.75, controller: 64, value: 0 },
      ],
    }],
  });
  const pedals = channelEvents(result.buffer).filter(event => event.status === 0xb1 && event.data[0] === 64);
  assert.deepEqual(pedals.map(event => [event.tick, event.data[1]]), [
    [0, 0], [0, 127], [1440, 0], [3840, 127], [5280, 0], [7680, 0],
  ]);
});

test('rejects malformed or out-of-range controller automation', () => {
  for (const event of [
    { cycle: -1, controller: 64, value: 127 },
    { cycle: 3, controller: 64, value: 127 },
    { cycle: 0, controller: 128, value: 127 },
    { cycle: 0, controller: 64, value: 128 },
  ]) {
    assert.throws(() => encodeMidi([], {
      ...config, midiTracks: [{ ...config.midiTracks[0], controllerEvents: [event] }],
    }), /controller event/);
  }
});
