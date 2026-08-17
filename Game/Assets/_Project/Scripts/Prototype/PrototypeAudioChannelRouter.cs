using System.Reflection;
using ExcelHell.Application;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Release-scope settings bridge for legacy SFX and the office clock.
    /// The final SUNO music pass owns its own crossfade volumes and reads the same settings independently.
    /// Master volume remains global through AudioListener.volume.
    /// </summary>
    [DefaultExecutionOrder(2460)]
    public sealed class PrototypeAudioChannelRouter : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float ClockBaseVolume = 0.45f;
        private static readonly FieldInfo SfxField = typeof(PrototypeAudioDirector).GetField("sfx", Flags);
        private static readonly FieldInfo ClockAudioField = typeof(PrototypeOfficeClock).GetField("clockAudio", Flags);

        private static readonly FieldInfo SettingsScreenField = typeof(ExcelHellApplication).GetField("settingsScreen", Flags);
        private static readonly FieldInfo MusicSliderField = typeof(ExcelHellApplication).GetField("musicSlider", Flags);
        private static readonly FieldInfo SfxSliderField = typeof(ExcelHellApplication).GetField("sfxSlider", Flags);

        private PrototypeAudioDirector director;
        private PrototypeOfficeClock officeClock;
        private ExcelHellApplication application;
        private AudioSource sfx;
        private AudioSource clockAudio;

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

            var currentClock = FindFirstObjectByType<PrototypeOfficeClock>(FindObjectsInactive.Include);
            if (currentClock != officeClock) BindClock(currentClock);

            application ??= FindFirstObjectByType<ExcelHellApplication>(FindObjectsInactive.Include);
            ReadLiveSettingsSliders();
            ApplyChannelVolumes();
        }

        private void Bind(PrototypeAudioDirector value)
        {
            director = value;
            sfx = director != null ? SfxField?.GetValue(director) as AudioSource : null;
            ApplyChannelVolumes();
        }

        private void BindClock(PrototypeOfficeClock value)
        {
            officeClock = value;
            clockAudio = officeClock != null ? ClockAudioField?.GetValue(officeClock) as AudioSource : null;
            ApplyChannelVolumes();
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
            if (sfx != null) sfx.volume = sfxVolume;
            if (clockAudio != null) clockAudio.volume = ClockBaseVolume * sfxVolume;
        }
    }
}
