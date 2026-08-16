EXCEL HELL — dynamic music pair
Generated specifically for the Dimatit & TAIFUN 3.0 jam build.

NORMAL LOOP
- excel_hell_normal_office_75bpm.wav / .ogg
- 75 BPM, 4/4, 25.6 s, 8 bars
- subdued corporate / office / minimal electronic
- deliberately low-event and slightly melancholy

PSYCHOSIS LOOP
- excel_hell_psychosis_75bpm.wav / .ogg
- 75 BPM, 4/4, 25.6 s, 8 bars
- same harmonic/rhythmic skeleton as NORMAL
- detuned, distorted, anxious ambient / digital corruption
- loudness matched closely to NORMAL for smooth crossfade

FINAL STINGER
- excel_hell_final_stinger.wav / .ogg
- 8.0 s
- optional end-of-run / report-collapse cue

UNITY IMPLEMENTATION
1. Start NORMAL and PSYCHOSIS on the same DSP time.
2. Keep both AudioSources playing for the whole level.
3. Put each through a separate AudioMixer group.
4. Crossfade mixer volume; never stop/restart either loop.
5. Suggested fade duration: 1.5–3.0 s.

Suggested Psychosis mapping:
Level 0–1: Normal   0 dB | Psychosis -80 dB
Level 2:   Normal  -4 dB | Psychosis -14 dB
Level 3:   Normal -12 dB | Psychosis  -4 dB
Level 4:   Normal -24 dB | Psychosis   0 dB

TECHNICAL
- stereo
- 44.1 kHz
- 16-bit WAV + OGG Vorbis
- 10 ms seam guard at loop boundary
- no vocals
- no third-party samples or copyrighted source recordings were used
