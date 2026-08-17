using System;
using System.Collections.Generic;
using System.Reflection;
using ExcelHell.Application;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Final jam audio layer. Replaces legacy music/ambience/ref-move without deleting their assets,
    /// adds staged SUNO soundtrack crossfades and the final REF/psychosis cues.
    /// </summary>
    [DefaultExecutionOrder(2390)]
    public sealed class PrototypeFinalAudioPass : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string SfxRoot = "SFX/EXCEL_HELL_Audio_Kit_v1/";
        private const string MusicRoot = "Soundtrak/SUNO_tracks/";
        private const float MusicBaseVolume = 0.38f;
        private const float CrossfadeSeconds = 3.2f;
        private const float PsychosisBurstGate = 0.13f;

        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo LegacyNormalField = typeof(PrototypeAudioDirector).GetField("normal", Flags);
        private static readonly FieldInfo LegacyPsychosisField = typeof(PrototypeAudioDirector).GetField("psychosis", Flags);
        private static readonly FieldInfo LegacyAmbienceField = typeof(PrototypeAudioDirector).GetField("ambience", Flags);
        private static readonly FieldInfo LegacyStingerField = typeof(PrototypeAudioDirector).GetField("stinger", Flags);
        private static readonly FieldInfo LegacyRefMoveField = typeof(PrototypeAudioDirector).GetField("refMove", Flags);

        private static readonly FieldInfo SettingsScreenField = typeof(ExcelHellApplication).GetField("settingsScreen", Flags);
        private static readonly FieldInfo MusicSliderField = typeof(ExcelHellApplication).GetField("musicSlider", Flags);
        private static readonly FieldInfo SfxSliderField = typeof(ExcelHellApplication).GetField("sfxSlider", Flags);

        private AudioClip lateLedger;
        private AudioClip spreadsheetDrift;
        private AudioClip shiftedCells;
        private AudioClip refSpawn;
        private AudioClip psychosisManifest;

        private AudioSource musicA;
        private AudioSource musicB;
        private AudioSource refSource;
        private AudioSource psychosisSource;
        private AudioSource activeMusic;
        private AudioSource incomingMusic;
        private AudioSource outgoingMusic;

        private PrototypeAudioDirector legacyDirector;
        private ExcelHellPrototype prototype;
        private ExcelHellApplication application;
        private CellState[,] states;
        private readonly HashSet<int> knownPsychosisRoots = new();
        private readonly Dictionary<int, GameObject> pendingPsychosisRoots = new();
        private readonly HashSet<int> knownMessageRoots = new();

        private AudioClip requestedTrack;
        private float crossfadeT = 1f;
        private float musicVolume = 0.8f;
        private float sfxVolume = 0.9f;
        private float lastPsychosisAt = -100f;
        private float psychosisRandomGain = 0.45f;
        private bool l2RefSpawned;
        private int boundLevel = -99;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeFinalAudioPass>(FindObjectsInactive.Include) != null) return;
            var go = new GameObject("[PRESENTATION] Final Audio Pass");
            DontDestroyOnLoad(go);
            go.AddComponent<PrototypeFinalAudioPass>();
        }

        private void Awake()
        {
            lateLedger = Load(MusicRoot + "Late_Ledger_Loop_MASTER_LOOP");
            spreadsheetDrift = Load(MusicRoot + "Spreadsheet_Drift_MASTER_LOOP");
            shiftedCells = Load(MusicRoot + "Shifted_Cells_MASTER_LOOP");
            refSpawn = Load(SfxRoot + "ref_spawn");
            psychosisManifest = Load(SfxRoot + "psychosis_manifest");

            musicA = CreateSource(true);
            musicB = CreateSource(true);
            refSource = CreateSource(false);
            psychosisSource = CreateSource(false);

            var settings = AppSettingsService.Current ?? AppPersistence.LoadSettings();
            musicVolume = Mathf.Clamp01(settings.MusicVolume);
            sfxVolume = Mathf.Clamp01(settings.SfxVolume);

            AppSettingsService.AudioVolumesChanged += OnAudioVolumesChanged;
            RequestMusic(lateLedger, true);
            Debug.Log("[AUDIO/FINAL] SUNO soundtrack + REF/psychosis pass enabled.");
        }

        private void OnDestroy()
        {
            AppSettingsService.AudioVolumesChanged -= OnAudioVolumesChanged;
        }

        private void Update()
        {
            if (PrototypeAuthoringMode.Active)
            {
                StopAllNewAudio();
                return;
            }

            BindLegacyDirector();
            DisableLegacyBeds();
            ReadLiveSettings();

            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) BindPrototype(current);

            if (prototype != null)
                ObserveRefSpawns();

            UpdateRequestedMusic();
            AdvanceCrossfade();
        }

        private void LateUpdate()
        {
            if (PrototypeAuthoringMode.Active) return;
            ObservePsychosisManifestations();
            DisableLegacyBeds();
        }

        private void BindLegacyDirector()
        {
            var current = PrototypeAudioDirector.Instance;
            if (current == legacyDirector) return;
            legacyDirector = current;
            if (legacyDirector == null) return;

            // Disable ref_move in code while leaving the asset untouched in Resources.
            LegacyRefMoveField?.SetValue(legacyDirector, null);
            DisableLegacyBeds();
            Debug.Log("[AUDIO/FINAL] legacy ref_move, ambience and soundtrack disabled.");
        }

        private void DisableLegacyBeds()
        {
            if (legacyDirector == null) return;
            StopAndMute(LegacyNormalField?.GetValue(legacyDirector) as AudioSource);
            StopAndMute(LegacyPsychosisField?.GetValue(legacyDirector) as AudioSource);
            StopAndMute(LegacyAmbienceField?.GetValue(legacyDirector) as AudioSource);
            StopAndMute(LegacyStingerField?.GetValue(legacyDirector) as AudioSource);
            LegacyRefMoveField?.SetValue(legacyDirector, null);
        }

        private static void StopAndMute(AudioSource source)
        {
            if (source == null) return;
            source.mute = true;
            source.volume = 0f;
            if (source.isPlaying) source.Stop();
        }

        private void BindPrototype(ExcelHellPrototype value)
        {
            prototype = value;
            states = null;
            knownPsychosisRoots.Clear();
            pendingPsychosisRoots.Clear();
            knownMessageRoots.Clear();

            var level = prototype == null ? -1 : PrototypeLevelRuntime.CurrentIndex;
            if (level != boundLevel)
            {
                boundLevel = level;
                if (level == 1) l2RefSpawned = false;
            }

            SnapshotCells();
        }

        private void SnapshotCells()
        {
            if (prototype == null) return;
            var cells = CellsField?.GetValue(prototype) as CellModel[,];
            if (cells == null) return;
            states = new CellState[cells.GetLength(0), cells.GetLength(1)];
            for (var r = 0; r < cells.GetLength(0); r++)
            for (var c = 0; c < cells.GetLength(1); c++)
                states[r, c] = cells[r, c].State;
        }

        private void ObserveRefSpawns()
        {
            var cells = CellsField?.GetValue(prototype) as CellModel[,];
            if (cells == null) return;
            if (states == null || states.GetLength(0) != cells.GetLength(0) || states.GetLength(1) != cells.GetLength(1))
            {
                SnapshotCells();
                return;
            }

            for (var r = 0; r < cells.GetLength(0); r++)
            for (var c = 0; c < cells.GetLength(1); c++)
            {
                var before = states[r, c];
                var after = cells[r, c].State;
                if (before == after) continue;

                if (after == CellState.Corrupted && before != CellState.Corrupted)
                {
                    PlayRefSpawn();
                    if (PrototypeLevelRuntime.CurrentIndex == 1)
                        l2RefSpawned = true;
                }

                states[r, c] = after;
            }
        }

        private void PlayRefSpawn()
        {
            if (refSpawn == null || refSource == null) return;
            refSource.pitch = 1f;
            refSource.volume = sfxVolume;
            refSource.PlayOneShot(refSpawn, 0.60f);
        }

        private void ObservePsychosisManifestations()
        {
            // Psychosis v2 creates a visual candidate first, then the pacing director may suppress it with Destroy().
            // Destroy is deferred until the end of the frame, so sounding a root immediately also sounded suppressed
            // candidates. Confirm it one frame later: accepted roots survive, suppressed roots compare as null.
            ConfirmPendingPsychosisRoots();

            foreach (var rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (rect == null || !rect.gameObject.activeInHierarchy) continue;
                var name = rect.gameObject.name;
                if (!IsPsychosisRoot(name)) continue;
                var id = rect.gameObject.GetInstanceID();
                if (knownPsychosisRoots.Contains(id) || pendingPsychosisRoots.ContainsKey(id)) continue;
                pendingPsychosisRoots[id] = rect.gameObject;
            }

            var day = CurrentDay();
            if (day < 2) return;
            foreach (var text in FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (text == null || text.transform.parent == null) continue;
                var root = text.transform.parent.gameObject;
                if (!root.name.StartsWith("Cell Message ", StringComparison.Ordinal)) continue;
                var id = root.GetInstanceID();
                if (knownMessageRoots.Add(id)) PlayPsychosisManifest();
            }
        }

        private void ConfirmPendingPsychosisRoots()
        {
            if (pendingPsychosisRoots.Count == 0) return;

            foreach (var pair in pendingPsychosisRoots)
            {
                var root = pair.Value;
                if (root == null || !root.activeInHierarchy) continue;
                if (knownPsychosisRoots.Add(pair.Key))
                    PlayPsychosisManifest();
            }

            pendingPsychosisRoots.Clear();
        }

        private static bool IsPsychosisRoot(string name)
        {
            return name == "Psychosis v2 False Key" ||
                   name == "Psychosis v2 Ghost Selection" ||
                   name == "Psychosis v2 Column Drift" ||
                   name == "Psychosis v2 Row Drift" ||
                   name == "Psychosis v2 Cell Escape" ||
                   name == "Psychosis v2 Grid Tear";
        }

        private void PlayPsychosisManifest()
        {
            if (psychosisManifest == null || psychosisSource == null) return;
            var now = Time.unscaledTime;
            if (now - lastPsychosisAt < PsychosisBurstGate) return;
            lastPsychosisAt = now;

            // One dedicated source means manifestations never stack copies of this cue.
            if (psychosisSource.isPlaying) psychosisSource.Stop();
            psychosisRandomGain = UnityEngine.Random.Range(0.35f, 0.55f);
            psychosisSource.pitch = UnityEngine.Random.Range(0.97f, 1.03f);
            psychosisSource.volume = psychosisRandomGain * sfxVolume;
            psychosisSource.clip = psychosisManifest;
            psychosisSource.Play();
        }

        private void UpdateRequestedMusic()
        {
            AudioClip desired;
            if (prototype == null)
            {
                desired = lateLedger;
            }
            else
            {
                var level = PrototypeLevelRuntime.CurrentIndex;
                desired = level switch
                {
                    <= 0 => lateLedger,
                    1 => l2RefSpawned ? spreadsheetDrift : lateLedger,
                    2 => spreadsheetDrift,
                    _ => shiftedCells
                };
            }

            RequestMusic(desired, false);
        }

        private void RequestMusic(AudioClip desired, bool immediate)
        {
            if (desired == null) return;
            if (requestedTrack == desired && activeMusic != null) return;
            requestedTrack = desired;

            if (activeMusic == null || immediate)
            {
                if (musicA.isPlaying) musicA.Stop();
                if (musicB.isPlaying) musicB.Stop();
                activeMusic = musicA;
                incomingMusic = null;
                outgoingMusic = null;
                activeMusic.clip = desired;
                activeMusic.volume = MusicBaseVolume * musicVolume;
                activeMusic.Play();
                crossfadeT = 1f;
                return;
            }

            outgoingMusic = activeMusic;
            incomingMusic = activeMusic == musicA ? musicB : musicA;
            incomingMusic.Stop();
            incomingMusic.clip = desired;
            incomingMusic.volume = 0f;
            incomingMusic.Play();
            activeMusic = incomingMusic;
            crossfadeT = 0f;

            Debug.Log($"[AUDIO/MUSIC] crossfade -> {desired.name}.");
        }

        private void AdvanceCrossfade()
        {
            if (activeMusic == null) return;

            if (incomingMusic == null || outgoingMusic == null || crossfadeT >= 1f)
            {
                activeMusic.volume = MusicBaseVolume * musicVolume;
                return;
            }

            crossfadeT = Mathf.Clamp01(crossfadeT + Time.unscaledDeltaTime / CrossfadeSeconds);
            var t = Mathf.SmoothStep(0f, 1f, crossfadeT);
            incomingMusic.volume = MusicBaseVolume * musicVolume * t;
            outgoingMusic.volume = MusicBaseVolume * musicVolume * (1f - t);

            if (crossfadeT < 1f) return;
            outgoingMusic.Stop();
            outgoingMusic.volume = 0f;
            outgoingMusic = null;
            incomingMusic = null;
        }

        private void ReadLiveSettings()
        {
            application ??= FindFirstObjectByType<ExcelHellApplication>(FindObjectsInactive.Include);
            if (application == null) return;
            var screen = SettingsScreenField?.GetValue(application) as GameObject;
            if (screen == null || !screen.activeInHierarchy) return;

            var music = MusicSliderField?.GetValue(application) as Slider;
            var effects = SfxSliderField?.GetValue(application) as Slider;
            if (music != null) musicVolume = Mathf.Clamp01(music.value);
            if (effects != null) sfxVolume = Mathf.Clamp01(effects.value);
            ApplyCurrentVolumes();
        }

        private void OnAudioVolumesChanged(float music, float effects)
        {
            musicVolume = Mathf.Clamp01(music);
            sfxVolume = Mathf.Clamp01(effects);
            ApplyCurrentVolumes();
        }

        private void ApplyCurrentVolumes()
        {
            refSource.volume = sfxVolume;
            if (psychosisSource.isPlaying)
                psychosisSource.volume = psychosisRandomGain * sfxVolume;
            AdvanceCrossfade();
        }

        private static int CurrentDay()
        {
            if (FindFirstObjectByType<ExcelHellPrototype>() == null) return 0;
            return PrototypeLevelRuntime.CurrentIndex + 1;
        }

        private void StopAllNewAudio()
        {
            if (musicA != null) musicA.Stop();
            if (musicB != null) musicB.Stop();
            if (refSource != null) refSource.Stop();
            if (psychosisSource != null) psychosisSource.Stop();
        }

        private AudioSource CreateSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private static AudioClip Load(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null) Debug.LogWarning($"[AUDIO/FINAL] missing {path}");
            return clip;
        }
    }
}
