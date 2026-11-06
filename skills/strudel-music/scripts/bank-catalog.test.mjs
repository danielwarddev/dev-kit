import assert from 'node:assert/strict';
import { execFile } from 'node:child_process';
import { mkdtemp, readFile, readdir, rm, stat, writeFile } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { promisify } from 'node:util';
import { test } from 'node:test';
import { initializeBankCatalog, loadBankCatalog, prepareSoundfont } from './bank-catalog.mjs';

const execute = promisify(execFile);
const fixtureBanks = [
  { id: 'soft', name: 'Soft fixture', file: 'soft.sf2', tags: ['soft-attack'] },
  { id: 'punchy', name: 'Punchy fixture', file: 'punchy.sf2', tags: ['punchy'] },
];

async function withDirectory(run) {
  const directory = await mkdtemp(path.join(os.tmpdir(), 'strudel-bank-catalog-'));
  try {
    await run(directory);
  } finally {
    await rm(directory, { recursive: true, force: true });
  }
}

async function withEnvironment(directory, run) {
  const keys = ['STRUDEL_SOUNDFONT_DIR', 'STRUDEL_SOUNDFONT_DOWNLOADS', 'FLUIDSYNTH_PATH'];
  const previous = keys.map(key => process.env[key]);
  process.env.STRUDEL_SOUNDFONT_DIR = directory;
  process.env.STRUDEL_SOUNDFONT_DOWNLOADS = '0';
  process.env.FLUIDSYNTH_PATH = process.execPath;
  try {
    await run();
  } finally {
    keys.forEach((key, index) => {
      if (previous[index] === undefined) delete process.env[key];
      else process.env[key] = previous[index];
    });
  }
}

test('catalog initialization is explicit and an empty library requires user consent', async () => {
  await withDirectory(async directory => {
    const catalog = await initializeBankCatalog(directory);
    const saved = JSON.parse(await readFile(catalog.catalogPath, 'utf8'));
    assert.equal(saved.schemaVersion, 1);
    assert.equal(saved.renderer, undefined);
    assert.deepEqual(saved.banks, []);
    assert.deepEqual(catalog.availableBankIds, []);
    await assert.rejects(loadBankCatalog(directory), error => error.code === 'SOUNDFONTS_UNAVAILABLE');
    assert.deepEqual(await readdir(directory), ['banks.json']);
  });
});

test('missing folder or catalog stops without creation or downloads even when downloads are enabled', async () => {
  await withDirectory(async directory => {
    const missing = path.join(directory, 'absent');
    await withEnvironment(missing, async () => {
      process.env.STRUDEL_SOUNDFONT_DOWNLOADS = '1';
      await assert.rejects(prepareSoundfont('soft'), error => {
        assert.equal(error.code, 'SOUNDFONTS_UNAVAILABLE');
        assert.match(error.message, /ask the user.*get some SoundFonts/);
        return true;
      });
    });
    await assert.rejects(stat(missing), { code: 'ENOENT' });
    await assert.rejects(loadBankCatalog(directory), error => error.code === 'SOUNDFONTS_UNAVAILABLE');
    assert.deepEqual(await readdir(directory), []);
  });
});

test('preserves user tags and selects cached per-song banks from the shared catalog', async () => {
  await withDirectory(async directory => {
    const contents = JSON.stringify({ schemaVersion: 1, banks: fixtureBanks });
    await writeFile(path.join(directory, 'banks.json'), contents);
    await writeFile(path.join(directory, 'punchy.sf2'), Buffer.from('RIFF\x04\x00\x00\x00sfbk'));
    await withEnvironment(directory, async () => {
      const selected = await prepareSoundfont('punchy');
      assert.deepEqual(selected.bank.tags, ['punchy']);
      assert.equal(selected.soundfont, path.join(directory, 'punchy.sf2'));
      assert.equal(await readFile(path.join(directory, 'banks.json'), 'utf8'), contents);
    });
  });
});

test('missing bank fails clearly offline instead of substituting or fetching', async () => {
  await withDirectory(async directory => {
    await writeFile(path.join(directory, 'banks.json'), JSON.stringify({ schemaVersion: 1, banks: fixtureBanks }));
    await writeFile(path.join(directory, 'punchy.sf2'), Buffer.from('RIFF\x04\x00\x00\x00sfbk'));
    await withEnvironment(directory, async () => {
      await assert.rejects(prepareSoundfont('soft'), /soft\.sf2.*downloads are disabled/);
      await assert.rejects(prepareSoundfont('unknown'), /Unknown bank "unknown"/);
    });
  });
});

test('rejects unsafe filenames and duplicate bank IDs without rewriting user data', async () => {
  await withDirectory(async directory => {
    const catalogPath = path.join(directory, 'banks.json');
    const unsafe = JSON.stringify({ schemaVersion: 1, banks: [{ ...fixtureBanks[0], file: '..\\outside.sf2' }] });
    await writeFile(catalogPath, unsafe);
    await assert.rejects(loadBankCatalog(directory), /without directory components/);
    assert.equal(await readFile(catalogPath, 'utf8'), unsafe);
    await writeFile(catalogPath, JSON.stringify({ schemaVersion: 1, banks: [fixtureBanks[0], fixtureBanks[0]] }));
    await assert.rejects(loadBankCatalog(directory), /duplicate bank ID/);
  });
});

test('PowerShell setup processes only the selected cached bank', { skip: process.platform !== 'win32' }, async () => {
  await withDirectory(async directory => {
    await writeFile(path.join(directory, 'banks.json'), JSON.stringify({ schemaVersion: 1, banks: fixtureBanks }));
    await writeFile(path.join(directory, 'soft.sf2'), Buffer.from('RIFF\x04\x00\x00\x00sfbk'));
    const setup = fileURLToPath(new URL('./setup-soundfonts.ps1', import.meta.url));
    const result = await execute('pwsh', ['-NoProfile', '-File', setup, '-BankId', 'soft'], {
      timeout: 30000,
      env: { ...process.env, STRUDEL_SOUNDFONT_DIR: directory, STRUDEL_SOUNDFONT_DOWNLOADS: '0', FLUIDSYNTH_PATH: process.execPath },
    });
    assert.match(result.stdout, /Cached: Soft fixture/);
    assert.doesNotMatch(result.stdout, /Punchy fixture/);
  });
});
