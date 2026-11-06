# Genre selection from context

Choose the musical style before instruments and patterns. This skill is not a
fantasy-music template; examples and bundled sounds must not dictate every cue.

## Emotional brief first

Ask internally: what should the player feel, what conflicting/underlying feeling
adds meaning, and where should that feeling change? Infer a concise answer from
the request and context. "Boss battle" is a use case, not an emotion: it could
feel terrified, defiant, exhilarated, tragic, mischievous, or overwhelmingly
powerful. Likewise, "upbeat" need not imply the same kind of confidence in every
game.

Translate the answer into musical behavior before selecting familiar sounds:

| Emotional direction | Phrasing / motif possibilities | Timbral possibilities |
|---|---|---|
| Defiant swagger | Syncopated short calls, deliberate gaps, assertive answers, a pocketed bass groove | Brass bites, clavi/electric keys, rounded bass, a close drum kit |
| Relentless threat | Insistent small cells, restricted breathing, delayed arrivals | Distorted guitar, mechanical percussion, low synth weight, rough textures |
| Heroic exhilaration | Broad upward contour, clear arrivals, a developed answering phrase | Brass/ensembles, ringing guitar or synth, expansive but controlled support |
| Fragile hope | Tentative entrances, gentle repetition, gradually opening phrases | Soft keys, intimate guitar, airy vocals, restrained warm support |
| Uncertainty / dread | Incomplete phrases, unstable intervals, suspended destinations | Low drones, sparse piano, breath/noise, unfamiliar or detuned colors |
| Playful momentum | Bouncy accents, surprising rests, compact exchanges | Mallets, bright keys, guitar comping, nimble percussion |

These are options, not universal emotion-to-instrument rules. Explain why the
chosen articulation, register, rhythm, and timbre express this cue's feeling.
Do not reduce emotion to changing a key or adding a louder pad.

## Priority

1. Honor an explicit genre/style in the user's prompt, including hybrids, even
   when it differs from the game's apparent aesthetic. Preserve explicit mood,
   tempo, instrument, and energy constraints.
2. If the prompt specifies a purpose or feeling but no genre, combine that with
   the game's setting, tone, pacing, and presentation. Read a small relevant
   project description rather than guessing from the repository name alone.
3. If the prompt is open, choose an appropriate style from game context. Keep
   related cues coherent without making exploration, shops, battles, and menus
   identical.
4. If both are absent, use a reasonable stated prototype assumption. Ask only
   when a genuine ambiguity blocks a useful result, not to solicit repeated
   musical approvals. Do not ask users to pick among audition sketches by default.

Consider two meaningfully different directions internally when style is open,
not only two variants of the previous palette. Select one and briefly explain
the choice in metadata. Broad genre vocabulary is useful; do not copy an
existing soundtrack or recording.

## Context to musical behavior

These are examples, not mandatory mappings or a supported-genre whitelist.

| Context / prompt | Plausible directions and behavior |
|---|---|
| Fantasy exploration | Folk/chamber/modal textures, acoustic phrasing, moderate pulse |
| Urgent fantasy boss fight | Orchestral action, rock, or orchestral/electronic hybrid; driving ostinato, strong kit, developed heroic/tense motif |
| Cyberpunk / futuristic action | Electro, synthwave, industrial, drum-and-bass; shaped synth bass, electronic rhythm, timbral modulation |
| Playful arcade / platforming | Funk, upbeat pop, chiptune, ska-influenced rhythm; buoyant syncopation and concise hooks |
| Cozy management / farming | Acoustic pop, jazz-influenced, lo-fi; relaxed groove and warm harmonic body |
| Noir / investigative play | Sparse jazz, downtempo, dark ambient; restrained foreground, economical harmony, space |
| Horror / unsettling exploration | Ambient, drone, industrial textures; controlled instability and non-melodic tension |
| Retro presentation | Chiptune is an option, not an obligation; match the prompt and game's tone rather than pixel art alone |
| Explicit genre prompt | Derive that genre's groove, sound palette, harmony/riff language, and structure; find appropriate instruments instead of forcing the starter palette |

"Upbeat" means more energy/momentum, not necessarily major-key cheerfulness.
"Boss battle" can be metal, electronic, orchestral, jazz, or something unusual;
choose from the prompt/game rather than hard-coding an orchestral answer.

## Make the genre audible

Change the groove, rhythmic subdivisions, articulation, instrumentation,
harmonic/riff vocabulary, and form as appropriate. Do not merely speed up the
previous exploration cue. A fast action cue can use short strings, punchier
drums, and rhythmic bass; a lyrical cue may use sustained ensembles and
breathable melody. Different genres may need entirely different sources.

Keep the skill's core quality standard across genres: full musical body, one
clear identity, purposeful contrast and tension/release, deliberate instrument
balance, and useful space for gameplay. Faster or heavier does not justify
clipping, an overpowering lead, constant maximal density, or abrupt gain jumps.

Document the selected style and its audible consequences in `genreSelection`.
When making a new cue, keep existing liked prototypes unchanged unless the user
asks to replace them.

## Contrast check for a new direction

Compare the proposed cue with the previous one before rendering. These are
loose creative questions, not a timbral-distance test or a minimum-change quota:

- What is the lead family and how does it phrase its identity?
- What supplies the backing/body, and how does it move rhythmically?
- What changed in the motif, harmonic path, and emotional destination?
- What changed in groove, articulation, register, and production space?

If a supposed new style retains the same string bed, key/progression, lyrical
phrase structure, and orchestration, a faster beat or swapped synth lead is not
enough. For an unconstrained new-direction request, normally change both the
foreground and backing families and several aspects of musical language. Do not
keep previous chords or motifs just because they already worked.

Honor explicit requests for linked themes, a fixed palette, or variations; then
retain intentional identity and create contrast through other choices. Timbral
freedom is not arbitrary instrument swapping: every choice should serve the
emotional brief and preserve a full, coherent mix.
