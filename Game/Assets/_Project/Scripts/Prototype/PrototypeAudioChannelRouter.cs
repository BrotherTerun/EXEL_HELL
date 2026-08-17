using System.Reflection;
using ExcelHell.Application;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Release-scope channel router between application settings and the runtime audio pass.
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

        private static readonly FieldInfo SettingsScreenField = typeof(ExcelHellApplication).GetField("settingsScreen", Flags);
        private static readonly FieldInfo MusicSliderField = typeof(ExcelHellApplication).GetField("musicSlider", Flags);
        private static readonly FieldInfo SfxSliderField = typeof(ExcelHellApplication).GetField("sfxSlider", Flags);

        private PrototypeAudioDirector director;
        private ExcelHellApplication application;
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

            application ??= FindFirstObjectByType<ExcelHellApplication>(FindObjectsInactive.Include);
            ReadLiveSettingsSliders();
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

        private void ReadLiveSettingsSliders()
        {
            if (application == null) return;
            var settingsScreen = SettingsScreenField?.GetValue(application) as GameObject;
            if (settingsScreen == null || !settingsScreen.activeInHierarchy) return;

            var musicSlider = MusicSliderField?.GetValue(application) as Slider;
            var sfxSlider = SfxSliderField?.GetValue(application) as Slider;
            if (musicSlider != null) musicVolume = Mathf.Clamp01(musicSlider.value);
            if (sfxSlider != null) sfxVolume = Mathf.Clamp01(sfxSlider.value);
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
