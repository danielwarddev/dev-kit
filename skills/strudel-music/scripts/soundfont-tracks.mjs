import { encodeMidi } from './midi.mjs';

export function groupSoundfontTracks(events, config, defaultBankId = config.bankId) {
  const midi = encodeMidi(events, config);
  const activeOrbits = new Set(midi.tracks.filter(track => track.notes > 0).map(track => track.orbit));
  const banks = new Set();
  for (const track of config.midiTracks) {
    if (!activeOrbits.has(track.orbit)) continue;
    const bankId = track.bankId ?? defaultBankId;
    if (typeof bankId !== 'string' || !bankId.trim()) throw new Error(`A SoundFont bankId is required for orbit ${track.orbit}`);
    banks.add(bankId);
  }
  const groups = [...banks].map(bankId => {
    const tracks = config.midiTracks.filter(track => (track.bankId ?? defaultBankId) === bankId);
    const orbits = new Set(tracks.map(track => track.orbit));
    const groupEvents = events.filter(event => orbits.has(Number(event.value.orbit ?? 1)));
    return {
      bankId, orbits: [...orbits],
      midi: tracks.length === config.midiTracks.length ? midi : encodeMidi(groupEvents, { ...config, midiTracks: tracks }),
    };
  });
  return { midi, groups };
}
