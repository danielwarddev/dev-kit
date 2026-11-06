# Composition, not pattern accumulation

Make musical decisions before syntax. A prototype needs a convincing first pass,
not exhaustive theory, elaborate tooling, or a perfect master.

## Brief and form

Choose tempo and meter for the intended feel, not a universal 4/4 requirement.
Explicitly define beats per Strudel cycle; BPM / beats-per-cycle / 60 gives CPS.
One cycle per bar is convenient but not mandatory.

Write a section map with cycle counts. A 32-bar tonal cue might use a four-bar
opening, eight-bar theme, eight-bar contrast, eight-bar developed return, and
four-bar release. This is an example, not a fixed template. Dance cues can use
build/drop/breakdown; ambient cues can use gradual textural states; stingers can
be one gesture and its decay. Avoid forcing verse/chorus onto everything.

Before patterns, decide the accompaniment backbone, foreground identity, and
tension/release arc below. These are internal composition decisions, not an
audition menu or a questionnaire. Briefly record the decisions in cue metadata.

Start from the emotional brief in `references\genres.md`. What should the
phrases communicate: hesitation, swagger, pressure, longing, relief, or another
feeling? Make the motif's contour, rhythmic placement, rests, repetition, and
answer express that. Choose accompaniment movement and timbre that support the
same intention. Do not write a familiar progression first and attach an emotion
label afterward.

For connected cues, share a motif or palette and choose compatible intensity
changes. Stems are optional; if requested, keep their timeline, tempo, and render
history identical, and do not normalize each stem independently.

## Accompaniment: choose the musical floor

Consider plausible approaches for this brief, then select one
primary backbone. Do not combine all of them by default.

| Approach | What it provides / what to watch |
|---|---|
| Broken chords / arpeggios | Harmonic motion and pulse; keep below/around the lead without competing melodic leaps |
| Sustained chords / pad / drone | Space and continuity; avoid muddy overlaps and constantly unresolved extensions |
| Strums / chord stabs / rhythmic comping | Groove and accents; leave the foreground's important attacks clear |
| Ostinato / riff | Repeated identity and momentum; make its pitches compatible with changing harmony |
| Bass-led groove / percussion bed | Rhythmic foundation with little harmonic clutter; don't fill every gap |
| Sparse answers / punctuations | Room for a strong foreground; silence must feel intentional rather than unfinished |
| Evolving textural bed | Atmosphere without conventional harmony; control density and spectral competition |

Choose harmonic pace, register, rhythmic shape, articulation, and density for
the selected backbone. Decide what stays stable enough to make the cue coherent
and what changes at section boundaries. A contrasting section can simplify or
reshape the same backbone rather than replace the entire sound palette.

Check the accompaniment on its own: does it establish a believable foundation,
or is it another wandering melody? If it carries a strong riff, simplify the
lead or let it answer the riff. Keep bass roots/inversions aligned with the
chosen harmony. Do not make acoustic realism depend on adding unrelated
instruments to every register.

## Fullness: required, not maximal

Every finished cue must sound full for its style. Melody plus a bare plucked
pattern is not the default finished product. Keep clarity and breathing room,
but make the music feel supported and developed rather than skeletal.

Assess the roles below, consider complementary options, and fill genuine gaps.
One instrument can cover multiple roles; not every cue needs every family.

| Role | Options to consider |
|---|---|
| Harmonic body / continuity | Bowed strings, ensembles, shaped synth pads, organ, sustained keys, wordless choir, resonant guitar |
| Foundation / weight | Bass guitar, synth/sub bass, cello, low strings, low piano/harp, appropriately tuned drums |
| Rhythmic presence | Acoustic/electronic drums, hand percussion, shakers, strumming, rhythmic keys, sequenced pulse |
| Identity / detail | Lead, vocal phrase, guitar figure, mallet answers, restrained countermotion, timbral evolution |
| Depth / cohesion | Balanced registers, stereo width, shared room impression, short delay, controlled sustain/release |

Strings need not mean an orchestra; a soft inner-voice ensemble can supply the
missing middle while the melody stays clear. Guitar can strum, arpeggiate, play
a riff, or swell; choose the articulation rather than merely a guitar label.
Wordless vocals can provide warmth or atmosphere without lyrics. Piano/keys can
provide a broader harmonic bed than isolated plucks.

Choose combinations with different jobs. Do not add strings, guitar, and choir
all doubling the same busy part, or add another lead to compensate for an empty
accompaniment. For chiptune, electronic, solo, or ambient briefs, achieve the
same completeness through appropriate voicing, register, timbre, movement, and
space rather than forcing acoustic instruments into the style.

Make the selected supports audible, not merely present in the code. Inspect
representative active sections and melodic rests for missing body, pulse, or
low end. If the foreground is removed, a believable musical bed should remain.
Quiet introductions, releases, and intentional breaks can be thinner, but should
retain cohesion; fullness does not require constant maximal intensity.

## Space and sustain

Use reverb to place instruments in a coherent space and delay for restrained
rhythmic/atmospheric support. Match delay to the groove, limit feedback, and keep
low end controlled. More wet signal is not the same as a fuller arrangement.
Keep incompatible global effects in separate Strudel orbits.

Sustain is an articulation decision, not simply a long reverb. Use appropriate
held notes, recorded sustain articulations, and release lengths. Piano sustain
pedal behavior depends on the instrument/renderer; do not claim authentic pedal
resonance from a generic long release. With a supporting MIDI instrument, CC64
can operate its pedal. For sampled audio, manage compatible note tails and chord
changes so sustained harmony does not turn into accidental dissonance.

## Foreground: choose the musical identity

The primary part needs a hook: one coherent thought that can be recognized when
it returns. Complexity is optional; an aimless chain of compatible notes is not
enough. Work on the foreground by itself before treating it as a completed lead
inside a full arrangement.

Start with a finished phrase appropriate to the emotion and style. Choose an
identifiable rhythmic shape, contour, emphasis, breath, and destination. Let the
idea be stated clearly and return recognizably before adding variations. Keep a
stable rhythmic or melodic fingerprint while changing selected endings, accents,
registers, or answers; do not generate an unrelated new line for each chord.
Repetition establishes identity, whereas indiscriminate variation can erase it.

A recognizable hook is an anchor, not the entire composition. Develop a small
family of related phrases: a call, an answering contour/rhythm, a phrase that
withholds its destination, and a return that resolves it. This is one useful
approach, not a mandatory A/B/C template. A partner phrase, a changed harmonic
destination, and an instrument exchange can provide real development without
making every bar unrelated. Avoid overcorrecting coherence into endless literal
repetition or only changing one final note for the entire cue.

Ask internally: what does this primary part say without the pads and drums?
What is its recognizable gesture, and how does its next phrase answer or develop
it? Simplify, reshape the rhythm, or strengthen its arrival if that is unclear.
A short isolated render can help inspect the idea, without demanding a user
audition checkpoint or claiming to have heard it when no listening input exists.

Be free to use a singing line, bass/guitar riff, chordal keyboard figure, rhythmic
stabs, octaves, dyads, or a combination. "One primary thought" does not mean
"only monophonic notes." Choose a coherent foreground role before ornament or
competing counterlines.

Consider different ways to harmonize the hook: a pedal/static bed, modal chords,
a functional progression, inversions, chordal answers, or rhythmic comping.
Change the progression when the melody needs a better foundation rather than
forcing it into the last cue's chords. Reharmonization can create contrast while
the hook remains stable. The strongest melody notes and arrivals should make
musical sense against that specific backing.

For chord-supported genres, make some voiced harmony audible in the lead or
accompaniment itself. A pad behind a single-note line may not provide the same
musical fullness as lower-register piano/guitar comping, chordal answers,
upper-register dyads, or a chord at a cadence. Choose a few useful points rather
than making the whole track block chords. Keep voicings clear of the melody's
register/attacks and balance their combined level, not just each note's gain.
Harmony and melody can be refined together; neither needs to be frozen while
the other is forced to fit. Honor intentionally monophonic or non-chordal briefs.

Decide whether the foreground is melody-led, riff-led, rhythmic, or textural.
An ambient cue may have no tune; a riff-led cue may not need a separate lead.
Do not force a singing melody onto those cases.

Check important foreground notes against the actual accompaniment, not just
membership in a scale. Account for accented dissonances, passing notes,
suspensions, and their destinations. A sustained melody note needs compatible
harmonic movement beneath it unless the clash is intentional. Avoid an active
countermelody until the main idea and accompaniment already work together.

## Tension and release: choose the destination

Map the intended energy and stability over the whole cue and within its phrases.
Identify where tension begins, what changes build it, the main arrival, and what
the release actually removes or resolves. Section labels alone are not an arc.

Possible tension tools include harmonic departure, a held suspension or pedal,
delayed cadence, rising register, denser rhythm, reduced breathing room,
increasing brightness, or a deliberate dropout before an arrival. Choose a
small compatible subset; a buildup is not just "add every track and turn up."

Release needs a complementary action: resolve the harmonic/contour destination,
restore an established groove or motif, reduce density/register/brightness, or
return to space. In functional tonal music, plan cadences and voice resolutions
explicitly. In modal, dance, ambient, or horror music, use the appropriate
rhythmic, timbral, or textural return instead of forcing a dominant-tonic cadence.
Persistent unease can be intentional, but should not be accidental ambiguity.

Keep prototype game cues moderate enough to repeat. A peak need not be loud or
dramatic. For loops, the end-to-start transition is part of the arc, not a
technical afterthought.

## Loops: ongoing music, not a repeated restart

Default full songs to looping unless explicitly requested otherwise. Design the
opening and closing together: the player should hear a continuing musical
thought, not an ending followed by a fresh introduction.

Check the final phrase's destination against the first phrase, including
melodic pickups, bass movement, chord voicing/common tones, groove phase, and
instrument density. A dominant resolving into the opening is one option, not a
universal requirement; modal, riff-based, and ambient music can return through
rhythm, timbre, or texture. Preserve appropriate body and momentum across the
wrap. An intentional recurring break can work, but avoid a disconnected teardown
and rebuild just because the file ended.

Keep one-time entrance fanfares, countdowns, final victory cadences, and fades
outside the repeating body when their function is truly one-shot. Do not hide a
weak musical handoff behind silence or a large crossfade. Render with preceding
history so held voices, sustain, reverb, and delay remain coherent at the wrap;
align meaningful pattern periods with the exported cycle.

Design for repeated play as well as a smooth seam. Develop the hook through
related answers, altered endings, register or instrument exchanges,
reharmonization, accompaniment voicing, groove/fill changes, or evolving texture.
Choose variations that suit the emotion and genre, with breathing space and
recognizable returns; do not erase the identity or force constant novelty.
Distribute interest through the body rather than reserving every change for
one grand buildup that resets on each pass. Choose enough musical development
and an appropriate duration for its gameplay role, not a fixed minimum length
or motif-count quota.

Review the closing phrase into the opening, then consider two consecutive full
passes for repeated intro gestures, density drops, overused fills, and a frozen
foreground. Use score/event inspection and, when listening input is available,
an auditory review. A two-pass playback preview can expose the intended wrap
without making audition/approval a mandatory user workflow. Record the handoff
and replay-interest decisions in `loopDesign`. A fixed audio loop necessarily
repeats; promise coherent development, not endlessly new material or a numerical
guarantee against listener fatigue.

## Pitched music

- Pick a harmonic vocabulary appropriate to the style: triads, modes, extended
  chords, drones, pedal tones, or deliberate dissonance. More complex is not
  inherently better.
- Plan melodic phrases with a contour, emphasis, and breathing room. Repeat
  something recognizable, then change its answer, rhythm, contour, or register.
  Random scale notes and repeating up/down arpeggios are not substitutes for a
  melody.
- Align important melody notes with the current harmony. Passing tones can add
  tension; resolve them intentionally when the style calls for resolution.
- Give accompaniment economical voice leading. Preserve common tones and move
  inner voices by small intervals where appropriate; avoid moving every voice
  with the bass by default.
- Let bass establish movement and groove rather than endlessly doubling every
  beat. Vary inversions, note lengths, approaches, and rests with intent.

## Rhythm and orchestration

Assign jobs: foreground, harmonic support, bass, pulse, ornament, atmosphere.
Not every cue needs every role. Three coherent parts can outperform eight busy
ones. Leave registers and time for each other; counterlines should answer or
complement the foreground.

Match articulation to the instrument. Plucked/mallet sounds decay naturally;
wind melodies need believable breaths and phrase lengths. Avoid transposing a
single acoustic recording across several octaves when pitch-region samples are
available. Do not add random timing or velocity offsets indiscriminately;
intentional accents and modest deterministic variation are more reproducible.

Choose fills and transition gestures sparingly. Change harmony, groove, contour,
or texture in contrasting sections, not merely loudness or instrument count.
Keep game music's foreground restrained beneath play and dialogue.

## Loose quality review before rendering

Use the six guardrails in `SKILL.md` as composer questions, not pass/fail tests:
is the render technically usable, are the roles balanced, do the dynamics feel
intentional, is the bed full for this style, does the composition express its
emotional arc, and does the palette/language fit the requested direction?

Ask what carries the accompaniment and identity, where tension builds, and what
makes release perceptible. If the answers are unclear, simplify or develop the
weak area rather than adding every available layer. Review phrase entrances,
accented/held notes, bass changes, and the main arrival against the section map.
Check that the foreground has a singular thought and recognizable returns,
not just a sequence of plausible pitches. More chords, effects, or instruments
do not rescue a weak hook.
Remove an unnecessary counterline or extension before using effects to hide a
clash. Intentional dissonance, percussive attacks, sparse passages, and related
themes are valid when they serve the brief.

Do not turn these suggestions into mandatory chord-tone percentages, motif
counts, spectral quotas, lead/backing dB bounds, or timbral-distance scores.
Resolve actual technical failures with the existing tools; musical choices
remain genre-aware judgment and do not require a user audition checkpoint.

## Style starting points

| Direction | Useful choices, not mandates |
|---|---|
| Folk/fantasy | Modal melody, acoustic plucks/winds, drones, light hand percussion |
| Orchestral/cinematic | Recorded instrument articulations, voice-led ensembles, dynamic gestures |
| Downtempo/lo-fi | Syncopation or modest swing, warm chords, restrained bass/drums |
| Synthwave | Pulsing bass, electronic drum bank, shaped synth chords, clear lead phrases |
| House/techno | Dance groove, phrase-aligned entrances, modulation, purposeful breakdowns |
| Drum and bass | Break-derived groove, bass movement, halftime contrast, controlled density |
| Jazz-influenced | Chord tones/extensions, economical voice leading, rhythmic conversation |
| Rock/metal | Riff-driven form, appropriate guitar articulation, drum/bass interaction |
| Chiptune | Deliberate limited voices, concise melodic identity, register separation |
| Ambient/horror | Texture, space, slow evolution, controlled instability; melody optional |

For an unlisted or hybrid genre, derive its musical characteristics rather than
choosing the closest label and ignoring the request. If the available sounds
cannot credibly realize it, find suitably licensed recordings or state the
limitation; do not pretend a square wave is a realistic electric guitar.

## Mix and endings

Start quiet. Keep the bass/kick centered, distribute supporting parts modestly,
and avoid filling every orbit with long reverb. In Strudel, delay/reverb are
shared per orbit: incompatible settings need different orbits.

Choose foreground/background balance using the actual sounds, not comparable
knob values. Sustained winds, bright leads, pads, and short plucks can differ
substantially in perceived loudness at the same gain. For gameplay prototypes,
the lead should remain identifiable without dominating the combined backing.
Consider register and spectral prominence as well as RMS and peak level.

Check representative active passages, not just a whole-file average that
includes silent introductions. If balance is uncertain, compare isolated parts
or stems using a common master gain. The bundled report's raw RMS in dB can be
recovered as `rmsDbfs - 20 * log10(normalizationGain)`; do not compare separately
peak-normalized stem loudness as if it were the original balance. Peak
normalization raises the whole mix and does not repair relative levels.

Design envelopes and dynamics separately from note pitches. For sustained
instruments, avoid a hard attack, an abrupt gate at each note end, and arbitrary
note-to-note velocity changes. Use appropriate attack, decay, sustain, release,
and note spacing without smearing fast passages into unintended chords.
Prefer a stable gain across sections when contrast already comes from harmony,
rhythm, and density. If a crescendo or fade is needed, make it intentional and
gradual; do not substitute large per-bar/per-section volume jumps. Strudel's
patterned gain is normally sampled at event onsets, whereas ADSR acts during
each sound; a smooth-looking control pattern is not necessarily a continuous
audio fade. Check pitch-region sample loudness if notes still jump in level.

If rests feel like the whole cue disappears, distinguish an intentional phrase
breath from an accidentally dry/short-decaying arrangement. Hold common tones
in supporting pads, use appropriate sustain/pedal, and let controlled room or
delay tails carry between gestures. Refresh the pedal with the harmony so this
does not turn into accumulated dissonance. Do not fill every rest with new notes.

For harsh attack emphasis, work on excitation and transient shape as well as
overall gain: softer velocity layers, suitable source/preset envelopes, gentle
peak compression and high-frequency rolloff can let the body/tail remain
audible. Reverb alone does not repair a sharp transient. Keep percussion's
intentional attack where appropriate, and do not claim Web Audio ADSR or generic
MIDI controllers changed an envelope unless that rendering path supports them.

An isolated audition can be drier or differently normalized than the full mix.
Label it clearly, or apply comparable articulation/space when evaluating its
tone; do not mistake an intentionally dry solo for the song's complete bed.

Non-loops need a composed ending and rendering time for tails, not an arbitrary
cut. Loops need compatible harmony, phrase continuation, and effect history.
Do not solve a musical seam with a large crossfade that smears the beat.

Sources: https://strudel.cc/learn/factories/,
https://strudel.cc/learn/time-modifiers/, https://strudel.cc/learn/effects/.
