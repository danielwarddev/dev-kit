# Sound banks are part of the palette

Do not assume changing instrument names inside VCSL/VSCO is enough variety.
Consider different recording/synthesis libraries when the desired timbre calls
for it. Match banks to the emotional/genre brief and inspect their licenses.

## Two rendering routes

Choose one per cue using the route table in `SKILL.md` (music workflow step 3).

| Route | Use / limitations |
|---|---|
| `render.mjs` | Strudel's own Web Audio synths and explicitly registered sample maps; supports its audio effects |
| `render-soundfont.mjs` | Actual Strudel note patterns exported to MIDI and rendered by native FluidSynth with SF2 banks; preserves native bank programming, loops, envelopes and modulators |

Do not flatten a complex SoundFont into a handful of raw samples and present
that as faithful bank playback. Banks with detailed programming (modulators,
layered presets, loops) depend on proper SoundFont synthesis.

The MIDI route maps note pitches, velocities, durations/`clip`, and Strudel
orbits to explicit instrument tracks. It does not translate arbitrary Web Audio
effects, FM, filter automation, or ADSR parameters. Use SoundFont articulation
and MIDI track controls instead. The source remains a Strudel pattern, but its
`orbit` identifiers are instrument tracks in this route, not browser sounds.

## Shared bank library

The shared catalog is `~\.strudel-skill-soundfonts\banks.json`, alongside the
actual SF2 files and license texts. On Windows this normally means
`C:\Users\YOUR_USER\.strudel-skill-soundfonts`.
`STRUDEL_SOUNDFONT_DIR` overrides the directory.
The skill stays project-local; only SoundFont assets/catalog are shared between
projects. Native FluidSynth binaries remain in the ignored `scripts\.cache`.

`scripts\soundfont-resources.json` holds only the pinned native renderer
specification. The skill ships no banks and no bank recommendations: which
banks to use is a per-library, per-song decision. Preflight is read-only;
rendering stops if the shared folder/catalog is missing or no registered SF2
files are present. The skill must ask the user whether they want to get some,
and wait for their decision before any initialization or download. An existing
shared catalog is authoritative: do not overwrite its edits or synchronize
duplicate tag lists per project. Do not commit or embed banks into the game by
default.

### Choosing and registering a bank

Use this when the library is missing or empty, or when no registered bank suits
the song's instrument roles or character:

1. Infer the needed coverage and character from the brief: full GM ensemble,
   specialist piano or orchestral section, drum kit, chip/retro synth, and so
   on. Search for candidate SF2 banks from their original publisher. Prefer
   versioned, pinnable downloads (release assets, commit-pinned raw URLs,
   distribution packages) over mirrors and re-uploads.
2. Apply the license review in `SKILL.md` to each candidate, including any
   provenance concerns the publisher discloses.
3. Use `ask_user` once with the proposed bank(s): source, version, license,
   any license alerts, download size, and why each fits. Wait for approval.
4. After approval, run setup without `-BankId` to create an empty catalog (if
   absent) and the local renderer.
5. Download each approved resource to a task-owned temporary directory and
   compute SHA-256 hashes of the download and of the SF2 inside it. Add the
   entry to `banks.json`, delete the temporary download, then run setup with
   `-BankId` so it fetches, verifies, and caches the bank and its license text.

Entries carry a stable `id`, relative SF2 `file`, `tags`, `strengths`,
`presetCoverage`, version, download URL/format, integrity checks, size and
licensing/provenance. Base tags and strengths on the publisher's documentation
and preset list, not the filename. `fileSha256` describes the extracted SF2;
`sha256` describes the downloaded resource, which may be an archive. A catalog
can list banks that are not downloaded: determine availability from the file,
not a stored `installed` flag. The renderer takes the shared catalog ID.

```json
{
  "id": "gm-bank",
  "name": "PUBLISHER BANK NAME",
  "version": "VERSION",
  "file": "Bank.sf2",
  "fileSha256": "SHA256_OF_THE_SF2",
  "sizeBytes": 0,
  "tags": ["general-midi", "acoustic"],
  "strengths": ["What this bank does well, from its documentation"],
  "presetCoverage": { "gmPrograms": 128, "percussion": true },
  "format": "sf2",
  "url": "https://PINNED_DOWNLOAD_URL",
  "sha256": "SHA256_OF_THE_DOWNLOAD",
  "license": "LICENSE NAME AND KEY TERMS",
  "credit": "AUTHORS",
  "licenseSource": "https://PINNED_LICENSE_URL"
}
```

Download formats:

| `format` | Download | SF2 and license |
|---|---|---|
| `sf2` | The SF2 itself; pin `sha256`, `gitBlobSha1`, or `fileSha256` | License text is fetched from `licenseSource` |
| `archive` | Zip/tar archive; pin `sha256` | `archiveFile` and optional `licenseFile` member names; otherwise `licenseSource` |
| `debian` | `.deb` package; pin `sha256` | `archiveFile` path inside the package data (default `usr/share/sounds/sf2/FILE`) and optional `licenseFile`; otherwise `licenseSource` |

Setup extracts only the selected bank/license into a task-owned temporary
directory and verifies the resulting SF2 before caching it.

Choose one bank or a small palette for each song from its emotion, genre,
instrument roles, tags and preset coverage, not a fixed bank for the game.
One suitable bank is fine; multiple complementary banks are also fine. Tags
describe capabilities and character; they are loose guidance, not a musical
score or exclusive genre mapping. Do not invent audition-based tonal labels
from a filename. Record
the default `bankId`, optional `midiTracks[].bankId` overrides, and each bank's
role/selection reason in cue metadata. Keep direct synthesis and
sample-map rendering as options rather than forcing every song through SF2.
Do not silently select share-alike/restrictive libraries when they conflict
with the destination's licensing requirements.

## Setup and rendering

Before composing a song, inspect the shared tags and installed bank IDs:

```powershell
node "SKILL_DIRECTORY\scripts\bank-catalog.mjs"
```

If this fails because the library is missing/empty, stop and ask the user if they
would like to get SoundFonts. A missing or invalid catalog must not be replaced
without approval, even if unregistered font files exist nearby.
Use `ask_user` for this decision, offering the browser route as an alternative
and stating what it would lose; do not continue composing, invoke setup, or
silently switch routes. If the user declines both, stop.

After the user approves acquisition, Windows with PowerShell 7 can explicitly
initialize an empty catalog and the local renderer:

```powershell
& "SKILL_DIRECTORY\scripts\setup-soundfonts.ps1"
```

This creates the catalog if absent; it does not register or download banks.
After registering an approved bank (see above), acquire it explicitly:

```powershell
& "SKILL_DIRECTORY\scripts\setup-soundfonts.ps1" -BankId BANK_ID
```

`STRUDEL_SOUNDFONT_DOWNLOADS=0` disables automatic bank/renderer downloads.
With a nonempty existing library, rendering can download only selected missing
catalog banks, verify their integrity, and retain their licenses. An empty
library never triggers automatic acquisition, even when downloads are enabled.
Missing assets fail with the selected file and setup instructions; do not
silently substitute another bank. Existing user files are not overwritten,
evicted, or repaired automatically. Invalid catalogs and mismatched SF2 hashes
fail instead of replacing user data. A shared cache avoids duplicate copies
across projects, but still consumes disk space for banks actually downloaded.

On other systems, use an installed FluidSynth and place verified SF2 files in
the shared directory. `FLUIDSYNTH_PATH` can select an existing binary;
`STRUDEL_BROWSER_PATH` can select the task-owned headless browser executable.

Write Strudel patterns with `note(...)`, `velocity(...)`, `clip(...)` and distinct
`orbit(...)` values. End with a Pattern as usual. Metadata must include:

```json
{
  "bankId": "gm-bank",
  "midiTracks": [
    { "orbit": 1, "channel": 1, "program": 27, "bankId": "guitar-bank", "volume": 90, "pan": 60, "reverb": 20 },
    { "orbit": 2, "channel": 10, "program": 0, "volume": 85, "pan": 64, "reverb": 10 }
  ]
}
```

Channels are 1-16; GM programs are **zero-based** 0-127. Channel 10 is standard
GM percussion (36 kick, 38 snare, 42 closed hi-hat, etc.). Initial track volume,
pan, reverb send and optional `chorus` send are MIDI values 0-127. Chorus sends
need the native chorus module enabled. Same-pitch retriggers normally release
the old voice first to avoid hung/cut-off notes.

`midiTracks[].bankId` selects a bank for that part. Omitted values inherit the
CLI default bank ID, or the song's top-level `bankId` when no CLI default is
given. Each preset must actually exist in its selected bank: specialist fonts
need their own program/key mappings, not assumed full-GM coverage. Multiple
parts may share one bank. MIDI orbits and channels remain unique across the song.
Consult catalog `presetCoverage.presets`, `midiMapping`, and `drumNoteMap` when
provided. A drum-only SF2 can be a bank-0 melodic preset rather than a bank-128
GM percussion preset: use its documented normal channel/program and mapped
keys, not automatically channel 10 or GM drum notes.

The exporter routes each note to exactly one bank group. Native FluidSynth
renders each group on the same MIDI timeline, with matching loop warmup and
effect history. Raw groups are summed without independent normalization; only
the combined full mix receives the loudness target. This supports multiple SF2s
in exported music without requiring a live multi-bank synth inside the game.

For a continuous pad, opt into `tieCommonNotes: true` on its MIDI track.
Touching/overlapping occurrences of the same pitch become one held note;
actual gaps remain gaps. The held voice retains its initial velocity, so this
is appropriate for steady layers rather than deliberately re-attacked melodies.

`controllerEvents` on a track allows explicit `{cycle, controller, value}` MIDI
CC automation. Times use Strudel cycles and values/controllers are 0-127.
For example:

```json
{
  "controllerEvents": [
    { "cycle": 0, "controller": 64, "value": 100 },
    { "cycle": 0.9875, "controller": 64, "value": 0 },
    { "cycle": 1.0125, "controller": 64, "value": 100 }
  ]
}
```

CC64 values >=64 engage the sustain pedal. Refresh it around harmony/phrase
changes and release keys before the refresh so old passing tones are not caught
under the next chord. The exporter repeats automation for loop warmup and
releases an engaged pedal at the music end. If a recipe explicitly doubles a
non-looping render, duplicate its automation too; excerpts need rebased times.

Controller behavior beyond standard pedal/volume/effects depends on the bank
and engine. Do not assume CC72/73/74 alter release, attack or brightness: many
banks do not define those CC-to-envelope/filter modulators. Softer note
velocities, an appropriate preset, and real audio processing are preferable to
claiming an unsupported envelope change.

Optional top-level `soundfontEffects` configures FluidSynth's real effects:

```json
{
  "soundfontEffects": {
    "chorus": true,
    "reverb": { "roomSize": 0.72, "damping": 0.58, "width": 0.8, "level": 0.7 }
  }
}
```

These settings are choices, not genre defaults. Unspecified settings preserve
the previous renderer behavior. Keep excessive reverb/chorus from obscuring
the phrase or bass.

```powershell
node "SKILL_DIRECTORY\scripts\render-soundfont.mjs" "OUTPUT\cue.strudel.js" "OUTPUT\cue.json" "OUTPUT" gm-bank
node "SKILL_DIRECTORY\scripts\render-soundfont.mjs" "OUTPUT\cue.strudel.js" "OUTPUT\cue.json" "OUTPUT" other-bank
```

The CLI default is optional when metadata provides the bank assignments.
Single-bank output names remain `NAME-BANK_ID`. Multi-bank output uses
`NAME-multi-bank`, with a combined sequencing MIDI, per-bank MIDI files, WAV,
and per-track/bank provenance in the validation report. The combined MIDI alone
does not embed SoundFont file assignments; retain the metadata/render recipe.

The tool normalizes
each full mix to a common -18 LUFS target with a -2 dBTP ceiling for bank auditions.
This is not independent stem normalization and does not establish instrument
balance by itself. Inspect actual output and obey the skill's mix/fullness rules.

For a bank comparison, keep the source, timing, MIDI programs, track controls
and engine identical; change only the bank. Compare MIDI hashes and match
loudness so "louder" is not mistaken for "better." Use a short representative
passage, not a full new song for every option.

Reference: https://www.fluidsynth.org/wiki/UserManual/.
