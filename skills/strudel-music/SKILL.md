---
name: strudel-music
description: 'Create original, full-sounding prototype music, sound effects, and ambience for any game, genre, mood, or gameplay purpose. Use for music cues, soundtracks, layered music, genre variations, loops, stingers, gameplay/UI effects, or playable Strudel compositions. Autonomously select sounds, compose, render WAV audio, and validate it; no repeated audition-and-feedback conversation required. Not for speech or production C# integration.'
---

# Strudel Music and Sound Effects

Deliver a complete, full-sounding prototype, not just code or a suggested workflow.
Fullness is an expected creative outcome for every cue, not an optional polish step. It
means a coherent musical body, foundation, continuity, and depth appropriate to
the style, not every instrument playing or maximum density at every moment.
Every full song should loop naturally and stay interesting over repeated play.
Treat this as a composition requirement, not just a renderer setting. Explicit
one-shot requests, stingers, and isolated auditions are exceptions.
This skill is game-, engine-, and genre-independent. Resolve bundled paths from
this skill's directory, not the project directory.

## Routing

- For songs and musical stingers, use the music workflow below.
- For gameplay/UI sound effects or ambience, read `references\sfx.md`. These need
  purposeful attack/body/tail and feedback timing, not a song hook or chord quota.
  Procedural SFX do not need SoundFonts; the music bank preflight is for songs.
- Keep each project's compositions, recipes and exports with that project,
  outside this general skill. Write new outputs to a chosen project audio
  directory, not into the skill.

## Music workflow

1. Read `references\genres.md`, `references\composition.md`, and the relevant rows in
   `references\instruments.md`. Read `references\rendering.md` before rendering.
   Read `references\banks.md` when using or comparing SoundFont banks.
2. Ask internally: what emotions should the player feel, what is underneath
   those emotions, and how should they change through the cue? Infer this from
   the prompt, gameplay purpose, and game context; do not make it a repeated
   user approval step. Choose phrasing, motif behavior, harmonic movement, and
   timbral roles to express that emotional brief, not just a generic mood label.
   Select a genre from the prompt first, then the game's setting, tone, and
   gameplay when the prompt leaves it open. A cue purpose such as "boss battle"
   sets its role/energy, not a universal genre. Do not reuse one game's or the
   last cue's palette for every game. Inspect a small relevant project
   description when useful, consider plausible styles internally, and choose one
   without a questionnaire when context is sufficient.
   Infer a compact musical brief from the request: purpose, style, mood, energy,
   duration, instrumentation, and loop behavior. Honor explicit choices. Infer
   missing details without a questionnaire; only ask about a genuine blocker,
   paid services, or destructive integration. For unspecified prototype music,
   default to one instrumental cue around 60-90 seconds, a modest arrangement,
   and `loop: true`. Honor an explicit non-looping request; stingers and isolated
   auditions are normally non-looping. Do not generate multiple candidates unless asked.
3. Choose one render route for the cue from the sounds and effects its brief
   needs. Both routes start from the same Strudel pattern:

   | Route | Sounds | Effects | Choose for |
   |---|---|---|---|
   | Browser (`render.mjs`) | Strudel synths and explicitly registered sample maps | All Strudel effects: filters and sweeps, delay, reverb, FM, envelopes, automation | Electronic, synth, chiptune and ambient music; cues whose character depends on Strudel effects or modulation |
   | SoundFont (`render-soundfont.mjs`) | Presets from registered SF2 banks, with their velocity layers, loops and envelopes | MIDI controls only: volume, pan, reverb/chorus sends, CC automation | Realistic acoustic instruments: orchestral, piano, guitar, choir, acoustic band |

   A render uses one route. If a cue needs both realistic acoustic parts and
   Strudel-processed synths, choose the route that serves its foreground and
   core identity, cover the rest within that route (bank synth presets, or
   recorded sample maps in the browser), and mention the trade-off on delivery.
   Record the chosen route and why in `musicalDesign`.
   For the SoundFont route only, inspect the user-folder `banks.json` and
   available SF2 files with `node "SKILL_DIRECTORY\scripts\bank-catalog.mjs"`
   before composing. This is read-only. The browser route needs no SoundFonts;
   do not run or block on this check for it. If the folder/catalog is missing
   or unusable, or no registered SoundFonts are present, stop and use `ask_user`
   to ask whether the user would like to get some, offering the browser route
   as an alternative and stating what it would lose. Do not create a catalog,
   download banks, or switch routes before that decision. If they decline both,
   stop. If they want SoundFonts, follow "Choosing and registering a bank" in
   `references\banks.md`: the skill ships no banks, so propose candidates that
   fit this song and get approval of sources, licenses and sizes before
   invoking setup. Preserve existing user files.
4. Make the composition decisions in `references\composition.md` before writing
   the full arrangement. First sketch the primary hook/phrase by itself:
   a melody, riff, chordal gesture, or other recognizable identity. Make it
   communicate one coherent thought before filling bars with accompaniment.
   Then consider plausible harmonizations and accompaniment approaches; choose a
   backbone, phrase/answer relationship, and tension/release arc that serve it.
   Plan the musical end-to-start handoff and how the arrangement will remain
   engaging across repeated passes; use the looping guidance in the composition
   reference. Do not compose a standalone intro/outro and assume it will loop.
   Decide how accompaniment supports rather than competes with the
   foreground. Keep these decisions compact and internal, not a user review
   checkpoint. Choose sounds and an appropriate musical form from that design.
   Be free to select new instrumental families when the emotion calls for them.
   Do not let the last cue, sample cache, or previously used bank become a sonic
   template. Find and register additional licensed instruments when needed.
   Consider the sound library itself, not only instrument names in VCSL/VSCO.
   A different bank can change tone substantially. On the SoundFont route,
   re-evaluate the shared catalog's tags, strengths and preset coverage for this
   song's emotion, genre and instrument roles, even within the same game. Choose
   one bank or a small palette of banks: both are valid. Assign each part to its
   chosen bank/preset rather than layering every bank, forcing multiple banks,
   or keeping one permanent bank for the game.
   Plan how the cue will sound full: consider strings/ensembles, pads, guitars,
   piano/keys, wordless vocals, percussion, sustain, reverb, and delay as options,
   then select complementary roles. Do not restrict the palette to familiar
   sounds or default to only a plucked backing and a melody.
   Acoustic realism requires recorded, preferably multisampled instruments.
   Electronic genres require intentional synthesis, not an acoustic sound quota.
   Do not silently substitute basic oscillators for requested acoustic sounds.
5. Compose the entire cue using separately editable parts and explicit section
   lengths. Develop musical material rather than repeating one bar with more
   layers or inventing unrelated foreground notes in every bar. Establish the
   hook clearly, let it return recognizably, and vary selected aspects rather
   than replacing the entire phrase each time. A different instrument can carry
   the same identity; chords, dyads, octaves, and rhythmic figures are valid
   foreground choices, not just single-note melodies.
   A hook is an anchor, not permission to repeat one phrase for the entire song.
   Develop related calls, answers, tension phrases, and resolving returns when
   the style calls for them. Trade phrases between parts or introduce a partner
   phrase without losing the central identity.
   Shape a continuing musical cycle rather than a recurring start-and-finish
   routine. Keep appropriate groove/body through the wrap, and distribute
   phrase, harmony, voicing, texture, or rhythmic variation within the loop.
   Repetition should establish identity without making every lap a predictable
   reset, a repeated entrance fanfare, or the same short phrase throughout.
   In chord-supported styles, include some audible voiced chord gestures in
   the foreground or accompaniment, not only a distant pad or single-note
   arpeggio. Consider lower-register comping, upper dyads, chordal answers, and
   cadence accents; leave room rather than making every event a block chord.
   Keep deliberate rests and transitions. Ambient/drone music may have
   texture evolution instead of chords, drums, or a prominent motif. Check the
   foreground against the accompaniment at important beats and transitions.
   Prefer clarity over extra counterlines, extensions, or unrelated timbres.
   Explicitly balance the chosen sounds and design their note/phrase envelopes.
   Similar gain values across instruments are not similar perceived loudness.
   Keep a prototype lead present but not overpowering; avoid accidental abrupt
   gain changes at section boundaries or hard attacks on sustained instruments.
   Preserve intentional rests without letting the whole bed collapse: consider
   held common tones, real sustain/pedal and controlled effect tails. For a
   harsh peak, soften excitation/transients and tone rather than only adding
   reverb. Verify that the chosen render path actually supports the articulation.
   Check fullness independently of melodic clarity: fill missing harmonic body,
   low-end foundation, rhythmic presence, or spatial continuity without turning
   the lead back up. Quiet openings and breaks can breathe, but the cue must not
   sound like an unfinished skeleton. Effects alone do not replace arrangement.
   For a requested new direction, run the contrast check in `references\genres.md`:
   compare foreground/backing families, motif/phrasing, harmony, and groove with
   the previous cue. Redesign if the distinction is mainly tempo or labels.
6. Save standalone REPL-compatible `NAME.strudel.js` and `NAME.json` metadata.
   Include explicit sample registration in the source; the browser REPL's
   preloaded sounds are not available in the bundled renderer. Register only the
   sounds needed, with pitch maps for pitched recordings. Resolve current API
   questions through Context7; if unavailable, use official Strudel docs.
   Record a short `musicalDesign` in the metadata: accompaniment choice and
   alternatives considered, foreground identity, and buildup/release locations.
   Include the selected fullness roles and why the omitted options are unneeded.
   Record `genreSelection`: prompt constraints, game cues, the chosen genre, and
   how its groove, palette, form, and articulation differ from other cue types.
   Include `emotionalIntent`: feeling, underlying tension, emotional arc, and
   concrete musical/instrument choices that express it.
   Describe the primary hook's phrasing, return/development, and how its harmony
   supports it. A standalone foreground preview is useful when checking clarity,
   but must not become a mandatory user approval step.
   For a loop, record `loopDesign`: the melodic/harmonic and rhythmic handoff,
   continuity of sounds/effects, and the variations that support repeated play.
   For the MIDI/SoundFont route, use explicit `midiTracks` and document the bank,
   version, rights and renderer rather than browser sample registration.
   Read the shared `~\.strudel-skill-soundfonts\banks.json` (or
   `STRUDEL_SOUNDFONT_DIR`) and select one or multiple banks per song from its
   tags, strengths and preset coverage. Consult stored `midiMapping` and
   `drumNoteMap` data when present; do not assume specialist fonts use GM
   programs or drum channels. Use a song-level `bankId` default and optional
   `midiTracks[].bankId` overrides. Record each chosen bank's role and why it
   fits this song; do not assign one bank permanently to a game or require one
   different bank per track.
7. Render with the route chosen in step 3: `render.mjs` for the browser route or
   `render-soundfont.mjs` for SoundFonts, following their respective references. Run the renderer and fix
   errors and failed checks autonomously; do not
   ask the user to listen to successive drafts. Keep corrective attempts bounded
   (normally two). Do not replace Strudel with an unrelated synthesis script.
   If genre-specific postprocessing is needed, save its recipe or script with
   the cue so it is repeatable. Apply time-based effects with preceding musical
   history before the final loop trim; do not introduce an empty effect buffer
   at the start of an otherwise loopable cue.
   Review the closing phrase into the opening and consider consecutive full
   passes, not only an isolated beginning or ending. Inspect the score/events
   and rendered duration/tails; use a two-pass playback preview when useful.
   Do not require user approval or claim an auditory review without audio input.
   SoundFonts live in the shared user cache, not inside the skill. The native
   renderer stops when the shared library is absent/empty, so ask the user before
   acquiring any fonts. With an existing library, it can fetch a selected missing
   catalog bank on demand when allowed. Respect disabled downloads; report
   missing assets explicitly rather than changing banks silently. Preserve user
   catalog edits and fonts. Multi-bank stems share one timeline and warmup;
   balance parts and normalize only the combined mix, not each bank separately.
8. Deliver WAV, editable source, and the generated validation report. Add OGG
   only when useful for the destination. Put outputs in a clearly named project
   music-prototype directory unless the user specifies another location. Never
   replace existing music or wire engine behavior without being asked.

## Loose quality guardrails

Review all six areas during composition and the final self-review. Treat them
as context-sensitive guidance, not a musical scoring system or a guarantee of
taste. For now, do not add musical-quality tests, rigid numerical thresholds,
or automatic rejection rules to enforce these suggestions. Use measurements
as diagnostics when useful, not substitutes for genre/emotional judgment.

| Area | Starting guidance |
|---|---|
| Technical | Use the intended sounds and render route; resolve loading errors, accidental silence, clipping, incorrect duration, and truncated tails. Plan the loop's musical handoff and effect history, not just its file boundary. |
| Balance | Give parts distinct foreground/support roles. Keep the identity audible without overpowering the backing or gameplay. Judge the actual instruments, not equal gain values; normalization alone does not repair balance. |
| Dynamics | Choose envelopes and phrasing for the instrument and emotion. Avoid unintended hard gates, arbitrary velocity jumps, and abrupt section-level gain changes. Crisp percussion, accents, deliberate dropouts, and dramatic swells can be appropriate. |
| Fullness | Aim for a developed musical bed with fitting foundation, body, movement, and space. Keep selected supports audible without forcing a track count, every instrument family, every frequency band, or maximum density in every moment. Quiet openings and breaks can breathe. |
| Composition | Make the primary part communicate a coherent hook and a related family of phrases, not one unchanged line throughout. Use calls, answers, tension, resolving returns, and audible chord support where appropriate. Compose a natural end-to-start continuation and enough internal development for repeated play, without recurring intro/outro resets. Preserve identity without freezing the melody or filling it with unrelated notes. Chordal/rhythmic hooks and non-melodic identities remain valid; avoid fixed templates or forced complexity. |
| Variety | Choose genre, bank, timbre, articulation, groove, and musical language deliberately. For a new direction, avoid merely speeding up or relabeling the previous cue; for linked themes or a fixed palette, preserve the requested identity and vary other aspects. |

The existing renderer checks still enforce technical correctness; this does
not request new tests for subjective musical properties or weaken those checks.
Compose original material, honor source licenses, and preserve existing user
assets. Never claim perceived quality or an audition from metrics alone.

## Instruments and licenses

Select instruments from the shared SoundFont catalog, deliberate synthesis,
or explicitly registered licensed recordings as the cue requires. Raw sample
mappings belong in the project's cue source, not a fixed bundled palette.
Preserve source revisions, base pitches, licenses and credits in cue metadata.
Sample packs and SoundFonts have their own licenses.
Never assume a sound's availability in Strudel makes it redistributable.

### License review

Check the license of every SoundFont, sample pack, wavetable, or recording
before using it in a cue, and before downloading or registering a new one.

- **Use freely:** CC0/public domain. With attribution recorded in cue metadata
  credits: CC-BY, MIT, BSD, Apache-2.0, and zlib.
- **Alert the user for anything else**, then follow their decision; do not
  refuse on your own. This includes custom licenses (even ones that permit
  commercial music), CC-BY-SA, GPL/LGPL, non-commercial (NC), no-derivatives
  (ND), personal-use-only, and unknown or missing licenses. Name the source,
  its license, and the concern, such as share-alike reaching rendered music, no
  commercial use, or sample-redistribution limits on near-raw sound effects.
  Alert once per source per task, not for each cue that reuses it.

This is a practical screen, not legal advice.

Use Strudel as an authoring tool and deliver rendered audio by default. Its
AGPL software license is separate from sample and composition rights. Do not
embed the Strudel runtime into a game's distributable or change the project's
license without separately considering integration obligations.

## Listening and autonomy

The baseline needs no audio-model service and no human revision loop. Validate
the real renderer output and inspect musical event structure. An available local
audio-capable model may provide one targeted internal critique; external audio
uploads require explicit permission. An audio model is not a dependable taste
oracle. Never claim to have listened if only code, metrics, or waveforms were
inspected. Describe the musical intent and disclose the validation actually done.

## Execution

From the skill's `scripts` directory, install the pinned dependencies once:

```powershell
npm ci --no-audit --no-fund
```

Run with explicit paths (portable to another OS using its path syntax):

```powershell
node "SKILL_DIRECTORY\scripts\render.mjs" "OUTPUT\cue.strudel.js" "OUTPUT\cue.json" "OUTPUT"
```

The renderer uses an installed Edge/Chrome on Windows, or Playwright Chromium
elsewhere. If the default browser is missing, install it with
`npx playwright install chromium` from `scripts`; do not install a browser
preemptively. The first render downloads only public samples requested by the
cue and caches them locally. Nothing is uploaded. Execute only trusted Strudel
source: it is JavaScript, not a sandboxed music format.
