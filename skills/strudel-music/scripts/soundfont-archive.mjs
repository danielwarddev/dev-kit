import { execFile } from 'node:child_process';
import { lstat, mkdir } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { promisify } from 'node:util';

const execute = promisify(execFile);
const tar = process.platform === 'win32' ? path.join(process.env.SystemRoot, 'System32', 'tar.exe') : 'tar';

function memberPath(member) {
  if (typeof member !== 'string' || !member || /[\0\r\n]/.test(member)
    || /^[\\/]/.test(member) || /^[a-z]:/i.test(member) || member.split(/[\\/]/).includes('..')) {
    throw new Error('Archive members must be safe relative paths');
  }
  return member.replaceAll('\\', '/').replace(/^\.\//, '');
}

export async function extractSoundfontArchive(archive, destination, fontMember, licenseMember) {
  const members = [memberPath(fontMember)];
  if (!/\.sf2$/i.test(members[0])) throw new Error('The selected archive member must be an SF2');
  if (licenseMember) members.push(memberPath(licenseMember));
  const listing = await execute(tar, ['-tf', archive], { timeout: 120000, maxBuffer: 4 * 1024 * 1024 });
  const available = new Set(listing.stdout.split(/\r?\n/).filter(Boolean).map(memberPath));
  if (members.some(member => !available.has(member))) throw new Error('Selected SF2/license member is missing from the archive');
  await mkdir(destination, { recursive: true });
  await execute(tar, ['-xf', archive, '-C', destination, '--', ...members], { timeout: 120000 });
  const files = members.map(member => path.resolve(destination, ...member.split('/')));
  for (const file of files) {
    if (!(await lstat(file)).isFile()) throw new Error('Selected archive members must be regular files');
  }
  return { font: files[0], license: files[1] };
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    const [archive, destination, font, license] = process.argv.slice(2);
    if (!archive || !destination || !font) throw new Error('Usage: soundfont-archive.mjs ARCHIVE DESTINATION SF2_MEMBER [LICENSE_MEMBER]');
    console.log(JSON.stringify(await extractSoundfontArchive(archive, destination, font, license)));
  } catch (error) {
    console.error(error.message);
    process.exitCode = 1;
  }
}
