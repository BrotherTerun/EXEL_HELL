using System.Reflection;
using ExcelHell.Application;
using UnityEngine;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Release-scope channel router between persisted application settings and the runtime audio pass.
    /// Music controls the two soundtrack beds plus the final musical stinger.
    /// SFX controls office ambience and every one-shot UI/gameplay cue.
    /// Master volume remains global through AudioListener.volume.
    /// </summary>
    [DefaultExecutionOrder(2460)]
    public sealed class PrototypeAudioChannelRouter : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo NormalField = typeof(PrototypeAudioDirector).GetField("normal", Flags);
        private static readonly FieldInfo PsychosisField = typeof(PrototypeAudioDirector).GetField("psychosis", Flags);
        private static readonly FieldInfo AmbienceField = typeof(PrototypeAudioDirector).GetField("ambience", Flags);
        private static readonly FieldInfo SfxField = typeof(PrototypeAudioDirector).GetField("sfx", Flags);
        private static readonly FieldInfo StingerField = typeof(PrototypeAudioDirector).GetField("stinger", Flags);
        private static readonly FieldInfo NormalTargetField = typeof(PrototypeAudioDirector).GetField("normalTarget", Flags);
        private static readonly FieldInfo PsychosisTargetField = typeof(PrototypeAudioDirector).GetField("psychosisTarget", Flags);

        private PrototypeAudioDirector director;
        private AudioSource normal;
        private AudioSource psychosis;
        private AudioSource ambience;
        private AudioSource sfx;
        private AudioSource stinger;

        private float musicVolume = 0.8f;
        private float sfxVolume = 0.9f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeAudioChannelRouter>(FindObjectsInactive.Include) != null) return;
            var root = new GameObject("[PRESENTATION] Audio Channel Router");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeAudioChannelRouter>();
        }

        private void OnEnable()
        {
            AppSettingsService.AudioVolumesChanged += OnAudioVolumesChanged;
            var settings = AppSettingsService.Current ?? AppPersistence.LoadSettings();
            OnAudioVolumesChanged(settings.MusicVolume, settings.SfxVolume);
        }

        private void OnDisable()
        {
            AppSettingsService.AudioVolumesChanged -= OnAudioVolumesChanged;
        }

        private void LateUpdate()
        {
            var current = PrototypeAudioDirector.Instance;
            if (current != director) Bind(current);
            ApplyChannelVolumes();
        }

        private void Bind(PrototypeAudioDirector value)
        {
            director = value;
            normal = psychosis = ambience = sfx = stinger = null;
            if (director == null) return;

            normal = NormalField?.GetValue(director) as AudioSource;
            psychosis = PsychosisField?.GetValue(director) as AudioSource;
            ambience = AmbienceField?.GetValue(director) as AudioSource;
            sfx = SfxField?.GetValue(director) as AudioSource;
            stinger = StingerField?.GetValue(director) as AudioSource;
            ApplyChannelVolumes();

            Debug.Log($"[AUDIO] settings channels bound: music={musicVolume:0.00}, sfx={sfxVolume:0.00}.");
        }

        private void OnAudioVolumesChanged(float music, float effects)
        {
            musicVolume = Mathf.Clamp01(music);
            sfxVolume = Mathf.Clamp01(effects);
            ApplyChannelVolumes();
        }

        private void ApplyChannelVolumes()
        {
            if (director == null) return;

            var normalBase = NormalTargetField?.GetValue(director) is float n ? n : 0f;
            var psychosisBase = PsychosisTargetField?.GetValue(director) is float p ? p : 0f;

            if (normal != null) normal.volume = normalBase * musicVolume;
            if (psychosis != null) psychosis.volume = psychosisBase * musicVolume;
            if (stinger != null) stinger.volume = 0.65f * musicVolume;

            if (ambience != null) ambience.volume = 0.13f * sfxVolume;
            if (sfx != null) sfx.volume = sfxVolume;
        }
    }
}
