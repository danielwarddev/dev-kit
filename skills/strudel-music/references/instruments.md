# Instrument selection

Always verify availability in the execution environment. The browser REPL and
`@strudel/web` do not have identical default sound loading.

## SoundFonts and recorded instruments

For SF2 instruments, consult the shared user catalog's tags, strengths,
`presetCoverage`, `midiMapping`, and drum maps; see `references\banks.md`.
Choose one or multiple banks per song, not a fixed palette for a game.

For raw recordings, keep explicit `await samples({...}, 'BASE_URL')` registration
in the project's cue source. Keys such as `D4` are recording base pitches, not
sample indices. `note(...)` selects a pitch-region recording and retunes it.
Verify base pitch from the provider's metadata/SFZ `pitch_keycenter`, not a
filename's octave convention. Sample-map loading does not automatically import
SFZ gain, velocity-layer, round-robin or articulation programming.

Use suitable recorded articulations and pitch regions. Sustained strings should
not be gated into unnatural short bow strokes; winds need breathable phrases.
Select velocity layers and round robins only where the cue needs them, and
balance the actual recordings rather than assuming preset-default levels.
Pin source versions and record licensing/provenance in cue metadata.

VCSL and VSCO 2 Community Edition are examples of CC0 recorded libraries:
https://github.com/sgossner/VCSL and https://github.com/sgossner/VSCO-2-CE.
They are not a required starter palette or a complete list of available sources.

## Broader fullness options

Consider these families when appropriate. Select options by musical role,
not a checklist or a previous cue's instruments.

| Family | Roles / loading considerations |
|---|---|
| Strings / ensembles | Sustained middle, low body, swells; choose compatible recorded articulations or bank presets |
| Guitar | Strummed support, arpeggios, riffs, ambient swells; load appropriately licensed recorded articulations or a compatible instrument |
| Wordless vocals / choir | Harmonic warmth, air, atmosphere, short phrases; verify sample consent/provenance and license; no lyrics required |
| Piano / keys / organ | Chordal body, comping, sustained support; verify key regions and sustain behavior |
| Synth pads / layered synthesis | Electronic harmonic body and evolution; use shaped timbres and voicings rather than an unprocessed default oscillator |
| Percussion / drum banks | Audible groove and weight; tune/balance the kit, don't make all hits decorative or nearly silent |

Explicitly load a suitable licensed pack or instrument when selected; do not
invent a sound name or pretend the current library contains it. Missing sources
do not excuse a thin result: another appropriate family can supply the role.

## Other sources

- **Drum machines:** REPL `Sounds` -> drum machines includes RolandTR808,
  RolandTR909, RolandTR707, and AkaiLinn. `.bank(...)` prefixes standard drum
  names (`bd`, `sd`, `hh`, etc.). In the renderer, explicitly register a
  compatible sample map first. Verify each pack's license independently, using
  the license review in `SKILL.md`.
- **General MIDI/soundfonts:** REPL examples include `gm_acoustic_bass`,
  `gm_electric_guitar_muted`, `gm_xylophone`, and `gm_synth_strings_1`. The pinned
  `@strudel/web` bundle does NOT register `gm_*` soundfonts. Use an explicitly
  loaded compatible library before choosing them. Check first-use loading and
  offline-render support; never accept silent voices as a successful render.
  The native route uses the shared SF2 catalog without flattening banks into
  raw samples; read `references\banks.md` for setup,
  explicit MIDI programs, permissions and additional-bank registration.
- **Synthesis:** `sine`, `triangle`, `square`, `sawtooth`, and noise are registered
  by the bundled renderer. Shape envelope, filter, and FM deliberately. They
  suit electronic/chiptune music; they are not a default realism substitute.
- **Wavetables:** Strudel documents AKWF waveforms and `samples('bubo:waveforms')`.
  Loading, sound names, and licenses must be verified for the chosen pack.
- **Custom recordings:** local import in the REPL, URL sample maps, or
  `@strudel/sampler` local serving. Prefer pitched multisamples and suitable
  articulations. Keep original files untouched and record provenance.

Do not use free-text sample searches that mix unknown licenses by default.
Remote availability is not permission to ship. Avoid unnecessary full-library
downloads; the renderer caches only assets that actual events request.

Official references:
https://strudel.cc/workshop/first-notes/,
https://strudel.cc/learn/samples/, https://strudel.cc/learn/synths/.
