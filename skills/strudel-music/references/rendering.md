# Rendering and verification

Use actual Strudel pattern evaluation and its Web Audio renderer, not a Python
approximation. The pinned tool schedules Strudel's actual `superdough` engine
into an offline context in a task-owned headless browser. It binds a temporary
HTTP server to loopback, closes both on
exit, and never touches a user's browser or game editor.

## Files and command

The source must evaluate to one Pattern. Define parts as variables, then finish
with `stack(...)` or `arrange(...)`. Include explicit `samples(...)` registration
and `setcpm(...)` in the source so it also works when pasted into Strudel's REPL.
The renderer's timing comes from JSON; keep both tempos consistent.

Example metadata (replace these choices; optional `sections` and `licenses` are
copied into the validation report):

```json
{
  "name": "example-cue",
  "bpm": 96,
  "beatsPerCycle": 4,
  "cycles": 32,
  "loop": true,
  "tailSeconds": 0,
  "sections": [
    { "name": "A", "cycles": 16 },
    { "name": "B", "cycles": 16 }
  ],
  "licenses": []
}
```

```powershell
node "SKILL_DIRECTORY\scripts\render.mjs" "OUTPUT\cue.strudel.js" "OUTPUT\cue.json" "OUTPUT"
```

Requires Node 20+, the pinned npm dependencies, and a supported Chromium browser.
On Windows the script detects Edge or Chrome at their standard installation
paths; otherwise it uses Playwright's installed Chromium. `STRUDEL_BROWSER_PATH`
can select another installed executable. No browser download is needed here.

Output is 44.1 kHz stereo 16-bit WAV, normalized together to -3 dBFS peak only
after rejecting clipped/silent raw PCM. Non-loops append silence to allow
`tailSeconds` (default 3) of decay. Loops render two complete passes and retain
the second, with the exact requested sample count and no endpoint fades.
An extra raw guard frame handles separate warmup/loop rounding at fractional
sample lengths; it is removed by the exact-length final trim.
Scheduling advances one cycle at a time with a one-cycle lookahead so completed
audio nodes can be reclaimed; scheduling a whole song up front can stall the
pinned renderer. This warmup assumes all meaningful pattern periods align with `cycles`, and
effect tails are shorter than a pass. Increase/align the cue length if not.

The `.validation.json` reports duration, peak/RMS level, clipped-sample count,
boundary jump, per-second RMS, onset counts by sound, public asset URLs, source
hash, and section/provenance metadata. Errors, unknown voices, failed downloads,
empty patterns, silence, clipped raw PCM, and invalid timings fail the command.
The boundary jump is reported, not universally failed: transients and
non-looping endings have different legitimate behavior.

Inspect the report for unexpected quiet sections and missing intended parts.
Metrics cannot establish groove, taste, realism, or musical seam quality.
The `loop` flag, exact duration, and warmed effects do not establish a natural
musical return or replay interest; also review the composition's `loopDesign`
and end-to-start phrase relationship. Do not add musical-quality scoring tests.
Report that limitation honestly rather than claiming an audition.

Only execute trusted source. Sample requests are GET downloads cached under
`scripts\.cache\samples`; source and rendered audio stay local. Re-rendering can
use cached assets. To reproduce on another machine, install dependencies and
allow the documented public sample downloads.

Optional game-ready OGG conversion (after a successful WAV render):

```powershell
ffmpeg -y -v error -i "OUTPUT\cue.wav" -c:a libvorbis -q:a 6 "OUTPUT\cue.ogg"
```

Optional two-pass loop preview, with no separator, fade, or crossfade:

```powershell
ffmpeg -y -v error -stream_loop 1 -i "OUTPUT\cue.wav" -c:a libvorbis -q:a 6 "OUTPUT\cue-loop-preview.ogg"
```

Label this as a playback preview; the single-pass WAV/OGG remains the game asset.
Check the converted file as well as the WAV. A preview exposes the wrap for
listening but cannot provide auditory evidence to a text-only assistant.

Do not embed Strudel into the game just to play the render.
For tool changes run `npm test` and `npm run check` from `scripts`, followed by
one real browser render. When a render fails, preserve useful diagnostics and
fix the cause rather than automatically installing unrelated packages.
