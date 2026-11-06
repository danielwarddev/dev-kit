import { execFile } from 'node:child_process';
import { mkdir, readFile, stat, writeFile } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { promisify } from 'node:util';

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const execute = promisify(execFile);

async function exists(file) {
  try {
    return (await stat(file)).isFile();
  } catch (error) {
    if (error.code !== 'ENOENT') throw error;
    return false;
  }
}

function bankDirectory() {
  return process.env.STRUDEL_SOUNDFONT_DIR ?? path.join(os.homedir(), '.strudel-skill-soundfonts');
}

function unavailable(message) {
  const error = new Error(`[SOUNDFONTS_UNAVAILABLE] ${message}. Stop and ask the user whether they would like to get some SoundFonts. No downloads were started.`);
  error.code = 'SOUNDFONTS_UNAVAILABLE';
  return error;
}

export async function initializeBankCatalog(directory = bankDirectory()) {
  directory = path.resolve(directory);
  const catalogPath = path.join(directory, 'banks.json');
  if (!await exists(catalogPath)) {
    await mkdir(directory, { recursive: true });
    try {
      await writeFile(catalogPath, `${JSON.stringify({ schemaVersion: 1, banks: [] }, null, 2)}\n`, { flag: 'wx' });
    } catch (error) {
      if (error.code !== 'EEXIST') throw error;
    }
  }
  return loadBankCatalog(directory, { requireFonts: false });
}

export async function loadBankCatalog(directory = bankDirectory(), { requireFonts = true } = {}) {
  directory = path.resolve(directory);
  const catalogPath = path.join(directory, 'banks.json');
  let contents;
  try {
    contents = await readFile(catalogPath, 'utf8');
  } catch (error) {
    if (error.code === 'ENOENT' || error.code === 'ENOTDIR') throw unavailable(`Shared folder or catalog is missing: ${catalogPath}`);
    throw error;
  }
  const catalog = JSON.parse(contents.replace(/^\uFEFF/, ''));
  if (catalog.schemaVersion !== 1 || !Array.isArray(catalog.banks)) throw new Error(`Invalid bank catalog: ${catalogPath}`);
  const ids = new Set();
  for (const bank of catalog.banks) {
    if (!bank || typeof bank.id !== 'string' || !/^[a-z0-9][a-z0-9_-]*$/i.test(bank.id) || ids.has(bank.id)) {
      throw new Error(`Invalid or duplicate bank ID in ${catalogPath}`);
    }
    if (typeof bank.file !== 'string' || !/\.sf2$/i.test(bank.file) || /[\\/:]/.test(bank.file)) {
      throw new Error(`Bank ${bank.id} requires an SF2 filename without directory components`);
    }
    if (!Array.isArray(bank.tags) || bank.tags.some(tag => typeof tag !== 'string' || !tag.trim())) {
      throw new Error(`Bank ${bank.id} requires an array of descriptive tags`);
    }
    ids.add(bank.id);
  }
  const availability = await Promise.all(catalog.banks.map(bank => exists(path.join(directory, bank.file))));
  const availableBankIds = catalog.banks.filter((bank, index) => availability[index]).map(bank => bank.id);
  if (requireFonts && !availableBankIds.length) throw unavailable(`No registered SF2 files are present in ${directory}`);
  return { directory, catalogPath, banks: catalog.banks, availableBankIds };
}

export async function prepareSoundfonts(bankIds) {
  const catalog = await loadBankCatalog();
  const banks = [...new Set(bankIds)].map(bankId => {
    const bank = catalog.banks.find(value => value.id === bankId);
    if (!bank) throw new Error(`Unknown bank "${bankId}" in ${catalog.catalogPath}`);
    return bank;
  });
  const prepared = [];
  for (const bank of banks) {
    const soundfont = path.join(catalog.directory, bank.file);
    const fontMissing = !await exists(soundfont);
    const rendererMissing = process.platform === 'win32' && !process.env.FLUIDSYNTH_PATH
      && !await exists(path.join(scriptDirectory, '.cache', 'fluidsynth-location.json'));
    if (fontMissing || rendererMissing) {
      const missing = fontMissing ? `SoundFont ${soundfont}` : 'local FluidSynth renderer';
      if (process.env.STRUDEL_SOUNDFONT_DOWNLOADS === '0') {
        throw new Error(`${missing} is missing and downloads are disabled. Run scripts\\setup-soundfonts.ps1 -BankId ${bank.id} with downloads enabled.`);
      }
      if (process.platform !== 'win32') {
        throw new Error(`${missing} is missing. Copy the verified SF2 into ${catalog.directory} and install FluidSynth with the host package manager.`);
      }
      const result = await execute('pwsh', [
        '-NoProfile', '-File', path.join(scriptDirectory, 'setup-soundfonts.ps1'), '-BankId', bank.id,
      ], { timeout: 600000, maxBuffer: 2 * 1024 * 1024 });
      if (result.stdout) console.log(result.stdout.trim());
      if (!await exists(soundfont)) throw new Error(`Setup did not provide ${soundfont}`);
    }
    prepared.push({ bank, soundfont, catalogPath: catalog.catalogPath });
  }
  return prepared;
}

export async function prepareSoundfont(bankId) {
  return (await prepareSoundfonts([bankId]))[0];
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    console.log(JSON.stringify(await (process.argv.includes('--init') ? initializeBankCatalog() : loadBankCatalog())));
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}
