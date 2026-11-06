import { createHash } from 'node:crypto';
import { createServer } from 'node:http';
import { access, mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright';
import { encodeWav, finishRender, validateConfig } from './audio.mjs';

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const [sourcePath, configPath, outputDirectory] = process.argv.slice(2);
if (!sourcePath || !configPath || !outputDirectory || process.argv.length !== 5) {
  console.error('Usage: node render.mjs SOURCE.strudel.js CONFIG.json OUTPUT_DIRECTORY');
  process.exit(1);
}

let browser;
let server;
try {
  const config = validateConfig(JSON.parse(await readFile(configPath, 'utf8')));
  const source = await readFile(sourcePath, 'utf8');
  const bundle = await readFile(path.join(scriptDirectory, 'node_modules', '@strudel', 'web', 'dist', 'index.mjs'));
  const offlineModule = await readFile(path.join(scriptDirectory, 'offline.mjs'));
  const destination = path.resolve(outputDirectory);
  const cache = path.join(scriptDirectory, '.cache', 'samples');
  await mkdir(destination, { recursive: true });
  await mkdir(cache, { recursive: true });

  server = createServer((request, response) => {
    if (request.url === '/strudel.mjs') {
      response.writeHead(200, { 'Content-Type': 'text/javascript' });
      response.end(bundle);
    } else if (request.url === '/offline.mjs') {
      response.writeHead(200, { 'Content-Type': 'text/javascript' });
      response.end(offlineModule);
    } else if (request.url === '/') {
      response.writeHead(200, { 'Content-Type': 'text/html' });
      response.end('<!doctype html><meta charset="utf-8"><link rel="icon" href="data:,"><title>Local Strudel renderer</title>');
    } else {
      response.writeHead(404);
      response.end();
    }
  });
  await new Promise((resolve, reject) => {
    server.once('error', reject);
    server.listen(0, '127.0.0.1', resolve);
  });

  let executablePath = process.env.STRUDEL_BROWSER_PATH;
  if (!executablePath && process.platform === 'win32') {
    for (const candidate of [
      'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
      'C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe',
      'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
    ]) {
      try { await access(candidate); executablePath = candidate; break; } catch {}
    }
  }
  browser = await chromium.launch({
    executablePath,
    headless: true,
    args: ['--autoplay-policy=no-user-gesture-required'],
  });
  const context = await browser.newContext({ acceptDownloads: true });
  const errors = [];
  const assets = new Set();
  await context.route('https://**/*', async (route) => {
    const url = route.request().url();
    try {
      if (route.request().method() !== 'GET') throw new Error('Only public asset GET downloads are supported');
      const cachePath = path.join(cache, createHash('sha256').update(url).digest('hex'));
      let body;
      try { body = await readFile(cachePath); } catch (error) {
        if (error.code !== 'ENOENT') throw error;
        const response = await fetch(url, { signal: AbortSignal.timeout(60000) });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        body = Buffer.from(await response.arrayBuffer());
        await writeFile(cachePath, body);
      }
      assets.add(url);
      await route.fulfill({ status: 200, body, headers: { 'access-control-allow-origin': '*' } });
    } catch (error) {
      errors.push(`Asset download failed: ${url}: ${error.message}`);
      await route.abort();
    }
  });
  const page = await context.newPage();
  page.setDefaultTimeout(300000);
  page.on('pageerror', (error) => errors.push(error.message));
  page.on('console', (message) => {
    const text = message.text();
    if (message.type() === 'error' || /error:|could not load|took too long/i.test(text)) errors.push(text);
    if (text.includes('[webaudio]')) console.log(text);
  });
  await page.goto(`http://127.0.0.1:${server.address().port}`);
  await page.evaluate(async () => {
    window.strudel = await import('/strudel.mjs');
    window.offline = await import('/offline.mjs');
    await window.strudel.initStrudel();
  });
  const cps = config.bpm / config.beatsPerCycle / 60;
  const patternInfo = await page.evaluate(async ({ source, config, cps }) => {
    const api = window.strudel;
    const pattern = await api.evaluate(source, false);
    if (!pattern || typeof pattern.queryArc !== 'function') throw new Error('Source did not produce a Strudel Pattern');
    const events = pattern.queryArc(0, config.cycles, { _cps: cps }).filter((hap) => hap.hasOnset());
    if (!events.length) throw new Error('Pattern contains no onsets');
    const sounds = {};
    const registered = api.soundMap.get();
    for (const event of events) {
      event.ensureObjectValue();
      const sound = event.value.s ?? 'triangle';
      if (['~', '-', '_'].includes(sound)) continue;
      const resolved = (event.value.bank ? `${event.value.bank}_${sound}` : sound).toLowerCase().replace(/\s+/g, '_');
      if (!registered[resolved]) throw new Error(`Sound "${resolved}" has not been registered`);
      sounds[resolved] = (sounds[resolved] ?? 0) + 1;
    }
    if (!Object.keys(sounds).length) throw new Error('Pattern contains only muted sounds');
    window.renderPattern = config.loop || config.tailSeconds === 0
      ? pattern
      : api.arrange([config.cycles, pattern], [config.tailSeconds * cps, api.silence]);
    return { onsets: events.length, sounds };
  }, { source, config, cps });
  if (errors.length) throw new Error(errors.join('\n'));
  console.log(`Rendering ${config.name}: ${config.durationSeconds.toFixed(2)}s, ${Object.keys(patternInfo.sounds).length} sounds, ${patternInfo.onsets} onsets`);
  const [download] = await Promise.all([
    page.waitForEvent('download', { timeout: 600000 }),
    page.evaluate(async ({ cps, config }) => {
      const end = config.loop ? config.cycles * 2 : config.cycles + config.tailSeconds * cps;
      await window.offline.renderToPcm(window.strudel, window.renderPattern, cps, end, config.name);
    }, { cps, config }),
  ]);
  if (await download.failure()) throw new Error(await download.failure());
  if (errors.length) throw new Error(errors.join('\n'));
  const rawWav = encodeWav(await readFile(await download.path()));
  const result = finishRender(rawWav, config);
  const wavPath = path.join(destination, `${config.name}.wav`);
  await writeFile(wavPath, result.wav);
  const report = {
    ...config,
    ...result.report,
    sampleRate: 44100,
    channels: 2,
    renderer: '@strudel/web 1.3.0 / chunk-scheduled offline Web Audio',
    sourceSha256: createHash('sha256').update(source).digest('hex'),
    pattern: patternInfo,
    downloadedAssets: [...assets].sort(),
    checks: { missingSounds: false, sampleDownloadErrors: 0, silent: false, clipped: false },
    limitations: 'Technical and pattern-structure checks only; no human or audio-model audition.',
  };
  await writeFile(path.join(destination, `${config.name}.validation.json`), `${JSON.stringify(report, null, 2)}\n`);
  console.log(`WAV: ${wavPath}`);
  console.log(`Verified ${report.durationSeconds.toFixed(3)}s, peak ${report.peakDbfs.toFixed(2)} dBFS, RMS ${report.rmsDbfs.toFixed(2)} dBFS, ${report.clippedSamples} clipped samples`);
} catch (error) {
  console.error(error.stack ?? error.message);
  process.exitCode = 1;
} finally {
  await browser?.close();
  if (server?.listening) await new Promise((resolve) => server.close(resolve));
}
