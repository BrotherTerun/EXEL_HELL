using System;
using System.Collections.Generic;
using Bayat.Unity.SaveGameFree;
using UnityEngine;

namespace ExcelHell.Application
{
    [Serializable]
    public sealed class AppProgressData
    {
        public int Version = 1;
        public int CurrentLevelIndex;
        public int HighestUnlockedLevelIndex;
        public bool CampaignCompleted;
        public List<string> NarrativeFlags = new();
        public string SavedAtUtc;
    }

    [Serializable]
    public sealed class AppSettingsData
    {
        public int Version = 1;
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 0.9f;
        public bool Fullscreen = true;
        public bool VSync = true;
        public int ResolutionWidth;
        public int ResolutionHeight;
    }

    public static class AppPersistence
    {
        private const string ProgressId = "excel_hell_progress.json";
        private const string SettingsId = "excel_hell_settings.json";

        public static bool HasProgress
        {
            get
            {
                try { return SaveGame.Exists(ProgressId); }
                catch (Exception exception)
                {
                    Debug.LogWarning($"EXEL HELL: progress existence check failed: {exception.Message}");
                    return false;
                }
            }
        }

        public static AppProgressData LoadProgress()
        {
            try
            {
                return SaveGame.Load(ProgressId, new AppProgressData()) ?? new AppProgressData();
            }
            catch (Exception exception)
            {
                Debug.LogError($"EXEL HELL: progress save is unreadable. Falling back to a fresh profile. {exception}");
                return new AppProgressData();
            }
        }

        public static void SaveProgress(int currentLevelIndex, int highestUnlockedLevelIndex = -1, bool completed = false,
            IEnumerable<string> narrativeFlags = null)
        {
            var previous = HasProgress ? LoadProgress() : new AppProgressData();
            previous.CurrentLevelIndex = Mathf.Max(0, currentLevelIndex);
            previous.HighestUnlockedLevelIndex = Mathf.Max(
                previous.HighestUnlockedLevelIndex,
                highestUnlockedLevelIndex < 0 ? previous.CurrentLevelIndex : highestUnlockedLevelIndex);
            previous.CampaignCompleted |= completed;
            previous.SavedAtUtc = DateTime.UtcNow.ToString("O");

            if (narrativeFlags != null)
            {
                previous.NarrativeFlags ??= new List<string>();
                foreach (var flag in narrativeFlags)
                    if (!string.IsNullOrWhiteSpace(flag) && !previous.NarrativeFlags.Contains(flag))
                        previous.NarrativeFlags.Add(flag);
            }

            try
            {
                SaveGame.Save(ProgressId, previous);
            }
            catch (Exception exception)
            {
                Debug.LogError($"EXEL HELL: progress save failed: {exception}");
            }
        }

        public static void DeleteProgress()
        {
            try
            {
                if (SaveGame.Exists(ProgressId)) SaveGame.Delete(ProgressId);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"EXEL HELL: progress deletion failed: {exception.Message}");
            }
        }

        public static AppSettingsData LoadSettings()
        {
            AppSettingsData settings;
            try
            {
                settings = SaveGame.Load(SettingsId, null as AppSettingsData);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"EXEL HELL: settings load failed, defaults will be used: {exception.Message}");
                settings = null;
            }

            settings ??= DefaultSettings();
            if (settings.ResolutionWidth <= 0 || settings.ResolutionHeight <= 0)
            {
                settings.ResolutionWidth = Screen.currentResolution.width;
                settings.ResolutionHeight = Screen.currentResolution.height;
            }
            return settings;
        }

        public static void SaveSettings(AppSettingsData settings)
        {
            if (settings == null) return;
            try
            {
                SaveGame.Save(SettingsId, settings);
            }
            catch (Exception exception)
            {
                Debug.LogError($"EXEL HELL: settings save failed: {exception}");
            }
        }

        public static AppSettingsData DefaultSettings()
        {
            return new AppSettingsData
            {
                ResolutionWidth = Screen.currentResolution.width,
                ResolutionHeight = Screen.currentResolution.height,
                Fullscreen = Screen.fullScreen,
                VSync = QualitySettings.vSyncCount > 0
            };
        }
    }

    public static class AppSettingsService
    {
        public static AppSettingsData Current { get; private set; }

        public static void LoadAndApply()
        {
            Current = AppPersistence.LoadSettings();
            Apply(Current, false);
        }

        public static void Apply(AppSettingsData settings, bool persist = true)
        {
            if (settings == null) return;
            Current = settings;
            settings.MasterVolume = Mathf.Clamp01(settings.MasterVolume);
            settings.MusicVolume = Mathf.Clamp01(settings.MusicVolume);
            settings.SfxVolume = Mathf.Clamp01(settings.SfxVolume);

            AudioListener.volume = settings.MasterVolume;
            QualitySettings.vSyncCount = settings.VSync ? 1 : 0;

            var mode = settings.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (settings.ResolutionWidth > 0 && settings.ResolutionHeight > 0)
                Screen.SetResolution(settings.ResolutionWidth, settings.ResolutionHeight, mode);

            if (persist) AppPersistence.SaveSettings(settings);
        }

        public static void ResetToDefaults()
        {
            Apply(AppPersistence.DefaultSettings());
        }
    }
}