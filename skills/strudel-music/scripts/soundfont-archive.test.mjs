import assert from 'node:assert/strict';
import { execFile } from 'node:child_process';
import { mkdir, mkdtemp, readFile, readdir, rm, writeFile } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { promisify } from 'node:util';
import { test } from 'node:test';
import { extractSoundfontArchive } from './soundfont-archive.mjs';

const execute = promisify(execFile);
const tar = process.platform === 'win32' ? path.join(process.env.SystemRoot, 'System32', 'tar.exe') : 'tar';

test('extracts only the selected bank and license, including spaced member names', async () => {
  const directory = await mkdtemp(path.join(os.tmpdir(), 'strudel-bank-archive-'));
  try {
    const source = path.join(directory, 'source');
    const output = path.join(directory, 'output');
    await mkdir(path.join(source, 'Bank Pack'), { recursive: true });
    await writeFile(path.join(source, 'Bank Pack', 'Sample Bank.sf2'), Buffer.from('RIFF\x04\x00\x00\x00sfbk'));
    await writeFile(path.join(source, 'Bank Pack', 'LICENSE.txt'), 'Fixture license');
    await writeFile(path.join(source, 'unselected.txt'), 'Do not extract');
    const archive = path.join(directory, 'bank.tar');
    await execute(tar, ['-cf', archive, '-C', source, 'Bank Pack', 'unselected.txt']);
    const result = await extractSoundfontArchive(archive, output, 'Bank Pack\\Sample Bank.sf2', 'Bank Pack\\LICENSE.txt');
    assert.equal(await readFile(result.license, 'utf8'), 'Fixture license');
    assert.equal((await readFile(result.font)).subarray(8).toString(), 'sfbk');
    assert.deepEqual(await readdir(output), ['Bank Pack']);
  } finally {
    await rm(directory, { recursive: true, force: true });
  }
});

test('rejects traversal and absolute member paths before creating files', async () => {
  await assert.rejects(extractSoundfontArchive('unused', 'unused', '..\\outside.sf2'), /safe relative paths/);
  await assert.rejects(extractSoundfontArchive('unused', 'unused', 'C:\\outside.sf2'), /safe relative paths/);
});
