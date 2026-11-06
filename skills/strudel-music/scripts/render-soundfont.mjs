import { createHash } from 'node:crypto';
import { createReadStream } from 'node:fs';
import { execFile } from 'node:child_process';
import { createServer } from 'node:http';
import { mkdir, mkdtemp, open, readFile, rm, writeFile } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { promisify } from 'node:util';
import { chromium } from 'playwright';
import { inspectPcm, readPcmWav, validateConfig } from './audio.mjs';
import { loadBankCatalog, prepareSoundfonts } from './bank-catalog.mjs';
import { groupSoundfontTracks } from './soundfont-tracks.mjs';

const [sourcePath, configPath, outputDirectory, bankId] = process.argv.slice(2);
const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const execute = promisify(execFile);
if (!sourcePath || !configPath || !outputDirectory || process.argv.length < 5 || process.argv.length > 6) {
  console.error('Usage: node render-soundfont.mjs SOURCE.strudel.js CONFIG.json OUTPUT_DIRECTORY [DEFAULT_BANK_ID]');
  process.exit(1);
}

async function hashFile(file) {
  const hash = createHash('sha256');
  for await (const chunk of createReadStream(file)) hash.update(chunk);
  return hash.digest('hex');
}

let server;
let browser;
let temporary;
try {
  const config = validateConfig(JSON.parse(await readFile(configPath, 'utf8')));
  const source = await readFile(sourcePath, 'utf8');
  const resources = JSON.parse(await readFile(path.join(scriptDirectory, 'soundfont-resources.json'), 'utf8'));
  await loadBankCatalog();
  const bundle = await readFile(path.join(scriptDirectory, 'node_modules', '@strudel', 'web', 'dist', 'index.mjs'));
  server = createServer((request, response) => {
    response.setHeader('Content-Type', request.url === '/strudel.mjs' ? 'text/javascript' : 'text/html');
    response.end(request.url === '/strudel.mjs' ? bundle : '<!doctype html><link rel="icon" href="data:,"><title>Local Strudel MIDI export</title>');
  });
  await new Promise((resolve, reject) => {
    server.once('error', reject);
    server.listen(0, '127.0.0.1', resolve);
  });
  const executablePath = process.env.STRUDEL_BROWSER_PATH
    ?? (process.platform === 'win32' ? 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe' : undefined);
  browser = await chromium.launch({ executablePath, headless: true });
  const page = await browser.newPage();
  await page.goto(`http://127.0.0.1:${server.address().port}`);
  const events = await page.evaluate(async ({ source, config }) => {
    const api = await import('/strudel.mjs');
    await api.initStrudel();
    const pattern = await api.evaluate(source, false);
    if (!pattern) throw new Error('Strudel source did not produce a Pattern');
    const cps = config.bpm / config.beatsPerCycle / 60;
    return pattern.queryArc(0, config.cycles * (config.loop ? 2 : 1), { _cps: cps })
      .filter(hap => hap.hasOnset()).map(hap => {
        hap.ensureObjectValue();
        return { begin: Number(hap.whole.begin), duration: Number(hap.duration), value: hap.value };
      }).filter(event => event.value.note !== undefined);
  }, { source, config });
  await browser.close();
  browser = null;
  await new Promise(resolve => server.close(resolve));
  server = null;
  const { midi, groups } = groupSoundfontTracks(events, config, bankId ?? config.bankId);
  const fonts = await prepareSoundfonts(groups.map(group => group.bankId));
  for (const font of fonts) {
    const fontHeader = Buffer.alloc(12);
    const fontFile = await open(font.soundfont);
    try {
      await fontFile.read(fontHeader, 0, 12, 0);
    } finally { await fontFile.close(); }
    if (fontHeader.toString('ascii', 0, 4) !== 'RIFF' || fontHeader.toString('ascii', 8, 12) !== 'sfbk') throw new Error(`Invalid SF2 bank: ${font.soundfont}`);
    font.sha256 = await hashFile(font.soundfont);
    if (font.bank.fileSha256 && font.sha256 !== font.bank.fileSha256.toLowerCase()) throw new Error(`SoundFont checksum mismatch: ${font.soundfont}`);
  }
  let synth = process.env.FLUIDSYNTH_PATH;
  if (!synth && process.platform === 'win32') {
    const location = JSON.parse((await readFile(path.join(scriptDirectory, '.cache', 'fluidsynth-location.json'), 'utf8')).replace(/^\uFEFF/, ''));
    synth = path.resolve(scriptDirectory, location.executable);
  }
  synth ??= 'fluidsynth';
  const output = path.resolve(outputDirectory);
  await mkdir(output, { recursive: true });
  const name = `${config.name}-${fonts.length === 1 ? fonts[0].bank.id : 'multi-bank'}`;
  const midiPath = path.join(output, `${name}.mid`);
  await writeFile(midiPath, midi.buffer);
  temporary = await mkdtemp(path.join(os.tmpdir(), 'strudel-soundfont-'));
  const synthConfig = path.join(temporary, 'empty.cfg');
  await writeFile(synthConfig, '');
  const effects = config.soundfontEffects ?? {};
  if (effects.chorus !== undefined && typeof effects.chorus !== 'boolean') throw new Error('soundfontEffects.chorus must be a boolean');
  const effectArguments = [];
  for (const [key, setting, maximum] of [
    ['roomSize', 'synth.reverb.room-size', 1],
    ['damping', 'synth.reverb.damp', 1],
    ['width', 'synth.reverb.width', 100],
    ['level', 'synth.reverb.level', 1],
  ]) {
    const value = effects.reverb?.[key];
    if (value !== undefined) {
      if (!Number.isFinite(value) || value < 0 || value > maximum) throw new Error(`Invalid reverb ${key}`);
      effectArguments.push('-o', `${setting}=${value}`);
    }
  }
  const begin = config.loop ? config.durationSeconds : 0;
  const duration = config.durationSeconds + config.tailSeconds;
  const rawPaths = [];
  const bankStems = [];
  for (const group of groups) {
    const font = fonts.find(value => value.bank.id === group.bankId);
    const groupMidiPath = group.midi === midi ? midiPath : path.join(output, `${name}-${group.bankId}.mid`);
    if (group.midi !== midi) await writeFile(groupMidiPath, group.midi.buffer);
    const rawPath = path.join(temporary, `${group.bankId}.wav`);
    console.log(`Rendering ${font.bank.name}: ${group.midi.noteCount} notes through native FluidSynth`);
    const native = await execute(synth, [
      '-ni', '-f', synthConfig, '-F', rawPath, '-T', 'wav', '-O', 's16', '-E', 'little',
      '-r', '44100', '-g', '0.15', '-C', effects.chorus ? '1' : '0', '-R', '1',
      ...effectArguments, font.soundfont, groupMidiPath,
    ], { timeout: 120000, maxBuffer: 2 * 1024 * 1024 });
    if (/error:|failed to|no preset found/i.test(native.stderr)) throw new Error(native.stderr);
    const raw = inspectPcm(readPcmWav(await readFile(rawPath)).pcm);
    if (!raw.peak || raw.clippedSamples) throw new Error(`Native bank ${group.bankId} output is silent or clipped`);
    if (raw.durationSeconds + 0.001 < begin + duration) throw new Error(`Native bank ${group.bankId} render ended too early`);
    rawPaths.push(rawPath);
    bankStems.push({
      bankId: group.bankId, orbits: group.orbits, notes: group.midi.noteCount,
      midiPath: groupMidiPath, midiSha256: createHash('sha256').update(group.midi.buffer).digest('hex'),
      rawPeakDbfs: raw.peakDbfs, rawRmsDbfs: raw.rmsDbfs,
    });
  }
  const audioInputs = (filters) => {
    if (rawPaths.length === 1) return ['-i', rawPaths[0], '-af', filters];
    const inputs = rawPaths.flatMap(file => ['-i', file]);
    const streams = rawPaths.map((file, index) => `[${index}:a]`).join('');
    return [...inputs, '-filter_complex', `${streams}amix=inputs=${rawPaths.length}:duration=longest:dropout_transition=0:normalize=0,${filters}[mix]`, '-map', '[mix]'];
  };
  const trim = `atrim=start=${begin}:duration=${duration},asetpts=PTS-STARTPTS`;
  const meter = await execute('ffmpeg', [
    '-hide_banner', ...audioInputs(`${trim},loudnorm=I=-18:TP=-2:LRA=11:print_format=json`), '-f', 'null', '-',
  ], { timeout: 120000, maxBuffer: 2 * 1024 * 1024 });
  const measurement = JSON.parse(meter.stderr.match(/\{\s*"input_i"[\s\S]*?\}/)?.[0] ?? 'null');
  if (!measurement || !Number.isFinite(Number(measurement.input_i))) throw new Error('Could not measure bank loudness');
  const normalizer = `loudnorm=I=-18:TP=-2:LRA=11:measured_I=${measurement.input_i}:measured_TP=${measurement.input_tp}:measured_LRA=${measurement.input_lra}:measured_thresh=${measurement.input_thresh}:offset=${measurement.target_offset}:linear=true`;
  const wavPath = path.join(output, `${name}.wav`);
  await execute('ffmpeg', [
    '-y', '-v', 'error', ...audioInputs(`${trim},${normalizer}`), '-ar', '44100', '-ac', '2', '-c:a', 'pcm_s16le', wavPath,
  ], { timeout: 120000 });
  const result = inspectPcm(readPcmWav(await readFile(wavPath)).pcm);
  if (result.clippedSamples || Math.abs(result.durationSeconds - duration) > 0.01) throw new Error('Final bank output failed validation');
  const finalMeter = await execute('ffmpeg', [
    '-hide_banner', '-i', wavPath, '-af', 'loudnorm=I=-18:TP=-2:LRA=11:print_format=json', '-f', 'null', '-',
  ], { timeout: 120000, maxBuffer: 2 * 1024 * 1024 });
  const finalLoudness = JSON.parse(finalMeter.stderr.match(/\{\s*"input_i"[\s\S]*?\}/)?.[0] ?? 'null');
  const integratedLufs = Number(finalLoudness?.input_i);
  const truePeakDbtp = Number(finalLoudness?.input_tp);
  if (!Number.isFinite(integratedLufs) || Math.abs(integratedLufs + 18) > 0.5 || !Number.isFinite(truePeakDbtp) || truePeakDbtp > -1.9) {
    throw new Error('Final bank loudness did not meet the comparison target');
  }
  const report = {
    ...config, ...result, bank: fonts.length === 1 ? fonts[0].bank : undefined,
    banks: fonts.map(font => font.bank), bankStems,
    midi: { notes: midi.noteCount, sourceNotes: midi.sourceNoteCount, tracks: midi.tracks },
    midiSha256: createHash('sha256').update(midi.buffer).digest('hex'),
    sourceSha256: createHash('sha256').update(source).digest('hex'),
    soundfontSha256: fonts.length === 1 ? fonts[0].sha256 : undefined,
    soundfontPath: fonts.length === 1 ? fonts[0].soundfont : undefined,
    soundfonts: fonts.map(font => ({ bankId: font.bank.id, path: font.soundfont, sha256: font.sha256 })),
    bankCatalogPath: fonts[0].catalogPath,
    renderer: `Strudel note events -> FluidSynth ${resources.renderer.version}`,
    loudnessTargetLufs: -18, integratedLufs, truePeakDbtp, nativeLoudness: measurement,
    checks: { silent: false, clipped: false },
    limitations: 'MIDI note/velocity/clip mapping and native SF2 behavior; Strudel Web Audio effects are not translated. No auditory audition.',
  };
  await writeFile(path.join(output, `${name}.validation.json`), `${JSON.stringify(report, null, 2)}\n`);
  console.log(`WAV: ${wavPath}`);
  console.log(`Verified ${result.durationSeconds.toFixed(3)}s, peak ${result.peakDbfs.toFixed(2)} dBFS, no clipping`);
} catch (error) {
  console.error(error.stack ?? error.message);
  if (error.stderr) console.error(error.stderr);
  process.exitCode = 1;
} finally {
  await browser?.close();
  if (server?.listening) await new Promise(resolve => server.close(resolve));
  if (temporary) await rm(temporary, { recursive: true, force: true });
}
