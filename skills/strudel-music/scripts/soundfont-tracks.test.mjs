import assert from 'node:assert/strict';
import { test } from 'node:test';
import { encodeMidi } from './midi.mjs';
import { groupSoundfontTracks } from './soundfont-tracks.mjs';

const config = {
  bpm: 120, beatsPerCycle: 4, cycles: 4, loop: true, tailSeconds: 0,
  midiTracks: [
    { orbit: 1, channel: 1, program: 4 },
    { orbit: 2, channel: 2, program: 33 },
  ],
};
const events = [
  { begin: 0, duration: 0.5, value: { note: 'c4', orbit: 1 } },
  { begin: 1, duration: 0.25, value: { note: 'e3', orbit: 2 } },
  { begin: 4, duration: 0.5, value: { note: 'c4', orbit: 1 } },
];

test('one bank preserves the existing complete MIDI and combines its instrument parts', () => {
  const result = groupSoundfontTracks(events, config, 'foundation');
  assert.deepEqual(result.midi.buffer, encodeMidi(events, config).buffer);
  assert.equal(result.groups.length, 1);
  assert.equal(result.groups[0].midi, result.midi);
  assert.deepEqual(result.groups[0].orbits, [1, 2]);
});

test('track bank overrides route each note exactly once without rebasing its timing', () => {
  const mixed = { ...config, midiTracks: [{ ...config.midiTracks[0], bankId: 'piano' }, config.midiTracks[1]] };
  const result = groupSoundfontTracks(events, mixed, 'foundation');
  assert.deepEqual(result.groups.map(group => group.bankId), ['piano', 'foundation']);
  assert.deepEqual(result.groups.map(group => group.midi.noteCount), [2, 1]);
  assert.equal(result.groups.reduce((sum, group) => sum + group.midi.noteCount, 0), result.midi.noteCount);
  const expected = encodeMidi([events[0], events[2]], { ...mixed, midiTracks: [mixed.midiTracks[0]] });
  assert.deepEqual(result.groups[0].midi.buffer, expected.buffer);
});

test('all explicit track banks work without a default, while unmapped notes still fail', () => {
  const explicit = {
    ...config,
    midiTracks: config.midiTracks.map((track, index) => ({ ...track, bankId: index ? 'bass' : 'keys' })),
  };
  assert.equal(groupSoundfontTracks(events, explicit).groups.length, 2);
  assert.throws(() => groupSoundfontTracks(events, config), /bankId is required/);
  assert.throws(() => groupSoundfontTracks([...events, { begin: 0, duration: 1, value: { orbit: 99, note: 'c4' } }], explicit), /No MIDI mapping/);
});

test('unused or fully muted banks are not selected for rendering or download', () => {
  const extra = { ...config, midiTracks: [...config.midiTracks, { orbit: 3, channel: 3, program: 0, bankId: 'unused' }] };
  const muted = { begin: 0, duration: 1, value: { orbit: 3, note: 'c4', velocity: 0 } };
  const result = groupSoundfontTracks([...events, muted], extra, 'foundation');
  assert.deepEqual(result.groups.map(group => group.bankId), ['foundation']);
  assert.deepEqual(result.groups[0].orbits, [1, 2]);
});
