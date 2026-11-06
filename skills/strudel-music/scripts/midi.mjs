const PPQ = 480;

export function midiNote(value) {
  if (typeof value === 'string' && /^-?\d+$/.test(value)) value = Number(value);
  if (typeof value === 'string') {
    const match = /^([a-g])([#b]?)(-?\d+)$/i.exec(value);
    if (!match) throw new Error(`Unsupported MIDI note: ${value}`);
    value = (Number(match[3]) + 1) * 12
      + { c: 0, d: 2, e: 4, f: 5, g: 7, a: 9, b: 11 }[match[1].toLowerCase()]
      + (match[2] === '#' ? 1 : match[2] === 'b' ? -1 : 0);
  }
  if (!Number.isInteger(value) || value < 0 || value > 127) throw new Error(`MIDI note out of range: ${value}`);
  return value;
}

function variableLength(value) {
  if (!Number.isInteger(value) || value < 0 || value > 0x0fffffff) throw new Error('Invalid MIDI delta time');
  const bytes = [value & 0x7f];
  while ((value >>>= 7)) bytes.unshift((value & 0x7f) | 0x80);
  return Buffer.from(bytes);
}

function track(events, endTick) {
  events.sort((a, b) => a.tick - b.tick || a.priority - b.priority);
  const pieces = [];
  let previous = 0;
  for (const event of events) {
    pieces.push(variableLength(event.tick - previous), Buffer.from(event.bytes));
    previous = event.tick;
  }
  pieces.push(variableLength(Math.max(0, endTick - previous)), Buffer.from([0xff, 0x2f, 0]));
  const body = Buffer.concat(pieces);
  const header = Buffer.alloc(8);
  header.write('MTrk');
  header.writeUInt32BE(body.length, 4);
  return Buffer.concat([header, body]);
}

export function encodeMidi(events, config) {
  if (!Array.isArray(config.midiTracks) || !config.midiTracks.length) throw new Error('midiTracks must map Strudel orbits to MIDI instruments');
  const channels = new Set();
  const orbits = new Map();
  for (const instrument of config.midiTracks) {
    if (!Number.isInteger(instrument.orbit) || orbits.has(instrument.orbit)) throw new Error('MIDI orbits must be unique integers');
    if (!Number.isInteger(instrument.channel) || instrument.channel < 1 || instrument.channel > 16 || channels.has(instrument.channel)) {
      throw new Error('MIDI channels must be unique integers from 1 to 16');
    }
    if (!Number.isInteger(instrument.program) || instrument.program < 0 || instrument.program > 127) throw new Error('MIDI programs must be 0-127');
    for (const key of ['volume', 'pan', 'reverb', 'chorus']) {
      if (instrument[key] !== undefined && (!Number.isInteger(instrument[key]) || instrument[key] < 0 || instrument[key] > 127)) {
        throw new Error(`${key} must be a MIDI value from 0 to 127`);
      }
      if (instrument.tieCommonNotes !== undefined && typeof instrument.tieCommonNotes !== 'boolean') {
        throw new Error('tieCommonNotes must be a boolean');
      }
      if (instrument.controllerEvents !== undefined && !Array.isArray(instrument.controllerEvents)) {
        throw new Error('controllerEvents must be an array');
      }
      for (const event of instrument.controllerEvents ?? []) {
        if (!Number.isFinite(event.cycle) || event.cycle < 0 || event.cycle > config.cycles
          || !Number.isInteger(event.controller) || event.controller < 0 || event.controller > 127
          || !Number.isInteger(event.value) || event.value < 0 || event.value > 127) {
          throw new Error('Invalid MIDI controller event');
        }
      }
    }
    channels.add(instrument.channel);
    orbits.set(instrument.orbit, { instrument, notes: [] });
  }
  const cycles = config.cycles * (config.loop ? 2 : 1);
  const musicEnd = Math.round(cycles * config.beatsPerCycle * PPQ);
  const endTick = musicEnd + Math.round((config.tailSeconds ?? 0) * config.bpm / 60 * PPQ);
  let sourceNoteCount = 0;
  for (const event of events) {
    const destination = orbits.get(Number(event.value.orbit ?? 1));
    if (!destination) throw new Error(`No MIDI mapping for orbit ${event.value.orbit ?? 1}`);
    const velocity = Number(event.value.velocity ?? 0.7);
    const clip = Number(event.value.clip ?? 1);
    if (!Number.isFinite(velocity) || velocity < 0 || velocity > 1 || !Number.isFinite(clip) || clip < 0) {
      throw new Error('Invalid MIDI note velocity or clip');
    }
    if (velocity === 0 || clip === 0) continue;
    const begin = Math.max(0, Math.round(event.begin * config.beatsPerCycle * PPQ));
    const end = Math.min(endTick, Math.max(begin + 1, Math.round((event.begin + event.duration * clip) * config.beatsPerCycle * PPQ)));
    destination.notes.push({ note: midiNote(event.value.note), begin, end, velocity: Math.max(1, Math.round(velocity * 127)) });
    sourceNoteCount++;
  }
  if (!sourceNoteCount) throw new Error('Source contains no playable MIDI notes');
  const tempo = Math.round(60000000 / config.bpm);
  if (tempo < 1 || tempo > 0xffffff) throw new Error('Tempo is outside the MIDI tempo range');
  const tempoBytes = Buffer.alloc(6);
  tempoBytes.set([0xff, 0x51, 3]);
  tempoBytes.writeUIntBE(tempo, 3, 3);
  const tracks = [track([
    { tick: 0, priority: 0, bytes: [0xf0, 5, 0x7e, 0x7f, 9, 1, 0xf7] },
    { tick: 0, priority: 1, bytes: tempoBytes },
  ], endTick)];
  const summary = [];
  let noteCount = 0;
  for (const { instrument, notes } of orbits.values()) {
    const channel = instrument.channel - 1;
    const output = [
      { tick: 0, priority: 0, bytes: [0xc0 | channel, instrument.program] },
      ...[[7, instrument.volume ?? 100], [10, instrument.pan ?? 64], [91, instrument.reverb ?? 20], [93, instrument.chorus ?? 0], [64, 0]]
        .map(([controller, value]) => ({ tick: 0, priority: 0, bytes: [0xb0 | channel, controller, value] })),
    ];
    for (let pass = 0; pass < (config.loop ? 2 : 1); pass++) {
      for (const event of instrument.controllerEvents ?? []) {
        output.push({
          tick: Math.round((event.cycle + pass * config.cycles) * config.beatsPerCycle * PPQ),
          priority: 0, bytes: [0xb0 | channel, event.controller, event.value],
        });
      }
    }
    const byPitch = new Map();
    for (const note of notes) {
      if (!byPitch.has(note.note)) byPitch.set(note.note, []);
      byPitch.get(note.note).push(note);
    }
    let emitted = 0;
    for (const samePitch of byPitch.values()) {
      samePitch.sort((a, b) => a.begin - b.begin);
      const articulated = [];
      for (const note of samePitch) {
        const previous = articulated.at(-1);
        if (instrument.tieCommonNotes && previous && note.begin <= previous.end) {
          previous.end = Math.max(previous.end, note.end);
        } else {
          articulated.push({ ...note });
        }
      }
      for (let index = 0; index < articulated.length; index++) {
        const note = articulated[index];
        const next = articulated[index + 1];
        // Retriggered MIDI pitches must release the old note before the new one.
        const end = next ? Math.min(note.end, next.begin) : note.end;
        if (end <= note.begin) throw new Error('Duplicate simultaneous MIDI pitch in one track');
        output.push(
          { tick: note.begin, priority: 2, bytes: [0x90 | channel, note.note, note.velocity] },
          { tick: end, priority: 1, bytes: [0x80 | channel, note.note, 0] },
        );
        emitted++;
      }
    }
    if ((instrument.controllerEvents ?? []).some(event => event.controller === 64 && event.value >= 64)) {
      output.push({ tick: musicEnd, priority: 3, bytes: [0xb0 | channel, 64, 0] });
    }
    tracks.push(track(output, endTick));
    noteCount += emitted;
    summary.push({ ...instrument, notes: emitted, sourceNotes: notes.length });
  }
  const header = Buffer.alloc(14);
  header.write('MThd');
  header.writeUInt32BE(6, 4);
  header.writeUInt16BE(1, 8);
  header.writeUInt16BE(tracks.length, 10);
  header.writeUInt16BE(PPQ, 12);
  return { buffer: Buffer.concat([header, ...tracks]), noteCount, sourceNoteCount, tracks: summary };
}
